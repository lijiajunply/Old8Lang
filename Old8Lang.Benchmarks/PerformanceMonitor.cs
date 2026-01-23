using System.Diagnostics;
using System.Text;

namespace Old8Lang.Benchmarks;

/// <summary>
/// 性能监控工具类
/// 提供详细的性能分析和内存监控功能
/// </summary>
public class PerformanceMonitor
{
    private readonly Stopwatch _stopwatch = new();
    private long _initialMemory;
    private long _peakMemory;
    private int _gcCollectionsBefore;
    private int _gcCollectionsAfter;

    /// <summary>
    /// 开始性能监控
    /// </summary>
    public void StartMonitoring()
    {
        // 强制垃圾回收以获得准确的初始内存使用量
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        _initialMemory = GC.GetTotalMemory(false);
        _peakMemory = _initialMemory;

        // 记录GC次数
        _gcCollectionsBefore = GC.CollectionCount(0) + GC.CollectionCount(1) + GC.CollectionCount(2);

        _stopwatch.Restart();
    }

    /// <summary>
    /// 停止性能监控并生成报告
    /// </summary>
    public PerformanceReport StopMonitoring()
    {
        _stopwatch.Stop();

        // 记录结束时的内存使用
        long finalMemory = GC.GetTotalMemory(false);
        _gcCollectionsAfter = GC.CollectionCount(0) + GC.CollectionCount(1) + GC.CollectionCount(2);

        return new PerformanceReport
        {
            ExecutionTime = _stopwatch.Elapsed,
            InitialMemory = _initialMemory,
            FinalMemory = finalMemory,
            MemoryUsed = finalMemory - _initialMemory,
            PeakMemory = _peakMemory,
            GCCollections = _gcCollectionsAfter - _gcCollectionsBefore
        };
    }

    /// <summary>
    /// 更新峰值内存使用量
    /// </summary>
    public void UpdatePeakMemory()
    {
        long currentMemory = GC.GetTotalMemory(false);
        if (currentMemory > _peakMemory)
        {
            _peakMemory = currentMemory;
        }
    }
}

/// <summary>
/// 性能报告类
/// 包含详细的性能分析结果
/// </summary>
public class PerformanceReport
{
    public TimeSpan ExecutionTime { get; set; }
    public long InitialMemory { get; set; }
    public long FinalMemory { get; set; }
    public long MemoryUsed { get; set; }
    public long PeakMemory { get; set; }
    public int GCCollections { get; set; }

    /// <summary>
    /// 生成详细的性能报告
    /// </summary>
    public string GenerateReport()
    {
        var report = new StringBuilder();
        report.AppendLine("=== 性能分析报告 ===");
        report.AppendLine();

        // 时间分析
        report.AppendLine("⏱️  执行时间分析:");
        report.AppendLine($"   执行时间: {ExecutionTime.TotalMilliseconds:F2} ms");
        report.AppendLine($"   执行时间: {ExecutionTime.TotalSeconds:F4} 秒");
        report.AppendLine($"   执行时间: {ExecutionTime.Ticks} ticks");
        report.AppendLine();

        // 内存分析
        report.AppendLine("💾 内存使用分析:");
        report.AppendLine($"   初始内存: {InitialMemory / 1024.0:F2} KB");
        report.AppendLine($"   最终内存: {FinalMemory / 1024.0:F2} KB");
        report.AppendLine($"   使用内存: {MemoryUsed / 1024.0:F2} KB");
        report.AppendLine($"   峰值内存: {PeakMemory / 1024.0:F2} KB");
        report.AppendLine($"   内存效率: {(double)MemoryUsed / Math.Max(1, PeakMemory) * 100:F2}%");
        report.AppendLine();

        // 垃圾回收分析
        report.AppendLine("🗑️  垃圾回收分析:");
        report.AppendLine($"   GC次数: {GCCollections}");
        report.AppendLine($"   GC压力: {(GCCollections > 0 ? "高" : "低")}");
        report.AppendLine();

        // 性能评级
        report.AppendLine("📊 性能评级:");
        report.AppendLine($"   时间评级: {GetTimeRating()}");
        report.AppendLine($"   内存评级: {GetMemoryRating()}");
        report.AppendLine($"   总体评级: {GetOverallRating()}");
        report.AppendLine();

        return report.ToString();
    }

    private string GetTimeRating()
    {
        if (ExecutionTime.TotalMilliseconds < 10) return "优秀 ⭐⭐⭐⭐⭐";
        if (ExecutionTime.TotalMilliseconds < 50) return "良好 ⭐⭐⭐⭐";
        if (ExecutionTime.TotalMilliseconds < 100) return "一般 ⭐⭐⭐";
        if (ExecutionTime.TotalMilliseconds < 500) return "较慢 ⭐⭐";
        return "很慢 ⭐";
    }

    private string GetMemoryRating()
    {
        double memoryMB = MemoryUsed / (1024.0 * 1024.0);
        if (memoryMB < 1) return "优秀 ⭐⭐⭐⭐⭐";
        if (memoryMB < 5) return "良好 ⭐⭐⭐⭐";
        if (memoryMB < 20) return "一般 ⭐⭐⭐";
        if (memoryMB < 50) return "较高 ⭐⭐";
        return "很高 ⭐";
    }

    private string GetOverallRating()
    {
        var timeScore = ExecutionTime.TotalMilliseconds;
        var memoryScore = MemoryUsed / (1024.0 * 1024.0);

        if (timeScore < 50 && memoryScore < 5) return "优秀 ⭐⭐⭐⭐⭐";
        if (timeScore < 100 && memoryScore < 20) return "良好 ⭐⭐⭐⭐";
        if (timeScore < 200 && memoryScore < 50) return "一般 ⭐⭐⭐";
        if (timeScore < 500) return "及格 ⭐⭐";
        return "需要优化 ⭐";
    }

    /// <summary>
    /// 生成CSV格式的报告（用于数据分析）
    /// </summary>
    public string GenerateCSVReport()
    {
        return $"{ExecutionTime.TotalMilliseconds:F2},{MemoryUsed / 1024.0:F2},{PeakMemory / 1024.0:F2},{GCCollections}";
    }
}

/// <summary>
/// 性能测试辅助工具
/// </summary>
public static class PerformanceTestHelper
{
    /// <summary>
    /// 执行带性能监控的测试
    /// </summary>
    public static PerformanceReport ExecuteWithMonitoring(Action testAction)
    {
        var monitor = new PerformanceMonitor();
        monitor.StartMonitoring();

        testAction();

        return monitor.StopMonitoring();
    }

    /// <summary>
    /// 执行多次测试并计算平均性能
    /// </summary>
    public static PerformanceReport ExecuteMultipleWithMonitoring(Action testAction, int iterations = 5)
    {
        var reports = new List<PerformanceReport>();

        for (int i = 0; i < iterations; i++)
        {
            var report = ExecuteWithMonitoring(testAction);
            reports.Add(report);

            // 在测试之间进行垃圾回收
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        return CalculateAverageReport(reports);
    }

    /// <summary>
    /// 计算多个性能报告的平均值
    /// </summary>
    private static PerformanceReport CalculateAverageReport(List<PerformanceReport> reports)
    {
        return new PerformanceReport
        {
            ExecutionTime = TimeSpan.FromTicks((long)reports.Average(r => r.ExecutionTime.Ticks)),
            InitialMemory = (long)reports.Average(r => r.InitialMemory),
            FinalMemory = (long)reports.Average(r => r.FinalMemory),
            MemoryUsed = (long)reports.Average(r => r.MemoryUsed),
            PeakMemory = (long)reports.Average(r => r.PeakMemory),
            GCCollections = (int)reports.Average(r => r.GCCollections)
        };
    }
}