using Old8Lang.AST;
using Old8Lang;

namespace Old8Lang.Error;

/// <summary>
/// Old8语言错误的基类
/// </summary>
public class Old8Exception : Exception
{
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
    /// 构造函数
    /// </summary>
    /// <param name="errorCode">错误代码</param>
    /// <param name="message">错误信息</param>
    /// <param name="position">位置信息</param>
    /// <param name="node">AST节点</param>
    /// <param name="suggestion">建议</param>
    /// <param name="sourceContext">源代码上下文</param>
    protected Old8Exception(
        string errorCode, 
        string message, 
        SourcePosition position, 
        IOldLangTree? node = null, 
        string? suggestion = null,
        string[]? sourceContext = null) 
        : base(FormatErrorMessage(errorCode, message, position, suggestion, sourceContext))
    {
        ErrorCode = errorCode;
        Position = position;
        Node = node;
        Suggestion = suggestion;
        SourceContext = sourceContext;
    }
    
    /// <summary>
    /// 从AST节点创建错误
    /// </summary>
    /// <param name="errorCode">错误代码</param>
    /// <param name="message">错误信息</param>
    /// <param name="node">AST节点</param>
    /// <param name="suggestion">建议</param>
    /// <param name="sourceContext">源代码上下文</param>
    protected Old8Exception(
        string errorCode, 
        string message, 
        IOldLangTree node, 
        string? suggestion = null,
        string[]? sourceContext = null) 
        : this(errorCode, message, node.Position, node, suggestion, sourceContext)
    {}
    
    /// <summary>
    /// 格式化错误信息
    /// </summary>
    /// <param name="errorCode">错误代码</param>
    /// <param name="message">错误信息</param>
    /// <param name="position">位置信息</param>
    /// <param name="suggestion">建议</param>
    /// <param name="sourceContext">源代码上下文</param>
    /// <returns>格式化后的错误信息</returns>
    private static string FormatErrorMessage(string errorCode, string message, SourcePosition position, string? suggestion, string[]? sourceContext)
    {
        var sb = new System.Text.StringBuilder();
        
        // 彩色输出 - 使用ANSI颜色代码
        const string Reset = "\u001b[0m";
        const string Red = "\u001b[31m";
        const string Yellow = "\u001b[33m";
        const string Blue = "\u001b[34m";
        const string Green = "\u001b[32m";
        
        // 错误标题
        sb.AppendLine($"{Red}[{errorCode}]{Reset} {Yellow}{message}{Reset}");
        
        // 位置信息
        sb.AppendLine($"{Blue}位置:{Reset} {position}");
        
        // 源代码上下文
        if (sourceContext != null && sourceContext.Length > 0)
        {
            sb.AppendLine($"{Blue}上下文:{Reset}");
            for (int i = 0; i < sourceContext.Length; i++)
            {
                var lineNumber = position.Line - sourceContext.Length / 2 + i;
                var isErrorLine = lineNumber == position.Line;
                
                // 显示行号
                sb.Append($"{Blue}{lineNumber,3}{Reset} | ");
                
                // 显示源代码行
                var line = sourceContext[i];
                sb.AppendLine(line);
                
                // 如果是错误行，显示错误位置指示器
                if (isErrorLine)
                {
                    sb.Append($"{Blue}      {Reset} | ");
                    sb.Append(new string(' ', position.Column - 1));
                    sb.AppendLine($"{Red}^ 错误发生在这里{Reset}");
                }
            }
        }
        
        // 建议
        if (!string.IsNullOrEmpty(suggestion))
        {
            sb.AppendLine($"{Green}建议:{Reset} {suggestion}");
        }
        
        return sb.ToString();
    }
}