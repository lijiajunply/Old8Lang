using Old8Lang.LangParser;
using Old8Lang.LanguageServer.Models;
using Old8Lang.LanguageServer.Services;

namespace Old8Lang.Tests.LanguageServer.Services;

/// <summary>
/// TokenLineIndex 按行索引单元测试：验证 O(1) 行定位、行内二分查找、边界位置、
/// 行范围切片及与线性扫描结果的完全一致性。
/// </summary>
public class TokenLineIndexTests
{
    [Fact]
    public void FindTokenAtPosition_MatchesLinearScanForAllTokens()
    {
        var code =
            "class Calculator {\n" +
            "    public value: int <- 0\n" +
            "    public func add(a: int, b: int) -> int {\n" +
            "        sum <- a + b\n" +
            "        this.value <- sum\n" +
            "        return sum\n" +
            "    }\n" +
            "}\n" +
            "calc <- Calculator()\n" +
            "res <- calc.add(10, 20)\n";

        var tokens = LangTokenizer.Tokenize(code);
        var index = new TokenLineIndex(tokens);

        Assert.True(index.IsMonotonic);
        Assert.Equal(tokens.Count, index.Count);

        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            if (token.Value.Length == 0 || token.Line < 1)
            {
                continue;
            }

            var lspLine = token.Line - 1;
            var startMatch = index.FindTokenAtPosition(lspLine, token.Column);
            var endCol = token.Column + token.Value.Length - 1;
            var endMatch = index.FindTokenAtPosition(lspLine, endCol);

            Assert.NotNull(startMatch);
            Assert.NotNull(endMatch);
            Assert.Equal(token.Value, startMatch.Value.Token.Value);
            Assert.Equal(i, startMatch.Value.Index);
            Assert.Equal(i, endMatch.Value.Index);

            var linear = SymbolFinder.FindTokenAtPosition(tokens, lspLine, token.Column, code);
            Assert.NotNull(linear);
            Assert.Equal(linear.Value.Value, startMatch.Value.Token.Value);
        }
    }

    [Fact]
    public void FindLastTokenAtOrBefore_HandlesEmptyLinesAndBounds()
    {
        var code = "a <- 1\n\n\nb <- a + 2\n";
        var tokens = LangTokenizer.Tokenize(code);
        var index = new TokenLineIndex(tokens);

        // 空行（LSP line 1 和 2）应回溯到第 0 行末尾的 `1`
        var idxOnEmptyLine = index.FindLastTokenAtOrBefore(2, 0);
        Assert.True(idxOnEmptyLine >= 0);
        Assert.Equal("1", tokens[idxOnEmptyLine].Value);

        // 超出文档末行应返回最后一个有效 token
        var idxAfterEnd = index.FindLastTokenAtOrBefore(999, 0);
        Assert.Equal(tokens.Count - 1, idxAfterEnd);

        // 负行号返回 -1
        Assert.Equal(-1, index.FindLastTokenAtOrBefore(-1, 0));
    }

    [Fact]
    public void GetTokenRangeForLines_ReturnsExactSlice()
    {
        var code = "x <- 1\ny <- 2\nz <- 3\nw <- 4\n";
        var tokens = LangTokenizer.Tokenize(code);
        var index = new TokenLineIndex(tokens);

        var (start, endExclusive) = index.GetTokenRangeForLines(1, 2);
        Assert.True(start < endExclusive);

        var sliceTokens = tokens.Skip(start).Take(endExclusive - start).ToList();
        Assert.All(sliceTokens, t => Assert.InRange(t.Line, 2, 3));
        Assert.Contains(sliceTokens, t => t.Value == "y");
        Assert.Contains(sliceTokens, t => t.Value == "z");
        Assert.DoesNotContain(sliceTokens, t => t.Value == "x");
        Assert.DoesNotContain(sliceTokens, t => t.Value == "w");
    }

    [Fact]
    public void NonMonotonicTokens_FallsBackCleanlyWithoutThrowing()
    {
        var manualTokens = new List<LangToken>
        {
            new("second", LangTokenType.Identifier, 5, 0),
            new("first", LangTokenType.Identifier, 2, 4)
        };

        var index = new TokenLineIndex(manualTokens);
        Assert.False(index.IsMonotonic);

        var hit = index.FindTokenAtPosition(1, 5); // LSP line 1 -> token line 2
        Assert.NotNull(hit);
        Assert.Equal("first", hit.Value.Token.Value);
        Assert.Equal(1, hit.Value.Index);
    }

    [Fact]
    public void DocumentParseResult_ReplacingTokens_InvalidatesCachedIndex()
    {
        var doc = new DocumentParseResult
        {
            Uri = "file:///reindex.old8",
            Text = "a <- 1\n",
            Tokens = LangTokenizer.Tokenize("a <- 1\n"),
            Diagnostics = []
        };

        var firstIndex = doc.TokenIndex;
        Assert.NotNull(firstIndex.FindTokenAtPosition(0, 0));
        Assert.Equal("a", firstIndex.FindTokenAtPosition(0, 0)!.Value.Token.Value);

        doc.Tokens = LangTokenizer.Tokenize("beta <- 2\n");
        var secondIndex = doc.TokenIndex;
        Assert.NotSame(firstIndex, secondIndex);
        Assert.Equal("beta", secondIndex.FindTokenAtPosition(0, 0)!.Value.Token.Value);
    }
}
