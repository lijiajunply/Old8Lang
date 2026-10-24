using Old8Lang.LangParser;
using Old8Lang.LanguageServer.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Old8Lang.LanguageServer.Services;

/// <summary>
/// 选择范围构建器 - 基于 token 建立“token → 括号表达式 → 语句 → 声明块 → 文档”的父级链。
///
/// 解析器的 AST 目前只提供起始位置，缺少可靠的结束位置，因此这里仍采用 token-based 方案：
/// 位置精确到 token，语句和声明块的范围由括号配对与换行/运算符续行推断。
/// </summary>
public static class SelectionRangeBuilder
{
    /// <summary>为每个请求位置构建选择范围链</summary>
    public static Container<SelectionRange> Build(DocumentParseResult document, IEnumerable<Position> positions)
    {
        var tokens = document.Tokens ?? [];
        var bracketPairs = BuildBracketPairs(tokens);
        var statements = BuildStatementSpans(tokens);
        var documentEnd = GetDocumentEnd(document.Text);
        var documentSpan = new Span(0, 0, documentEnd.Line, documentEnd.Character);

        var result = new List<SelectionRange>();

        foreach (var position in positions)
        {
            var line = (int)position.Line;
            var character = (int)position.Character;

            var candidates = new List<Span>();

            var token = SymbolFinder.FindTokenAtPosition(tokens, line, character, document.Text);
            Span? tokenSpan = token != null ? TokenSpan(token.Value) : null;

            candidates.AddRange(bracketPairs.Where(span => span.ContainsPosition(line, character)));
            candidates.AddRange(statements.Where(span => span.ContainsPosition(line, character)));

            var lineSpan = GetLineSpan(tokens, document.Text, line);
            if (lineSpan != null)
            {
                candidates.Add(lineSpan.Value);
            }

            candidates.AddRange(BuildDeclarationSpans(tokens, bracketPairs, line, character));
            candidates.Add(documentSpan);

            result.Add(BuildChain(line, character, tokenSpan, candidates));
        }

        return new Container<SelectionRange>(result);
    }

    private static SelectionRange BuildChain(int line, int character, Span? tokenSpan, List<Span> candidates)
    {
        var distinct = candidates
            .Where(span => span.StartLine < span.EndLine ||
                           (span.StartLine == span.EndLine && span.StartChar < span.EndChar))
            .Distinct()
            .ToList();

        // 有 token 时以 token 为最内层；落在空白处时退化为零宽范围
        var current = tokenSpan ?? new Span(line, character, line, character);

        var chain = new List<Span> { current };
        var currentSpan = current;

        while (true)
        {
            var next = distinct
                .Where(span => span.StrictlyContains(currentSpan) && !chain.Contains(span))
                .OrderBy(span => span.Area())
                .FirstOrDefault();

            if (next == default)
            {
                break;
            }

            chain.Add(next);
            currentSpan = next;
        }

        // Parent 是 init-only，从最外层向最内层构造
        SelectionRange? node = null;
        for (var i = chain.Count - 1; i >= 0; i--)
        {
            node = new SelectionRange
            {
                Range = ToRange(chain[i]),
                Parent = node!
            };
        }

        return node!;
    }

    private static OmniSharp.Extensions.LanguageServer.Protocol.Models.Range ToRange(Span span)
    {
        return new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range
        {
            Start = new Position(span.StartLine, span.StartChar),
            End = new Position(span.EndLine, span.EndChar)
        };
    }

    private static Span TokenSpan(LangToken token)
    {
        var line = token.Line - 1;
        return new Span(line, token.Column, line, token.Column + token.Value.Length);
    }

    private static Span? GetLineSpan(List<LangToken> tokens, string text, int line)
    {
        var lineTokens = tokens.Where(token => token.Line - 1 == line).ToList();

        if (lineTokens.Count == 0)
        {
            return null;
        }

        var start = lineTokens[0].Column;
        var last = lineTokens[^1];
        var end = last.Column + last.Value.Length;
        return new Span(line, start, line, end);
    }

    /// <summary>构建括号配对范围（圆括号、方括号、花括号）</summary>
    private static List<Span> BuildBracketPairs(List<LangToken> tokens)
    {
        var spans = new List<Span>();
        var stack = new Stack<(LangTokenType Type, int Index)>();

        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];

            if (TryGetMatchingOpen(token.Type, out _))
            {
                stack.Push((token.Type, i));
                continue;
            }

            if (!TryGetMatchingClose(token.Type, out var expectedOpen))
            {
                continue;
            }

            // 找到最近的同类左括号（容忍不配对的 token）
            if (stack.Count == 0 || stack.Peek().Type != expectedOpen)
            {
                continue;
            }

            var (_, openIndex) = stack.Pop();
            var open = tokens[openIndex];
            spans.Add(new Span(
                open.Line - 1,
                open.Column,
                token.Line - 1,
                token.Column + token.Value.Length));
        }

        return spans;
    }

    /// <summary>
    /// 为包含光标的花括号块补充“声明头 + 块”范围，例如 <c>func f() { ... }</c>。
    /// </summary>
    private static IEnumerable<Span> BuildDeclarationSpans(
        List<LangToken> tokens,
        List<Span> bracketPairs,
        int line,
        int character)
    {
        foreach (var pair in bracketPairs)
        {
            if (!pair.ContainsPosition(line, character))
            {
                continue;
            }

            var openIndex = FindTokenIndex(tokens, pair.StartLine, pair.StartChar);
            if (openIndex < 0 || tokens[openIndex].Type != LangTokenType.LeftBrace)
            {
                continue;
            }

            var keywordIndex = FindDeclarationKeywordIndex(tokens, openIndex);
            if (keywordIndex < 0)
            {
                continue;
            }

            var keyword = tokens[keywordIndex];
            yield return new Span(
                keyword.Line - 1,
                keyword.Column,
                pair.EndLine,
                pair.EndChar);
        }
    }

    private static int FindTokenIndex(List<LangToken> tokens, int line, int column)
    {
        for (var i = 0; i < tokens.Count; i++)
        {
            if (tokens[i].Line - 1 == line && tokens[i].Column == column)
            {
                return i;
            }
        }

        return -1;
    }

    private static int FindDeclarationKeywordIndex(List<LangToken> tokens, int openBraceIndex)
    {
        // 向前回溯，跳过参数列表/返回类型，寻找最近的声明关键字
        for (var i = openBraceIndex - 1; i >= 0 && i >= openBraceIndex - 200; i--)
        {
            var type = tokens[i].Type;

            if (type is LangTokenType.LeftBrace or LangTokenType.RightBrace or LangTokenType.Semicolon)
            {
                return -1;
            }

            if (type is LangTokenType.Func or LangTokenType.Async or LangTokenType.Class
                or LangTokenType.If or LangTokenType.Elif or LangTokenType.Else
                or LangTokenType.For or LangTokenType.While or LangTokenType.Switch
                or LangTokenType.Try or LangTokenType.Catch or LangTokenType.Finally
                or LangTokenType.Using)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// 按换行切分语句。括号未闭合，或上一行以运算符结尾、下一行以运算符/else/catch 开头时视为续行。
    /// </summary>
    private static List<Span> BuildStatementSpans(List<LangToken> tokens)
    {
        var spans = new List<Span>();
        var index = 0;

        while (index < tokens.Count)
        {
            var start = index;
            var parenDepth = 0;
            var bracketDepth = 0;

            while (index < tokens.Count)
            {
                var token = tokens[index];

                switch (token.Type)
                {
                    case LangTokenType.LeftParen:
                        parenDepth++;
                        break;
                    case LangTokenType.RightParen:
                        parenDepth = Math.Max(0, parenDepth - 1);
                        break;
                    case LangTokenType.LeftBracket:
                        bracketDepth++;
                        break;
                    case LangTokenType.RightBracket:
                        bracketDepth = Math.Max(0, bracketDepth - 1);
                        break;
                }

                index++;

                var atTopLevel = parenDepth == 0 && bracketDepth == 0;

                if (atTopLevel && token.Type is LangTokenType.LeftBrace or LangTokenType.RightBrace)
                {
                    break;
                }

                if (atTopLevel && index < tokens.Count)
                {
                    var next = tokens[index];
                    if (next.Line > token.Line)
                    {
                        if (ContinuesAfter(token.Type) || ContinuesBefore(next.Type))
                        {
                            continue;
                        }

                        break;
                    }
                }
            }

            spans.Add(SpanOfTokens(tokens, start, Math.Max(start, index - 1)));
        }

        return spans;
    }

    private static Span SpanOfTokens(List<LangToken> tokens, int start, int end)
    {
        var first = tokens[start];
        var last = tokens[end];
        return new Span(
            first.Line - 1,
            first.Column,
            last.Line - 1,
            last.Column + last.Value.Length);
    }

    private static bool ContinuesAfter(LangTokenType type)
    {
        return type is LangTokenType.Plus or LangTokenType.Minus or LangTokenType.Star
            or LangTokenType.Slash or LangTokenType.Percent or LangTokenType.Caret
            or LangTokenType.Ampersand or LangTokenType.Pipe or LangTokenType.Comma
            or LangTokenType.Dot or LangTokenType.Colon or LangTokenType.Question
            or LangTokenType.Assignment or LangTokenType.Equals or LangTokenType.LessThan
            or LangTokenType.GreaterThan or LangTokenType.LessThanEquals
            or LangTokenType.GreaterThanEquals or LangTokenType.NotEquals
            or LangTokenType.And or LangTokenType.Or or LangTokenType.NullishCoalescing
            or LangTokenType.Arrow;
    }

    private static bool ContinuesBefore(LangTokenType type)
    {
        return type is LangTokenType.Else or LangTokenType.Elif or LangTokenType.Catch
            or LangTokenType.Finally or LangTokenType.Dot or LangTokenType.And
            or LangTokenType.Or;
    }

    private static bool TryGetMatchingOpen(LangTokenType type, out LangTokenType close)
    {
        close = type switch
        {
            LangTokenType.LeftParen => LangTokenType.RightParen,
            LangTokenType.LeftBracket => LangTokenType.RightBracket,
            LangTokenType.LeftBrace => LangTokenType.RightBrace,
            _ => LangTokenType.EndOfFile
        };

        return type is LangTokenType.LeftParen or LangTokenType.LeftBracket or LangTokenType.LeftBrace;
    }

    private static bool TryGetMatchingClose(LangTokenType type, out LangTokenType open)
    {
        open = type switch
        {
            LangTokenType.RightParen => LangTokenType.LeftParen,
            LangTokenType.RightBracket => LangTokenType.LeftBracket,
            LangTokenType.RightBrace => LangTokenType.LeftBrace,
            _ => LangTokenType.EndOfFile
        };

        return type is LangTokenType.RightParen or LangTokenType.RightBracket or LangTokenType.RightBrace;
    }

    private static (int Line, int Character) GetDocumentEnd(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return (0, 0);
        }

        var lines = text.Replace("\r\n", "\n").Split('\n');
        return (lines.Length - 1, lines[^1].Length);
    }

    /// <summary>内部范围表示（LSP 行列号，均从 0 开始）</summary>
    private readonly record struct Span(int StartLine, int StartChar, int EndLine, int EndChar)
    {
        public long Area() => (long)(EndLine - StartLine) * 100_000 + (EndChar - StartChar);

        public bool ContainsPosition(int line, int character)
        {
            if (line < StartLine || line > EndLine)
            {
                return false;
            }

            if (line == StartLine && character < StartChar)
            {
                return false;
            }

            if (line == EndLine && character > EndChar)
            {
                return false;
            }

            return true;
        }

        public bool StrictlyContains(Span other)
        {
            if (this == other)
            {
                return false;
            }

            var startsBefore = StartLine < other.StartLine ||
                              (StartLine == other.StartLine && StartChar <= other.StartChar);
            var endsAfter = EndLine > other.EndLine ||
                            (EndLine == other.EndLine && EndChar >= other.EndChar);
            return startsBefore && endsAfter;
        }
    }
}
