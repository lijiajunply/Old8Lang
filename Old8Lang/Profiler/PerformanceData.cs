namespace Old8Lang.Profiler;

/// <summary>
/// 性能计数器类型
/// </summary>
public enum PerformanceCounterType
{
    /// <summary>
    /// 执行时间
    /// </summary>
    ExecutionTime,
    
    /// <summary>
    /// 内存使用
    /// </summary>
    MemoryUsage,
    
    /// <summary>
    /// 函数调用次数
    /// </summary>
    FunctionCallCount,
    
    /// <summary>
    /// 垃圾回收次数
    /// </summary>
    GarbageCollectionCount,
    
    /// <summary>
    /// 字节码生成时间
    /// </summary>
    CompilationTime,
    
    /// <summary>
    /// 解析时间
    /// </summary>
    ParsingTime
}

/// <summary>
/// 性能数据点
/// </summary>
public class PerformanceDataPoint
{
    /// <summary>
    /// 数据点名称
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 计数器类型
    /// </summary>
    public PerformanceCounterType Type { get; set; }
    
    /// <summary>
    /// 值
    /// </summary>
    public double Value { get; set; }
    
    /// <summary>
    /// 单位
    /// </summary>
    public string Unit { get; set; } = string.Empty;
    
    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;
    
    /// <summary>
    /// 标签/元数据
    /// </summary>
    public Dictionary<string, string> Tags { get; set; } = new();
    
    public override string ToString()
    {
        return $"{Name}: {Value:F2} {Unit} ({Type})";
    }
}

/// <summary>
/// 函数性能统计
/// </summary>
public class FunctionPerformanceStats
{
    /// <summary>
    /// 函数名
    /// </summary>
    public string FunctionName { get; set; } = string.Empty;
    
    /// <summary>
    /// 调用次数
    /// </summary>
    public long CallCount { get; set; }
    
    /// <summary>
    /// 总执行时间（毫秒）
    /// </summary>
    public double TotalExecutionTimeMs { get; set; }
    
    /// <summary>
    /// 平均执行时间（毫秒）
    /// </summary>
    public double AverageExecutionTimeMs => CallCount > 0 ? TotalExecutionTimeMs / CallCount : 0;
    
    /// <summary>
    /// 最小执行时间（毫秒）
    /// </summary>
    public double MinExecutionTimeMs { get; set; } = double.MaxValue;
    
    /// <summary>
    /// 最大执行时间（毫秒）
    /// </summary>
    public double MaxExecutionTimeMs { get; set; }
    
    /// <summary>
    /// 最后执行时间
    /// </summary>
    public DateTime LastExecutionTime { get; set; }
    
    /// <summary>
    /// 源文件位置
    /// </summary>
    public string? SourceFile { get; set; }
    
    /// <summary>
    /// 行号
    /// </summary>
    public int? LineNumber { get; set; }
    
    /// <summary>
    /// 执行时间历史记录
    /// </summary>
    public List<double> ExecutionTimes { get; set; } = new();
    
    /// <summary>
    /// 添加执行时间记录
    /// </summary>
    /// <param name="executionTimeMs">执行时间（毫秒）</param>
    public void AddExecutionTime(double executionTimeMs)
    {
        CallCount++;
        TotalExecutionTimeMs += executionTimeMs;
        MinExecutionTimeMs = Math.Min(MinExecutionTimeMs, executionTimeMs);
        MaxExecutionTimeMs = Math.Max(MaxExecutionTimeMs, executionTimeMs);
        LastExecutionTime = DateTime.Now;
        ExecutionTimes.Add(executionTimeMs);
        
        // 限制历史记录数量，避免内存泄漏
        if (ExecutionTimes.Count > 1000)
        {
            ExecutionTimes.RemoveAt(0);
        }
    }
    
    /// <summary>
    /// 获取执行时间标准差（使用总体标准差）
    /// </summary>
    public double GetStandardDeviation()
    {
        if (ExecutionTimes.Count < 2) return 0;

        var mean = AverageExecutionTimeMs;
        var sumOfSquares = ExecutionTimes.Sum(t => Math.Pow(t - mean, 2));
        return Math.Sqrt(sumOfSquares / ExecutionTimes.Count);
    }
    
    /// <summary>
    /// 获取中位数
    /// </summary>
    public double GetMedian()
    {
        if (ExecutionTimes.Count == 0) return 0;
        
        var sortedTimes = ExecutionTimes.OrderBy(t => t).ToList();
        int count = sortedTimes.Count;
        
        if (count % 2 == 0)
        {
            return (sortedTimes[count / 2 - 1] + sortedTimes[count / 2]) / 2;
        }
        else
        {
            return sortedTimes[count / 2];
        }
    }
    
    public override string ToString()
    {
        return $"{FunctionName}: {CallCount} calls, avg {AverageExecutionTimeMs:F2}ms, total {TotalExecutionTimeMs:F2}ms";
    }
}

/// <summary>
/// 内存使用统计
/// </summary>
public class MemoryUsageStats
{
    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;
    
    /// <summary>
    /// 托管内存（字节）
    /// </summary>
    public long ManagedMemoryBytes { get; set; }
    
    /// <summary>
    /// 托管内存（MB）
    /// </summary>
    public double ManagedMemoryMB => ManagedMemoryBytes / (1024.0 * 1024.0);
    
    /// <summary>
    /// 非托管内存（字节）
    /// </summary>
    public long UnmanagedMemoryBytes { get; set; }
    
    /// <summary>
    /// 非托管内存（MB）
    /// </summary>
    public double UnmanagedMemoryMB => UnmanagedMemoryBytes / (1024.0 * 1024.0);
    
    /// <summary>
    /// GC代数0的回收次数
    /// </summary>
    public long Gen0Collections { get; set; }
    
    /// <summary>
    /// GC代数1的回收次数
    /// </summary>
    public long Gen1Collections { get; set; }
    
    /// <summary>
    /// GC代数2的回收次数
    /// </summary>
    public long Gen2Collections { get; set; }
    
    /// <summary>
    /// 总GC次数
    /// </summary>
    public long TotalGcCollections => Gen0Collections + Gen1Collections + Gen2Collections;
    
    /// <summary>
    /// 进程工作集（字节）
    /// </summary>
    public long WorkingSetBytes { get; set; }
    
    /// <summary>
    /// 进程工作集（MB）
    /// </summary>
    public double WorkingSetMB => WorkingSetBytes / (1024.0 * 1024.0);
    
    /// <summary>
    /// 私有内存（字节）
    /// </summary>
    public long PrivateMemoryBytes { get; set; }
    
    /// <summary>
    /// 私有内存（MB）
    /// </summary>
    public double PrivateMemoryMB => PrivateMemoryBytes / (1024.0 * 1024.0);
    
    public override string ToString()
    {
        return $"Memory: {ManagedMemoryMB:F2}MB managed, {UnmanagedMemoryMB:F2}MB unmanaged, GC: {TotalGcCollections} times";
    }
}

/// <summary>
/// 性能分析会话
/// </summary>
public class ProfilingSession
{
    /// <summary>
    /// 会话ID
    /// </summary>
    public Guid SessionId { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// 会话名称
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime StartTime { get; set; } = DateTime.Now;
    
    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTime? EndTime { get; set; }
    
    /// <summary>
    /// 会话持续时间（毫秒）
    /// </summary>
    public double DurationMs => EndTime?.Subtract(StartTime).TotalMilliseconds ?? DateTime.Now.Subtract(StartTime).TotalMilliseconds;
    
    /// <summary>
    /// 函数性能统计
    /// </summary>
    public Dictionary<string, FunctionPerformanceStats> FunctionStats { get; set; } = new();
    
    /// <summary>
    /// 内存使用历史
    /// </summary>
    public List<MemoryUsageStats> MemoryHistory { get; set; } = new();
    
    /// <summary>
    /// 通用性能数据点
    /// </summary>
    public List<PerformanceDataPoint> DataPoints { get; set; } = new();
    
    /// <summary>
    /// 源文件路径
    /// </summary>
    public string? SourceFilePath { get; set; }
    
    /// <summary>
    /// 执行模式（解释/编译）
    /// </summary>
    public string? ExecutionMode { get; set; }
    
    /// <summary>
    /// 标签/元数据
    /// </summary>
    public Dictionary<string, string> Tags { get; set; } = new();
    
    /// <summary>
    /// 是否正在运行
    /// </summary>
    public bool IsActive => EndTime == null;
    
    /// <summary>
    /// 结束会话
    /// </summary>
    public void EndSession()
    {
        if (!EndTime.HasValue)
        {
            EndTime = DateTime.Now;
        }
    }
    
    /// <summary>
    /// 添加函数执行记录
    /// </summary>
    /// <param name="functionName">函数名</param>
    /// <param name="executionTimeMs">执行时间（毫秒）</param>
    /// <param name="sourceFile">源文件</param>
    /// <param name="lineNumber">行号</param>
    public void RecordFunctionExecution(string functionName, double executionTimeMs, string? sourceFile = null, int? lineNumber = null)
    {
        if (!FunctionStats.ContainsKey(functionName))
        {
            FunctionStats[functionName] = new FunctionPerformanceStats
            {
                FunctionName = functionName,
                SourceFile = sourceFile,
                LineNumber = lineNumber
            };
        }
        
        FunctionStats[functionName].AddExecutionTime(executionTimeMs);
    }
    
    /// <summary>
    /// 记录内存使用情况
    /// </summary>
    public void RecordMemoryUsage()
    {
        var stats = new MemoryUsageStats();
        
        // 获取当前进程信息
        using var process = System.Diagnostics.Process.GetCurrentProcess();
        stats.WorkingSetBytes = process.WorkingSet64;
        stats.PrivateMemoryBytes = process.PrivateMemorySize64;
        
        // 获取GC信息
        stats.Gen0Collections = GC.CollectionCount(0);
        stats.Gen1Collections = GC.CollectionCount(1);
        stats.Gen2Collections = GC.CollectionCount(2);
        
        // 获取托管内存信息
        stats.ManagedMemoryBytes = GC.GetTotalMemory(false);
        
        MemoryHistory.Add(stats);
        
        // 限制历史记录数量
        if (MemoryHistory.Count > 1000)
        {
            MemoryHistory.RemoveAt(0);
        }
    }
    
    /// <summary>
    /// 添加性能数据点
    /// </summary>
    /// <param name="dataPoint">数据点</param>
    public void AddDataPoint(PerformanceDataPoint dataPoint)
    {
        DataPoints.Add(dataPoint);
    }
    
    /// <summary>
    /// 获取热点函数（按总执行时间排序）
    /// </summary>
    /// <param name="topCount">返回前N个</param>
    /// <returns>热点函数列表</returns>
    public List<FunctionPerformanceStats> GetHotspotFunctions(int topCount = 10)
    {
        return FunctionStats.Values
            .OrderByDescending(f => f.TotalExecutionTimeMs)
            .Take(topCount)
            .ToList();
    }
    
    /// <summary>
    /// 获取最频繁调用的函数（按调用次数排序）
    /// </summary>
    /// <param name="topCount">返回前N个</param>
    /// <returns>函数列表</returns>
    public List<FunctionPerformanceStats> GetMostFrequentFunctions(int topCount = 10)
    {
        return FunctionStats.Values
            .OrderByDescending(f => f.CallCount)
            .Take(topCount)
            .ToList();
    }
    
    /// <summary>
    /// 获取最慢的函数（按平均执行时间排序）
    /// </summary>
    /// <param name="topCount">返回前N个</param>
    /// <returns>函数列表</returns>
    public List<FunctionPerformanceStats> GetSlowestFunctions(int topCount = 10)
    {
        return FunctionStats.Values
            .Where(f => f.CallCount > 0)
            .OrderByDescending(f => f.AverageExecutionTimeMs)
            .Take(topCount)
            .ToList();
    }
}