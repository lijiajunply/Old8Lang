using System;
using System.Collections.Generic;

namespace Old8Lang.Interpreter;

/// <summary>
/// 性能指标数据，存储解释器执行的性能统计信息
/// </summary>
public class PerformanceMetrics
{
    /// <summary>
    /// 总执行时间（毫秒）
    /// </summary>
    public long ExecutionTimeMs { get; set; }

    /// <summary>
    /// 内存使用峰值（字节）
    /// </summary>
    public long MemoryUsageBytes { get; set; }

    /// <summary>
    /// 函数调用总次数
    /// </summary>
    public int FunctionCallCount { get; set; }

    /// <summary>
    /// 变量查找总次数
    /// </summary>
    public int VariableLookupCount { get; set; }

    /// <summary>
    /// 循环迭代总次数
    /// </summary>
    public long LoopIterationCount { get; set; }

    /// <summary>
    /// 对象分配次数
    /// </summary>
    public int ObjectAllocationCount { get; set; }

    /// <summary>
    /// 缓存命中率（0-1）
    /// </summary>
    public double CacheHitRate { get; set; }

    /// <summary>
    /// GC 回收次数
    /// </summary>
    public int GCCollectionCount { get; set; }

    /// <summary>
    /// 监控开始时间
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 监控结束时间
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 函数级别性能指标
    /// </summary>
    public List<FunctionMetrics> FunctionMetrics { get; set; } = new();

    /// <summary>
    /// 作用域级别性能指标
    /// </summary>
    public List<ScopeMetrics> ScopeMetrics { get; set; } = new();

    /// <summary>
    /// 验证性能指标数据的有效性
    /// </summary>
    public bool Validate()
    {
        if (ExecutionTimeMs < 0) return false;
        if (MemoryUsageBytes < 0) return false;
        if (FunctionCallCount < 0) return false;
        if (VariableLookupCount < 0) return false;
        if (LoopIterationCount < 0) return false;
        if (ObjectAllocationCount < 0) return false;
        if (CacheHitRate < 0 || CacheHitRate > 1) return false;
        if (GCCollectionCount < 0) return false;
        if (EndTime.HasValue && EndTime.Value < StartTime) return false;

        return true;
    }
}

/// <summary>
/// 函数级别性能指标
/// </summary>
public class FunctionMetrics
{
    /// <summary>
    /// 函数名称
    /// </summary>
    public string FunctionName { get; set; } = string.Empty;

    /// <summary>
    /// 调用次数
    /// </summary>
    public int CallCount { get; set; }

    /// <summary>
    /// 总执行时间（毫秒）
    /// </summary>
    public long TotalTimeMs { get; set; }

    /// <summary>
    /// 平均执行时间（毫秒）
    /// </summary>
    public double AverageTimeMs => CallCount > 0 ? (double)TotalTimeMs / CallCount : 0;

    /// <summary>
    /// 最小执行时间（毫秒）
    /// </summary>
    public long MinTimeMs { get; set; } = long.MaxValue;

    /// <summary>
    /// 最大执行时间（毫秒）
    /// </summary>
    public long MaxTimeMs { get; set; }

    /// <summary>
    /// 最大递归深度
    /// </summary>
    public int RecursionDepth { get; set; }
}

/// <summary>
/// 作用域级别性能指标
/// </summary>
public class ScopeMetrics
{
    /// <summary>
    /// 作用域标识符
    /// </summary>
    public string ScopeId { get; set; } = string.Empty;

    /// <summary>
    /// 作用域层级（0=全局）
    /// </summary>
    public int ScopeLevel { get; set; }

    /// <summary>
    /// 查找次数
    /// </summary>
    public int LookupCount { get; set; }

    /// <summary>
    /// 缓存命中次数
    /// </summary>
    public int CacheHitCount { get; set; }

    /// <summary>
    /// 缓存未命中次数
    /// </summary>
    public int CacheMissCount { get; set; }

    /// <summary>
    /// 变量数量
    /// </summary>
    public int VariableCount { get; set; }

    /// <summary>
    /// 缓存命中率
    /// </summary>
    public double CacheHitRate => LookupCount > 0 ? (double)CacheHitCount / LookupCount : 0;
}

/// <summary>
/// 对象池统计信息
/// </summary>
public class ObjectPoolStats
{
    /// <summary>
    /// 对象池名称
    /// </summary>
    public string PoolName { get; set; } = string.Empty;

    /// <summary>
    /// 对象类型名称
    /// </summary>
    public string ObjectType { get; set; } = string.Empty;

    /// <summary>
    /// 池大小
    /// </summary>
    public int PoolSize { get; set; }

    /// <summary>
    /// 活跃对象数
    /// </summary>
    public int ActiveCount { get; set; }

    /// <summary>
    /// 可用对象数
    /// </summary>
    public int AvailableCount { get; set; }

    /// <summary>
    /// 总分配次数
    /// </summary>
    public long TotalAllocations { get; set; }

    /// <summary>
    /// 总归还次数
    /// </summary>
    public long TotalReturns { get; set; }

    /// <summary>
    /// 池命中率（0-1）
    /// </summary>
    public double CacheHitRate => TotalAllocations > 0 ? (double)(TotalAllocations - (TotalAllocations - TotalReturns)) / TotalAllocations : 0;

    /// <summary>
    /// 验证对象池统计数据的有效性
    /// </summary>
    public bool Validate()
    {
        if (PoolSize <= 0) return false;
        if (ActiveCount < 0) return false;
        if (AvailableCount < 0) return false;
        if (ActiveCount + AvailableCount > PoolSize) return false;
        if (TotalAllocations < 0) return false;
        if (TotalReturns < 0) return false;
        if (TotalReturns > TotalAllocations) return false;

        return true;
    }
}
