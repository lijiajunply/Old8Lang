using MediatR;

namespace Old8Lang.LanguageServer.Models;

/// <summary>
/// 启动性能分析请求
/// </summary>
public class StartProfilingRequest : IRequest<ProfilerSessionStatusResponse>
{
    /// <summary>
    /// 文档 URI
    /// </summary>
    public required string Uri { get; set; }

    /// <summary>
    /// 会话名称（可选）
    /// </summary>
    public string? SessionName { get; set; }

    /// <summary>
    /// 执行模式（interpreter/compiler）
    /// </summary>
    public string ExecutionMode { get; set; } = "interpreter";
}

/// <summary>
/// 停止性能分析请求
/// </summary>
public class StopProfilingRequest : IRequest<PerformanceReportResponse>
{
    /// <summary>
    /// 文档 URI
    /// </summary>
    public required string Uri { get; set; }
}

/// <summary>
/// 获取性能分析状态请求
/// </summary>
public class GetProfilingStatusRequest : IRequest<ProfilerSessionStatusResponse>
{
    /// <summary>
    /// 文档 URI
    /// </summary>
    public required string Uri { get; set; }
}

/// <summary>
/// 启动调试请求
/// </summary>
public class StartDebuggingRequest : IRequest<DebugSessionStatusResponse>
{
    /// <summary>
    /// 文档 URI
    /// </summary>
    public required string Uri { get; set; }

    /// <summary>
    /// 执行模式（interpreter/compiler）
    /// </summary>
    public string ExecutionMode { get; set; } = "interpreter";
}

/// <summary>
/// 停止调试请求
/// </summary>
public class StopDebuggingRequest : IRequest<DebugSessionStatusResponse>
{
    /// <summary>
    /// 文档 URI
    /// </summary>
    public required string Uri { get; set; }
}

/// <summary>
/// 调试和性能分析请求 (兼容用)
/// </summary>
public class DebugProfilerRequest : IRequest<ProfilerSessionStatusResponse>
{
    /// <summary>
    /// 文档 URI
    /// </summary>
    public required string Uri { get; set; }

    /// <summary>
    /// 会话名称（可选）
    /// </summary>
    public string? SessionName { get; set; }

    /// <summary>
    /// 执行模式（interpreter/compiler）
    /// </summary>
    public string ExecutionMode { get; set; } = "interpreter";
}

/// <summary>
/// 性能报告请求
/// </summary>
public class PerformanceReportRequest : IRequest<PerformanceReportResponse>
{
    /// <summary>
    /// 文档 URI
    /// </summary>
    public required string Uri { get; set; }

    /// <summary>
    /// 报告格式（text/json/markdown/html/csv）
    /// </summary>
    public string Format { get; set; } = "markdown";
}

/// <summary>
/// 断点操作请求
/// </summary>
public class BreakpointRequest : IRequest<bool>
{
    /// <summary>
    /// 文档 URI
    /// </summary>
    public required string Uri { get; set; }

    /// <summary>
    /// 行号
    /// </summary>
    public int Line { get; set; }

    /// <summary>
    /// 条件表达式（可选）
    /// </summary>
    public string? Condition { get; set; }
}

/// <summary>
/// 调试控制请求
/// </summary>
public class DebugControlRequest : IRequest<bool>
{
    /// <summary>
    /// 文档 URI
    /// </summary>
    public required string Uri { get; set; }

    /// <summary>
    /// 控制命令（continue/pause/stepInto/stepOver/stepOut）
    /// </summary>
    public required string Command { get; set; }
}

/// <summary>
/// 调试会话状态响应
/// </summary>
public class DebugSessionStatusResponse
{
    /// <summary>
    /// 是否正在调试
    /// </summary>
    public bool IsDebugging { get; set; }

    /// <summary>
    /// 调试器状态
    /// </summary>
    public string? State { get; set; }

    /// <summary>
    /// 断点数量
    /// </summary>
    public int BreakpointCount { get; set; }

    /// <summary>
    /// 调用栈深度
    /// </summary>
    public int CallStackDepth { get; set; }

    /// <summary>
    /// 会话开始时间
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 最近的事件
    /// </summary>
    public List<DebugEventInfo>? RecentEvents { get; set; }
}

/// <summary>
/// 调试事件信息
/// </summary>
public class DebugEventInfo
{
    /// <summary>
    /// 事件类型
    /// </summary>
    public required string EventType { get; set; }

    /// <summary>
    /// 事件消息
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// 位置信息
    /// </summary>
    public PositionInfo? Position { get; set; }

    /// <summary>
    /// 当前函数
    /// </summary>
    public string? CurrentFunction { get; set; }

    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// 位置信息
/// </summary>
public class PositionInfo
{
    public int Line { get; set; }
    public int Column { get; set; }
}

/// <summary>
/// 性能分析会话状态响应
/// </summary>
public class ProfilerSessionStatusResponse
{
    /// <summary>
    /// 是否正在性能分析
    /// </summary>
    public bool IsProfiling { get; set; }

    /// <summary>
    /// 会话 ID
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// 会话名称
    /// </summary>
    public string? SessionName { get; set; }

    /// <summary>
    /// 执行模式
    /// </summary>
    public string? ExecutionMode { get; set; }

    /// <summary>
    /// 会话持续时间（毫秒）
    /// </summary>
    public double DurationMs { get; set; }

    /// <summary>
    /// 函数调用次数
    /// </summary>
    public int FunctionCallCount { get; set; }

    /// <summary>
    /// 数据点数量
    /// </summary>
    public int DataPointCount { get; set; }

    /// <summary>
    /// 会话开始时间
    /// </summary>
    public DateTime? StartTime { get; set; }
}

/// <summary>
/// 性能报告响应
/// </summary>
public class PerformanceReportResponse
{
    /// <summary>
    /// 报告格式
    /// </summary>
    public required string Format { get; set; }

    /// <summary>
    /// 报告内容
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// 生成时间
    /// </summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    /// 会话摘要
    /// </summary>
    public SessionSummaryInfo? Summary { get; set; }
}

/// <summary>
/// 会话摘要信息
/// </summary>
public class SessionSummaryInfo
{
    /// <summary>
    /// 总执行时间（毫秒）
    /// </summary>
    public double TotalExecutionTimeMs { get; set; }

    /// <summary>
    /// 函数调用次数
    /// </summary>
    public int TotalFunctionCalls { get; set; }

    /// <summary>
    /// 解析时间（毫秒）
    /// </summary>
    public double? ParsingTimeMs { get; set; }

    /// <summary>
    /// 编译时间（毫秒）
    /// </summary>
    public double? CompilationTimeMs { get; set; }

    /// <summary>
    /// 峰值内存使用（MB）
    /// </summary>
    public double PeakMemoryMb { get; set; }

    /// <summary>
    /// 最热函数（前5个）
    /// </summary>
    public List<HotFunctionInfo>? HotFunctions { get; set; }
}

/// <summary>
/// 热点函数信息
/// </summary>
public class HotFunctionInfo
{
    /// <summary>
    /// 函数名
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// 调用次数
    /// </summary>
    public int CallCount { get; set; }

    /// <summary>
    /// 总执行时间（毫秒）
    /// </summary>
    public double TotalTimeMs { get; set; }

    /// <summary>
    /// 平均执行时间（毫秒）
    /// </summary>
    public double AverageTimeMs { get; set; }
}
