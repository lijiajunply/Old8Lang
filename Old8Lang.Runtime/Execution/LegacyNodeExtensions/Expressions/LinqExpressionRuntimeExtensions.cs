using Old8Lang.Interpreter;
using System.Reflection.Emit;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Expression.Linq;

/// <summary>
/// LinqExpression 的编译器入口辅助实现（阶段 2 从语法节点迁出）。
/// </summary>
internal static partial class LinqExpressionRuntimeExtensions
{
    /// <summary>
    /// 生成 LINQ 方法调用链的 IL 代码
    /// </summary>
    internal static void GenerateLinqMethodCalls(this LinqExpression node, ILGenerator ilGenerator, LocalManager local)
    {
        // 1. 加载数据源
        node.FromClause.DataSource.Accept(new CompilerVisitor(ilGenerator, local));

        // 2. 转换为 IEnumerable
        node.ConvertToEnumerable(ilGenerator);

        // 3. 处理查询体子句（where, orderby, let, join 等）
        foreach (var clause in node.BodyClauses)
        {
            node.GenerateClauseIL(clause, ilGenerator, local);
        }

        // 4. 处理终止子句（select 或 group）
        node.GenerateTerminationClauseIL(node.TerminationClause, ilGenerator, local);

        // 5. 处理查询延续（into 子句）
        if (node.Continuation != null)
        {
            node.GenerateContinuationIL(node.Continuation, ilGenerator, local);
        }

        // 6. 转换结果为 ListLangValue
        node.ConvertToListLangValue(ilGenerator, local);
    }
    /// <summary>
    /// 将值转换为 IEnumerable
    /// </summary>
    internal static void ConvertToEnumerable(this LinqExpression node, ILGenerator ilGenerator)
    {
        // 使用辅助方法进行转换，简化 IL 代码生成
        ilGenerator.Emit(OpCodes.Call, typeof(LinqCompilerHelper).GetMethod("ConvertToEnumerable")!);
    }
    /// <summary>
    /// 生成查询体子句的 IL 代码
    /// </summary>
    internal static void GenerateClauseIL(this LinqExpression node, LinqClause clause, ILGenerator ilGenerator, LocalManager local)
    {
        switch (clause)
        {
            case WhereClause whereClause:
                node.GenerateWhereClauseIL(whereClause, ilGenerator, local);
                break;
            case OrderByClause orderByClause:
                node.GenerateOrderByClauseIL(orderByClause, ilGenerator, local);
                break;
            case LetClause letClause:
                node.GenerateLetClauseIL(letClause, ilGenerator, local);
                break;
            case JoinClause joinClause:
                node.GenerateJoinClauseIL(joinClause, ilGenerator, local);
                break;
            default:
                throw new NotSupportedException($"不支持的 LINQ 子句类型: {clause.GetType().Name}");
        }
    }
    /// <summary>
    /// 生成终止子句的 IL 代码
    /// </summary>
    internal static void GenerateTerminationClauseIL(this LinqExpression node, LinqClause clause, ILGenerator ilGenerator, LocalManager local)
    {
        switch (clause)
        {
            case SelectClause selectClause:
                node.GenerateSelectClauseIL(selectClause, ilGenerator, local);
                break;
            case GroupByClause groupByClause:
                node.GenerateGroupByClauseIL(groupByClause, ilGenerator, local);
                break;
            default:
                throw new NotSupportedException($"不支持的终止子句类型: {clause.GetType().Name}");
        }
    }
}
