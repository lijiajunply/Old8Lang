namespace Old8Lang.Concurrency;

/// <summary>
/// 资源包装类，用于跟踪资源的最后访问时间
/// </summary>
/// <typeparam name="T">资源类型</typeparam>
public class ResourceWrapper<T>(T resource) where T : class
{
    private const int MaxIdleTimeMinutes = 30;

    public T Resource { get; } = resource;
    public long LastAccessTimeTicks { get; private set; } = DateTime.Now.Ticks;

    public void UpdateLastAccessTime()
    {
        LastAccessTimeTicks = DateTime.Now.Ticks;
    }

    public bool IsIdle => DateTime.Now.Ticks - LastAccessTimeTicks > TimeSpan.FromMinutes(MaxIdleTimeMinutes).Ticks;
}
