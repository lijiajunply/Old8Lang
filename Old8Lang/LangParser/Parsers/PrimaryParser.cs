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
public class PrimaryParser(
    ParserContext context,
    Func<StatementParser> statementParserFactory,
    Func<ExpressionParser> expressionParserFactory,
    FunctionParser functionParser)
    : ParserBase(context)
{
    #region Primary

    // primary = stringLiteral
    //         | intLiteral
    //         | charLiteral
    //         | doubleLiteral
    //         | identifier
    //         | trueLiteral
    //         | falseLiteral
    //         | listInit
    //         | instantiate
    //         | stringTree
    //         | lambda
    //         | listOrDictionary
    //         | range
    //         | array
    //         | tuple
    //         | slice
    //         | asStatement
    public LangExpression ParsePrimary()
    {
        // 处理 await 表达式
        if (CurrentToken.Type == LangTokenType.Await)
        {
            var awaitToken = CurrentToken;
            var position = new SourcePosition(awaitToken.Line, awaitToken.Column, tokenValue: awaitToken.Value);
            Expect(LangTokenType.Await);
            var expr = expressionParserFactory().ParseExpression();
            return new AwaitExpression(expr, position);
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
            var expr = ParsePrimary();
            return new Operation(null, LangTokenType.Exclamation, expr, position);
        }

        // 处理前缀 minus 表达式
        if (CurrentToken.Type == LangTokenType.Minus)
        {
            var minusToken = CurrentToken;
            var position = new SourcePosition(minusToken.Line, minusToken.Column, tokenValue: minusToken.Value);
            Expect(LangTokenType.Minus);
            var expr = ParsePrimary();
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
            // 直接创建一个 LangId 对象来处理 this 关键字
            var thisToken = CurrentToken;
            var position = new SourcePosition(thisToken.Line, thisToken.Column, tokenValue: thisToken.Value);
            Expect(LangTokenType.This);
            return new LangId(thisToken.Value, position: position);
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
            LangTokenType.Identifier when Peek().Type == LangTokenType.LeftParen => ParseInstantiate(),
            LangTokenType.Identifier => ParseIdentifier(),
            LangTokenType.True or LangTokenType.False => ParseBoolLiteral(),
            LangTokenType.Null => ParseNullLiteral(),
            _ => throw CreateSyntaxError(
                $"语法错误：无法识别的主表达式类型 '{CurrentToken.Type}'，值为 '{CurrentToken.Value}'。建议检查表达式结构是否正确。")
        };
    }

    /// <summary>
    /// 解析列表或字典初始化
    /// list = "{" expression ( "," expression )* "}" ;
    /// dictionary = "{" dicTuple ( "," dicTuple )* "}" ;
    /// dicTuple = expression ":" expression ;
    ///
    /// 区分规则：
    /// - 如果第一个元素后面跟着冒号，则是字典
    /// - 否则是列表
    /// </summary>
    /// <returns>列表或字典初始化</returns>
    public LangValueType ParseListOrDictionary()
    {
        var leftBraceToken = CurrentToken;
        var position = new SourcePosition(leftBraceToken.Line, leftBraceToken.Column, tokenValue: leftBraceToken.Value);
        Expect(LangTokenType.LeftBrace);

        // 空的 {} - 返回空列表
        if (CurrentToken.Type == LangTokenType.RightBrace)
        {
            Expect(LangTokenType.RightBrace);
            return new ListLangValue(new List<LangExpression>(), position);
        }

        // 保存当前位置，用于判断是列表还是字典
        var savedIndex = CurrentIndex;

        // 解析第一个表达式
        var firstExpr = expressionParserFactory().ParseExpression();

        // 检查是否是字典（有冒号）
        if (CurrentToken.Type == LangTokenType.Colon)
        {
            // 是字典，解析为字典
            var elements = new List<TupleLangValue>();

            // 解析第一个键值对
            Expect(LangTokenType.Colon);
            var firstValue = expressionParserFactory().ParseExpression();
            elements.Add(new TupleLangValue(firstExpr, firstValue, position));

            // 解析剩余的键值对
            while (CurrentToken.Type == LangTokenType.Comma)
            {
                Expect(LangTokenType.Comma);

                // 检查是否是尾随逗号
                if (CurrentToken.Type == LangTokenType.RightBrace)
                {
                    break;
                }

                var key = expressionParserFactory().ParseExpression();
                Expect(LangTokenType.Colon);
                var value = expressionParserFactory().ParseExpression();
                elements.Add(new TupleLangValue(key, value, position));
            }

            Expect(LangTokenType.RightBrace);
            return new DictionaryLangValue(elements, position);
        }
        else
        {
            // 是列表，解析为列表
            var elements = new List<LangExpression> { firstExpr };

            // 解析剩余的元素
            while (CurrentToken.Type == LangTokenType.Comma)
            {
                Expect(LangTokenType.Comma);

                // 检查是否是尾随逗号
                if (CurrentToken.Type == LangTokenType.RightBrace)
                {
                    break;
                }

                elements.Add(expressionParserFactory().ParseExpression());
            }

            Expect(LangTokenType.RightBrace);
            return new ListLangValue(elements, position);
        }
    }


    /// <summary>
    /// 已废弃：此方法不再使用，因为 list 关键词已被删除
    /// list = "list" "[" expression ( "," expression )* "]" ;
    /// </summary>
    /// <returns>列表初始化</returns>
    [Obsolete("此方法已废弃，请使用 ParseListOrDictionary 方法")]
    public LangValueType ParseList()
    {
        // list关键字已经被跳过，所以使用当前token的位置（即左括号）
        var listToken = CurrentToken;
        var position = new SourcePosition(listToken.Line, listToken.Column, tokenValue: "list");
        Expect(LangTokenType.LeftBracket);
        var elements = new List<LangExpression>();

        if (CurrentToken.Type == LangTokenType.RightBracket)
        {
            Expect(LangTokenType.RightBracket);
            // 空列表，返回ListValue
            return new ListLangValue(elements, position);
        }

        elements.Add(expressionParserFactory().ParseExpression());
        while (CurrentToken.Type == LangTokenType.Comma)
        {
            Expect(LangTokenType.Comma);
            elements.Add(expressionParserFactory().ParseExpression());
        }

        Expect(LangTokenType.RightBracket);
        // 返回ListValue表示列表
        return new ListLangValue(elements, position);
    }


    /// <summary>
    /// 已废弃：此方法不再使用，已被 ParseListOrDictionary 方法替代
    /// dictionary = "{" dicTuple ( "," dicTuple )* "}" ;
    /// dicTuple = expression ":" expression ;
    /// </summary>
    /// <returns>返回字典</returns>
    [Obsolete("此方法已废弃，请使用 ParseListOrDictionary 方法")]
    public LangValueType ParseDictionary()
    {
        // 处理左括号，只支持 {}
        var leftBraceToken = CurrentToken;
        var dictPosition =
            new SourcePosition(leftBraceToken.Line, leftBraceToken.Column, tokenValue: leftBraceToken.Value);
        Expect(LangTokenType.LeftBrace);

        var rightType = LangTokenType.RightBrace;

        var elements = new List<TupleLangValue>();

        if (CurrentToken.Type == rightType)
        {
            Expect(rightType);
            return new DictionaryLangValue(elements, dictPosition);
        }

        // 解析字典元素
        while (true)
        {
            var key = expressionParserFactory().ParseExpression();
            var colonToken = CurrentToken;
            var tuplePosition = new SourcePosition(colonToken.Line, colonToken.Column, tokenValue: colonToken.Value);
            Expect(LangTokenType.Colon);
            var value = expressionParserFactory().ParseExpression();
            elements.Add(new TupleLangValue(key, value, tuplePosition));

            if (CurrentToken.Type != LangTokenType.Comma)
            {
                break;
            }

            Expect(LangTokenType.Comma);
        }

        Expect(rightType);

        return new DictionaryLangValue(elements, dictPosition);
    }


    /// <summary>
    /// array = "[" expression ( "," expression )* "]" ;
    /// range = "[" expression "~" expression "]" ;
    /// list_comprehension = "[" expression ( "if" expression "else" expression )? "for" identifier "in" expression ( "if" expression )* ( "for" identifier "in" expression ( "if" expression )* )* "]" ;
    /// </summary>
    /// <returns>数组初始化、Range或者列表推导式</returns>
    public LangValueType ParseArrayOrRange()
    {
        var leftBracketToken = CurrentToken;
        var position = new SourcePosition(leftBracketToken.Line, leftBracketToken.Column,
            tokenValue: leftBracketToken.Value);
        Expect(LangTokenType.LeftBracket);
        var elements = new List<LangExpression>();

        if (CurrentToken.Type == LangTokenType.RightBracket)
        {
            Expect(LangTokenType.RightBracket);
            // 空数组，返回ArrayValue
            return new ArrayLangValue(elements, position);
        }

        // 保存当前位置，用于回退
        var exprStartIndex = CurrentIndex;

        elements.Add(expressionParserFactory().ParseExpression());

        // 检查是否是范围表达式（支持包含和排除边界）
        var rangeTokenType = CurrentToken.Type;
        if (rangeTokenType is LangTokenType.Wavy or LangTokenType.WavyLessThan or
            LangTokenType.GreaterThanWavy or LangTokenType.GreaterThanWavyLessThan)
        {
            var rangeToken = CurrentToken;
            var rangePosition = new SourcePosition(rangeToken.Line, rangeToken.Column, tokenValue: rangeToken.Value);

            // 根据token类型确定边界排除规则
            bool includeStart = true;
            bool includeEnd = true;

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
            }

            Expect(rangeTokenType);
            elements.Add(expressionParserFactory().ParseExpression());
            Expect(LangTokenType.RightBracket);

            return new RangeLangValue(elements[0], elements[1], rangePosition, includeStart, includeEnd);
        }

        // 检查是否是列表推导式
        // 列表推导式的特征是: 包含 for 关键字
        // 例如: [expr for var in iterable]
        // 或: [expr if condition else expr for var in iterable]
        var isListComprehension = false;

        // 限制扫描范围到 30 个 token（列表推导式的 for 关键字通常在前30个token内）
        const int maxScanDepth = 30;
        var scanLimit = Math.Min(CurrentIndex + maxScanDepth, Tokens.Count);

        // 扫描剩余的令牌，查找 for 关键字
        for (int i = CurrentIndex; i < scanLimit; i++)
        {
            var tokenType = Tokens[i].Type;

            // 遇到右括号，不是列表推导式
            if (tokenType == LangTokenType.RightBracket)
                break;

            if (tokenType == LangTokenType.For)
            {
                isListComprehension = true;
                break;
            }
        }

        if (isListComprehension)
        {
            // 回退到表达式开始位置，准备解析列表推导式
            CurrentIndex = exprStartIndex;
            elements.Clear();

            // 解析列表推导式
            return ParseListComprehension(position);
        }

        while (CurrentToken.Type == LangTokenType.Comma)
        {
            Expect(LangTokenType.Comma);
            elements.Add(expressionParserFactory().ParseExpression());
        }

        Expect(LangTokenType.RightBracket);
        // 返回ArrayValue表示数组
        return new ArrayLangValue(elements, position);
    }

    /// <summary>
    /// 解析列表推导式
    /// list_comprehension = "[" expression ( "if" expression "else" expression )? "for" identifier "in" expression ( "if" expression )* ( "for" identifier "in" expression ( "if" expression )* )* "]" ;
    /// </summary>
    /// <returns>列表推导式节点</returns>
    public ListComprehension ParseListComprehension(SourcePosition position)
    {
        // 解析表达式部分
        var expression = expressionParserFactory().ParseExpression();

        // 三元表达式已经在 ParseExpression 中处理，这里不需要额外处理

        // 解析 for 循环部分
        var loops = new List<ListComprehension>();

        while (CurrentToken.Type == LangTokenType.For)
        {
            Expect(LangTokenType.For);

            // 解析变量
            var variable = ParseIdentifier();

            Expect(LangTokenType.In);

            // 解析可迭代对象
            var iterable = expressionParserFactory().ParseExpression();

            // 解析条件筛选（可选）
            List<LangExpression> conditions = [];

            while (CurrentToken.Type == LangTokenType.If)
            {
                Expect(LangTokenType.If);
                conditions.Add(expressionParserFactory().ParseExpression());
            }

            // 组合多个条件，使用 AND 操作符连接
            LangExpression? condition = null;
            if (conditions.Count > 0)
            {
                condition = conditions[0];
                for (int i = 1; i < conditions.Count; i++)
                {
                    condition = new Operation(
                        condition,
                        LangTokenType.And,
                        conditions[i],
                        new SourcePosition(CurrentToken.Line, CurrentToken.Column));
                }
            }

            // 创建循环节点
            loops.Add(new ListComprehension(
                expression,
                variable,
                iterable,
                condition,
                null,
                position));
        }

        Expect(LangTokenType.RightBracket);

        if (loops.Count == 0)
        {
            throw CreateSyntaxError("列表推导式必须包含至少一个 for 循环");
        }

        // 处理嵌套循环
        for (int i = loops.Count - 2; i >= 0; i--)
        {
            var currentLoop = loops[i];
            var nextLoop = loops[i + 1];

            // 将下一个循环作为当前循环的嵌套循环
            currentLoop = new ListComprehension(
                currentLoop.Expression,
                currentLoop.Variable,
                currentLoop.Iterable,
                currentLoop.Condition,
                [nextLoop],
                currentLoop.Position);

            loops[i] = currentLoop;
        }

        return loops[0];
    }

    /// <summary>
    /// lambda = "(" idList? ")" "->" block ;
    /// tuple = "(" expression ( "," expression )* ")" ;
    /// </summary>
    /// <returns>返回Lambda或元组</returns>
    public LangExpression ParseLambdaOrTuple()
    {
        var leftParenToken = CurrentToken;
        var position = new SourcePosition(leftParenToken.Line, leftParenToken.Column, tokenValue: leftParenToken.Value);
        Expect(LangTokenType.LeftParen);

        // 保存当前位置，用于回滚
        var savedIndex = CurrentIndex;

        // 检查是否是异步Lambda表达式
        var isAsync = false;
        if (CurrentToken.Type == LangTokenType.Async && Peek().Type == LangTokenType.RightParen &&
            Peek(2).Type == LangTokenType.Arrow)
        {
            // 异步无参数Lambda：async () -> block 或 async () -> expression
            isAsync = true;
            Expect(LangTokenType.Async);
        }

        // 检查是否是Lambda表达式：() -> block 或 (params) -> block
        if (CurrentToken.Type == LangTokenType.RightParen)
        {
            Expect(LangTokenType.RightParen);

            // 检查是否有返回类型注解：():returnType -> ...
            LangId? returnTypeAnnotation = null;
            if (CurrentToken.Type == LangTokenType.Colon && Peek().Type == LangTokenType.Identifier)
            {
                Expect(LangTokenType.Colon);
                var returnTypeName = CurrentToken.Value;
                Expect(LangTokenType.Identifier);
                returnTypeAnnotation = new LangId("", returnTypeName, null, position);
            }

            // 检查箭头符号
            if (CurrentToken.Type == LangTokenType.Arrow)
            {
                Expect(LangTokenType.Arrow);

                BlockStatement block;

                // 检查是块语句还是表达式
                if (CurrentToken.Type == LangTokenType.LeftBrace)
                {
                    // 块语句：():returnType -> { ... }
                    block = statementParserFactory().ParseBlock();
                }
                else
                {
                    // 表达式：():returnType -> expression
                    // 我们需要将表达式转换为块语句，添加return
                    var expr = expressionParserFactory().ParseExpression();
                    var returnStmt = new ReturnStatement(expr, position);
                    block = new BlockStatement([returnStmt]);
                }

                // 创建Lambda表达式，根据isAsync标志决定创建AsyncFuncLangValue还是FuncLangValue
                if (isAsync)
                {
                    return new AsyncFuncLangValue(returnTypeAnnotation, [], block, position);
                }
                else
                {
                    return new FuncLangValue(returnTypeAnnotation, [], block, position, true);
                }
            }
        }

        // 检查是否是有参数的Lambda表达式
        // 只有当括号内的内容是标识符列表时，才可能是Lambda表达式
        // 如果是其他表达式（如数字、字符串、表达式调用等），则是元组
        var isLambda = true;
        var ids = new List<LangId>();

        // 检查是否是异步Lambda表达式（有参数）
        if (CurrentToken.Type == LangTokenType.Async && Peek().Type == LangTokenType.Identifier)
        {
            // 异步有参数Lambda：async (params) -> block 或 async (params) -> expression
            isAsync = true;
            Expect(LangTokenType.Async);
        }

        // 检查第一个元素是否是标识符
        if (CurrentToken.Type == LangTokenType.Identifier)
        {
            // 解析第一个参数，允许类型注解
            ids.Add(ParseLambdaParameter());

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

                ids.Add(ParseLambdaParameter());
            }

            // 检查是否有箭头符号或返回类型注解
            if (isLambda && CurrentToken.Type == LangTokenType.RightParen)
            {
                Expect(LangTokenType.RightParen);

                // 检查是否有返回类型注解：(params):returnType -> ...
                LangId? returnTypeAnnotation = null;
                if (CurrentToken.Type == LangTokenType.Colon)
                {
                    Expect(LangTokenType.Colon);

                    // 解析简单返回类型注解
                    if (CurrentToken.Type == LangTokenType.Identifier)
                    {
                        var returnTypeName = CurrentToken.Value;
                        Expect(LangTokenType.Identifier);

                        // 验证是否为支持的类型
                        var supportedTypes = new[]
                            { "int", "double", "string", "bool", "char", "void", "list", "dict" };
                        if (!supportedTypes.Contains(returnTypeName))
                        {
                            throw CreateSyntaxError(
                                $"不支持的返回类型注解: {returnTypeName}。支持的类型: int, double, string, bool, char, void, list, dict");
                        }

                        returnTypeAnnotation = new LangId("", returnTypeName, null, position);
                    }
                    else
                    {
                        throw CreateSyntaxError($"期望返回类型名称，但得到 {CurrentToken.Type}");
                    }
                }

                // 检查箭头符号
                if (CurrentToken.Type == LangTokenType.Arrow)
                {
                    Expect(LangTokenType.Arrow);

                    BlockStatement block;

                    // 检查是块语句还是表达式
                    if (CurrentToken.Type == LangTokenType.LeftBrace)
                    {
                        // 块语句：(params):returnType -> { ... }
                        block = statementParserFactory().ParseBlock();
                    }
                    else
                    {
                        // 表达式：(params):returnType -> expression
                        // 我们需要将表达式转换为块语句，添加return
                        var expr = expressionParserFactory().ParseExpression();
                        var returnStmt = new ReturnStatement(expr, position);
                        block = new BlockStatement([returnStmt]);
                    }

                    // 创建Lambda表达式，根据isAsync标志决定创建AsyncFuncLangValue还是FuncLangValue
                    if (isAsync)
                    {
                        return new AsyncFuncLangValue(returnTypeAnnotation, ids, block, position);
                    }
                    else
                    {
                        return new FuncLangValue(returnTypeAnnotation, ids, block, position, true);
                    }
                }
            }

            // 严格检查：如果看起来像 Lambda 参数列表但缺少 ->
            if (isLambda && CurrentToken.Type == LangTokenType.RightParen)
            {
                var rightParenLine = CurrentToken.Line;
                var nextToken = Peek();

                // 检查右括号后是否还有内容，且在同一行，且不是分号
                if (nextToken.Type != LangTokenType.Semicolon &&
                    nextToken.Type != LangTokenType.EndOfFile &&
                    nextToken.Line == rightParenLine)
                {
                    // 构建参数列表字符串用于错误消息
                    var paramList = string.Join(", ", ids.Select(id => id.IdName));

                    throw CreateSyntaxError(
                        $"语法错误：Lambda 表达式缺少箭头 '->'。\n" +
                        $"检测到参数列表 '({paramList})'，但缺少 '->' 符号。\n" +
                        $"建议：使用 '({paramList}) -> expression' 或 '({paramList}) -> {{ ... }}' 格式定义 Lambda 表达式。\n" +
                        $"如果这不是 Lambda 表达式，请在参数列表后添加分号 ';' 或换行符。");
                }
            }
        }
        else
        {
            // 第一个元素不是标识符，不是Lambda表达式
            // isLambda = false;
        }

        // 元组：(expr1, expr2, ...)
        // 回滚到左括号后，重新解析为表达式列表
        CurrentIndex = savedIndex;

        var elements = new List<LangExpression>();

        // 空括号情况：()
        if (CurrentToken.Type == LangTokenType.RightParen)
        {
            // 空括号没有箭头，语义不明确，抛出错误
            // 注意：() -> expr 的Lambda形式在前面已经处理过了（第1890行）
            throw CreateSyntaxError(
                "语法错误：空括号 '()' 不能作为表达式。建议：如果要定义无参Lambda，请使用 '() -> expression' 或 '() -> { ... }' 格式。");
        }

        // 解析第一个元素
        elements.Add(expressionParserFactory().ParseExpression());

        // 检查是否有逗号
        bool hasComma = CurrentToken.Type == LangTokenType.Comma;

        // 解析更多元素
        while (CurrentToken.Type == LangTokenType.Comma)
        {
            Expect(LangTokenType.Comma);

            // 检查是否还有元素，或者是单元素元组的结束
            if (CurrentToken.Type == LangTokenType.RightParen)
            {
                // 单元素元组，没有更多元素
                break;
            }

            elements.Add(expressionParserFactory().ParseExpression());
        }

        // 必须是右括号，否则抛出语法错误
        Expect(LangTokenType.RightParen);

        // 构建元组，支持任意数量元素
        if (elements.Count == 1)
        {
            // 检查是否是单元素元组还是括号表达式
            // 如果没有逗号，那么是括号表达式：(expr)
            // 如果有逗号，那么是单元素元组：(expr,)
            if (!hasComma)
            {
                // 单个表达式，返回表达式本身，不是元组
                return elements[0];
            }

            // 单元素元组：(expr,)
            return new TupleLangValue(elements[0], new NullLangValue(), position);
        }

        if (elements.Count == 2)
        {
            // 双元素元组：(expr1, expr2)
            return new TupleLangValue(elements[0], elements[1], position);
        }

        // 多元素元组：(expr1, expr2, expr3, ...) - 递归构建嵌套元组
        var tuple = new TupleLangValue(elements[0], elements[1], position);
        for (int i = 2; i < elements.Count; i++)
        {
            tuple = new TupleLangValue(tuple, elements[i], position);
        }

        return tuple;
    }

    /// <summary>
    /// 解析字符串树，支持模板字符串
    /// 支持格式：
    /// - $"string" 简单模板字符串
    /// - $"string {placeholder}" 带占位符的模板
    /// - $"string ${expression} string" 混合模板
    /// </summary>
    /// <returns>字符串树</returns>
    public LangExpression ParseStringTemplate()
    {
        // 检查当前token是否是Dollar（用于字符串插值）
        if (CurrentToken.Type == LangTokenType.Dollar)
        {
            var dollarToken = CurrentToken;
            var position = new SourcePosition(dollarToken.Line, dollarToken.Column, tokenValue: dollarToken.Value);

            // 跳过$符号
            Expect(LangTokenType.Dollar);

            // 处理$"string" 格式（字符串插值）
            if (CurrentToken.Type == LangTokenType.String)
            {
                var stringValue = CurrentToken.Value;
                Expect(LangTokenType.String);

                // 完整的字符串模板解析
                var parts = new List<LangExpression>();
                var i = 0;
                var len = stringValue.Length;

                // Debug: 打印完整的字符串内容
                // System.Console.WriteLine($"Debug: 字符串模板完整内容: '{stringValue}', 长度: {len}");

                while (i < len)
                {
                    var c = stringValue[i];

                    if (c == '{' && i + 1 < len)
                    {
                        var next = stringValue[i + 1];

                        if (next == '{')
                        {
                            // 转义的 {{，添加一个 {
                            parts.Add(new StringLangValue("{", position));
                            i += 2;
                        }
                        else
                        {
                            // 普通的 {，开始解析表达式
                            i += 1;
                            var exprStart = i;
                            var braceCount = 1;
                            var inString = false;
                            var stringChar = '\0'; // 记录当前字符串的引号类型（单引号或双引号）

                            // 查找匹配的 }
                            var foundMatchingBrace = false;
                            while (i < len && braceCount > 0)
                            {
                                c = stringValue[i];

                                // Debug: 详细跟踪
                                // System.Console.WriteLine($"Debug: i={i}, c='{c}', braceCount={braceCount}, inString={inString}");

                                // 处理字符串中的引号
                                if (!inString && (c == '"' || c == '\''))
                                {
                                    inString = true;
                                    stringChar = c;
                                    // System.Console.WriteLine($"Debug: 进入字符串模式, stringChar='{stringChar}'");
                                    i++;
                                    continue;
                                }

                                if (inString)
                                {
                                    // 在字符串中，查找匹配的结束引号
                                    if (c == stringChar)
                                    {
                                        // 检查是否是转义引号
                                        var isEscaped = false;
                                        var backslashCount = 0;
                                        var j = i - 1;
                                        while (j >= 0 && stringValue[j] == '\\')
                                        {
                                            backslashCount++;
                                            j--;
                                        }
                                        isEscaped = (backslashCount % 2) == 1;

                                        if (!isEscaped)
                                        {
                                            inString = false;
                                            stringChar = '\0';
                                        }
                                    }
                                    i++;
                                    continue;
                                }

                                // 不在字符串中，才处理大括号
                                if (c == '{')
                                {
                                    braceCount++;
                                }
                                else if (c == '}')
                                {
                                    braceCount--;
                                    if (braceCount == 0)
                                    {
                                        foundMatchingBrace = true;
                                        break;
                                    }
                                }

                                i++;
                            }

                            if (foundMatchingBrace)
                            {
                                // 提取表达式字符串
                                var exprStr = stringValue.Substring(exprStart, i - exprStart).Trim();
                                // Debug: 打印提取的表达式
                                // System.Console.WriteLine($"Debug: 提取到表达式: '{exprStr}'");

                                // 检查表达式是否为空
                                if (string.IsNullOrWhiteSpace(exprStr))
                                {
                                    throw CreateSyntaxError("语法错误：字符串模板的花括号内不能为空。建议：在花括号内提供有效的表达式，如 ${variableName}。");
                                }

                                // 完整的表达式解析：支持所有表达式类型，包括点操作符
                                // 将表达式包装成括号表达式，然后作为赋值语句的右值
                                // 使用括号可以避免三元运算符中的 if 被误认为 if 语句
                                var wrappedExpr = $"__temp <- ({exprStr})";

                                // 将表达式字符串转换为Token流
                                var exprTokens = LangTokenizer.Tokenize(wrappedExpr);

                                // 创建一个新的LangParser实例来解析这个表达式
                                var exprParser = new LangParser(exprTokens, wrappedExpr,
                                    $"{Context.FileName}:template");

                                // 解析完整表达式
                                var programBlock = exprParser.ParseProgram();
                                if (programBlock.Count > 0 && programBlock[0] is SetStatement setStmt)
                                {
                                    parts.Add(setStmt.Value);
                                }
                                else
                                {
                                    throw CreateSyntaxError("无法解析字符串模板中的表达式");
                                }

                                i++;
                            }
                            else
                            {
                                // 未找到匹配的 }，抛出语法错误
                                // Debug: 打印详细信息
                                // System.Console.WriteLine($"Debug: 未找到匹配的大括号, exprStart={exprStart}, i={i}, len={len}, braceCount={braceCount}");
                                // System.Console.WriteLine($"Debug: 字符串片段: '{stringValue.Substring(exprStart, Math.Min(i - exprStart + 20, stringValue.Length - exprStart))}'");
                                throw CreateSyntaxError("字符串模板中缺少匹配的右大括号 '}'");
                            }
                        }
                    }
                    else if (c == '}')
                    {
                        if (i + 1 < len && stringValue[i + 1] == '}')
                        {
                            // 转义的 }}，添加一个 }
                            parts.Add(new StringLangValue("}", position));
                            i += 2;
                        }
                        else
                        {
                            // 普通的 }，直接添加
                            parts.Add(new StringLangValue("}", position));
                            i++;
                        }
                    }
                    else
                    {
                        // 普通字符，添加到结果中
                        var start = i;
                        while (i < len && stringValue[i] != '{' && stringValue[i] != '}')
                        {
                            i++;
                        }

                        var text = stringValue.Substring(start, i - start);
                        if (!string.IsNullOrEmpty(text))
                        {
                            parts.Add(new StringLangValue(text, position));
                        }
                    }
                }

                return new StringTemplateValue(parts, position);
            }
        }

        // 如果不是字符串插值，返回普通表达式
        return ParsePrimary();
    }


    /// <summary>
    /// 解析标识符，支持带类型注解的标识符：identifier:type
    /// 允许将关键字用作标识符
    /// </summary>
    /// <returns>标识符</returns>
    public LangId ParseIdentifier()
    {
        var identifierToken = CurrentToken;
        var position = new SourcePosition(identifierToken.Line, identifierToken.Column,
            tokenValue: identifierToken.Value);
        var value = identifierToken.Value;

        // 检查当前token是否是标识符或关键字
        if (CurrentToken.Type is LangTokenType.Identifier or LangTokenType.Func or LangTokenType.Class
            or LangTokenType.If or LangTokenType.Else or LangTokenType.While or LangTokenType.For
            or LangTokenType.Return or LangTokenType.Import or LangTokenType.True or LangTokenType.False)
        {
            CurrentIndex++;
        }
        else
        {
            Expect(LangTokenType.Identifier);
        }

        // 默认不处理类型注解
        return new LangId(value, position: position);
    }

    /// <summary>
    /// 解析字符串字面量
    /// </summary>
    /// <returns>字符串值</returns>
    public StringLangValue ParseStringLiteral()
    {
        var stringToken = CurrentToken;
        var position = new SourcePosition(stringToken.Line, stringToken.Column, tokenValue: stringToken.Value);
        var value = stringToken.Value;
        Expect(LangTokenType.String);
        return new StringLangValue(value, position);
    }

    /// <summary>
    /// 解析字符字面量
    /// </summary>
    /// <returns>字符值</returns>
    public CharLangValue ParseCharLiteral()
    {
        var charToken = CurrentToken;
        var position = new SourcePosition(charToken.Line, charToken.Column, tokenValue: charToken.Value);
        var value = charToken.Value;
        Expect(LangTokenType.Char);
        char c;
        try
        {
            c = char.Parse(value);
        }
        catch
        {
            c = value[0];
        }

        return new CharLangValue(c, position);
    }

    /// <summary>
    /// 解析整数字面量
    /// </summary>
    /// <returns>整数值或双精度浮点数值</returns>
    public LangValueType ParseIntLiteral()
    {
        var numberToken = CurrentToken;
        var position = CreateSourcePosition(numberToken);
        var value = numberToken.Value;
        Expect(LangTokenType.Number);

        // 尝试解析为整数，如果超出Int32范围则转为双精度浮点数
        try
        {
            return new IntLangValue(int.Parse(value), position);
        }
        catch
        {
            // 整数解析失败，可能是超出范围，尝试解析为双精度浮点数
            try
            {
                return new DoubleLangValue(double.Parse(value), position);
            }
            catch
            {
                throw CreateSyntaxError($"无法解析数字字面量 '{value}'");
            }
        }
    }

    /// <summary>
    /// 解析双精度字面量
    /// </summary>
    /// <returns>双精度值</returns>
    public DoubleLangValue ParseDoubleLiteral()
    {
        var numberToken = CurrentToken;
        var position = CreateSourcePosition(numberToken);
        var value = numberToken.Value;
        Expect(LangTokenType.Number);
        try
        {
            return new DoubleLangValue(double.Parse(value), position);
        }
        catch
        {
            throw CreateSyntaxError("无法解析双精度字面量");
        }
    }

    /// <summary>
    /// 解析布尔字面量
    /// </summary>
    /// <returns>布尔值</returns>
    public BoolLangValue ParseBoolLiteral()
    {
        var boolToken = CurrentToken;
        var position = CreateSourcePosition(boolToken);
        var value = boolToken.Type == LangTokenType.True;
        Expect(boolToken.Type);
        return new BoolLangValue(value, position);
    }

    public NullLangValue ParseNullLiteral()
    {
        var nullToken = CurrentToken;
        var position = CreateSourcePosition(nullToken);
        Expect(LangTokenType.Null);
        return new NullLangValue(position);
    }

    /// <summary>
    /// 解析列表初始化或切片
    /// </summary>
    /// <returns>列表初始化或切片</returns>
    public LangExpression ParseListInitOrSlice()
    {
        var identifier = ParseIdentifier();
        var leftBracketToken = CurrentToken;
        var position = new SourcePosition(leftBracketToken.Line, leftBracketToken.Column,
            tokenValue: leftBracketToken.Value);
        Expect(LangTokenType.LeftBracket);

        // 检查是否是开放式切片的开始：list[:...] 或 list[::...]
        if (CurrentToken.Type == LangTokenType.Colon)
        {
            // 开放式切片：[:end], [:end:step], 或 [::step]
            Expect(LangTokenType.Colon);

            LangExpression? end = null;
            LangExpression? step = null;

            // 检查是否有 end 参数（如果不是冒号或右括号）
            if (CurrentToken.Type != LangTokenType.Colon && CurrentToken.Type != LangTokenType.RightBracket)
            {
                end = expressionParserFactory().ParseExpression();
            }

            // 检查是否有第二个冒号（步长参数）
            if (CurrentToken.Type == LangTokenType.Colon)
            {
                Expect(LangTokenType.Colon);
                // 只有在不是右括号时才解析 step
                if (CurrentToken.Type != LangTokenType.RightBracket)
                {
                    step = expressionParserFactory().ParseExpression();
                }
            }

            Expect(LangTokenType.RightBracket);
            return new SliceLangValue(identifier, null, end, step);
        }

        // 处理切片或索引访问：list[start:end], list[start:end:step], list[start:], list[start::step], list[index]
        // 尝试解析第一个表达式（可能是索引或切片的起始值）
        var start = expressionParserFactory().ParseExpression();

        if (CurrentToken.Type == LangTokenType.Colon)
        {
            // 这是切片操作
            Expect(LangTokenType.Colon);

            LangExpression? end = null;
            LangExpression? step = null;

            // 检查是否有 end 参数（如果不是冒号或右括号）
            if (CurrentToken.Type != LangTokenType.Colon && CurrentToken.Type != LangTokenType.RightBracket)
            {
                end = expressionParserFactory().ParseExpression();
            }

            // 检查是否有第二个冒号（步长参数）
            if (CurrentToken.Type == LangTokenType.Colon)
            {
                Expect(LangTokenType.Colon);
                // 只有在不是右括号时才解析 step
                if (CurrentToken.Type != LangTokenType.RightBracket)
                {
                    step = expressionParserFactory().ParseExpression();
                }
            }

            Expect(LangTokenType.RightBracket);
            return new SliceLangValue(identifier, start, end, step);
        }

        if (CurrentToken.Type == LangTokenType.RightBracket)
        {
            // 列表访问：list[index] - 使用 OldItem
            Expect(LangTokenType.RightBracket);
            return new LangListItem(identifier, start, position);
        }

        // 如果既不是冒号也不是右方括号，则为语法错误
        throw CreateSyntaxError(
            $"语法错误：索引或切片语法错误。在 '{CurrentToken.Value}' 处期望 ':' 或 ']'。建议：使用 array[index] 进行索引访问，或使用 array[start:end] 进行切片。");
    }

    /// <summary>
    /// 解析函数调用或实例化
    /// </summary>
    /// <returns>函数调用或实例化</returns>
    public Instance ParseInstantiate()
    {
        var identifier = ParseIdentifier();
        Expect(LangTokenType.LeftParen);
        var args = functionParser.ParseArgList();
        Expect(LangTokenType.RightParen);
        return new Instance(identifier, args);
    }

    /// <summary>
    /// 解析Lambda参数（支持简单类型注解）
    /// </summary>
    /// <returns>LangId标识符</returns>
    private LangId ParseLambdaParameter()
    {
        var identifierToken = CurrentToken;
        var position = new SourcePosition(identifierToken.Line, identifierToken.Column,
            tokenValue: identifierToken.Value);
        var value = identifierToken.Value;

        // 检查当前token是否是标识符或关键字
        if (CurrentToken.Type is LangTokenType.Identifier or LangTokenType.Func or LangTokenType.Class
            or LangTokenType.If or LangTokenType.Else or LangTokenType.While or LangTokenType.For
            or LangTokenType.Return or LangTokenType.Import or LangTokenType.True or LangTokenType.False)
        {
            CurrentIndex++;
        }
        else
        {
            Expect(LangTokenType.Identifier);
        }

        // 处理类型注解：identifier:type (只支持简单类型)
        var typeAnnotation = "";
        if (CurrentToken.Type == LangTokenType.Colon)
        {
            Expect(LangTokenType.Colon);

            // 解析简单类型注解
            if (CurrentToken.Type == LangTokenType.Identifier)
            {
                typeAnnotation = CurrentToken.Value;
                Expect(LangTokenType.Identifier);

                // 验证是否为支持的类型
                var supportedTypes = new[] { "int", "double", "string", "bool", "char", "void", "list", "dict" };
                if (!supportedTypes.Contains(typeAnnotation))
                {
                    throw CreateSyntaxError(
                        $"不支持的类型注解: {typeAnnotation}。支持的类型: int, double, string, bool, char, void, list, dict");
                }
            }
            else
            {
                throw CreateSyntaxError($"期望类型名称，但得到 {CurrentToken.Type}");
            }
        }

        return new LangId(value, typeAnnotation, null, position);
    }

    #endregion
}