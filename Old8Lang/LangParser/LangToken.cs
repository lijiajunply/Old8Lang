using System.Collections.Frozen;
using System.Text;

namespace Old8Lang.LangParser;

/// <summary>
/// 表示Old8Lang语言的词法标记
/// </summary>
/// <param name="value">标记的字符串值</param>
/// <param name="type">标记的类型</param>
/// <param name="line">标记在源代码中的行号</param>
/// <param name="column">标记在源代码中的列号</param>
/// <remarks>
/// 该结构体用于存储词法分析过程中生成的标记信息，包括标记的值、类型、行号和列号。
/// 这些信息对于后续的语法分析和错误报告非常重要。
/// </remarks>
public readonly struct LangToken(string value, LangTokenType type, int line = 0, int column = 0)
{
    /// <summary>
    /// 标记的字符串值
    /// </summary>
    public readonly string Value = value;
    
    /// <summary>
    /// 标记的类型
    /// </summary>
    public readonly LangTokenType Type = type;
    
    /// <summary>
    /// 标记在源代码中的行号（从1开始）
    /// </summary>
    public readonly int Line = line;
    
    /// <summary>
    /// 标记在源代码中的列号（从1开始）
    /// </summary>
    public readonly int Column = column + 1;

    /// <summary>
    /// 将标记转换为字符串表示
    /// </summary>
    /// <returns>包含标记值、类型、行号和列号的字符串</returns>
    public override string ToString()
    {
        return $"{Value} {Type} {Line} {Column}";
    }
}

/// <summary>
/// Old8Lang语言的词法分析器，负责将源代码转换为标记流
/// </summary>
/// <remarks>
/// 该类实现了完整的词法分析功能，包括：
/// - 识别关键字、标识符、字面量（数字、字符串、字符）
/// - 处理运算符和分隔符
/// - 支持科学计数法
/// - 处理注释过滤
/// - 优化的关键字查找算法
/// </remarks>
public static class LangTokenizer
{
    /// <summary>
    /// 静态缓存关键字集合，避免每次Tokenize时重新创建，提高性能
    /// </summary>
    private static readonly FrozenSet<string> KeywordSet = 
        Enum.GetNames<KeywordType>()
            .Select(x => x.ToLower())
            .ToFrozenSet();

    /// <summary>
    /// 按首字母索引的关键字列表，按长度降序排列以优先匹配最长关键字
    /// </summary>
    private static readonly Dictionary<char, List<string>> KeywordsByFirstChar = 
        KeywordSet
            .GroupBy(k => k[0])
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Length).ToList());

    /// <summary>
    /// 将Old8Lang源代码转换为标记流
    /// </summary>
    /// <param name="code">要分析的Old8Lang源代码</param>
    /// <returns>包含所有标记的列表</returns>
    /// <exception cref="Error.SyntaxError">当遇到无法识别的字符时抛出</exception>
    /// <remarks>
    /// 该方法执行以下步骤：
    /// 1. 过滤掉源代码中的注释
    /// 2. 逐字符扫描源代码
    /// 3. 识别并生成各种类型的标记
    /// 4. 返回完整的标记列表
    /// </remarks>
    public static List<LangToken> Tokenize(string code)
    {
        var tokens = new List<LangToken>();

        // 先过滤掉源代码中的注释
        code = new FilteringCommentsTokenizer(code).FilteringComments();

        // 初始化行号和列号信息
        var line = 1;
        var column = 0; // 累积到上一行的字符数

        // 逐字符扫描源代码
        for (var i = 0; i < code.Length; i++)
        {
            #region 特殊字符处理

            // 处理回车符，跳过并继续
            if (code[i] == '\r')
            {
                i++;
            }

            // 处理换行符，更新行号和列累积值
            if (code[i] == '\n')
            {
                line++;
                column = i;
                continue;
            }

            // 跳过空格和制表符
            if (code[i] == ' ' || code[i] == '\t')
            {
                continue;
            }

            #endregion

            #region 符号

            if (code[i] == '+')
            {
                if (i + 1 < code.Length && code[i + 1] == '+')
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
                if (i + 1 < code.Length && code[i + 1] == '-')
                {
                    tokens.Add(new LangToken("--", LangTokenType.MinusMinus, line, i - column));
                    i++;
                    continue;
                }

                if (i + 1 < code.Length && code[i + 1] == '>')
                {
                    tokens.Add(new LangToken("->", LangTokenType.Arrow, line, i - column));
                    i++;
                    continue;
                }

                // 检查是否是负数字面量
                // 如果前一个token是运算符、赋值、左括号、逗号等，并且后面跟着数字，则视为负数
                if (i + 1 < code.Length && char.IsDigit(code[i + 1]) &&
                    (tokens.Count == 0 ||
                     tokens[^1].Type is LangTokenType.Assignment or LangTokenType.LeftParen or
                                        LangTokenType.Comma or LangTokenType.LeftBracket or
                                        LangTokenType.Plus or LangTokenType.Minus or
                                        LangTokenType.Star or LangTokenType.Slash or
                                        LangTokenType.Percent or LangTokenType.Caret or
                                        LangTokenType.GreaterThan or LangTokenType.LessThan or
                                        LangTokenType.Equals or LangTokenType.NotEquals or
                                        LangTokenType.GreaterThanEquals or LangTokenType.LessThanEquals or
                                        LangTokenType.And or LangTokenType.Or or LangTokenType.Xor or
                                        LangTokenType.Return or LangTokenType.Colon))
                {
                    // 解析负数
                    var sb = new StringBuilder("-");
                    i++;

                    while (i < code.Length && (char.IsDigit(code[i]) || code[i] == '.'))
                    {
                        sb.Append(code[i]);
                        i++;
                    }

                    // 处理科学计数法
                    if (i < code.Length && char.ToLower(code[i]) == 'e')
                    {
                        sb.Append(code[i]);
                        i++;

                        // 处理指数符号 (+/-)
                        if (i < code.Length && (code[i] == '+' || code[i] == '-'))
                        {
                            sb.Append(code[i]);
                            i++;
                        }

                        // 处理指数数字
                        while (i < code.Length && char.IsDigit(code[i]))
                        {
                            sb.Append(code[i]);
                            i++;
                        }
                    }

                    tokens.Add(new LangToken(sb.ToString(), LangTokenType.Number, line, i - 1 - column));
                    i--; // 回退一位，因为外层循环会 i++
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
                if (i + 1 < code.Length && code[i + 1] == '|')
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
                while (i < code.Length)
                {
                    if (code[i] == '\\') // 处理转义字符
                    {
                        if (i + 1 < code.Length)
                        {
                            i++;
                            // 处理常见转义序列
                            switch (code[i])
                            {
                                case 'n':
                                    sb.Append('\n');
                                    break;
                                case 't':
                                    sb.Append('\t');
                                    break;
                                case 'r':
                                    sb.Append('\r');
                                    break;
                                case '\\':
                                    sb.Append('\\');
                                    break;
                                case '"':
                                    sb.Append('"');
                                    break;
                                default:
                                    sb.Append(code[i]);
                                    break;
                            }
                        }
                    }
                    else if (code[i] == '"') // 遇到未转义的双引号，结束字符串
                    {
                        break;
                    }
                    else
                    {
                        if (code[i] == '\n')
                        {
                            line++;
                            column = i;
                        }

                        sb.Append(code[i]);
                    }

                    i++;
                }

                tokens.Add(new LangToken(sb.ToString(), LangTokenType.String, line, i - column));
                continue;
            }

            // 处理字符字面量 'c'
            if (code[i] == '\'')
            {
                var sb = new StringBuilder();
                i++;
                while (i < code.Length)
                {
                    if (code[i] == '\\') // 处理转义字符
                    {
                        if (i + 1 < code.Length)
                        {
                            i++;
                            sb.Append(code[i]);
                        }
                    }
                    else if (code[i] == '\'') // 遇到未转义的单引号，结束字符
                    {
                        break;
                    }
                    else
                    {
                        if (code[i] == '\n')
                        {
                            line++;
                            column = i;
                        }

                        sb.Append(code[i]);
                    }

                    i++;
                }

                tokens.Add(new LangToken(sb.ToString(), LangTokenType.Char, line, i - column));
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

            if (code[i] == ';')
            {
                tokens.Add(new LangToken(";", LangTokenType.Semicolon, line, i - column));
                continue;
            }

            if (code[i] == ':')
            {
                tokens.Add(new LangToken(":", LangTokenType.Colon, line, i - column));
                continue;
            }

            if (code[i] == '.')
            {
                tokens.Add(new LangToken(".", LangTokenType.Dot, line, i - column));
                continue;
            }

            if (code[i] == '~')
            {
                tokens.Add(new LangToken("~", LangTokenType.Wavy, line, i - column));
                continue;
            }

            if (code[i] == '?')
            {
                if (i + 1 < code.Length && code[i + 1] == '?')
                {
                    tokens.Add(new LangToken("??", LangTokenType.NullishCoalescing, line, i - column));
                    i++;
                    continue;
                }
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

            // 使用首字母索引和 AsSpan() 优化关键字识别，避免字符串分配
            string? matchedKeyword = null;

            // 只查找以当前字符开头的关键字
            if (KeywordsByFirstChar.TryGetValue(code[i], out var candidates))
            {
                var codeSpan = code.AsSpan(i);

                // 按长度降序排列，优先匹配最长的关键字
                foreach (var keyword in candidates)
                {
                    if (keyword.Length <= codeSpan.Length &&
                        codeSpan.Slice(0, keyword.Length).Equals(keyword.AsSpan(), StringComparison.Ordinal) &&
                        (i + keyword.Length == code.Length ||
                         !char.IsLetterOrDigit(code[i + keyword.Length]) &&
                         code[i + keyword.Length] != '_'))
                    {
                        matchedKeyword = keyword;
                        break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(matchedKeyword))
            {
                // 添加关键字标记
                tokens.Add(new LangToken(matchedKeyword,
                    Enum.Parse<LangTokenType>(char.ToUpper(matchedKeyword[0]) + matchedKeyword[1..]),
                    line, i - column));
                i += matchedKeyword.Length - 1;
                continue;
            }

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

                // 处理科学计数法 (e.g., 1.23e3, 1.23E-4, 1e10)
                if (i + 1 < code.Length && char.ToLower(code[i + 1]) == 'e')
                {
                    sb.Append(code[i + 1]);
                    i++;

                    // 处理指数符号 (+/-)
                    if (i + 1 < code.Length && (code[i + 1] == '+' || code[i + 1] == '-'))
                    {
                        sb.Append(code[i + 1]);
                        i++;
                    }

                    // 处理指数数字
                    while (i + 1 < code.Length && char.IsDigit(code[i + 1]))
                    {
                        sb.Append(code[i + 1]);
                        i++;
                    }
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

            // 处理无法识别的字符
            throw new Error.SyntaxError(
                code[i].ToString(),
                line,
                i - column,
                $"语法错误：无法识别的字符 '{code[i]}'。建议检查是否输入了无效字符或特殊字符。");

            #endregion
        }

        return tokens;
    }
}

/// <summary>
/// 注释过滤词法分析器，负责从源代码中移除注释，但保留换行符
/// </summary>
/// <param name="input">原始源代码</param>
/// <remarks>
/// 该结构体用于在词法分析之前过滤掉源代码中的注释，支持：
/// - 单行注释 (// ...)
/// - 多行注释 (/* ... */)
/// 过滤过程中会保留换行符，以确保后续词法分析时行号的准确性。
/// </remarks>
public struct FilteringCommentsTokenizer(string input)
{
    /// <summary>
    /// 当前扫描索引
    /// </summary>
    private int CurrentIndex = 0;

    /// <summary>
    /// 过滤源代码中的注释
    /// </summary>
    /// <returns>过滤掉注释后的源代码</returns>
    public string FilteringComments()
    {
        var result = new StringBuilder();

        // 扫描整个输入字符串
        while (CurrentIndex < input.Length)
        {
            var currentChar = input[CurrentIndex];

            // 处理单行注释
            if (currentChar == '/' && CurrentIndex + 1 < input.Length && input[CurrentIndex + 1] == '/')
            {
                // 跳过单行注释，但保留换行符
                Advance(); // 跳过 '/'  
                Advance(); // 跳过 '/'  

                // 跳过注释内容直到换行符
                while (CurrentIndex < input.Length && input[CurrentIndex] != '\n')
                {
                    Advance();
                }

                // 保留换行符
                if (CurrentIndex < input.Length && input[CurrentIndex] == '\n')
                {
                    result.Append('\n');
                    Advance();
                }
            }
            // 处理多行注释
            else if (currentChar == '/' && CurrentIndex + 1 < input.Length && input[CurrentIndex + 1] == '*')
            {
                // 跳过多行注释，但保留其中的换行符
                Advance(); // 跳过 '/'  
                Advance(); // 跳过 '*'  

                // 跳过注释内容直到结束标记
                while (CurrentIndex < input.Length)
                {
                    // 检查是否到达注释结束标记 */
                    if (input[CurrentIndex] == '*' && CurrentIndex + 1 < input.Length && input[CurrentIndex + 1] == '/')
                    {
                        Advance(); // 跳过 '*'  
                        Advance(); // 跳过 '/'  
                        break;
                    }

                    // 保留多行注释中的换行符
                    if (input[CurrentIndex] == '\n')
                    {
                        result.Append('\n');
                    }

                    Advance();
                }
            }
            // 处理普通字符
            else
            {
                result.Append(currentChar);
                Advance();
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// 前进到下一个字符
    /// </summary>
    private void Advance()
    {
        CurrentIndex++;
    }

    // 以下方法暂时注释掉，如需使用可取消注释
    // private char Peek()
    // {
    //     return CurrentIndex + 1 >= input.Length ? '\0' : input[CurrentIndex + 1];
    // }
}