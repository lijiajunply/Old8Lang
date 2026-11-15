using System.Collections;
using System.Collections.Concurrent;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Visitor;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Core;

/// <summary>
/// IL 模式下「原生集合接收者 → 解释器实例方法实现」的桥接。
/// </summary>
/// <remarks>
/// 仍有部分 <c>List</c> / <c>Dictionary</c> / <c>Array</c> 实例方法没有自己的 IL 实现，
/// 或旧实现只认 <see cref="ListLangValue"/> 形式的接收者。IL 模式下列表、数组、字典在栈上是
/// 原生 <see cref="List{T}"/> / <c>T[]</c> / <see cref="Dictionary{TKey,TValue}"/>，此时
/// <see cref="Old8Lang.AST.Expression.OperationHelpers.InstanceMethodTypeResolver"/> 会放弃解析，
/// 编译期报「方法未找到」。
/// <para>
/// 编译期在注册表解析失败后改为发射一次 <see cref="Invoke"/> 调用：这里把原生接收者与实参包装成
/// 语言值，按实例方法注册表解析并执行解释器实现，最后把结果还原成原生值。变异方法（增删改清、
/// 数组 <c>Set</c>）在执行后把包装值的变化同步回原生集合，因此别名仍能看到同一对象的变化。
/// </para>
/// </remarks>
public static class IlNativeCollectionBridge
{
    /// <summary>会原地修改列表的方法名（大小写不敏感）。</summary>
    private static readonly HashSet<string> ListMutators = new(StringComparer.OrdinalIgnoreCase)
    {
        "Add", "AddList", "AddRange", "Insert", "Remove", "RemoveAt", "Clear", "Pop"
    };

    /// <summary>会原地修改字典的方法名（大小写不敏感）。</summary>
    private static readonly HashSet<string> DictionaryMutators = new(StringComparer.OrdinalIgnoreCase)
    {
        "Add", "Remove", "Clear", "Update"
    };

    /// <summary>会原地修改数组的方法名（大小写不敏感）。</summary>
    private static readonly HashSet<string> ArrayMutators = new(StringComparer.OrdinalIgnoreCase)
    {
        "Set"
    };

    /// <summary>
    /// 编译期已解析出的具体方法类型缓存：运行期按类型复用同一实例，避免重复构造，
    /// 也避免仅凭参数个数在重载组里选错（例如 <c>First(predicate)</c> 与无参 <c>First()</c>）。
    /// </summary>
    private static readonly ConcurrentDictionary<Type, IInstanceMethod> MethodCache = new();

    /// <summary>
    /// 执行一次桥接调用；方法不存在时抛出参数错误，供编译期保证可解析的调用点使用。
    /// </summary>
    /// <param name="receiver">原生集合接收者（列表、数组、字典），也允许直接传语言值。</param>
    /// <param name="methodName">语言层方法名。</param>
    /// <param name="arguments">已经转换为语言值（或可被 <see cref="IlValueBridge.Wrap"/> 处理）的实参。</param>
    /// <param name="methodType">编译期解析出的具体实例方法类型；为空时按名称重新解析（动态接收者路径）。</param>
    /// <returns>还原成原生表示的调用结果。</returns>
    public static object? Invoke(object? receiver, string methodName, object?[] arguments, Type? methodType)
    {
        if (receiver is null)
        {
            // 与 object 动态分派路径保持同一种语言层错误。
            throw new NullReferenceException($"无法在 null 上调用方法 '{methodName}'");
        }

        if (TryInvoke(receiver, methodName, arguments, methodType, out var result))
        {
            return result;
        }

        var receiverName = receiver.GetType().Name;
        throw new ArgumentException($"类型 '{receiverName}' 不支持方法 '{methodName}'");
    }

    /// <summary>
    /// 尝试执行一次桥接调用；接收者不是受支持的集合时返回 <c>false</c>。
    /// </summary>
    /// <param name="receiver">原生集合接收者或语言值。</param>
    /// <param name="methodName">语言层方法名。</param>
    /// <param name="arguments">实参（语言值或原生值）。</param>
    /// <param name="methodType">编译期解析出的具体实例方法类型；为空时按名称重新解析。</param>
    /// <param name="result">还原成原生表示的调用结果。</param>
    /// <returns>是否完成了一次语言层实例方法调用。</returns>
    public static bool TryInvoke(object? receiver, string methodName, object?[] arguments,
        Type? methodType, out object? result)
    {
        result = null;
        if (receiver is null)
        {
            return false;
        }

        var wrapped = receiver is System.Runtime.CompilerServices.ITuple && receiver is not TupleLangValue
            ? WrapFlatTuple((System.Runtime.CompilerServices.ITuple)receiver)
            : WrapFully(receiver);
        if (wrapped is not (ListLangValue or ArrayLangValue or DictionaryLangValue or TupleLangValue))
        {
            return false;
        }

        InstanceMethodInitializer.EnsureInitialized();
        var argumentExpressions = arguments
            .Select(argument => (LangExpression)new BridgedArgumentExpression(WrapFully(argument)))
            .ToList();

        IInstanceMethod? method;
        if (methodType is not null && typeof(IInstanceMethod).IsAssignableFrom(methodType))
        {
            method = MethodCache.GetOrAdd(methodType,
                static type => (IInstanceMethod)Activator.CreateInstance(type)!);
        }
        else
        {
            method = InstanceMethodRegistry.Instance.ResolveMethod(
                wrapped.GetType(), methodName, argumentExpressions, local: null);
        }

        if (method is null)
        {
            return false;
        }

        var manager = new VariateManager();
        var value = method.Execute(wrapped, argumentExpressions, manager, default);
        // 别名（例如字典的 Delete / delete）通过方法自身的规范名判断，而不是调用点写法。
        var canonicalName = method.Names.Length > 0 ? method.Names[0] : methodName;
        SyncBack(wrapped, receiver, canonicalName);
        // 元组保持语言值，链式 .ToStr() / 切片显示才与解释器一致；其余结果还原成原生值。
        result = value is TupleLangValue ? value : IlValueBridge.Unwrap(value);
        return true;
    }

    /// <summary>
    /// 把原生值包装成语言值；字典需要额外执行一次解释器访问，把 <c>Tuples</c> 填充进 <c>Value</c>。
    /// </summary>
    private static LangValueType WrapFully(object? value)
    {
        var wrapped = IlValueBridge.Wrap(value);
        if (wrapped is DictionaryLangValue dictionary &&
            dictionary.Value.Count == 0 && dictionary.Tuples.Count > 0)
        {
            // IlValueBridge.Wrap 只构造 Tuples；解释器实现的 ExecuteInternal 直接读 Value。
            // 已填充 Value 的字典（如 Clone 的返回值）不能再次访问，否则会被清空重建。
            dictionary.Accept(RuntimeVisitors.CreateInterpreter(new VariateManager()));
        }

        return wrapped;
    }

    /// <summary>
    /// 把变异方法的包装值变化同步回原生集合。
    /// </summary>
    /// <remarks>
    /// 先完成全部值的还原与类型校验，再修改原生容器，避免中途失败把容器留在
    /// 「已清空或部分写入」的状态。
    /// </remarks>
    private static void SyncBack(LangValueType wrapped, object nativeReceiver, string canonicalName)
    {
        switch (nativeReceiver)
        {
            // 数组必须排在 IList 之前：CLR 数组同时实现 IList，但长度固定。
            case Array array when wrapped is ArrayLangValue arrayValue && ArrayMutators.Contains(canonicalName):
            {
                var elementType = array.GetType().GetElementType();
                var items = new List<object?>();
                foreach (var item in arrayValue.GetItems())
                {
                    var nativeItem = IlValueBridge.Unwrap(item);
                    if (elementType is not null && nativeItem is not null &&
                        !elementType.IsInstanceOfType(nativeItem))
                    {
                        throw new ArgumentException(
                            $"无法把 {nativeItem.GetType().Name} 写入 {elementType.Name} 数组");
                    }

                    items.Add(nativeItem);
                }

                var count = Math.Min(array.Length, items.Count);
                for (var index = 0; index < count; index++)
                {
                    array.SetValue(items[index], index);
                }

                break;
            }
            case IDictionary dictionary
                when wrapped is DictionaryLangValue dictionaryValue && DictionaryMutators.Contains(canonicalName):
            {
                var pairs = new List<(object Key, object? Value)>(dictionaryValue.Value.Count);
                foreach (var (key, value) in dictionaryValue.Value)
                {
                    var nativeKey = IlValueBridge.Unwrap(key);
                    if (nativeKey is null)
                    {
                        throw new ArgumentException("字典的键不能为 null");
                    }

                    pairs.Add((nativeKey, IlValueBridge.Unwrap(value)));
                }

                dictionary.Clear();
                foreach (var (key, value) in pairs)
                {
                    dictionary[key] = value;
                }

                break;
            }
            case IList list when wrapped is ListLangValue listValue && ListMutators.Contains(canonicalName):
            {
                var elementType = GetListElementType(list.GetType());
                var items = new List<object?>();
                foreach (var item in listValue.Values)
                {
                    var nativeItem = IlValueBridge.Unwrap(item);
                    if (elementType is not null && nativeItem is not null &&
                        !elementType.IsInstanceOfType(nativeItem))
                    {
                        throw new ArgumentException(
                            $"无法把 {nativeItem.GetType().Name} 加入 {elementType.Name} 列表");
                    }

                    items.Add(nativeItem);
                }

                list.Clear();
                foreach (var item in items)
                {
                    list.Add(item);
                }

                break;
            }
        }
    }

    /// <summary>取 <see cref="List{T}"/> 的元素类型；非泛型 <see cref="IList"/> 返回 <c>null</c>。</summary>
    private static Type? GetListElementType(Type listType)
    {
        if (listType.IsGenericType && listType.GetGenericTypeDefinition() == typeof(List<>))
        {
            return listType.GetGenericArguments()[0];
        }

        return null;
    }
    /// <summary>
    /// 把原生元组转成语言值元组。
    /// </summary>
    /// <remarks>
    /// 虚拟机的多元组是右嵌套 <c>Tuple&lt;object?, object?&gt;</c>（<c>(1,2,3)</c> 实为
    /// <c>(1, (2, 3))</c>），直接按 <see cref="System.Runtime.CompilerServices.ITuple.Length"/>
    /// 枚举会得到 2 个元素。这里按 VM 的编码递归展开末尾元组，并沿用 VM 的
    /// 「null 视为占位/忽略」约定；IL 的 <c>ValueTuple</c> 是扁平的，不受影响。
    /// </remarks>
    private static TupleLangValue WrapFlatTuple(System.Runtime.CompilerServices.ITuple tuple)
    {
        var items = new List<object?>();

        void Collect(System.Runtime.CompilerServices.ITuple current)
        {
            for (var i = 0; i < current.Length; i++)
            {
                var element = current[i];
                if (i == current.Length - 1 && element is Tuple<object?, object?> nested)
                {
                    Collect(nested);
                }
                else if (element is not null)
                {
                    items.Add(element);
                }
            }
        }

        Collect(tuple);

        var elements = items
            .Select(item => (LangExpression)IlValueBridge.Wrap(item))
            .ToList();
        var value = new TupleLangValue(elements);
        value.Accept(RuntimeVisitors.CreateInterpreter(new VariateManager()));
        return value;
    }

}

/// <summary>
/// 把已经求值的语言值作为表达式传给解释器实现。
/// </summary>
/// <remarks>
/// <see cref="Accept{TResult}"/> 直接返回包装值，而不是让访问器按 AST 重新构造它。
/// 对函数值尤其重要：IL 里的 lambda 由 <c>IlDelegateFuncLangValue</c> 承接，若按
/// <c>FuncLangValue</c> 节点重新访问，会退化成函数体为空的普通函数值，回调随即失败。
/// </remarks>
internal sealed class BridgedArgumentExpression(LangValueType value) : LangExpression
{
    /// <summary>已求值的语言值。</summary>
    public LangValueType Value { get; } = value;

    /// <inheritdoc />
    public override TResult Accept<TResult>(IVisitor<TResult> visitor) => (TResult)(object)Value;

}
