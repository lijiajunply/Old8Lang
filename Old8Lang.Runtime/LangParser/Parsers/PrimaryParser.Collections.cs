using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.LangParser.Parsers;

/// <summary>
/// Primary 表达式解析器 - 集合类型解析
/// </summary>
public partial class PrimaryParser
{
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
}
