using Old8Lang.AST;
using Old8Lang.Interpreter;

namespace Old8Lang.Error;

/// <summary>
/// 调用栈帧信息
/// </summary>
public class CallStackFrame
{
    /// <summary>
    /// 函数名称
    /// </summary>
    public string FunctionName { get; set; }
    
    /// <summary>
    /// 源代码位置
    /// </summary>
    public SourcePosition Position { get; set; }
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="functionName">函数名称</param>
    /// <param name="position">源代码位置</param>
    public CallStackFrame(string functionName, SourcePosition position)
    {
        FunctionName = functionName;
        Position = position;
    }
    
    public override string ToString()
    {
        return $"{FunctionName} at {Position}";
    }
}

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
    /// Old8Lang调用栈
    /// </summary>
    public List<CallStackFrame> CallStack { get; set; }

    /// <summary>
    /// 原始错误消息（不包含格式化信息）
    /// </summary>
    public string OriginalMessage { get; }

    /// <summary>
    /// 友好的错误消息（用于字符串拼接和显示）
    /// </summary>
    public virtual string FriendlyMessage => OriginalMessage.Split('\n')[0];
    
    /// <summary>
    /// 变量状态信息
    /// </summary>
    public Dictionary<string, string> VariableStates { get; set; } = new();
    
    /// <summary>
    /// 使用 AsyncLocal 实现线程安全的调用栈，每个异步上下文有独立的调用栈
    /// </summary>
    private static readonly AsyncLocal<List<CallStackFrame>> _asyncCallStack = new();

    /// <summary>
    /// 当前调用栈，用于在错误发生时收集
    /// 使用 AsyncLocal 确保每个异步上下文（包括不同线程）都有自己的调用栈副本
    /// </summary>
    public static List<CallStackFrame> CurrentCallStack
    {
        get
        {
            if (_asyncCallStack.Value is null)
            {
                _asyncCallStack.Value = new List<CallStackFrame>();
            }
            return _asyncCallStack.Value;
        }
        set
        {
            _asyncCallStack.Value = value;
        }
    }

    /// <summary>
    /// 入栈，记录函数调用
    /// </summary>
    /// <param name="functionName">函数名称</param>
    /// <param name="position">源代码位置</param>
    public static void PushCallStack(string functionName, SourcePosition position)
    {
        CurrentCallStack.Add(new CallStackFrame(functionName, position));
    }

    /// <summary>
    /// 出栈，函数调用结束
    /// </summary>
    public static void PopCallStack()
    {
        if (CurrentCallStack.Count > 0)
        {
            CurrentCallStack.RemoveAt(CurrentCallStack.Count - 1);
        }
    }

    /// <summary>
    /// 清空调用栈
    /// </summary>
    public static void ClearCallStack()
    {
        CurrentCallStack.Clear();
    }

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
    /// <param name="innerException">内部异常</param>
    public Old8Exception(
        string errorCode,
        string message,
        SourcePosition position,
        IOldLangTree? node = null,
        string? suggestion = null,
        string[]? sourceContext = null,
        Guid? requestId = null,
        Exception? innerException = null)
        : this(errorCode, message, position, node, suggestion, sourceContext, requestId, innerException, new Dictionary<string, string>())
    {
    }
    
    /// <summary>
    /// 构造函数，支持变量状态收集
    /// </summary>
    /// <param name="errorCode">错误代码</param>
    /// <param name="message">错误信息</param>
    /// <param name="position">位置信息</param>
    /// <param name="node">AST节点</param>
    /// <param name="suggestion">建议</param>
    /// <param name="sourceContext">源代码上下文</param>
    /// <param name="requestId">请求ID</param>
    /// <param name="innerException">内部异常</param>
    /// <param name="variableStates">变量状态信息</param>
    public Old8Exception(
        string errorCode,
        string message,
        SourcePosition position,
        IOldLangTree? node,
        string? suggestion,
        string[]? sourceContext,
        Guid? requestId,
        Exception? innerException,
        Dictionary<string, string> variableStates)
        : base(FormatErrorMessage(errorCode, message, position, suggestion,
            GetSourceContextFromInterpreter(position, sourceContext), DateTime.Now, requestId ?? Guid.NewGuid(), 
            GetCombinedCallStack(innerException), variableStates), innerException)
    {
        ErrorCode = errorCode;
        Position = position;
        Node = node;
        Suggestion = suggestion;
        SourceContext = GetSourceContextFromInterpreter(position, sourceContext);
        Timestamp = DateTime.Now;
        RequestId = requestId ?? Guid.NewGuid();
        // 保存原始消息
        OriginalMessage = message;
        // 获取合并后的调用栈
        CallStack = GetCombinedCallStack(innerException);
        // 初始化变量状态
        VariableStates = variableStates;
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
    /// <param name="innerException">内部异常</param>
    public Old8Exception(
        string errorCode,
        string message,
        IOldLangTree node,
        string? suggestion = null,
        string[]? sourceContext = null,
        Guid? requestId = null,
        Exception? innerException = null)
        : this(errorCode, message, node.Position, node, suggestion,
            GetSourceContextFromInterpreter(node.Position, sourceContext), requestId, innerException, new Dictionary<string, string>())
    {
    }
    
    /// <summary>
    /// 从AST节点创建错误，支持变量状态收集
    /// </summary>
    /// <param name="errorCode">错误代码</param>
    /// <param name="message">错误信息</param>
    /// <param name="node">AST节点</param>
    /// <param name="suggestion">建议</param>
    /// <param name="sourceContext">源代码上下文</param>
    /// <param name="requestId">请求ID</param>
    /// <param name="innerException">内部异常</param>
    /// <param name="variableStates">变量状态信息</param>
    public Old8Exception(
        string errorCode,
        string message,
        IOldLangTree node,
        string? suggestion,
        string[]? sourceContext,
        Guid? requestId,
        Exception? innerException,
        Dictionary<string, string> variableStates)
        : this(errorCode, message, node.Position, node, suggestion,
            GetSourceContextFromInterpreter(node.Position, sourceContext), requestId, innerException, variableStates)
    {
    }

    /// <summary>
    /// 合并当前调用栈和内部异常的调用栈
    /// </summary>
    /// <param name="innerException">内部异常</param>
    /// <returns>合并后的调用栈</returns>
    private static List<CallStackFrame> GetCombinedCallStack(Exception? innerException)
    {
        // 复制当前调用栈
        var combinedCallStack = new List<CallStackFrame>(CurrentCallStack);
        
        // 如果有内部异常，并且是 Old8Exception 类型，合并其调用栈
        if (innerException is Old8Exception old8InnerException)
        {
            // 将内部异常的调用栈添加到当前调用栈前面
            // 这样可以保持调用链的正确顺序：最外层调用 -> 内层调用 -> 实际异常发生点
            combinedCallStack.InsertRange(0, old8InnerException.CallStack);
        }
        
        return combinedCallStack;
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
    /// <param name="callStack">调用栈信息</param>
    /// <param name="variableStates">变量状态信息</param>
    /// <returns>格式化后的错误信息</returns>
    private static string FormatErrorMessage(string errorCode, string message, SourcePosition position,
        string? suggestion, string[]? sourceContext, DateTime timestamp, Guid requestId, 
        List<CallStackFrame> callStack, Dictionary<string, string> variableStates)
    {
        var sb = new System.Text.StringBuilder();

        // 彩色输出 - 使用ANSI颜色代码
        const string reset = "\u001b[0m";
        const string red = "\u001b[31m";
        const string blue = "\u001b[34m";
        const string green = "\u001b[32m";
        const string cyan = "\u001b[36m";
        const string magenta = "\u001b[35m";

        // 错误标题 - 标准化格式，避免重复显示错误代码
        sb.AppendLine($"{red}[{errorCode}]{reset} {message}");

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

        // 变量状态
        if (variableStates.Count > 0)
        {
            sb.AppendLine($"{magenta}相关变量状态:{reset}");
            foreach (var (varName, varValue) in variableStates)
            {
                sb.AppendLine($"  {varName} = {varValue}");
            }
        }

        // 建议
        if (!string.IsNullOrEmpty(suggestion))
        {
            sb.AppendLine($"{green}建议:{reset} {suggestion}");
        }

        // Old8Lang调用栈
        if (callStack.Count > 0)
        {
            sb.AppendLine($"{cyan}Old8Lang调用栈:{reset}");
            for (int i = 0; i < callStack.Count; i++)
            {
                var frame = callStack[i];
                var indent = new string(' ', i * 2);
                sb.AppendLine($"{indent}{frame.FunctionName} at {frame.Position}");
            }
        }

        return sb.ToString();
    }
}