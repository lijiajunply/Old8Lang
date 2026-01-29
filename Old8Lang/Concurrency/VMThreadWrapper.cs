namespace Old8Lang.Concurrency;

/// <summary>
/// 虚拟机线程包装器 - 用于在虚拟机模式下管理线程
/// </summary>
public class VMThreadWrapper : IDisposable
{
    private readonly Thread _thread;
    private readonly Lock _lock = new();
    private object? _result;
    private Exception? _exception;
    private bool _isCompleted;
    private bool _disposed;
    private bool _isStarted;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="action">线程执行的动作</param>
    public VMThreadWrapper(Action action)
    {
        _thread = new Thread(() =>
        {
            try
            {
                action();
                lock (_lock)
                {
                    _isCompleted = true;
                }
            }
            catch (Exception ex)
            {
                lock (_lock)
                {
                    _exception = ex;
                    _isCompleted = true;
                }
            }
        });
    }

    /// <summary>
    /// 启动线程
    /// </summary>
    public void Start()
    {
        lock (_lock)
        {
            if (_isStarted)
            {
                // 线程已启动，忽略重复调用
                return;
            }
            _isStarted = true;
        }
        _thread.Start();
    }

    /// <summary>
    /// 等待线程完成并获取结果
    /// </summary>
    /// <returns>线程执行结果</returns>
    public object? Join()
    {
        _thread.Join();

        lock (_lock)
        {
            if (_exception is not null)
            {
                throw new Exception($"线程执行异常: {_exception.Message}", _exception);
            }

            return _result;
        }
    }

    /// <summary>
    /// 检查线程是否存活
    /// </summary>
    public bool IsAlive => _thread.IsAlive;

    /// <summary>
    /// 检查线程是否已完成
    /// </summary>
    public bool IsCompleted
    {
        get
        {
            lock (_lock)
            {
                return _isCompleted;
            }
        }
    }

    /// <summary>
    /// 设置线程执行结果
    /// </summary>
    /// <param name="result">执行结果</param>
    public void SetResult(object? result)
    {
        lock (_lock)
        {
            _result = result;
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        // 等待线程完成（如果还在运行）
        if (_thread.IsAlive)
        {
            // 给线程一些时间完成，但不要无限等待
            _thread.Join(TimeSpan.FromSeconds(5));
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
