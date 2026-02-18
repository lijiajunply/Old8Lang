namespace Old8Lang.Interpreter;

/// <summary>
/// 性能监控配置
/// </summary>
public class PerformanceMonitorConfig
{
    /// <summary>
    /// 是否启用监控
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// 是否启用详细监控
    /// </summary>
    public bool DetailedMonitoring { get; set; }

    /// <summary>
    /// 采样率（0-1），1.0 表示 100% 采样
    /// </summary>
    public double SampleRate { get; set; } = 1.0;

    /// <summary>
    /// 最大函数指标数
    /// </summary>
    public int MaxFunctionMetrics { get; set; } = 100;

    /// <summary>
    /// 最大作用域指标数
    /// </summary>
    public int MaxScopeMetrics { get; set; } = 50;

    /// <summary>
    /// 是否启用内存跟踪
    /// </summary>
    public bool EnableMemoryTracking { get; set; } = true;

    /// <summary>
    /// 是否启用缓存跟踪
    /// </summary>
    public bool EnableCacheTracking { get; set; } = true;

    /// <summary>
    /// 创建默认配置
    /// </summary>
    public static PerformanceMonitorConfig Default => new()
    {
        Enabled = false,
        DetailedMonitoring = false,
        SampleRate = 1.0,
        MaxFunctionMetrics = 100,
        MaxScopeMetrics = 50,
        EnableMemoryTracking = true,
        EnableCacheTracking = true
    };

    /// <summary>
    /// 创建基础监控配置（最小开销）
    /// </summary>
    public static PerformanceMonitorConfig Basic => new()
    {
        Enabled = true,
        DetailedMonitoring = false,
        SampleRate = 1.0,
        MaxFunctionMetrics = 0,
        MaxScopeMetrics = 0,
        EnableMemoryTracking = true,
        EnableCacheTracking = true
    };

    /// <summary>
    /// 创建详细监控配置
    /// </summary>
    public static PerformanceMonitorConfig Detailed => new()
    {
        Enabled = true,
        DetailedMonitoring = true,
        SampleRate = 1.0,
        MaxFunctionMetrics = 100,
        MaxScopeMetrics = 50,
        EnableMemoryTracking = true,
        EnableCacheTracking = true
    };

    /// <summary>
    /// 验证配置的有效性
    /// </summary>
    public bool Validate()
    {
        if (SampleRate < 0 || SampleRate > 1) return false;
        if (MaxFunctionMetrics < 0) return false;
        if (MaxScopeMetrics < 0) return false;

        return true;
    }
}
