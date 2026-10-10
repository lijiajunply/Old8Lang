using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Thread;

/// <summary>
/// Thread.Retry(retryCount, delayMs) - 实现线程重试机制
/// </summary>
public class ThreadRetryMethod : BaseInstanceMethod, IIlNativeValueInstanceMethod
{
    public override string[] Names => ["Retry", "retry"];
    public override Type TargetType => typeof(ThreadLangValue);
    public override string[]? ParameterNames => ["retryCount", "delayMs"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var thread = (ThreadLangValue)instance;
        var retryCountParam = parameters[0].Run(manager);

        if (retryCountParam is not IntLangValue retryCount)
        {
            throw new TypeError(position, "int", retryCountParam.TypeToString());
        }

        int delayMs = 0;
        if (parameters.Count > 1)
        {
            var delayParam = parameters[1].Run(manager);
            if (delayParam is not IntLangValue delay)
            {
                throw new TypeError(position, "int", delayParam.TypeToString());
            }
            delayMs = delay.Value;
        }

        return thread.Retry(retryCount.Value, delayMs);
    }

    /// <summary>
    /// IL 模式：接收者与重试参数都转成 Old8Lang 值后调用静态 helper（delayMs 可选）。
    /// </summary>
    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {

        IlValueBridge.EmitLoadWrapped(instance, ilGenerator, local);

        IlValueBridge.EmitLoadWrapped(parameters[0], ilGenerator, local);

        var helperName = parameters.Count > 1 ? nameof(RetryHelperWithDelay) : nameof(RetryHelper);
        if (parameters.Count > 1)
        {
            IlValueBridge.EmitLoadWrapped(parameters[1], ilGenerator, local);
        }

        var helperMethod = typeof(ThreadRetryMethod).GetMethod(helperName,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    /// <summary>
    /// IL 模式的辅助方法：按次数重试线程（无延迟）。
    /// </summary>
    public static ThreadLangValue RetryHelper(LangValueType instance, LangValueType retryCount) =>
        RetryHelperWithDelay(instance, retryCount, new IntLangValue(0));

    /// <summary>
    /// IL 模式的辅助方法：按次数与延迟重试线程。
    /// </summary>
    public static ThreadLangValue RetryHelperWithDelay(LangValueType instance, LangValueType retryCount,
        LangValueType delayMs)
    {
        if (instance is not ThreadLangValue thread)
        {
            throw new ArgumentException($"实例必须是线程，实际是 {instance.GetType().Name}");
        }

        return thread.Retry((int)IlValueBridge.ToInt64(retryCount), (int)IlValueBridge.ToInt64(delayMs));
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(ThreadLangValue);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        // 见 Todo.md「VM 线程模型」：VM 的线程是 VMThreadLangValue，与本方法的 TargetType 不同构。
        throw new NotSupportedException(
            "Thread.Retry 在 VM 模式下不可用：VM 的线程是 VMThreadLangValue（线程 id + ResourceManager），没有重试原语");
    }
}
