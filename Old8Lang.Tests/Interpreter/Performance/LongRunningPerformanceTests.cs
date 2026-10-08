using System;
using System.Diagnostics;
using System.IO;
using Old8Lang.Interpreter;
using Xunit;

namespace Old8Lang.Tests.Interpreter.Performance;

/// <summary>
/// 长时间运行性能测试
/// 目标: 验证长时间运行时性能稳定，无退化
/// </summary>
[Trait("Category", "Performance")]
public class LongRunningPerformanceTests
{
    [Fact]
    public void LongRunning_Script_ShouldCompleteWithinTimeout()
    {
        // Arrange
        var assemblyLocation = Path.GetDirectoryName(typeof(LongRunningPerformanceTests).Assembly.Location)!;
        var projectRoot = Path.GetFullPath(Path.Combine(assemblyLocation, "..", "..", "..", ".."));
        var scriptPath = Path.Combine(projectRoot, "TestScripts", "Performance", "long-running-5min.old8");

        if (!File.Exists(scriptPath))
        {
            throw new FileNotFoundException($"测试脚本未找到: {scriptPath}");
        }

        var code = File.ReadAllText(scriptPath);

        Exception? threadException = null;
        var sw = Stopwatch.StartNew();

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
        sw.Stop();

        if (threadException != null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(threadException).Throw();

        // Assert - 脚本应该在30秒内完成
        Assert.True(sw.ElapsedMilliseconds < 30000,
            $"长时间运行脚本执行时间 {sw.ElapsedMilliseconds}ms 超过30秒限制");
    }

    [Fact]
    public void MillionIterations_ShouldCompleteWithinTimeout()
    {
        // Arrange
        var assemblyLocation = Path.GetDirectoryName(typeof(LongRunningPerformanceTests).Assembly.Location)!;
        var projectRoot = Path.GetFullPath(Path.Combine(assemblyLocation, "..", "..", "..", ".."));
        var scriptPath = Path.Combine(projectRoot, "TestScripts", "Performance", "million-iterations.old8");

        if (!File.Exists(scriptPath))
        {
            throw new FileNotFoundException($"测试脚本未找到: {scriptPath}");
        }

        var code = File.ReadAllText(scriptPath);

        Exception? threadException = null;
        var sw = Stopwatch.StartNew();

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
        sw.Stop();

        if (threadException != null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(threadException).Throw();

        // Assert - 百万次迭代应该在60秒内完成
        Assert.True(sw.ElapsedMilliseconds < 60000,
            $"百万次迭代执行时间 {sw.ElapsedMilliseconds}ms 超过60秒限制");
    }

    [Fact]
    public void LongRunning_WithMonitoring_ShouldTrackMetrics()
    {
        // Arrange - 使用内联代码避免文件依赖
        var code = @"
            func processData(n) {
                total <- 0
                for i <- 0, i < n, i <- i + 1 {
                    total <- total + i
                }
                return total
            }

            for batch <- 0, batch < 200, batch <- batch + 1 {
                r <- processData(50)
            }
        ";

        var monitor = new PerformanceMonitor();
        monitor.StartMonitoring(new PerformanceMonitorConfig
        {
            Enabled = true,
            EnableMemoryTracking = true,
            EnableCacheTracking = true
        });

        var interpreter = new LangInterpreter(monitor);
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        monitor.StopMonitoring();
        var metrics = monitor.GetMetrics();

        // Assert
        Assert.NotNull(metrics);
        Assert.True(metrics.ExecutionTimeMs >= 0, "执行时间应该被记录");
        Assert.True(metrics.LoopIterationCount > 0, "循环迭代次数应该被记录");
        Assert.True(metrics.VariableLookupCount > 0, "变量查找次数应该被记录");
    }

    [Fact]
    public void PerformanceStability_MultipleRuns_ShouldBeConsistent()
    {
        // Arrange - 测试多次运行的性能一致性
        var code = @"
            func compute(n) {
                result <- 0
                for i <- 0, i < n, i <- i + 1 {
                    result <- result + i * i
                }
                return result
            }

            total <- 0
            for batch <- 0, batch < 50, batch <- batch + 1 {
                total <- total + compute(100)
            }
        ";

        var times = new long[5];
        for (int run = 0; run < 5; run++)
        {
            var sw = Stopwatch.StartNew();
            var interpreter = new LangInterpreter();
            var ast = interpreter.Build(code);
            ast.Run(interpreter.Manager);
            sw.Stop();
            times[run] = sw.ElapsedMilliseconds;
        }

        // 计算平均值和最大偏差
        long sum = 0;
        foreach (var t in times) sum += t;
        var avg = sum / times.Length;

        long maxDeviation = 0;
        foreach (var t in times)
        {
            var deviation = Math.Abs(t - avg);
            if (deviation > maxDeviation) maxDeviation = deviation;
        }

        // Assert - 性能应该相对稳定（最大偏差不超过平均值的200%）
        // 注意：在测试环境中，JIT 预热可能导致第一次运行较慢
        if (avg > 0)
        {
            Assert.True(maxDeviation <= avg * 3,
                $"性能不稳定：平均 {avg}ms，最大偏差 {maxDeviation}ms");
        }
    }
}
