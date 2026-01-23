namespace Old8Lang.AST.Expression.Intermediates;

/// <summary>
/// 元组解构模式
/// 用于 match 表达式中的元组解构匹配
/// 例如：case (0, 0) -> "origin"
///      case (x, 0) -> "on X-axis"
///      case (x, y) -> "point"
/// </summary>
public class TuplePattern(List<TuplePatternElement> elements)
{
    /// <summary>
    /// 元组模式的元素列表
    /// </summary>
    public List<TuplePatternElement> Elements { get; } = elements;
}

/// <summary>
/// 元组模式中的单个元素
/// </summary>
public class TuplePatternElement
{
    /// <summary>
    /// 匹配的值表达式（null 表示变量绑定或通配符）
    /// </summary>
    public LangExpression? Value { get; }

    /// <summary>
    /// 绑定的变量名（如果是变量绑定模式）
    /// </summary>
    public string? Variable { get; }

    /// <summary>
    /// 是否是通配符模式 (_)
    /// </summary>
    public bool IsWildcard { get; }

    /// <summary>
    /// 构造函数 - 值匹配模式
    /// </summary>
    public TuplePatternElement(LangExpression value)
    {
        Value = value;
        Variable = null;
        IsWildcard = false;
    }

    /// <summary>
    /// 构造函数 - 变量绑定或通配符模式
    /// </summary>
    public TuplePatternElement(string variable, bool isWildcard = false)
    {
        Variable = variable;
        IsWildcard = isWildcard;
        Value = null;
    }
}
