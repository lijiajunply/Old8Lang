using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Expression.Linq;

/// <summary>
/// LINQ 查询延续（into 子句）
/// 例如: ... into g select g.Key
/// </summary>
public class QueryContinuation(
    string variable,
    List<LinqClause> bodyClauses,
    LinqClause terminationClause,
    SourcePosition position = default)
    : IOldLangTree
{
    /// <summary>
    /// 延续变量名
    /// </summary>
    public string Variable { get; set; } = variable;

    /// <summary>
    /// 延续后的查询体子句
    /// </summary>
    public List<LinqClause> BodyClauses { get; set; } = bodyClauses;

    /// <summary>
    /// 延续后的终止子句
    /// </summary>
    public LinqClause TerminationClause { get; set; } = terminationClause;

    public SourcePosition Position { get; set; } = position;

    public TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        throw new NotImplementedException();
    }
}
