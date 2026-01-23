using Old8Lang.AST;

namespace Old8Lang.Error;

/// <summary>
/// 类型转换错误
/// </summary>
public class CastError : RuntimeError
{
    /// <summary>
    /// 类型转换错误代码
    /// </summary>
    public new const string ErrorCode = "CAST_ERROR";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="sourceType">源类型</param>
    /// <param name="targetType">目标类型</param>
    public CastError(IOldLangTree node, string sourceType, string targetType)
        : base(
            node,
            ErrorCode,
            $"无法将类型 '{sourceType}' 转换为 '{targetType}'",
            "请检查类型转换是否合法，或使用显式转换方法")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="sourceType">源类型</param>
    /// <param name="targetType">目标类型</param>
    /// <param name="reason">失败原因</param>
    public CastError(IOldLangTree node, string sourceType, string targetType, string reason)
        : base(
            node,
            ErrorCode,
            $"无法将类型 '{sourceType}' 转换为 '{targetType}': {reason}",
            "请检查类型转换是否合法，或使用显式转换方法")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="message">错误信息</param>
    public CastError(IOldLangTree node, string message)
        : base(
            node,
            ErrorCode,
            message,
            "请检查类型转换是否合法")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="sourceType">源类型</param>
    /// <param name="targetType">目标类型</param>
    public CastError(SourcePosition position, string sourceType, string targetType)
        : base(
            position,
            ErrorCode,
            $"无法将类型 '{sourceType}' 转换为 '{targetType}'",
            "请检查类型转换是否合法，或使用显式转换方法")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="sourceType">源类型</param>
    /// <param name="targetType">目标类型</param>
    /// <param name="reason">失败原因</param>
    public CastError(SourcePosition position, string sourceType, string targetType, string reason)
        : base(
            position,
            ErrorCode,
            $"无法将类型 '{sourceType}' 转换为 '{targetType}': {reason}",
            "请检查类型转换是否合法，或使用显式转换方法")
    {
    }
}

/// <summary>
/// 空引用错误
/// </summary>
public class NullReferenceError : RuntimeError
{
    /// <summary>
    /// 空引用错误代码
    /// </summary>
    public new const string ErrorCode = "NULL_REFERENCE_ERROR";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    public NullReferenceError(IOldLangTree node)
        : base(
            node,
            ErrorCode,
            "空引用错误: 尝试访问空对象的成员",
            "请在访问对象成员前检查对象是否为空")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="memberName">成员名称</param>
    public NullReferenceError(IOldLangTree node, string memberName)
        : base(
            node,
            ErrorCode,
            $"空引用错误: 尝试访问空对象的成员 '{memberName}'",
            "请在访问对象成员前检查对象是否为空")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="objectName">对象名称</param>
    /// <param name="memberName">成员名称</param>
    public NullReferenceError(IOldLangTree node, string objectName, string memberName)
        : base(
            node,
            ErrorCode,
            $"空引用错误: 对象 '{objectName}' 为空，无法访问成员 '{memberName}'",
            "请在访问对象成员前检查对象是否为空")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="message">错误信息</param>
    public NullReferenceError(SourcePosition position, string message)
        : base(
            position,
            ErrorCode,
            message,
            "请在访问对象成员前检查对象是否为空")
    {
    }
}

/// <summary>
/// 不支持的操作错误
/// </summary>
public class UnsupportedOperationError : RuntimeError
{
    /// <summary>
    /// 不支持的操作错误代码
    /// </summary>
    public new const string ErrorCode = "UNSUPPORTED_OPERATION_ERROR";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="operation">操作名称</param>
    /// <param name="typeName">类型名称</param>
    public UnsupportedOperationError(IOldLangTree node, string operation, string typeName)
        : base(
            node,
            ErrorCode,
            $"类型 '{typeName}' 不支持操作 '{operation}'",
            "请检查该类型是否支持此操作")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="operation">操作名称</param>
    /// <param name="leftType">左操作数类型</param>
    /// <param name="rightType">右操作数类型</param>
    public UnsupportedOperationError(IOldLangTree node, string operation, string leftType, string rightType)
        : base(
            node,
            ErrorCode,
            $"不支持类型 '{leftType}' 和 '{rightType}' 之间的 '{operation}' 操作",
            "请检查操作数类型是否兼容")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="message">错误信息</param>
    public UnsupportedOperationError(IOldLangTree node, string message)
        : base(
            node,
            ErrorCode,
            message,
            "请检查该操作是否被支持")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="operation">操作名称</param>
    /// <param name="typeName">类型名称</param>
    public UnsupportedOperationError(SourcePosition position, string operation, string typeName)
        : base(
            position,
            ErrorCode,
            $"类型 '{typeName}' 不支持操作 '{operation}'",
            "请检查该类型是否支持此操作")
    {
    }
}

/// <summary>
/// 类型推断错误
/// </summary>
public class TypeInferenceError : RuntimeError
{
    /// <summary>
    /// 类型推断错误代码
    /// </summary>
    public new const string ErrorCode = "TYPE_INFERENCE_ERROR";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="variableName">变量名称</param>
    public TypeInferenceError(IOldLangTree node, string variableName)
        : base(
            node,
            ErrorCode,
            $"无法推断变量 '{variableName}' 的类型",
            "请提供显式类型注解或初始值")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="message">错误信息</param>
    /// <param name="suggestion">建议</param>
    public TypeInferenceError(IOldLangTree node, string message, string suggestion)
        : base(
            node,
            ErrorCode,
            message,
            suggestion)
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="message">错误信息</param>
    public TypeInferenceError(SourcePosition position, string message)
        : base(
            position,
            ErrorCode,
            message,
            "请提供显式类型注解或初始值")
    {
    }
}

/// <summary>
/// 类型约束冲突错误
/// </summary>
public class TypeConstraintError : RuntimeError
{
    /// <summary>
    /// 类型约束冲突错误代码
    /// </summary>
    public new const string ErrorCode = "TYPE_CONSTRAINT_ERROR";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="variableName">变量名称</param>
    /// <param name="conflictingTypes">冲突的类型</param>
    public TypeConstraintError(IOldLangTree node, string variableName, string[] conflictingTypes)
        : base(
            node,
            ErrorCode,
            $"变量 '{variableName}' 存在类型约束冲突: {string.Join(", ", conflictingTypes)}",
            "请检查变量的使用是否一致")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="message">错误信息</param>
    public TypeConstraintError(IOldLangTree node, string message)
        : base(
            node,
            ErrorCode,
            message,
            "请检查类型约束是否一致")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="message">错误信息</param>
    public TypeConstraintError(SourcePosition position, string message)
        : base(
            position,
            ErrorCode,
            message,
            "请检查类型约束是否一致")
    {
    }
}
