using Old8Lang.Debugger;
using Old8Lang.Profiler;
using Old8Lang.Interpreter;
using Old8Lang.AST.Statement;
using System.Collections.Concurrent;

namespace Old8Lang.LanguageServer.Services;

/// <summary>
/// 调试和性能分析服务 - 管理调试会话和性能分析会话
/// </summary>
public class DebugProfilerService
{
    private readonly ConcurrentDictionary<string, DebugSession> _debugSessions = new();
    private readonly ConcurrentDictionary<string, ProfilerSession> _profilerSessions = new();
    private readonly ConcurrentDictionary<string, DebugVariableSnapshot> _variableSnapshots = new(StringComparer.Ordinal);
    private readonly Lock _lockObject = new();

    /// <summary>
    /// 启动调试会话
    /// </summary>
    /// <param name="uri">文档 URI</param>
    /// <param name="interpreter">解释器实例</param>
    /// <param name="ast">抽象语法树</param>
    /// <returns>调试会话</returns>
    public DebugSession StartDebugSession(string uri, LangInterpreter interpreter, BlockStatement ast)
    {
        lock (_lockObject)
        {
            // 如果已存在会话,先停止
            if (_debugSessions.TryGetValue(uri, out var existingSession))
            {
                StopDebugSession(uri);
            }

            var debugger = new Debugger.Debugger();
            var debuggableInterpreter = new DebuggableInterpreter(interpreter, debugger);

            var session = new DebugSession
            {
                Uri = uri,
                Debugger = debugger,
                DebuggableInterpreter = debuggableInterpreter,
                Interpreter = interpreter,
                Ast = ast,
                StartTime = DateTime.Now
            };

            _debugSessions[uri] = session;

            // 订阅调试器事件
            debugger.StateChanged += (sender, args) => session.OnStateChanged(args);
            debugger.BreakpointHit += (sender, args) => session.OnBreakpointHit(args);
            debugger.ErrorOccurred += (sender, args) => session.OnErrorOccurred(args);

            debugger.StartDebugging(uri);

            return session;
        }
    }

    /// <summary>
    /// 停止调试会话
    /// </summary>
    /// <param name="uri">文档 URI</param>
    public void StopDebugSession(string uri)
    {
        lock (_lockObject)
        {
            if (_debugSessions.TryRemove(uri, out var session))
            {
                session.Debugger.StopDebugging();
            }
        }
    }

    /// <summary>
    /// 获取调试会话
    /// </summary>
    /// <param name="uri">文档 URI</param>
    /// <returns>调试会话,如果不存在则返回 null</returns>
    public DebugSession? GetDebugSession(string uri)
    {
        _debugSessions.TryGetValue(uri, out var session);
        return session;
    }

    /// <summary>
    /// 启动性能分析会话
    /// </summary>
    /// <param name="uri">文档 URI</param>
    /// <param name="sessionName">会话名称</param>
    /// <param name="executionMode">执行模式(interpreter/compiler)</param>
    /// <returns>性能分析会话</returns>
    public ProfilerSession StartProfilingSession(string uri, string sessionName = "", string executionMode = "interpreter")
    {
        lock (_lockObject)
        {
            // 如果已存在会话,先停止
            if (_profilerSessions.TryGetValue(uri, out var existingSession))
            {
                StopProfilingSession(uri);
            }

            var profilerManager = new ProfilerManager();
            var sessionId = profilerManager.StartProfiling(sessionName, uri, executionMode);

            var session = new ProfilerSession
            {
                Uri = uri,
                SessionId = sessionId,
                ProfilerManager = profilerManager,
                SessionName = sessionName,
                ExecutionMode = executionMode,
                StartTime = DateTime.Now
            };

            _profilerSessions[uri] = session;

            return session;
        }
    }

    /// <summary>
    /// 停止性能分析会话
    /// </summary>
    /// <param name="uri">文档 URI</param>
    /// <returns>性能摘要</returns>
    public PerformanceSummary? StopProfilingSession(string uri)
    {
        lock (_lockObject)
        {
            if (_profilerSessions.TryRemove(uri, out var session))
            {
                return session.ProfilerManager.StopProfiling();
            }

            return null;
        }
    }

    /// <summary>
    /// 获取性能分析会话
    /// </summary>
    /// <param name="uri">文档 URI</param>
    /// <returns>性能分析会话,如果不存在则返回 null</returns>
    public ProfilerSession? GetProfilingSession(string uri)
    {
        _profilerSessions.TryGetValue(uri, out var session);
        return session;
    }

    /// <summary>
    /// 生成性能报告
    /// </summary>
    /// <param name="uri">文档 URI</param>
    /// <param name="format">报告格式</param>
    /// <returns>性能报告文本</returns>
    public string? GeneratePerformanceReport(string uri, ReportFormat format = ReportFormat.Markdown)
    {
        if (_profilerSessions.TryGetValue(uri, out var session))
        {
            return session.ProfilerManager.GenerateReport(format);
        }

        return null;
    }

    /// <summary>
    /// 更新指定文档的暂停态调试变量快照（供 Inline Values 读取）。
    /// </summary>
    public void UpdateVariableSnapshot(
        string uri,
        int frameId,
        int stoppedLine,
        IReadOnlyDictionary<string, string> variables)
    {
        var copy = new Dictionary<string, string>(variables, StringComparer.Ordinal);
        _variableSnapshots[uri] = new DebugVariableSnapshot
        {
            Uri = uri,
            FrameId = frameId,
            StoppedLine = stoppedLine,
            Variables = copy,
            UpdatedAtUtc = DateTime.UtcNow
        };
    }

    /// <summary>清除指定文档的调试变量快照</summary>
    public void ClearVariableSnapshot(string uri)
    {
        _variableSnapshots.TryRemove(uri, out _);
    }

    /// <summary>
    /// 获取指定文档的调试变量快照；优先返回外部 DAP 推送的快照，否则尝试从 LSP 调试会话当前栈帧读取。
    /// </summary>
    public DebugVariableSnapshot? GetVariableSnapshot(string uri, int? frameId = null)
    {
        if (_variableSnapshots.TryGetValue(uri, out var snapshot))
        {
            if (frameId == null || frameId <= 0 || snapshot.FrameId == frameId)
            {
                return snapshot;
            }
        }

        if (_debugSessions.TryGetValue(uri, out var session))
        {
            var frame = session.Debugger.CallStack.CurrentFrame;
            if (frame != null && frame.LocalVariables.Count > 0)
            {
                return new DebugVariableSnapshot
                {
                    Uri = uri,
                    FrameId = frameId ?? 1,
                    StoppedLine = Math.Max(0, frame.Line - 1),
                    Variables = new Dictionary<string, string>(frame.LocalVariables, StringComparer.Ordinal),
                    UpdatedAtUtc = DateTime.UtcNow
                };
            }
        }

        return null;
    }

    /// <summary>
    /// 清除所有会话
    /// </summary>
    public void ClearAllSessions()
    {
        lock (_lockObject)
        {
            foreach (var uri in _debugSessions.Keys.ToList())
            {
                StopDebugSession(uri);
            }

            foreach (var uri in _profilerSessions.Keys.ToList())
            {
                StopProfilingSession(uri);
            }

            _variableSnapshots.Clear();
        }
    }
}

/// <summary>
/// 暂停态调试变量快照（由 DAP 跟踪器或调试会话写入，供 InlineValuesHandler 渲染）
/// </summary>
public sealed class DebugVariableSnapshot
{
    public required string Uri { get; init; }
    public required int FrameId { get; init; }
    public required int StoppedLine { get; init; }
    public required IReadOnlyDictionary<string, string> Variables { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
}

/// <summary>
/// 调试会话
/// </summary>
public class DebugSession
{
    public required string Uri { get; init; }
    public required Debugger.Debugger Debugger { get; init; }
    public required DebuggableInterpreter DebuggableInterpreter { get; init; }
    public required LangInterpreter Interpreter { get; init; }
    public required BlockStatement Ast { get; init; }
    public DateTime StartTime { get; init; }

    // 事件队列
    private readonly Queue<DebuggerEventArgs> _eventQueue = new();
    private readonly object _eventLock = new();

    public void OnStateChanged(DebuggerEventArgs args)
    {
        lock (_eventLock)
        {
            _eventQueue.Enqueue(args);
        }
    }

    public void OnBreakpointHit(DebuggerEventArgs args)
    {
        lock (_eventLock)
        {
            _eventQueue.Enqueue(args);
        }
    }

    public void OnErrorOccurred(DebuggerEventArgs args)
    {
        lock (_eventLock)
        {
            _eventQueue.Enqueue(args);
        }
    }

    public List<DebuggerEventArgs> GetEvents()
    {
        lock (_eventLock)
        {
            var events = _eventQueue.ToList();
            _eventQueue.Clear();
            return events;
        }
    }
}

/// <summary>
/// 性能分析会话
/// </summary>
public class ProfilerSession
{
    public required string Uri { get; init; }
    public required Guid SessionId { get; init; }
    public required ProfilerManager ProfilerManager { get; init; }
    public required string SessionName { get; init; }
    public required string ExecutionMode { get; init; }
    public DateTime StartTime { get; init; }
}
