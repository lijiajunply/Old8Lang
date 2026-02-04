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
/// Thread.Then(continuation) - 线程完成后执行下一个线程
/// </summary>
public class ThreadThenMethod : BaseInstanceMethod
{
    public override string[] Names => ["Then", "then"];
    public override Type TargetType => typeof(ThreadLangValue);
    public override string[]? ParameterNames => ["continuation"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var thread = (ThreadLangValue)instance;
        var continuationParam = parameters[0].Run(manager);

        if (continuationParam is not FuncLangValue continuation)
        {
            throw new TypeError(position, "FuncValue", continuationParam.GetType().Name);
        }

        if (thread.ExternalManager is null)
        {
            throw new InvalidOperationError(position, "Then 方法需要有效的执行上下文（ExternalManager）");
        }

        var capturedManager = thread.ExternalManager;

        return thread.Then(result =>
        {
            var closedFunc = continuation.Run(capturedManager);
            if (closedFunc is FuncLangValue closedFuncValue)
            {
                continuation = closedFuncValue;
            }

            var args = new List<LangExpression> { result };
            var nextThreadResult = continuation.Run(capturedManager, args);

            if (nextThreadResult is ThreadLangValue threadValue)
            {
                return threadValue;
            }

            throw new InvalidOperationError(position, "Then 的 continuation 函数必须返回一个 Thread");
        });
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        throw new NotSupportedException("Thread.Then 方法在编译器模式下暂不支持");
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(ThreadLangValue);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        throw new NotSupportedException("Thread.Then 方法在 VM 模式下暂不支持");
    }
}
