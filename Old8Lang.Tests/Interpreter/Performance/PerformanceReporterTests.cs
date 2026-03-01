using System;
using System.IO;
using Old8Lang.Interpreter;
using Xunit;

namespace Old8Lang.Tests.Interpreter.Performance;

/// <summary>
/// 性能报告生成器测试
/// </summary>
public class PerformanceReporterTests
{
    private static PerformanceMetrics CreateSampleMetrics()
    {
        return new PerformanceMetrics
        {
            StartTime = DateTime.Now.AddSeconds(-5),
            EndTime = DateTime.Now,
            ExecutionTimeMs = 5000,
            MemoryUsageBytes = 1024 * 1024,
            GCCollectionCount = 2,
            FunctionCallCount = 100,
            VariableLookupCount = 500,
            LoopIterationCount = 1000,
            ObjectAllocationCount = 200,
            CacheHitRate = 0.85,
            ObjectPoolStats =
            [
                new ObjectPoolStats
                {
                    PoolName = "IntPool",
                    ObjectType = "IntLangValue",
                    PoolSize = 100,
                    ActiveCount = 10,
                    AvailableCount = 90,
                    TotalAllocations = 500,
                    TotalReturns = 490
                }
            ],
            FunctionMetrics =
            [
                new FunctionMetrics
                {
                    FunctionName = "testFunc",
                    CallCount = 50,
                    TotalTimeMs = 250,
                    MinTimeMs = 3,
                    MaxTimeMs = 15
                }
            ]
        };
    }

    [Fact]
    public void GenerateTextReport_ShouldContainKeyMetrics()
    {
        var reporter = new PerformanceReporter();
        var metrics = CreateSampleMetrics();

        var report = reporter.GenerateTextReport(metrics);

        Assert.Contains("5000", report);
        Assert.Contains("函数调用次数", report);
        Assert.Contains("缓存命中率", report);
        Assert.NotEmpty(report);
    }

    [Fact]
    public void GenerateJsonReport_ShouldBeValidJson()
    {
        var reporter = new PerformanceReporter();
        var metrics = CreateSampleMetrics();

        var report = reporter.GenerateJsonReport(metrics);

        Assert.NotEmpty(report);
        Assert.Contains("executionTimeMs", report);
        Assert.Contains("functionCallCount", report);
        Assert.Contains("cacheHitRate", report);

        // 验证是有效的 JSON
        var parsed = System.Text.Json.JsonDocument.Parse(report);
        Assert.NotNull(parsed);
    }

    [Fact]
    public void GenerateCsvReport_ShouldContainHeaders()
    {
        var reporter = new PerformanceReporter();
        var metrics = CreateSampleMetrics();

        var report = reporter.GenerateCsvReport(metrics);

        Assert.Contains("指标,值", report);
        Assert.Contains("执行时间(ms)", report);
        Assert.Contains("5000", report);
        Assert.NotEmpty(report);
    }

    [Fact]
    public void SaveReport_ShouldCreateFile()
    {
        var reporter = new PerformanceReporter();
        var metrics = CreateSampleMetrics();
        var report = reporter.GenerateTextReport(metrics);

        var tempFile = Path.GetTempFileName();
        try
        {
            reporter.SaveReport(report, tempFile);
            Assert.True(File.Exists(tempFile));
            var content = File.ReadAllText(tempFile);
            Assert.Equal(report, content);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void SaveReport_ShouldCreateDirectory()
    {
        var reporter = new PerformanceReporter();
        var metrics = CreateSampleMetrics();
        var report = reporter.GenerateTextReport(metrics);

        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var tempFile = Path.Combine(tempDir, "report.txt");
        try
        {
            reporter.SaveReport(report, tempFile);
            Assert.True(File.Exists(tempFile));
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void GenerateJsonReport_WithFunctionMetrics_ShouldIncludeDetails()
    {
        var reporter = new PerformanceReporter();
        var metrics = CreateSampleMetrics();

        var report = reporter.GenerateJsonReport(metrics);

        Assert.Contains("testFunc", report);
        Assert.Contains("callCount", report);
    }

    [Fact]
    public void GenerateTextReport_WithObjectPoolStats_ShouldIncludePoolInfo()
    {
        var reporter = new PerformanceReporter();
        var metrics = CreateSampleMetrics();

        var report = reporter.GenerateTextReport(metrics);

        Assert.Contains("IntPool", report);
        Assert.Contains("对象池统计", report);
    }
}
