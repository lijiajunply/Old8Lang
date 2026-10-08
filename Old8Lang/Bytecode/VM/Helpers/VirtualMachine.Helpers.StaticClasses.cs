using System.Collections;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.StaticValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Bytecode.Closures;
using Old8Lang.Bytecode.Metadata;
using Old8Lang.Error;

// ReSharper disable once CheckNamespace
namespace Old8Lang.Bytecode.VM;

/// <summary>
/// VirtualMachine - 静态类 API（Task / Thread / Assert）
/// </summary>
/// <remarks>
/// 解释器把 <c>Task</c> / <c>Thread</c> / <c>Assert</c> 注册为运行期全局对象，由各类的
/// <c>Dot(Instance, manager)</c> 完成静态方法分发；字节码虚拟机没有这套对象，改为在编译期把
/// <c>类名.方法(参数)</c> 改写成带限定名的 <c>CallNative</c>（见 <c>BytecodeVisitor</c> 的 Dot 分支），
/// 由本文件在运行期分发。可达的方法集合由 <see cref="VmStaticClassRegistry"/> 描述。
/// </remarks>
public partial class VirtualMachine
{
    /// <summary>
    /// 尝试把限定名（如 <c>Task.Delay</c>）按静态类方法分发。
    /// </summary>
    /// <returns>该名字属于静态类方法时返回 true（结果写入 <paramref name="result"/>）</returns>
    private bool TryInvokeStaticClassMethod(string qualifiedName, object?[] args, out object? result)
    {
        result = null;

        int separator = qualifiedName.IndexOf('.');
        if (separator <= 0)
        {
            return false;
        }

        string className = qualifiedName[..separator];
        if (!VmStaticClassRegistry.IsStaticClass(className))
        {
            return false;
        }

        string canonicalMethod = qualifiedName[(separator + 1)..];
        var position = new SourcePosition();

        // 虚拟机下无法构造 CancellationToken（编译期遇到 CancellationToken 就会报 VmUnsupportedError），
        // 所以 Task.Delay 只可能有一个参数。这里给出比“参数类型不匹配”更贴近原因的说明。
        // 少于 1 个参数不在这里管：交给复用的实现报它自己的参数个数错误。
        if (qualifiedName == "Task.Delay" && args.Length > 1)
        {
            throw new VmUnsupportedError(position,
                $"Task.Delay 的第 2 个参数（CancellationToken）：本方法在虚拟机模式下只接受 1 个参数（毫秒），" +
                $"但提供了 {args.Length} 个");
        }

        switch (qualifiedName)
        {
            case "Task.Run":
            case "Task.StartNew":
                result = InvokeTaskRun(args, position, qualifiedName);
                return true;

            case "Assert.AssertThrows":
            case "Assert.AssertNotThrows":
                result = InvokeAssertThrows(qualifiedName, args, position);
                return true;

            case "Assert.AssertEqual":
            case "Assert.AssertNotEqual":
            case "Assert.AssertContainsItem":
            case "Assert.AssertNotContainsItem":
                result = InvokeAssertEquality(qualifiedName, args, position);
                return true;

            case "Assert.AssertInstanceOf":
            case "Assert.AssertNotInstanceOf":
                result = InvokeAssertInstanceOf(qualifiedName, args, position);
                return true;
        }

        result = InvokeReusableStaticClassMethod(className, canonicalMethod, args, position);
        return true;
    }

    #region 复用解释器实现

    /// <summary>
    /// 调用静态类在 <c>VmReusableMethods</c> 里登记的、可以直接复用的实现。
    /// </summary>
    private object? InvokeReusableStaticClassMethod(string className, string canonicalMethod, object?[] args,
        SourcePosition position)
    {
        var method = FindReusableStaticClassMethod(className, canonicalMethod)
                     ?? throw new InvalidOperationError(position,
                         $"虚拟机模式下暂不支持 {className}.{canonicalMethod}");

        var langArguments = args.Select(ToStaticClassArgument).ToList();

        try
        {
            return method(langArguments, position);
        }
        catch (Exception ex) when (className == "Assert" && ex is not Old8Exception and not VmException)
        {
            // Assert 类的断言失败用普通 Exception 表示（见 AssertClassLangValue 各方法）。
            // 统一成 AssertionError：它是 Old8Exception，语言层的 try/catch 与错误报告都更清晰，
            // 也便于 Assert.Throws 区分“被断言的异常”和“断言自身失败”。
            throw new AssertionError(position, StripAssertionPrefix(ex.Message));
        }
    }

    private static Func<List<LangValueType>, SourcePosition, LangValueType>? FindReusableStaticClassMethod(
        string className, string canonicalMethod) => className switch
    {
        "Task" => TaskClassLangValue.VmReusableMethods.GetValueOrDefault(canonicalMethod),
        "Thread" => ThreadClassLangValue.VmReusableMethods.GetValueOrDefault(canonicalMethod),
        "Assert" => AssertClassLangValue.VmReusableMethods.GetValueOrDefault(canonicalMethod),
        _ => null,
    };

    /// <summary>
    /// 把求值栈上的值转换成静态类实现要求的 <see cref="LangValueType"/> 视图。
    /// </summary>
    private LangValueType ToStaticClassArgument(object? value)
    {
        switch (value)
        {
            case null:
                return new NullLangValue();
            case LangValueType langValue:
                return langValue;
            case List<object?> list:
                return new ListLangValue(list.Select(ToStaticClassArgument).ToList());
            case object[] array:
                return new ArrayLangValue(array.Select(ToStaticClassArgument).ToList());
            case Dictionary<object, object?> dict:
            {
                var dictionary = new DictionaryLangValue();
                foreach (var (key, item) in dict)
                {
                    dictionary.Value.Add((ToStaticClassArgument(key), ToStaticClassArgument(item)));
                }

                return dictionary;
            }
        }

        try
        {
            return ConvertToLangValueType(value);
        }
        catch (CastError)
        {
            // 虚拟机自有对象（BytecodeObjectInstance、ClosureValue、FunctionMetadata ...）没有等价的
            // LangValueType 表示，包成 NativeAnyLangValue，语义上对应解释器里的“原生对象”。
            return new NativeAnyLangValue(value);
        }
    }

    /// <summary>
    /// 去掉消息里已有的“断言失败: ”前缀，避免 <see cref="AssertionError"/> 再加一次导致重复。
    /// </summary>
    private static string StripAssertionPrefix(string message)
    {
        const string prefix = "断言失败: ";
        return message.StartsWith(prefix, StringComparison.Ordinal) ? message[prefix.Length..] : message;
    }

    /// <summary>
    /// 取断言消息参数；没有提供时退回到默认文案。
    /// </summary>
    private string StaticClassMessage(object?[] args, int index, string defaultMessage)
    {
        if (args.Length <= index)
        {
            return StripAssertionPrefix(defaultMessage);
        }

        var message = ToStaticClassArgument(args[index]);
        return message is StringLangValue text
            ? text.Value
            : message.ToDisplayString();
    }

    #endregion

    #region Task

    /// <summary>
    /// <c>Task.Run</c> / <c>Task.StartNew</c>：在线程池上执行一个虚拟机函数值。
    /// </summary>
    private object? InvokeTaskRun(object?[] args, SourcePosition position, string qualifiedName)
    {
        if (args.Length != 1)
        {
            throw new ArgumentError(position, $"{qualifiedName} 期望 1 个参数(函数)，但提供了 {args.Length} 个");
        }

        var callable = args[0];
        if (callable is not (ClosureValue or FunctionMetadata))
        {
            throw new TypeError(position, "function", GetValueTypeName(callable));
        }

        // worker 虚拟机的创建与执行都必须发生在 Task.Run 的委托内部：求值栈、调用栈与帧池都按线程存放，
        // 在调用方线程上执行 worker 会让栈语义错位（与 OpCode.CallAsync 的既有写法保持一致）。
        var task = System.Threading.Tasks.Task.Run(() =>
        {
            var workerVm = CreateWorkerVirtualMachine();
            return workerVm.ConvertCallableResultToLangValue(workerVm.CallCallableAndGetResult(callable, []));
        });

        return new TaskLangValue(task, CancellationToken.None, position);
    }

    /// <summary>
    /// 把函数返回值包装成 <see cref="TaskLangValue"/> 要求的 <see cref="LangValueType"/>。
    /// </summary>
    private LangValueType ConvertCallableResultToLangValue(object? value)
    {
        // null 用 VoidLangValue 表示“没有值”，与 OpCode.CallAsync 的既有约定一致
        // （不能用 ConvertToLangValue：它对不认识的类型会静默返回 VoidLangValue，把结果丢掉）。
        return value is null ? new VoidLangValue() : ToStaticClassArgument(value);
    }

    #endregion

    #region Assert

    /// <summary>
    /// <c>Assert.Throws</c> / <c>Assert.NotThrows</c>：执行函数值并检查是否抛异常。
    /// </summary>
    private object? InvokeAssertThrows(string qualifiedName, object?[] args, SourcePosition position)
    {
        bool expectThrow = qualifiedName == "Assert.AssertThrows";
        string displayName = expectThrow ? "AssertThrows" : "AssertNotThrows";

        if (args.Length is < 1 or > 2)
        {
            throw new ArgumentError(position,
                $"{displayName} 期望 1-2 个参数(action, message)，但提供了 {args.Length} 个");
        }

        var callable = args[0];
        if (callable is not (ClosureValue or FunctionMetadata))
        {
            throw new TypeError(position, "function", GetValueTypeName(callable));
        }

        Exception? thrown = null;
        try
        {
            CallCallableSafely(callable, [], position);
        }
        catch (Exception ex)
        {
            // 断言自身失败（AssertionError）不是“被断言的异常”，必须原样抛出，
            // 否则 Assert.Throws(() -> Assert.True(false)) 会假通过。
            // 解释器按消息前缀“断言失败:”判断，会误伤；这里改按异常类型判断。
            if (ex is AssertionError)
            {
                throw;
            }

            thrown = ex;
        }

        if (expectThrow && thrown is null)
        {
            throw new AssertionError(position, StaticClassMessage(args, 1, "断言失败: 期望抛出异常但未抛出"));
        }

        if (!expectThrow && thrown is not null)
        {
            throw new AssertionError(position, StaticClassMessage(args, 1,
                $"断言失败: 期望不抛出异常但抛出了 {thrown.GetType().Name}: {thrown.Message}"));
        }

        return new VoidLangValue();
    }

    /// <summary>
    /// 相等语义的断言。
    /// </summary>
    /// <remarks>
    /// 刻意不复用 <c>AssertClassLangValue</c> 各方法，因为它们的相等判定在遇到不认识的类型时会退回
    /// 比较 <c>ToDisplayString()</c>，而虚拟机自有对象（<c>BytecodeObjectInstance</c>）的
    /// <c>ToString()</c> 只含类名，会把同一类的两个不同实例判成相等
    /// （<c>Assert.Equal</c> 假通过、<c>Assert.NotEqual</c> 假失败）。
    /// </remarks>
    private object? InvokeAssertEquality(string qualifiedName, object?[] args, SourcePosition position)
    {
        // 报错信息里用源码中的写法（Assert.Equal 而不是 Assert.AssertEqual）
        string displayName = qualifiedName[(qualifiedName.IndexOf('.') + 1)..];

        switch (qualifiedName)
        {
            case "Assert.AssertEqual":
            case "Assert.AssertNotEqual":
            {
                if (args.Length is < 2 or > 3)
                {
                    throw new ArgumentError(position,
                        $"Assert.{displayName} 期望 2-3 个参数(expected, actual, message)，但提供了 {args.Length} 个");
                }

                bool shouldBeEqual = qualifiedName == "Assert.AssertEqual";
                if (StaticClassValuesEqual(args[0], args[1]) != shouldBeEqual)
                {
                    string defaultMessage = shouldBeEqual
                        ? $"断言失败: 期望值 '{DisplayAssertValue(args[0])}' 但实际为 '{DisplayAssertValue(args[1])}'"
                        : $"断言失败: 期望值不为 '{DisplayAssertValue(args[0])}' 但实际相等";
                    throw new AssertionError(position, StaticClassMessage(args, 2, defaultMessage));
                }

                return new VoidLangValue();
            }

            case "Assert.AssertContainsItem":
            case "Assert.AssertNotContainsItem":
            {
                if (args.Length is < 2 or > 3)
                {
                    throw new ArgumentError(position,
                        $"Assert.{displayName} 期望 2-3 个参数(collection, item, message)，但提供了 {args.Length} 个");
                }

                bool shouldContain = qualifiedName == "Assert.AssertContainsItem";
                bool contains = GetStaticClassItems(args[0], position)
                    .Any(item => StaticClassValuesEqual(item, args[1]));
                if (contains != shouldContain)
                {
                    string defaultMessage = shouldContain
                        ? $"断言失败: 集合不包含元素 '{DisplayAssertValue(args[1])}'"
                        : $"断言失败: 集合包含元素 '{DisplayAssertValue(args[1])}'";
                    throw new AssertionError(position, StaticClassMessage(args, 2, defaultMessage));
                }

                return new VoidLangValue();
            }

            default:
                throw new InvalidOperationError(position, $"未处理的断言方法：{qualifiedName}");
        }
    }

    /// <summary>
    /// 断言里的相等判定。
    /// </summary>
    private bool StaticClassValuesEqual(object? left, object? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (IsOpaqueVmValue(left) || IsOpaqueVmValue(right))
        {
            // 类实例、函数值这类没有 LangValueType 表示的值：只能用虚拟机自身的相等语义
            // （认 _eq 运算符重载，否则引用相等）。两个方向都试一次，
            // 因为虚拟机的 Equals 只在左操作数是 LangValueType 时才换算右操作数。
            return Equals(left, right) || Equals(right, left);
        }

        // 其余值换算到解释器的口径后，沿用解释器的断言相等规则：
        // 这样 42 与 IntLangValue(42)、两个内容相同的列表、混合数值比较都能按解释器的结果判等。
        return AssertClassLangValue.AreEqual(ToStaticClassArgument(left), ToStaticClassArgument(right));
    }

    /// <summary>
    /// 该值是否是没有等价 <see cref="LangValueType"/> 表示的虚拟机自有值（类实例、函数值等）。
    /// </summary>
    /// <remarks>
    /// 判定口径与 <see cref="ToStaticClassArgument"/> 退回 <see cref="NativeAnyLangValue"/> 的条件一致。
    /// </remarks>
    private static bool IsOpaqueVmValue(object? value) =>
        value is not null
            and not LangValueType
            and not List<object?>
            and not object[]
            and not Dictionary<object, object?>
            and not int and not long and not short and not byte and not sbyte
            and not ushort and not uint and not ulong and not float and not double
            and not string and not bool and not char;

    /// <summary>
    /// 断言失败信息里的值展示。
    /// </summary>
    /// <remarks>
    /// 虚拟机自有对象的 <c>ToString()</c> 只含类名，同一类的两个不同实例会显示成完全一样的一串，
    /// 断言失败时无法分辨。这里改用“类型名#实例标识”。
    /// </remarks>
    private string DisplayAssertValue(object? value)
    {
        return IsOpaqueVmValue(value)
            ? $"{GetStaticClassTypeName(value)}#{System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value):x8}"
            : ToString(value);
    }

    /// <summary>
    /// <c>Assert.InstanceOf</c> / <c>Assert.NotInstanceOf</c>。
    /// </summary>
    private object? InvokeAssertInstanceOf(string qualifiedName, object?[] args, SourcePosition position)
    {
        bool expectInstance = qualifiedName == "Assert.AssertInstanceOf";
        string displayName = expectInstance ? "AssertInstanceOf" : "AssertNotInstanceOf";

        if (args.Length is < 2 or > 3)
        {
            throw new ArgumentError(position,
                $"{displayName} 期望 2-3 个参数(obj, type, message)，但提供了 {args.Length} 个");
        }

        string expectedType = ToString(args[1]);
        string actualType = GetStaticClassTypeName(args[0]);
        if (string.Equals(actualType, expectedType, StringComparison.OrdinalIgnoreCase) != expectInstance)
        {
            string defaultMessage = expectInstance
                ? $"断言失败: 期望类型为 '{expectedType}' 但实际为 '{actualType}'"
                : $"断言失败: 期望类型不为 '{expectedType}' 但实际为该类型";
            throw new AssertionError(position, StaticClassMessage(args, 2, defaultMessage));
        }

        return new VoidLangValue();
    }

    /// <summary>
    /// 取断言比较用的类型名。虚拟机自有对象用类名（与解释器里 <c>AnyLangValue</c> 的口径一致），
    /// 其余走 <see cref="LangValueType"/> 的口径（<c>Int</c> / <c>List</c> / <c>Dictionary</c> ...），
    /// 这样按解释器写好的断言在虚拟机下同样成立。
    /// </summary>
    private string GetStaticClassTypeName(object? value)
    {
        switch (value)
        {
            case null:
                return "Null";
            case BytecodeObjectInstance instance:
                return instance.ClassName;
            case LangValueType langValue:
                return langValue.TypeToString();
        }

        try
        {
            return ConvertToLangValueType(value).TypeToString();
        }
        catch (CastError)
        {
            return GetValueTypeName(value);
        }
    }

    private static IEnumerable<object?> GetStaticClassItems(object? collection, SourcePosition position)
        => collection switch
        {
            Array array => array.Cast<object?>(),
            ILangList langList => langList.GetItems().Cast<object?>(),
            IList list => list.Cast<object?>(),
            _ => throw new TypeError(position, "collection",
                collection?.GetType().Name ?? "null"),
        };

    #endregion

    #region 函数值调用

    /// <summary>
    /// 在当前虚拟机上同步执行一个函数值并丢弃它的返回值，同时保证求值栈恢复原状。
    /// </summary>
    /// <remarks>
    /// <c>ExecuteFrame</c> 的 finally 只归还 locals 与帧，不回滚求值栈，因此被调用帧在表达式求值中途
    /// 抛异常时会把自己的中间值留在栈上，污染调用方后续的 Pop/Add。这里照抄
    /// <c>VirtualMachine.ExecuteFunction</c> 的“快照 + 回滚”范式。
    /// </remarks>
    private void CallCallableSafely(object callable, object?[] args, SourcePosition position)
    {
        var snapshot = _stack.Count;
        try
        {
            InvokeCallable(callable, args, position);
        }
        finally
        {
            // 无论正常返回还是抛异常，都把这次调用在求值栈上留下的值（返回值与中途的中间值）清掉
            while (_stack.Count > snapshot)
            {
                _stack.Pop();
            }
        }
    }

    /// <summary>
    /// 执行一个函数值并取出返回值。用于 worker 虚拟机上的一次性调用：worker 用完即弃，
    /// 因此不需要回滚求值栈。
    /// </summary>
    private object? CallCallableAndGetResult(object? callable, object?[] args)
    {
        InvokeCallable(callable, args, new SourcePosition());
        return _stack.Count > 0 ? _stack.Pop() : null;
    }

    /// <summary>
    /// 调用一个虚拟机函数值（闭包或函数元数据）。
    /// </summary>
    private void InvokeCallable(object? callable, object?[] args, SourcePosition position)
    {
        switch (callable)
        {
            case ClosureValue closure:
                // 带上 ConstantPool：与 ExecuteCallDynamicInstruction 的动态调用路径一致
                // （CallFunctionObject 会丢掉它）。
                CallClosureFunction(closure.Function, args, closure.CapturedVariables, closure.ConstantPool);
                break;
            case FunctionMetadata function:
                CallFunction(function, args);
                break;
            default:
                throw new TypeError(position, "function", GetValueTypeName(callable));
        }
    }

    #endregion
}
