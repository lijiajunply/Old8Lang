using Old8Lang.AST.Statement;
using Old8Lang.Interpreter;
using Old8Lang.LanguageServer.Services;
using Old8Lang.Profiler;
using Xunit.Abstractions;

namespace Old8Lang.Tests.LanguageServer.Services;

public class DebugProfilerServiceTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void TestStartDebugSession()
    {
        // Arrange
        var service = new DebugProfilerService();
        var uri = "file:///test.old8";
        var interpreter = new LangInterpreter();
        var ast = new BlockStatement([]);

        // Act
        var session = service.StartDebugSession(uri, interpreter, ast);

        // Assert
        Assert.NotNull(session);
        Assert.Equal(uri, session.Uri);
        Assert.Equal(interpreter, session.Interpreter);
        Assert.Equal(ast, session.Ast);
        Assert.NotNull(session.Debugger);
        Assert.NotNull(session.DebuggableInterpreter);
        Assert.True(session.StartTime <= DateTime.Now);

        // Verify session is stored
        var retrievedSession = service.GetDebugSession(uri);
        Assert.NotNull(retrievedSession);
        Assert.Same(session, retrievedSession);
    }

    [Fact]
    public void TestStartDebugSession_ReplacesExisting()
    {
        // Arrange
        var service = new DebugProfilerService();
        var uri = "file:///test.old8";
        var interpreter = new LangInterpreter();
        var ast = new BlockStatement([]);

        // Start first session
        var session1 = service.StartDebugSession(uri, interpreter, ast);
        Assert.NotNull(session1);

        var interpreter2 = new LangInterpreter();
        var ast2 = new BlockStatement([]);

        // Act - Start second session with same URI
        var session2 = service.StartDebugSession(uri, interpreter2, ast2);

        // Assert
        Assert.NotNull(session2);
        Assert.Equal(uri, session2.Uri);
        Assert.Equal(interpreter2, session2.Interpreter);
        Assert.Equal(ast2, session2.Ast);

        // Verify only second session is stored
        var retrievedSession = service.GetDebugSession(uri);
        Assert.NotNull(retrievedSession);
        Assert.Same(session2, retrievedSession);
        Assert.NotSame(session1, retrievedSession);
    }

    [Fact]
    public void TestStopDebugSession()
    {
        // Arrange
        var service = new DebugProfilerService();
        var uri = "file:///test.old8";
        var interpreter = new LangInterpreter();
        var ast = new BlockStatement([]);

        var session = service.StartDebugSession(uri, interpreter, ast);
        Assert.NotNull(service.GetDebugSession(uri));

        // Act
        service.StopDebugSession(uri);

        // Assert
        var retrievedSession = service.GetDebugSession(uri);
        Assert.Null(retrievedSession);
    }

    [Fact]
    public void TestStopDebugSession_NotExisting()
    {
        // Arrange
        var service = new DebugProfilerService();
        var uri = "file:///nonexistent.old8";

        // Act - Should not throw
        service.StopDebugSession(uri);

        // Assert
        var session = service.GetDebugSession(uri);
        Assert.Null(session);
    }

    [Fact]
    public void TestGetDebugSession_Existing()
    {
        // Arrange
        var service = new DebugProfilerService();
        var uri = "file:///test.old8";
        var interpreter = new LangInterpreter();
        var ast = new BlockStatement([]);

        var expectedSession = service.StartDebugSession(uri, interpreter, ast);

        // Act
        var actualSession = service.GetDebugSession(uri);

        // Assert
        Assert.NotNull(actualSession);
        Assert.Same(expectedSession, actualSession);
    }

    [Fact]
    public void TestGetDebugSession_NotExisting()
    {
        // Arrange
        var service = new DebugProfilerService();
        var uri = "file:///nonexistent.old8";

        // Act
        var session = service.GetDebugSession(uri);

        // Assert
        Assert.Null(session);
    }

    [Fact]
    public void TestStartProfilingSession()
    {
        // Arrange
        var service = new DebugProfilerService();
        var uri = "file:///test.old8";
        var sessionName = "Test Session";
        var executionMode = "interpreter";

        // Act
        var session = service.StartProfilingSession(uri, sessionName, executionMode);

        // Assert
        Assert.NotNull(session);
        Assert.Equal(uri, session.Uri);
        Assert.Equal(sessionName, session.SessionName);
        Assert.Equal(executionMode, session.ExecutionMode);
        Assert.NotNull(session.ProfilerManager);
        Assert.True(session.StartTime <= DateTime.Now);

        // Verify session is stored
        var retrievedSession = service.GetProfilingSession(uri);
        Assert.NotNull(retrievedSession);
        Assert.Same(session, retrievedSession);
    }

    [Fact]
    public void TestStartProfilingSession_DefaultValues()
    {
        // Arrange
        var service = new DebugProfilerService();
        var uri = "file:///test.old8";

        // Act
        var session = service.StartProfilingSession(uri);

        // Assert
        Assert.NotNull(session);
        Assert.Equal(uri, session.Uri);
        Assert.Equal("", session.SessionName); // Default empty string
        Assert.Equal("interpreter", session.ExecutionMode); // Default mode
    }

    [Fact]
    public void TestStartProfilingSession_ReplacesExisting()
    {
        // Arrange
        var service = new DebugProfilerService();
        var uri = "file:///test.old8";

        // Start first session
        var session1 = service.StartProfilingSession(uri, "Session1", "interpreter");
        Assert.NotNull(session1);

        // Act - Start second session with same URI
        var session2 = service.StartProfilingSession(uri, "Session2", "compiler");

        // Assert
        Assert.NotNull(session2);
        Assert.Equal("Session2", session2.SessionName);
        Assert.Equal("compiler", session2.ExecutionMode);

        // Verify only second session is stored
        var retrievedSession = service.GetProfilingSession(uri);
        Assert.NotNull(retrievedSession);
        Assert.Same(session2, retrievedSession);
        Assert.NotSame(session1, retrievedSession);
    }

    [Fact]
    public void TestStopProfilingSession()
    {
        // Arrange
        var service = new DebugProfilerService();
        var uri = "file:///test.old8";

        var session = service.StartProfilingSession(uri, "Test Session", "interpreter");
        Assert.NotNull(service.GetProfilingSession(uri));

        // Act
        var summary = service.StopProfilingSession(uri);

        // Assert
        Assert.NotNull(summary);

        // Verify session is removed
        var retrievedSession = service.GetProfilingSession(uri);
        Assert.Null(retrievedSession);
    }

    [Fact]
    public void TestStopProfilingSession_NotExisting()
    {
        // Arrange
        var service = new DebugProfilerService();
        var uri = "file:///nonexistent.old8";

        // Act
        var summary = service.StopProfilingSession(uri);

        // Assert
        Assert.Null(summary);
    }

    [Fact]
    public void TestGetProfilingSession_Existing()
    {
        // Arrange
        var service = new DebugProfilerService();
        var uri = "file:///test.old8";
        var expectedSession = service.StartProfilingSession(uri, "Test Session", "interpreter");

        // Act
        var actualSession = service.GetProfilingSession(uri);

        // Assert
        Assert.NotNull(actualSession);
        Assert.Same(expectedSession, actualSession);
    }

    [Fact]
    public void TestGetProfilingSession_NotExisting()
    {
        // Arrange
        var service = new DebugProfilerService();
        var uri = "file:///nonexistent.old8";

        // Act
        var session = service.GetProfilingSession(uri);

        // Assert
        Assert.Null(session);
    }

    [Fact]
    public void TestGeneratePerformanceReport_Existing()
    {
        // Arrange
        var service = new DebugProfilerService();
        var uri = "file:///test.old8";

        var session = service.StartProfilingSession(uri, "Test Session", "interpreter");
        Assert.NotNull(service.GetProfilingSession(uri));

        // Act
        var report = service.GeneratePerformanceReport(uri, ReportFormat.Markdown);

        // Assert
        Assert.NotNull(report);

        // Verify session still exists after report generation
        var retrievedSession = service.GetProfilingSession(uri);
        Assert.NotNull(retrievedSession);
        Assert.Same(session, retrievedSession);
    }

    [Fact]
    public void TestGeneratePerformanceReport_NotExisting()
    {
        // Arrange
        var service = new DebugProfilerService();
        var uri = "file:///nonexistent.old8";

        // Act
        var report = service.GeneratePerformanceReport(uri);

        // Assert
        Assert.Null(report);
    }

    [Fact]
    public void TestClearAllSessions()
    {
        // Arrange
        var service = new DebugProfilerService();

        // Start multiple sessions
        var debugUri1 = "file:///debug1.old8";
        var debugUri2 = "file:///debug2.old8";
        var profileUri1 = "file:///profile1.old8";
        var profileUri2 = "file:///profile2.old8";

        var interpreter1 = new LangInterpreter();
        var interpreter2 = new LangInterpreter();
        var ast = new BlockStatement([]);

        service.StartDebugSession(debugUri1, interpreter1, ast);
        service.StartDebugSession(debugUri2, interpreter2, ast);
        service.StartProfilingSession(profileUri1, "Profile1", "interpreter");
        service.StartProfilingSession(profileUri2, "Profile2", "compiler");

        // Verify all sessions exist
        Assert.NotNull(service.GetDebugSession(debugUri1));
        Assert.NotNull(service.GetDebugSession(debugUri2));
        Assert.NotNull(service.GetProfilingSession(profileUri1));
        Assert.NotNull(service.GetProfilingSession(profileUri2));

        // Act
        service.ClearAllSessions();

        // Assert
        Assert.Null(service.GetDebugSession(debugUri1));
        Assert.Null(service.GetDebugSession(debugUri2));
        Assert.Null(service.GetProfilingSession(profileUri1));
        Assert.Null(service.GetProfilingSession(profileUri2));
    }

    [Fact]
    public void TestMultipleIndependentSessions()
    {
        // Arrange
        var service = new DebugProfilerService();

        var debugUri = "file:///debug.old8";
        var profileUri = "file:///profile.old8";

        var interpreter = new LangInterpreter();
        var ast = new BlockStatement([]);

        // Act - Start both debug and profiling sessions for different URIs
        var debugSession = service.StartDebugSession(debugUri, interpreter, ast);
        var profileSession = service.StartProfilingSession(profileUri, "Profile", "interpreter");

        // Assert
        Assert.NotNull(debugSession);
        Assert.NotNull(profileSession);

        // Verify both sessions exist independently
        var retrievedDebug = service.GetDebugSession(debugUri);
        var retrievedProfile = service.GetProfilingSession(profileUri);

        Assert.Same(debugSession, retrievedDebug);
        Assert.Same(profileSession, retrievedProfile);

        // Verify no cross-contamination
        Assert.Null(service.GetDebugSession(profileUri));
        Assert.Null(service.GetProfilingSession(debugUri));
    }
}