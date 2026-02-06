using Old8Lang.Interpreter;
using System.Reflection.Emit;
using Old8Lang.Compiler.CodeGeneration;

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
        // 在编译器模式下，LINQ 查询需要生成 IL 代码来执行查询逻辑
        // 由于编译器模式下无法直接访问 VariateManager（生成的方法是无参数的 Action），
        // 我们需要通过 LocalManager.Interpreter.Manager 来访问

        // 检查是否有 Interpreter
        if (local.Interpreter?.Manager == null)
        {
            throw new NotSupportedException("LINQ 查询在编译器模式下需要 Interpreter 支持");
        }

        // 将 LINQ 查询转换为手动的循环和过滤逻辑
        GenerateLinqMethodCalls(ilGenerator, local);
    }

    /// <summary>
    /// 生成 LINQ 方法调用链的 IL 代码
    /// </summary>
    private void GenerateLinqMethodCalls(ILGenerator ilGenerator, LocalManager local)
    {
        // 1. 加载数据源
        FromClause.DataSource.LoadIlValue(ilGenerator, local);

        // 2. 转换为 IEnumerable
        ConvertToEnumerable(ilGenerator);

        // 3. 处理查询体子句（where, orderby, let, join 等）
        foreach (var clause in BodyClauses)
        {
            GenerateClauseIL(clause, ilGenerator, local);
        }

        // 4. 处理终止子句（select 或 group）
        GenerateTerminationClauseIL(TerminationClause, ilGenerator, local);

        // 5. 处理查询延续（into 子句）
        if (Continuation != null)
        {
            GenerateContinuationIL(Continuation, ilGenerator, local);
        }

        // 6. 转换结果为 ListLangValue
        ConvertToListLangValue(ilGenerator, local);
    }

    /// <summary>
    /// 将值转换为 IEnumerable
    /// </summary>
    private void ConvertToEnumerable(ILGenerator ilGenerator)
    {
        // 使用辅助方法进行转换，简化 IL 代码生成
        ilGenerator.Emit(OpCodes.Call, typeof(LinqCompilerHelper).GetMethod("ConvertToEnumerable")!);
    }

    public override Type OutputType(LocalManager local)
    {
        // LINQ 查询返回 IEnumerable<T>
        return typeof(System.Collections.IEnumerable);
    }

    /// <summary>
    /// 生成查询体子句的 IL 代码
    /// </summary>
    private void GenerateClauseIL(LinqClause clause, ILGenerator ilGenerator, LocalManager local)
    {
        switch (clause)
        {
            case WhereClause whereClause:
                GenerateWhereClauseIL(whereClause, ilGenerator, local);
                break;
            case OrderByClause orderByClause:
                GenerateOrderByClauseIL(orderByClause, ilGenerator, local);
                break;
            case LetClause letClause:
                GenerateLetClauseIL(letClause, ilGenerator, local);
                break;
            case JoinClause joinClause:
                GenerateJoinClauseIL(joinClause, ilGenerator, local);
                break;
            default:
                throw new NotSupportedException($"不支持的 LINQ 子句类型: {clause.GetType().Name}");
        }
    }

    /// <summary>
    /// 生成终止子句的 IL 代码
    /// </summary>
    private void GenerateTerminationClauseIL(LinqClause clause, ILGenerator ilGenerator, LocalManager local)
    {
        switch (clause)
        {
            case SelectClause selectClause:
                GenerateSelectClauseIL(selectClause, ilGenerator, local);
                break;
            case GroupByClause groupByClause:
                GenerateGroupByClauseIL(groupByClause, ilGenerator, local);
                break;
            default:
                throw new NotSupportedException($"不支持的终止子句类型: {clause.GetType().Name}");
        }
    }
}