namespace Old8Lang.AST.Expression;

/// <summary>
/// 泛型约束类型枚举
/// </summary>
public enum GenericConstraintKind
{
    /// <summary>
    /// 类型名称约束（接口或基类）
    /// 例如: T: IComparable
    /// </summary>
    TypeName,

    /// <summary>
    /// new() 约束 - 要求类型有无参构造函数
    /// 例如: T: new()
    /// </summary>
    New,

    /// <summary>
    /// class 约束 - 要求类型是引用类型
    /// 例如: T: class
    /// </summary>
    Class,

    /// <summary>
    /// struct 约束 - 要求类型是值类型
    /// 例如: T: struct
    /// </summary>
    Struct,

    /// <summary>
    /// 类型参数约束 - 要求 T 派生自另一个类型参数 U
    /// 例如: T: U (在 where 子句中)
    /// </summary>
    TypeParameter
}

/// <summary>
/// 泛型约束定义
/// 表示单个泛型约束，如 new()、class、struct、IComparable 或类型参数约束
/// </summary>
public class GenericConstraint
{
    /// <summary>
    /// 约束类型
    /// </summary>
    public GenericConstraintKind Kind { get; }

    /// <summary>
    /// 类型名称（用于 TypeName 和 TypeParameter 约束）
    /// 对于 TypeName: 接口或基类名称，如 "IComparable"
    /// 对于 TypeParameter: 类型参数名称，如 "U"
    /// 对于其他约束类型: null
    /// </summary>
    public string? TypeName { get; }

    /// <summary>
    /// 源代码位置
    /// </summary>
    public SourcePosition Position { get; }

    /// <summary>
    /// 私有构造函数，使用工厂方法创建实例
    /// </summary>
    private GenericConstraint(GenericConstraintKind kind, string? typeName, SourcePosition position)
    {
        Kind = kind;
        TypeName = typeName;
        Position = position;
    }

    /// <summary>
    /// 创建 new() 约束
    /// </summary>
    public static GenericConstraint CreateNew(SourcePosition position = default)
    {
        return new GenericConstraint(GenericConstraintKind.New, null, position);
    }

    /// <summary>
    /// 创建 class 约束
    /// </summary>
    public static GenericConstraint CreateClass(SourcePosition position = default)
    {
        return new GenericConstraint(GenericConstraintKind.Class, null, position);
    }

    /// <summary>
    /// 创建 struct 约束
    /// </summary>
    public static GenericConstraint CreateStruct(SourcePosition position = default)
    {
        return new GenericConstraint(GenericConstraintKind.Struct, null, position);
    }

    /// <summary>
    /// 创建类型名称约束（接口或基类）
    /// </summary>
    /// <param name="typeName">接口或基类名称</param>
    /// <param name="position">源代码位置</param>
    public static GenericConstraint CreateTypeName(string typeName, SourcePosition position = default)
    {
        if (string.IsNullOrEmpty(typeName))
        {
            throw new ArgumentException("类型名称不能为空", nameof(typeName));
        }
        return new GenericConstraint(GenericConstraintKind.TypeName, typeName, position);
    }

    /// <summary>
    /// 创建类型参数约束（T: U）
    /// </summary>
    /// <param name="typeParameterName">类型参数名称</param>
    /// <param name="position">源代码位置</param>
    public static GenericConstraint CreateTypeParameter(string typeParameterName, SourcePosition position = default)
    {
        if (string.IsNullOrEmpty(typeParameterName))
        {
            throw new ArgumentException("类型参数名称不能为空", nameof(typeParameterName));
        }
        return new GenericConstraint(GenericConstraintKind.TypeParameter, typeParameterName, position);
    }

    /// <summary>
    /// 从字符串解析约束（用于向后兼容）
    /// </summary>
    /// <param name="constraintString">约束字符串</param>
    /// <param name="genericParamNames">当前泛型参数名称集合（用于判断是否为类型参数约束）</param>
    /// <param name="position">源代码位置</param>
    public static GenericConstraint Parse(string constraintString, HashSet<string>? genericParamNames = null, SourcePosition position = default)
    {
        var trimmed = constraintString.Trim();

        // 检查特殊约束
        if (trimmed.Equals("new()", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Equals("new", StringComparison.OrdinalIgnoreCase))
        {
            return CreateNew(position);
        }

        if (trimmed.Equals("class", StringComparison.OrdinalIgnoreCase))
        {
            return CreateClass(position);
        }

        if (trimmed.Equals("struct", StringComparison.OrdinalIgnoreCase))
        {
            return CreateStruct(position);
        }

        // 检查是否为类型参数约束
        if (genericParamNames != null && genericParamNames.Contains(trimmed))
        {
            return CreateTypeParameter(trimmed, position);
        }

        // 默认为类型名称约束
        return CreateTypeName(trimmed, position);
    }

    /// <summary>
    /// 获取约束的字符串表示
    /// </summary>
    public override string ToString()
    {
        return Kind switch
        {
            GenericConstraintKind.New => "new()",
            GenericConstraintKind.Class => "class",
            GenericConstraintKind.Struct => "struct",
            GenericConstraintKind.TypeName => TypeName ?? "",
            GenericConstraintKind.TypeParameter => TypeName ?? "",
            _ => ""
        };
    }

    /// <summary>
    /// 检查约束是否与另一个约束冲突
    /// </summary>
    /// <param name="other">另一个约束</param>
    /// <returns>如果冲突返回 true</returns>
    public bool ConflictsWith(GenericConstraint other)
    {
        // class 和 struct 约束互斥
        if ((Kind == GenericConstraintKind.Class && other.Kind == GenericConstraintKind.Struct) ||
            (Kind == GenericConstraintKind.Struct && other.Kind == GenericConstraintKind.Class))
        {
            return true;
        }

        // struct 和 new() 约束冲突（struct 隐含 new()）
        if ((Kind == GenericConstraintKind.Struct && other.Kind == GenericConstraintKind.New) ||
            (Kind == GenericConstraintKind.New && other.Kind == GenericConstraintKind.Struct))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 检查约束是否为特殊约束（new、class、struct）
    /// </summary>
    public bool IsSpecialConstraint => Kind is GenericConstraintKind.New or GenericConstraintKind.Class or GenericConstraintKind.Struct;

    /// <summary>
    /// 检查约束是否为类型约束（TypeName 或 TypeParameter）
    /// </summary>
    public bool IsTypeConstraint => Kind is GenericConstraintKind.TypeName or GenericConstraintKind.TypeParameter;
}
