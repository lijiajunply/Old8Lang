namespace Old8Lang.Concurrency;

/// <summary>
/// 资源包装类，用于跟踪资源的最后访问时间
/// </summary>
/// <typeparam name="T">资源类型</typeparam>
public class ResourceWrapper<T>(T resource) where T : class
{
    private const int MaxIdleTimeMinutes = 30;
    private static readonly long MaxIdleTicks = TimeSpan.FromMinutes(MaxIdleTimeMinutes).Ticks;
    private const long AccessUpdateThrottleTicks = TimeSpan.TicksPerSecond;

    public T Resource { get; } = resource;
    public long LastAccessTimeTicks { get; private set; } = DateTime.UtcNow.Ticks;

    public void UpdateLastAccessTime()
    {
        // 清理窗口是分钟级，这里做秒级节流可显著降低热路径中的时间读取和字段写入开销。
        var nowTicks = DateTime.UtcNow.Ticks;
        if (nowTicks - LastAccessTimeTicks < AccessUpdateThrottleTicks)
        {
            return;
        }

        LastAccessTimeTicks = nowTicks;
    }

    public bool IsIdle => DateTime.UtcNow.Ticks - LastAccessTimeTicks > MaxIdleTicks;
}
