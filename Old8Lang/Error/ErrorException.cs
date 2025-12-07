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
    /// 构造函数
    /// </summary>
    /// <param name="errorCode">错误代码</param>
    /// <param name="message">错误信息</param>
    /// <param name="position">位置信息</param>
    /// <param name="node">AST节点</param>
    /// <param name="suggestion">建议</param>
    protected Old8Exception(
        string errorCode, 
        string message, 
        SourcePosition position, 
        IOldLangTree? node = null, 
        string? suggestion = null) 
        : base($"[{errorCode}] {message}\n位置: {position}\n{suggestion}")
    {
        ErrorCode = errorCode;
        Position = position;
        Node = node;
        Suggestion = suggestion;
    }
    
    /// <summary>
    /// 从AST节点创建错误
    /// </summary>
    /// <param name="errorCode">错误代码</param>
    /// <param name="message">错误信息</param>
    /// <param name="node">AST节点</param>
    /// <param name="suggestion">建议</param>
    protected Old8Exception(
        string errorCode, 
        string message, 
        IOldLangTree node, 
        string? suggestion = null) 
        : this(errorCode, message, node.Position, node, suggestion)
    {}
}