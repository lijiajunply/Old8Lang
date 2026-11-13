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
        // WhereClause 是 LINQ 查询的内部组成部分，不应该被独立访问
        throw new InvalidOperationException("WhereClause 不应该被独立访问，应该通过 LinqExpression 处理");
    }
}