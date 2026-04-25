namespace Old8Lang.Concurrency;

/// <summary>
/// 虚拟机线程包装器 - 用于在虚拟机模式下管理线程
/// </summary>
public class VMThreadWrapper : IDisposable
{
    private readonly Action _action;
    private readonly TaskCompletionSource<object?> _completionSource =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Thread _thread;
    private int _started;
    private int _disposed;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="action">线程执行的动作</param>
    public VMThreadWrapper(Action action)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
        _thread = new Thread(() =>
        {
            try
            {
                _action();
                _completionSource.TrySetResult(null);
            }
            catch (Exception ex)
            {
                _completionSource.TrySetException(ex);
            }
        });
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

        _thread.Start();
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
    public bool IsAlive => Volatile.Read(ref _started) != 0 && _thread.IsAlive;

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
}
