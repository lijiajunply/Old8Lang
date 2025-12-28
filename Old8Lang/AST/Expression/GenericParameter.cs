using Old8Lang.AST;

namespace Old8Lang.AST.Expression;

/// <summary>
/// 泛型参数定义
/// 例如: <T: IComparable> 中的 T
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
    /// 源代码位置
    /// </summary>
    public SourcePosition Position { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    public GenericParameter(string name, List<string>? constraints = null, SourcePosition position = default)
    {
        Name = name;
        Constraints = constraints;
        Position = position;
    }

    /// <summary>
    /// 是否有约束
    /// </summary>
    public bool HasConstraints => Constraints is { Count: > 0 };

    /// <summary>
    /// 获取约束的可读字符串
    /// </summary>
    public string GetConstraintsString()
    {
        if (!HasConstraints) return "";
        return string.Join(" | ", Constraints!);
    }

    public override string ToString()
    {
        if (HasConstraints)
        {
            return $"{Name}: {GetConstraintsString()}";
        }
        return Name;
    }
}
