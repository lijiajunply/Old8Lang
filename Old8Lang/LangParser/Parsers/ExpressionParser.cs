using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.LangParser.Core;

namespace Old8Lang.LangParser.Parsers;

/// <summary>
/// 表达式解析器
/// 负责解析各种表达式，包括算术、逻辑、比较、三元表达式等
/// </summary>
public class ExpressionParser(ParserContext context, PrimaryParser primaryParser, FunctionParser functionParser)
    : ParserBase(context)
{
    #region Expression

    // expression = boolOpera
    //            | nullishCoalescing
    //            | ternaryExpression
    //            | binaryExpression
    //            | dotExpr
    //            | numberOpera1
    //            | numberOpera2
    //            | notBool
    //            | minusPrefix
    //            | primary ;
    public LangExpression ParseExpression()
    {
        // 1. 解析逻辑表达式
        var expr = ParseBoolOpera();
        // 2. 解析空值合并表达式
        expr = ParseNullishCoalescing(expr);
        // 3. 解析三元表达式（最低优先级）
        expr = ParseTernaryExpression(expr);
        return expr;
    }

    // 逻辑表达式
    public LangExpression ParseBoolOpera()
    {
        var left = ParseBinaryExpression();

        while (CurrentToken.Type == LangTokenType.And || CurrentToken.Type == LangTokenType.Or ||
               CurrentToken.Type == LangTokenType.Xor)
        {
            var operatorToken = CurrentToken;
            var position = CreateSourcePosition(operatorToken);
            Expect(operatorToken.Type);
            var right = ParseBinaryExpression();
            left = new Operation(left, operatorToken.Type, right, position);
        }

        return left;
    }

    /// <summary>
    /// 解析空值合并表达式
    /// nullishCoalescing = expression "??" expression ;
    /// </summary>
    public LangExpression ParseNullishCoalescing(LangExpression left)
    {
        while (CurrentToken.Type == LangTokenType.NullishCoalescing)
        {
            var operatorToken = CurrentToken;
            var position = CreateSourcePosition(operatorToken);
            Expect(LangTokenType.NullishCoalescing);
            // 空值合并运算符的右操作数优先级与逻辑表达式相同
            var right = ParseBinaryExpression();
            left = new Operation(left, operatorToken.Type, right, position);
        }

        return left;
    }

    /// <summary>
    /// 解析三元表达式
    /// ternaryExpression = expression "?" expression ":" expression ;
    /// 注意：需要与类型注解区分开，类型注解的形式是 "identifier : type"
    /// </summary>
    /// <returns>三元表达式节点</returns>
    public LangExpression ParseTernaryExpression(LangExpression condition)
    {
        // 检查是否有 ?，这是三元表达式的标志
        if (CurrentToken.Type == LangTokenType.Question)
        {
            var questionToken = CurrentToken;
            Expect(LangTokenType.Question);

            // 解析问号后的表达式（true分支）
            var trueExpr = ParseExpression();

            // 检查是否有 :，这是三元表达式的分支分隔符
            if (CurrentToken.Type == LangTokenType.Colon)
            {
                Expect(LangTokenType.Colon);

                // 解析冒号后的表达式（false分支）
                var falseExpr = ParseExpression();

                // 创建三元表达式节点
                // 语法：condition ? trueExpr : falseExpr
                return new TernaryExpression(
                    condition,
                    trueExpr,
                    falseExpr,
                    new SourcePosition(questionToken.Line, questionToken.Column));
            }

            // 三元表达式缺少冒号，抛出错误
            throw CreateSyntaxError("语法错误：三元表达式不完整，缺少 ':' 和假值分支。建议：使用完整的三元表达式格式 'condition ? trueValue : falseValue'。");
        }

        // 不是三元表达式，返回原始条件表达式
        return condition;
    }

    // 比较表达式
    public LangExpression ParseBinaryExpression()
    {
        var left = ParseNumberOpera1();

        while (CurrentToken.Type is LangTokenType.In or LangTokenType.LessThanEquals or LangTokenType.GreaterThanEquals
               or LangTokenType.Equals
               or LangTokenType.NotEquals or LangTokenType.LessThan or LangTokenType.GreaterThan)
        {
            var operatorToken = CurrentToken;
            var position = CreateSourcePosition(operatorToken);
            Expect(operatorToken.Type);
            var right = ParseNumberOpera1();
            left = new Operation(left, operatorToken.Type, right, position);
        }

        return left;
    }

    // 加减表达式
    public LangExpression ParseNumberOpera1()
    {
        var left = ParseNumberOpera2();

        while (CurrentToken.Type == LangTokenType.Plus || CurrentToken.Type == LangTokenType.Minus)
        {
            var operatorToken = CurrentToken;
            var position = CreateSourcePosition(operatorToken);
            Expect(operatorToken.Type);
            var right = ParseNumberOpera2();
            left = new Operation(left, operatorToken.Type, right, position);
        }

        return left;
    }

    // 乘除表达式
    public LangExpression ParseNumberOpera2()
    {
        // 处理幂运算（右结合，最高优先级）
        var left = ParsePower();

        // 处理乘法、除法和取模运算（左结合）
        while (CurrentToken.Type is LangTokenType.Star or LangTokenType.Slash or LangTokenType.Percent)
        {
            var operatorToken = CurrentToken;
            var position = CreateSourcePosition(operatorToken);
            Expect(operatorToken.Type);
            var right = ParsePower(); // 右边也需要先处理幂运算
            left = new Operation(left, operatorToken.Type, right, position);
        }

        // 处理后置自增自减
        if (CurrentToken.Type == LangTokenType.PlusPlus)
        {
            var operatorToken = CurrentToken;
            var position = CreateSourcePosition(operatorToken);
            Expect(LangTokenType.PlusPlus);
            left = new Operation(left, LangTokenType.Plus, new IntLangValue(1), position);
        }
        else if (CurrentToken.Type == LangTokenType.MinusMinus)
        {
            var operatorToken = CurrentToken;
            var position = CreateSourcePosition(operatorToken);
            Expect(LangTokenType.MinusMinus);
            left = new Operation(left, LangTokenType.Minus, new IntLangValue(1), position);
        }

        // 处理 as 操作符
        while (CurrentToken.Type == LangTokenType.As)
        {
            var operatorToken = CurrentToken;
            var position = CreateSourcePosition(operatorToken);
            Expect(LangTokenType.As);
            var right = primaryParser.ParsePrimary();
            // 处理右操作数的点运算符
            right = ParseDotExpr(right);
            left = new Operation(left, operatorToken.Type, right, position);
        }

        return left;
    }

    // 处理幂运算（右结合）
    public LangExpression ParsePower()
    {
        var left = primaryParser.ParsePrimary();

        // 处理点运算符（最高优先级）
        left = ParseDotExpr(left);

        // 处理右结合的幂运算
        if (CurrentToken.Type == LangTokenType.Caret)
        {
            var operatorToken = CurrentToken;
            var position = CreateSourcePosition(operatorToken);
            Expect(operatorToken.Type);
            var right = ParsePower(); // 递归调用，实现右结合
            left = new Operation(left, operatorToken.Type, right, position);
        }

        return left;
    }

// dotExpr = expression ( "." expression )* ;
    public LangExpression ParseDotExpr(LangExpression left)
    {
        while (true)
        {
            if (CurrentToken.Type == LangTokenType.Dot)
            {
                // 处理成员访问: left.right
                var dotToken = CurrentToken;
                var position = new SourcePosition(dotToken.Line, dotToken.Column, tokenValue: dotToken.Value);
                Expect(LangTokenType.Dot);
                var right = primaryParser.ParsePrimary();
                left = new Operation(left, LangTokenType.Dot, right, position);
            }
            else if (CurrentToken.Type == LangTokenType.LeftBracket)
            {
                // 处理索引访问或切片: left[right] 或 left[start:end]
                var leftBracketToken = CurrentToken;
                var position = new SourcePosition(leftBracketToken.Line, leftBracketToken.Column,
                    tokenValue: leftBracketToken.Value);
                Expect(LangTokenType.LeftBracket);

                // 检查是否是切片语法（包含冒号）
                var isSlice = false;
                var lookAheadIndex = CurrentIndex;

                // 向前查找冒号，但不要越过右方括号
                while (lookAheadIndex < CurrentIndex + 10 && lookAheadIndex < Tokens.Count) // 限制查找深度避免无限循环
                {
                    var checkToken = Tokens[lookAheadIndex];
                    if (checkToken.Type == LangTokenType.Colon)
                    {
                        isSlice = true;
                        break;
                    }
                    if (checkToken.Type == LangTokenType.RightBracket)
                    {
                        break;
                    }
                    lookAheadIndex++;
                }

                if (isSlice)
                {
                    // 解析切片语法
                    var start = ParseExpression();
                    LangExpression? end = null;
                    LangExpression? step = null;

                    if (CurrentToken.Type == LangTokenType.Colon)
                    {
                        Expect(LangTokenType.Colon);

                        // 检查是否有 end 参数
                        if (CurrentToken.Type != LangTokenType.Colon && CurrentToken.Type != LangTokenType.RightBracket)
                        {
                            end = ParseExpression();
                        }

                        // 检查是否有第二个冒号（步长参数）
                        if (CurrentToken.Type == LangTokenType.Colon)
                        {
                            Expect(LangTokenType.Colon);
                            if (CurrentToken.Type != LangTokenType.RightBracket)
                            {
                                step = ParseExpression();
                            }
                        }
                    }

                    Expect(LangTokenType.RightBracket);

                    // 处理嵌套切片
                    if (left is LangId leftId)
                    {
                        left = new SliceLangValue(leftId, start, end, step);
                    }
                    else if (left is LangListItem nestedItem)
                    {
                        left = new NestedSliceAccess(nestedItem, start, end, step, position);
                    }
                    else
                    {
                        // 其他类型的切片访问
                        left = new SliceLangValue(new LangId(""), start, end, step); // 临时处理
                    }
                }
                else
                {
                    // 解析普通索引访问
                    var right = ParseExpression(); // 允许索引是复杂表达式
                    Expect(LangTokenType.RightBracket);

                    // 创建LangListItem而不是Operation
                    // 如果left是LangId，直接创建LangListItem
                    if (left is LangId leftId)
                    {
                        left = new LangListItem(leftId, right, position);
                    }
                    // 如果left是LangListItem，这是嵌套索引访问，需要特殊处理
                    else if (left is LangListItem nestedItem)
                    {
                        // 创建一个特殊的嵌套索引访问表达式
                        left = new NestedIndexAccess(nestedItem, right, position);
                    }
                    // 如果left是Operation，这也是嵌套访问
                    else
                    {
                        left = new Operation(left, LangTokenType.Dot, right, position);
                    }
                }
            }
            else if (CurrentToken.Type == LangTokenType.LeftParen)
            {
                // 处理函数调用: left(arguments)
                var leftParenToken = CurrentToken;
                var position = new SourcePosition(leftParenToken.Line, leftParenToken.Column,
                    tokenValue: leftParenToken.Value);
                Expect(LangTokenType.LeftParen);
                var args = functionParser.ParseArgList();
                Expect(LangTokenType.RightParen);

                // 创建函数调用表达式
                left = new FunctionCallExpression(left, args, position);
            }
            else
            {
                break;
            }
        }

        return left;
    }

    #endregion
}