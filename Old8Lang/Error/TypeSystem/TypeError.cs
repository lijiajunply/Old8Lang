using Old8Lang.AST;

namespace Old8Lang.Error;

/// <summary>
/// 类型不匹配错误
/// </summary>
public class TypeError : RuntimeError
{
    /// <summary>
    /// 类型错误代码
    /// </summary>
    public new const string ErrorCode = "TYPE_ERROR";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="expectedType">期望类型</param>
    /// <param name="actualType">实际类型</param>
    public TypeError(IOldLangTree node, string expectedType, string actualType)
        : base(
            node,
            ErrorCode,
            $"类型不匹配: 期望 {expectedType}，但得到 {actualType}",
            "请检查变量类型或转换操作")
    {}

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="expectedType">期望类型</param>
    /// <param name="actualType">实际类型</param>
    /// <param name="detail">详细错误信息</param>
    public TypeError(IOldLangTree node, string expectedType, string actualType, string detail)
        : base(
            node,
            ErrorCode,
            $"类型不匹配: 期望 {expectedType}，但得到 {actualType}。{detail}",
            "请检查变量类型或转换操作")
    {}

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="message">错误信息</param>
    public TypeError(IOldLangTree node, string message)
        : base(
            node,
            ErrorCode,
            message,
            "请检查类型相关操作")
    {}

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="message">错误信息</param>
    public TypeError(SourcePosition position, string message)
        : base(
            position,
            ErrorCode,
            message,
            "请检查类型相关操作")
    {}

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="expectedType">期望类型</param>
    /// <param name="actualType">实际类型</param>
    public TypeError(SourcePosition position, string expectedType, string actualType)
        : base(
            position,
            ErrorCode,
            $"类型不匹配: 期望 {expectedType}，但得到 {actualType}",
            "请检查变量类型或转换操作")
    {}

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="expectedType">期望类型</param>
    /// <param name="actualType">实际类型</param>
    /// <param name="detail">详细错误信息</param>
    public TypeError(SourcePosition position, string expectedType, string actualType, string detail)
        : base(
            position,
            ErrorCode,
            $"类型不匹配: 期望 {expectedType}，但得到 {actualType}。{detail}",
            "请检查变量类型或转换操作")
    {}
}