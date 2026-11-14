
using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Expression.Linq;

/// <summary>
/// LINQ 查询表达式
/// </summary>
public partial class LinqExpression(
    FromClause fromClause,
    List<LinqClause> bodyClauses,
    LinqClause terminationClause,
    QueryContinuation? continuation = null,
    SourcePosition position = default)
    : LangExpression(position)
{

    /// <summary>
    /// From 子句
    /// </summary>
    public FromClause FromClause { get; set; } = fromClause;

    /// <summary>
    /// 查询体子句列表（where, orderby, let, join 等）
    /// </summary>
    public List<LinqClause> BodyClauses { get; set; } = bodyClauses;

    /// <summary>
    /// 终止子句（select 或 group）
    /// </summary>
    public LinqClause TerminationClause { get; set; } = terminationClause;

    /// <summary>
    /// 查询延续（into 子句）
    /// </summary>
    public QueryContinuation? Continuation { get; set; } = continuation;

    public override TResult Accept<TResult>(IVisitor<TResult> visitor) => visitor.VisitLinqExpression(this);




}
