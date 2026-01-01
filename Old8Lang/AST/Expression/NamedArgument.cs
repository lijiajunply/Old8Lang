namespace Old8Lang.AST.Expression;

/// <summary>
/// 命名参数，用于函数调用时指定参数名称
/// 例如：f(name: "Alice", age: 25)
/// </summary>
public class NamedArgument
{
    /// <summary>
    /// 参数名称
    /// </summary>
    public readonly string Name;

    /// <summary>
    /// 参数值表达式
    /// </summary>
    public readonly LangExpression Value;

    /// <summary>
    /// 源代码位置信息
    /// </summary>
    public readonly SourcePosition Position;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="name">参数名称</param>
    /// <param name="value">参数值表达式</param>
    /// <param name="position">源代码位置信息</param>
    public NamedArgument(string name, LangExpression value, SourcePosition position = default)
    {
        Name = name;
        Value = value;
        Position = position;
    }

    public override string ToString()
    {
        return $"{Name}: {Value}";
    }
}
