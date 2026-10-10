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
using Old8Lang.Interpreter;

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

                return new DictionaryLangValue(pairs);
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
                throw new ArgumentException(
                    $"IL 模式暂不支持把原生类型 {value.GetType().Name} 转换为 Old8Lang 值");
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
                var result = new Dictionary<object, object?>();
                foreach (var tuple in dictionaryValue.Tuples)
                {
                    result[Unwrap(tuple.Get(0))!] = Unwrap(tuple.Get(1));
                }

                return result;
            }
            case IlDelegateFuncLangValue delegateFunc:
                return delegateFunc.Function;
            default:
                return value;
        }
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
        expression.LoadIlValue(ilGenerator, local);

        // 函数字面量：栈上是编译好的委托，直接按函数值包装。
        // 不能走下面的 OutputType 判断——lambda 的 OutputType 是它函数体的返回类型（可能是值类型），
        // 按它装箱会把委托对象当成值类型处理。
        if (expression is FuncLangValue)
        {
            ilGenerator.Emit(OpCodes.Call,
                typeof(IlValueBridge).GetMethod(nameof(WrapFunction))!);
            return;
        }

        Type? expressionType;
        try
        {
            expressionType = expression.OutputType(local);
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

    public override LangValueType Run(VariateManager manager) => Run(manager, []);

    public override LangValueType Run(VariateManager variateManagerFunc, List<LangExpression> ids, object? obj = null)
    {
        var arguments = ids.Select(id => IlValueBridge.Unwrap(id.Run(variateManagerFunc))).ToArray();
        var result = IlValueBridge.InvokeDelegate(Function, arguments);
        return IlValueBridge.Wrap(result);
    }

    public override string ToString() => Function.Method.Name;
}
