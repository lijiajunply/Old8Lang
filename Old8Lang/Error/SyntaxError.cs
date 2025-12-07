namespace Old8Lang.Error;

/// <summary>
/// 语法错误
/// </summary>
public class SyntaxError : Old8Exception
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="message">错误信息</param>
    public SyntaxError(SourcePosition position, string message) 
        : base(
            "SYNTAX_ERROR", 
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
            "SYNTAX_ERROR", 
            message,
            position,
            suggestion: "请检查语法是否正确",
            sourceContext: sourceContext)
    {}
    
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