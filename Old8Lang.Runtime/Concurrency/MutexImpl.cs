namespace Old8Lang.Concurrency;

/// <summary>
/// 轻量互斥锁实现，面向 VM 高频 Lock/Unlock 场景。
/// </summary>
public sealed class MutexImpl
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private int _ownerExecutionUnitId;

    public void Lock()
    {
        using var blockingScope = VMThreadPoolCompatibility.EnterBlockingRegion();
        _semaphore.Wait();
        Volatile.Write(ref _ownerExecutionUnitId, VMExecutionUnitContext.CurrentId);
    }

    public bool TryLock(int timeoutMs)
    {
        var currentExecutionUnitId = VMExecutionUnitContext.CurrentId;
        if (Volatile.Read(ref _ownerExecutionUnitId) == currentExecutionUnitId)
        {
            return false;
        }

        using var blockingScope = VMThreadPoolCompatibility.EnterBlockingRegion();
        if (!_semaphore.Wait(timeoutMs))
        {
            return false;
        }

        Volatile.Write(ref _ownerExecutionUnitId, currentExecutionUnitId);
        return true;
    }

    public void Unlock()
    {
        var currentExecutionUnitId = VMExecutionUnitContext.CurrentId;
        if (Volatile.Read(ref _ownerExecutionUnitId) != currentExecutionUnitId)
        {
            throw new SynchronizationLockException("当前执行单元未持有该 Mutex");
        }

        Volatile.Write(ref _ownerExecutionUnitId, 0);
        _semaphore.Release();
    }

    public void Dispose()
    {
        _semaphore.Dispose();
    }
}
