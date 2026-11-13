using System.Globalization;
using System.Diagnostics.CodeAnalysis;
using Old8Lang.LangParser;

namespace Old8Lang.Syntax;

/// <summary>
/// 解析阶段产出的标量字面量。它只保存源码值与位置，不承载运算、模块或对象行为。
/// </summary>
public sealed record LiteralSyntax(LiteralKind Kind, object? LiteralValue, SourcePosition Position) : ILiteralSyntax
{
    public static LiteralSyntax String(string value, SourcePosition position) => new(LiteralKind.String, value, position);

    public static LiteralSyntax Char(string value, SourcePosition position) => new(LiteralKind.Char, ParseChar(value), position);

    public static LiteralSyntax Bool(bool value, SourcePosition position) => new(LiteralKind.Bool, value, position);

    public static LiteralSyntax Null(SourcePosition position) => new(LiteralKind.Null, null, position);

    public static LiteralSyntax Void(SourcePosition position) => new(LiteralKind.Void, null, position);

    public static bool TryParseInteger(string value, SourcePosition position, [NotNullWhen(true)] out LiteralSyntax? literal)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.CurrentCulture, out var intValue))
        {
            literal = new LiteralSyntax(LiteralKind.Int, intValue, position);
            return true;
        }

        // Old8Lang 当前没有 long 运行时值；保持原行为，超出 int 但可表达的整数转为 double。
        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.CurrentCulture, out var longValue))
        {
            literal = new LiteralSyntax(LiteralKind.Double, (double)longValue, position);
            return true;
        }

        if (double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture,
                out var doubleValue))
        {
            literal = new LiteralSyntax(LiteralKind.Double, doubleValue, position);
            return true;
        }

        literal = null;
        return false;
    }

    public static bool TryParseDouble(string value, SourcePosition position, [NotNullWhen(true)] out LiteralSyntax? literal)
    {
        if (double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture,
                out var doubleValue))
        {
            literal = new LiteralSyntax(LiteralKind.Double, doubleValue, position);
            return true;
        }

        literal = null;
        return false;
    }

    private static char ParseChar(string value)
    {
        if (value.StartsWith('\'') && value.EndsWith('\'') && value.Length >= 3)
        {
            var content = value.Substring(1, value.Length - 2);
            if (content.StartsWith('\\'))
            {
                return content switch
                {
                    "\\n" => '\n',
                    "\\t" => '\t',
                    "\\r" => '\r',
                    "\\" => '\\',
                    "\\'" => '\'',
                    "\\\"" => '\"',
                    "\\0" => '\0',
                    _ when EscapeSequenceHelper.TryParseUnicodeEscapeFromContent(content, out var unicodeChar) => unicodeChar,
                    _ when EscapeSequenceHelper.TryParseHexEscapeFromContent(content, out var hexChar) => hexChar,
                    _ => content[0]
                };
            }

            return content[0];
        }

        return char.TryParse(value, out var parsed) ? parsed : value.FirstOrDefault();
    }
}
