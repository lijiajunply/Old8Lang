using Old8Lang.AST.Visitor;
using Old8Lang.Compiler;
using Old8Lang.Interpreter;
using System.Reflection.Emit;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;

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
        ConvertToEnumerable(ilGenerator, local);

        // 3. 处理查询体子句（where, orderby, let 等）
        foreach (var clause in BodyClauses)
        {
            GenerateClauseIL(clause, ilGenerator, local);
        }

        // 4. 处理终止子句（select 或 group）
        GenerateTerminationClauseIL(TerminationClause, ilGenerator, local);

        // 5. 转换结果为 ListLangValue
        ConvertToListLangValue(ilGenerator, local);
    }

    /// <summary>
    /// 将值转换为 IEnumerable
    /// </summary>
    private void ConvertToEnumerable(ILGenerator ilGenerator, LocalManager local)
    {
        // 调用 LinqQueryExecutor.ConvertToEnumerable 的等价逻辑
        // 简化实现：假设数据源已经是 ILangList 或 ArrayLangValue

        // 检查是否是 ILangList
        var notListLabel = ilGenerator.DefineLabel();
        var endLabel = ilGenerator.DefineLabel();

        ilGenerator.Emit(OpCodes.Dup);
        ilGenerator.Emit(OpCodes.Isinst, typeof(ILangList));
        ilGenerator.Emit(OpCodes.Brfalse_S, notListLabel);

        // 如果是 ILangList，调用 GetItems()
        ilGenerator.Emit(OpCodes.Castclass, typeof(ILangList));
        ilGenerator.Emit(OpCodes.Callvirt, typeof(ILangList).GetMethod("GetItems")!);
        ilGenerator.Emit(OpCodes.Br_S, endLabel);

        // 如果不是 ILangList，检查是否是 ArrayLangValue
        ilGenerator.MarkLabel(notListLabel);
        ilGenerator.Emit(OpCodes.Dup);
        ilGenerator.Emit(OpCodes.Isinst, typeof(ArrayLangValue));
        var notArrayLabel = ilGenerator.DefineLabel();
        ilGenerator.Emit(OpCodes.Brfalse_S, notArrayLabel);

        // 如果是 ArrayLangValue，获取 Values 属性
        ilGenerator.Emit(OpCodes.Castclass, typeof(ArrayLangValue));
        ilGenerator.Emit(OpCodes.Callvirt, typeof(ArrayLangValue).GetProperty("Values")!.GetMethod!);
        ilGenerator.Emit(OpCodes.Br_S, endLabel);

        // 如果都不是，抛出异常
        ilGenerator.MarkLabel(notArrayLabel);
        ilGenerator.Emit(OpCodes.Pop);
        ilGenerator.Emit(OpCodes.Ldstr, "无法将数据源转换为 IEnumerable");
        ilGenerator.Emit(OpCodes.Newobj, typeof(InvalidOperationException).GetConstructor([typeof(string)])!);
        ilGenerator.Emit(OpCodes.Throw);

        ilGenerator.MarkLabel(endLabel);
    }

    public override Type? OutputType(LocalManager local)
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