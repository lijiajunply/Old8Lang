using Old8Lang.Profiler;

namespace Old8Lang.App.Services;

/// <summary>
/// Profiler 服务
/// </summary>
public static class ProfilerService
{
    private static readonly ProfilerManager Profiler = new();

    /// <summary>
    /// 获取 Profiler 实例
    /// </summary>
    /// <returns>Profiler 实例</returns>
    public static ProfilerManager GetProfiler()
    {
        return Profiler;
    }

    /// <summary>
    /// 清除 Profiler 实例
    /// </summary>
    public static void ClearProfiler()
    {
        Profiler.ClearSession();
    }
}