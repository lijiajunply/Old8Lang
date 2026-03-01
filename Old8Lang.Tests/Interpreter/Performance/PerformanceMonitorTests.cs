using System;
using Old8Lang.Interpreter;
using Xunit;

namespace Old8Lang.Tests.Interpreter.Performance;

/// <summary>
/// 性能监控器单元测试
/// </summary>
public class PerformanceMonitorTests
{
    [Fact]
    public void StartMonitoring_ShouldSetIsMonitoringTrue()
    {
        var monitor = new PerformanceMonitor();
        monitor.StartMonitoring();
        Assert.True(monitor.IsMonitoring);
        monitor.StopMonitoring();
    }

    [Fact]
    public void StopMonitoring_ShouldSetIsMonitoringFalse()
    {
        var monitor = new PerformanceMonitor();
        monitor.StartMonitoring();
        monitor.StopMonitoring();
        Assert.False(monitor.IsMonitoring);
    }

    [Fact]
    public void StartMonitoring_WhenAlreadyRunning_ShouldThrow()
    {
        var monitor = new PerformanceMonitor();
        monitor.StartMonitoring();
        Assert.Throws<InvalidOperationException>(() => monitor.StartMonitoring());
        monitor.StopMonitoring();
    }

    [Fact]
    public void GetMetrics_WhenMonitoring_ShouldThrow()
    {
        var monitor = new PerformanceMonitor();
        monitor.StartMonitoring();
        Assert.Throws<InvalidOperationException>(() => monitor.GetMetrics());
        monitor.StopMonitoring();
    }

    [Fact]
    public void RecordFunctionCall_ShouldIncrementCount()
    {
        var monitor = new PerformanceMonitor();
        monitor.StartMonitoring(new PerformanceMonitorConfig { Enabled = true });
        monitor.RecordFunctionCall("testFunc", 10);
        monitor.RecordFunctionCall("testFunc", 20);
        monitor.StopMonitoring();

        var metrics = monitor.GetMetrics();
        Assert.Equal(2, metrics.FunctionCallCount);
    }

    [Fact]
    public void RecordVariableLookup_ShouldTrackCacheHits()
    {
        var monitor = new PerformanceMonitor();
        monitor.StartMonitoring(new PerformanceMonitorConfig
        {
            Enabled = true,
            EnableCacheTracking = true
        });

        monitor.RecordVariableLookup("scope1", cacheHit: true);
        monitor.RecordVariableLookup("scope1", cacheHit: true);
        monitor.RecordVariableLookup("scope1", cacheHit: false);
        monitor.StopMonitoring();

        var metrics = monitor.GetMetrics();
        Assert.Equal(3, metrics.VariableLookupCount);
        Assert.True(metrics.CacheHitRate > 0.5, $"缓存命中率 {metrics.CacheHitRate} 应该 >0.5");
    }

    [Fact]
    public void RecordLoopIteration_ShouldIncrementCount()
    {
        var monitor = new PerformanceMonitor();
        monitor.StartMonitoring(new PerformanceMonitorConfig { Enabled = true });

        for (int i = 0; i < 100; i++)
        {
            monitor.RecordLoopIteration();
        }

        monitor.StopMonitoring();
        var metrics = monitor.GetMetrics();
        Assert.Equal(100, metrics.LoopIterationCount);
    }

    [Fact]
    public void GetMetrics_ShouldReturnValidMetrics()
    {
        var monitor = new PerformanceMonitor();
        monitor.StartMonitoring(PerformanceMonitorConfig.Default);
        monitor.StopMonitoring();

        var metrics = monitor.GetMetrics();
        Assert.NotNull(metrics);
        Assert.True(metrics.Validate(), "指标数据应该有效");
    }

    [Fact]
    public void DetectPerformanceDegradation_WithNoBaseline_ShouldReturnFalse()
    {
        var monitor = new PerformanceMonitor();
        monitor.StartMonitoring();
        monitor.StopMonitoring();

        Assert.False(monitor.DetectPerformanceDegradation());
    }

    [Fact]
    public void PerformanceMonitorConfig_Default_ShouldBeValid()
    {
        var config = PerformanceMonitorConfig.Default;
        Assert.True(config.Validate());
        Assert.True(config.Enabled);
    }

    [Fact]
    public void PerformanceMonitorConfig_Basic_ShouldBeValid()
    {
        var config = PerformanceMonitorConfig.Basic;
        Assert.True(config.Validate());
        Assert.True(config.Enabled);
    }

    [Fact]
    public void PerformanceMonitorConfig_Detailed_ShouldBeValid()
    {
        var config = PerformanceMonitorConfig.Detailed;
        Assert.True(config.Validate());
        Assert.True(config.Enabled);
        Assert.True(config.DetailedMonitoring);
    }

    [Fact]
    public void ObjectPoolStats_Validate_ShouldReturnTrue()
    {
        var stats = new ObjectPoolStats
        {
            PoolName = "TestPool",
            ObjectType = "TestType",
            PoolSize = 100,
            ActiveCount = 10,
            AvailableCount = 90,
            TotalAllocations = 200,
            TotalReturns = 190
        };
        Assert.True(stats.Validate());
    }
}
