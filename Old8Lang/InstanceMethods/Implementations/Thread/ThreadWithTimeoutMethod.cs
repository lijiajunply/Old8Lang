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
/// Thread.WithTimeout(timeoutMs) - 为线程添加超时限制
/// </summary>
public class ThreadWithTimeoutMethod : BaseInstanceMethod
{
    public override string[] Names => ["WithTimeout", "withTimeout"];
    public override Type TargetType => typeof(ThreadLangValue);
    public override string[]? ParameterNames => ["timeoutMs"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var thread = (ThreadLangValue)instance;
        var timeoutParam = parameters[0].Run(manager);

        if (timeoutParam is not IntLangValue timeout)
        {
            throw new TypeError(position, "int", timeoutParam.TypeToString());
        }

        return thread.WithTimeout(timeout.Value);
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        throw new NotSupportedException("Thread.WithTimeout 方法在编译器模式下暂不支持");
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(ThreadLangValue);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        throw new NotSupportedException("Thread.WithTimeout 方法在 VM 模式下暂不支持");
    }
}
