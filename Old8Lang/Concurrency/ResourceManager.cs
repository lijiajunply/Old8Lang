using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Old8Lang.Concurrency;

/// <summary>
/// 资源管理器 - 统一管理所有并发资源
/// </summary>
public static class ResourceManager
{
    // 资源自动清理间隔（分钟）
    private const int AutoCleanupIntervalMinutes = 5;

    // 资源类型枚举
    private enum ResourceType
    {
        Mutex,
        Semaphore,
        AtomicInt,
        Channel,
        ReadWriteLock,
        CountDownLatch,
        CyclicBarrier,
        CancellationTokenSource
    }

    // 资源类型映射
    private static readonly ConcurrentDictionary<int, ResourceType> ResourceTypes = new();

    // 存储各类资源
    private static readonly ConcurrentDictionary<int, ResourceWrapper<SemaphoreSlim>> Mutexes = new();
    private static readonly ConcurrentDictionary<int, ResourceWrapper<SemaphoreSlim>> Semaphores = new();
    private static readonly ConcurrentDictionary<int, ResourceWrapper<AtomicIntImpl>> AtomicInts = new();
    private static readonly ConcurrentDictionary<int, ResourceWrapper<Channel<object>>> Channels = new();
    private static readonly ConcurrentDictionary<int, ResourceWrapper<ReaderWriterLockSlim>> ReadWriteLocks = new();
    private static readonly ConcurrentDictionary<int, ResourceWrapper<CountDownLatchImpl>> CountDownLatches = new();
    private static readonly ConcurrentDictionary<int, ResourceWrapper<CyclicBarrierImpl>> CyclicBarriers = new();
    private static readonly ConcurrentDictionary<int, ResourceWrapper<CancellationTokenSource>> CancellationTokenSources = new();

    // ID 计数器
    private static int _mutexIdCounter;
    private static int _semaphoreIdCounter;
    private static int _atomicIntIdCounter;
    private static int _channelIdCounter;
    private static int _readWriteLockIdCounter;
    private static int _countDownLatchIdCounter;
    private static int _cyclicBarrierIdCounter;
    private static int _cancellationTokenSourceIdCounter;

    // 定时器，用于定期清理不再使用的资源
    private static Timer? _cleanupTimer;

    // 定时器属性，延迟初始化
    private static Timer CleanupTimer
    {
        get
        {
            if (_cleanupTimer == null)
            {
                // 使用 Interlocked 确保线程安全的单例初始化
                Interlocked.CompareExchange(ref _cleanupTimer,
                    new Timer(CleanupResources, null, TimeSpan.FromMinutes(AutoCleanupIntervalMinutes), TimeSpan.FromMinutes(AutoCleanupIntervalMinutes)),
                    null);
            }
            return _cleanupTimer;
        }
    }

    #region Mutex

    public static int CreateMutex()
    {
        var id = Interlocked.Increment(ref _mutexIdCounter);
        Mutexes[id] = new ResourceWrapper<SemaphoreSlim>(new SemaphoreSlim(1, 1));
        ResourceTypes[id] = ResourceType.Mutex;
        return id;
    }

    public static void LockMutex(int mutexId)
    {
        if (!Mutexes.TryGetValue(mutexId, out var wrapper))
        {
            throw new ArgumentException($"Mutex ID {mutexId} 不存在");
        }

        wrapper.UpdateLastAccessTime();
        wrapper.Resource.Wait();
    }

    public static bool TryLockMutex(int mutexId, int timeoutMs)
    {
        if (!Mutexes.TryGetValue(mutexId, out var wrapper))
        {
            throw new ArgumentException($"Mutex ID {mutexId} 不存在");
        }

        wrapper.UpdateLastAccessTime();
        return wrapper.Resource.Wait(timeoutMs);
    }

    public static void UnlockMutex(int mutexId)
    {
        if (!Mutexes.TryGetValue(mutexId, out var wrapper))
        {
            throw new ArgumentException($"Mutex ID {mutexId} 不存在");
        }

        wrapper.UpdateLastAccessTime();
        wrapper.Resource.Release();
    }

    public static void DisposeMutex(int mutexId)
    {
        if (Mutexes.TryRemove(mutexId, out var wrapper))
        {
            wrapper.Resource.Dispose();
            ResourceTypes.TryRemove(mutexId, out _);
        }
    }

    #endregion

    #region Semaphore

    public static int CreateSemaphore(int initialCount, int maxCount)
    {
        if (initialCount < 0 || maxCount < 1 || initialCount > maxCount)
        {
            throw new ArgumentException("信号量参数无效");
        }

        var id = Interlocked.Increment(ref _semaphoreIdCounter);
        Semaphores[id] = new ResourceWrapper<SemaphoreSlim>(new SemaphoreSlim(initialCount, maxCount));
        ResourceTypes[id] = ResourceType.Semaphore;
        return id;
    }

    public static void AcquireSemaphore(int semaphoreId)
    {
        if (!Semaphores.TryGetValue(semaphoreId, out var wrapper))
        {
            throw new ArgumentException($"Semaphore ID {semaphoreId} 不存在");
        }

        wrapper.UpdateLastAccessTime();
        wrapper.Resource.Wait();
    }

    public static bool TryAcquireSemaphore(int semaphoreId, int timeoutMs)
    {
        if (!Semaphores.TryGetValue(semaphoreId, out var wrapper))
        {
            throw new ArgumentException($"Semaphore ID {semaphoreId} 不存在");
        }

        wrapper.UpdateLastAccessTime();
        return wrapper.Resource.Wait(timeoutMs);
    }

    public static void ReleaseSemaphore(int semaphoreId)
    {
        if (!Semaphores.TryGetValue(semaphoreId, out var wrapper))
        {
            throw new ArgumentException($"Semaphore ID {semaphoreId} 不存在");
        }

        wrapper.UpdateLastAccessTime();
        wrapper.Resource.Release();
    }

    public static void DisposeSemaphore(int semaphoreId)
    {
        if (Semaphores.TryRemove(semaphoreId, out var wrapper))
        {
            wrapper.Resource.Dispose();
            ResourceTypes.TryRemove(semaphoreId, out _);
        }
    }

    #endregion

    #region AtomicInt

    public static int CreateAtomicInt(int initialValue)
    {
        var id = Interlocked.Increment(ref _atomicIntIdCounter);
        AtomicInts[id] = new ResourceWrapper<AtomicIntImpl>(new AtomicIntImpl(initialValue));
        ResourceTypes[id] = ResourceType.AtomicInt;
        return id;
    }

    public static int GetAtomicInt(int atomicId)
    {
        if (!AtomicInts.TryGetValue(atomicId, out var wrapper))
        {
            throw new ArgumentException($"AtomicInt ID {atomicId} 不存在");
        }

        wrapper.UpdateLastAccessTime();
        return wrapper.Resource.Get();
    }

    public static void SetAtomicInt(int atomicId, int newValue)
    {
        if (!AtomicInts.TryGetValue(atomicId, out var wrapper))
        {
            throw new ArgumentException($"AtomicInt ID {atomicId} 不存在");
        }

        wrapper.UpdateLastAccessTime();
        wrapper.Resource.Set(newValue);
    }

    public static int IncrementAtomicInt(int atomicId)
    {
        if (!AtomicInts.TryGetValue(atomicId, out var wrapper))
        {
            throw new ArgumentException($"AtomicInt ID {atomicId} 不存在");
        }

        wrapper.UpdateLastAccessTime();
        return wrapper.Resource.Increment();
    }

    public static int DecrementAtomicInt(int atomicId)
    {
        if (!AtomicInts.TryGetValue(atomicId, out var wrapper))
        {
            throw new ArgumentException($"AtomicInt ID {atomicId} 不存在");
        }

        wrapper.UpdateLastAccessTime();
        return wrapper.Resource.Decrement();
    }

    public static int AddAtomicInt(int atomicId, int delta)
    {
        if (!AtomicInts.TryGetValue(atomicId, out var wrapper))
        {
            throw new ArgumentException($"AtomicInt ID {atomicId} 不存在");
        }

        wrapper.UpdateLastAccessTime();
        return wrapper.Resource.Add(delta);
    }

    public static bool CompareAndSetAtomicInt(int atomicId, int expectedValue, int newValue)
    {
        if (!AtomicInts.TryGetValue(atomicId, out var wrapper))
        {
            throw new ArgumentException($"AtomicInt ID {atomicId} 不存在");
        }

        wrapper.UpdateLastAccessTime();
        return wrapper.Resource.CompareAndSet(expectedValue, newValue);
    }

    public static void DisposeAtomicInt(int atomicId)
    {
        AtomicInts.TryRemove(atomicId, out _);
        ResourceTypes.TryRemove(atomicId, out _);
    }

    #endregion

    #region Channel

    public static int CreateChannel()
    {
        var id = Interlocked.Increment(ref _channelIdCounter);
        Channels[id] = new ResourceWrapper<Channel<object>>(Channel.CreateUnbounded<object>());
        ResourceTypes[id] = ResourceType.Channel;
        return id;
    }

    public static int CreateBoundedChannel(int capacity)
    {
        if (capacity < 1)
        {
            throw new ArgumentException("通道容量必须大于等于1");
        }

        var id = Interlocked.Increment(ref _channelIdCounter);
        Channels[id] = new ResourceWrapper<Channel<object>>(Channel.CreateBounded<object>(capacity));
        ResourceTypes[id] = ResourceType.Channel;
        return id;
    }

    public static void SendChannel(int channelId, object value)
    {
        if (!Channels.TryGetValue(channelId, out var wrapper))
        {
            throw new ArgumentException($"Channel ID {channelId} 不存在");
        }

        wrapper.UpdateLastAccessTime();
        wrapper.Resource.Writer.WriteAsync(value).GetAwaiter().GetResult();
    }

    public static bool TrySendChannel(int channelId, object value, int timeoutMs)
    {
        if (!Channels.TryGetValue(channelId, out var wrapper))
        {
            throw new ArgumentException($"Channel ID {channelId} 不存在");
        }

        wrapper.UpdateLastAccessTime();
        var task = wrapper.Resource.Writer.WriteAsync(value).AsTask();
        return task.Wait(timeoutMs);
    }

    public static object ReceiveChannel(int channelId)
    {
        if (!Channels.TryGetValue(channelId, out var wrapper))
        {
            throw new ArgumentException($"Channel ID {channelId} 不存在");
        }

        wrapper.UpdateLastAccessTime();
        return wrapper.Resource.Reader.ReadAsync().GetAwaiter().GetResult();
    }

    public static object? TryReceiveChannel(int channelId, int timeoutMs)
    {
        if (!Channels.TryGetValue(channelId, out var wrapper))
        {
            throw new ArgumentException($"Channel ID {channelId} 不存在");
        }

        wrapper.UpdateLastAccessTime();
        var task = wrapper.Resource.Reader.ReadAsync().AsTask();
        if (task.Wait(timeoutMs))
        {
            return task.GetAwaiter().GetResult();
        }

        return null;
    }

    public static void CloseChannel(int channelId)
    {
        if (!Channels.TryGetValue(channelId, out var wrapper))
        {
            throw new ArgumentException($"Channel ID {channelId} 不存在");
        }

        wrapper.UpdateLastAccessTime();
        wrapper.Resource.Writer.Complete();
    }

    public static void DisposeChannel(int channelId)
    {
        Channels.TryRemove(channelId, out _);
        ResourceTypes.TryRemove(channelId, out _);
    }

    #endregion

    #region ReadWriteLock

    public static int CreateReadWriteLock()
    {
        var id = Interlocked.Increment(ref _readWriteLockIdCounter);
        ReadWriteLocks[id] = new ResourceWrapper<ReaderWriterLockSlim>(new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion));
        ResourceTypes[id] = ResourceType.ReadWriteLock;
        return id;
    }

    public static void AcquireReadLock(int lockId)
    {
        if (!ReadWriteLocks.TryGetValue(lockId, out var wrapper))
        {
            throw new InvalidOperationException($"读写锁 {lockId} 不存在");
        }

        wrapper.Resource.EnterReadLock();
        wrapper.UpdateLastAccessTime();
    }

    public static void ReleaseReadLock(int lockId)
    {
        if (!ReadWriteLocks.TryGetValue(lockId, out var wrapper))
        {
            throw new InvalidOperationException($"读写锁 {lockId} 不存在");
        }

        wrapper.Resource.ExitReadLock();
    }

    public static void AcquireWriteLock(int lockId)
    {
        if (!ReadWriteLocks.TryGetValue(lockId, out var wrapper))
        {
            throw new InvalidOperationException($"读写锁 {lockId} 不存在");
        }

        wrapper.Resource.EnterWriteLock();
        wrapper.UpdateLastAccessTime();
    }

    public static void ReleaseWriteLock(int lockId)
    {
        if (!ReadWriteLocks.TryGetValue(lockId, out var wrapper))
        {
            throw new InvalidOperationException($"读写锁 {lockId} 不存在");
        }

        wrapper.Resource.ExitWriteLock();
    }

    public static bool TryAcquireReadLock(int lockId, int timeoutMs)
    {
        if (!ReadWriteLocks.TryGetValue(lockId, out var wrapper))
        {
            throw new InvalidOperationException($"读写锁 {lockId} 不存在");
        }

        bool acquired = wrapper.Resource.TryEnterReadLock(timeoutMs);
        if (acquired)
        {
            wrapper.UpdateLastAccessTime();
        }
        return acquired;
    }

    public static bool TryAcquireWriteLock(int lockId, int timeoutMs)
    {
        if (!ReadWriteLocks.TryGetValue(lockId, out var wrapper))
        {
            throw new InvalidOperationException($"读写锁 {lockId} 不存在");
        }

        bool acquired = wrapper.Resource.TryEnterWriteLock(timeoutMs);
        if (acquired)
        {
            wrapper.UpdateLastAccessTime();
        }
        return acquired;
    }

    public static void DisposeReadWriteLock(int lockId)
    {
        if (ReadWriteLocks.TryRemove(lockId, out var wrapper))
        {
            wrapper.Resource.Dispose();
            ResourceTypes.TryRemove(lockId, out _);
        }
    }

    #endregion

    #region CountDownLatch

    public static int CreateCountDownLatch(int count)
    {
        var id = Interlocked.Increment(ref _countDownLatchIdCounter);
        CountDownLatches[id] = new ResourceWrapper<CountDownLatchImpl>(new CountDownLatchImpl(count));
        ResourceTypes[id] = ResourceType.CountDownLatch;
        return id;
    }

    public static void CountDown(int latchId)
    {
        if (!CountDownLatches.TryGetValue(latchId, out var wrapper))
        {
            throw new InvalidOperationException($"倒计时锁 {latchId} 不存在");
        }

        wrapper.Resource.CountDown();
        wrapper.UpdateLastAccessTime();
    }

    public static void WaitCountDownLatch(int latchId)
    {
        if (!CountDownLatches.TryGetValue(latchId, out var wrapper))
        {
            throw new InvalidOperationException($"倒计时锁 {latchId} 不存在");
        }

        wrapper.Resource.Wait();
        wrapper.UpdateLastAccessTime();
    }

    public static bool WaitCountDownLatchTimeout(int latchId, int timeoutMs)
    {
        if (!CountDownLatches.TryGetValue(latchId, out var wrapper))
        {
            throw new InvalidOperationException($"倒计时锁 {latchId} 不存在");
        }

        bool success = wrapper.Resource.Wait(timeoutMs);
        wrapper.UpdateLastAccessTime();
        return success;
    }

    public static int GetCountDownLatchCount(int latchId)
    {
        if (!CountDownLatches.TryGetValue(latchId, out var wrapper))
        {
            throw new InvalidOperationException($"倒计时锁 {latchId} 不存在");
        }

        wrapper.UpdateLastAccessTime();
        return wrapper.Resource.GetCount();
    }

    public static void DisposeCountDownLatch(int latchId)
    {
        if (CountDownLatches.TryRemove(latchId, out var wrapper))
        {
            wrapper.Resource.Dispose();
            ResourceTypes.TryRemove(latchId, out _);
        }
    }

    #endregion

    #region CyclicBarrier

    public static int CreateCyclicBarrier(int participantCount)
    {
        var id = Interlocked.Increment(ref _cyclicBarrierIdCounter);
        CyclicBarriers[id] = new ResourceWrapper<CyclicBarrierImpl>(new CyclicBarrierImpl(participantCount));
        ResourceTypes[id] = ResourceType.CyclicBarrier;
        return id;
    }

    public static void AwaitCyclicBarrier(int barrierId)
    {
        if (!CyclicBarriers.TryGetValue(barrierId, out var wrapper))
        {
            throw new InvalidOperationException($"循环栅栏 {barrierId} 不存在");
        }

        wrapper.Resource.Await();
        wrapper.UpdateLastAccessTime();
    }

    public static bool AwaitCyclicBarrierTimeout(int barrierId, int timeoutMs)
    {
        if (!CyclicBarriers.TryGetValue(barrierId, out var wrapper))
        {
            throw new InvalidOperationException($"循环栅栏 {barrierId} 不存在");
        }

        bool success = wrapper.Resource.Await(timeoutMs);
        wrapper.UpdateLastAccessTime();
        return success;
    }

    public static int GetCyclicBarrierParticipantCount(int barrierId)
    {
        if (!CyclicBarriers.TryGetValue(barrierId, out var wrapper))
        {
            throw new InvalidOperationException($"循环栅栏 {barrierId} 不存在");
        }

        wrapper.UpdateLastAccessTime();
        return wrapper.Resource.GetParticipantCount();
    }

    public static int GetCyclicBarrierWaitingCount(int barrierId)
    {
        if (!CyclicBarriers.TryGetValue(barrierId, out var wrapper))
        {
            throw new InvalidOperationException($"循环栅栏 {barrierId} 不存在");
        }

        wrapper.UpdateLastAccessTime();
        return wrapper.Resource.GetWaitingCount();
    }

    public static void DisposeCyclicBarrier(int barrierId)
    {
        if (CyclicBarriers.TryRemove(barrierId, out var wrapper))
        {
            wrapper.Resource.Dispose();
            ResourceTypes.TryRemove(barrierId, out _);
        }
    }

    #endregion

    #region CancellationTokenSource

    public static int CreateCancellationTokenSource()
    {
        var id = Interlocked.Increment(ref _cancellationTokenSourceIdCounter);
        CancellationTokenSources[id] = new ResourceWrapper<CancellationTokenSource>(new CancellationTokenSource());
        ResourceTypes[id] = ResourceType.CancellationTokenSource;
        return id;
    }

    public static void Cancel(int ctsId)
    {
        if (!CancellationTokenSources.TryGetValue(ctsId, out var wrapper))
        {
            throw new ArgumentException($"取消令牌源 ID {ctsId} 不存在");
        }

        wrapper.UpdateLastAccessTime();
        wrapper.Resource.Cancel();
    }

    public static void CancelAfter(int ctsId, int delayMs)
    {
        if (!CancellationTokenSources.TryGetValue(ctsId, out var wrapper))
        {
            throw new ArgumentException($"取消令牌源 ID {ctsId} 不存在");
        }

        wrapper.UpdateLastAccessTime();
        wrapper.Resource.CancelAfter(delayMs);
    }

    public static void DisposeCancellationTokenSource(int ctsId)
    {
        if (CancellationTokenSources.TryRemove(ctsId, out var wrapper))
        {
            wrapper.Resource.Dispose();
            ResourceTypes.TryRemove(ctsId, out _);
        }
    }

    #endregion

    #region 统一Dispose接口（用于using语句）

    /// <summary>
    /// 尝试释放指定ID的资源（using语句支持）
    /// </summary>
    public static void TryDispose(int id)
    {
        if (ResourceTypes.TryGetValue(id, out var type))
        {
            switch (type)
            {
                case ResourceType.Mutex:
                    DisposeMutex(id);
                    break;
                case ResourceType.Semaphore:
                    DisposeSemaphore(id);
                    break;
                case ResourceType.AtomicInt:
                    DisposeAtomicInt(id);
                    break;
                case ResourceType.Channel:
                    DisposeChannel(id);
                    break;
                case ResourceType.ReadWriteLock:
                    DisposeReadWriteLock(id);
                    break;
                case ResourceType.CountDownLatch:
                    DisposeCountDownLatch(id);
                    break;
                case ResourceType.CyclicBarrier:
                    DisposeCyclicBarrier(id);
                    break;
                case ResourceType.CancellationTokenSource:
                    DisposeCancellationTokenSource(id);
                    break;
            }
        }
    }

    #endregion

    #region 资源自动清理

    private static void CleanupResources(object? state)
    {
        // 清理闲置的 Mutex
        foreach (var (id, wrapper) in Mutexes)
        {
            if (wrapper.IsIdle && Mutexes.TryRemove(id, out var removedWrapper))
            {
                removedWrapper.Resource.Dispose();
                ResourceTypes.TryRemove(id, out _);
            }
        }

        // 清理闲置的 Semaphore
        foreach (var (id, wrapper) in Semaphores)
        {
            if (wrapper.IsIdle && Semaphores.TryRemove(id, out var removedWrapper))
            {
                removedWrapper.Resource.Dispose();
                ResourceTypes.TryRemove(id, out _);
            }
        }

        // 清理闲置的 AtomicInt
        foreach (var (id, wrapper) in AtomicInts)
        {
            if (wrapper.IsIdle)
            {
                AtomicInts.TryRemove(id, out _);
                ResourceTypes.TryRemove(id, out _);
            }
        }

        // 清理闲置的取消令牌源
        foreach (var (id, wrapper) in CancellationTokenSources)
        {
            if (wrapper.IsIdle && CancellationTokenSources.TryRemove(id, out var removedWrapper))
            {
                removedWrapper.Resource.Dispose();
                ResourceTypes.TryRemove(id, out _);
            }
        }

        // 清理闲置的 Channel
        foreach (var (id, wrapper) in Channels)
        {
            if (wrapper.IsIdle)
            {
                Channels.TryRemove(id, out _);
                ResourceTypes.TryRemove(id, out _);
            }
        }

        // 清理闲置的 ReadWriteLock
        foreach (var (id, wrapper) in ReadWriteLocks)
        {
            if (wrapper.IsIdle && ReadWriteLocks.TryRemove(id, out var removedWrapper))
            {
                removedWrapper.Resource.Dispose();
                ResourceTypes.TryRemove(id, out _);
            }
        }

        // 清理闲置的 CountDownLatch
        foreach (var (id, wrapper) in CountDownLatches)
        {
            if (wrapper.IsIdle && CountDownLatches.TryRemove(id, out var removedWrapper))
            {
                removedWrapper.Resource.Dispose();
                ResourceTypes.TryRemove(id, out _);
            }
        }

        // 清理闲置的 CyclicBarrier
        foreach (var (id, wrapper) in CyclicBarriers)
        {
            if (wrapper.IsIdle && CyclicBarriers.TryRemove(id, out var removedWrapper))
            {
                removedWrapper.Resource.Dispose();
                ResourceTypes.TryRemove(id, out _);
            }
        }
    }

    /// <summary>
    /// 关闭并清理所有资源，包括停止定时器
    /// </summary>
    public static void Shutdown()
    {
        // 停止并释放定时器
        var timer = Interlocked.Exchange(ref _cleanupTimer, null);
        if (timer != null)
        {
            timer.Change(Timeout.Infinite, Timeout.Infinite);
            timer.Dispose();
        }

        // 清理所有 Mutex
        foreach (var (id, wrapper) in Mutexes)
        {
            if (Mutexes.TryRemove(id, out var removedWrapper))
            {
                removedWrapper.Resource.Dispose();
            }
        }

        // 清理所有 Semaphore
        foreach (var (id, wrapper) in Semaphores)
        {
            if (Semaphores.TryRemove(id, out var removedWrapper))
            {
                removedWrapper.Resource.Dispose();
            }
        }

        // 清理所有 AtomicInt
        AtomicInts.Clear();

        // 清理所有取消令牌源
        foreach (var (id, wrapper) in CancellationTokenSources)
        {
            if (CancellationTokenSources.TryRemove(id, out var removedWrapper))
            {
                removedWrapper.Resource.Dispose();
            }
        }

        // 清理所有 Channel
        Channels.Clear();

        // 清理所有 ReadWriteLock
        foreach (var (id, wrapper) in ReadWriteLocks)
        {
            if (ReadWriteLocks.TryRemove(id, out var removedWrapper))
            {
                removedWrapper.Resource.Dispose();
            }
        }

        // 清理所有 CountDownLatch
        foreach (var (id, wrapper) in CountDownLatches)
        {
            if (CountDownLatches.TryRemove(id, out var removedWrapper))
            {
                removedWrapper.Resource.Dispose();
            }
        }

        // 清理所有 CyclicBarrier
        foreach (var (id, wrapper) in CyclicBarriers)
        {
            if (CyclicBarriers.TryRemove(id, out var removedWrapper))
            {
                removedWrapper.Resource.Dispose();
            }
        }

        // 清理资源类型映射
        ResourceTypes.Clear();
    }

    #endregion
}
