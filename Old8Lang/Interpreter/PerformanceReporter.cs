using System.Text;
using System.Text.Json;

namespace Old8Lang.Interpreter;

/// <summary>
/// 性能报告生成器，支持文本、JSON、CSV 格式
/// </summary>
public class PerformanceReporter : IPerformanceReporter
{
    /// <summary>
    /// 生成文本格式报告
    /// </summary>
    public string GenerateTextReport(PerformanceMetrics metrics)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== Old8Lang 性能报告 ===");
        sb.AppendLine($"开始时间: {metrics.StartTime:yyyy-MM-dd HH:mm:ss}");
        if (metrics.EndTime.HasValue)
            sb.AppendLine($"结束时间: {metrics.EndTime.Value:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();
        sb.AppendLine("--- 执行统计 ---");
        sb.AppendLine($"执行时间:       {metrics.ExecutionTimeMs} ms");
        sb.AppendLine($"内存使用:       {metrics.MemoryUsageBytes / 1024.0:F2} KB");
        sb.AppendLine($"GC 回收次数:    {metrics.GCCollectionCount}");
        sb.AppendLine();
        sb.AppendLine("--- 运行时统计 ---");
        sb.AppendLine($"函数调用次数:   {metrics.FunctionCallCount}");
        sb.AppendLine($"变量查找次数:   {metrics.VariableLookupCount}");
        sb.AppendLine($"循环迭代次数:   {metrics.LoopIterationCount}");
        sb.AppendLine($"对象分配次数:   {metrics.ObjectAllocationCount}");
        sb.AppendLine($"缓存命中率:     {metrics.CacheHitRate:P1}");
        sb.AppendLine();

        if (metrics.ObjectPoolStats.Count > 0)
        {
            sb.AppendLine("--- 对象池统计 ---");
            foreach (var pool in metrics.ObjectPoolStats)
            {
                sb.AppendLine($"  {pool.PoolName}: 分配={pool.TotalAllocations}, 归还={pool.TotalReturns}, 活跃={pool.ActiveCount}");
            }
            sb.AppendLine();
        }

        if (metrics.FunctionMetrics.Count > 0)
        {
            sb.AppendLine("--- 函数性能 (Top 10) ---");
            foreach (var func in metrics.FunctionMetrics.OrderByDescending(f => f.TotalTimeMs).Take(10))
            {
                sb.AppendLine($"  {func.FunctionName}: 调用={func.CallCount}, 总时间={func.TotalTimeMs}ms, 平均={func.AverageTimeMs:F2}ms");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// 生成 JSON 格式报告
    /// </summary>
    public string GenerateJsonReport(PerformanceMetrics metrics)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var report = new
        {
            startTime = metrics.StartTime,
            endTime = metrics.EndTime,
            executionTimeMs = metrics.ExecutionTimeMs,
            memoryUsageBytes = metrics.MemoryUsageBytes,
            gcCollectionCount = metrics.GCCollectionCount,
            functionCallCount = metrics.FunctionCallCount,
            variableLookupCount = metrics.VariableLookupCount,
            loopIterationCount = metrics.LoopIterationCount,
            objectAllocationCount = metrics.ObjectAllocationCount,
            cacheHitRate = metrics.CacheHitRate,
            objectPoolStats = metrics.ObjectPoolStats.Select(p => new
            {
                poolName = p.PoolName,
                objectType = p.ObjectType,
                poolSize = p.PoolSize,
                activeCount = p.ActiveCount,
                availableCount = p.AvailableCount,
                totalAllocations = p.TotalAllocations,
                totalReturns = p.TotalReturns
            }),
            functionMetrics = metrics.FunctionMetrics.Select(f => new
            {
                functionName = f.FunctionName,
                callCount = f.CallCount,
                totalTimeMs = f.TotalTimeMs,
                averageTimeMs = f.AverageTimeMs,
                minTimeMs = f.MinTimeMs,
                maxTimeMs = f.MaxTimeMs
            })
        };

        return JsonSerializer.Serialize(report, options);
    }

    /// <summary>
    /// 生成 CSV 格式报告
    /// </summary>
    public string GenerateCsvReport(PerformanceMetrics metrics)
    {
        var sb = new StringBuilder();

        // 基础指标
        sb.AppendLine("指标,值");
        sb.AppendLine($"执行时间(ms),{metrics.ExecutionTimeMs}");
        sb.AppendLine($"内存使用(bytes),{metrics.MemoryUsageBytes}");
        sb.AppendLine($"GC回收次数,{metrics.GCCollectionCount}");
        sb.AppendLine($"函数调用次数,{metrics.FunctionCallCount}");
        sb.AppendLine($"变量查找次数,{metrics.VariableLookupCount}");
        sb.AppendLine($"循环迭代次数,{metrics.LoopIterationCount}");
        sb.AppendLine($"对象分配次数,{metrics.ObjectAllocationCount}");
        sb.AppendLine($"缓存命中率,{metrics.CacheHitRate:F4}");
        sb.AppendLine();

        if (metrics.FunctionMetrics.Count > 0)
        {
            sb.AppendLine("函数名,调用次数,总时间(ms),平均时间(ms),最小时间(ms),最大时间(ms)");
            foreach (var func in metrics.FunctionMetrics)
            {
                sb.AppendLine($"{func.FunctionName},{func.CallCount},{func.TotalTimeMs},{func.AverageTimeMs:F2},{func.MinTimeMs},{func.MaxTimeMs}");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// 保存报告到文件
    /// </summary>
    public void SaveReport(string content, string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(filePath, content, System.Text.Encoding.UTF8);
    }
}
