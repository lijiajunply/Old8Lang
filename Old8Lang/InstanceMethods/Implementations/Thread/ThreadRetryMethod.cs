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
public class ThreadRetryMethod : BaseInstanceMethod
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

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        throw new NotSupportedException("Thread.Retry 方法在编译器模式下暂不支持");
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(ThreadLangValue);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        throw new NotSupportedException("Thread.Retry 方法在 VM 模式下暂不支持");
    }
}
