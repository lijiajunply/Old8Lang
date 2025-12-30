using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Expression.Linq;

/// <summary>
/// LINQ Join 子句
/// 例如: join b in list2 on a.id equals b.id
/// </summary>
public class JoinClause(
    string rangeVariable,
    LangExpression innerDataSource,
    LangExpression outerKeyExpression,
    LangExpression innerKeyExpression,
    string? typeAnnotation = null,
    bool isGroupJoin = false,
    string? groupVariable = null,
    SourcePosition position = default)
    : LinqClause(position)
{
    /// <summary>
    /// 连接范围变量名
    /// </summary>
    public string RangeVariable { get; set; } = rangeVariable;

    /// <summary>
    /// 内部数据源表达式
    /// </summary>
    public LangExpression InnerDataSource { get; set; } = innerDataSource;

    /// <summary>
    /// 外部键表达式
    /// </summary>
    public LangExpression OuterKeyExpression { get; set; } = outerKeyExpression;

    /// <summary>
    /// 内部键表达式
    /// </summary>
    public LangExpression InnerKeyExpression { get; set; } = innerKeyExpression;

    /// <summary>
    /// 可选的类型注解
    /// </summary>
    public string? TypeAnnotation { get; set; } = typeAnnotation;

    /// <summary>
    /// 是否为 group join（into 子句）
    /// </summary>
    public bool IsGroupJoin { get; set; } = isGroupJoin;

    /// <summary>
    /// Group join 的结果变量名（如果 IsGroupJoin 为 true）
    /// </summary>
    public string? GroupVariable { get; set; } = groupVariable;

    public override TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        throw new NotImplementedException();
    }
}