using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.ExceptionServices;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.Interpreter;

using Old8Lang.AST.Visitor;

namespace Old8Lang.InstanceMethods.Core;

/// <summary>
/// IL 模式下「原生值」与「Old8Lang 值」之间的桥接。
/// </summary>
/// <remarks>
/// IL 模式下局部变量保存的是原生 .NET 值（列表是 <c>List&lt;T&gt;</c>、数组是 <c>T[]</c>、
/// 字典是 <c>Dictionary&lt;K,V&gt;</c>，函数值是 .NET 委托），而实例方法的实现体是按
/// <see cref="LangValueType"/> 写的。这个桥接类负责两个方向的转换：
/// <list type="bullet">
/// <item>调用 helper 之前：<see cref="Wrap"/> 把接收者转成 Old8Lang 值，
/// <see cref="WrapFunction"/> 把编译好的委托包装成函数值；</item>
/// <item>调用函数值回调之前：<see cref="Unwrap"/> 把 Old8Lang 参数值转回原生值。</item>
/// </list>
/// 注意：这里只转换基础类型、字符串、字符、集合与函数值；其余类型原样传递。
/// </remarks>
public static class IlValueBridge
{
    /// <summary>
    /// 把 IL 模式下的原生值转换为 Old8Lang 值，集合会递归转换元素。
    /// </summary>
    public static LangValueType Wrap(object? value)
    {
        switch (value)
        {
            case null:
                return new NullLangValue();
            case LangValueType langValue:
                return langValue;
            case bool boolValue:
                return new BoolLangValue(boolValue);
            case int intValue:
                return new IntLangValue(intValue);
            case long longValue:
                return new IntLangValue((int)longValue);
            case short shortValue:
                return new IntLangValue(shortValue);
            case byte byteValue:
                return new IntLangValue(byteValue);
            case sbyte sbyteValue:
                return new IntLangValue(sbyteValue);
            case double doubleValue:
                return new DoubleLangValue(doubleValue);
            case float floatValue:
                return new DoubleLangValue(floatValue);
            case decimal decimalValue:
                return new DoubleLangValue((double)decimalValue);
            case string stringValue:
                return new StringLangValue(stringValue);
            case char charValue:
                return new CharLangValue(charValue);
            case Delegate function:
                return WrapFunction(function);
            case Task task:
                return ToTaskLangValue(task);
            case IDictionary dictionary:
            {
                var pairs = new List<KeyValuePair<LangExpression, LangExpression>>();
                foreach (DictionaryEntry entry in dictionary)
                {
                    pairs.Add(new KeyValuePair<LangExpression, LangExpression>(
                        Wrap(entry.Key), Wrap(entry.Value)));
                }

                var dictionaryValue = new DictionaryLangValue(pairs);
                // 构造只填 Tuples；语言层操作（ToStr / 索引 / 遍历）读的是 Value，
                // 不先解释器访问一次会得到 {(), ()} 或把已有键当缺失。
                dictionaryValue.Accept(RuntimeVisitors.CreateInterpreter(new VariateManager()));
                return dictionaryValue;
            }
            case Array array:
            {
                // 数组包成 ArrayLangValue（而不是 ListLangValue）：解释器里两者是不同的值类型，
                // 例如 FlatMap 只扁平化列表、不扁平化数组，包错会导致跨模式语义分歧。
                var elements = new List<LangValueType>();
                foreach (var element in array)
                {
                    elements.Add(Wrap(element));
                }

                return new ArrayLangValue(elements);
            }
            case System.Runtime.CompilerServices.ITuple nativeTuple:
            {
                // IL 里的元组是 ValueTuple / Tuple，直接包成 NativeAny 会让
                // 「元组解构 match」「元组实例方法」这些语言层逻辑认不出来。
                var tupleElements = new List<LangExpression>();
                for (var index = 0; index < nativeTuple.Length; index++)
                {
                    tupleElements.Add(Wrap(nativeTuple[index]));
                }

                var tupleValue = new TupleLangValue(tupleElements);
                // 语言层的 TupleLangValue 需要解释器访问一次来填充 ItemValues
                tupleValue.Accept(RuntimeVisitors.CreateInterpreter(new VariateManager()));
                return tupleValue;
            }
            case IEnumerable enumerable:
            {
                var items = new List<LangValueType>();
                foreach (var item in enumerable)
                {
                    items.Add(Wrap(item));
                }

                return new ListLangValue(items);
            }
            default:
                // 已特化的 Old8Lang 类与宿主 CLR 对象都可能作为集合元素流经
                // 语言层实例方法。保留原对象的 NativeAny 包装，使其能够再次
                // Unwrap 回原生对象，而不是在桥接边界直接失败。
                return new NativeAnyLangValue(value);
        }
    }

    /// <summary>
    /// 把 Old8Lang 值转换回 IL 模式下的原生值。
    /// </summary>
    public static object? Unwrap(LangValueType value)
    {
        switch (value)
        {
            case NullLangValue:
            case VoidLangValue:
                return null;
            case BoolLangValue boolValue:
                return boolValue.Value;
            case IntLangValue intValue:
                return intValue.Value;
            case DoubleLangValue doubleValue:
                return doubleValue.Value;
            case StringLangValue stringValue:
                return stringValue.Value;
            case CharLangValue charValue:
                return charValue.Value;
            case ListLangValue listValue:
                return listValue.Values.Select(Unwrap).ToList();
            case ArrayLangValue arrayValue:
                return arrayValue.GetItems().Select(Unwrap).ToArray();
            case DictionaryLangValue dictionaryValue:
            {
                // 解释器实现可能只填充 Tuples（Wrap 构造的字典）或只填充 Value（新建的字典），
                // 统一以 Value 为准，必要时先按 Tuples 求值一次。
                if (dictionaryValue.Value.Count == 0 && dictionaryValue.Tuples.Count > 0)
                {
                    dictionaryValue.Accept(RuntimeVisitors.CreateInterpreter(new VariateManager()));
                }

                var result = new Dictionary<object, object?>();
                foreach (var (key, itemValue) in dictionaryValue.Value)
                {
                    result[Unwrap(key)!] = Unwrap(itemValue);
                }

                return result;
            }
            case TupleLangValue tupleValue:
            {
                // 解释器实现返回的元组只带元素表达式，ItemValues 需要先经解释器访问填充。
                if (tupleValue.ItemValues.Count == 0)
                {
                    tupleValue.Accept(RuntimeVisitors.CreateInterpreter(new VariateManager()));
                }

                return tupleValue.ItemValues.Select(Unwrap).ToArray();
            }
            case IlDelegateFuncLangValue delegateFunc:
                return delegateFunc.Function;
            case NativeAnyLangValue nativeValue:
                return nativeValue.GetNativeObject();
            default:
                return value;
        }
    }

    /// <summary>
    /// 把 IL 运行期的动态值转换为逻辑 / 条件判断所需的原生 <see cref="bool"/>。
    /// </summary>
    /// <remarks>
    /// 静态类型为 <c>object</c> 的表达式（未标注类字段上的方法调用、字典索引结果等）
    /// 运行期可能是装箱的 <see cref="bool"/> 或语言值 <see cref="BoolLangValue"/>。
    /// 直接把这种引用当作 <c>bool</c> 参与 <c>brfalse</c> / <c>xor</c> 会按引用是否为空判断，
    /// 得到恒真或垃圾结果。这里按语言语义严格转换，非布尔值报错（与解释器一致）。
    /// </remarks>
    public static bool ToBoolean(object? value)
    {
        return value switch
        {
            bool boolean => boolean,
            BoolLangValue boolValue => boolValue.Value,
            null => false,
            _ => throw new InvalidOperationError(new SourcePosition(0, 0),
                $"期望布尔类型，实际得到了 {value.GetType().Name}")
        };
    }

    /// <summary>
    /// 生成「加载表达式 → 值类型装箱 → 调用 <see cref="Wrap"/>」的 IL 序列。
    /// </summary>
    /// <remarks>
    /// <see cref="Wrap"/> 的形参是 <see cref="object"/>，IL 里把 <c>int</c>/<c>bool</c>/<c>double</c>
    /// 这类值类型直接传给它是**非法的**（CLR 不会自动装箱），必须显式 <c>box</c>。
    /// 所有实例方法的 IL 实现都应通过这个方法转换接收者与参数，避免漏装箱导致无效 IL。
    /// </remarks>
    public static void EmitLoadWrapped(LangExpression expression, ILGenerator ilGenerator, LocalManager local)
    {
        expression.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));

        // 函数字面量：栈上是编译好的委托，直接按函数值包装。
        // 不能走下面的 OutputType 判断——lambda 的 OutputType 是它函数体的返回类型（可能是值类型），
        // 按它装箱会把委托对象当成值类型处理。
        if (expression is FuncLangValue or LambdaExpression)
        {
            ilGenerator.Emit(OpCodes.Call,
                typeof(IlValueBridge).GetMethod(nameof(WrapFunction))!);
            return;
        }

        Type? expressionType;
        try
        {
            expressionType = expression.Accept(new TypeInferenceVisitor(local));
        }
        catch
        {
            expressionType = null;
        }

        if (expressionType is { IsValueType: true })
        {
            ilGenerator.Emit(OpCodes.Box, expressionType);
        }

        ilGenerator.Emit(OpCodes.Call, typeof(IlValueBridge).GetMethod(nameof(Wrap))!);
    }

    /// <summary>
    /// 把 IL 模式下编译好的委托包装成函数值，使其可以被按 <see cref="LangValueType"/> 实现的实例方法使用。
    /// </summary>
    public static LangValueType WrapFunction(Delegate function) => new IlDelegateFuncLangValue(function);

    /// <summary>
    /// 把 IL 模式下的原生 <see cref="Task"/> 包装成 <see cref="TaskLangValue"/>。
    /// </summary>
    /// <remarks>
    /// IL 模式下 <c>Task.Delay</c> 得到的是原生 <see cref="Task"/>，<c>Task.Run</c> 得到的是
    /// <c>Task&lt;object&gt;</c>；而 Task 的实例方法（Then / Catch / Finally / ContinueWith）都以
    /// <see cref="TaskLangValue"/> 为接收者。
    /// </remarks>
    public static TaskLangValue ToTaskLangValue(Task task)
    {
        switch (task)
        {
            case Task<LangValueType> typedTask:
                return new TaskLangValue(typedTask);
            case Task<object> objectTask:
                return new TaskLangValue(objectTask.ContinueWith(
                    t => t.IsFaulted
                        // 直接透传原始异常：否则 t.Result 抛出的 AggregateException 会被再包一层，
                        // Catch 拿到的消息会变成「One or more errors occurred. (真实消息)」。
                        ? throw (t.Exception!.InnerException ?? t.Exception)
                        : Wrap(t.Result), CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default));
            default:
                return new TaskLangValue(task.ContinueWith(
                    t => t.IsFaulted
                        ? throw (t.Exception!.InnerException ?? t.Exception)
                        : (LangValueType)new VoidLangValue(), CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default));
        }
    }

    /// <summary>
    /// 调用委托，并把内部异常原样抛出（而不是包一层 <see cref="System.Reflection.TargetInvocationException"/>）。
    /// </summary>
    public static object? InvokeDelegate(Delegate function, params object?[] arguments)
    {
        try
        {
            return function.DynamicInvoke(arguments);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }

    /// <summary>
    /// 调用静态类型未知（例如 <c>object</c> 槽位）的函数值。
    /// </summary>
    /// <remarks>
    /// 函数返回值、字典 / 集合里取出的函数值在 IL 里的静态类型通常是 <c>object</c>，
    /// 无法走「已知委托类型的局部变量」那条快路径。这里按运行期实际类型调用；
    /// 非函数值报错而不是静默返回 null。
    /// </remarks>
    public static object? InvokeFunctionValue(object? function, object?[] arguments)
    {
        return function switch
        {
            null => null,
            Delegate del => InvokeDelegate(del, arguments),
            IlDelegateFuncLangValue wrapped => InvokeDelegate(wrapped.Function, arguments),
            _ => throw new InvalidOperationException($"对象 '{function}' 不是函数值，无法调用")
        };
    }

    /// <summary>
    /// 调用委托并把结果当作布尔值使用。
    /// </summary>
    public static bool InvokePredicate(Delegate function, object? argument)
    {
        var result = InvokeDelegate(function, argument);
        return result switch
        {
            null => false,
            bool boolValue => boolValue,
            BoolLangValue boolLangValue => boolLangValue.Value,
            _ => throw new ArgumentException($"谓词必须返回布尔值，实际返回 {result.GetType().Name}")
        };
    }

    /// <summary>
    /// 把原生值当作数值使用。
    /// </summary>
    public static double ToDouble(object? value) => value switch
    {
        int intValue => intValue,
        long longValue => longValue,
        short shortValue => shortValue,
        byte byteValue => byteValue,
        sbyte sbyteValue => sbyteValue,
        double doubleValue => doubleValue,
        float floatValue => floatValue,
        decimal decimalValue => (double)decimalValue,
        char charValue => charValue,
        IntLangValue intLangValue => intLangValue.Value,
        DoubleLangValue doubleLangValue => doubleLangValue.Value,
        CharLangValue charLangValue => charLangValue.Value,
        _ => throw new ArgumentException($"无法把 {value?.GetType().Name ?? "null"} 当作数值")
    };

    /// <summary>
    /// 把原生值当作整数使用。
    /// </summary>
    public static long ToInt64(object? value) => value switch
    {
        int intValue => intValue,
        long longValue => longValue,
        short shortValue => shortValue,
        byte byteValue => byteValue,
        sbyte sbyteValue => sbyteValue,
        double doubleValue => (long)doubleValue,
        float floatValue => (long)floatValue,
        decimal decimalValue => (long)decimalValue,
        char charValue => charValue,
        IntLangValue intLangValue => intLangValue.Value,
        DoubleLangValue doubleLangValue => (long)doubleLangValue.Value,
        CharLangValue charLangValue => charLangValue.Value,
        _ => throw new ArgumentException($"无法把 {value?.GetType().Name ?? "null"} 当作整数")
    };
}

/// <summary>
/// IL 模式下由编译好的 .NET 委托驱动的函数值。
/// </summary>
/// <remarks>
/// 继承 <see cref="FuncLangValue"/> 是为了兼容既有实例方法实现里
/// <c>is FuncLangValue</c> 的类型检查。形参列表按委托的真实参数构造，
/// 这样按 <c>func.Ids.Count</c> 区分「比较器模式 / 选择器模式」的实现（例如 SortBy）也能正确分支。
/// </remarks>
internal sealed class IlDelegateFuncLangValue(Delegate function)
    : FuncLangValue(null, BuildParameterIds(function), new BlockStatement([]), position: default, isLambda: true)
{
    /// <summary>
    /// 被包装的委托。
    /// </summary>
    public Delegate Function { get; } = function;

    private static List<LangId> BuildParameterIds(Delegate function) =>
        function.Method.GetParameters()
            .Select(parameter => new LangId(parameter.Name ?? "arg"))
            .ToList();
    public override LangValueType Run(VariateManager variateManagerFunc, List<LangExpression> ids, object? obj = null)
    {
        var arguments = ids.Select(id => IlValueBridge.Unwrap(id.Accept(RuntimeVisitors.CreateInterpreter(variateManagerFunc)))).ToArray();
        var result = IlValueBridge.InvokeDelegate(Function, arguments);
        return IlValueBridge.Wrap(result);
    }

    public override string ToString() => Function.Method.Name;
}
