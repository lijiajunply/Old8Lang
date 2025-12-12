using Old8Lang.AST;
using Old8Lang.LangParser;

namespace Old8Lang.Error;

/// <summary>
/// Old8语言错误的基类
/// </summary>
public class Old8Exception : Exception
{
    /// <summary>
    /// 当前解释器实例，用于获取源代码上下文
    /// </summary>
    public static LangInterpreter? CurrentInterpreter { get; set; }

    /// <summary>
    /// 错误代码
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// 源代码位置信息
    /// </summary>
    public SourcePosition Position { get; }

    /// <summary>
    /// 错误建议
    /// </summary>
    public string? Suggestion { get; }

    /// <summary>
    /// AST节点
    /// </summary>
    public IOldLangTree? Node { get; }

    /// <summary>
    /// 源代码上下文
    /// </summary>
    public string[]? SourceContext { get; }

    /// <summary>
    /// 错误发生时间戳
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// 请求ID，用于跟踪分布式系统中的请求
    /// </summary>
    public Guid RequestId { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="errorCode">错误代码</param>
    /// <param name="message">错误信息</param>
    /// <param name="position">位置信息</param>
    /// <param name="node">AST节点</param>
    /// <param name="suggestion">建议</param>
    /// <param name="sourceContext">源代码上下文</param>
    /// <param name="requestId">请求ID，用于跟踪分布式系统中的请求</param>
    protected Old8Exception(
        string errorCode,
        string message,
        SourcePosition position,
        IOldLangTree? node = null,
        string? suggestion = null,
        string[]? sourceContext = null,
        Guid? requestId = null)
        : base(FormatErrorMessage(errorCode, message, position, suggestion,
            GetSourceContextFromInterpreter(position, sourceContext), DateTime.Now, requestId ?? Guid.NewGuid()))
    {
        ErrorCode = errorCode;
        Position = position;
        Node = node;
        Suggestion = suggestion;
        SourceContext = GetSourceContextFromInterpreter(position, sourceContext);
        Timestamp = DateTime.Now;
        RequestId = requestId ?? Guid.NewGuid();
    }

    /// <summary>
    /// 从AST节点创建错误
    /// </summary>
    /// <param name="errorCode">错误代码</param>
    /// <param name="message">错误信息</param>
    /// <param name="node">AST节点</param>
    /// <param name="suggestion">建议</param>
    /// <param name="sourceContext">源代码上下文</param>
    /// <param name="requestId">请求ID，用于跟踪分布式系统中的请求</param>
    protected Old8Exception(
        string errorCode,
        string message,
        IOldLangTree node,
        string? suggestion = null,
        string[]? sourceContext = null,
        Guid? requestId = null)
        : this(errorCode, message, node.Position, node, suggestion,
            GetSourceContextFromInterpreter(node.Position, sourceContext), requestId)
    {
    }

    /// <summary>
    /// 从当前解释器获取源代码上下文
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="providedContext">提供的上下文信息</param>
    /// <returns>源代码上下文</returns>
    private static string[] GetSourceContextFromInterpreter(SourcePosition position, string[]? providedContext)
    {
        // 如果提供了上下文，直接使用
        if (providedContext is not null && providedContext.Length > 0)
        {
            return providedContext;
        }

        // 否则从当前解释器获取
        if (CurrentInterpreter is not null)
        {
            return CurrentInterpreter.GetSourceContext(position);
        }

        // 如果没有解释器，返回空数组
        return [];
    }

    /// <summary>
    /// 格式化错误信息
    /// </summary>
    /// <param name="errorCode">错误代码</param>
    /// <param name="message">错误信息</param>
    /// <param name="position">位置信息</param>
    /// <param name="suggestion">建议</param>
    /// <param name="sourceContext">源代码上下文</param>
    /// <param name="timestamp">错误发生时间</param>
    /// <param name="requestId">请求ID</param>
    /// <returns>格式化后的错误信息</returns>
    private static string FormatErrorMessage(string errorCode, string message, SourcePosition position,
        string? suggestion, string[]? sourceContext, DateTime timestamp, Guid requestId)
    {
        var sb = new System.Text.StringBuilder();

        // 彩色输出 - 使用ANSI颜色代码
        const string reset = "\u001b[0m";
        const string red = "\u001b[31m";
        const string yellow = "\u001b[33m";
        const string blue = "\u001b[34m";
        const string green = "\u001b[32m";

        // 错误标题 - 标准化格式
        sb.AppendLine($"{red}[{errorCode}]{reset} {yellow}{message}{reset}");

        // 基本信息
        sb.AppendLine($"{blue}时间:{reset} {timestamp:yyyy-MM-dd HH:mm:ss.fff}");
        sb.AppendLine($"{blue}请求ID:{reset} {requestId}");
        sb.AppendLine($"{blue}位置:{reset} {position}");

        // 源代码上下文
        if (sourceContext is { Length: > 0 })
        {
            sb.AppendLine($"{blue}上下文:{reset}");
            for (int i = 0; i < sourceContext.Length; i++)
            {
                var lineNumber = position.Line - sourceContext.Length / 2 + i;
                var isErrorLine = lineNumber == position.Line;

                // 显示行号
                sb.Append($"{blue}{lineNumber,3}{reset} | ");

                // 显示源代码行
                var line = sourceContext[i];
                sb.AppendLine(line);

                // 如果是错误行，显示错误位置指示器
                if (isErrorLine)
                {
                    sb.Append($"{blue}      {reset} | ");
                    // 确保列号大于0，避免ArgumentOutOfRangeException
                    var spaceCount = Math.Max(0, position.Column - 1);
                    sb.Append(new string(' ', spaceCount));
                    sb.AppendLine($"{red}^ 错误发生在这里{reset}");
                }
            }
        }

        // 建议
        if (!string.IsNullOrEmpty(suggestion))
        {
            sb.AppendLine($"{green}建议:{reset} {suggestion}");
        }

        // 堆栈跟踪
        sb.AppendLine($"{blue}堆栈跟踪:{reset}");
        sb.AppendLine(new System.Diagnostics.StackTrace(2, true).ToString().Trim());

        return sb.ToString();
    }
}