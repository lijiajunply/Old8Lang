using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Expression.Linq;

/// <summary>
/// LINQ Select 子句
/// 例如: select x * 2
/// </summary>
public class SelectClause(LangExpression projection, SourcePosition position = default)
    : LinqClause(position)
{
    /// <summary>
    /// 投影表达式
    /// </summary>
    public LangExpression Projection { get; set; } = projection;

    public override TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        throw new NotImplementedException();
    }
}