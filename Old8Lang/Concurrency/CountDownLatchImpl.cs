namespace Old8Lang.Concurrency;

/// <summary>
/// 倒计时锁实现
/// </summary>
public class CountDownLatchImpl : IDisposable
{
    private int Count;
    private readonly ManualResetEventSlim Event = new(false);

    public CountDownLatchImpl(int initialCount)
    {
        if (initialCount < 0)
            throw new ArgumentOutOfRangeException(nameof(initialCount), "计数不能为负数");

        Count = initialCount;

        // 如果初始计数为0，立即设置信号
        if (Count == 0)
        {
            Event.Set();
        }
    }

    public void CountDown()
    {
        // 使用 Interlocked 确保线程安全的递减
        int newCount = Interlocked.Decrement(ref Count);

        // 如果计数达到0，设置信号
        if (newCount == 0)
        {
            Event.Set();
        }
    }

    public void Wait()
    {
        Event.Wait();
    }

    public bool Wait(int timeoutMs)
    {
        return Event.Wait(timeoutMs);
    }

    public int GetCount()
    {
        // 返回当前计数，但不允许小于0
        int currentCount = Volatile.Read(ref Count);
        return Math.Max(0, currentCount);
    }

    public void Dispose()
    {
        Event.Dispose();
    }
}
