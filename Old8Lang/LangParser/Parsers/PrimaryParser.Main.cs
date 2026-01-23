using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.LangParser.Core;

namespace Old8Lang.LangParser.Parsers;

/// <summary>
/// Primary 表达式解析器
/// 负责解析主表达式，包括字面量、列表、字典、数组、元组、Lambda、字符串模板等
/// </summary>
public partial class PrimaryParser(
    ParserContext context,
    Func<StatementParser> statementParserFactory,
    Func<ExpressionParser> expressionParserFactory,
    FunctionParser functionParser,
    LinqParser linqParser)
    : ParserBase(context)
{
    #region Primary

    public LangExpression ParsePrimary()
    {
        // 处理 LINQ 查询表达式（from ... select/group）
        if (CurrentToken.Type == LangTokenType.From)
        {
            return linqParser.ParseLinqExpression();
        }

        // 处理 await 表达式
        if (CurrentToken.Type == LangTokenType.Await)
        {
            var awaitToken = CurrentToken;
            var position = new SourcePosition(awaitToken.Line, awaitToken.Column, tokenValue: awaitToken.Value);
            Expect(LangTokenType.Await);
            var expr = expressionParserFactory().ParseExpression();
            return new AwaitExpression(expr, position);
        }

        // 处理异步流：async { block }
        if (CurrentToken.Type == LangTokenType.Async && Peek().Type == LangTokenType.LeftBrace)
        {
            var asyncToken = CurrentToken;
            var position = new SourcePosition(asyncToken.Line, asyncToken.Column, tokenValue: asyncToken.Value);
            Expect(LangTokenType.Async);

            // 解析块语句
            var block = statementParserFactory().ParseBlock();

            // 创建异步流表达式
            return new AsyncStreamExpression(block, position);
        }

        // 处理异步Lambda表达式 - 无参数情况
        if (CurrentToken.Type == LangTokenType.Async && Peek().Type == LangTokenType.LeftParen)
        {
            // 异步Lambda表达式：async () -> block 或 async (params) -> block
            var asyncToken = CurrentToken;
            var position = new SourcePosition(asyncToken.Line, asyncToken.Column, tokenValue: asyncToken.Value);
            Expect(LangTokenType.Async);

            var leftParenToken = CurrentToken;
            Expect(LangTokenType.LeftParen);

            // 检查是否是无参数异步Lambda
            if (CurrentToken.Type == LangTokenType.RightParen && Peek().Type == LangTokenType.Arrow)
            {
                // 无参数异步Lambda：async () -> block 或 async () -> expression
                Expect(LangTokenType.RightParen);
                Expect(LangTokenType.Arrow);

                BlockStatement block;

                // 检查是块语句还是表达式
                if (CurrentToken.Type == LangTokenType.LeftBrace)
                {
                    // 块语句：async () -> { ... }
                    block = statementParserFactory().ParseBlock();
                }
                else
                {
                    // 表达式：async () -> expression
                    // 我们需要将表达式转换为块语句，添加return
                    var expr = expressionParserFactory().ParseExpression();
                    var returnStmt = new ReturnStatement(expr, position);
                    block = new BlockStatement([returnStmt]);
                }

                // 创建异步Lambda表达式
                return new AsyncFuncLangValue(null, [], block, position);
            }

            // 有参数异步Lambda
            var isLambda = true;
            var ids = new List<LangId>();

            // 检查第一个元素是否是标识符
            if (CurrentToken.Type == LangTokenType.Identifier)
            {
                // 解析第一个参数，允许类型注解
                ids.Add(functionParser.ParseTypedIdentifier(true));

                // 解析更多参数，允许类型注解
                while (CurrentToken.Type == LangTokenType.Comma)
                {
                    Expect(LangTokenType.Comma);
                    if (CurrentToken.Type != LangTokenType.Identifier)
                    {
                        // 不是标识符，不是Lambda表达式
                        isLambda = false;
                        break;
                    }

                    ids.Add(functionParser.ParseTypedIdentifier(true));
                }

                // 检查是否有箭头符号
                if (isLambda && CurrentToken.Type == LangTokenType.RightParen && Peek().Type == LangTokenType.Arrow)
                {
                    // 异步Lambda表达式：async (params) -> block 或 async (params) -> expression
                    Expect(LangTokenType.RightParen);
                    Expect(LangTokenType.Arrow);

                    BlockStatement block;

                    // 检查是块语句还是表达式
                    if (CurrentToken.Type == LangTokenType.LeftBrace)
                    {
                        // 块语句：async (params) -> { ... }
                        block = statementParserFactory().ParseBlock();
                    }
                    else
                    {
                        // 表达式：async (params) -> expression
                        // 我们需要将表达式转换为块语句，添加return
                        var expr = expressionParserFactory().ParseExpression();
                        var returnStmt = new ReturnStatement(expr, position);
                        block = new BlockStatement([returnStmt]);
                    }

                    // 创建异步Lambda表达式
                    return new AsyncFuncLangValue(null, ids, block, position);
                }
            }

            // 不是Lambda表达式，抛出错误
            throw CreateSyntaxError("语法错误：异步Lambda表达式格式不正确");
        }

        // 处理 not 表达式
        if (CurrentToken.Type == LangTokenType.Not)
        {
            var notToken = CurrentToken;
            var position = new SourcePosition(notToken.Line, notToken.Column, tokenValue: notToken.Value);
            Expect(LangTokenType.Not);
            // 修复：not 运算符的右操作数应该包含点运算符的处理
            // 调用 ParsePower() 而不是 ParsePrimary()，确保 not this.connected 被正确解析为 not (this.connected)
            var expr = expressionParserFactory().ParsePower();
            return new Operation(null, LangTokenType.Exclamation, expr, position);
        }

        // 处理前缀 minus 表达式
        if (CurrentToken.Type == LangTokenType.Minus)
        {
            var minusToken = CurrentToken;
            var position = new SourcePosition(minusToken.Line, minusToken.Column, tokenValue: minusToken.Value);
            Expect(LangTokenType.Minus);
            // 修复：前缀负号运算符的右操作数也应该包含点运算符的处理
            var expr = expressionParserFactory().ParsePower();
            return new Operation(null, LangTokenType.Minus, expr, position);
        }

        // 处理前缀自增 ++i
        if (CurrentToken.Type == LangTokenType.PlusPlus)
        {
            var plusPlusToken = CurrentToken;
            var position =
                new SourcePosition(plusPlusToken.Line, plusPlusToken.Column, tokenValue: plusPlusToken.Value);
            Expect(LangTokenType.PlusPlus);
            var expr = ParsePrimary();
            return new Operation(expr, LangTokenType.Plus, new IntLangValue(1), position);
        }

        // 处理前缀自减 --i
        if (CurrentToken.Type == LangTokenType.MinusMinus)
        {
            var minusMinusToken = CurrentToken;
            var position = new SourcePosition(minusMinusToken.Line, minusMinusToken.Column,
                tokenValue: minusMinusToken.Value);
            Expect(LangTokenType.MinusMinus);
            var expr = ParsePrimary();
            return new Operation(expr, LangTokenType.Minus, new IntLangValue(1), position);
        }

        // 处理 match 表达式
        if (CurrentToken.Type == LangTokenType.Match)
        {
            return ParseMatchExpression();
        }

        // 处理 if-then-else 三元表达式
        if (CurrentToken.Type == LangTokenType.If)
        {
            var ifToken = CurrentToken;
            var position = new SourcePosition(ifToken.Line, ifToken.Column, tokenValue: ifToken.Value);
            Expect(LangTokenType.If);

            // 解析条件表达式
            var condition = expressionParserFactory().ParseExpression();

            // 检查是否有 then 关键字
            if (CurrentToken.Type == LangTokenType.Then)
            {
                Expect(LangTokenType.Then);

                // 解析 true 分支表达式
                var trueExpr = expressionParserFactory().ParseExpression();

                // 检查是否有 else 关键字
                if (CurrentToken.Type == LangTokenType.Else)
                {
                    Expect(LangTokenType.Else);

                    // 解析 false 分支表达式
                    var falseExpr = expressionParserFactory().ParseExpression();

                    // 创建三元表达式节点
                    // 语法：if condition then trueExpr else falseExpr
                    return new TernaryExpression(condition, trueExpr, falseExpr, position);
                }
                else
                {
                    throw CreateSyntaxError(
                        "语法错误：if-then-else 表达式不完整，缺少 'else' 和假值分支。建议：使用完整的 if-then-else 表达式格式 'if condition then trueValue else falseValue'。");
                }
            }
            else
            {
                throw CreateSyntaxError(
                    "语法错误：if 表达式后缺少 'then' 关键字。如果要使用 if-then-else 三元表达式，请使用格式 'if condition then value else value'。");
            }
        }

        // 处理关键字作为标识符的情况
        if (CurrentToken.Type is LangTokenType.Func or
            LangTokenType.Class or
            LangTokenType.If or
            LangTokenType.Else or
            LangTokenType.While or
            LangTokenType.For or
            LangTokenType.Return or
            LangTokenType.Import)
        {
            // 检查是否是列表初始化：list[...]
            if (Peek().Type == LangTokenType.LeftBracket)
            {
                return ParseListInitOrSlice();
            }

            // 检查是否是泛型实例化：Class<T>(...) 或 func<T>(...)
            // 使用启发式方法判断，避免误将比较运算符当作泛型
            if (Peek().Type == LangTokenType.LessThan && IsLikelyGenericInstantiation())
            {
                return ParseGenericInstantiation();
            }

            // 检查是否是函数调用：func(...)
            if (Peek().Type == LangTokenType.LeftParen)
            {
                return ParseInstantiate();
            }

            // 否则作为普通标识符处理
            return ParseIdentifier();
        }

        if (CurrentToken.Type == LangTokenType.This)
        {
            // 创建一个 ThisExpression 对象来处理 this 关键字
            var thisToken = CurrentToken;
            var position = new SourcePosition(thisToken.Line, thisToken.Column, tokenValue: thisToken.Value);
            Expect(LangTokenType.This);
            return new ThisExpression(position);
        }

        if (CurrentToken.Type == LangTokenType.Super)
        {
            // 创建一个 SuperExpression 对象来处理 super 关键字
            var superToken = CurrentToken;
            var position = new SourcePosition(superToken.Line, superToken.Column, tokenValue: superToken.Value);
            Expect(LangTokenType.Super);
            return new SuperExpression(position);
        }

        return CurrentToken.Type switch
        {
            LangTokenType.String => ParseStringLiteral(),
            LangTokenType.Char => ParseCharLiteral(),
            LangTokenType.Number => CurrentToken.Value.Contains('.') || CurrentToken.Value.Contains('e') ||
                                    CurrentToken.Value.Contains('E')
                ? ParseDoubleLiteral()
                : ParseIntLiteral(),
            LangTokenType.LeftBracket => ParseArrayOrRange(),
            LangTokenType.LeftParen => ParseLambdaOrTuple(),
            LangTokenType.LeftBrace => ParseListOrDictionary(),
            LangTokenType.Dollar => ParseStringTemplate(), // 处理字符串模板：$"string", ${expression}, $($"string")
            LangTokenType.Identifier when Peek().Type == LangTokenType.LeftBracket => ParseListInitOrSlice(),
            LangTokenType.Identifier when Peek().Type == LangTokenType.LessThan && IsLikelyGenericInstantiation() =>
                ParseGenericInstantiation(),
            LangTokenType.Identifier when Peek().Type == LangTokenType.LeftParen => ParseInstantiate(),
            LangTokenType.Identifier => ParseIdentifier(),
            LangTokenType.True or LangTokenType.False => ParseBoolLiteral(),
            LangTokenType.Null => ParseNullLiteral(),
            _ => throw CreateSyntaxError(
                $"语法错误：无法识别的主表达式类型 '{CurrentToken.Type}'，值为 '{CurrentToken.Value}'。建议检查表达式结构是否正确。")
        };
    }
    #endregion
}
