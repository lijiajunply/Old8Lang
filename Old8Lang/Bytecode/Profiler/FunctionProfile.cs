using System.Diagnostics;

namespace Old8Lang.Bytecode.Profiler;

/// <summary>
/// 函数性能统计数据
/// </summary>
public class FunctionProfile
{
    /// <summary>函数名称</summary>
    public string Name { get; set; } = "";

    /// <summary>调用次数</summary>
    public long CallCount { get; set; }

    /// <summary>总执行时间（毫秒）</summary>
    public double TotalTime { get; set; }

    /// <summary>平均执行时间（毫秒）</summary>
    public double AverageTime => CallCount > 0 ? TotalTime / CallCount : 0;

    /// <summary>最小执行时间（毫秒）</summary>
    public double MinTime { get; set; } = double.MaxValue;

    /// <summary>最大执行时间（毫秒）</summary>
    public double MaxTime { get; set; }
}
