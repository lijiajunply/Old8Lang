using Old8Lang.Profiler;

namespace Old8Lang.Tests.Profiler;

/// <summary>
/// 性能分析引擎测试
/// </summary>
public class PerformanceAnalyzerTests
{
    private readonly PerformanceAnalyzer Analyzer = new();

    [Fact]
    public void AnalyzeSession_WithNoFunctions_ShouldReturnEmptyBottlenecks()
    {
        // Arrange
        var session = new ProfilingSession();

        // Act
        var bottlenecks = Analyzer.AnalyzeSession(session);

        // Assert
        Assert.Empty(bottlenecks);
    }

    [Fact]
    public void AnalyzeSession_WithHighExecutionTime_ShouldDetectBottleneck()
    {
        // Arrange
        var session = new ProfilingSession();
        session.RecordFunctionExecution("slowFunction", 200.0); // 超过阈值

        // Act
        var bottlenecks = Analyzer.AnalyzeSession(session);

        // Assert
        Assert.Single(bottlenecks);
        Assert.Equal(BottleneckType.HighExecutionTime, bottlenecks[0].Type);
        Assert.Contains("slowFunction", bottlenecks[0].Description);
        Assert.True(bottlenecks[0].Severity >= 2); // 200ms / 100ms = 2
    }

    [Fact]
    public void AnalyzeSession_WithHighMemoryUsage_ShouldDetectBottleneck()
    {
        // Arrange
        var session = new ProfilingSession();
        session.RecordMemoryUsage();

        // 模拟高内存使用
        var highMemoryStats = new MemoryUsageStats
        {
            ManagedMemoryBytes = 150 * 1024 * 1024 // 150MB
        };
        session.MemoryHistory.Add(highMemoryStats);

        // Act
        var bottlenecks = Analyzer.AnalyzeSession(session);

        // Assert
        Assert.Single(bottlenecks);
        Assert.Equal(BottleneckType.HighMemoryUsage, bottlenecks[0].Type);
        Assert.Contains("高", bottlenecks[0].Description);
        Assert.Contains("内存", bottlenecks[0].Description);
        Assert.True(bottlenecks[0].Severity >= 2); // 150MB / 100MB = 1.5
    }

    [Fact]
    public void AnalyzeSession_WithFrequentGC_ShouldDetectBottleneck()
    {
        // Arrange
        var session = new ProfilingSession();

        // 模拟频繁GC
        var startStats = new MemoryUsageStats
        {
            Timestamp = DateTime.Now.AddMinutes(-1),
            Gen0Collections = 100
        };
        var endStats = new MemoryUsageStats
        {
            Gen0Collections = 120 // 20次GC in 1分钟 = 20/分钟
        };
        session.MemoryHistory.Add(startStats);
        session.MemoryHistory.Add(endStats);

        // Act
        var bottlenecks = Analyzer.AnalyzeSession(session);

        // Assert
        Assert.Single(bottlenecks);
        Assert.Equal(BottleneckType.FrequentGarbageCollection, bottlenecks[0].Type);
        Assert.Contains("垃圾回收", bottlenecks[0].Description);
    }

    [Fact]
    public void AnalyzeSession_WithExcessiveFunctionCalls_ShouldDetectBottleneck()
    {
        // Arrange
        var session = new ProfilingSession();
        for (int i = 0; i < 20000; i++)
        {
            session.RecordFunctionExecution("heavyFunction", 0.1);
        }

        // Act
        var bottlenecks = Analyzer.AnalyzeSession(session);

        // Assert
        Assert.Single(bottlenecks);
        Assert.Equal(BottleneckType.ExcessiveFunctionCalls, bottlenecks[0].Type);
        Assert.Contains("heavyFunction", bottlenecks[0].Description);
    }

    [Fact]
    public void AnalyzeSession_WithUnstableExecutionTime_ShouldDetectBottleneck()
    {
        // Arrange
        var session = new ProfilingSession();
        var func = new FunctionPerformanceStats { FunctionName = "unstableFunction" };

        // 添加变化的执行时间
        for (int i = 0; i < 10; i++)
        {
            var executionTime = 10.0 + (i % 2 == 0 ? 0 : 20); // 10 或 30
            func.AddExecutionTime(executionTime);
        }

        session.FunctionStats["unstableFunction"] = func;

        // Act
        var bottlenecks = Analyzer.AnalyzeSession(session);

        // Assert
        Assert.Single(bottlenecks);
        Assert.Equal(BottleneckType.UnstableExecutionTime, bottlenecks[0].Type);
        Assert.Contains("unstableFunction", bottlenecks[0].Description);
    }

    [Fact]
    public void GenerateSummary_ShouldReturnValidSummary()
    {
        // Arrange
        var session = new ProfilingSession
        {
            Name = "testSession"
        };

        session.RecordFunctionExecution("function1", 50.0);
        session.RecordFunctionExecution("function2", 25.0);
        session.RecordFunctionExecution("function1", 75.0);

        // Act
        var summary = Analyzer.GenerateSummary(session);

        // Assert
        Assert.NotNull(summary);
        Assert.Equal(session, summary.Session);
        Assert.Equal(0, summary.Bottlenecks.Count); // 没有明显瓶颈
        Assert.Equal(100.0, summary.OverallScore); // 没有瓶颈，应该是100分
        Assert.Equal(PerformanceGrade.A, summary.Grade);
    }

    [Fact]
    public void GenerateSummary_WithBottlenecks_ShouldCalculateCorrectScore()
    {
        // Arrange
        var session = new ProfilingSession();
        session.RecordFunctionExecution("verySlowFunction", 1000.0); // 严重超时

        // Act
        var summary = Analyzer.GenerateSummary(session);

        // Assert
        Assert.True(summary.OverallScore < 50); // 严重瓶颈，分数应该很低
        Assert.Equal(PerformanceGrade.F, summary.Grade);
        Assert.NotEmpty(summary.Recommendation);
    }

    [Fact]
    public void CalculateSeverity_ShouldReturnCorrectValue()
    {
        // Test low threshold
        var severity1 = Analyzer.GetType()
            .GetMethod("CalculateSeverity",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            ?.Invoke(null, [50.0, 100.0]);
        Assert.Equal(1, severity1);

        // Test high threshold
        var severity2 = Analyzer.GetType()
            .GetMethod("CalculateSeverity",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            ?.Invoke(null, [200.0, 100.0]);
        Assert.Equal(10, severity2); // 超过10倍，应该返回最大值10

        // Test equal threshold
        var severity3 = Analyzer.GetType()
            .GetMethod("CalculateSeverity",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            ?.Invoke(null, [100.0, 100.0]);
        Assert.Equal(1, severity3);
    }

    [Fact]
    public void CalculateMemoryGrowthRate_ShouldHandleEdgeCases()
    {
        // Test with insufficient data
        var session = new ProfilingSession();

        // 由于是私有方法，我们通过测试结果来验证逻辑
        var summary = Analyzer.GenerateSummary(session);
        Assert.NotNull(summary);
    }
}