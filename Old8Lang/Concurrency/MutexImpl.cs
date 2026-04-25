namespace Old8Lang.Concurrency;

/// <summary>
/// 轻量互斥锁实现，面向 VM 高频 Lock/Unlock 场景。
/// </summary>
public sealed class MutexImpl
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public void Lock()
    {
        _semaphore.Wait();
    }

    public bool TryLock(int timeoutMs)
    {
        return _semaphore.Wait(timeoutMs);
    }

    public void Unlock()
    {
        _semaphore.Release();
    }

    public void Dispose()
    {
        _semaphore.Dispose();
    }
}
