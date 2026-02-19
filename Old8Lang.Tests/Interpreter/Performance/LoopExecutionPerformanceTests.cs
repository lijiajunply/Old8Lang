using System;
using System.Diagnostics;
using Old8Lang.Interpreter;
using Old8Lang.LangParser;
using Xunit;

namespace Old8Lang.Tests.Interpreter.Performance;

/// <summary>
/// 循环执行性能测试
/// 目标: 嵌套循环性能提升 ≥30%
/// </summary>
public class LoopExecutionPerformanceTests
{
    private const int WarmupRuns = 3;
    private const int MeasurementRuns = 10;

    [Fact]
    public void SimpleLoop_ExecutionTime_ShouldBeFast()
    {
        // Arrange
        var code = @"
            sum <- 0
            for i <- 0, i < 10000, i <- i + 1 {
                sum <- sum + i
            }
        ";

        // Warmup
        for (int i = 0; i < WarmupRuns; i++)
        {
            var interpreter = new LangInterpreter();
            var ast = interpreter.Build(code);
            ast.Run(interpreter.Manager);
        }

        // Measure
        var times = new long[MeasurementRuns];
        for (int i = 0; i < MeasurementRuns; i++)
        {
            var sw = Stopwatch.StartNew();
            var interpreter = new LangInterpreter();
            var ast = interpreter.Build(code);
            ast.Run(interpreter.Manager);
            sw.Stop();
            times[i] = sw.ElapsedMilliseconds;
        }

        var avgTime = CalculateAverage(times);

        // Assert
        Assert.True(avgTime < 100,
            $"简单循环平均执行时间 {avgTime}ms 过长");
    }

    [Fact]
    public void NestedLoop_ExecutionTime_ShouldBeOptimized()
    {
        // Arrange
        var code = @"
            sum <- 0
            for i <- 0, i < 100, i <- i + 1 {
                for j <- 0, j < 100, j <- j + 1 {
                    sum <- sum + i * j
                }
            }
        ";

        // Warmup
        for (int i = 0; i < WarmupRuns; i++)
        {
            var interpreter = new LangInterpreter();
            var ast = interpreter.Build(code);
            ast.Run(interpreter.Manager);
        }

        // Measure
        var times = new long[MeasurementRuns];
        for (int i = 0; i < MeasurementRuns; i++)
        {
            var sw = Stopwatch.StartNew();
            var interpreter = new LangInterpreter();
            var ast = interpreter.Build(code);
            ast.Run(interpreter.Manager);
            sw.Stop();
            times[i] = sw.ElapsedMilliseconds;
        }

        var avgTime = CalculateAverage(times);

        // Assert - 嵌套循环应该在合理时间内完成
        Assert.True(avgTime < 500,
            $"嵌套循环平均执行时间 {avgTime}ms 过长");
    }

    [Fact]
    public void LoopWithInvariantHoisting_ShouldBeOptimized()
    {
        // Arrange
        var code = @"
            constant <- 100
            sum <- 0

            for i <- 0, i < 1000, i <- i + 1 {
                // 这个计算是循环不变的，应该被提升
                multiplier <- constant * 2
                sum <- sum + i * multiplier
            }
        ";

        var monitor = new PerformanceMonitor();
        monitor.StartMonitoring(new PerformanceMonitorConfig
        {
            Enabled = true,
            DetailedMonitoring = true
        });

        // Act
        var interpreter = new LangInterpreter(monitor);
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        monitor.StopMonitoring();
        var metrics = monitor.GetMetrics();

        // Assert - 循环迭代计数应该正确
        Assert.True(metrics.LoopIterationCount >= 1000,
            $"循环迭代计数 {metrics.LoopIterationCount} 不正确");
    }

    [Fact]
    public void LoopWithVariableLookup_CacheHitRate_ShouldBeHigh()
    {
        // Arrange
        var code = @"
            global_var <- 100
            sum <- 0

            for i <- 0, i < 1000, i <- i + 1 {
                sum <- sum + global_var
            }
        ";

        var monitor = new PerformanceMonitor();
        monitor.StartMonitoring(new PerformanceMonitorConfig
        {
            Enabled = true,
            EnableCacheTracking = true
        });

        // Act
        var interpreter = new LangInterpreter(monitor);
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        monitor.StopMonitoring();
        var metrics = monitor.GetMetrics();

        // Assert - 循环内的变量查找应该有很高的缓存命中率
        Assert.True(metrics.CacheHitRate > 0.90,
            $"循环内变量查找缓存命中率 {metrics.CacheHitRate:P} 低于 90%");
    }

    private static double CalculateAverage(long[] values)
    {
        long sum = 0;
        foreach (var value in values)
        {
            sum += value;
        }
        return sum / (double)values.Length;
    }
}
