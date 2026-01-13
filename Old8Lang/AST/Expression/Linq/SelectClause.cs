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
        // SelectClause 是 LINQ 查询的内部组成部分，不应该被独立访问
        throw new InvalidOperationException("SelectClause 不应该被独立访问，应该通过 LinqExpression 处理");
    }
}