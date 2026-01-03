namespace Old8Lang.Concurrency;

/// <summary>
/// 循环栅栏实现 - 线程同步点,所有参与者都到达后才能继续
/// </summary>
public class CyclicBarrierImpl : IDisposable
{
    private readonly int ParticipantCount;
    private int WaitingCount;
    private readonly ManualResetEventSlim Event = new(false);
    private readonly Lock Lock = new();

    public CyclicBarrierImpl(int participantCount)
    {
        if (participantCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(participantCount), "参与者数量必须大于0");

        ParticipantCount = participantCount;
    }

    public void Await()
    {
        lock (Lock)
        {
            WaitingCount++;

            // 如果所有参与者都到达了
            if (WaitingCount >= ParticipantCount)
            {
                // 重置计数器并发出信号
                WaitingCount = 0;
                Event.Set();
                Event.Reset();
                return;
            }
        }

        // 等待其他参与者
        Event.Wait();
    }

    public bool Await(int timeoutMs)
    {
        lock (Lock)
        {
            WaitingCount++;

            // 如果所有参与者都到达了
            if (WaitingCount >= ParticipantCount)
            {
                // 重置计数器并发出信号
                WaitingCount = 0;
                Event.Set();
                Event.Reset();
                return true;
            }
        }

        // 等待其他参与者（带超时）
        bool success = Event.Wait(timeoutMs);

        // 如果超时，需要减少等待计数
        if (!success)
        {
            lock (Lock)
            {
                WaitingCount--;
            }
        }

        return success;
    }

    public int GetParticipantCount()
    {
        return ParticipantCount;
    }

    public int GetWaitingCount()
    {
        lock (Lock)
        {
            return WaitingCount;
        }
    }

    public void Dispose()
    {
        Event.Dispose();
    }
}
