using System;
using System.Diagnostics;
using System.IO;
using Old8Lang.Interpreter;
using Old8Lang.LangParser;
using Xunit;

namespace Old8Lang.Tests.Interpreter.Performance;

/// <summary>
/// 小型脚本性能测试
/// 目标: 50行脚本执行时间 <100ms，性能提升 ≥50%
/// </summary>
public class SmallScriptPerformanceTests
{
    private const int WarmupRuns = 3;
    private const int MeasurementRuns = 10;
    private const long TargetExecutionTimeMs = 100;

    [Fact]
    public void SmallScript_ExecutionTime_ShouldBeLessThan100ms()
    {
        // Arrange
        var scriptPath = Path.Combine("TestScripts", "Performance", "small-script-50lines.old8");
        var code = File.ReadAllText(scriptPath);

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

        // Calculate average
        var avgTime = CalculateAverage(times);

        // Assert
        Assert.True(avgTime < TargetExecutionTimeMs,
            $"平均执行时间 {avgTime}ms 超过目标 {TargetExecutionTimeMs}ms");
    }

    [Fact]
    public void SmallScript_WithPerformanceMonitoring_OverheadShouldBeLessThan1Percent()
    {
        // Arrange
        var scriptPath = Path.Combine("TestScripts", "Performance", "small-script-50lines.old8");
        var code = File.ReadAllText(scriptPath);

        // Measure without monitoring
        var timesWithoutMonitoring = new long[MeasurementRuns];
        for (int i = 0; i < MeasurementRuns; i++)
        {
            var sw = Stopwatch.StartNew();
            var interpreter = new LangInterpreter();
            var ast = interpreter.Build(code);
            ast.Run(interpreter.Manager);
            sw.Stop();
            timesWithoutMonitoring[i] = sw.ElapsedMilliseconds;
        }

        // Measure with monitoring
        var timesWithMonitoring = new long[MeasurementRuns];
        for (int i = 0; i < MeasurementRuns; i++)
        {
            var monitor = new PerformanceMonitor();
            monitor.StartMonitoring();

            var sw = Stopwatch.StartNew();
            var interpreter = new LangInterpreter(monitor);
            var ast = interpreter.Build(code);
            ast.Run(interpreter.Manager);
            sw.Stop();

            monitor.StopMonitoring();
            timesWithMonitoring[i] = sw.ElapsedMilliseconds;
        }

        // Calculate overhead
        var avgWithout = CalculateAverage(timesWithoutMonitoring);
        var avgWith = CalculateAverage(timesWithMonitoring);
        var overhead = (avgWith - avgWithout) / (double)avgWithout;

        // Assert
        Assert.True(overhead < 0.01,
            $"性能监控开销 {overhead:P} 超过 1%");
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
