using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Expression.Linq;

/// <summary>
/// LINQ Where 子句
/// 例如: where x > 5
/// </summary>
public class WhereClause(LangExpression condition, SourcePosition position = default)
    : LinqClause(position)
{
    /// <summary>
    /// 过滤条件表达式
    /// </summary>
    public LangExpression Condition { get; set; } = condition;

    public override TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        throw new NotImplementedException();
    }
}