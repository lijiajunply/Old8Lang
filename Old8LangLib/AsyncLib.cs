using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Old8LangLib;

/// <summary>
/// 异步和多线程函数库
/// 提供 Mutex、Semaphore、原子操作、Sleep 等功能
/// </summary>
public static class AsyncLib
{
    // 存储 Mutex 对象（使用 SemaphoreSlim 实现）
    private static readonly ConcurrentDictionary<int, SemaphoreSlim> Mutexes = new();
    private static int _mutexIdCounter;

    // 存储 Semaphore 对象
    private static readonly ConcurrentDictionary<int, SemaphoreSlim> Semaphores = new();
    private static int _semaphoreIdCounter;

    // 存储原子整数对象
    private static readonly ConcurrentDictionary<int, AtomicInt> AtomicInts = new();
    private static int _atomicIntIdCounter;

    // 存储通道对象（使用 Channel<object> 实现，支持任意类型数据）
    private static readonly ConcurrentDictionary<int, Channel<object>> Channels = new();
    private static int _channelIdCounter;

    #region Mutex 互斥锁

    /// <summary>
    /// 创建一个 Mutex
    /// </summary>
    /// <returns>Mutex ID</returns>
    public static int MutexCreate()
    {
        var id = Interlocked.Increment(ref _mutexIdCounter);
        Mutexes[id] = new SemaphoreSlim(1, 1); // 信号量初始值和最大值都是 1
        return id;
    }

    /// <summary>
    /// 锁定 Mutex（阻塞等待）
    /// </summary>
    /// <param name="mutexId">Mutex ID</param>
    public static void MutexLock(int mutexId)
    {
        if (!Mutexes.TryGetValue(mutexId, out var mutex))
        {
            throw new ArgumentException($"Mutex ID {mutexId} 不存在");
        }

        mutex.Wait();
    }

    /// <summary>
    /// 尝试锁定 Mutex（带超时）
    /// </summary>
    /// <param name="mutexId">Mutex ID</param>
    /// <param name="timeoutMs">超时时间（毫秒）</param>
    /// <returns>是否成功锁定</returns>
    public static bool MutexTryLock(int mutexId, int timeoutMs)
    {
        if (!Mutexes.TryGetValue(mutexId, out var mutex))
        {
            throw new ArgumentException($"Mutex ID {mutexId} 不存在");
        }

        return mutex.Wait(timeoutMs);
    }

    /// <summary>
    /// 解锁 Mutex
    /// </summary>
    /// <param name="mutexId">Mutex ID</param>
    public static void MutexUnlock(int mutexId)
    {
        if (!Mutexes.TryGetValue(mutexId, out var mutex))
        {
            throw new ArgumentException($"Mutex ID {mutexId} 不存在");
        }

        mutex.Release();
    }

    /// <summary>
    /// 销毁 Mutex
    /// </summary>
    /// <param name="mutexId">Mutex ID</param>
    public static void MutexDispose(int mutexId)
    {
        if (Mutexes.TryRemove(mutexId, out var mutex))
        {
            mutex.Dispose();
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
        Semaphores[id] = new SemaphoreSlim(initialCount, maxCount);
        return id;
    }

    /// <summary>
    /// 获取信号量（阻塞等待）
    /// </summary>
    /// <param name="semaphoreId">Semaphore ID</param>
    public static void SemaphoreAcquire(int semaphoreId)
    {
        if (!Semaphores.TryGetValue(semaphoreId, out var semaphore))
        {
            throw new ArgumentException($"Semaphore ID {semaphoreId} 不存在");
        }

        semaphore.Wait();
    }

    /// <summary>
    /// 尝试获取信号量（带超时）
    /// </summary>
    /// <param name="semaphoreId">Semaphore ID</param>
    /// <param name="timeoutMs">超时时间（毫秒）</param>
    /// <returns>是否成功获取</returns>
    public static bool SemaphoreTryAcquire(int semaphoreId, int timeoutMs)
    {
        if (!Semaphores.TryGetValue(semaphoreId, out var semaphore))
        {
            throw new ArgumentException($"Semaphore ID {semaphoreId} 不存在");
        }

        return semaphore.Wait(timeoutMs);
    }

    /// <summary>
    /// 释放信号量
    /// </summary>
    /// <param name="semaphoreId">Semaphore ID</param>
    public static void SemaphoreRelease(int semaphoreId)
    {
        if (!Semaphores.TryGetValue(semaphoreId, out var semaphore))
        {
            throw new ArgumentException($"Semaphore ID {semaphoreId} 不存在");
        }

        semaphore.Release();
    }

    /// <summary>
    /// 销毁 Semaphore
    /// </summary>
    /// <param name="semaphoreId">Semaphore ID</param>
    public static void SemaphoreDispose(int semaphoreId)
    {
        if (Semaphores.TryRemove(semaphoreId, out var semaphore))
        {
            semaphore.Dispose();
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
        AtomicInts[id] = new AtomicInt(initialValue);
        return id;
    }

    /// <summary>
    /// 获取原子整数的值
    /// </summary>
    /// <param name="atomicId">AtomicInt ID</param>
    /// <returns>当前值</returns>
    public static int AtomicIntGet(int atomicId)
    {
        if (!AtomicInts.TryGetValue(atomicId, out var atomic))
        {
            throw new ArgumentException($"AtomicInt ID {atomicId} 不存在");
        }

        return atomic.Get();
    }

    /// <summary>
    /// 设置原子整数的值
    /// </summary>
    /// <param name="atomicId">AtomicInt ID</param>
    /// <param name="newValue">新值</param>
    public static void AtomicIntSet(int atomicId, int newValue)
    {
        if (!AtomicInts.TryGetValue(atomicId, out var atomic))
        {
            throw new ArgumentException($"AtomicInt ID {atomicId} 不存在");
        }

        atomic.Set(newValue);
    }

    /// <summary>
    /// 原子自增
    /// </summary>
    /// <param name="atomicId">AtomicInt ID</param>
    /// <returns>自增后的值</returns>
    public static int AtomicIntIncrement(int atomicId)
    {
        if (!AtomicInts.TryGetValue(atomicId, out var atomic))
        {
            throw new ArgumentException($"AtomicInt ID {atomicId} 不存在");
        }

        return atomic.Increment();
    }

    /// <summary>
    /// 原子自减
    /// </summary>
    /// <param name="atomicId">AtomicInt ID</param>
    /// <returns>自减后的值</returns>
    public static int AtomicIntDecrement(int atomicId)
    {
        if (!AtomicInts.TryGetValue(atomicId, out var atomic))
        {
            throw new ArgumentException($"AtomicInt ID {atomicId} 不存在");
        }

        return atomic.Decrement();
    }

    /// <summary>
    /// 原子加法
    /// </summary>
    /// <param name="atomicId">AtomicInt ID</param>
    /// <param name="delta">增量</param>
    /// <returns>操作后的值</returns>
    public static int AtomicIntAdd(int atomicId, int delta)
    {
        if (!AtomicInts.TryGetValue(atomicId, out var atomic))
        {
            throw new ArgumentException($"AtomicInt ID {atomicId} 不存在");
        }

        return atomic.Add(delta);
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
        if (!AtomicInts.TryGetValue(atomicId, out var atomic))
        {
            throw new ArgumentException($"AtomicInt ID {atomicId} 不存在");
        }

        return atomic.CompareAndSet(expectedValue, newValue);
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

    #region Channel 通道

    /// <summary>
    /// 创建一个无界通道
    /// </summary>
    /// <returns>Channel ID</returns>
    public static int ChannelCreate()
    {
        var id = Interlocked.Increment(ref _channelIdCounter);
        Channels[id] = Channel.CreateUnbounded<object>();
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
        Channels[id] = Channel.CreateBounded<object>(capacity);
        return id;
    }

    /// <summary>
    /// 向通道发送数据（阻塞等待）
    /// </summary>
    /// <param name="channelId">Channel ID</param>
    /// <param name="value">要发送的数据</param>
    public static void ChannelSend(int channelId, object value)
    {
        if (!Channels.TryGetValue(channelId, out var channel))
        {
            throw new ArgumentException($"Channel ID {channelId} 不存在");
        }

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
        if (!Channels.TryGetValue(channelId, out var channel))
        {
            throw new ArgumentException($"Channel ID {channelId} 不存在");
        }

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
        if (!Channels.TryGetValue(channelId, out var channel))
        {
            throw new ArgumentException($"Channel ID {channelId} 不存在");
        }

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
        if (!Channels.TryGetValue(channelId, out var channel))
        {
            throw new ArgumentException($"Channel ID {channelId} 不存在");
        }

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
        if (!Channels.TryGetValue(channelId, out var channel))
        {
            throw new ArgumentException($"Channel ID {channelId} 不存在");
        }

        channel.Writer.Complete();
    }

    /// <summary>
    /// 销毁通道
    /// </summary>
    /// <param name="channelId">Channel ID</param>
    public static void ChannelDispose(int channelId)
    {
        if (Channels.TryRemove(channelId, out var channel))
        {
            // Channel 会自动释放资源，无需手动 Dispose
        }
    }

    #endregion
}