using Old8Lang.AST;

namespace Old8Lang.Error;

/// <summary>
/// 值错误（值不在有效范围内或不符合要求）
/// </summary>
public class ValueError : RuntimeError
{
    /// <summary>
    /// 值错误代码
    /// </summary>
    public new const string ErrorCode = "VALUE_ERROR";

    /// <summary>
    /// 构造函数 - 值超出范围
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="value">错误的值</param>
    /// <param name="minValue">最小有效值</param>
    /// <param name="maxValue">最大有效值</param>
    public ValueError(IOldLangTree node, object value, object minValue, object maxValue)
        : base(
            node,
            ErrorCode,
            $"值 '{value}' 超出有效范围 [{minValue}, {maxValue}]",
            "请确保值在有效范围内")
    {
    }

    /// <summary>
    /// 构造函数 - 值不符合要求
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="value">错误的值</param>
    /// <param name="requirement">要求说明</param>
    public ValueError(IOldLangTree node, object value, string requirement)
        : base(
            node,
            ErrorCode,
            $"值 '{value}' 不符合要求: {requirement}",
            "请检查值是否符合要求")
    {
    }

    /// <summary>
    /// 构造函数 - 自定义错误信息
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="message">错误信息</param>
    public ValueError(IOldLangTree node, string message)
        : base(
            node,
            ErrorCode,
            message,
            "请检查值是否符合要求")
    {
    }

    /// <summary>
    /// 构造函数 - 使用位置信息
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="message">错误信息</param>
    public ValueError(SourcePosition position, string message)
        : base(
            position,
            ErrorCode,
            message,
            "请检查值是否符合要求")
    {
    }
}

/// <summary>
/// 断言错误（断言失败）
/// </summary>
public class AssertionError : RuntimeError
{
    /// <summary>
    /// 断言错误代码
    /// </summary>
    public new const string ErrorCode = "ASSERTION_ERROR";

    /// <summary>
    /// 构造函数 - 断言失败
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="message">断言错误信息</param>
    public AssertionError(IOldLangTree node, string message)
        : base(
            node,
            ErrorCode,
            $"断言失败: {message}",
            "请检查断言条件是否正确")
    {
    }

    /// <summary>
    /// 构造函数 - 断言失败（无消息）
    /// </summary>
    /// <param name="node">AST节点</param>
    public AssertionError(IOldLangTree node)
        : base(
            node,
            ErrorCode,
            "断言失败",
            "请检查断言条件是否正确")
    {
    }

    /// <summary>
    /// 构造函数 - 使用位置信息
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="message">错误信息</param>
    public AssertionError(SourcePosition position, string message)
        : base(
            position,
            ErrorCode,
            $"断言失败: {message}",
            "请检查断言条件是否正确")
    {
    }
}

/// <summary>
/// 递归深度超限错误
/// </summary>
public class RecursionError : RuntimeError
{
    /// <summary>
    /// 递归错误代码
    /// </summary>
    public new const string ErrorCode = "RECURSION_ERROR";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="maxDepth">最大递归深度</param>
    public RecursionError(IOldLangTree node, int maxDepth)
        : base(
            node,
            ErrorCode,
            $"递归深度超过限制: 最大递归深度为 {maxDepth}",
            "请检查递归终止条件，或者考虑使用迭代方式实现")
    {
    }

    /// <summary>
    /// 构造函数 - 自定义错误信息
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="message">错误信息</param>
    public RecursionError(IOldLangTree node, string message)
        : base(
            node,
            ErrorCode,
            message,
            "请检查递归终止条件，或者考虑使用迭代方式实现")
    {
    }

    /// <summary>
    /// 构造函数 - 使用位置信息
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="maxDepth">最大递归深度</param>
    public RecursionError(SourcePosition position, int maxDepth)
        : base(
            position,
            ErrorCode,
            $"递归深度超过限制: 最大递归深度为 {maxDepth}",
            "请检查递归终止条件，或者考虑使用迭代方式实现")
    {
    }
}
