using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Channels;

namespace Old8LangLib;

/// <summary>
/// 异步和多线程函数库
/// 提供 Mutex、Semaphore、原子操作、Sleep 等功能
/// </summary>
public static class AsyncLib
{
    // 资源自动清理间隔（分钟）
    private const int AutoCleanupIntervalMinutes = 5;
    
    // 资源最大闲置时间（分钟），超过此时间未使用的资源将被自动清理
    private const int MaxIdleTimeMinutes = 30;
    
    // 资源包装类，用于跟踪资源的最后访问时间
    private class ResourceWrapper<T> where T : class
    {
        public T Resource { get; }
        public long LastAccessTimeTicks { get; private set; } = DateTime.Now.Ticks;
        
        public ResourceWrapper(T resource)
        {
            Resource = resource;
        }
        
        public void UpdateLastAccessTime()
        {
            LastAccessTimeTicks = DateTime.Now.Ticks;
        }
        
        public bool IsIdle => DateTime.Now.Ticks - LastAccessTimeTicks > TimeSpan.FromMinutes(MaxIdleTimeMinutes).Ticks;
    }
    
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
    
    // 存储 Mutex 对象（使用 SemaphoreSlim 实现）
    private static readonly ConcurrentDictionary<int, ResourceWrapper<SemaphoreSlim>> Mutexes = new();
    private static int _mutexIdCounter;

    // 存储 Semaphore 对象
    private static readonly ConcurrentDictionary<int, ResourceWrapper<SemaphoreSlim>> Semaphores = new();
    private static int _semaphoreIdCounter;

    // 存储原子整数对象
    private static readonly ConcurrentDictionary<int, ResourceWrapper<AtomicInt>> AtomicInts = new();
    private static int _atomicIntIdCounter;

    // 存储通道对象（使用 Channel<object> 实现，支持任意类型数据）
    private static readonly ConcurrentDictionary<int, ResourceWrapper<Channel<object>>> Channels = new();
    private static int _channelIdCounter;
    
    // 存储取消令牌源对象
    private static readonly ConcurrentDictionary<int, ResourceWrapper<CancellationTokenSource>> CancellationTokenSources = new();
    private static int _cancellationTokenSourceIdCounter;

    #region Mutex 互斥锁

    /// <summary>
    /// 创建一个 Mutex
    /// </summary>
    /// <returns>Mutex ID</returns>
    public static int MutexCreate()
    {
        var id = Interlocked.Increment(ref _mutexIdCounter);
        Mutexes[id] = new ResourceWrapper<SemaphoreSlim>(new SemaphoreSlim(1, 1)); // 信号量初始值和最大值都是 1
        return id;
    }

    /// <summary>
    /// 锁定 Mutex（阻塞等待）
    /// </summary>
    /// <param name="mutexId">Mutex ID</param>
    public static void MutexLock(int mutexId)
    {
        if (!Mutexes.TryGetValue(mutexId, out var wrapper))
        {
            throw new ArgumentException($"Mutex ID {mutexId} 不存在");
        }
        
        wrapper.UpdateLastAccessTime();
        wrapper.Resource.Wait();
    }

    /// <summary>
    /// 尝试锁定 Mutex（带超时）
    /// </summary>
    /// <param name="mutexId">Mutex ID</param>
    /// <param name="timeoutMs">超时时间（毫秒）</param>
    /// <returns>是否成功锁定</returns>
    public static bool MutexTryLock(int mutexId, int timeoutMs)
    {
        if (!Mutexes.TryGetValue(mutexId, out var wrapper))
        {
            throw new ArgumentException($"Mutex ID {mutexId} 不存在");
        }
        
        wrapper.UpdateLastAccessTime();
        return wrapper.Resource.Wait(timeoutMs);
    }

    /// <summary>
    /// 解锁 Mutex
    /// </summary>
    /// <param name="mutexId">Mutex ID</param>
    public static void MutexUnlock(int mutexId)
    {
        if (!Mutexes.TryGetValue(mutexId, out var wrapper))
        {
            throw new ArgumentException($"Mutex ID {mutexId} 不存在");
        }
        
        wrapper.UpdateLastAccessTime();
        wrapper.Resource.Release();
    }

    /// <summary>
    /// 销毁 Mutex
    /// </summary>
    /// <param name="mutexId">Mutex ID</param>
    public static void MutexDispose(int mutexId)
    {
        if (Mutexes.TryRemove(mutexId, out var wrapper))
        {
            wrapper.Resource.Dispose();
        }
    }

    #endregion

    #region Semaphore 信号量

    /// <summary>
    /// 创建一个 Semaphore
    /// </summary>
    /// <param name="initialCount">初始信号量计数</param>
    /// <param name="maxCount">最大信号量计数</param>
    /// <returns>Semaphore ID</returns>
    public static int SemaphoreCreate(int initialCount, int maxCount)
    {
        if (initialCount < 0 || maxCount < 1 || initialCount > maxCount)
        {
            throw new ArgumentException("信号量参数无效");
        }

        var id = Interlocked.Increment(ref _semaphoreIdCounter);
        Semaphores[id] = new ResourceWrapper<SemaphoreSlim>(new SemaphoreSlim(initialCount, maxCount));
        return id;
    }

    /// <summary>
    /// 获取信号量（阻塞等待）
    /// </summary>
    /// <param name="semaphoreId">Semaphore ID</param>
    public static void SemaphoreAcquire(int semaphoreId)
    {
        if (!Semaphores.TryGetValue(semaphoreId, out var wrapper))
        {
            throw new ArgumentException($"Semaphore ID {semaphoreId} 不存在");
        }
        
        wrapper.UpdateLastAccessTime();
        wrapper.Resource.Wait();
    }

    /// <summary>
    /// 尝试获取信号量（带超时）
    /// </summary>
    /// <param name="semaphoreId">Semaphore ID</param>
    /// <param name="timeoutMs">超时时间（毫秒）</param>
    /// <returns>是否成功获取</returns>
    public static bool SemaphoreTryAcquire(int semaphoreId, int timeoutMs)
    {
        if (!Semaphores.TryGetValue(semaphoreId, out var wrapper))
        {
            throw new ArgumentException($"Semaphore ID {semaphoreId} 不存在");
        }
        
        wrapper.UpdateLastAccessTime();
        return wrapper.Resource.Wait(timeoutMs);
    }

    /// <summary>
    /// 释放信号量
    /// </summary>
    /// <param name="semaphoreId">Semaphore ID</param>
    public static void SemaphoreRelease(int semaphoreId)
    {
        if (!Semaphores.TryGetValue(semaphoreId, out var wrapper))
        {
            throw new ArgumentException($"Semaphore ID {semaphoreId} 不存在");
        }
        
        wrapper.UpdateLastAccessTime();
        wrapper.Resource.Release();
    }

    /// <summary>
    /// 销毁 Semaphore
    /// </summary>
    /// <param name="semaphoreId">Semaphore ID</param>
    public static void SemaphoreDispose(int semaphoreId)
    {
        if (Semaphores.TryRemove(semaphoreId, out var wrapper))
        {
            wrapper.Resource.Dispose();
        }
    }

    #endregion

    #region 原子操作

    /// <summary>
    /// 原子整数类
    /// </summary>
    private class AtomicInt(int initialValue)
    {
        private int Value = initialValue;

        public int Get() => Interlocked.CompareExchange(ref Value, 0, 0);

        public void Set(int newValue) => Interlocked.Exchange(ref Value, newValue);

        public int Increment() => Interlocked.Increment(ref Value);

        public int Decrement() => Interlocked.Decrement(ref Value);

        public int Add(int delta) => Interlocked.Add(ref Value, delta);

        public bool CompareAndSet(int expectedValue, int newValue)
        {
            return Interlocked.CompareExchange(ref Value, newValue, expectedValue) == expectedValue;
        }
    }

    /// <summary>
    /// 创建原子整数
    /// </summary>
    /// <param name="initialValue">初始值</param>
    /// <returns>AtomicInt ID</returns>
    public static int AtomicIntCreate(int initialValue)
    {
        var id = Interlocked.Increment(ref _atomicIntIdCounter);
        AtomicInts[id] = new ResourceWrapper<AtomicInt>(new AtomicInt(initialValue));
        return id;
    }

    /// <summary>
    /// 获取原子整数的值
    /// </summary>
    /// <param name="atomicId">AtomicInt ID</param>
    /// <returns>当前值</returns>
    public static int AtomicIntGet(int atomicId)
    {
        if (!AtomicInts.TryGetValue(atomicId, out var wrapper))
        {
            throw new ArgumentException($"AtomicInt ID {atomicId} 不存在");
        }
        
        wrapper.UpdateLastAccessTime();
        return wrapper.Resource.Get();
    }

    /// <summary>
    /// 设置原子整数的值
    /// </summary>
    /// <param name="atomicId">AtomicInt ID</param>
    /// <param name="newValue">新值</param>
    public static void AtomicIntSet(int atomicId, int newValue)
    {
        if (!AtomicInts.TryGetValue(atomicId, out var wrapper))
        {
            throw new ArgumentException($"AtomicInt ID {atomicId} 不存在");
        }
        
        wrapper.UpdateLastAccessTime();
        wrapper.Resource.Set(newValue);
    }

    /// <summary>
    /// 原子自增
    /// </summary>
    /// <param name="atomicId">AtomicInt ID</param>
    /// <returns>自增后的值</returns>
    public static int AtomicIntIncrement(int atomicId)
    {
        if (!AtomicInts.TryGetValue(atomicId, out var wrapper))
        {
            throw new ArgumentException($"AtomicInt ID {atomicId} 不存在");
        }
        
        wrapper.UpdateLastAccessTime();
        return wrapper.Resource.Increment();
    }

    /// <summary>
    /// 原子自减
    /// </summary>
    /// <param name="atomicId">AtomicInt ID</param>
    /// <returns>自减后的值</returns>
    public static int AtomicIntDecrement(int atomicId)
    {
        if (!AtomicInts.TryGetValue(atomicId, out var wrapper))
        {
            throw new ArgumentException($"AtomicInt ID {atomicId} 不存在");
        }
        
        wrapper.UpdateLastAccessTime();
        return wrapper.Resource.Decrement();
    }

    /// <summary>
    /// 原子加法
    /// </summary>
    /// <param name="atomicId">AtomicInt ID</param>
    /// <param name="delta">增量</param>
    /// <returns>操作后的值</returns>
    public static int AtomicIntAdd(int atomicId, int delta)
    {
        if (!AtomicInts.TryGetValue(atomicId, out var wrapper))
        {
            throw new ArgumentException($"AtomicInt ID {atomicId} 不存在");
        }
        
        wrapper.UpdateLastAccessTime();
        return wrapper.Resource.Add(delta);
    }

    /// <summary>
    /// 原子比较并设置（CAS）
    /// </summary>
    /// <param name="atomicId">AtomicInt ID</param>
    /// <param name="expectedValue">期望值</param>
    /// <param name="newValue">新值</param>
    /// <returns>是否成功设置</returns>
    public static bool AtomicIntCompareAndSet(int atomicId, int expectedValue, int newValue)
    {
        if (!AtomicInts.TryGetValue(atomicId, out var wrapper))
        {
            throw new ArgumentException($"AtomicInt ID {atomicId} 不存在");
        }
        
        wrapper.UpdateLastAccessTime();
        return wrapper.Resource.CompareAndSet(expectedValue, newValue);
    }

    /// <summary>
    /// 销毁原子整数
    /// </summary>
    /// <param name="atomicId">AtomicInt ID</param>
    public static void AtomicIntDispose(int atomicId)
    {
        AtomicInts.TryRemove(atomicId, out _);
    }

    #endregion

    #region 工具函数

    /// <summary>
    /// 睡眠指定毫秒数
    /// </summary>
    /// <param name="milliseconds">毫秒数</param>
    public static void Sleep(int milliseconds)
    {
        if (milliseconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(milliseconds), "睡眠时间不能为负数");
        }

        Thread.Sleep(milliseconds);
    }

    /// <summary>
    /// 获取当前线程 ID
    /// </summary>
    /// <returns>线程 ID</returns>
    public static int GetCurrentThreadId()
    {
        return Environment.CurrentManagedThreadId;
    }

    /// <summary>
    /// 获取当前处理器数量
    /// </summary>
    /// <returns>处理器数量</returns>
    public static int GetProcessorCount()
    {
        return Environment.ProcessorCount;
    }

    #endregion

    #region 取消令牌

    /// <summary>
    /// 创建取消令牌源
    /// </summary>
    /// <returns>取消令牌源 ID</returns>
    public static int CreateCancellationTokenSource()
    {
        var id = Interlocked.Increment(ref _cancellationTokenSourceIdCounter);
        CancellationTokenSources[id] = new ResourceWrapper<CancellationTokenSource>(new CancellationTokenSource());
        return id;
    }

    /// <summary>
    /// 取消任务
    /// </summary>
    /// <param name="ctsId">取消令牌源 ID</param>
    public static void Cancel(int ctsId)
    {
        if (!CancellationTokenSources.TryGetValue(ctsId, out var wrapper))
        {
            throw new ArgumentException($"取消令牌源 ID {ctsId} 不存在");
        }
        
        wrapper.UpdateLastAccessTime();
        wrapper.Resource.Cancel();
    }

    /// <summary>
    /// 延迟取消任务
    /// </summary>
    /// <param name="ctsId">取消令牌源 ID</param>
    /// <param name="delayMs">延迟毫秒数</param>
    public static void CancelAfter(int ctsId, int delayMs)
    {
        if (!CancellationTokenSources.TryGetValue(ctsId, out var wrapper))
        {
            throw new ArgumentException($"取消令牌源 ID {ctsId} 不存在");
        }
        
        wrapper.UpdateLastAccessTime();
        wrapper.Resource.CancelAfter(delayMs);
    }

    /// <summary>
    /// 销毁取消令牌源
    /// </summary>
    /// <param name="ctsId">取消令牌源 ID</param>
    public static void DisposeCancellationTokenSource(int ctsId)
    {
        if (CancellationTokenSources.TryRemove(ctsId, out var wrapper))
        {
            wrapper.Resource.Dispose();
        }
    }

    #endregion

    #region 资源自动清理
    
    /// <summary>
    /// 定期清理闲置资源
    /// </summary>
    /// <param name="state">定时器状态</param>
    private static void CleanupResources(object? state)
    {
        // 清理闲置的 Mutex
        foreach (var (id, wrapper) in Mutexes)
        {
            if (wrapper.IsIdle && Mutexes.TryRemove(id, out var removedWrapper))
            {
                removedWrapper.Resource.Dispose();
            }
        }
        
        // 清理闲置的 Semaphore
        foreach (var (id, wrapper) in Semaphores)
        {
            if (wrapper.IsIdle && Semaphores.TryRemove(id, out var removedWrapper))
            {
                removedWrapper.Resource.Dispose();
            }
        }
        
        // 清理闲置的 AtomicInt
        foreach (var (id, wrapper) in AtomicInts)
        {
            if (wrapper.IsIdle)
            {
                AtomicInts.TryRemove(id, out _);
            }
        }
        
        // 清理闲置的取消令牌源
        foreach (var (id, wrapper) in CancellationTokenSources)
        {
            if (wrapper.IsIdle && CancellationTokenSources.TryRemove(id, out var removedWrapper))
            {
                removedWrapper.Resource.Dispose();
            }
        }
        
        // 清理闲置的 Channel
        foreach (var (id, wrapper) in Channels)
        {
            if (wrapper.IsIdle)
            {
                Channels.TryRemove(id, out _);
                // Channel 会自动释放资源，无需手动 Dispose
            }
        }
    }
    
    #endregion

    #region Channel 通道

    /// <summary>
    /// 创建一个无界通道
    /// </summary>
    /// <returns>Channel ID</returns>
    public static int ChannelCreate()
    {
        var id = Interlocked.Increment(ref _channelIdCounter);
        Channels[id] = new ResourceWrapper<Channel<object>>(Channel.CreateUnbounded<object>());
        return id;
    }

    /// <summary>
    /// 创建一个有界通道
    /// </summary>
    /// <param name="capacity">通道容量</param>
    /// <returns>Channel ID</returns>
    public static int ChannelCreateBounded(int capacity)
    {
        if (capacity < 1)
        {
            throw new ArgumentException("通道容量必须大于等于1");
        }

        var id = Interlocked.Increment(ref _channelIdCounter);
        Channels[id] = new ResourceWrapper<Channel<object>>(Channel.CreateBounded<object>(capacity));
        return id;
    }

    /// <summary>
    /// 向通道发送数据（阻塞等待）
    /// </summary>
    /// <param name="channelId">Channel ID</param>
    /// <param name="value">要发送的数据</param>
    public static void ChannelSend(int channelId, object value)
    {
        if (!Channels.TryGetValue(channelId, out var wrapper))
        {
            throw new ArgumentException($"Channel ID {channelId} 不存在");
        }
        
        wrapper.UpdateLastAccessTime();
        var channel = wrapper.Resource;

        channel.Writer.WriteAsync(value).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 向通道发送数据（带超时）
    /// </summary>
    /// <param name="channelId">Channel ID</param>
    /// <param name="value">要发送的数据</param>
    /// <param name="timeoutMs">超时时间（毫秒）</param>
    /// <returns>是否成功发送</returns>
    public static bool ChannelTrySend(int channelId, object value, int timeoutMs)
    {
        if (!Channels.TryGetValue(channelId, out var wrapper))
        {
            throw new ArgumentException($"Channel ID {channelId} 不存在");
        }
        
        wrapper.UpdateLastAccessTime();
        var channel = wrapper.Resource;

        var task = channel.Writer.WriteAsync(value).AsTask();
        return task.Wait(timeoutMs);
    }

    /// <summary>
    /// 从通道接收数据（阻塞等待）
    /// </summary>
    /// <param name="channelId">Channel ID</param>
    /// <returns>接收到的数据</returns>
    public static object ChannelReceive(int channelId)
    {
        if (!Channels.TryGetValue(channelId, out var wrapper))
        {
            throw new ArgumentException($"Channel ID {channelId} 不存在");
        }
        
        wrapper.UpdateLastAccessTime();
        var channel = wrapper.Resource;

        return channel.Reader.ReadAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// 从通道接收数据（带超时）
    /// </summary>
    /// <param name="channelId">Channel ID</param>
    /// <param name="timeoutMs">超时时间（毫秒）</param>
    /// <returns>接收到的数据，如果超时则返回 null</returns>
    public static object? ChannelTryReceive(int channelId, int timeoutMs)
    {
        if (!Channels.TryGetValue(channelId, out var wrapper))
        {
            throw new ArgumentException($"Channel ID {channelId} 不存在");
        }
        
        wrapper.UpdateLastAccessTime();
        var channel = wrapper.Resource;

        var task = channel.Reader.ReadAsync().AsTask();
        if (task.Wait(timeoutMs))
        {
            return task.GetAwaiter().GetResult();
        }


        return null;
    }

    /// <summary>
    /// 关闭通道写入端
    /// </summary>
    /// <param name="channelId">Channel ID</param>
    public static void ChannelClose(int channelId)
    {
        if (!Channels.TryGetValue(channelId, out var wrapper))
        {
            throw new ArgumentException($"Channel ID {channelId} 不存在");
        }
        
        wrapper.UpdateLastAccessTime();
        var channel = wrapper.Resource;

        channel.Writer.Complete();
    }

    /// <summary>
    /// 销毁通道
    /// </summary>
    /// <param name="channelId">Channel ID</param>
    public static void ChannelDispose(int channelId)
    {
        Channels.TryRemove(channelId, out _);
    }

    #endregion
    
    #region 资源管理
    
    /// <summary>
    /// 关闭并清理所有资源，包括停止定时器
    /// </summary>
    public static void Shutdown()
    {
        // 停止并释放定时器
        var timer = Interlocked.Exchange(ref _cleanupTimer, null);
        if (timer != null)
        {
            // 先停止定时器
            timer.Change(Timeout.Infinite, Timeout.Infinite);
            // 释放定时器资源
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
    }
    
    #endregion
}