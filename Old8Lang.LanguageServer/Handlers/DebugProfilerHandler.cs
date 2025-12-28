using MediatR;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.JsonRpc;
using Old8Lang.LanguageServer.Models;
using Old8Lang.LanguageServer.Services;
using Old8Lang.Profiler;
using Old8Lang.Debugger;

namespace Old8Lang.LanguageServer.Handlers;

/// <summary>
/// 启动性能分析命令
/// </summary>
[Method("old8lang/startProfiling")]
public interface IStartProfilingHandler : IJsonRpcRequestHandler<StartProfilingRequest, ProfilerSessionStatusResponse> { }

public class StartProfilingHandler : IStartProfilingHandler
{
    private readonly DebugProfilerService _service;
    private readonly ILogger<StartProfilingHandler> _logger;

    public StartProfilingHandler(DebugProfilerService service, ILogger<StartProfilingHandler> logger)
    {
        _service = service;
        _logger = logger;
    }

    public Task<ProfilerSessionStatusResponse> Handle(StartProfilingRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var session = _service.StartProfilingSession(
                request.Uri,
                request.SessionName ?? "",
                request.ExecutionMode
            );

            _logger.LogInformation("Started profiling session for {Uri}", request.Uri);

            return Task.FromResult(new ProfilerSessionStatusResponse
            {
                IsProfiling = true,
                SessionId = session.SessionId.ToString(),
                SessionName = session.SessionName,
                ExecutionMode = session.ExecutionMode,
                StartTime = session.StartTime,
                DurationMs = 0,
                FunctionCallCount = 0,
                DataPointCount = 0
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start profiling session");
            throw;
        }
    }
}

/// <summary>
/// 停止性能分析命令
/// </summary>
[Method("old8lang/stopProfiling")]
public interface IStopProfilingHandler : IJsonRpcRequestHandler<StopProfilingRequest, PerformanceReportResponse> { }

public class StopProfilingHandler : IStopProfilingHandler
{
    private readonly DebugProfilerService _service;
    private readonly ILogger<StopProfilingHandler> _logger;

    public StopProfilingHandler(DebugProfilerService service, ILogger<StopProfilingHandler> logger)
    {
        _service = service;
        _logger = logger;
    }

    public Task<PerformanceReportResponse> Handle(StopProfilingRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var summary = _service.StopProfilingSession(request.Uri);

            if (summary == null)
            {
                throw new InvalidOperationException($"No profiling session found for {request.Uri}");
            }

            var report = GenerateMarkdownReport(summary);

            _logger.LogInformation("Stopped profiling session for {Uri}", request.Uri);

            // 从 Session 中提取信息
            var session = summary.Session;
            var hottestFunctions = session.GetHotspotFunctions(5);
            var totalCalls = session.FunctionStats.Values.Sum(f => f.CallCount);
            var peakMemory = session.MemoryHistory.Count > 0
                ? session.MemoryHistory.Max(m => m.ManagedMemoryMB)
                : 0;

            // 获取解析和编译时间
            var parsingTime = session.DataPoints
                .FirstOrDefault(p => p.Type == PerformanceCounterType.ParsingTime)?.Value ?? 0;
            var compilationTime = session.DataPoints
                .FirstOrDefault(p => p.Type == PerformanceCounterType.CompilationTime)?.Value ?? 0;

            return Task.FromResult(new PerformanceReportResponse
            {
                Format = "markdown",
                Content = report,
                GeneratedAt = DateTime.Now,
                Summary = new SessionSummaryInfo
                {
                    TotalExecutionTimeMs = session.DurationMs,
                    TotalFunctionCalls = (int)totalCalls,
                    ParsingTimeMs = parsingTime,
                    CompilationTimeMs = compilationTime,
                    PeakMemoryMb = peakMemory,
                    HotFunctions = hottestFunctions.Select(f => new HotFunctionInfo
                    {
                        Name = f.FunctionName,
                        CallCount = (int)f.CallCount,
                        TotalTimeMs = f.TotalExecutionTimeMs,
                        AverageTimeMs = f.AverageExecutionTimeMs
                    }).ToList()
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop profiling session");
            throw;
        }
    }

    private static string GenerateMarkdownReport(PerformanceSummary summary)
    {
        var session = summary.Session;
        var hottestFunctions = session.GetHotspotFunctions(5);
        var totalCalls = session.FunctionStats.Values.Sum(f => f.CallCount);
        var peakMemory = session.MemoryHistory.Count > 0
            ? session.MemoryHistory.Max(m => m.ManagedMemoryMB)
            : 0;

        var parsingTime = session.DataPoints
            .FirstOrDefault(p => p.Type == PerformanceCounterType.ParsingTime)?.Value ?? 0;
        var compilationTime = session.DataPoints
            .FirstOrDefault(p => p.Type == PerformanceCounterType.CompilationTime)?.Value ?? 0;

        return $@"# 性能分析报告

## 会话信息
- **会话名称**: {session.Name}
- **执行模式**: {session.ExecutionMode ?? "未知"}
- **总执行时间**: {session.DurationMs:F2} ms
- **函数调用次数**: {totalCalls}
- **峰值内存**: {peakMemory:F2} MB
- **性能评分**: {summary.FormattedScore}

## 最热函数 (Top 5)
{string.Join("\n", hottestFunctions.Select((f, i) =>
    $"{i + 1}. **{f.FunctionName}** - 调用 {f.CallCount} 次，总耗时 {f.TotalExecutionTimeMs:F2} ms，平均 {f.AverageExecutionTimeMs:F4} ms"))}

## 时间分布
- **解析时间**: {parsingTime:F2} ms
- **编译时间**: {compilationTime:F2} ms

## 性能瓶颈
{string.Join("\n", summary.Bottlenecks.Take(3).Select((b, i) => $"{i + 1}. {b.Description}"))}

## 建议
{summary.Recommendation}
";
    }
}

/// <summary>
/// 获取性能分析状态命令
/// </summary>
[Method("old8lang/getProfilingStatus")]
public interface IGetProfilingStatusHandler : IJsonRpcRequestHandler<GetProfilingStatusRequest, ProfilerSessionStatusResponse> { }

public class GetProfilingStatusHandler : IGetProfilingStatusHandler
{
    private readonly DebugProfilerService _service;
    private readonly ILogger<GetProfilingStatusHandler> _logger;

    public GetProfilingStatusHandler(DebugProfilerService service, ILogger<GetProfilingStatusHandler> logger)
    {
        _service = service;
        _logger = logger;
    }

    public Task<ProfilerSessionStatusResponse> Handle(GetProfilingStatusRequest request, CancellationToken cancellationToken)
    {
        var session = _service.GetProfilingSession(request.Uri);

        if (session == null)
        {
            return Task.FromResult(new ProfilerSessionStatusResponse
            {
                IsProfiling = false
            });
        }

        var status = session.ProfilerManager.GetSessionStatus();

        return Task.FromResult(new ProfilerSessionStatusResponse
        {
            IsProfiling = (bool)(status["isProfiling"] ?? false),
            SessionId = status.GetValueOrDefault("sessionId")?.ToString(),
            SessionName = session.SessionName,
            ExecutionMode = session.ExecutionMode,
            DurationMs = Convert.ToDouble(status.GetValueOrDefault("durationMs") ?? 0),
            FunctionCallCount = Convert.ToInt32(status.GetValueOrDefault("functionCount") ?? 0),
            DataPointCount = Convert.ToInt32(status.GetValueOrDefault("dataPointCount") ?? 0),
            StartTime = session.StartTime
        });
    }
}

/// <summary>
/// 启动调试会话命令
/// </summary>
[Method("old8lang/startDebugging")]
public interface IStartDebuggingHandler : IJsonRpcRequestHandler<StartDebuggingRequest, DebugSessionStatusResponse> { }

public class StartDebuggingHandler : IStartDebuggingHandler
{
    private readonly DebugProfilerService _service;
    private readonly DocumentManager _documentManager;
    private readonly ILogger<StartDebuggingHandler> _logger;

    public StartDebuggingHandler(
        DebugProfilerService service,
        DocumentManager documentManager,
        ILogger<StartDebuggingHandler> logger)
    {
        _service = service;
        _documentManager = documentManager;
        _logger = logger;
    }

    public Task<DebugSessionStatusResponse> Handle(StartDebuggingRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var document = _documentManager.GetDocument(request.Uri);
            if (document?.Ast == null)
            {
                throw new InvalidOperationException($"Document {request.Uri} not found or has no AST");
            }

            // 创建解释器(无参数构造函数)
            var interpreter = new Old8Lang.Interpreter.LangInterpreter();
            var session = _service.StartDebugSession(request.Uri, interpreter, document.Ast);

            _logger.LogInformation("Started debug session for {Uri}", request.Uri);

            return Task.FromResult(new DebugSessionStatusResponse
            {
                IsDebugging = true,
                State = session.Debugger.State.ToString(),
                BreakpointCount = session.Debugger.BreakpointManager.GetAllBreakpoints().Count,
                CallStackDepth = session.Debugger.CallStack.Depth,
                StartTime = session.StartTime,
                RecentEvents = new List<DebugEventInfo>()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start debug session");
            throw;
        }
    }
}

/// <summary>
/// 停止调试会话命令
/// </summary>
[Method("old8lang/stopDebugging")]
public interface IStopDebuggingHandler : IJsonRpcRequestHandler<StopDebuggingRequest, DebugSessionStatusResponse> { }

public class StopDebuggingHandler : IStopDebuggingHandler
{
    private readonly DebugProfilerService _service;
    private readonly ILogger<StopDebuggingHandler> _logger;

    public StopDebuggingHandler(DebugProfilerService service, ILogger<StopDebuggingHandler> logger)
    {
        _service = service;
        _logger = logger;
    }

    public Task<DebugSessionStatusResponse> Handle(StopDebuggingRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _service.StopDebugSession(request.Uri);

            _logger.LogInformation("Stopped debug session for {Uri}", request.Uri);

            return Task.FromResult(new DebugSessionStatusResponse
            {
                IsDebugging = false,
                State = "Completed"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop debug session");
            throw;
        }
    }
}

/// <summary>
/// 添加断点命令
/// </summary>
[Method("old8lang/addBreakpoint")]
public interface IAddBreakpointHandler : IJsonRpcRequestHandler<BreakpointRequest, bool> { }

public class AddBreakpointHandler : IAddBreakpointHandler
{
    private readonly DebugProfilerService _service;
    private readonly ILogger<AddBreakpointHandler> _logger;

    public AddBreakpointHandler(DebugProfilerService service, ILogger<AddBreakpointHandler> logger)
    {
        _service = service;
        _logger = logger;
    }

    public Task<bool> Handle(BreakpointRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var session = _service.GetDebugSession(request.Uri);
            if (session == null)
            {
                throw new InvalidOperationException($"No debug session found for {request.Uri}");
            }

            // 使用正确的 API: AddLineBreakpoint
            var breakpointId = session.Debugger.BreakpointManager.AddLineBreakpoint(
                request.Uri,
                request.Line,
                request.Condition
            );

            _logger.LogInformation("Added breakpoint {Id} at line {Line} in {Uri}", breakpointId, request.Line, request.Uri);

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add breakpoint");
            return Task.FromResult(false);
        }
    }
}

/// <summary>
/// 移除断点命令
/// </summary>
[Method("old8lang/removeBreakpoint")]
public interface IRemoveBreakpointHandler : IJsonRpcRequestHandler<BreakpointRequest, bool> { }

public class RemoveBreakpointHandler : IRemoveBreakpointHandler
{
    private readonly DebugProfilerService _service;
    private readonly ILogger<RemoveBreakpointHandler> _logger;

    public RemoveBreakpointHandler(DebugProfilerService service, ILogger<RemoveBreakpointHandler> logger)
    {
        _service = service;
        _logger = logger;
    }

    public Task<bool> Handle(BreakpointRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var session = _service.GetDebugSession(request.Uri);
            if (session == null)
            {
                throw new InvalidOperationException($"No debug session found for {request.Uri}");
            }

            // 查找匹配的断点
            var breakpoints = session.Debugger.BreakpointManager.GetBreakpointsInFile(request.Uri);
            var breakpoint = breakpoints.FirstOrDefault(b => b.Line == request.Line);

            if (breakpoint == null)
            {
                _logger.LogWarning("No breakpoint found at line {Line} in {Uri}", request.Line, request.Uri);
                return Task.FromResult(false);
            }

            // 使用断点ID移除
            var removed = session.Debugger.BreakpointManager.RemoveBreakpoint(breakpoint.Id);

            _logger.LogInformation("Removed breakpoint {Id} at line {Line} in {Uri}", breakpoint.Id, request.Line, request.Uri);

            return Task.FromResult(removed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove breakpoint");
            return Task.FromResult(false);
        }
    }
}

/// <summary>
/// 调试控制命令（继续、暂停、单步等）
/// </summary>
[Method("old8lang/debugControl")]
public interface IDebugControlHandler : IJsonRpcRequestHandler<DebugControlRequest, bool> { }

public class DebugControlHandler : IDebugControlHandler
{
    private readonly DebugProfilerService _service;
    private readonly ILogger<DebugControlHandler> _logger;

    public DebugControlHandler(DebugProfilerService service, ILogger<DebugControlHandler> logger)
    {
        _service = service;
        _logger = logger;
    }

    public Task<bool> Handle(DebugControlRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var session = _service.GetDebugSession(request.Uri);
            if (session == null)
            {
                throw new InvalidOperationException($"No debug session found for {request.Uri}");
            }

            switch (request.Command.ToLowerInvariant())
            {
                case "continue":
                    session.Debugger.Continue();
                    break;
                case "pause":
                    session.Debugger.Pause();
                    break;
                case "stepinto":
                    session.Debugger.Step(StepType.StepInto);
                    break;
                case "stepover":
                    session.Debugger.Step(StepType.StepOver);
                    break;
                case "stepout":
                    session.Debugger.Step(StepType.StepOut);
                    break;
                default:
                    throw new ArgumentException($"Unknown debug command: {request.Command}");
            }

            _logger.LogInformation("Executed debug command {Command} for {Uri}", request.Command, request.Uri);

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute debug command");
            return Task.FromResult(false);
        }
    }
}
