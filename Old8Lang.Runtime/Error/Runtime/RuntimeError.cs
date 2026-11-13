using Old8Lang.AST;

namespace Old8Lang.Error;

/// <summary>
/// 运行时错误基类
/// </summary>
public class RuntimeError : Old8Exception
{
    /// <summary>
    /// 运行时错误代码
    /// </summary>
    public new const string ErrorCode = "RUNTIME_ERROR";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="errorCode">错误代码</param>
    /// <param name="message">错误信息</param>
    protected RuntimeError(IOldLangTree node, string errorCode, string message)
        : base(
            errorCode,
            message,
            node)
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="errorCode">错误代码</param>
    /// <param name="message">错误信息</param>
    /// <param name="suggestion">建议</param>
    protected RuntimeError(IOldLangTree node, string errorCode, string message, string suggestion)
        : base(
            errorCode,
            message,
            node,
            suggestion)
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="errorCode">错误代码</param>
    /// <param name="message">错误信息</param>
    /// <param name="suggestion">建议</param>
    /// <param name="requestId">请求ID，用于跟踪分布式系统中的请求</param>
    protected RuntimeError(IOldLangTree node, string errorCode, string message, string suggestion, Guid requestId)
        : base(
            errorCode,
            message,
            node,
            suggestion,
            null,
            requestId)
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="errorCode">错误代码</param>
    /// <param name="message">错误信息</param>
    protected RuntimeError(SourcePosition position, string errorCode, string message)
        : base(
            errorCode,
            message,
            position)
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="errorCode">错误代码</param>
    /// <param name="message">错误信息</param>
    /// <param name="suggestion">建议</param>
    protected RuntimeError(SourcePosition position, string errorCode, string message, string suggestion)
        : base(
            errorCode,
            message,
            position,
            null,
            suggestion)
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="errorCode">错误代码</param>
    /// <param name="message">错误信息</param>
    /// <param name="suggestion">建议</param>
    /// <param name="requestId">请求ID，用于跟踪分布式系统中的请求</param>
    protected RuntimeError(SourcePosition position, string errorCode, string message, string suggestion, Guid requestId)
        : base(
            errorCode,
            message,
            position,
            null,
            suggestion,
            null,
            requestId)
    {
    }
}

/// <summary>
/// 属性错误（属性不存在）
/// </summary>
public class AttributeError : RuntimeError
{
    /// <summary>
    /// 属性错误代码
    /// </summary>
    public new const string ErrorCode = "ATTRIBUTE_ERROR";

    public AttributeError(IOldLangTree node, string attributeName, string typeName)
        : base(
            node,
            ErrorCode,
            $"类型 '{typeName}' 没有属性 '{attributeName}'",
            "请检查属性名称是否正确")
    {
    }

    public AttributeError(SourcePosition position, string attributeName, string typeName)
        : base(
            position,
            ErrorCode,
            $"类型 '{typeName}' 没有属性 '{attributeName}'",
            "请检查属性名称是否正确")
    {
    }
}

/// <summary>
/// 键错误（字典键不存在）
/// </summary>
public class KeyError : RuntimeError
{
    /// <summary>
    /// 键错误代码
    /// </summary>
    public new const string ErrorCode = "KEY_ERROR";

    public KeyError(IOldLangTree node, object key)
        : base(
            node,
            ErrorCode,
            $"键 '{key}' 不存在",
            "请检查键是否存在或使用安全访问")
    {
    }

    public KeyError(SourcePosition position, object key)
        : base(
            position,
            ErrorCode,
            $"键 '{key}' 不存在",
            "请检查键是否存在或使用安全访问")
    {
    }
}

/// <summary>
/// 除零错误
/// </summary>
public class ZeroDivisionError : RuntimeError
{
    /// <summary>
    /// 除零错误代码
    /// </summary>
    public new const string ErrorCode = "ZERO_DIVISION_ERROR";

    public ZeroDivisionError(IOldLangTree node)
        : base(
            node,
            ErrorCode,
            "除零错误",
            "请确保除数不为零")
    {
    }

    public ZeroDivisionError(SourcePosition position)
        : base(
            position,
            ErrorCode,
            "除零错误",
            "请确保除数不为零")
    {
    }
}

/// <summary>
/// 无效操作错误
/// </summary>
public class InvalidOperationError : RuntimeError
{
    /// <summary>
    /// 无效操作错误代码
    /// </summary>
    public new const string ErrorCode = "INVALID_OPERATION_ERROR";

    public InvalidOperationError(IOldLangTree node, string message)
        : base(
            node,
            ErrorCode,
            message,
            "请检查操作是否合法")
    {
    }

    public InvalidOperationError(IOldLangTree node, string message, string suggestion)
        : base(
            node,
            ErrorCode,
            message,
            suggestion)
    {
    }

    public InvalidOperationError(SourcePosition position, string message)
        : base(
            position,
            ErrorCode,
            message,
            "请检查操作是否合法")
    {
    }

    public InvalidOperationError(SourcePosition position, string message, string suggestion)
        : base(
            position,
            ErrorCode,
            message,
            suggestion)
    {
    }
}

/// <summary>
/// 内存溢出错误
/// </summary>
public class OutOfMemoryError : RuntimeError
{
    /// <summary>
    /// 内存溢出错误代码
    /// </summary>
    public new const string ErrorCode = "OUT_OF_MEMORY_ERROR";

    public OutOfMemoryError(IOldLangTree node)
        : base(
            node,
            ErrorCode,
            "内存溢出",
            "程序使用了过多内存，请检查是否存在内存泄漏或优化内存使用")
    {
    }

    public OutOfMemoryError(IOldLangTree node, string message)
        : base(
            node,
            ErrorCode,
            $"内存溢出: {message}",
            "程序使用了过多内存，请检查是否存在内存泄漏或优化内存使用")
    {
    }

    public OutOfMemoryError(SourcePosition position)
        : base(
            position,
            ErrorCode,
            "内存溢出",
            "程序使用了过多内存，请检查是否存在内存泄漏或优化内存使用")
    {
    }

    public OutOfMemoryError(SourcePosition position, string message)
        : base(
            position,
            ErrorCode,
            $"内存溢出: {message}",
            "程序使用了过多内存，请检查是否存在内存泄漏或优化内存使用")
    {
    }
}

/// <summary>
/// 数值溢出错误
/// </summary>
public class OverflowError : RuntimeError
{
    /// <summary>
    /// 数值溢出错误代码
    /// </summary>
    public new const string ErrorCode = "OVERFLOW_ERROR";

    public OverflowError(IOldLangTree node, string operation)
        : base(
            node,
            ErrorCode,
            $"数值溢出: {operation}",
            "数值运算结果超过了数据类型的范围")
    {
    }

    public OverflowError(IOldLangTree node, string operation, long value)
        : base(
            node,
            ErrorCode,
            $"数值溢出: {operation} 结果 {value} 超过了数据类型的范围",
            "数值运算结果超过了数据类型的范围")
    {
    }

    public OverflowError(SourcePosition position, string operation)
        : base(
            position,
            ErrorCode,
            $"数值溢出: {operation}",
            "数值运算结果超过了数据类型的范围")
    {
    }

    public OverflowError(SourcePosition position, string operation, long value)
        : base(
            position,
            ErrorCode,
            $"数值溢出: {operation} 结果 {value} 超过了数据类型的范围",
            "数值运算结果超过了数据类型的范围")
    {
    }
}