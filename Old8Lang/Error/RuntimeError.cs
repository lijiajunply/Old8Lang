using Old8Lang.AST;

namespace Old8Lang.Error;

/// <summary>
/// 运行时错误基类
/// </summary>
public class RuntimeError : Old8Exception
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="message">错误信息</param>
    public RuntimeError(IOldLangTree node, string message) 
        : base(
            "RUNTIME_ERROR", 
            message,
            node)
    {}
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="message">错误信息</param>
    /// <param name="suggestion">建议</param>
    public RuntimeError(IOldLangTree node, string message, string suggestion) 
        : base(
            "RUNTIME_ERROR", 
            message,
            node,
            suggestion)
    {}
}

/// <summary>
/// 属性错误（属性不存在）
/// </summary>
public class AttributeError : RuntimeError
{
    public AttributeError(IOldLangTree node, string attributeName, string typeName) 
        : base(
            node, 
            $"类型 '{typeName}' 没有属性 '{attributeName}'",
            "请检查属性名称是否正确")
    {}
}

/// <summary>
/// 键错误（字典键不存在）
/// </summary>
public class KeyError : RuntimeError
{
    public KeyError(IOldLangTree node, object key) 
        : base(
            node, 
            $"键 '{key}' 不存在",
            "请检查键是否存在或使用安全访问")
    {}
}

/// <summary>
/// 除零错误
/// </summary>
public class ZeroDivisionError : RuntimeError
{
    public ZeroDivisionError(IOldLangTree node) 
        : base(
            node, 
            "除零错误",
            "请确保除数不为零")
    {}
}

/// <summary>
/// 无效操作错误
/// </summary>
public class InvalidOperationError : RuntimeError
{
    public InvalidOperationError(IOldLangTree node, string operation) 
        : base(
            node, 
            $"无效操作: {operation}",
            "请检查操作是否合法")
    {}
}

/// <summary>
/// 内存溢出错误
/// </summary>
public class OutOfMemoryError : RuntimeError
{
    public OutOfMemoryError(IOldLangTree node) 
        : base(
            node, 
            "内存溢出",
            "程序使用了过多内存，请检查是否存在内存泄漏或优化内存使用")
    {}
    
    public OutOfMemoryError(IOldLangTree node, string message) 
        : base(
            node, 
            $"内存溢出: {message}",
            "程序使用了过多内存，请检查是否存在内存泄漏或优化内存使用")
    {}
}

/// <summary>
/// 数值溢出错误
/// </summary>
public class OverflowError : RuntimeError
{
    public OverflowError(IOldLangTree node, string operation) 
        : base(
            node, 
            $"数值溢出: {operation}",
            "数值运算结果超过了数据类型的范围")
    {}
    
    public OverflowError(IOldLangTree node, string operation, long value) 
        : base(
            node, 
            $"数值溢出: {operation} 结果 {value} 超过了数据类型的范围",
            "数值运算结果超过了数据类型的范围")
    {}
}