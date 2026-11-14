using Old8Lang.AST.Visitor;


namespace Old8Lang.AST.Expression.Intermediates;

/// <summary>
/// Match 表达式节点
/// 语法: match value { case pattern -> expression ... }
/// </summary>
public partial class MatchExpression(
    LangExpression matchExpression,
    List<MatchCase> matchCases,
    SourcePosition position = default)
    : LangExpression(position)
{
    /// <summary>
    /// 被匹配的表达式
    /// </summary>
    public LangExpression MatchValue { get; } = matchExpression;

    /// <summary>
    /// case 分支列表
    /// </summary>
    public List<MatchCase> Cases { get; } = matchCases;

    public override TResult Accept<TResult>(IVisitor<TResult> visitor) => visitor.VisitMatchExpression(this);
}
