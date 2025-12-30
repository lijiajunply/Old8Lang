using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Linq;
using Old8Lang.LangParser.Core;

namespace Old8Lang.LangParser.Parsers;

/// <summary>
/// LINQ 查询表达式解析器
/// 负责解析 LINQ 查询语法，包括 from, where, select, orderby, join, group, let 等子句
/// </summary>
public class LinqParser(ParserContext context, Func<ExpressionParser> expressionParserFactory)
    : ParserBase(context)
{
    /// <summary>
    /// 解析 LINQ 查询表达式
    /// 语法: from rangeVariable in dataSource [query-body]
    /// </summary>
    public LinqExpression ParseLinqExpression()
    {
        var position = CreateSourcePosition(CurrentToken);

        // 解析 from 子句
        var fromClause = ParseFromClause();

        // 解析查询体（零个或多个子句）
        var bodyClauses = new List<LinqClause>();
        LinqClause terminationClause;

        while (true)
        {
            // 检查是否是终止子句（select 或 group）
            if (CurrentToken.Type == LangTokenType.Select)
            {
                terminationClause = ParseSelectClause();
                break;
            }
            else if (CurrentToken.Type == LangTokenType.Group)
            {
                terminationClause = ParseGroupByClause();
                break;
            }
            // 解析查询体子句
            else if (CurrentToken.Type == LangTokenType.Where)
            {
                bodyClauses.Add(ParseWhereClause());
            }
            else if (CurrentToken.Type == LangTokenType.OrderBy)
            {
                bodyClauses.Add(ParseOrderByClause());
            }
            else if (CurrentToken.Type == LangTokenType.Let)
            {
                bodyClauses.Add(ParseLetClause());
            }
            else if (CurrentToken.Type == LangTokenType.Join)
            {
                bodyClauses.Add(ParseJoinClause());
            }
            else if (CurrentToken.Type == LangTokenType.From)
            {
                // 支持多个 from 子句
                bodyClauses.Add(ParseFromClause());
            }
            else
            {
                throw CreateSyntaxError($"LINQ 查询表达式缺少 select 或 group 终止子句");
            }
        }

        // 检查是否有 into 查询延续
        QueryContinuation? continuation = null;
        if (CurrentToken.Type == LangTokenType.Into)
        {
            continuation = ParseQueryContinuation();
        }

        return new LinqExpression(fromClause, bodyClauses, terminationClause, continuation, position);
    }

    /// <summary>
    /// 解析 from 子句
    /// 语法: from [type] rangeVariable in dataSource
    /// </summary>
    private FromClause ParseFromClause()
    {
        var position = CreateSourcePosition(CurrentToken);
        Expect(LangTokenType.From);

        string? typeAnnotation = null;
        string rangeVariable;

        // 检查是否有类型注解
        // from int x in numbers 或 from x in numbers
        var firstToken = CurrentToken;
        if (CurrentToken.Type == LangTokenType.Identifier)
        {
            var nextToken = Peek();
            if (nextToken.Type == LangTokenType.Identifier)
            {
                // 有类型注解: from Type variable in ...
                typeAnnotation = CurrentToken.Value;
                Expect(LangTokenType.Identifier);
                rangeVariable = CurrentToken.Value;
                Expect(LangTokenType.Identifier);
            }
            else
            {
                // 没有类型注解: from variable in ...
                rangeVariable = CurrentToken.Value;
                Expect(LangTokenType.Identifier);
            }
        }
        else
        {
            throw CreateSyntaxError("期望范围变量名称");
        }

        // 期望 in 关键字
        Expect(LangTokenType.In);

        // 解析数据源表达式（使用 ParseBoolOpera 避免解析过多）
        var dataSource = expressionParserFactory().ParseBoolOpera();

        return new FromClause(rangeVariable, dataSource, typeAnnotation, position);
    }

    /// <summary>
    /// 解析 where 子句
    /// 语法: where condition
    /// </summary>
    private WhereClause ParseWhereClause()
    {
        var position = CreateSourcePosition(CurrentToken);
        Expect(LangTokenType.Where);

        var condition = ParseLinqClauseExpression();

        return new WhereClause(condition, position);
    }

    /// <summary>
    /// 解析 LINQ 子句内的表达式（在 LINQ 关键字之前停止）
    /// </summary>
    private LangExpression ParseLinqClauseExpression()
    {
        // 使用一个简单的策略：解析表达式直到遇到 LINQ 关键字
        // 这需要在 ExpressionParser 中添加一个方法来支持，
        // 目前先使用 ParseBoolOpera 来解析简单的表达式
        return expressionParserFactory().ParseBoolOpera();
    }

    /// <summary>
    /// 解析 select 子句
    /// 语法: select projection
    /// </summary>
    private SelectClause ParseSelectClause()
    {
        var position = CreateSourcePosition(CurrentToken);
        Expect(LangTokenType.Select);

        var projection = ParseLinqClauseExpression();

        return new SelectClause(projection, position);
    }

    /// <summary>
    /// 解析 orderby 子句
    /// 语法: orderby key1 [ascending|descending] [, key2 [ascending|descending]] ...
    /// </summary>
    private OrderByClause ParseOrderByClause()
    {
        var position = CreateSourcePosition(CurrentToken);
        Expect(LangTokenType.OrderBy);

        var orderings = new List<OrderingItem>();

        do
        {
            if (orderings.Count > 0)
            {
                Expect(LangTokenType.Comma);
            }

            var keyExpression = ParseLinqClauseExpression();
            var isAscending = true;

            // 检查是否指定了排序方向
            if (CurrentToken.Type == LangTokenType.Ascending)
            {
                Expect(LangTokenType.Ascending);
                isAscending = true;
            }
            else if (CurrentToken.Type == LangTokenType.Descending)
            {
                Expect(LangTokenType.Descending);
                isAscending = false;
            }

            orderings.Add(new OrderingItem(keyExpression, isAscending));
        } while (CurrentToken.Type == LangTokenType.Comma);

        return new OrderByClause(orderings, position);
    }

    /// <summary>
    /// 解析 group by 子句
    /// 语法: group element by key
    /// </summary>
    private GroupByClause ParseGroupByClause()
    {
        var position = CreateSourcePosition(CurrentToken);
        Expect(LangTokenType.Group);

        var elementExpression = ParseLinqClauseExpression();

        Expect(LangTokenType.By);

        var keyExpression = ParseLinqClauseExpression();

        return new GroupByClause(elementExpression, keyExpression, position);
    }

    /// <summary>
    /// 解析 let 子句
    /// 语法: let variable <- expression
    /// </summary>
    private LetClause ParseLetClause()
    {
        var position = CreateSourcePosition(CurrentToken);
        Expect(LangTokenType.Let);

        if (CurrentToken.Type != LangTokenType.Identifier)
        {
            throw CreateSyntaxError("期望变量名称");
        }

        var variable = CurrentToken.Value;
        Expect(LangTokenType.Identifier);

        Expect(LangTokenType.Assignment); // <-

        var expression = ParseLinqClauseExpression();

        return new LetClause(variable, expression, position);
    }

    /// <summary>
    /// 解析 join 子句
    /// 语法: join [type] rangeVariable in dataSource on outerKey equals innerKey [into groupVariable]
    /// </summary>
    private JoinClause ParseJoinClause()
    {
        var position = CreateSourcePosition(CurrentToken);
        Expect(LangTokenType.Join);

        string? typeAnnotation = null;
        string rangeVariable;

        // 检查是否有类型注解
        var firstToken = CurrentToken;
        if (CurrentToken.Type == LangTokenType.Identifier)
        {
            var nextToken = Peek();
            if (nextToken.Type == LangTokenType.Identifier)
            {
                // 有类型注解: join Type variable in ...
                typeAnnotation = CurrentToken.Value;
                Expect(LangTokenType.Identifier);
                rangeVariable = CurrentToken.Value;
                Expect(LangTokenType.Identifier);
            }
            else
            {
                // 没有类型注解: join variable in ...
                rangeVariable = CurrentToken.Value;
                Expect(LangTokenType.Identifier);
            }
        }
        else
        {
            throw CreateSyntaxError("期望范围变量名称");
        }

        // 期望 in 关键字
        Expect(LangTokenType.In);

        // 解析数据源表达式
        var innerDataSource = expressionParserFactory().ParseBoolOpera();

        // 期望 on 关键字
        Expect(LangTokenType.On);

        // 解析外部键表达式
        var outerKeyExpression = ParseLinqClauseExpression();

        // 期望 equals 关键字（注意：equals 在 LINQ 中是上下文关键字）
        if (CurrentToken.Type != LangTokenType.Equals)
        {
            throw CreateSyntaxError("join 子句中期望 equals 关键字");
        }
        Expect(LangTokenType.Equals);

        // 解析内部键表达式
        var innerKeyExpression = ParseLinqClauseExpression();

        // 检查是否有 into 子句（group join）
        bool isGroupJoin = false;
        string? groupVariable = null;

        if (CurrentToken.Type == LangTokenType.Into)
        {
            Expect(LangTokenType.Into);
            if (CurrentToken.Type != LangTokenType.Identifier)
            {
                throw CreateSyntaxError("期望分组变量名称");
            }
            groupVariable = CurrentToken.Value;
            Expect(LangTokenType.Identifier);
            isGroupJoin = true;
        }

        return new JoinClause(rangeVariable, innerDataSource, outerKeyExpression, innerKeyExpression,
            typeAnnotation, isGroupJoin, groupVariable, position);
    }

    /// <summary>
    /// 解析查询延续
    /// 语法: into variable [query-body]
    /// </summary>
    private QueryContinuation ParseQueryContinuation()
    {
        var position = CreateSourcePosition(CurrentToken);
        Expect(LangTokenType.Into);

        if (CurrentToken.Type != LangTokenType.Identifier)
        {
            throw CreateSyntaxError("期望延续变量名称");
        }

        var variable = CurrentToken.Value;
        Expect(LangTokenType.Identifier);

        // 解析查询体（与主查询体相同的结构）
        var bodyClauses = new List<LinqClause>();
        LinqClause terminationClause;

        while (true)
        {
            if (CurrentToken.Type == LangTokenType.Select)
            {
                terminationClause = ParseSelectClause();
                break;
            }
            else if (CurrentToken.Type == LangTokenType.Group)
            {
                terminationClause = ParseGroupByClause();
                break;
            }
            else if (CurrentToken.Type == LangTokenType.Where)
            {
                bodyClauses.Add(ParseWhereClause());
            }
            else if (CurrentToken.Type == LangTokenType.OrderBy)
            {
                bodyClauses.Add(ParseOrderByClause());
            }
            else if (CurrentToken.Type == LangTokenType.Let)
            {
                bodyClauses.Add(ParseLetClause());
            }
            else
            {
                throw CreateSyntaxError($"查询延续中缺少 select 或 group 终止子句");
            }
        }

        return new QueryContinuation(variable, bodyClauses, terminationClause, position);
    }
}
