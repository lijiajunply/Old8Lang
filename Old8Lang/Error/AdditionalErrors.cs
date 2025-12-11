using Old8Lang.AST;

namespace Old8Lang.Error;

/// <summary>
/// 值错误（值不在有效范围内或不符合要求）
/// </summary>
public class ValueError : RuntimeError
{
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
    /// 构造函数 - 断言失败
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="message">断言错误信息</param>
    public AssertionError(IOldLangTree node, string message)
        : base(
            node,
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
            $"断言失败: {message}",
            "请检查断言条件是否正确")
    {
    }
}

/// <summary>
/// 文件不存在错误
/// </summary>
public class FileNotFoundError : RuntimeError
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="filePath">文件路径</param>
    public FileNotFoundError(IOldLangTree node, string filePath)
        : base(
            node,
            $"文件不存在: '{filePath}'",
            "请检查文件路径是否正确，以及文件是否存在")
    {
    }

    /// <summary>
    /// 构造函数 - 使用位置信息
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="filePath">文件路径</param>
    public FileNotFoundError(SourcePosition position, string filePath)
        : base(
            position,
            $"文件不存在: '{filePath}'",
            "请检查文件路径是否正确，以及文件是否存在")
    {
    }
}

/// <summary>
/// 递归深度超限错误
/// </summary>
public class RecursionError : RuntimeError
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="maxDepth">最大递归深度</param>
    public RecursionError(IOldLangTree node, int maxDepth)
        : base(
            node,
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
            $"递归深度超过限制: 最大递归深度为 {maxDepth}",
            "请检查递归终止条件，或者考虑使用迭代方式实现")
    {
    }
}

/// <summary>
/// 未实现错误（功能尚未实现）
/// </summary>
public class NotImplementedError : RuntimeError
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="feature">未实现的功能</param>
    public NotImplementedError(IOldLangTree node, string feature)
        : base(
            node,
            $"功能 '{feature}' 尚未实现",
            "该功能将在未来版本中实现")
    {
    }

    /// <summary>
    /// 构造函数 - 无功能名称
    /// </summary>
    /// <param name="node">AST节点</param>
    public NotImplementedError(IOldLangTree node)
        : base(
            node,
            "该功能尚未实现",
            "该功能将在未来版本中实现")
    {
    }

    /// <summary>
    /// 构造函数 - 使用位置信息
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="feature">未实现的功能</param>
    public NotImplementedError(SourcePosition position, string feature)
        : base(
            position,
            $"功能 '{feature}' 尚未实现",
            "该功能将在未来版本中实现")
    {
    }
}

/// <summary>
/// 权限错误（没有足够的权限执行操作）
/// </summary>
public class PermissionError : RuntimeError
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="operation">操作名称</param>
    /// <param name="resource">资源名称</param>
    public PermissionError(IOldLangTree node, string operation, string resource)
        : base(
            node,
            $"权限不足: 无法对 '{resource}' 执行 '{operation}' 操作",
            "请检查是否有足够的权限执行该操作")
    {
    }

    /// <summary>
    /// 构造函数 - 自定义错误信息
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="message">错误信息</param>
    public PermissionError(IOldLangTree node, string message)
        : base(
            node,
            message,
            "请检查是否有足够的权限执行该操作")
    {
    }

    /// <summary>
    /// 构造函数 - 使用位置信息
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="message">错误信息</param>
    public PermissionError(SourcePosition position, string message)
        : base(
            position,
            message,
            "请检查是否有足够的权限执行该操作")
    {
    }
}

/// <summary>
/// 超时错误（操作超时）
/// </summary>
public class TimeoutError : RuntimeError
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="operation">操作名称</param>
    /// <param name="timeoutSeconds">超时时间（秒）</param>
    public TimeoutError(IOldLangTree node, string operation, int timeoutSeconds)
        : base(
            node,
            $"操作超时: '{operation}' 在 {timeoutSeconds} 秒内未完成",
            "请检查操作是否耗时过长，或者增加超时时间")
    {
    }

    /// <summary>
    /// 构造函数 - 自定义错误信息
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="message">错误信息</param>
    public TimeoutError(IOldLangTree node, string message)
        : base(
            node,
            message,
            "请检查操作是否耗时过长，或者增加超时时间")
    {
    }

    /// <summary>
    /// 构造函数 - 使用位置信息
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="message">错误信息</param>
    public TimeoutError(SourcePosition position, string message)
        : base(
            position,
            message,
            "请检查操作是否耗时过长，或者增加超时时间")
    {
    }
}
