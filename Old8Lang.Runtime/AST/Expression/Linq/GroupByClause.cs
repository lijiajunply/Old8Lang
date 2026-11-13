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
        // GroupByClause 是 LINQ 查询的内部组成部分，不应该被独立访问
        throw new InvalidOperationException("GroupByClause 不应该被独立访问，应该通过 LinqExpression 处理");
    }
}