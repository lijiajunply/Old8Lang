using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Old8Lang.Interpreter;

/// <summary>
/// 性能监控器实现
/// </summary>
public class PerformanceMonitor : IPerformanceMonitor
{
    private PerformanceMonitorConfig _config = PerformanceMonitorConfig.Default;
    private PerformanceMetrics _metrics = new();
    private readonly Stopwatch _stopwatch = new();
    private readonly object _lock = new();
    private long _initialMemory;
    private int _initialGCCount;

    // 函数级别指标缓存
    private readonly ConcurrentDictionary<string, FunctionMetrics> _functionMetrics = new();

    // 作用域级别指标缓存
    private readonly ConcurrentDictionary<string, ScopeMetrics> _scopeMetrics = new();

    /// <summary>
    /// 检查监控是否启用
    /// </summary>
    public bool IsMonitoring { get; private set; }

    /// <summary>
    /// 启动性能监控
    /// </summary>
    public void StartMonitoring(PerformanceMonitorConfig? config = null)
    {
        if (IsMonitoring)
        {
            throw new InvalidOperationException("性能监控已在运行。请先调用 StopMonitoring()。");
        }

        _config = config ?? PerformanceMonitorConfig.Default;

        if (!_config.Validate())
        {
            throw new ArgumentException("无效的配置参数", nameof(config));
        }

        lock (_lock)
        {
            _metrics = new PerformanceMetrics
            {
                StartTime = DateTime.Now
            };

            _functionMetrics.Clear();
            _scopeMetrics.Clear();

            if (_config.EnableMemoryTracking)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                _initialMemory = GC.GetTotalMemory(false);
                _initialGCCount = GC.CollectionCount(0);
            }

            _stopwatch.Restart();
            IsMonitoring = true;
        }
    }

    /// <summary>
    /// 停止性能监控
    /// </summary>
    public void StopMonitoring()
    {
        if (!IsMonitoring)
        {
            return;
        }

        lock (_lock)
        {
            _stopwatch.Stop();
            _metrics.EndTime = DateTime.Now;
            _metrics.ExecutionTimeMs = _stopwatch.ElapsedMilliseconds;

            if (_config.EnableMemoryTracking)
            {
                var currentMemory = GC.GetTotalMemory(false);
                _metrics.MemoryUsageBytes = Math.Max(0, currentMemory - _initialMemory);
                _metrics.GCCollectionCount = GC.CollectionCount(0) - _initialGCCount;
            }

            // 计算缓存命中率
            if (_metrics.VariableLookupCount > 0)
            {
                var totalHits = _scopeMetrics.Values.Sum(s => s.CacheHitCount);
                _metrics.CacheHitRate = (double)totalHits / _metrics.VariableLookupCount;
            }

            // 复制函数和作用域指标
            if (_config.DetailedMonitoring)
            {
                _metrics.FunctionMetrics.AddRange(_functionMetrics.Values.Take(_config.MaxFunctionMetrics));
                _metrics.ScopeMetrics.AddRange(_scopeMetrics.Values.Take(_config.MaxScopeMetrics));
            }

            IsMonitoring = false;
        }
    }

    /// <summary>
    /// 获取当前性能指标
    /// </summary>
    public PerformanceMetrics GetMetrics()
    {
        if (IsMonitoring)
        {
            throw new InvalidOperationException("性能监控未启用。请先调用 StartMonitoring()。");
        }

        lock (_lock)
        {
            return _metrics;
        }
    }

    /// <summary>
    /// 重置性能指标
    /// </summary>
    public void ResetMetrics()
    {
        lock (_lock)
        {
            _metrics = new PerformanceMetrics();
            _functionMetrics.Clear();
            _scopeMetrics.Clear();
            _stopwatch.Reset();
        }
    }

    /// <summary>
    /// 记录函数调用
    /// </summary>
    public void RecordFunctionCall(string functionName, long executionTimeMs)
    {
        if (!IsMonitoring || !_config.Enabled)
        {
            return;
        }

        // 采样检查
        if (_config.SampleRate < 1.0 && Random.Shared.NextDouble() > _config.SampleRate)
        {
            return;
        }

        System.Threading.Interlocked.Increment(ref _metrics.FunctionCallCount);

        if (_config.DetailedMonitoring && _functionMetrics.Count < _config.MaxFunctionMetrics)
        {
            _functionMetrics.AddOrUpdate(
                functionName,
                _ => new FunctionMetrics
                {
                    FunctionName = functionName,
                    CallCount = 1,
                    TotalTimeMs = executionTimeMs,
                    MinTimeMs = executionTimeMs,
                    MaxTimeMs = executionTimeMs
                },
                (_, existing) =>
                {
                    existing.CallCount++;
                    existing.TotalTimeMs += executionTimeMs;
                    existing.MinTimeMs = Math.Min(existing.MinTimeMs, executionTimeMs);
                    existing.MaxTimeMs = Math.Max(existing.MaxTimeMs, executionTimeMs);
                    return existing;
                });
        }
    }

    /// <summary>
    /// 记录变量查找
    /// </summary>
    public void RecordVariableLookup(string scopeId, bool cacheHit)
    {
        if (!IsMonitoring || !_config.Enabled)
        {
            return;
        }

        if (!_config.EnableCacheTracking)
        {
            return;
        }

        System.Threading.Interlocked.Increment(ref _metrics.VariableLookupCount);

        if (_config.DetailedMonitoring && _scopeMetrics.Count < _config.MaxScopeMetrics)
        {
            _scopeMetrics.AddOrUpdate(
                scopeId,
                _ => new ScopeMetrics
                {
                    ScopeId = scopeId,
                    LookupCount = 1,
                    CacheHitCount = cacheHit ? 1 : 0,
                    CacheMissCount = cacheHit ? 0 : 1
                },
                (_, existing) =>
                {
                    existing.LookupCount++;
                    if (cacheHit)
                    {
                        existing.CacheHitCount++;
                    }
                    else
                    {
                        existing.CacheMissCount++;
                    }
                    return existing;
                });
        }
    }

    /// <summary>
    /// 记录对象分配
    /// </summary>
    public void RecordObjectAllocation(string objectType)
    {
        if (!IsMonitoring || !_config.Enabled)
        {
            return;
        }

        System.Threading.Interlocked.Increment(ref _metrics.ObjectAllocationCount);
    }

    /// <summary>
    /// 记录循环迭代
    /// </summary>
    public void RecordLoopIteration()
    {
        if (!IsMonitoring || !_config.Enabled)
        {
            return;
        }

        System.Threading.Interlocked.Increment(ref _metrics.LoopIterationCount);
    }
}
