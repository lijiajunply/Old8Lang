using Old8Lang.AST;

namespace Old8Lang.Error;

/// <summary>
/// 未实现错误（功能尚未实现）
/// </summary>
public class NotImplementedError : RuntimeError
{
    /// <summary>
    /// 未实现错误代码
    /// </summary>
    public new const string ErrorCode = "NOT_IMPLEMENTED_ERROR";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="feature">未实现的功能</param>
    public NotImplementedError(IOldLangTree node, string feature)
        : base(
            node,
            ErrorCode,
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
            ErrorCode,
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
            ErrorCode,
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
    /// 权限错误代码
    /// </summary>
    public new const string ErrorCode = "PERMISSION_ERROR";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="operation">操作名称</param>
    /// <param name="resource">资源名称</param>
    public PermissionError(IOldLangTree node, string operation, string resource)
        : base(
            node,
            ErrorCode,
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
            ErrorCode,
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
            ErrorCode,
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
    /// 超时错误代码
    /// </summary>
    public new const string ErrorCode = "TIMEOUT_ERROR";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="operation">操作名称</param>
    /// <param name="timeoutSeconds">超时时间（秒）</param>
    public TimeoutError(IOldLangTree node, string operation, int timeoutSeconds)
        : base(
            node,
            ErrorCode,
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
            ErrorCode,
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
            ErrorCode,
            message,
            "请检查操作是否耗时过长，或者增加超时时间")
    {
    }
}
