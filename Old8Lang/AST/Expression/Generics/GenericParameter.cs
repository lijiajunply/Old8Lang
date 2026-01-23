namespace Old8Lang.AST.Expression;

/// <summary>
/// 泛型参数定义
/// 例如: &lt;T: IComparable> 中的 T
/// 或: &lt;T?: IComparable> 中的可空类型参数 T?
/// 或: &lt;T: new() & class & IComparable> 中的多约束类型参数
/// </summary>
public class GenericParameter
{
    /// <summary>
    /// 类型参数名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 类型约束列表（接口或基类名称）- 向后兼容
    /// 例如: T: IComparable | ICloneable → ["IComparable", "ICloneable"]
    /// </summary>
    public List<string>? Constraints { get; }

    /// <summary>
    /// 结构化约束列表（新版本）
    /// 支持 new()、class、struct、类型名称和类型参数约束
    /// </summary>
    public List<GenericConstraint>? StructuredConstraints { get; private set; }

    /// <summary>
    /// 是否为可空类型参数
    /// 例如: T? → true, T → false
    /// </summary>
    public bool IsNullable { get; }

    /// <summary>
    /// 源代码位置
    /// </summary>
    public SourcePosition Position { get; }

    /// <summary>
    /// 构造函数（向后兼容）
    /// </summary>
    public GenericParameter(string name, List<string>? constraints = null, SourcePosition position = default, bool isNullable = false)
    {
        Name = name;
        Constraints = constraints;
        Position = position;
        IsNullable = isNullable;

        // 将字符串约束转换为结构化约束
        if (constraints is { Count: > 0 })
        {
            StructuredConstraints = constraints
                .Select(c => GenericConstraint.Parse(c, null, position))
                .ToList();
        }
    }

    /// <summary>
    /// 构造函数（使用结构化约束）
    /// </summary>
    public GenericParameter(string name, List<GenericConstraint>? structuredConstraints, SourcePosition position, bool isNullable)
    {
        Name = name;
        StructuredConstraints = structuredConstraints;
        Position = position;
        IsNullable = isNullable;

        // 同时维护字符串约束列表以保持向后兼容
        if (structuredConstraints is { Count: > 0 })
        {
            Constraints = structuredConstraints.Select(c => c.ToString()).ToList();
        }
    }

    /// <summary>
    /// 是否有约束
    /// </summary>
    public bool HasConstraints => Constraints is { Count: > 0 } || StructuredConstraints is { Count: > 0 };

    /// <summary>
    /// 是否有 new() 约束
    /// </summary>
    public bool HasNewConstraint => StructuredConstraints?.Any(c => c.Kind == GenericConstraintKind.New) ?? false;

    /// <summary>
    /// 是否有 class 约束
    /// </summary>
    public bool HasClassConstraint => StructuredConstraints?.Any(c => c.Kind == GenericConstraintKind.Class) ?? false;

    /// <summary>
    /// 是否有 struct 约束
    /// </summary>
    public bool HasStructConstraint => StructuredConstraints?.Any(c => c.Kind == GenericConstraintKind.Struct) ?? false;

    /// <summary>
    /// 获取所有类型名称约束
    /// </summary>
    public IEnumerable<string> TypeNameConstraints =>
        StructuredConstraints?
            .Where(c => c.Kind == GenericConstraintKind.TypeName)
            .Select(c => c.TypeName!)
        ?? Enumerable.Empty<string>();

    /// <summary>
    /// 获取所有类型参数约束
    /// </summary>
    public IEnumerable<string> TypeParameterConstraints =>
        StructuredConstraints?
            .Where(c => c.Kind == GenericConstraintKind.TypeParameter)
            .Select(c => c.TypeName!)
        ?? Enumerable.Empty<string>();

    /// <summary>
    /// 添加结构化约束
    /// </summary>
    public void AddStructuredConstraint(GenericConstraint constraint)
    {
        StructuredConstraints ??= [];
        StructuredConstraints.Add(constraint);

        // 同步更新字符串约束列表
        if (Constraints is not null)
        {
            Constraints.Add(constraint.ToString());
        }
    }

    /// <summary>
    /// 设置结构化约束列表
    /// </summary>
    public void SetStructuredConstraints(List<GenericConstraint> constraints)
    {
        StructuredConstraints = constraints;
    }

    /// <summary>
    /// 验证约束是否有冲突
    /// </summary>
    /// <returns>如果有冲突返回错误消息，否则返回 null</returns>
    public string? ValidateConstraints()
    {
        if (StructuredConstraints is null || StructuredConstraints.Count < 2)
            return null;

        for (int i = 0; i < StructuredConstraints.Count; i++)
        {
            for (int j = i + 1; j < StructuredConstraints.Count; j++)
            {
                if (StructuredConstraints[i].ConflictsWith(StructuredConstraints[j]))
                {
                    return $"泛型参数 '{Name}' 的约束 '{StructuredConstraints[i]}' 与 '{StructuredConstraints[j]}' 冲突";
                }
            }
        }

        return null;
    }

    /// <summary>
    /// 获取约束的可读字符串
    /// </summary>
    /// <param name="usePipe">是否使用 | 分隔符（默认），false 则使用 &</param>
    public string GetConstraintsString(bool usePipe = true)
    {
        if (StructuredConstraints is { Count: > 0 })
        {
            string separator = usePipe ? " | " : " & ";
            return string.Join(separator, StructuredConstraints.Select(c => c.ToString()));
        }

        if (!HasConstraints) return "";
        string sep = usePipe ? " | " : " & ";
        return string.Join(sep, Constraints!);
    }

    public override string ToString()
    {
        var nullableMarker = IsNullable ? "?" : "";
        if (HasConstraints)
        {
            return $"{Name}{nullableMarker}: {GetConstraintsString(false)}";
        }
        return Name + nullableMarker;
    }
}
