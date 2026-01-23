using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.LangParser.Core;

namespace Old8Lang.LangParser.Parsers;

/// <summary>
/// Primary 表达式解析器 - 实例化和切片
/// </summary>
public partial class PrimaryParser
{
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
}
