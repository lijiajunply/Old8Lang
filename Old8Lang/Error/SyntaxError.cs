namespace Old8Lang.Error;

/// <summary>
/// 语法错误
/// </summary>
public class SyntaxError : Old8Exception
{
    /// <summary>
    /// 语法错误代码
    /// </summary>
    public new const string ErrorCode = "SYNTAX_ERROR";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="message">错误信息</param>
    public SyntaxError(SourcePosition position, string message) 
        : base(
            ErrorCode, 
            message,
            position,
            suggestion: "请检查语法是否正确")
    {}
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="message">错误信息</param>
    /// <param name="sourceContext">源代码上下文</param>
    public SyntaxError(SourcePosition position, string message, string[] sourceContext) 
        : base(
            ErrorCode, 
            message,
            position,
            suggestion: "请检查语法是否正确",
            sourceContext: sourceContext)
    {}
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="message">错误信息</param>
    /// <param name="suggestion">建议</param>
    /// <param name="requestId">请求ID，用于跟踪分布式系统中的请求</param>
    public SyntaxError(SourcePosition position, string message, string suggestion, Guid requestId)
        : base(
            ErrorCode,
            message,
            position,
            null,
            suggestion,
            null,
            requestId)
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="message">错误信息</param>
    /// <param name="sourceContext">源代码上下文</param>
    /// <param name="requestId">请求ID，用于跟踪分布式系统中的请求</param>
    public SyntaxError(SourcePosition position, string message, string[] sourceContext, Guid requestId)
        : base(
            ErrorCode,
            message,
            position,
            null,
            "请检查语法是否正确",
            sourceContext,
            requestId)
    {
    }
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="tokenValue">令牌值</param>
    /// <param name="line">行号</param>
    /// <param name="column">列号</param>
    /// <param name="message">错误信息</param>
    public SyntaxError(string? tokenValue, int line, int column, string message) 
        : this(new SourcePosition(line, column, tokenValue: tokenValue), message)
    {}
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="tokenValue">令牌值</param>
    /// <param name="line">行号</param>
    /// <param name="column">列号</param>
    /// <param name="message">错误信息</param>
    /// <param name="sourceContext">源代码上下文</param>
    public SyntaxError(string? tokenValue, int line, int column, string message, string[] sourceContext) 
        : this(new SourcePosition(line, column, tokenValue: tokenValue), message, sourceContext)
    {}
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="tokenValue">令牌值</param>
    /// <param name="line">行号</param>
    /// <param name="column">列号</param>
    /// <param name="fileName">文件名</param>
    /// <param name="message">错误信息</param>
    public SyntaxError(string? tokenValue, int line, int column, string? fileName, string message) 
        : this(new SourcePosition(line, column, fileName: fileName, tokenValue: tokenValue), message)
    {}
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="tokenValue">令牌值</param>
    /// <param name="line">行号</param>
    /// <param name="column">列号</param>
    /// <param name="fileName">文件名</param>
    /// <param name="message">错误信息</param>
    /// <param name="sourceContext">源代码上下文</param>
    public SyntaxError(string? tokenValue, int line, int column, string? fileName, string message, string[] sourceContext) 
        : this(new SourcePosition(line, column, fileName: fileName, tokenValue: tokenValue), message, sourceContext)
    {}
}