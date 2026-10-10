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
public class ThreadThenMethod : BaseInstanceMethod, IIlNativeValueInstanceMethod
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

    /// <summary>
    /// IL 模式：接收者转成 <see cref="ThreadLangValue"/>，continuation 转成函数值后调用静态 helper。
    /// </summary>
    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {

        IlValueBridge.EmitLoadWrapped(instance, ilGenerator, local);

        IlValueBridge.EmitLoadWrapped(parameters[0], ilGenerator, local);

        var helperMethod = typeof(ThreadThenMethod).GetMethod(nameof(ThenHelper),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    /// <summary>
    /// IL 模式的辅助方法：线程完成后执行 continuation。
    /// </summary>
    public static ThreadLangValue ThenHelper(LangValueType instance, LangValueType continuation)
    {
        if (instance is not ThreadLangValue thread)
        {
            throw new ArgumentException($"实例必须是线程，实际是 {instance.GetType().Name}");
        }

        if (continuation is not FuncLangValue func)
        {
            throw new ArgumentException("continuation 参数必须是函数类型");
        }

        return thread.Then(result =>
        {
            var next = func.Run(new VariateManager(), [result]);
            if (next is ThreadLangValue nextThread)
            {
                return nextThread;
            }

            throw new InvalidOperationException("Then 的 continuation 函数必须返回一个 Thread");
        });
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(ThreadLangValue);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        // 见 Todo.md「VM 线程模型」：VM 的线程是 VMThreadLangValue，与本方法的 TargetType 不同构。
        throw new NotSupportedException(
            "Thread.Then 在 VM 模式下不可用：VM 的线程是 VMThreadLangValue（线程 id + ResourceManager），与 ThreadLangValue 不同构");
    }
}
