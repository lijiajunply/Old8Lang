using Old8Lang.AST.Visitor;
using Old8Lang.Compiler;
using Old8Lang.Interpreter;
using System.Reflection.Emit;

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

    public override LangValueType Run(VariateManager manager)
    {
        var executor = new LinqQueryExecutor(manager);
        return executor.ExecuteQuery(this);
    }

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        throw new NotImplementedException("LINQ 查询表达式的 LoadIlValue 方法尚未实现");
    }

    public override Type? OutputType(LocalManager local)
    {
        // LINQ 查询返回 IEnumerable<T>
        return typeof(System.Collections.IEnumerable);
    }
}