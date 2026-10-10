using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Bytecode.Closures;
using Old8Lang.Bytecode.Core;
using Old8Lang.Bytecode.Metadata;
using Old8Lang.Concurrency;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.Thread;

// 虚拟机模式下的线程组合方法。
//
// 解释器 / IL 模式用的是 ThreadLangValue（原生 Thread + CancellationTokenSource），
// 而虚拟机的线程对象是 VMThreadLangValue（整数线程 id + Concurrency.ResourceManager），
// 两者不同构（见 Docs/MODE_SUPPORT.md「已知限制 · 虚拟机模式」）。
// 因此这里为 VMThreadLangValue 单独注册同名实例方法，语义与解释器的 ThreadLangValue 对齐：
//   - Then(callback)        线程结束后用结果调用 callback（callback 必须返回一个线程），等待并取回其结果
//   - WithTimeout(ms)       等待原线程，超时抛 TimeoutException
//   - Retry(n, delayMs)     重新执行 spawn 时登记的函数（协作式：只能重试 spawn 创建的线程）
//   - Cancel()              请求取消（只对尚未启动的线程生效，与解释器一致）
//
// 这些方法在解释器 / IL 模式下不会被分发（TargetType 是 VM 专属类型），
// 对应的解释器实现见 ThreadThenMethod / ThreadWithTimeoutMethod / ThreadRetryMethod / ThreadCancelMethod。

/// <summary>
/// VM 模式 Thread.Then(callback)：线程完成后执行 callback。
/// </summary>
public class VmThreadThenMethod : BaseInstanceMethod
{
    public override string[] Names => ["Then", "then"];
    public override Type TargetType => typeof(VMThreadLangValue);
    public override string[]? ParameterNames => ["continuation"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        throw new NotSupportedException("Thread.Then 在解释器 / IL 模式下由 ThreadThenMethod 处理");
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        throw new NotSupportedException("Thread.Then 在解释器 / IL 模式下由 ThreadThenMethod 处理");
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters,
        LocalManager local) => typeof(VMThreadLangValue);

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is not VMThreadLangValue thread)
        {
            throw new ArgumentException($"实例必须是 VMThreadLangValue，实际是 {instance?.GetType().Name}");
        }

        if (arguments.Length < 1 || arguments[0] is not (ClosureValue or FunctionMetadata))
        {
            throw new ArgumentException("continuation 参数必须是函数类型");
        }

        var continuation = arguments[0];
        var vm = VMContext.CurrentVM ?? throw new InvalidOperationException("VM 上下文未初始化");

        var threadIdHolder = new int[1];
        var nextThreadId = ResourceManager.CreateThread(() =>
        {
            try
            {
                var result = thread.Join();
                var next = vm.ExecuteFunctionObjectInWorker(continuation, [result]);

                if (next is not VMThreadLangValue nextThread)
                {
                    throw new InvalidOperationException("Then 的 continuation 函数必须返回一个 Thread");
                }

                nextThread.Start();
                ResourceManager.SetThreadResult(threadIdHolder[0], nextThread.Join());
            }
            catch (Exception ex)
            {
                ResourceManager.SetThreadException(threadIdHolder[0], ex);
            }
        });

        threadIdHolder[0] = nextThreadId;
        return new VMThreadLangValue(nextThreadId);
    }
}

/// <summary>
/// VM 模式 Thread.WithTimeout(timeoutMs)：给线程加超时。
/// </summary>
public class VmThreadWithTimeoutMethod : BaseInstanceMethod
{
    public override string[] Names => ["WithTimeout", "withTimeout"];
    public override Type TargetType => typeof(VMThreadLangValue);
    public override string[]? ParameterNames => ["timeoutMs"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        throw new NotSupportedException("Thread.WithTimeout 在解释器 / IL 模式下由 ThreadWithTimeoutMethod 处理");
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        throw new NotSupportedException("Thread.WithTimeout 在解释器 / IL 模式下由 ThreadWithTimeoutMethod 处理");
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters,
        LocalManager local) => typeof(VMThreadLangValue);

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is not VMThreadLangValue thread)
        {
            throw new ArgumentException($"实例必须是 VMThreadLangValue，实际是 {instance?.GetType().Name}");
        }

        if (arguments.Length < 1)
        {
            throw new ArgumentException("WithTimeout 方法需要一个超时参数");
        }

        var timeoutMs = Convert.ToInt32(arguments[0]);
        var threadIdHolder = new int[1];

        var timeoutThreadId = ResourceManager.CreateThread(() =>
        {
            try
            {
                if (!ResourceManager.WaitThreadCompletion(thread.ThreadId, timeoutMs))
                {
                    throw new TimeoutException($"线程等待超时（{timeoutMs}ms）");
                }

                ResourceManager.SetThreadResult(threadIdHolder[0], thread.Join());
            }
            catch (Exception ex)
            {
                ResourceManager.SetThreadException(threadIdHolder[0], ex);
            }
        });

        threadIdHolder[0] = timeoutThreadId;
        return new VMThreadLangValue(timeoutThreadId);
    }
}

/// <summary>
/// VM 模式 Thread.Retry(retryCount, delayMs?)：重新执行 spawn 时登记的函数。
/// </summary>
public class VmThreadRetryMethod : BaseInstanceMethod
{
    public override string[] Names => ["Retry", "retry"];
    public override Type TargetType => typeof(VMThreadLangValue);
    public override string[]? ParameterNames => ["retryCount", "delayMs"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        throw new NotSupportedException("Thread.Retry 在解释器 / IL 模式下由 ThreadRetryMethod 处理");
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        throw new NotSupportedException("Thread.Retry 在解释器 / IL 模式下由 ThreadRetryMethod 处理");
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters,
        LocalManager local) => typeof(VMThreadLangValue);

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is not VMThreadLangValue thread)
        {
            throw new ArgumentException($"实例必须是 VMThreadLangValue，实际是 {instance?.GetType().Name}");
        }

        if (arguments.Length < 1)
        {
            throw new ArgumentException("Retry 方法需要一个重试次数参数");
        }

        var retryCount = Convert.ToInt32(arguments[0]);
        var delayMs = arguments.Length > 1 ? Convert.ToInt32(arguments[1]) : 0;

        if (!ResourceManager.TryGetThreadPayload(thread.ThreadId, out var funcObject, out var funcArguments))
        {
            throw new InvalidOperationException(
                "Retry 只能用于 spawn(...) 创建的线程（找不到可重新执行的函数）");
        }

        var vm = VMContext.CurrentVM ?? throw new InvalidOperationException("VM 上下文未初始化");
        var threadIdHolder = new int[1];

        var retryThreadId = ResourceManager.CreateThread(() =>
        {
            Exception? lastException = null;

            for (var attempt = 0; attempt <= retryCount; attempt++)
            {
                try
                {
                    var result = vm.ExecuteFunctionObjectInWorker(funcObject, funcArguments);
                    ResourceManager.SetThreadResult(threadIdHolder[0], result);
                    return;
                }
                catch (Exception ex)
                {
                    lastException = ex;

                    if (attempt < retryCount && delayMs > 0)
                    {
                        System.Threading.Thread.Sleep(delayMs);
                    }
                }
            }

            ResourceManager.SetThreadException(threadIdHolder[0],
                lastException ?? new Exception("线程执行失败，重试次数耗尽"));
        });

        threadIdHolder[0] = retryThreadId;
        return new VMThreadLangValue(retryThreadId);
    }
}

/// <summary>
/// VM 模式 Thread.Cancel()：请求取消线程（协作式）。
/// </summary>
public class VmThreadCancelMethod : BaseInstanceMethod
{
    public override string[] Names => ["Cancel", "cancel"];
    public override Type TargetType => typeof(VMThreadLangValue);
    public override string[]? ParameterNames => null;
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        throw new NotSupportedException("Thread.Cancel 在解释器 / IL 模式下由 ThreadCancelMethod 处理");
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        throw new NotSupportedException("Thread.Cancel 在解释器 / IL 模式下由 ThreadCancelMethod 处理");
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters,
        LocalManager local) => typeof(VoidLangValue);

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is not VMThreadLangValue thread)
        {
            throw new ArgumentException($"实例必须是 VMThreadLangValue，实际是 {instance?.GetType().Name}");
        }

        ResourceManager.CancelThread(thread.ThreadId);
        return new VoidLangValue();
    }
}
