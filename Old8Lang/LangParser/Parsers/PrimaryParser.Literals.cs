using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.LangParser.Core;

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
}
