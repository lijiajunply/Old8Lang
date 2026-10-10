using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.StaticValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Task;

/// <summary>
/// <c>TaskCompletionSource</c> / <c>CancellationTokenSource</c> 实例方法的公共基类。
/// </summary>
/// <remarks>
/// 解释器里这两个类型的方法都由各自的 <c>LangValueType.Dot</c> 实现，虚拟机模式下没有
/// <c>VariateManager</c>，因此在这里给出 <c>ExecuteInVMInternal</c> 版本。
/// IL 模式本来就不支持这两个类型（<c>LoadIlValue</c> 直接抛错），所以 IL 分支保持抛错。
/// </remarks>
public abstract class CompletionSourceVmMethodBase(Type returnType) : BaseInstanceMethod
{
    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        throw new NotImplementedError(position, $"IL 模式暂不支持 {PrimaryName}");
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters,
        LocalManager local)
    {
        return returnType;
    }

    /// <summary>把虚拟机栈上的值转成语言值（函数值与原生对象走 ObjToValue 包装）</summary>
    protected static LangValueType ToLangValue(object? raw)
    {
        return raw as LangValueType ?? LangValueType.ObjToValue(raw);
    }
}

#region TaskCompletionSource

/// <summary>TaskCompletionSource.SetResult(result)</summary>
public class TaskCompletionSourceSetResultMethod : CompletionSourceVmMethodBase
{
    public TaskCompletionSourceSetResultMethod() : base(typeof(void)) { }

    public override string[] Names => ["SetResult"];
    public override Type TargetType => typeof(TaskCompletionSourceLangValue);
    public override string[]? ParameterNames => ["result"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        ((TaskCompletionSourceLangValue)instance).SetResult(parameters[0].Run(manager));
        return new VoidLangValue(position);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        ((TaskCompletionSourceLangValue)instance!).SetResult(ToLangValue(arguments[0]));
        return VoidLangValue.Instance;
    }
}

/// <summary>TaskCompletionSource.SetException(message)</summary>
public class TaskCompletionSourceSetExceptionMethod : CompletionSourceVmMethodBase
{
    public TaskCompletionSourceSetExceptionMethod() : base(typeof(void)) { }

    public override string[] Names => ["SetException"];
    public override Type TargetType => typeof(TaskCompletionSourceLangValue);
    public override string[]? ParameterNames => ["exception"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var message = ToExceptionMessage(parameters[0].Run(manager));
        ((TaskCompletionSourceLangValue)instance).SetException(new Exception(message));
        return new VoidLangValue(position);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        var message = ToExceptionMessage(ToLangValue(arguments[0]));
        ((TaskCompletionSourceLangValue)instance!).SetException(new Exception(message));
        return VoidLangValue.Instance;
    }

    internal static string ToExceptionMessage(LangValueType value)
    {
        return value is StringLangValue stringValue ? stringValue.Value : value.ToDisplayString();
    }
}

/// <summary>TaskCompletionSource.SetCanceled()</summary>
public class TaskCompletionSourceSetCanceledMethod : CompletionSourceVmMethodBase
{
    public TaskCompletionSourceSetCanceledMethod() : base(typeof(void)) { }

    public override string[] Names => ["SetCanceled"];
    public override Type TargetType => typeof(TaskCompletionSourceLangValue);
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        ((TaskCompletionSourceLangValue)instance).SetCanceled();
        return new VoidLangValue(position);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        ((TaskCompletionSourceLangValue)instance!).SetCanceled();
        return VoidLangValue.Instance;
    }
}

/// <summary>TaskCompletionSource.TrySetResult(result)</summary>
public class TaskCompletionSourceTrySetResultMethod : CompletionSourceVmMethodBase
{
    public TaskCompletionSourceTrySetResultMethod() : base(typeof(bool)) { }

    public override string[] Names => ["TrySetResult"];
    public override Type TargetType => typeof(TaskCompletionSourceLangValue);
    public override string[]? ParameterNames => ["result"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var success = ((TaskCompletionSourceLangValue)instance).SetResult(parameters[0].Run(manager));
        return new BoolLangValue(success, position);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        return ((TaskCompletionSourceLangValue)instance!).SetResult(ToLangValue(arguments[0]));
    }
}

/// <summary>TaskCompletionSource.TrySetException(message)</summary>
public class TaskCompletionSourceTrySetExceptionMethod : CompletionSourceVmMethodBase
{
    public TaskCompletionSourceTrySetExceptionMethod() : base(typeof(bool)) { }

    public override string[] Names => ["TrySetException"];
    public override Type TargetType => typeof(TaskCompletionSourceLangValue);
    public override string[]? ParameterNames => ["exception"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var message = TaskCompletionSourceSetExceptionMethod.ToExceptionMessage(parameters[0].Run(manager));
        var success = ((TaskCompletionSourceLangValue)instance).SetException(new Exception(message));
        return new BoolLangValue(success, position);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        var message = TaskCompletionSourceSetExceptionMethod.ToExceptionMessage(ToLangValue(arguments[0]));
        return ((TaskCompletionSourceLangValue)instance!).SetException(new Exception(message));
    }
}

/// <summary>TaskCompletionSource.TrySetCanceled()</summary>
public class TaskCompletionSourceTrySetCanceledMethod : CompletionSourceVmMethodBase
{
    public TaskCompletionSourceTrySetCanceledMethod() : base(typeof(bool)) { }

    public override string[] Names => ["TrySetCanceled"];
    public override Type TargetType => typeof(TaskCompletionSourceLangValue);
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var success = ((TaskCompletionSourceLangValue)instance).SetCanceled();
        return new BoolLangValue(success, position);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        return ((TaskCompletionSourceLangValue)instance!).SetCanceled();
    }
}

#endregion

#region CancellationTokenSource

/// <summary>CancellationTokenSource.Cancel()</summary>
public class CancellationTokenSourceCancelMethod : CompletionSourceVmMethodBase
{
    public CancellationTokenSourceCancelMethod() : base(typeof(void)) { }

    public override string[] Names => ["Cancel"];
    public override Type TargetType => typeof(CancellationTokenSourceLangValue);
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        ((CancellationTokenSourceLangValue)instance).Cancel();
        return new VoidLangValue(position);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        ((CancellationTokenSourceLangValue)instance!).Cancel();
        return VoidLangValue.Instance;
    }
}

/// <summary>CancellationTokenSource.CancelAfter(millisecondsDelay)</summary>
public class CancellationTokenSourceCancelAfterMethod : CompletionSourceVmMethodBase
{
    public CancellationTokenSourceCancelAfterMethod() : base(typeof(void)) { }

    public override string[] Names => ["CancelAfter"];
    public override Type TargetType => typeof(CancellationTokenSourceLangValue);
    public override string[]? ParameterNames => ["millisecondsDelay"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var delay = parameters[0].Run(manager);
        if (delay is not IntLangValue delayMs)
        {
            throw new TypeError(parameters[0], "int", delay.TypeToString());
        }

        ((CancellationTokenSourceLangValue)instance).CancelAfter(delayMs.Value);
        return new VoidLangValue(position);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (arguments[0] is not int delayMs)
        {
            throw new TypeError(new SourcePosition(), "int",
                Old8Lang.Runtime.VMReflectionHelper.DescribeValueType(arguments[0]));
        }

        ((CancellationTokenSourceLangValue)instance!).CancelAfter(delayMs);
        return VoidLangValue.Instance;
    }
}

/// <summary>CancellationTokenSource.Dispose()</summary>
public class CancellationTokenSourceDisposeMethod : CompletionSourceVmMethodBase
{
    public CancellationTokenSourceDisposeMethod() : base(typeof(void)) { }

    public override string[] Names => ["Dispose"];
    public override Type TargetType => typeof(CancellationTokenSourceLangValue);
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        ((CancellationTokenSourceLangValue)instance).Dispose();
        return new VoidLangValue(position);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        ((CancellationTokenSourceLangValue)instance!).Dispose();
        return VoidLangValue.Instance;
    }
}

#endregion
