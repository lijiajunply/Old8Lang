namespace Old8Lang.Concurrency;

/// <summary>
/// 虚拟机线程包装器 - 用于在虚拟机模式下管理线程
/// </summary>
public class VMThreadWrapper : IDisposable
{
    private const int BootstrapDedicatedThreadCount = 16;
    private const int StartupFallbackTimeoutMs = 2;
    private readonly Action _action;
    private readonly TaskCompletionSource<object?> _completionSource =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly bool _preferDedicatedThread;
    private int _started;
    private int _disposed;
    private int _status;
    private int _dedicatedFallbackStarted;
    private int _cancelRequested;

    private const int StatusCreated = 0;
    private const int StatusQueued = 1;
    private const int StatusRunning = 2;
    private const int StatusCompleted = 3;
    private static int _activeExecutionUnits;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="action">线程执行的动作</param>
    /// <param name="preferDedicatedThread">是否优先使用独立线程</param>
    public VMThreadWrapper(Action action, bool preferDedicatedThread = false)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
        _preferDedicatedThread = preferDedicatedThread;
    }

    /// <summary>
    /// 启动线程
    /// </summary>
    public void Start()
    {
        if (Interlocked.Exchange(ref _started, 1) != 0)
        {
            return;
        }

        // 取消请求在启动前到达：不执行动作，直接以「线程被取消」结束（协作式取消，与解释器语义一致）
        if (Volatile.Read(ref _cancelRequested) != 0)
        {
            Volatile.Write(ref _status, StatusCompleted);
            _completionSource.TrySetException(new OperationCanceledException("线程被取消"));
            return;
        }

        Volatile.Write(ref _status, StatusQueued);

        if (_preferDedicatedThread)
        {
            StartDedicatedThread();
            return;
        }

        VMThreadPoolCompatibility.PrepareForQueuedWork();
        _ = Task.Factory.StartNew(
            static state => ((VMThreadWrapper)state!).RunCore(),
            this,
            CancellationToken.None,
            TaskCreationOptions.DenyChildAttach | TaskCreationOptions.HideScheduler,
            TaskScheduler.Default);

        if (Volatile.Read(ref _activeExecutionUnits) < BootstrapDedicatedThreadCount)
        {
            StartDedicatedThread();
            return;
        }

        if (Volatile.Read(ref _status) == StatusQueued &&
            !SpinWait.SpinUntil(() => Volatile.Read(ref _status) != StatusQueued, StartupFallbackTimeoutMs))
        {
            StartDedicatedThread();
        }
    }

    /// <summary>
    /// 等待线程完成并获取结果
    /// </summary>
    /// <returns>线程执行结果</returns>
    public object? Join()
    {
        if (Volatile.Read(ref _started) == 0)
        {
            throw new InvalidOperationException("线程尚未启动");
        }

        return _completionSource.Task.GetAwaiter().GetResult();
    }

    /// <summary>
    /// 检查线程是否存活
    /// </summary>
    public bool IsAlive => Volatile.Read(ref _started) != 0 && Volatile.Read(ref _status) != StatusCompleted;

    /// <summary>
    /// 请求取消线程（协作式）。
    /// </summary>
    /// <remarks>
    /// 只对「尚未启动」的线程生效：取消后 <see cref="Start"/> 不会执行动作，
    /// 线程以 <see cref="OperationCanceledException"/>（消息「线程被取消」）结束，<see cref="Join"/> 会抛出它。
    /// 已经在运行的线程无法被打断——它与解释器的协作式取消语义一致
    /// （解释器里 `spawn` 出来的线程也没有可观察的取消令牌）。
    /// </remarks>
    public void RequestCancel() => Interlocked.Exchange(ref _cancelRequested, 1);

    /// <summary>
    /// 等待线程完成，返回是否在超时前完成。
    /// </summary>
    /// <param name="millisecondsTimeout">超时毫秒数</param>
    /// <returns>已完成返回 true；超时返回 false</returns>
    public bool WaitCompletion(int millisecondsTimeout)
    {
        if (Volatile.Read(ref _started) == 0)
        {
            throw new InvalidOperationException("线程尚未启动");
        }

        try
        {
            return _completionSource.Task.Wait(millisecondsTimeout);
        }
        catch (AggregateException)
        {
            // 已完成（异常状态）：由 Join 负责抛出原始异常
            return true;
        }
    }

    /// <summary>
    /// 检查线程是否已完成
    /// </summary>
    public bool IsCompleted
    {
        get => _completionSource.Task.IsCompleted;
    }

    /// <summary>
    /// 设置线程执行结果
    /// </summary>
    /// <param name="result">执行结果</param>
    public void SetResult(object? result)
    {
        _completionSource.TrySetResult(result);
    }

    public void SetException(Exception exception)
    {
        _completionSource.TrySetException(exception);
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        GC.SuppressFinalize(this);
    }

    private void RunCore()
    {
        if (Interlocked.CompareExchange(ref _status, StatusRunning, StatusQueued) != StatusQueued)
        {
            return;
        }

        Interlocked.Increment(ref _activeExecutionUnits);
        using var executionUnitScope = VMExecutionUnitContext.EnterNewScope();
        try
        {
            _action();
            _completionSource.TrySetResult(null);
        }
        catch (Exception ex)
        {
            _completionSource.TrySetException(ex);
        }
        finally
        {
            Interlocked.Decrement(ref _activeExecutionUnits);
            Volatile.Write(ref _status, StatusCompleted);
        }
    }

    private void StartDedicatedThread()
    {
        if (Interlocked.Exchange(ref _dedicatedFallbackStarted, 1) != 0)
        {
            return;
        }

        var thread = new Thread(RunCore)
        {
            IsBackground = true
        };
        thread.Start();
    }
}
