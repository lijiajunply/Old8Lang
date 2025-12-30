using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Expression.Linq;

/// <summary>
/// LINQ Group By 子句
/// 例如: group x by x.Category
/// </summary>
public class GroupByClause(
    LangExpression elementExpression,
    LangExpression keyExpression,
    SourcePosition position = default)
    : LinqClause(position)
{
    /// <summary>
    /// 分组元素表达式
    /// </summary>
    public LangExpression ElementExpression { get; set; } = elementExpression;

    /// <summary>
    /// 分组键表达式
    /// </summary>
    public LangExpression KeyExpression { get; set; } = keyExpression;

    public override TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        throw new NotImplementedException();
    }
}