using System.Diagnostics;

namespace Old8Lang.LangParser.Optimization;

/// <summary>
/// 解析器性能指标
/// </summary>
/// <remarks>
/// 用于收集和报告解析器的性能数据，包括时间、内存、GC 等指标。
/// </remarks>
public sealed class ParserPerformanceMetrics
{
    // 时间指标
    public long TokenizationTimeMs { get; set; }
    public long ParsingTimeMs { get; set; }
    public long TotalTimeMs { get; set; }

    // 数量指标
    public long TokenCount { get; set; }
    public long SourceCodeLength { get; set; }

    // 内存指标
    public long MemoryAllocatedBytes { get; set; }
    public long PeakMemoryUsageBytes { get; set; }

    // GC 指标
    public int GCGen0Collections { get; set; }
    public int GCGen1Collections { get; set; }
    public int GCGen2Collections { get; set; }

    // 计算属性
    public double TokensPerSecond => TotalTimeMs > 0 ? TokenCount / (TotalTimeMs / 1000.0) : 0;
    public double CharsPerSecond => TotalTimeMs > 0 ? SourceCodeLength / (TotalTimeMs / 1000.0) : 0;

    /// <summary>
    /// 格式化输出性能指标
    /// </summary>
    public override string ToString()
    {
        return $"""
            解析性能指标:
            - 词法分析: {TokenizationTimeMs}ms
            - 语法分析: {ParsingTimeMs}ms
            - 总时间: {TotalTimeMs}ms
            - Token 数量: {TokenCount}
            - 源代码长度: {SourceCodeLength} 字符
            - Token/秒: {TokensPerSecond:F0}
            - 字符/秒: {CharsPerSecond:F0}
            - 内存分配: {MemoryAllocatedBytes / 1024.0:F2} KB
            - 峰值内存: {PeakMemoryUsageBytes / 1024.0:F2} KB
            - GC 收集: Gen0={GCGen0Collections}, Gen1={GCGen1Collections}, Gen2={GCGen2Collections}
            """;
    }

    /// <summary>
    /// 创建性能指标收集器
    /// </summary>
    /// <returns>新的性能指标实例</returns>
    public static ParserPerformanceMetrics Create()
    {
        return new ParserPerformanceMetrics();
    }

    /// <summary>
    /// 开始收集性能指标
    /// </summary>
    /// <param name="sourceCodeLength">源代码长度</param>
    /// <returns>性能指标实例和计时器</returns>
    public static (ParserPerformanceMetrics metrics, Stopwatch stopwatch, long memoryBefore, int gc0Before, int gc1Before, int gc2Before) BeginCollection(int sourceCodeLength)
    {
        var metrics = new ParserPerformanceMetrics
        {
            SourceCodeLength = sourceCodeLength
        };

        // 强制 GC 以获得更准确的内存测量（仅用于性能测试）
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var memoryBefore = GC.GetTotalMemory(false);
        var gc0Before = GC.CollectionCount(0);
        var gc1Before = GC.CollectionCount(1);
        var gc2Before = GC.CollectionCount(2);
        var stopwatch = Stopwatch.StartNew();

        return (metrics, stopwatch, memoryBefore, gc0Before, gc1Before, gc2Before);
    }

    /// <summary>
    /// 结束收集并计算指标
    /// </summary>
    public static void EndCollection(
        ParserPerformanceMetrics metrics,
        Stopwatch stopwatch,
        long memoryBefore,
        int gc0Before,
        int gc1Before,
        int gc2Before,
        long tokenCount)
    {
        stopwatch.Stop();
        var memoryAfter = GC.GetTotalMemory(false);
        var gc0After = GC.CollectionCount(0);
        var gc1After = GC.CollectionCount(1);
        var gc2After = GC.CollectionCount(2);

        metrics.TotalTimeMs = stopwatch.ElapsedMilliseconds;
        metrics.TokenCount = tokenCount;
        metrics.MemoryAllocatedBytes = Math.Max(0, memoryAfter - memoryBefore);
        metrics.PeakMemoryUsageBytes = memoryAfter;
        metrics.GCGen0Collections = gc0After - gc0Before;
        metrics.GCGen1Collections = gc1After - gc1Before;
        metrics.GCGen2Collections = gc2After - gc2Before;
    }
}
