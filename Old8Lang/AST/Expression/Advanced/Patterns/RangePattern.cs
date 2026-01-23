namespace Old8Lang.AST.Expression.Intermediates;

/// <summary>
/// 范围匹配模式
/// 用于 match 表达式中的范围匹配
/// 例如：case [0~12] -> "child"
///      case [13~19] -> "teen"
///      case [20~64] -> "adult"
/// </summary>
public class RangePattern
{
    /// <summary>
    /// 范围起始值表达式
    /// </summary>
    public LangExpression Start { get; }

    /// <summary>
    /// 范围结束值表达式
    /// </summary>
    public LangExpression End { get; }

    /// <summary>
    /// 是否包含起始值（默认：true）
    /// </summary>
    public bool IncludeStart { get; }

    /// <summary>
    /// 是否包含结束值（默认：true）
    /// </summary>
    public bool IncludeEnd { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    public RangePattern(LangExpression start, LangExpression end, bool includeStart = true, bool includeEnd = true)
    {
        Start = start;
        End = end;
        IncludeStart = includeStart;
        IncludeEnd = includeEnd;
    }
}
