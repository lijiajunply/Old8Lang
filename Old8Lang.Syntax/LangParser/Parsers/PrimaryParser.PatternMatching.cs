using Old8Lang.AST;
using Old8Lang.AST.Expression.Intermediates;

namespace Old8Lang.LangParser.Parsers;

/// <summary>
/// Primary 表达式解析器 - 模式匹配解析
/// </summary>
public partial class PrimaryParser
{
    private LangExpression ParseMatchExpression()
    {
        var matchToken = CurrentToken;
        var position = new SourcePosition(matchToken.Line, matchToken.Column, tokenValue: matchToken.Value);
        Expect(LangTokenType.Match);

        // 解析被匹配的表达式
        var matchValue = expressionParserFactory().ParseExpression();

        // 期望左花括号
        Expect(LangTokenType.LeftBrace);

        // 解析所有 case 分支
        var cases = new List<MatchCase>();
        while (CurrentToken.Type == LangTokenType.Case || CurrentToken.Type == LangTokenType.Default)
        {
            // 处理 default 分支 (相当于通配符)
            if (CurrentToken.Type == LangTokenType.Default)
            {
                Expect(LangTokenType.Default);
                Expect(LangTokenType.Arrow);
                var resultExpr = expressionParserFactory().ParseExpression();
                cases.Add(new MatchCase("_", resultExpr, isWildcard: true));
                continue;
            }

            Expect(LangTokenType.Case);

            // 检查模式类型
            // 1. 通配符: case _ -> expression
            if (CurrentToken is { Type: LangTokenType.Identifier, Value: "_" })
            {
                Expect(LangTokenType.Identifier); // 消费 _
                Expect(LangTokenType.Arrow);
                var resultExpr = expressionParserFactory().ParseExpression();
                cases.Add(new MatchCase("_", resultExpr, isWildcard: true));
            }
            // 2. 范围匹配: case [start~end] -> expression
            else if (CurrentToken.Type == LangTokenType.LeftBracket)
            {
                var rangePattern = ParseRangePattern();
                Expect(LangTokenType.Arrow);
                var resultExpr = expressionParserFactory().ParseExpression();
                cases.Add(new MatchCase(rangePattern, resultExpr));
            }
            // 3. 元组解构: case (x, 0) -> expression
            else if (CurrentToken.Type == LangTokenType.LeftParen)
            {
                var tuplePattern = ParseTuplePattern();
                Expect(LangTokenType.Arrow);
                var resultExpr = expressionParserFactory().ParseExpression();
                cases.Add(new MatchCase(tuplePattern, resultExpr));
            }
            // 4. 类型匹配（带可选守卫）: case x:int [if condition] -> expression
            else if (CurrentToken.Type == LangTokenType.Identifier)
            {
                var varName = CurrentToken.Value;
                Expect(LangTokenType.Identifier);

                // 检查是否有类型注解
                if (CurrentToken.Type == LangTokenType.Colon)
                {
                    Expect(LangTokenType.Colon);

                    if (CurrentToken.Type != LangTokenType.Identifier)
                    {
                        throw CreateSyntaxError($"期望类型名称，但得到 {CurrentToken.Type}");
                    }

                    var typeAnnotation = CurrentToken.Value;
                    Expect(LangTokenType.Identifier);

                    // 检查是否有守卫条件
                    LangExpression? guardCondition = null;
                    if (CurrentToken.Type == LangTokenType.If)
                    {
                        Expect(LangTokenType.If);
                        guardCondition = expressionParserFactory().ParseExpression();
                    }

                    Expect(LangTokenType.Arrow);
                    var resultExpr = expressionParserFactory().ParseExpression();
                    cases.Add(new MatchCase(varName, typeAnnotation, resultExpr, guardCondition));
                }
                // 5. 变量绑定: case identifier -> expression
                else if (CurrentToken.Type == LangTokenType.Arrow)
                {
                    Expect(LangTokenType.Arrow);
                    var resultExpr = expressionParserFactory().ParseExpression();
                    cases.Add(new MatchCase(varName, resultExpr));
                }
                else
                {
                    // 不是箭头也不是冒号，回退并按值匹配处理
                    // 但这种情况不应该发生，因为我们已经消费了标识符
                    // 实际上这是一个语法错误
                    throw CreateSyntaxError($"语法错误：case 后的标识符 '{varName}' 后应跟 ':' (类型注解) 或 '->' (变量绑定/箭头)");
                }
            }
            // 6. 值匹配: case expression -> expression
            else
            {
                var pattern = expressionParserFactory().ParseExpression();
                Expect(LangTokenType.Arrow);
                var resultExpr = expressionParserFactory().ParseExpression();
                cases.Add(new MatchCase(pattern, resultExpr));
            }
        }

        // 期望右花括号
        Expect(LangTokenType.RightBrace);

        // 检查是否至少有一个 case
        if (cases.Count == 0)
        {
            throw CreateSyntaxError("Match 表达式至少需要一个 case 或 default 分支");
        }

        return new MatchExpression(matchValue, cases, position);
    }

    /// <summary>
    /// 解析元组模式: (x, 0) 或 (0, y) 或 (x, y)
    /// </summary>

    private TuplePattern ParseTuplePattern()
    {
        Expect(LangTokenType.LeftParen);

        var elements = new List<TuplePatternElement>();

        while (CurrentToken.Type != LangTokenType.RightParen)
        {
            // 通配符 _
            if (CurrentToken is { Type: LangTokenType.Identifier, Value: "_" })
            {
                Expect(LangTokenType.Identifier);
                elements.Add(new TuplePatternElement("_", isWildcard: true));
            }
            // 变量绑定: identifier (后面不是运算符)
            else if (CurrentToken.Type == LangTokenType.Identifier &&
                     (Peek().Type == LangTokenType.Comma || Peek().Type == LangTokenType.RightParen))
            {
                var varName = CurrentToken.Value;
                Expect(LangTokenType.Identifier);
                elements.Add(new TuplePatternElement(varName));
            }
            // 值匹配: expression
            else
            {
                var valueExpr = expressionParserFactory().ParseExpression();
                elements.Add(new TuplePatternElement(valueExpr));
            }

            // 检查是否有逗号
            if (CurrentToken.Type == LangTokenType.Comma)
            {
                Expect(LangTokenType.Comma);
            }
            else if (CurrentToken.Type != LangTokenType.RightParen)
            {
                throw CreateSyntaxError($"期望 ',' 或 ')'，但得到 {CurrentToken.Type}");
            }
        }

        Expect(LangTokenType.RightParen);

        if (elements.Count < 2)
        {
            throw CreateSyntaxError("元组模式至少需要 2 个元素");
        }

        return new TuplePattern(elements);
    }

    /// <summary>
    /// 解析范围模式: [start~end] 或 [start~<end] 等
    /// </summary>

    private RangePattern ParseRangePattern()
    {
        Expect(LangTokenType.LeftBracket);

        var startExpr = expressionParserFactory().ParseExpression();

        // 检查范围操作符
        bool includeStart = true;
        bool includeEnd = true;

        var rangeTokenType = CurrentToken.Type;
        switch (rangeTokenType)
        {
            case LangTokenType.Wavy: // [start~end] - 包含两边
                includeStart = true;
                includeEnd = true;
                break;
            case LangTokenType.WavyLessThan: // [start~<end] - 包含start，排除end
                includeStart = true;
                includeEnd = false;
                break;
            case LangTokenType.GreaterThanWavy: // [start>~end] - 排除start，包含end
                includeStart = false;
                includeEnd = true;
                break;
            case LangTokenType.GreaterThanWavyLessThan: // [start>~<end] - 排除两边
                includeStart = false;
                includeEnd = false;
                break;
            default:
                throw CreateSyntaxError($"期望范围操作符 (~, ~<, >~, >~<)，但得到 {rangeTokenType}");
        }

        Expect(rangeTokenType);

        var endExpr = expressionParserFactory().ParseExpression();

        Expect(LangTokenType.RightBracket);

        return new RangePattern(startExpr, endExpr, includeStart, includeEnd);
    }

}
