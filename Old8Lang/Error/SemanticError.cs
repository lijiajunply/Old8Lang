using Old8Lang.AST;

namespace Old8Lang.Error;

/// <summary>
/// 语义错误基类
/// </summary>
public class SemanticError : Old8Exception
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="message">错误信息</param>
    /// <param name="suggestion">建议</param>
    public SemanticError(SourcePosition position, string message, string? suggestion = null)
        : base(
            "SEMANTIC_ERROR", 
            message,
            position,
            suggestion: suggestion)
    {}
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="message">错误信息</param>
    /// <param name="suggestion">建议</param>
    public SemanticError(IOldLangTree node, string message, string? suggestion = null)
        : base(
            "SEMANTIC_ERROR", 
            message,
            node,
            suggestion: suggestion)
    {}
}

/// <summary>
/// 参数错误
/// </summary>
public class ArgumentError : SemanticError
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="message">错误信息</param>
    public ArgumentError(SourcePosition position, string message)
        : base(
            position, 
            $"参数错误：{message}",
            "请检查函数调用的参数数量和类型是否正确")
    {}
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="message">错误信息</param>
    public ArgumentError(IOldLangTree node, string message)
        : base(
            node, 
            $"参数错误：{message}",
            "请检查函数调用的参数数量和类型是否正确")
    {}
}

/// <summary>
/// 格式错误
/// </summary>
public class FormatError : SemanticError
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="message">错误信息</param>
    public FormatError(SourcePosition position, string message)
        : base(
            position, 
            $"格式错误：{message}",
            "请检查语法格式是否符合要求")
    {}
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="message">错误信息</param>
    public FormatError(IOldLangTree node, string message)
        : base(
            node, 
            $"格式错误：{message}",
            "请检查语法格式是否符合要求")
    {}
}
