using System.Collections.Frozen;
using System.Text;
using Old8Lang.Error;

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
    /// 关键字到 TokenType 的映射表（处理驼峰命名等特殊情况）
    /// </summary>
    private static readonly FrozenDictionary<string, LangTokenType> KeywordToTokenType =
        Enum.GetValues<KeywordType>()
            .ToFrozenDictionary(
                k => k.ToString().ToLower(),
                k => Enum.Parse<LangTokenType>(k.ToString()));

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
    /// <returns>包含所有标记的列表、文件头指令和文档注释的元组</returns>
    /// <exception cref="Error.SyntaxError">当遇到无法识别的字符时抛出</exception>
    /// <remarks>
    /// 该方法执行以下步骤：
    /// 1. 过滤掉源代码中的注释，同时提取文件头指令和文档注释
    /// 2. 逐字符扫描源代码
    /// 3. 识别并生成各种类型的标记
    /// 4. 将文档注释 Token 插入到标记流的合适位置
    /// 5. 返回完整的标记列表、文件头指令和文档注释
    /// </remarks>
    public static (List<LangToken> tokens, List<LangToken> headerDirectives, List<LangToken> docComments)
        TokenizeWithDirectivesAndDocs(string code)
    {
        return TokenizeWithDirectivesAndDocs(code, null);
    }

    /// <summary>
    /// 将Old8Lang源代码转换为标记流（支持预编译符号）
    /// </summary>
    /// <param name="code">要分析的Old8Lang源代码</param>
    /// <param name="preprocessorSymbols">预编译符号管理器（可选，如果为null则不处理预编译指令）</param>
    /// <returns>包含所有标记的列表、文件头指令和文档注释的元组</returns>
    /// <exception cref="Error.SyntaxError">当遇到无法识别的字符时抛出</exception>
    /// <remarks>
    /// 该方法执行以下步骤：
    /// 0. 如果提供了预编译符号管理器，先处理预编译指令（#if, #define等）
    /// 1. 过滤掉源代码中的注释，同时提取文件头指令和文档注释
    /// 2. 逐字符扫描源代码
    /// 3. 识别并生成各种类型的标记
    /// 4. 将文档注释 Token 插入到标记流的合适位置
    /// 5. 返回完整的标记列表、文件头指令和文档注释
    /// </remarks>
    public static (List<LangToken> tokens, List<LangToken> headerDirectives, List<LangToken> docComments)
        TokenizeWithDirectivesAndDocs(string code, PreprocessorSymbols? preprocessorSymbols)
    {
        // 0. 如果提供了预编译符号管理器，先处理预编译指令
        if (preprocessorSymbols is not null)
        {
            var preprocessor = new PreprocessorTokenizer(code, preprocessorSymbols);
            code = preprocessor.Process();
        }

        var tokens = new List<LangToken>();

        // 先过滤掉源代码中的注释，同时提取文件头指令和文档注释
        var filter = new FilteringCommentsTokenizer(code);
        code = filter.FilteringComments();
        var headerDirectives = filter.HeaderDirectives;
        var docComments = filter.DocComments;

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
                column = i + 1;  // 下一行从换行符之后的下一个字符开始
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
                                case 'u':
                                    // 处理Unicode转义序列 \uXXXX
                                    if (EscapeSequenceHelper.TryParseUnicodeEscape(code, i - 1, out var unicodeChar, out var unicodeAdvance))
                                    {
                                        sb.Append(unicodeChar);
                                        i += unicodeAdvance; // 跳过已解析的十六进制数字
                                    }
                                    else
                                    {
                                        // Unicode序列不完整或解析失败，追加原始字符
                                        sb.Append("\\u");
                                    }
                                    break;
                                case 'x':
                                    // 处理十六进制转义序列 \xXX
                                    if (EscapeSequenceHelper.TryParseHexEscape(code, i - 1, out var hexChar, out var hexAdvance))
                                    {
                                        sb.Append(hexChar);
                                        i += hexAdvance; // 跳过已解析的十六进制数字
                                    }
                                    else
                                    {
                                        // 十六进制序列不完整或解析失败，追加原始字符
                                        sb.Append("\\x");
                                    }
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
                            column = i + 1;
                        }

                        sb.Append(code[i]);
                    }

                    i++;
                }

                // 检查字符串是否正确闭合
                if (i >= code.Length || code[i] != '"')
                {
                    throw new SyntaxError(
                        sb.ToString(),
                        line,
                        i - column,
                        "语法错误：未闭合的字符串字面量，缺少结束引号");
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
                            // 处理常见转义字符
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
                                case '\'':
                                    sb.Append('\'');
                                    break;
                                case '"':
                                    sb.Append('"');
                                    break;
                                case '0':
                                    sb.Append('\0');
                                    break;
                                case 'u':
                                    // 处理Unicode转义序列 \uXXXX
                                    if (EscapeSequenceHelper.TryParseUnicodeEscape(code, i, out var unicodeChar, out var unicodeAdvance))
                                    {
                                        sb.Append(unicodeChar);
                                        i += unicodeAdvance; // 跳过已解析的十六进制数字（不包括 \u，因为外层已经在 \ 的位置）
                                    }
                                    else
                                    {
                                        // Unicode序列不完整或解析失败，追加原始字符
                                        sb.Append("\\u");
                                    }

                                    break;
                                case 'x':
                                    // 处理十六进制转义序列 \xXX
                                    if (EscapeSequenceHelper.TryParseHexEscape(code, i, out var hexChar, out var hexAdvance))
                                    {
                                        sb.Append(hexChar);
                                        i += hexAdvance; // 跳过已解析的十六进制数字（不包括 \x，因为外层已经在 \ 的位置）
                                    }
                                    else
                                    {
                                        // 十六进制序列不完整或解析失败，追加原始字符
                                        sb.Append("\\x");
                                    }

                                    break;
                                default:
                                    // 未知的转义字符，追加原始字符
                                    sb.Append('\\');
                                    sb.Append(code[i]);
                                    break;
                            }
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
                            column = i + 1;
                        }

                        sb.Append(code[i]);
                    }

                    i++;
                }

                // 为字符token添加单引号以保持格式一致性
                tokens.Add(new LangToken($"'{sb}'", LangTokenType.Char, line, i - column));
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
                if (i + 1 < code.Length && code[i + 1] == '<')
                {
                    tokens.Add(new LangToken("~<", LangTokenType.WavyLessThan, line, i - column));
                    i++;
                    continue;
                }

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

                if (i + 1 < code.Length && code[i + 1] == '~')
                {
                    if (i + 2 < code.Length && code[i + 2] == '<')
                    {
                        tokens.Add(new LangToken(">~<", LangTokenType.GreaterThanWavyLessThan, line, i - column));
                        i += 2;
                        continue;
                    }

                    tokens.Add(new LangToken(">~", LangTokenType.GreaterThanWavy, line, i - column));
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

            if (code[i] == '@')
            {
                tokens.Add(new LangToken("@", LangTokenType.At, line, i - column));
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
                // 特殊处理 "is not" 关键字组合
                if (matchedKeyword == "is")
                {
                    var nextKeywordStart = i + matchedKeyword.Length;

                    // 跳过空格和制表符
                    while (nextKeywordStart < code.Length &&
                           (code[nextKeywordStart] == ' ' || code[nextKeywordStart] == '\t'))
                    {
                        nextKeywordStart++;
                    }

                    // 检查是否紧跟 "not"
                    var codeSpanAfterIs = code.AsSpan(nextKeywordStart);
                    if (codeSpanAfterIs.Length >= 3 &&
                        codeSpanAfterIs.Slice(0, 3).Equals("not".AsSpan(), StringComparison.Ordinal) &&
                        (nextKeywordStart + 3 == code.Length ||
                         !char.IsLetterOrDigit(code[nextKeywordStart + 3]) &&
                         code[nextKeywordStart + 3] != '_'))
                    {
                        // 识别为 "is not"
                        tokens.Add(new LangToken("is not",
                            LangTokenType.IsNot,
                            line, i - column));
                        i = nextKeywordStart + 2; // 跳过 "not"
                        continue;
                    }
                }

                // 添加关键字标记
                tokens.Add(new LangToken(matchedKeyword,
                    KeywordToTokenType[matchedKeyword],
                    line, i - column));
                i += matchedKeyword.Length - 1;
                continue;
            }

            #endregion

            #region 数字 和 标识符

            if (char.IsDigit(code[i]))
            {
                var startIndex = i; // 记录数字起始位置
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

                tokens.Add(new LangToken(sb.ToString(), LangTokenType.Number, line, startIndex - column));
                continue;
            }

            if (char.IsLetter(code[i]) || code[i] == '_')
            {
                var startIndex = i; // 记录标识符起始位置
                var sb = new StringBuilder(code[i].ToString());
                while (i + 1 < code.Length &&
                       (char.IsLetter(code[i + 1]) || char.IsDigit(code[i + 1]) || code[i + 1] == '_'))
                {
                    sb.Append(code[i + 1]);
                    i++;
                }

                tokens.Add(new LangToken(sb.ToString(), LangTokenType.Identifier, line, startIndex - column));
                continue;
            }

            // 处理无法识别的字符
            throw new SyntaxError(
                code[i].ToString(),
                line,
                i - column,
                $"语法错误：无法识别的字符 '{code[i]}'。建议检查是否输入了无效字符或特殊字符。");

            #endregion
        }

        // 将文档注释插入到 tokens 中适当的位置
        // 文档注释应该出现在函数/类声明之前
        var mergedTokens = MergeDocCommentsWithTokens(tokens, docComments);

        return (mergedTokens, headerDirectives, docComments);
    }

    /// <summary>
    /// 将文档注释合并到 tokens 流中
    /// </summary>
    /// <param name="tokens">原始 token 列表</param>
    /// <param name="docComments">文档注释列表</param>
    /// <returns>合并后的 token 列表</returns>
    private static List<LangToken> MergeDocCommentsWithTokens(List<LangToken> tokens, List<LangToken> docComments)
    {
        if (docComments.Count == 0)
        {
            return tokens;
        }

        var result = new List<LangToken>();
        int tokenIndex = 0;
        int docCommentIndex = 0;

        // 创建一个文档注释组的字典，key 是注释所在行号，value 是该位置的所有注释
        var docCommentGroups = new Dictionary<int, List<LangToken>>();
        foreach (var doc in docComments)
        {
            if (!docCommentGroups.ContainsKey(doc.Line))
            {
                docCommentGroups[doc.Line] = [];
            }

            docCommentGroups[doc.Line].Add(doc);
        }

        // 合并 tokens 和 doc comments
        while (tokenIndex < tokens.Count)
        {
            var token = tokens[tokenIndex];

            // 查找在当前 token 之前的文档注释
            // 文档注释应该出现在它所描述的声明之前
            var relevantDocs = new List<LangToken>();
            for (int line = token.Line - 1; line > 0 && docCommentGroups.ContainsKey(line); line--)
            {
                // 只收集连续的文档注释行
                if (docCommentGroups.TryGetValue(line, out var group))
                {
                    relevantDocs.InsertRange(0, group);
                }
                else
                {
                    break;
                }
            }

            // 添加相关的文档注释
            foreach (var doc in relevantDocs)
            {
                // 避免重复添加
                if (docCommentIndex < docComments.Count && docComments[docCommentIndex].Equals(doc))
                {
                    result.Add(doc);
                    docCommentIndex++;
                }
            }

            // 添加当前 token
            result.Add(token);
            tokenIndex++;
        }

        return result;
    }

    /// <summary>
    /// 将Old8Lang源代码转换为标记流（向后兼容版本）
    /// </summary>
    /// <param name="code">要分析的Old8Lang源代码</param>
    /// <returns>包含所有标记的列表和文件头指令的元组</returns>
    public static (List<LangToken> tokens, List<LangToken> headerDirectives) TokenizeWithDirectives(string code)
    {
        var (tokens, headerDirectives, _) = TokenizeWithDirectivesAndDocs(code, null);
        return (tokens, headerDirectives);
    }

    /// <summary>
    /// 将Old8Lang源代码转换为标记流（向后兼容版本，支持预编译符号）
    /// </summary>
    /// <param name="code">要分析的Old8Lang源代码</param>
    /// <param name="preprocessorSymbols">预编译符号管理器</param>
    /// <returns>包含所有标记的列表和文件头指令的元组</returns>
    public static (List<LangToken> tokens, List<LangToken> headerDirectives) TokenizeWithDirectives(string code, PreprocessorSymbols? preprocessorSymbols)
    {
        var (tokens, headerDirectives, _) = TokenizeWithDirectivesAndDocs(code, preprocessorSymbols);
        return (tokens, headerDirectives);
    }

    /// <summary>
    /// 将Old8Lang源代码转换为标记流（向后兼容版本）
    /// </summary>
    /// <param name="code">要分析的Old8Lang源代码</param>
    /// <returns>包含所有标记的列表</returns>
    public static List<LangToken> Tokenize(string code)
    {
        return TokenizeWithDirectives(code, null).tokens;
    }

    /// <summary>
    /// 将Old8Lang源代码转换为标记流（向后兼容版本，支持预编译符号）
    /// </summary>
    /// <param name="code">要分析的Old8Lang源代码</param>
    /// <param name="preprocessorSymbols">预编译符号管理器</param>
    /// <returns>包含所有标记的列表</returns>
    public static List<LangToken> Tokenize(string code, PreprocessorSymbols? preprocessorSymbols)
    {
        return TokenizeWithDirectives(code, preprocessorSymbols).tokens;
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
/// - 文件头指令 (#!...) - 会被保留用于后续解析
/// 过滤过程中会保留换行符，以确保后续词法分析时行号的准确性。
/// 重要：字符串字面量中的 // 和 /* */ 不会被误认为注释。
/// </remarks>
public struct FilteringCommentsTokenizer(string input)
{
    /// <summary>
    /// 当前扫描索引
    /// </summary>
    private int CurrentIndex = 0;

    /// <summary>
    /// 文件头指令列表
    /// </summary>
    public List<LangToken> HeaderDirectives = [];

    /// <summary>
    /// 文档注释列表（按行号索引，用于后续解析时关联）
    /// </summary>
    public List<LangToken> DocComments = [];

    /// <summary>
    /// 过滤源代码中的注释
    /// </summary>
    /// <returns>过滤掉注释后的源代码</returns>
    public string FilteringComments()
    {
        var result = new StringBuilder();
        bool inDoubleQuoteString = false;
        bool inSingleQuoteString = false;
        bool escapeNext = false;
        int line = 1;
        int lineStartIndex = 0;
        bool isFileHeader = true; // 是否还在文件头部分

        // 扫描整个输入字符串
        while (CurrentIndex < input.Length)
        {
            var currentChar = input[CurrentIndex];

            // 处理换行符 - 更新行号
            if (currentChar == '\n')
            {
                line++;
                lineStartIndex = CurrentIndex + 1;
            }

            // 在文件头部分且不在字符串中，检查文件头指令
            if (isFileHeader && !inDoubleQuoteString && !inSingleQuoteString)
            {
                // 检查是否是文件头指令 (#!)
                if (currentChar == '#' && CurrentIndex + 1 < input.Length && input[CurrentIndex + 1] == '!')
                {
                    Advance(); // 跳过 '#'
                    Advance(); // 跳过 '!'

                    // 读取指令名和值
                    var directiveStart = CurrentIndex;
                    var directiveLineStart = lineStartIndex;

                    // 读取整行
                    while (CurrentIndex < input.Length && input[CurrentIndex] != '\n')
                    {
                        Advance();
                    }

                    var directiveContent = input.Substring(directiveStart, CurrentIndex - directiveStart).Trim();

                    // 解析指令名和值
                    var spaceIndex = directiveContent.IndexOf(' ');
                    if (spaceIndex > 0)
                    {
                        var directiveName = directiveContent.Substring(0, spaceIndex).Trim();
                        var directiveValue = directiveContent.Substring(spaceIndex + 1).Trim();

                        // 创建 token 并添加到列表
                        HeaderDirectives.Add(new LangToken(
                            $"{directiveName}:{directiveValue}",
                            LangTokenType.FileHeaderDirective,
                            line,
                            directiveStart - directiveLineStart
                        ));
                    }

                    // 保留换行符
                    if (CurrentIndex < input.Length && input[CurrentIndex] == '\n')
                    {
                        result.Append('\n');
                        Advance();
                    }

                    continue;
                }

                // 如果遇到非空白、非注释、非文件头指令的内容，则标记文件头结束
                if (currentChar != ' ' && currentChar != '\t' && currentChar != '\n' && currentChar != '\r' &&
                    !(currentChar == '/' && CurrentIndex + 1 < input.Length &&
                      (input[CurrentIndex + 1] == '/' || input[CurrentIndex + 1] == '*')))
                {
                    isFileHeader = false;
                }
            }

            // 处理转义字符
            if (escapeNext)
            {
                result.Append(currentChar);
                Advance();
                escapeNext = false;
                continue;
            }

            // 处理转义字符开始
            if (currentChar == '\\' && (inDoubleQuoteString || inSingleQuoteString))
            {
                result.Append(currentChar);
                Advance();
                escapeNext = true;
                continue;
            }

            // 处理字符串字面量
            if (currentChar == '"' && !inSingleQuoteString)
            {
                inDoubleQuoteString = !inDoubleQuoteString;
                result.Append(currentChar);
                Advance();
                continue;
            }

            if (currentChar == '\'' && !inDoubleQuoteString)
            {
                inSingleQuoteString = !inSingleQuoteString;
                result.Append(currentChar);
                Advance();
                continue;
            }

            // 只有在字符串外部才处理注释
            if (!inDoubleQuoteString && !inSingleQuoteString)
            {
                // 处理文档注释（优先级最高，必须在单行注释之前检查）
                if (currentChar == '/' && CurrentIndex + 2 < input.Length &&
                    input[CurrentIndex + 1] == '/' && input[CurrentIndex + 2] == '/')
                {
                    // 文档注释：///
                    Advance(); // 跳过第一个 '/'
                    Advance(); // 跳过第二个 '/'
                    Advance(); // 跳过第三个 '/'

                    var docCommentStart = CurrentIndex;
                    var docCommentLineStart = lineStartIndex;
                    var sb = new StringBuilder();

                    // 读取文档注释内容直到换行符
                    while (CurrentIndex < input.Length && input[CurrentIndex] != '\n')
                    {
                        sb.Append(input[CurrentIndex]);
                        Advance();
                    }

                    // 创建文档注释 token
                    // 保留前导空格（用于 NumPy Style 的缩进），只去除尾随空格
                    var docCommentContent = sb.ToString().TrimEnd();
                    DocComments.Add(new LangToken(
                        docCommentContent,
                        LangTokenType.DocComment,
                        line,
                        docCommentStart - docCommentLineStart
                    ));

                    // 保留换行符并更新行号
                    if (CurrentIndex < input.Length && input[CurrentIndex] == '\n')
                    {
                        result.Append('\n');
                        line++; // 更新行号
                        lineStartIndex = CurrentIndex + 1; // 更新行起始索引
                        Advance();
                    }

                    continue;
                }
                // 处理单行注释
                else if (currentChar == '/' && CurrentIndex + 1 < input.Length && input[CurrentIndex + 1] == '/')
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

                    continue;
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
                        if (input[CurrentIndex] == '*' && CurrentIndex + 1 < input.Length &&
                            input[CurrentIndex + 1] == '/')
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

                    continue;
                }
            }

            // 处理普通字符（包括字符串内的内容）
            result.Append(currentChar);
            Advance();
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

/// <summary>
/// 转义序列处理辅助类，提供 Unicode 和十六进制转义序列的解析功能
/// </summary>
public static class EscapeSequenceHelper
{
    /// <summary>
    /// 尝试解析 Unicode 转义序列 \uXXXX
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <param name="startIndex">开始位置（\u 的索引）</param>
    /// <param name="result">解析结果字符</param>
    /// <param name="advanceCount">需要跳过的字符数（包括 \u 和 4 位十六进制）</param>
    /// <returns>是否成功解析</returns>
    public static bool TryParseUnicodeEscape(string input, int startIndex, out char result, out int advanceCount)
    {
        result = '\0';
        advanceCount = 0;

        // 检查是否有足够的字符（\u + 4位十六进制，索引从 i 开始是 \，i+1 是 u）
        if (startIndex + 6 > input.Length)
        {
            return false;
        }

        // 提取 4 位十六进制数（从 \u 后面开始）
        var hexStr = input.Substring(startIndex + 2, 4);
        if (int.TryParse(hexStr, System.Globalization.NumberStyles.HexNumber, null, out var unicodeCode))
        {
            result = (char)unicodeCode;
            advanceCount = 4; // 只跳过 4 位十六进制，不包括 \u（因为外层 for 循环已经在 \ 的位置）
            return true;
        }

        return false;
    }

    /// <summary>
    /// 尝试解析十六进制转义序列 \xXX
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <param name="startIndex">开始位置（\x 的索引）</param>
    /// <param name="result">解析结果字符</param>
    /// <param name="advanceCount">需要跳过的字符数（包括 \x 和 2 位十六进制）</param>
    /// <returns>是否成功解析</returns>
    public static bool TryParseHexEscape(string input, int startIndex, out char result, out int advanceCount)
    {
        result = '\0';
        advanceCount = 0;

        // 检查是否有足够的字符（\x + 2位十六进制，索引从 i 开始是 \，i+1 是 x）
        if (startIndex + 4 > input.Length)
        {
            return false;
        }

        // 提取 2 位十六进制数（从 \x 后面开始）
        var hexStr = input.Substring(startIndex + 2, 2);
        if (int.TryParse(hexStr, System.Globalization.NumberStyles.HexNumber, null, out var hexCode))
        {
            result = (char)hexCode;
            advanceCount = 2; // 只跳过 2 位十六进制，不包括 \x（因为外层 for 循环已经在 \ 的位置）
            return true;
        }

        return false;
    }

    /// <summary>
    /// 尝试从已提取的字符串解析 Unicode 转义序列 \uXXXX
    /// </summary>
    /// <param name="content">已提取的转义序列内容（如 "\u4E2D"）</param>
    /// <param name="result">解析结果字符</param>
    /// <returns>是否成功解析</returns>
    public static bool TryParseUnicodeEscapeFromContent(string content, out char result)
    {
        result = '\0';

        if (content.StartsWith("\\u") && content.Length == 6)
        {
            var hexCode = content.Substring(2);
            if (int.TryParse(hexCode, System.Globalization.NumberStyles.HexNumber, null, out var code))
            {
                result = (char)code;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 尝试从已提取的字符串解析十六进制转义序列 \xXX
    /// </summary>
    /// <param name="content">已提取的转义序列内容（如 "\x41"）</param>
    /// <param name="result">解析结果字符</param>
    /// <returns>是否成功解析</returns>
    public static bool TryParseHexEscapeFromContent(string content, out char result)
    {
        result = '\0';

        if (content.StartsWith("\\x") && content.Length >= 3)
        {
            var hexCode = content.Substring(2);
            if (int.TryParse(hexCode, System.Globalization.NumberStyles.HexNumber, null, out var code))
            {
                result = (char)code;
                return true;
            }
        }

        return false;
    }
}