using Microsoft.Extensions.Logging.Abstractions;
using Old8Lang.LanguageServer.Handlers;
using Old8Lang.LanguageServer.Models;
using Old8Lang.LanguageServer.Services;

namespace Old8Lang.Tests.LanguageServer.Handlers;

/// <summary>
/// DebugProfilerHandler.cs 中全部 8 个自定义 JSON-RPC Handler 的单元测试。
/// </summary>
public class DebugProfilerHandlersTests
{
    private const string Uri = "file:///debug-profile.old8";

    [Fact]
    public async Task ProfilingHandlers_FullLifecycle_StartStatusAndStop()
    {
        var service = new DebugProfilerService();
        var startHandler = new StartProfilingHandler(service, NullLogger<StartProfilingHandler>.Instance);
        var statusHandler = new GetProfilingStatusHandler(service, NullLogger<GetProfilingStatusHandler>.Instance);
        var stopHandler = new StopProfilingHandler(service, NullLogger<StopProfilingHandler>.Instance);

        // 启动前查询状态应为未分析
        var initialStatus = await statusHandler.Handle(new GetProfilingStatusRequest { Uri = Uri }, CancellationToken.None);
        Assert.False(initialStatus.IsProfiling);

        // 启动性能分析会话
        var started = await startHandler.Handle(new StartProfilingRequest
        {
            Uri = Uri,
            SessionName = "UnitTestSession",
            ExecutionMode = "interpreter"
        }, CancellationToken.None);
        Assert.True(started.IsProfiling);
        Assert.Equal("UnitTestSession", started.SessionName);
        Assert.Equal("interpreter", started.ExecutionMode);
        Assert.False(string.IsNullOrWhiteSpace(started.SessionId));

        // 查询运行中状态
        var runningStatus = await statusHandler.Handle(new GetProfilingStatusRequest { Uri = Uri }, CancellationToken.None);
        Assert.True(runningStatus.IsProfiling);
        Assert.Equal("UnitTestSession", runningStatus.SessionName);

        // 停止并生成报告
        var report = await stopHandler.Handle(new StopProfilingRequest { Uri = Uri }, CancellationToken.None);
        Assert.Equal("markdown", report.Format);
        Assert.Contains("性能分析报告", report.Content);
        Assert.NotNull(report.Summary);

        // 停止后再次查询应为未分析；重复停止应抛出异常
        var afterStop = await statusHandler.Handle(new GetProfilingStatusRequest { Uri = Uri }, CancellationToken.None);
        Assert.False(afterStop.IsProfiling);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            stopHandler.Handle(new StopProfilingRequest { Uri = Uri }, CancellationToken.None));
    }

    [Fact]
    public async Task DebuggingHandlers_FullLifecycle_BreakpointsAndControl()
    {
        var service = new DebugProfilerService();
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(Uri, "x <- 1\ny <- x + 2\nPrintLine(y)\n");

        var startHandler = new StartDebuggingHandler(service, documentManager, NullLogger<StartDebuggingHandler>.Instance);
        var stopHandler = new StopDebuggingHandler(service, NullLogger<StopDebuggingHandler>.Instance);
        var addBpHandler = new AddBreakpointHandler(service, NullLogger<AddBreakpointHandler>.Instance);
        var removeBpHandler = new RemoveBreakpointHandler(service, NullLogger<RemoveBreakpointHandler>.Instance);
        var controlHandler = new DebugControlHandler(service, NullLogger<DebugControlHandler>.Instance);

        // 未启动会话时添加/移除断点与控制命令均应安全返回 false
        Assert.False(await addBpHandler.Handle(new BreakpointRequest { Uri = Uri, Line = 2 }, CancellationToken.None));
        Assert.False(await removeBpHandler.Handle(new BreakpointRequest { Uri = Uri, Line = 2 }, CancellationToken.None));
        Assert.False(await controlHandler.Handle(new DebugControlRequest { Uri = Uri, Command = "continue" }, CancellationToken.None));

        // 文档不存在时启动调试应抛出 InvalidOperationException
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            startHandler.Handle(new StartDebuggingRequest { Uri = "file:///missing.old8" }, CancellationToken.None));

        // 正常启动调试会话
        var started = await startHandler.Handle(new StartDebuggingRequest { Uri = Uri }, CancellationToken.None);
        Assert.True(started.IsDebugging);
        Assert.Equal(0, started.BreakpointCount);

        // 添加条件断点与普通断点
        Assert.True(await addBpHandler.Handle(new BreakpointRequest { Uri = Uri, Line = 2, Condition = "x > 0" }, CancellationToken.None));
        Assert.Single(service.GetDebugSession(Uri)!.Debugger.BreakpointManager.GetAllBreakpoints());

        // 移除存在的断点返回 true，移除不存在行返回 false
        Assert.True(await removeBpHandler.Handle(new BreakpointRequest { Uri = Uri, Line = 2 }, CancellationToken.None));
        Assert.False(await removeBpHandler.Handle(new BreakpointRequest { Uri = Uri, Line = 99 }, CancellationToken.None));

        // 执行全部合法控制命令，非法命令返回 false
        foreach (var cmd in new[] { "pause", "continue", "stepInto", "stepOver", "stepOut" })
        {
            Assert.True(await controlHandler.Handle(new DebugControlRequest { Uri = Uri, Command = cmd }, CancellationToken.None));
        }

        Assert.False(await controlHandler.Handle(new DebugControlRequest { Uri = Uri, Command = "unknownCmd" }, CancellationToken.None));

        // 停止调试会话
        var stopped = await stopHandler.Handle(new StopDebuggingRequest { Uri = Uri }, CancellationToken.None);
        Assert.False(stopped.IsDebugging);
        Assert.Equal("Completed", stopped.State);
        Assert.Null(service.GetDebugSession(Uri));
    }
}
