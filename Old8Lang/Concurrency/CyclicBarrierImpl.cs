namespace Old8Lang.Concurrency;

/// <summary>
/// 循环栅栏实现 - 线程同步点,所有参与者都到达后才能继续
/// 使用 .NET 内置的 Barrier 类实现
/// </summary>
public class CyclicBarrierImpl : IDisposable
{
    private readonly Barrier _barrier;
    private readonly int _participantCount;

    public CyclicBarrierImpl(int participantCount)
    {
        if (participantCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(participantCount), "参与者数量必须大于0");

        _participantCount = participantCount;
        _barrier = new Barrier(participantCount);
    }

    public void Await()
    {
        _barrier.SignalAndWait();
    }

    public bool Await(int timeoutMs)
    {
        try
        {
            return _barrier.SignalAndWait(timeoutMs);
        }
        catch (BarrierPostPhaseException)
        {
            // 如果在 post-phase 动作中发生异常，返回 false
            return false;
        }
    }

    public int GetParticipantCount()
    {
        return _participantCount;
    }

    public int GetWaitingCount()
    {
        // Barrier 的 ParticipantsRemaining 表示还未到达的参与者数量
        // 等待中的数量 = 总参与者数 - 剩余参与者数
        return _participantCount - _barrier.ParticipantsRemaining;
    }

    public void Dispose()
    {
        _barrier.Dispose();
    }
}
