using System;
using System.Diagnostics;
using System.IO;
using Old8Lang.Interpreter;
using Xunit;

namespace Old8Lang.Tests.Interpreter.Performance;

/// <summary>
/// 中等规模程序性能测试
/// 目标: 1000行程序执行时间 <2s，性能提升 ≥40%
/// </summary>
public class MediumProgramPerformanceTests
{
    private const int WarmupRuns = 2;
    private const int MeasurementRuns = 5;
    private const long TargetExecutionTimeMs = 2000;

    [Fact]
    public void MediumProgram_ExecutionTime_ShouldBeLessThan2Seconds()
    {
        // Arrange
        var assemblyLocation = Path.GetDirectoryName(typeof(MediumProgramPerformanceTests).Assembly.Location)!;
        var projectRoot = Path.GetFullPath(Path.Combine(assemblyLocation, "..", "..", "..", ".."));
        var scriptPath = Path.Combine(projectRoot, "TestScripts", "Performance", "medium-program-1000lines.old8");

        if (!File.Exists(scriptPath))
        {
            throw new FileNotFoundException($"测试脚本未找到: {scriptPath}");
        }

        var code = File.ReadAllText(scriptPath);

        // 在大栈线程上运行，避免 xUnit 线程池线程栈溢出
        double avgTime = 0;
        Exception? threadException = null;
        var thread = new Thread(() =>
        {
            try
            {
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

                avgTime = CalculateAverage(times);
            }
            catch (Exception ex)
            {
                threadException = ex;
            }
        }, 64 * 1024 * 1024); // 64MB 栈，避免深层调用栈溢出

        thread.Start();
        thread.Join();

        if (threadException != null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(threadException).Throw();

        // Assert
        Assert.True(avgTime < TargetExecutionTimeMs,
            $"平均执行时间 {avgTime}ms 超过目标 {TargetExecutionTimeMs}ms");
    }

    [Fact]
    public void MediumProgram_WithPerformanceMonitoring_ShouldTrackMetrics()
    {
        // Arrange
        var assemblyLocation = Path.GetDirectoryName(typeof(MediumProgramPerformanceTests).Assembly.Location)!;
        var projectRoot = Path.GetFullPath(Path.Combine(assemblyLocation, "..", "..", "..", ".."));
        var scriptPath = Path.Combine(projectRoot, "TestScripts", "Performance", "medium-program-1000lines.old8");

        if (!File.Exists(scriptPath))
        {
            throw new FileNotFoundException($"测试脚本未找到: {scriptPath}");
        }

        var code = File.ReadAllText(scriptPath);

        PerformanceMetrics? metrics = null;
        Exception? threadException = null;
        var thread = new Thread(() =>
        {
            try
            {
                var monitor = new PerformanceMonitor();
                monitor.StartMonitoring(new PerformanceMonitorConfig
                {
                    Enabled = true,
                    EnableCacheTracking = true,
                    EnableMemoryTracking = true
                });

                var interpreter = new LangInterpreter(monitor);
                var ast = interpreter.Build(code);
                ast.Run(interpreter.Manager);

                monitor.StopMonitoring();
                metrics = monitor.GetMetrics();
            }
            catch (Exception ex)
            {
                threadException = ex;
            }
        }, 64 * 1024 * 1024);

        thread.Start();
        thread.Join();

        if (threadException != null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(threadException).Throw();

        // Assert
        Assert.NotNull(metrics);
        Assert.True(metrics!.ExecutionTimeMs > 0, "执行时间应该被记录");
        Assert.True(metrics.VariableLookupCount > 0, "变量查找次数应该被记录");
        Assert.True(metrics.CacheHitRate >= 0, "缓存命中率应该被计算");
    }

    [Fact]
    public void MediumProgram_CacheHitRate_ShouldBeHigh()
    {
        // Arrange
        var assemblyLocation = Path.GetDirectoryName(typeof(MediumProgramPerformanceTests).Assembly.Location)!;
        var projectRoot = Path.GetFullPath(Path.Combine(assemblyLocation, "..", "..", "..", ".."));
        var scriptPath = Path.Combine(projectRoot, "TestScripts", "Performance", "medium-program-1000lines.old8");

        if (!File.Exists(scriptPath))
        {
            throw new FileNotFoundException($"测试脚本未找到: {scriptPath}");
        }

        var code = File.ReadAllText(scriptPath);

        PerformanceMetrics? metrics = null;
        Exception? threadException = null;
        var thread = new Thread(() =>
        {
            try
            {
                var monitor = new PerformanceMonitor();
                monitor.StartMonitoring(new PerformanceMonitorConfig
                {
                    Enabled = true,
                    EnableCacheTracking = true
                });

                var interpreter = new LangInterpreter(monitor);
                var ast = interpreter.Build(code);
                ast.Run(interpreter.Manager);

                monitor.StopMonitoring();
                metrics = monitor.GetMetrics();
            }
            catch (Exception ex)
            {
                threadException = ex;
            }
        }, 64 * 1024 * 1024);

        thread.Start();
        thread.Join();

        if (threadException != null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(threadException).Throw();

        // Assert - 中等规模程序应该有良好的缓存命中率
        Assert.NotNull(metrics);
        Assert.True(metrics!.CacheHitRate > 0.70,
            $"缓存命中率 {metrics.CacheHitRate:P} 低于 70%");
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
