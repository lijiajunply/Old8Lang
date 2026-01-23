using Old8Lang.Profiler;

namespace Old8Lang.Tests.Profiler;

/// <summary>
/// ProfilerManager 测试
/// </summary>
public class ProfilerManagerTests
{
    private readonly ProfilerManager _profiler = new();

    [Fact]
    public void StartProfiling_ShouldCreateSession()
    {
        // Act
        var sessionId = _profiler.StartProfiling("test-session", "test.old8", "解释模式");

        // Assert
        Assert.NotEqual(Guid.Empty, sessionId);
        Assert.True(_profiler.IsProfiling);
        Assert.NotNull(_profiler.CurrentSession);
        Assert.Equal("test-session", _profiler.CurrentSession.Name);
        Assert.Equal("test.old8", _profiler.CurrentSession.SourceFilePath);
        Assert.Equal("解释模式", _profiler.CurrentSession.ExecutionMode);
    }

    [Fact]
    public void StartProfiling_WhenAlreadyProfiling_ShouldThrowException()
    {
        // Arrange
        _profiler.StartProfiling("test", "test.old8");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _profiler.StartProfiling("test2", "test2.old8"));
    }

    [Fact]
    public void StopProfiling_ShouldReturnSummary()
    {
        // Arrange
        _profiler.StartProfiling("test-session", "test.old8", "解释模式");
        _profiler.RecordCustomData("test", 100.0);
        
        // Act
        var summary = _profiler.StopProfiling();

        // Assert
        Assert.NotNull(summary);
        Assert.Equal("test-session", summary.Session.Name);
        Assert.True(summary.OverallScore > 0);
        Assert.False(_profiler.IsProfiling);
        Assert.Null(_profiler.CurrentSession);
    }

    [Fact]
    public void StopProfiling_WhenNotProfiling_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _profiler.StopProfiling());
    }

    [Fact]
    public void RecordFunctionStartAndEnd_ShouldTrackExecution()
    {
        // Arrange
        _profiler.StartProfiling("test", "test.old8", "解释模式");
        
        // Act
        _profiler.RecordFunctionStart("testFunction", "test.old8", 10);
        _profiler.RecordFunctionEnd("testFunction", "test.old8", 10);
        
        var summary = _profiler.StopProfiling();

        // Assert
        Assert.True(summary.Session.FunctionStats.ContainsKey("testFunction"));
        var stats = summary.Session.FunctionStats["testFunction"];
        Assert.Equal(1, stats.CallCount);
        Assert.True(stats.TotalExecutionTimeMs > 0);
    }

    [Fact]
    public void GetSessionStatus_ShouldReturnCorrectInfo()
    {
        // Arrange
        _profiler.StartProfiling("test", "test.old8", "解释模式");
        
        // Act
        var status = _profiler.GetSessionStatus();

        // Assert
        Assert.True((bool)status["isProfiling"]);
        Assert.True((bool)status["hasSession"]);
        Assert.NotNull(status["sessionId"]);
        Assert.NotNull(status["sessionName"]);
        Assert.NotNull(status["durationMs"]);
    }

    [Fact]
    public void GetSessionStatus_WhenNotProfiling_ShouldReturnCorrectInfo()
    {
        // Act
        var status = _profiler.GetSessionStatus();

        // Assert
        Assert.False((bool)status["isProfiling"]);
        Assert.False((bool)status["hasSession"]);
    }

    [Fact]
    public void ClearSession_ShouldResetState()
    {
        // Arrange
        _profiler.StartProfiling("test", "test.old8", "解释模式");
        Assert.True(_profiler.IsProfiling);
        
        // Act
        _profiler.ClearSession();

        // Assert
        Assert.False(_profiler.IsProfiling);
        Assert.Null(_profiler.CurrentSession);
    }
}