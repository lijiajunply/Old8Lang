using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Runtime;
using Old8Lang.Syntax;

namespace Old8Lang.LangParser.Parsers;

/// <summary>
/// Primary 表达式解析器 - 字面量解析
/// </summary>
public partial class PrimaryParser
{
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
        return (StringLangValue)ScalarLiteralRuntimeValueFactory.Create(LiteralSyntax.String(value, position));
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

        return (CharLangValue)ScalarLiteralRuntimeValueFactory.Create(LiteralSyntax.Char(value, position));
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

        if (LiteralSyntax.TryParseInteger(value, position, out var literal))
            return ScalarLiteralRuntimeValueFactory.Create(literal);

        throw CreateSyntaxError($"无法解析数字字面量 '{value}'");
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
        if (LiteralSyntax.TryParseDouble(value, position, out var literal))
            return (DoubleLangValue)ScalarLiteralRuntimeValueFactory.Create(literal);

        throw CreateSyntaxError("无法解析双精度字面量");
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
        return (BoolLangValue)ScalarLiteralRuntimeValueFactory.Create(LiteralSyntax.Bool(value, position));
    }


    public NullLangValue ParseNullLiteral()
    {
        var nullToken = CurrentToken;
        var position = CreateSourcePosition(nullToken);
        Expect(LangTokenType.Null);
        return (NullLangValue)ScalarLiteralRuntimeValueFactory.Create(LiteralSyntax.Null(position));
    }
}
