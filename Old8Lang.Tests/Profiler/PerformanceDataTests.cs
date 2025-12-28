using Xunit;
using Old8Lang.Profiler;

namespace Old8Lang.Tests.Profiler;

/// <summary>
/// PerformanceData 测试
/// </summary>
public class PerformanceDataTests
{
    [Fact]
    public void PerformanceDataPoint_Constructor_ShouldInitializeCorrectly()
    {
        // Act
        var dataPoint = new PerformanceDataPoint
        {
            Name = "testData",
            Type = PerformanceCounterType.ExecutionTime,
            Value = 100.5,
            Unit = "ms"
        };

        // Assert
        Assert.Equal("testData", dataPoint.Name);
        Assert.Equal(PerformanceCounterType.ExecutionTime, dataPoint.Type);
        Assert.Equal(100.5, dataPoint.Value);
        Assert.Equal("ms", dataPoint.Unit);
        Assert.True(dataPoint.Timestamp <= DateTime.Now);
    }

    [Fact]
    public void PerformanceDataPoint_ToString_ShouldReturnFormattedString()
    {
        // Arrange
        var dataPoint = new PerformanceDataPoint
        {
            Name = "testData",
            Type = PerformanceCounterType.ExecutionTime,
            Value = 100.5,
            Unit = "ms"
        };

        // Act
        var result = dataPoint.ToString();

        // Assert
        Assert.Equal("testData: 100.50 ms (ExecutionTime)", result);
    }

    [Fact]
    public void FunctionPerformanceStats_Constructor_ShouldInitializeCorrectly()
    {
        // Act
        var stats = new FunctionPerformanceStats
        {
            FunctionName = "testFunction",
            SourceFile = "test.old8",
            LineNumber = 10
        };

        // Assert
        Assert.Equal("testFunction", stats.FunctionName);
        Assert.Equal("test.old8", stats.SourceFile);
        Assert.Equal(10, stats.LineNumber);
        Assert.Equal(0, stats.CallCount);
        Assert.Equal(0.0, stats.TotalExecutionTimeMs);
        Assert.Equal(0.0, stats.AverageExecutionTimeMs);
        Assert.Equal(double.MaxValue, stats.MinExecutionTimeMs);
        Assert.Equal(0.0, stats.MaxExecutionTimeMs);
        Assert.Empty(stats.ExecutionTimes);
    }

    [Fact]
    public void FunctionPerformanceStats_AddExecutionTime_ShouldUpdateStats()
    {
        // Arrange
        var stats = new FunctionPerformanceStats
        {
            FunctionName = "testFunction"
        };

        // Act
        stats.AddExecutionTime(10.0);
        stats.AddExecutionTime(20.0);
        stats.AddExecutionTime(15.0);

        // Assert
        Assert.Equal(3, stats.CallCount);
        Assert.Equal(45.0, stats.TotalExecutionTimeMs);
        Assert.Equal(15.0, stats.AverageExecutionTimeMs);
        Assert.Equal(10.0, stats.MinExecutionTimeMs);
        Assert.Equal(20.0, stats.MaxExecutionTimeMs);
        Assert.Equal(3, stats.ExecutionTimes.Count);
        Assert.Contains(10.0, stats.ExecutionTimes);
        Assert.Contains(20.0, stats.ExecutionTimes);
        Assert.Contains(15.0, stats.ExecutionTimes);
    }

    [Fact]
    public void FunctionPerformanceStats_GetMedian_ShouldReturnCorrectValue()
    {
        // Arrange
        var stats = new FunctionPerformanceStats
        {
            FunctionName = "testFunction"
        };

        // Act
        var times = new[] { 10, 20, 30, 40, 50 };
        foreach (var time in times)
        {
            stats.AddExecutionTime(time);
        }
        var median = stats.GetMedian();

        // Assert
        Assert.Equal(30.0, median); // 中间值
    }

    [Fact]
    public void FunctionPerformanceStats_GetStandardDeviation_ShouldReturnCorrectValue()
    {
        // Arrange
        var stats = new FunctionPerformanceStats
        {
            FunctionName = "testFunction"
        };

        // Act
        var times = new[] { 10, 20, 30, 40, 50 };
        foreach (var time in times)
        {
            stats.AddExecutionTime(time);
        }
        var stdDev = stats.GetStandardDeviation();

        // Assert
        Assert.True(stdDev > 14 && stdDev < 15); // 标准差约为 14.14
    }

    [Fact]
    public void MemoryUsageStats_Constructor_ShouldInitializeCorrectly()
    {
        // Act
        var stats = new MemoryUsageStats();

        // Assert
        Assert.True(stats.Timestamp <= DateTime.Now);
        Assert.Equal(0, stats.ManagedMemoryBytes);
        Assert.Equal(0, stats.UnmanagedMemoryBytes);
        Assert.Equal(0, stats.Gen0Collections);
        Assert.Equal(0, stats.Gen1Collections);
        Assert.Equal(0, stats.Gen2Collections);
        Assert.Equal(0, stats.TotalGcCollections);
        Assert.Equal(0.0, stats.ManagedMemoryMB);
        Assert.Equal(0.0, stats.UnmanagedMemoryMB);
        Assert.Equal(0, stats.WorkingSetBytes);
        Assert.Equal(0, stats.PrivateMemoryBytes);
        Assert.Equal(0.0, stats.WorkingSetMB);
        Assert.Equal(0.0, stats.PrivateMemoryMB);
    }

    [Fact]
    public void MemoryUsageStats_ToString_ShouldReturnFormattedString()
    {
        // Act
        var stats = new MemoryUsageStats
        {
            ManagedMemoryBytes = 1024 * 1024, // 1MB
            UnmanagedMemoryBytes = 512 * 1024, // 0.5MB
            Gen0Collections = 5,
            Gen1Collections = 2,
            Gen2Collections = 1
        };

        var result = stats.ToString();

        // Assert
        Assert.Contains("1.00MB", result);
        Assert.Contains("0.50MB", result);
        Assert.Contains("GC", result);
        Assert.Contains("8", result);
    }

    [Fact]
    public void ProfilingSession_Constructor_ShouldInitializeCorrectly()
    {
        // Act
        var session = new ProfilingSession
        {
            Name = "testSession",
            SourceFilePath = "test.old8",
            ExecutionMode = "解释模式"
        };

        // Assert
        Assert.Equal("testSession", session.Name);
        Assert.Equal("test.old8", session.SourceFilePath);
        Assert.Equal("解释模式", session.ExecutionMode);
        Assert.True(session.IsActive);
        Assert.True(session.StartTime <= DateTime.Now);
        Assert.False(session.EndTime.HasValue);
        Assert.Empty(session.FunctionStats);
        Assert.Empty(session.MemoryHistory);
        Assert.Empty(session.DataPoints);
        Assert.Empty(session.Tags);
    }

    [Fact]
    public void ProfilingSession_EndSession_ShouldSetEndTime()
    {
        // Arrange
        var session = new ProfilingSession();
        var startTime = session.StartTime;

        // Act
        Assert.True(session.IsActive);
        session.EndSession();

        // Assert
        Assert.False(session.IsActive);
        Assert.True(session.EndTime.HasValue);
        Assert.True(session.EndTime.Value >= startTime);
    }

    [Fact]
    public void ProfilingSession_DurationMs_ShouldCalculateCorrectly()
    {
        // Arrange
        var session = new ProfilingSession();
        
        // Simulate some execution time
        session.EndTime = session.StartTime.AddMilliseconds(1000);

        // Act & Assert
        Assert.Equal(1000.0, session.DurationMs);
    }

    [Fact]
    public void ProfilingSession_RecordFunctionExecution_ShouldAddToStats()
    {
        // Arrange
        var session = new ProfilingSession();

        // Act
        session.RecordFunctionExecution("func1", 10.0, "test.old8", 10);
        session.RecordFunctionExecution("func1", 15.0, "test.old8", 10);
        session.RecordFunctionExecution("func2", 20.0, "test.old8", 20);

        // Assert
        Assert.Equal(2, session.FunctionStats.Count);
        Assert.True(session.FunctionStats.ContainsKey("func1"));
        Assert.True(session.FunctionStats.ContainsKey("func2"));
        
        var func1Stats = session.FunctionStats["func1"];
        Assert.Equal(2, func1Stats.CallCount);
        Assert.Equal(25.0, func1Stats.TotalExecutionTimeMs);
        Assert.Equal(12.5, func1Stats.AverageExecutionTimeMs);
        
        var func2Stats = session.FunctionStats["func2"];
        Assert.Equal(1, func2Stats.CallCount);
        Assert.Equal(20.0, func2Stats.TotalExecutionTimeMs);
        Assert.Equal(20.0, func2Stats.AverageExecutionTimeMs);
    }

    [Fact]
    public void ProfilingSession_GetHotspotFunctions_ShouldReturnSortedByTotalTime()
    {
        // Arrange
        var session = new ProfilingSession();
        session.RecordFunctionExecution("slowFunc", 100.0);
        session.RecordFunctionExecution("mediumFunc", 50.0);
        session.RecordFunctionExecution("fastFunc", 10.0);

        // Act
        var hotspots = session.GetHotspotFunctions(3);

        // Assert
        Assert.Equal(3, hotspots.Count);
        Assert.Equal("slowFunc", hotspots[0].FunctionName);
        Assert.Equal("mediumFunc", hotspots[1].FunctionName);
        Assert.Equal("fastFunc", hotspots[2].FunctionName);
    }

    [Fact]
    public void ProfilingSession_GetMostFrequentFunctions_ShouldReturnSortedByCallCount()
    {
        // Arrange
        var session = new ProfilingSession();
        session.RecordFunctionExecution("frequent", 1.0);
        session.RecordFunctionExecution("frequent", 1.0);
        session.RecordFunctionExecution("frequent", 1.0);
        session.RecordFunctionExecution("rare", 5.0);

        // Act
        var frequent = session.GetMostFrequentFunctions(2);

        // Assert
        Assert.Equal(2, frequent.Count);
        Assert.Equal("frequent", frequent[0].FunctionName);
        Assert.Equal(3, frequent[0].CallCount);
        Assert.Equal("rare", frequent[1].FunctionName);
        Assert.Equal(1, frequent[1].CallCount);
    }

    [Fact]
    public void ProfilingSession_GetSlowestFunctions_ShouldReturnSortedByAvgTime()
    {
        // Arrange
        var session = new ProfilingSession();
        session.RecordFunctionExecution("fast", 5.0);
        session.RecordFunctionExecution("fast", 10.0);
        session.RecordFunctionExecution("slow", 50.0);
        session.RecordFunctionExecution("slow", 30.0);

        // Act
        var slowest = session.GetSlowestFunctions(2);

        // Assert
        Assert.Equal(2, slowest.Count);
        Assert.Equal("slow", slowest[0].FunctionName);
        Assert.Equal(40.0, slowest[0].AverageExecutionTimeMs);
        Assert.Equal("fast", slowest[1].FunctionName);
        Assert.Equal(7.5, slowest[1].AverageExecutionTimeMs);
    }
}