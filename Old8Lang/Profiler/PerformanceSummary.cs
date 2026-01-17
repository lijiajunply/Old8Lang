namespace Old8Lang.Profiler;

/// <summary>
/// 性能摘要
/// </summary>
public class PerformanceSummary
{
    /// <summary>
    /// 性能会话
    /// </summary>
    public ProfilingSession Session { get; set; } = null!;
    
    /// <summary>
    /// 性能瓶颈列表
    /// </summary>
    public List<PerformanceBottleneck> Bottlenecks { get; set; } = [];
    
    /// <summary>
    /// 总体性能分数（0-100，100最优）
    /// </summary>
    public double OverallScore { get; set; }
    
    /// <summary>
    /// 总体建议
    /// </summary>
    public string Recommendation { get; set; } = string.Empty;
    
    /// <summary>
    /// 关键指标
    /// </summary>
    public Dictionary<string, double> KeyMetrics { get; set; } = new();
    
    /// <summary>
    /// 性能等级
    /// </summary>
    public PerformanceGrade Grade => OverallScore switch
    {
        >= 90 => PerformanceGrade.A,
        >= 80 => PerformanceGrade.B,
        >= 70 => PerformanceGrade.C,
        >= 60 => PerformanceGrade.D,
        _ => PerformanceGrade.F
    };
    
    /// <summary>
    /// 格式化性能分数
    /// </summary>
    public string FormattedScore => $"{OverallScore:F1}/100 ({Grade})";
    
    /// <summary>
    /// 生成文本摘要
    /// </summary>
    public override string ToString()
    {
        var summary = $"性能分析摘要 - {FormattedScore}\n";
        summary += $"会话时长: {Session.DurationMs:F0}ms\n";
        summary += $"函数调用总数: {Session.FunctionStats.Values.Sum(f => f.CallCount):N0}\n";
        summary += $"瓶颈数量: {Bottlenecks.Count}\n";
        
        if (Bottlenecks.Count > 0)
        {
            summary += "\n主要瓶颈:\n";
            foreach (var bottleneck in Bottlenecks.Take(3))
            {
                summary += $"  - {bottleneck.Description}\n";
            }
        }
        
        summary += $"\n建议: {Recommendation}";
        
        return summary;
    }
}

/// <summary>
/// 性能等级
/// </summary>
public enum PerformanceGrade
{
    /// <summary>
    /// 优秀 (90-100)
    /// </summary>
    A,
    
    /// <summary>
    /// 良好 (80-89)
    /// </summary>
    B,
    
    /// <summary>
    /// 一般 (70-79)
    /// </summary>
    C,
    
    /// <summary>
    /// 较差 (60-69)
    /// </summary>
    D,
    
    /// <summary>
    /// 很差 (0-59)
    /// </summary>
    F
}