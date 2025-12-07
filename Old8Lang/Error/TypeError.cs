using Old8Lang.AST;

namespace Old8Lang.Error;

/// <summary>
/// 类型不匹配错误
/// </summary>
public class TypeError : Old8Exception
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="expectedType">期望类型</param>
    /// <param name="actualType">实际类型</param>
    public TypeError(IOldLangTree node, string expectedType, string actualType) 
        : base(
            "TYPE_ERROR", 
            $"类型不匹配: 期望 {expectedType}，但得到 {actualType}",
            node,
            "请检查变量类型或转换操作")
    {}
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="message">错误信息</param>
    public TypeError(IOldLangTree node, string message) 
        : base(
            "TYPE_ERROR", 
            message,
            node,
            "请检查类型相关操作")
    {}
}