namespace Old8Lang.AST.Expression;

/// <summary>
/// 泛型参数定义
/// 例如: <T: IComparable> 中的 T
/// 或: <T?: IComparable> 中的可空类型参数 T?
/// </summary>
public class GenericParameter
{
    /// <summary>
    /// 类型参数名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 类型约束列表（接口或基类名称）
    /// 例如: T: IComparable | ICloneable → ["IComparable", "ICloneable"]
    /// </summary>
    public List<string>? Constraints { get; }

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
    /// 构造函数
    /// </summary>
    public GenericParameter(string name, List<string>? constraints = null, SourcePosition position = default, bool isNullable = false)
    {
        Name = name;
        Constraints = constraints;
        Position = position;
        IsNullable = isNullable;
    }

    /// <summary>
    /// 是否有约束
    /// </summary>
    public bool HasConstraints => Constraints is { Count: > 0 };

    /// <summary>
    /// 获取约束的可读字符串
    /// </summary>
    /// <param name="usePipe">是否使用 | 分隔符（默认），false 则使用 &</param>
    public string GetConstraintsString(bool usePipe = true)
    {
        if (!HasConstraints) return "";
        string separator = usePipe ? " | " : " & ";
        return string.Join(separator, Constraints!);
    }

    public override string ToString()
    {
        var nullableMarker = IsNullable ? "?" : "";
        if (HasConstraints)
        {
            return $"{Name}{nullableMarker}: {GetConstraintsString()}";
        }
        return Name + nullableMarker;
    }
}
