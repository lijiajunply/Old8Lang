using Old8Lang.AST;

namespace Old8Lang.Error;

/// <summary>
/// 类未找到错误
/// </summary>
public class ClassNotFoundError : RuntimeError
{
    /// <summary>
    /// 类未找到错误代码
    /// </summary>
    public new const string ErrorCode = "CLASS_NOT_FOUND_ERROR";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="className">类名</param>
    public ClassNotFoundError(IOldLangTree node, string className)
        : base(
            node,
            ErrorCode,
            $"类 '{className}' 未找到",
            "请检查类名是否正确，或者类是否已定义")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="className">类名</param>
    /// <param name="suggestion">建议</param>
    public ClassNotFoundError(IOldLangTree node, string className, string suggestion)
        : base(
            node,
            ErrorCode,
            $"类 '{className}' 未找到",
            suggestion)
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="className">类名</param>
    public ClassNotFoundError(SourcePosition position, string className)
        : base(
            position,
            ErrorCode,
            $"类 '{className}' 未找到",
            "请检查类名是否正确，或者类是否已定义")
    {
    }
}

/// <summary>
/// 方法未找到错误
/// </summary>
public class MethodNotFoundError : RuntimeError
{
    /// <summary>
    /// 方法未找到错误代码
    /// </summary>
    public new const string ErrorCode = "METHOD_NOT_FOUND_ERROR";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="methodName">方法名</param>
    /// <param name="className">类名</param>
    public MethodNotFoundError(IOldLangTree node, string methodName, string className)
        : base(
            node,
            ErrorCode,
            $"类 '{className}' 中未找到方法 '{methodName}'",
            "请检查方法名是否正确，或者方法是否已定义")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="methodName">方法名</param>
    public MethodNotFoundError(IOldLangTree node, string methodName)
        : base(
            node,
            ErrorCode,
            $"方法 '{methodName}' 未找到",
            "请检查方法名是否正确，或者方法是否已定义")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="methodName">方法名</param>
    /// <param name="className">类名</param>
    public MethodNotFoundError(SourcePosition position, string methodName, string className)
        : base(
            position,
            ErrorCode,
            $"类 '{className}' 中未找到方法 '{methodName}'",
            "请检查方法名是否正确，或者方法是否已定义")
    {
    }
}

/// <summary>
/// 访问权限违规错误
/// </summary>
public class AccessViolationError : RuntimeError
{
    /// <summary>
    /// 访问权限违规错误代码
    /// </summary>
    public new const string ErrorCode = "ACCESS_VIOLATION_ERROR";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="memberName">成员名称</param>
    /// <param name="className">类名</param>
    /// <param name="accessModifier">访问修饰符</param>
    public AccessViolationError(IOldLangTree node, string memberName, string className, string accessModifier)
        : base(
            node,
            ErrorCode,
            $"无法访问类 '{className}' 的 {accessModifier} 成员 '{memberName}'",
            "请检查成员的访问权限，或使用反射进行访问")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="message">错误信息</param>
    public AccessViolationError(IOldLangTree node, string message)
        : base(
            node,
            ErrorCode,
            message,
            "请检查访问权限是否正确")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="memberName">成员名称</param>
    /// <param name="className">类名</param>
    /// <param name="accessModifier">访问修饰符</param>
    public AccessViolationError(SourcePosition position, string memberName, string className, string accessModifier)
        : base(
            position,
            ErrorCode,
            $"无法访问类 '{className}' 的 {accessModifier} 成员 '{memberName}'",
            "请检查成员的访问权限，或使用反射进行访问")
    {
    }
}

/// <summary>
/// 构造函数错误
/// </summary>
public class ConstructorError : RuntimeError
{
    /// <summary>
    /// 构造函数错误代码
    /// </summary>
    public new const string ErrorCode = "CONSTRUCTOR_ERROR";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="className">类名</param>
    /// <param name="message">错误信息</param>
    public ConstructorError(IOldLangTree node, string className, string message)
        : base(
            node,
            ErrorCode,
            $"类 '{className}' 的构造函数错误: {message}",
            "请检查构造函数的参数是否正确")
    {
    }

    /// <summary>
    /// 构造函数 - 参数数量不匹配
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="className">类名</param>
    /// <param name="expectedCount">期望参数数量</param>
    /// <param name="actualCount">实际参数数量</param>
    public ConstructorError(IOldLangTree node, string className, int expectedCount, int actualCount)
        : base(
            node,
            ErrorCode,
            $"类 '{className}' 的构造函数期望 {expectedCount} 个参数，但提供了 {actualCount} 个",
            "请检查构造函数的参数数量")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="className">类名</param>
    /// <param name="message">错误信息</param>
    public ConstructorError(SourcePosition position, string className, string message)
        : base(
            position,
            ErrorCode,
            $"类 '{className}' 的构造函数错误: {message}",
            "请检查构造函数的参数是否正确")
    {
    }
}

/// <summary>
/// 静态成员访问错误
/// </summary>
public class StaticMemberError : RuntimeError
{
    /// <summary>
    /// 静态成员访问错误代码
    /// </summary>
    public new const string ErrorCode = "STATIC_MEMBER_ERROR";

    /// <summary>
    /// 构造函数 - 通过实例访问静态成员
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="memberName">成员名称</param>
    /// <param name="className">类名</param>
    public StaticMemberError(IOldLangTree node, string memberName, string className)
        : base(
            node,
            ErrorCode,
            $"不能通过实例访问类 '{className}' 的静态成员 '{memberName}'",
            "请使用类名直接访问静态成员")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="message">错误信息</param>
    public StaticMemberError(IOldLangTree node, string message)
        : base(
            node,
            ErrorCode,
            message,
            "请检查静态成员的访问方式")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="memberName">成员名称</param>
    /// <param name="className">类名</param>
    public StaticMemberError(SourcePosition position, string memberName, string className)
        : base(
            position,
            ErrorCode,
            $"不能通过实例访问类 '{className}' 的静态成员 '{memberName}'",
            "请使用类名直接访问静态成员")
    {
    }
}

/// <summary>
/// 实例成员访问错误
/// </summary>
public class InstanceMemberError : RuntimeError
{
    /// <summary>
    /// 实例成员访问错误代码
    /// </summary>
    public new const string ErrorCode = "INSTANCE_MEMBER_ERROR";

    /// <summary>
    /// 构造函数 - 通过类名访问实例成员
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="memberName">成员名称</param>
    /// <param name="className">类名</param>
    public InstanceMemberError(IOldLangTree node, string memberName, string className)
        : base(
            node,
            ErrorCode,
            $"不能通过类名访问类 '{className}' 的实例成员 '{memberName}'",
            "请先创建类的实例，然后通过实例访问成员")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="node">AST节点</param>
    /// <param name="message">错误信息</param>
    public InstanceMemberError(IOldLangTree node, string message)
        : base(
            node,
            ErrorCode,
            message,
            "请检查实例成员的访问方式")
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="position">位置信息</param>
    /// <param name="memberName">成员名称</param>
    /// <param name="className">类名</param>
    public InstanceMemberError(SourcePosition position, string memberName, string className)
        : base(
            position,
            ErrorCode,
            $"不能通过类名访问类 '{className}' 的实例成员 '{memberName}'",
            "请先创建类的实例，然后通过实例访问成员")
    {
    }
}
