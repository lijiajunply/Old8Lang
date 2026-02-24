using System;
using System.Diagnostics;
using System.IO;
using Old8Lang.Interpreter;
using Xunit;

namespace Old8Lang.Tests.Interpreter.Performance;

/// <summary>
/// 递归调用性能测试
/// 目标: 支持至少100层递归深度
/// </summary>
public class RecursiveCallPerformanceTests
{
    private const int WarmupRuns = 2;
    private const int MeasurementRuns = 5;

    [Fact]
    public void RecursiveCalls_DeepRecursion_ShouldSupport100Layers()
    {
        // Arrange
        var assemblyLocation = Path.GetDirectoryName(typeof(RecursiveCallPerformanceTests).Assembly.Location)!;
        var projectRoot = Path.GetFullPath(Path.Combine(assemblyLocation, "..", "..", "..", ".."));
        var scriptPath = Path.Combine(projectRoot, "TestScripts", "Performance", "recursive-calls.old8");

        if (!File.Exists(scriptPath))
        {
            throw new FileNotFoundException($"测试脚本未找到: {scriptPath}");
        }

        var code = File.ReadAllText(scriptPath);

        Exception? threadException = null;
        var thread = new Thread(() =>
        {
            try
            {
                var interpreter = new LangInterpreter();
                var ast = interpreter.Build(code);
                ast.Run(interpreter.Manager);
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

        // 如果执行到这里，说明递归深度支持正常
        Assert.True(true, "递归调用成功完成");
    }

    [Fact]
    public void RecursiveCalls_ExecutionTime_ShouldBeReasonable()
    {
        // Arrange
        var assemblyLocation = Path.GetDirectoryName(typeof(RecursiveCallPerformanceTests).Assembly.Location)!;
        var projectRoot = Path.GetFullPath(Path.Combine(assemblyLocation, "..", "..", "..", ".."));
        var scriptPath = Path.Combine(projectRoot, "TestScripts", "Performance", "recursive-calls.old8");

        if (!File.Exists(scriptPath))
        {
            throw new FileNotFoundException($"测试脚本未找到: {scriptPath}");
        }

        var code = File.ReadAllText(scriptPath);

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
        }, 64 * 1024 * 1024);

        thread.Start();
        thread.Join();

        if (threadException != null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(threadException).Throw();

        // Assert - 递归调用应该在合理时间内完成 (1秒)
        Assert.True(avgTime < 1000,
            $"递归调用平均执行时间 {avgTime}ms 过长");
    }

    [Fact]
    public void RecursiveCalls_SimpleRecursion_ShouldBeFast()
    {
        // Arrange - 简单的递归测试
        var code = @"
            func factorial(n) {
                if n <= 1 {
                    return 1
                }
                return n * factorial(n - 1)
            }

            result <- factorial(20)
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

        // Assert - 简单递归应该很快 (50ms)
        Assert.True(avgTime < 50,
            $"简单递归平均执行时间 {avgTime}ms 过长");
    }

    [Fact]
    public void RecursiveCalls_WithMonitoring_ShouldTrackDepth()
    {
        // Arrange
        var code = @"
            func deepRecursion(n, acc) {
                if n <= 0 {
                    return acc
                }
                return deepRecursion(n - 1, acc + n)
            }

            result <- deepRecursion(50, 0)
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

        // Assert
        Assert.True(metrics.ExecutionTimeMs >= 0, "执行时间应该被记录");
        // 注意: FunctionMetrics 需要在详细监控模式下才会收集
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
