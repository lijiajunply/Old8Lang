namespace Old8Lang.Concurrency;

/// <summary>
/// 轻量互斥锁实现，面向 VM 高频 Lock/Unlock 场景。
/// </summary>
public sealed class MutexImpl
{
    private readonly object _sync = new();

    public void Lock()
    {
        Monitor.Enter(_sync);
    }

    public bool TryLock(int timeoutMs)
    {
        return Monitor.TryEnter(_sync, timeoutMs);
    }

    public void Unlock()
    {
        Monitor.Exit(_sync);
    }

    public void Dispose()
    {
        // 无托管资源，保留接口形状以复用 ResourceManager 释放逻辑。
    }
}
