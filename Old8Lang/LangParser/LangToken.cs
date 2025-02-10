using System.Text;

namespace Old8Lang.LangParser;

public readonly struct LangToken(string value, LangTokenType type, int line = 0, int column = 0)
{
    public readonly string Value = value;
    public readonly LangTokenType Type = type;
    public readonly int Line = line;
    public readonly int Column = column + 1;

    public override string ToString()
    {
        return $"{Value} {Type} {Line} {Column}";
    }
}

public static class LangTokenizer
{
    public static List<LangToken> Tokenize(string code)
    {
        var tokens = new List<LangToken>();

        // 行 : line
        // 列 : 字符总数 - 累积到上一行的字符数 - \r次数 + 1
        var line = 1;
        var column = 0; // 累积到上一行的字符数 + \r次数
        var lineBreaksCount = 0; // \r次数

        for (var i = 0; i < code.Length; i++)
        {
            bool Func(string a)
            {
                var len = a.Length;
                for (var j = 0; j < len; j++)
                {
                    if (j == len - 1 && i + j <= code.Length && a[j] == code[i + j] && i + j + 1 < code.Length &&
                        code[i + j + 1] == ' ')
                    {
                        tokens.Add(new LangToken(a, Enum.Parse<LangTokenType>(char.ToUpper(a[0]) + a[1..]), line,
                            i - column));
                        i += len - 1;
                        return true;
                    }

                    if (i + j < code.Length && a[j] == code[i + j]) continue;
                    return false;
                }

                return false;
            }

            #region 特殊字符

            if (code[i] == '\r')
            {
                lineBreaksCount++;
                continue;
            }

            if (code[i] == '\n')
            {
                line++;
                column = i + lineBreaksCount;
                continue;
            }

            if (code[i] == ' ' || code[i] == '\t')
            {
                continue;
            }

            #endregion

            #region 符号

            if (code[i] == '+')
            {
                if (i + 1 <= code.Length && code[i + 1] == '+')
                {
                    tokens.Add(new LangToken("++", LangTokenType.PlusPlus, line, i - column));
                    i++;
                    continue;
                }

                tokens.Add(new LangToken("+", LangTokenType.Plus, line, i - column));
                continue;
            }

            if (code[i] == '-')
            {
                if (i + 1 <= code.Length && code[i + 1] == '-')
                {
                    tokens.Add(new LangToken("--", LangTokenType.MinusMinus, line, i - column));
                    i++;
                    continue;
                }

                if (i + 1 <= code.Length && code[i + 1] == '>')
                {
                    tokens.Add(new LangToken("->", LangTokenType.Arrow, line, i - column));
                    i++;
                    continue;
                }

                tokens.Add(new LangToken("-", LangTokenType.Minus, line, i - column));
                continue;
            }

            if (code[i] == '*')
            {
                tokens.Add(new LangToken("*", LangTokenType.Star, line, i - column));
                continue;
            }

            if (code[i] == '/')
            {
                tokens.Add(new LangToken("/", LangTokenType.Slash, line, i - column));
                continue;
            }

            if (code[i] == '%')
            {
                tokens.Add(new LangToken("%", LangTokenType.Percent, line, i - column));
                continue;
            }

            if (code[i] == '^')
            {
                tokens.Add(new LangToken("^", LangTokenType.Caret, line, i - column));
                continue;
            }

            if (code[i] == '|')
            {
                if (i + 1 <= code.Length && code[i + 1] == '|')
                {
                    tokens.Add(new LangToken("||", LangTokenType.Or, line, i - column));
                    i++;
                    continue;
                }

                tokens.Add(new LangToken("|", LangTokenType.Pipe, line, i - column));
                continue;
            }

            if (code[i] == '"')
            {
                var sb = new StringBuilder();
                i++;
                while (i < code.Length && code[i] != '"')
                {
                    sb.Append(code[i]);
                    i++;
                }

                tokens.Add(new LangToken(sb.ToString(), LangTokenType.String, line, i - column));
                continue;
            }

            if (code[i] == '(')
            {
                tokens.Add(new LangToken("(", LangTokenType.LeftParen, line, i - column));
                continue;
            }

            if (code[i] == ')')
            {
                tokens.Add(new LangToken(")", LangTokenType.RightParen, line, i - column));
                continue;
            }

            if (code[i] == '{')
            {
                tokens.Add(new LangToken("{", LangTokenType.LeftBrace, line, i - column));
                continue;
            }

            if (code[i] == '}')
            {
                tokens.Add(new LangToken("}", LangTokenType.RightBrace, line, i - column));
                continue;
            }

            if (code[i] == '[')
            {
                tokens.Add(new LangToken("[", LangTokenType.LeftBracket, line, i - column));
                continue;
            }

            if (code[i] == ']')
            {
                tokens.Add(new LangToken("]", LangTokenType.RightBracket, line, i - column));
                continue;
            }

            if (code[i] == ',')
            {
                tokens.Add(new LangToken(",", LangTokenType.Comma, line, i - column));
                continue;
            }

            if (code[i] == ':')
            {
                tokens.Add(new LangToken(":", LangTokenType.Colon, line, i - column));
                continue;
            }

            if (code[i] == '.')
            {
                if (i + 1 < code.Length && code[i + 1] == '.')
                {
                    tokens.Add(new LangToken("..", LangTokenType.DotDot, line, i - column));
                    i++;
                    continue;
                }

                tokens.Add(new LangToken(".", LangTokenType.Dot, line, i - column));
                continue;
            }

            if (code[i] == '?')
            {
                tokens.Add(new LangToken("?", LangTokenType.Question, line, i - column));
                continue;
            }

            if (code[i] == '=')
            {
                if (i + 1 >= code.Length || code[i + 1] != '=') continue;
                tokens.Add(new LangToken("==", LangTokenType.Equals, line, i - column));
                i++;
                continue;
            }

            if (code[i] == '<')
            {
                if (i + 1 < code.Length && code[i + 1] == '=')
                {
                    tokens.Add(new LangToken("<=", LangTokenType.LessThanEquals, line, i - column));
                    i++;
                    continue;
                }

                if (i + 1 < code.Length && code[i + 1] == '-')
                {
                    tokens.Add(new LangToken("<-", LangTokenType.Assignment, line, i - column));
                    i++;
                    continue;
                }

                tokens.Add(new LangToken("<", LangTokenType.LessThan, line, i - column));
                continue;
            }

            if (code[i] == '>')
            {
                if (i + 1 < code.Length && code[i + 1] == '=')
                {
                    tokens.Add(new LangToken(">=", LangTokenType.GreaterThanEquals, line, i - column));
                    i++;
                    continue;
                }

                tokens.Add(new LangToken(">", LangTokenType.GreaterThan, line, i - column));
                continue;
            }

            if (code[i] == '!')
            {
                if (i + 1 < code.Length && code[i + 1] == '=')
                {
                    tokens.Add(new LangToken("!=", LangTokenType.NotEquals, line, i - column));
                    i++;
                    continue;
                }

                tokens.Add(new LangToken("!", LangTokenType.Exclamation, line, i - column));
                continue;
            }

            if (code[i] == '&')
            {
                if (i + 1 < code.Length && code[i + 1] == '&')
                {
                    tokens.Add(new LangToken("&&", LangTokenType.And, line, i - column));
                    i++;
                    continue;
                }

                tokens.Add(new LangToken("&", LangTokenType.Ampersand, line, i - column));
                continue;
            }

            if (code[i] == '|')
            {
                if (i + 1 < code.Length && code[i + 1] == '|')
                {
                    tokens.Add(new LangToken("||", LangTokenType.Or, line, i - column));
                    i++;
                    continue;
                }

                tokens.Add(new LangToken("|", LangTokenType.Pipe, line, i - column));
                continue;
            }

            if (code[i] == '$')
            {
                tokens.Add(new LangToken("$", LangTokenType.Dollar, line, i - column));
                continue;
            }

            #endregion

            #region 关键词

            var enumList = Enum.GetNames<KeywordType>().Select(x => x.ToLower());
            var enumerable = enumList as string[] ?? enumList.ToArray();
            if (enumerable.Any(x => x[0] == code[i]) && enumerable.Any(Func)) continue;

            #endregion

            #region 数字 和 标识符

            if (char.IsDigit(code[i]))
            {
                var sb = new StringBuilder(code[i].ToString());
                while (i + 1 < code.Length && (char.IsDigit(code[i + 1]) || code[i + 1] == '.'))
                {
                    sb.Append(code[i + 1]);
                    i++;
                }

                tokens.Add(new LangToken(sb.ToString(), LangTokenType.Number, line, i - column));
                continue;
            }

            if (char.IsLetter(code[i]) || code[i] == '_')
            {
                var sb = new StringBuilder(code[i].ToString());
                while (i + 1 < code.Length &&
                       (char.IsLetter(code[i + 1]) || char.IsDigit(code[i + 1]) || code[i + 1] == '_'))
                {
                    sb.Append(code[i + 1]);
                    i++;
                }

                tokens.Add(new LangToken(sb.ToString(), LangTokenType.Identifier, line, i - column));
                continue;
            }

            #endregion
        }

        return tokens;
    }
}