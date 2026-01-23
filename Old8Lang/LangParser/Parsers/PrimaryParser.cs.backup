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
    FunctionParser functionParser,
    LinqParser linqParser)
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
            return new ListLangValue(new List<LangExpression>(), position: position);
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
            return new DictionaryLangValue(elements, null, null, position);
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
            return new ListLangValue(elements, position: position);
        }
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
            return new ArrayLangValue(elements, null, position);
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
        return new ArrayLangValue(elements, null, position);
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
                returnTypeAnnotation = new LangId("", returnTypeName, null, position: position);
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
                    return new FuncLangValue(returnTypeAnnotation, [], block, null, position, true);
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
            // 预读检查：如果是 identifier: number/string/... 则不是Lambda，而是命名元组
            if (Peek(1).Type == LangTokenType.Colon)
            {
                var tokenAfterColon = Peek(2);
                // 如果冒号后不是类型名称（identifier），则是命名元组
                if (tokenAfterColon.Type != LangTokenType.Identifier)
                {
                    isLambda = false;
                }
                else
                {
                    // 冒号后是标识符，但需要检查是否为支持的类型名称
                    var potentialType = tokenAfterColon.Value;
                    var supportedTypes = new[] { "int", "double", "string", "bool", "char", "void", "list", "dict", "any", "tuple", "array" };
                    var isGenericTypeParameter = potentialType.Length == 1 && char.IsUpper(potentialType[0]);

                    if (!supportedTypes.Contains(potentialType) && !isGenericTypeParameter)
                    {
                        // 不是支持的类型，说明是命名元组
                        isLambda = false;
                    }
                }
            }

            // 如果还认为是Lambda，继续解析Lambda参数
            if (isLambda)
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

                        returnTypeAnnotation = new LangId("", returnTypeName, null, position: position);
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
                        return new FuncLangValue(returnTypeAnnotation, ids, block, null, position, true);
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

        // 元组：(expr1, expr2, ...) 或命名元组：(x: expr1, y: expr2, ...)
        // 回滚到左括号后，重新解析为表达式列表
        CurrentIndex = savedIndex;

        var elements = new List<LangExpression>();
        var fieldNames = new List<string?>(); // 字段名列表，null表示未命名
        bool hasAnyFieldName = false; // 标记是否有任何命名字段

        // 空括号情况：()
        if (CurrentToken.Type == LangTokenType.RightParen)
        {
            // 空括号没有箭头，语义不明确，抛出错误
            // 注意：() -> expr 的Lambda形式在前面已经处理过了（第1890行）
            throw CreateSyntaxError(
                "语法错误：空括号 '()' 不能作为表达式。建议：如果要定义无参Lambda，请使用 '() -> expression' 或 '() -> { ... }' 格式。");
        }

        // 解析第一个元素（可能是命名字段）
        var firstElement = ParseTupleElementWithOptionalName(out string? firstName);
        elements.Add(firstElement);
        fieldNames.Add(firstName);
        if (firstName is not null) hasAnyFieldName = true;

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

            var element = ParseTupleElementWithOptionalName(out string? fieldName);
            elements.Add(element);
            fieldNames.Add(fieldName);
            if (fieldName is not null) hasAnyFieldName = true;
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

            // 单元素元组：(expr,) 或 (x: expr,)
            // 注意：单元素元组不支持命名字段，因为命名字段需要多个元素才有意义
            // 使用列表构造函数创建单元素元组，避免引入 NullLangValue
            return new TupleLangValue([elements[0]], position);
        }

        // 多元素元组：使用支持命名字段的构造函数
        if (hasAnyFieldName)
        {
            // 有命名字段，使用命名元组构造函数
            return new TupleLangValue(elements, fieldNames, position);
        }
        else
        {
            // 无命名字段，统一使用列表构造函数
            return new TupleLangValue(elements, position);
        }
    }

    /// <summary>
    /// 解析元组元素，支持可选的命名语法：name: expression 或 expression
    /// </summary>
    /// <param name="fieldName">输出参数，字段名（如果有）</param>
    /// <returns>元组元素表达式</returns>
    private LangExpression ParseTupleElementWithOptionalName(out string? fieldName)
    {
        fieldName = null;

        // 尝试预读：检查模式是否为 identifier: expression
        int savedIndex = CurrentIndex;

        // 检查当前token是否是标识符
        if (CurrentToken.Type == LangTokenType.Identifier)
        {
            string potentialFieldName = CurrentToken.Value;
            CurrentIndex++; // 移动到下一个token

            // 检查是否紧跟冒号
            if (CurrentToken.Type == LangTokenType.Colon)
            {
                // 确认是命名元组语法：name: expression
                Expect(LangTokenType.Colon);
                fieldName = potentialFieldName;

                // 解析冒号后的表达式
                return expressionParserFactory().ParseExpression();
            }
            else
            {
                // 不是命名元组语法，回滚并正常解析表达式
                CurrentIndex = savedIndex;
            }
        }

        // 普通元组元素，没有命名
        return expressionParserFactory().ParseExpression();
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

                                // 处理字符串中的引号
                                if (!inString && (c == '"' || c == '\''))
                                {
                                    inString = true;
                                    stringChar = c;
                                    i++;
                                    continue;
                                }

                                if (inString)
                                {
                                    // 在字符串中，查找匹配的结束引号
                                    if (c == stringChar)
                                    {
                                        // 检查是否是转义引号
                                        var backslashCount = 0;
                                        var j = i - 1;
                                        while (j >= 0 && stringValue[j] == '\\')
                                        {
                                            backslashCount++;
                                            j--;
                                        }

                                        var isEscaped = backslashCount % 2 == 1;

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

        char c = ParseCharValue(value);

        return new CharLangValue(c, position);
    }

    /// <summary>
    /// 解析字符值，支持Unicode转义序列
    /// </summary>
    /// <param name="value">字符字面量值</param>
    /// <returns>解析后的字符</returns>
    private static char ParseCharValue(string value)
    {
        // 移除首尾的单引号
        if (value.StartsWith('\'') && value.EndsWith('\'') && value.Length >= 3)
        {
            var content = value.Substring(1, value.Length - 2);

            // 处理转义字符
            if (content.StartsWith('\\'))
            {
                switch (content)
                {
                    case "\\n": return '\n';
                    case "\\t": return '\t';
                    case "\\r": return '\r';
                    case "\\": return '\\';
                    case "\'": return '\'';
                    case "\"": return '\"';
                    case "\\0": return '\0';
                    default:
                        // 处理Unicode转义序列 \uXXXX
                        if (EscapeSequenceHelper.TryParseUnicodeEscapeFromContent(content, out var unicodeChar))
                        {
                            return unicodeChar;
                        }

                        // 处理十六进制转义序列 \xXX
                        if (EscapeSequenceHelper.TryParseHexEscapeFromContent(content, out var hexChar))
                        {
                            return hexChar;
                        }

                        break;
                }
            }

            // 普通字符或未识别的转义序列，取第一个字符
            return content[0];
        }

        // 如果格式不正确，尝试直接解析
        try
        {
            return char.Parse(value);
        }
        catch
        {
            return value.Length > 0 ? value[0] : '\0';
        }
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

        // 尝试解析为整数，如果超出Int32范围则尝试Long，最后才转为双精度浮点数
        try
        {
            return new IntLangValue(int.Parse(value), position);
        }
        catch
        {
            // 整数解析失败，可能是超出范围，尝试解析为 long
            try
            {
                long longValue = long.Parse(value);
                // 成功解析为 long，但 IntLangValue 只支持 int
                // 我们需要创建一个 LongLangValue 或者将其存储为 double
                // 由于 Old8Lang 没有 LongLangValue，我们暂时使用 double
                // 但为了保持精度，我们应该添加 LongLangValue 支持
                return new DoubleLangValue(longValue, position);
            }
            catch
            {
                // long 解析也失败，尝试解析为双精度浮点数
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
        functionParser.ParseArgList(out var positionalArgs, out var namedArgs);
        Expect(LangTokenType.RightParen);
        return new Instance(identifier, positionalArgs, namedArgs);
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
                var supportedTypes = new[] { "int", "double", "string", "bool", "char", "void", "list", "dict", "any" };
                // 允许单个大写字母作为泛型类型参数（如 T, U, V）
                var isGenericTypeParameter = typeAnnotation.Length == 1 && char.IsUpper(typeAnnotation[0]);

                if (!supportedTypes.Contains(typeAnnotation) && !isGenericTypeParameter)
                {
                    throw CreateSyntaxError(
                        $"不支持的类型注解: {typeAnnotation}。支持的类型: int, double, string, bool, char, void, list, dict, any 或单个大写字母作为泛型类型参数");
                }
            }
            else
            {
                throw CreateSyntaxError($"期望类型名称，但得到 {CurrentToken.Type}");
            }
        }

        return new LangId(value, typeAnnotation, null, position: position);
    }

    /// <summary>
    /// 解析 match 表达式
    /// 语法: match expression { case pattern -> expression ... }
    /// 支持多种模式：
    /// 1. 值匹配: case 0 -> "zero"
    /// 2. 变量绑定: case x -> "value is " + x
    /// 3. 通配符: case _ -> "default"
    /// 4. 元组解构: case (x, 0) -> "on X-axis"
    /// 5. 类型匹配: case x:int -> "int value"
    /// 6. 范围匹配: case [0~12] -> "child"
    /// 7. 守卫条件: case x:int if x > 0 -> "positive"
    /// 8. default 分支: default -> "default value"
    /// </summary>
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

    /// <summary>
    /// 解析泛型实例化表达式
    /// 语法：Box&lt;int>() 或 map&lt;string>(arr, func) 或 Box&lt;int> (不调用构造)
    /// </summary>
    /// <returns>泛型实例化表达式</returns>
    private LangExpression ParseGenericInstantiation()
    {
        // 解析基础表达式（类名或函数名）
        var baseExpression = ParseIdentifier();
        var position = CreateSourcePosition(CurrentToken);

        // 解析类型参数列表（支持嵌套泛型）
        var typeArgumentsString = ParseGenericTypeArguments();

        // 拆分类型参数（处理逗号分隔的多个类型参数）
        // 注意：需要小心处理嵌套泛型，比如 "List<int>, Dict<string, int>"
        var typeArguments = SplitTypeArguments(typeArgumentsString);

        // 检查是否有调用参数
        if (CurrentToken.Type == LangTokenType.LeftParen)
        {
            // 有调用参数：Box<int>(...) 或 map<string>(...)
            Expect(LangTokenType.LeftParen);
            var callArguments = new List<LangExpression>();

            while (CurrentToken.Type != LangTokenType.RightParen)
            {
                if (CurrentToken.Type == LangTokenType.EndOfFile)
                {
                    throw CreateSyntaxError("意外的文件结束符，期望 ')'");
                }

                var arg = expressionParserFactory().ParseExpression();
                callArguments.Add(arg);

                if (CurrentToken.Type == LangTokenType.Comma)
                {
                    Expect(LangTokenType.Comma);
                    continue;
                }

                break;
            }

            Expect(LangTokenType.RightParen);

            // 返回泛型函数调用或类实例化
            return new GenericInstanceExpression(baseExpression, typeArguments, callArguments, position);
        }
        else
        {
            // 没有调用参数：Box<int> (只是类型引用，不调用构造)
            return new GenericInstanceExpression(baseExpression, typeArguments, position);
        }
    }

    /// <summary>
    /// 启发式判断 &lt; 是否为泛型的开始（而非比较运算符）
    ///
    /// 泛型特征：identifier &lt; TypeName [, TypeName]* > (
    ///
    /// 比较运算符特征：identifier &lt; number/identifier/expression
    /// </summary>
    /// <returns>如果可能是泛型返回 true</returns>
    private bool IsLikelyGenericInstantiation()
    {
        // 保存当前位置
        var savedIndex = CurrentIndex;

        try
        {
            // CurrentToken 是标识符（如 Box），Peek() 是 <
            // 记录外层标识符（调用者），用于上下文判断
            string? outerIdentifier = null;
            if (CurrentToken.Type == LangTokenType.Identifier)
            {
                outerIdentifier = CurrentToken.Value;
                CurrentIndex++;
            }

            // 现在 CurrentToken 应该是 <
            if (CurrentToken.Type != LangTokenType.LessThan)
                return false;

            CurrentIndex++;

            // 检查 < 后面的 token
            // 泛型：应该是类型名（标识符，且通常首字母大写或已知类型）
            // 比较：可能是数字、小写标识符、运算符等

            if (CurrentToken.Type != LangTokenType.Identifier)
                return false;

            // 记住内层标识符（类型参数）
            var innerIdentifier = CurrentToken.Value;
            CurrentIndex++;

            // 检查标识符后面的 token
            var nextTokenType = CurrentToken.Type;

            // 检查外层标识符是否是明确的类型名
            var outerIsTypeName = outerIdentifier is not null &&
                                  (char.IsUpper(outerIdentifier[0]) || IsBuiltInTypeName(outerIdentifier));

            // 强泛型证据：这些模式只能是泛型，不可能是比较运算符
            // 1. Type> - 泛型结束符
            // 2. Type, - 多个类型参数
            // 3. Type? - 可空类型
            if (nextTokenType == LangTokenType.GreaterThan ||
                nextTokenType == LangTokenType.Comma ||
                nextTokenType == LangTokenType.Question)
            {
                // 如果外层是明确的类型名（如 List, Box, int），那么即使内层是小写也是泛型
                // 例如：List<a> - List是类型名，a是自定义类型参数
                if (outerIsTypeName)
                {
                    return true;
                }

                // 如果外层不是类型名，再检查内层
                // 例如：int> 或 Box> 肯定是泛型，但 a < b > c 可能是比较链
                if (char.IsUpper(innerIdentifier[0]) || IsBuiltInTypeName(innerIdentifier))
                {
                    return true;
                }

                // 两者都不是类型名，保守返回 false
                return false;
            }

            // 强比较证据：这些模式只能是比较运算符
            if (nextTokenType == LangTokenType.Plus ||
                nextTokenType == LangTokenType.Minus ||
                nextTokenType == LangTokenType.Star ||
                nextTokenType == LangTokenType.Slash ||
                nextTokenType == LangTokenType.And ||
                nextTokenType == LangTokenType.Or ||
                nextTokenType == LangTokenType.RightParen ||
                nextTokenType == LangTokenType.LeftParen ||
                nextTokenType == LangTokenType.EndOfFile)
            {
                return false;
            }

            // 嵌套泛型的情况：Type< - 需要进一步检查
            if (nextTokenType == LangTokenType.LessThan)
            {
                // 如果外层是类型名（如 List<List<a>>），那么即使内层是小写也是泛型
                if (outerIsTypeName)
                {
                    return true;
                }

                // 只有类型名（首字母大写或内置类型）才可能是嵌套泛型
                if (char.IsUpper(innerIdentifier[0]) || IsBuiltInTypeName(innerIdentifier))
                {
                    return true;
                }

                // 小写变量名 + < 很可能是链式比较：a < b < c
                return false;
            }

            // 默认：只有明确的类型名才当作泛型
            // 如果外层是类型名，那么即使内层是小写也是泛型
            if (outerIsTypeName)
            {
                return true;
            }

            if (char.IsUpper(innerIdentifier[0]) || IsBuiltInTypeName(innerIdentifier))
            {
                return true;
            }

            // 所有其他情况：保守策略，当作比较运算符
            return false;
        }
        finally
        {
            // 恢复位置
            CurrentIndex = savedIndex;
        }
    }

    /// <summary>
    /// 判断是否为内置类型名称
    /// </summary>
    private bool IsBuiltInTypeName(string name)
    {
        return name switch
        {
            "int" or "string" or "double" or "bool" or "char" or
                "long" or "float" or "byte" or "short" or "decimal" or
                "void" or "object" or "dynamic" => true,
            _ => false
        };
    }

    /// <summary>
    /// 解析泛型类型参数，支持嵌套泛型
    /// 语法：&lt;int>, &lt;T>, &lt;List&lt;int>>, &lt;List&lt;List&lt;string>>>
    /// </summary>
    /// <returns>类型参数字符串（不包括外层 &lt; 和 >）</returns>
    private string ParseGenericTypeArguments()
    {
        var result = "";
        Expect(LangTokenType.LessThan);

        while (CurrentToken.Type != LangTokenType.GreaterThan)
        {
            if (CurrentToken.Type == LangTokenType.EndOfFile)
            {
                throw CreateSyntaxError("意外的文件结束符，期望 '>'");
            }

            if (CurrentToken.Type != LangTokenType.Identifier)
            {
                throw CreateSyntaxError($"期望类型参数名称，但得到 {CurrentToken.Type}");
            }

            result += CurrentToken.Value;
            Expect(LangTokenType.Identifier);

            // 递归处理嵌套泛型
            if (CurrentToken.Type == LangTokenType.LessThan)
            {
                result += "<" + ParseGenericTypeArguments() + ">";
            }

            // 可空类型标记
            if (CurrentToken.Type == LangTokenType.Question)
            {
                result += "?";
                Expect(LangTokenType.Question);
            }

            if (CurrentToken.Type == LangTokenType.Comma)
            {
                result += ", ";
                Expect(LangTokenType.Comma);
                continue;
            }

            break;
        }

        Expect(LangTokenType.GreaterThan);
        return result;
    }

    /// <summary>
    /// 拆分类型参数字符串为独立的类型参数列表
    /// 例如: "string, int" -> ["string", "int"]
    ///       "List<int>, Dict<string, int>" -> ["List<int>", "Dict<string, int>"]
    /// </summary>
    /// <param name="typeArgumentsString">类型参数字符串</param>
    /// <returns>类型参数列表</returns>
    private List<string> SplitTypeArguments(string typeArgumentsString)
    {
        var result = new List<string>();
        var current = "";
        var depth = 0; // 嵌套深度（用于处理嵌套泛型）

        for (int i = 0; i < typeArgumentsString.Length; i++)
        {
            char c = typeArgumentsString[i];

            if (c == '<')
            {
                depth++;
                current += c;
            }
            else if (c == '>')
            {
                depth--;
                current += c;
            }
            else if (c == ',' && depth == 0)
            {
                // 顶层逗号，分隔类型参数
                result.Add(current.Trim());
                current = "";
            }
            else
            {
                current += c;
            }
        }

        // 添加最后一个参数
        if (!string.IsNullOrWhiteSpace(current))
        {
            result.Add(current.Trim());
        }

        return result;
    }

    #endregion
}