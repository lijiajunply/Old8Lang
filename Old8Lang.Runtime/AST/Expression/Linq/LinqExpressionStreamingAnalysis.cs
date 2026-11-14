namespace Old8Lang.AST.Expression.Linq;

/// <summary>
/// LINQ 查询能否走流式 IL 生成的判定。
/// </summary>
/// <remarks>
/// 该判定同时被 IL 后端（生成流式代码）与 Runtime 的类型推断使用，因此保留在共享层；
/// 具体 IL 生成位于 <c>Old8Lang.Compiler</c> 的 <c>LinqExpressionRuntimeExtensions</c>。
/// </remarks>
internal static class LinqExpressionStreamingAnalysis
{
    /// <summary>
    /// 仅由 let/where 子句与 select 终止子句组成、且没有 continuation 的查询可流式生成。
    /// </summary>
    internal static bool CanGenerateStreamingQuery(this LinqExpression node) =>
        node.Continuation is null && node.TerminationClause is SelectClause &&
        node.BodyClauses.Take(node.BodyClauses.LastOrDefault() is OrderByClause
                ? node.BodyClauses.Count - 1
                : node.BodyClauses.Count)
            .All(clause => clause is LetClause or WhereClause);
}
