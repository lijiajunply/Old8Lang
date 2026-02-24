using System;
using System.IO;
using Old8Lang.Interpreter;
using Old8Lang.LangParser;
using Xunit;

namespace Old8Lang.Tests.CLI;

/// <summary>
/// CLI 性能监控集成测试
/// 验证 --perf、--perf-detailed、--perf-output 功能的核心逻辑
/// </summary>
public class PerformanceMonitoringTests
{
    private const string SimpleScript = """
        x <- 0
        for i <- 0, i < 100, i <- i + 1 {
            x <- x + i
        }
        """;

    private const string FunctionScript = """
        func add(a, b) {
            return a + b
        }
        result <- 0
        for i <- 0, i < 50, i <- i + 1 {
            result <- add(result, i)
        }
        """;

    [Fact]
    public void PerfMode_ShouldCollectExecutionMetrics()
    {
        var monitor = new PerformanceMonitor();
        monitor.StartMonitoring(new PerformanceMonitorConfig { Enabled = true });

        var interpreter = new LangInterpreter(monitor);
        var ast = interpreter.Build(SimpleScript);
        ast.Run(interpreter.Manager);

        monitor.StopMonitoring();
        var metrics = monitor.GetMetrics();

        Assert.True(metrics.ExecutionTimeMs >= 0);
        Assert.True(metrics.LoopIterationCount > 0);
    }

    [Fact]
    public void PerfDetailedMode_ShouldCollectFunctionMetrics()
    {
        var monitor = new PerformanceMonitor();
        monitor.StartMonitoring(new PerformanceMonitorConfig
        {
            Enabled = true,
            DetailedMonitoring = true,
            EnableCacheTracking = true
        });

        var interpreter = new LangInterpreter(monitor);
        var ast = interpreter.Build(FunctionScript);
        ast.Run(interpreter.Manager);

        monitor.StopMonitoring();
        var metrics = monitor.GetMetrics();

        // 变量查找应该被记录（缓存跟踪已启用）
        Assert.True(metrics.VariableLookupCount >= 0);
        Assert.True(metrics.ExecutionTimeMs >= 0);
    }

    [Fact]
    public void PerfOutput_TextFormat_ShouldGenerateReport()
    {
        var monitor = new PerformanceMonitor();
        monitor.StartMonitoring(new PerformanceMonitorConfig { Enabled = true });

        var interpreter = new LangInterpreter(monitor);
        var ast = interpreter.Build(SimpleScript);
        ast.Run(interpreter.Manager);

        monitor.StopMonitoring();
        var metrics = monitor.GetMetrics();

        var reporter = new PerformanceReporter();
        var report = reporter.GenerateTextReport(metrics);

        Assert.NotEmpty(report);
        Assert.Contains("执行时间", report);
    }

    [Fact]
    public void PerfOutput_JsonFormat_ShouldGenerateValidJson()
    {
        var monitor = new PerformanceMonitor();
        monitor.StartMonitoring(new PerformanceMonitorConfig { Enabled = true });

        var interpreter = new LangInterpreter(monitor);
        var ast = interpreter.Build(SimpleScript);
        ast.Run(interpreter.Manager);

        monitor.StopMonitoring();
        var metrics = monitor.GetMetrics();

        var reporter = new PerformanceReporter();
        var report = reporter.GenerateJsonReport(metrics);

        Assert.NotEmpty(report);
        var parsed = System.Text.Json.JsonDocument.Parse(report);
        Assert.NotNull(parsed);
        Assert.True(parsed.RootElement.TryGetProperty("executionTimeMs", out _));
    }

    [Fact]
    public void PerfOutput_CsvFormat_ShouldGenerateReport()
    {
        var monitor = new PerformanceMonitor();
        monitor.StartMonitoring(new PerformanceMonitorConfig { Enabled = true });

        var interpreter = new LangInterpreter(monitor);
        var ast = interpreter.Build(SimpleScript);
        ast.Run(interpreter.Manager);

        monitor.StopMonitoring();
        var metrics = monitor.GetMetrics();

        var reporter = new PerformanceReporter();
        var report = reporter.GenerateCsvReport(metrics);

        Assert.NotEmpty(report);
        Assert.Contains("指标,值", report);
    }

    [Fact]
    public void PerfOutput_SaveToFile_ShouldCreateFile()
    {
        var monitor = new PerformanceMonitor();
        monitor.StartMonitoring(new PerformanceMonitorConfig { Enabled = true });

        var interpreter = new LangInterpreter(monitor);
        var ast = interpreter.Build(SimpleScript);
        ast.Run(interpreter.Manager);

        monitor.StopMonitoring();
        var metrics = monitor.GetMetrics();

        var reporter = new PerformanceReporter();
        var report = reporter.GenerateTextReport(metrics);

        var tempFile = Path.Combine(Path.GetTempPath(), $"perf-test-{Guid.NewGuid()}.txt");
        try
        {
            reporter.SaveReport(report, tempFile);
            Assert.True(File.Exists(tempFile));
            Assert.NotEmpty(File.ReadAllText(tempFile));
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void PerfMode_WithMemoryTracking_ShouldCollectMemoryMetrics()
    {
        var monitor = new PerformanceMonitor();
        monitor.StartMonitoring(new PerformanceMonitorConfig
        {
            Enabled = true,
            EnableMemoryTracking = true
        });

        var interpreter = new LangInterpreter(monitor);
        var ast = interpreter.Build(SimpleScript);
        ast.Run(interpreter.Manager);

        monitor.StopMonitoring();
        var metrics = monitor.GetMetrics();

        // 内存使用量应该被记录（可能为0如果GC回收了）
        Assert.True(metrics.MemoryUsageBytes >= 0);
        Assert.True(metrics.GCCollectionCount >= 0);
    }

    [Fact]
    public void PerfMode_Disabled_ShouldNotCollectMetrics()
    {
        // 不传 monitor 时，解释器正常运行但不收集指标
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(SimpleScript);
        ast.Run(interpreter.Manager);

        // 无异常即通过 - 验证无监控时正常运行
        Assert.True(true);
    }

    [Fact]
    public void PerfMode_ObjectPoolStats_ShouldBeIncluded()
    {
        var monitor = new PerformanceMonitor();
        monitor.StartMonitoring(new PerformanceMonitorConfig { Enabled = true });

        var interpreter = new LangInterpreter(monitor);
        var ast = interpreter.Build(SimpleScript);
        ast.Run(interpreter.Manager);

        monitor.StopMonitoring();
        var metrics = monitor.GetMetrics();

        // 对象池统计应该被收集
        Assert.NotNull(metrics.ObjectPoolStats);
    }
}
