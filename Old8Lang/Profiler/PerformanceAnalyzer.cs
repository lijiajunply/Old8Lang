namespace Old8Lang.Profiler;

/// <summary>
/// 性能瓶颈类型
/// </summary>
public enum BottleneckType
{
    /// <summary>
    /// 高执行时间
    /// </summary>
    HighExecutionTime,
    
    /// <summary>
    /// 高内存使用
    /// </summary>
    HighMemoryUsage,
    
    /// <summary>
    /// 频繁垃圾回收
    /// </summary>
    FrequentGarbageCollection,
    
    /// <summary>
    /// 函数调用过多
    /// </summary>
    ExcessiveFunctionCalls,
    
    /// <summary>
    /// 执行时间不稳定
    /// </summary>
    UnstableExecutionTime
}

/// <summary>
/// 性能瓶颈
/// </summary>
public class PerformanceBottleneck
{
    /// <summary>
    /// 瓶颈类型
    /// </summary>
    public BottleneckType Type { get; set; }
    
    /// <summary>
    /// 描述
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// 严重程度（1-10，10最严重）
    /// </summary>
    public int Severity { get; set; }
    
    /// <summary>
    /// 影响的函数名
    /// </summary>
    public string? FunctionName { get; set; }
    
    /// <summary>
    /// 建议的解决方案
    /// </summary>
    public string? Suggestion { get; set; }
    
    /// <summary>
    /// 相关的性能指标
    /// </summary>
    public Dictionary<string, double> Metrics { get; set; } = new();
    
    public override string ToString()
    {
        return $"[{Type}] {Description} (严重程度: {Severity}/10)";
    }
}

/// <summary>
/// 性能分析引擎
/// </summary>
public class PerformanceAnalyzer
{
    /// <summary>
    /// 高执行时间阈值（毫秒）
    /// </summary>
    public double HighExecutionTimeThresholdMs { get; set; } = 100.0;
    
    /// <summary>
    /// 高内存使用阈值（MB）
    /// </summary>
    public double HighMemoryUsageThresholdMB { get; set; } = 100.0;
    
    /// <summary>
    /// 频繁GC阈值（每分钟次数）
    /// </summary>
    public int FrequentGCThresholdPerMinute { get; set; } = 10;
    
    /// <summary>
    /// 过多函数调用阈值
    /// </summary>
    public long ExcessiveFunctionCallThreshold { get; set; } = 10000;
    
    /// <summary>
    /// 执行时间不稳定阈值（标准差/平均值）
    /// </summary>
    public double UnstableExecutionTimeThreshold { get; set; } = 0.5;
    
    /// <summary>
    /// 分析性能会话
    /// </summary>
    /// <param name="session">性能会话</param>
    /// <returns>性能瓶颈列表</returns>
    public List<PerformanceBottleneck> AnalyzeSession(ProfilingSession session)
    {
        if (session == null)
            throw new ArgumentNullException(nameof(session));
        
        var bottlenecks = new List<PerformanceBottleneck>();
        
        // 分析执行时间瓶颈
        bottlenecks.AddRange(AnalyzeExecutionTimeBottlenecks(session));
        
        // 分析内存使用瓶颈
        bottlenecks.AddRange(AnalyzeMemoryUsageBottlenecks(session));
        
        // 分析垃圾回收瓶颈
        bottlenecks.AddRange(AnalyzeGarbageCollectionBottlenecks(session));
        
        // 分析函数调用频率
        bottlenecks.AddRange(AnalyzeFunctionCallBottlenecks(session));
        
        // 分析执行时间稳定性
        bottlenecks.AddRange(AnalyzeExecutionTimeStability(session));
        
        return bottlenecks.OrderByDescending(b => b.Severity).ToList();
    }
    
    /// <summary>
    /// 分析执行时间瓶颈
    /// </summary>
    private List<PerformanceBottleneck> AnalyzeExecutionTimeBottlenecks(ProfilingSession session)
    {
        var bottlenecks = new List<PerformanceBottleneck>();
        
        // 找出执行时间最长的函数
        var slowFunctions = session.FunctionStats.Values
            .Where(f => f.AverageExecutionTimeMs > HighExecutionTimeThresholdMs)
            .OrderByDescending(f => f.AverageExecutionTimeMs)
            .ToList();
        
        foreach (var func in slowFunctions)
        {
            var severity = CalculateSeverity(func.AverageExecutionTimeMs, HighExecutionTimeThresholdMs);
            
            bottlenecks.Add(new PerformanceBottleneck
            {
                Type = BottleneckType.HighExecutionTime,
                Description = $"函数 '{func.FunctionName}' 平均执行时间过长: {func.AverageExecutionTimeMs:F2}ms",
                FunctionName = func.FunctionName,
                Severity = severity,
                Suggestion = GenerateExecutionTimeSuggestion(func),
                Metrics = new Dictionary<string, double>
                {
                    ["average_time_ms"] = func.AverageExecutionTimeMs,
                    ["total_time_ms"] = func.TotalExecutionTimeMs,
                    ["call_count"] = func.CallCount,
                    ["max_time_ms"] = func.MaxExecutionTimeMs,
                    ["min_time_ms"] = func.MinExecutionTimeMs
                }
            });
        }
        
        return bottlenecks;
    }
    
    /// <summary>
    /// 分析内存使用瓶颈
    /// </summary>
    private List<PerformanceBottleneck> AnalyzeMemoryUsageBottlenecks(ProfilingSession session)
    {
        var bottlenecks = new List<PerformanceBottleneck>();
        
        if (session.MemoryHistory.Count == 0)
            return bottlenecks;
        
        // 找出内存使用峰值
        var maxMemoryUsage = session.MemoryHistory.Max(m => m.ManagedMemoryMB);
        var avgMemoryUsage = session.MemoryHistory.Average(m => m.ManagedMemoryMB);
        
        if (maxMemoryUsage > HighMemoryUsageThresholdMB)
        {
            var severity = CalculateSeverity(maxMemoryUsage, HighMemoryUsageThresholdMB);
            
            bottlenecks.Add(new PerformanceBottleneck
            {
                Type = BottleneckType.HighMemoryUsage,
                Description = $"内存使用过高: 峰值 {maxMemoryUsage:F2}MB，平均 {avgMemoryUsage:F2}MB",
                Severity = severity,
                Suggestion = "考虑优化内存分配模式，使用对象池，减少大对象分配，或及时释放不再使用的对象",
                Metrics = new Dictionary<string, double>
                {
                    ["peak_memory_mb"] = maxMemoryUsage,
                    ["avg_memory_mb"] = avgMemoryUsage,
                    ["memory_growth_rate"] = CalculateMemoryGrowthRate(session)
                }
            });
        }
        
        return bottlenecks;
    }
    
    /// <summary>
    /// 分析垃圾回收瓶颈
    /// </summary>
    private List<PerformanceBottleneck> AnalyzeGarbageCollectionBottlenecks(ProfilingSession session)
    {
        var bottlenecks = new List<PerformanceBottleneck>();
        
        if (session.MemoryHistory.Count < 2)
            return bottlenecks;
        
        // 计算GC频率
        var firstGC = session.MemoryHistory.First();
        var lastGC = session.MemoryHistory.Last();
        var durationMinutes = (lastGC.Timestamp - firstGC.Timestamp).TotalMinutes;
        
        if (durationMinutes <= 0)
            return bottlenecks;
        
        var gen0GCPerMinute = (lastGC.Gen0Collections - firstGC.Gen0Collections) / durationMinutes;
        var gen1GCPerMinute = (lastGC.Gen1Collections - firstGC.Gen1Collections) / durationMinutes;
        var gen2GCPerMinute = (lastGC.Gen2Collections - firstGC.Gen2Collections) / durationMinutes;
        
        if (gen0GCPerMinute > FrequentGCThresholdPerMinute)
        {
            var severity = CalculateSeverity(gen0GCPerMinute, FrequentGCThresholdPerMinute);
            
            bottlenecks.Add(new PerformanceBottleneck
            {
                Type = BottleneckType.FrequentGarbageCollection,
                Description = $"垃圾回收过于频繁: Gen0 {gen0GCPerMinute:F1}次/分钟, Gen1 {gen1GCPerMinute:F1}次/分钟, Gen2 {gen2GCPerMinute:F1}次/分钟",
                Severity = severity,
                Suggestion = "减少临时对象分配，使用对象池，预分配内存，或考虑使用结构体替代类",
                Metrics = new Dictionary<string, double>
                {
                    ["gen0_gc_per_minute"] = gen0GCPerMinute,
                    ["gen1_gc_per_minute"] = gen1GCPerMinute,
                    ["gen2_gc_per_minute"] = gen2GCPerMinute,
                    ["total_gen0_gc"] = lastGC.Gen0Collections,
                    ["total_gen1_gc"] = lastGC.Gen1Collections,
                    ["total_gen2_gc"] = lastGC.Gen2Collections
                }
            });
        }
        
        return bottlenecks;
    }
    
    /// <summary>
    /// 分析函数调用频率瓶颈
    /// </summary>
    private List<PerformanceBottleneck> AnalyzeFunctionCallBottlenecks(ProfilingSession session)
    {
        var bottlenecks = new List<PerformanceBottleneck>();
        
        // 找出调用次数过多的函数
        var frequentFunctions = session.FunctionStats.Values
            .Where(f => f.CallCount > ExcessiveFunctionCallThreshold)
            .OrderByDescending(f => f.CallCount)
            .ToList();
        
        foreach (var func in frequentFunctions)
        {
            var severity = CalculateSeverity(func.CallCount, ExcessiveFunctionCallThreshold);
            
            bottlenecks.Add(new PerformanceBottleneck
            {
                Type = BottleneckType.ExcessiveFunctionCalls,
                Description = $"函数 '{func.FunctionName}' 调用次数过多: {func.CallCount:N0} 次",
                FunctionName = func.FunctionName,
                Severity = severity,
                Suggestion = "考虑使用缓存、循环优化、减少递归调用，或将频繁调用的代码内联",
                Metrics = new Dictionary<string, double>
                {
                    ["call_count"] = func.CallCount,
                    ["total_time_ms"] = func.TotalExecutionTimeMs,
                    ["avg_time_ms"] = func.AverageExecutionTimeMs,
                    ["calls_per_second"] = func.CallCount / (session.DurationMs / 1000.0)
                }
            });
        }
        
        return bottlenecks;
    }
    
    /// <summary>
    /// 分析执行时间稳定性
    /// </summary>
    private List<PerformanceBottleneck> AnalyzeExecutionTimeStability(ProfilingSession session)
    {
        var bottlenecks = new List<PerformanceBottleneck>();
        
        // 找出执行时间不稳定的函数
        var unstableFunctions = session.FunctionStats.Values
            .Where(f => f.ExecutionTimes.Count > 5 && CalculateCoefficicientOfVariation(f) > UnstableExecutionTimeThreshold)
            .OrderByDescending(f => CalculateCoefficicientOfVariation(f))
            .ToList();
        
        foreach (var func in unstableFunctions)
        {
            var cv = CalculateCoefficicientOfVariation(func);
            var severity = CalculateSeverity(cv, UnstableExecutionTimeThreshold);
            
            bottlenecks.Add(new PerformanceBottleneck
            {
                Type = BottleneckType.UnstableExecutionTime,
                Description = $"函数 '{func.FunctionName}' 执行时间不稳定: 变异系数 {cv:F3}",
                FunctionName = func.FunctionName,
                Severity = severity,
                Suggestion = "检查函数中的条件分支、循环或依赖的外部资源，确保执行路径一致",
                Metrics = new Dictionary<string, double>
                {
                    ["coefficient_of_variation"] = cv,
                    ["std_deviation"] = func.GetStandardDeviation(),
                    ["mean_time_ms"] = func.AverageExecutionTimeMs,
                    ["median_time_ms"] = func.GetMedian(),
                    ["execution_count"] = func.ExecutionTimes.Count
                }
            });
        }
        
        return bottlenecks;
    }
    
    /// <summary>
    /// 计算严重程度
    /// </summary>
    private static int CalculateSeverity(double actualValue, double threshold)
    {
        if (actualValue <= threshold)
            return 1;
        
        var ratio = actualValue / threshold;
        return Math.Min(10, (int)Math.Ceiling(ratio));
    }
    
    /// <summary>
    /// 生成执行时间优化建议
    /// </summary>
    private static string GenerateExecutionTimeSuggestion(FunctionPerformanceStats func)
    {
        if (func.CallCount > 1000)
            return $"函数调用频繁({func.CallCount:N0}次)，考虑使用缓存或算法优化";
        
        if (func.MaxExecutionTimeMs > func.AverageExecutionTimeMs * 3)
            return "执行时间波动较大，检查条件分支和循环";
        
        return "考虑优化算法、减少循环次数、使用更高效的数据结构";
    }
    
    /// <summary>
    /// 计算内存增长率
    /// </summary>
    private static double CalculateMemoryGrowthRate(ProfilingSession session)
    {
        if (session.MemoryHistory.Count < 2)
            return 0;
        
        var first = session.MemoryHistory.First();
        var last = session.MemoryHistory.Last();
        var durationMinutes = (last.Timestamp - first.Timestamp).TotalMinutes;
        
        if (durationMinutes <= 0)
            return 0;
        
        var memoryGrowth = last.ManagedMemoryMB - first.ManagedMemoryMB;
        return memoryGrowth / durationMinutes; // MB per minute
    }
    
    /// <summary>
    /// 计算变异系数（标准差/平均值）
    /// </summary>
    private static double CalculateCoefficicientOfVariation(FunctionPerformanceStats func)
    {
        if (func.AverageExecutionTimeMs <= 0)
            return 0;
        
        return func.GetStandardDeviation() / func.AverageExecutionTimeMs;
    }
    
    /// <summary>
    /// 生成性能摘要报告
    /// </summary>
    /// <param name="session">性能会话</param>
    /// <returns>性能摘要</returns>
    public PerformanceSummary GenerateSummary(ProfilingSession session)
    {
        var bottlenecks = AnalyzeSession(session);
        
        return new PerformanceSummary
        {
            Session = session,
            Bottlenecks = bottlenecks,
            OverallScore = CalculateOverallScore(bottlenecks),
            Recommendation = GenerateOverallRecommendation(bottlenecks)
        };
    }
    
    /// <summary>
    /// 计算总体性能分数
    /// </summary>
    private static double CalculateOverallScore(List<PerformanceBottleneck> bottlenecks)
    {
        if (bottlenecks.Count == 0)
            return 100.0;
        
        var totalSeverity = bottlenecks.Sum(b => b.Severity);
        var maxPossibleSeverity = bottlenecks.Count * 10;
        
        return Math.Max(0, 100.0 - (totalSeverity / maxPossibleSeverity * 100.0));
    }
    
    /// <summary>
    /// 生成总体建议
    /// </summary>
    private static string GenerateOverallRecommendation(List<PerformanceBottleneck> bottlenecks)
    {
        if (bottlenecks.Count == 0)
            return "性能表现良好，没有发现明显瓶颈";
        
        var highSeverityBottlenecks = bottlenecks.Where(b => b.Severity >= 7).ToList();
        if (highSeverityBottlenecks.Count > 0)
            return $"发现 {highSeverityBottlenecks.Count} 个严重性能问题，建议优先解决高严重程度的瓶颈";
        
        return $"发现 {bottlenecks.Count} 个性能问题，建议按严重程度顺序逐步优化";
    }
}