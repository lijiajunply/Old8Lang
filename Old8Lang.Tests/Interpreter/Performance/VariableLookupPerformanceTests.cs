using System;
using System.Diagnostics;
using System.IO;
using Old8Lang.Interpreter;
using Old8Lang.LangParser;
using Xunit;

namespace Old8Lang.Tests.Interpreter.Performance;

/// <summary>
/// 变量查找性能测试
/// 目标: 变量查找性能提升 ≥40%
/// </summary>
public class VariableLookupPerformanceTests
{
    private const int WarmupRuns = 3;
    private const int MeasurementRuns = 10;

    [Fact]
    public void VariableLookup_CacheHitRate_ShouldBeGreaterThan80Percent()
    {
        // Arrange
        var scriptPath = Path.Combine("TestScripts", "Performance", "variable-lookup.old8");
        var code = File.ReadAllText(scriptPath);

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

        // Assert
        Assert.True(metrics.CacheHitRate > 0.80,
            $"缓存命中率 {metrics.CacheHitRate:P} 低于 80%");
    }

    [Fact]
    public void VariableLookup_GlobalVariables_ShouldBeFast()
    {
        // Arrange
        var code = @"
            global_var1 = 100
            global_var2 = 200
            global_var3 = 300

            function test() {
                for (i = 0; i < 1000; i++) {
                    x = global_var1 + global_var2 + global_var3
                }
            }

            test()
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

        // Assert - 全局变量查找应该很快
        Assert.True(avgTime < 50,
            $"全局变量查找平均时间 {avgTime}ms 过长");
    }

    [Fact]
    public void VariableLookup_NestedScopes_ShouldUseFlattenedCache()
    {
        // Arrange
        var code = @"
            outer_var = 100

            function outer() {
                middle_var = 200

                function middle() {
                    inner_var = 300

                    function inner() {
                        for (i = 0; i < 100; i++) {
                            sum = outer_var + middle_var + inner_var
                        }
                    }

                    inner()
                }

                middle()
            }

            outer()
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

        // Assert - 嵌套作用域应该有良好的缓存命中率
        Assert.True(metrics.CacheHitRate > 0.70,
            $"嵌套作用域缓存命中率 {metrics.CacheHitRate:P} 低于 70%");
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
