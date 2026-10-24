using Old8Lang.LanguageServer.Handlers;
using Old8Lang.LanguageServer.Services;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;

namespace Old8Lang.Tests.LanguageServer.Handlers.Visualization;

/// <summary>
/// SelectionRange 处理器测试
/// </summary>
public class SelectionRangeHandlerTests
{
    private const string Source = """
                                 func add(a: int, b: int) -> int {
                                     return a + b
                                 }

                                 PrintLine(add(1, 2))
                                 """;

    private static SelectionRangeHandler CreateHandler(out string uri)
    {
        uri = "file:///selection.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, Source);
        return new SelectionRangeHandler(documentManager);
    }

    private static Task<Container<SelectionRange>?> RequestAsync(SelectionRangeHandler handler, string uri,
        params Position[] positions)
    {
        return handler.Handle(new SelectionRangeParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Positions = new Container<Position>(positions)
        }, CancellationToken.None);
    }

    private static List<SelectionRange> Flatten(SelectionRange range)
    {
        var chain = new List<SelectionRange>();
        for (var node = range; node != null; node = node.Parent)
        {
            chain.Add(node);
        }

        return chain;
    }

    [Fact]
    public async Task Handle_TokenInsideCall_BuildsNestedBracketAndStatementRanges()
    {
        var handler = CreateHandler(out var uri);

        var result = await RequestAsync(handler, uri, new Position(4, 17));

        var chain = Flatten(Assert.Single(result!));

        // token "2"
        Assert.Equal(new Position(4, 17), chain[0].Range.Start);
        Assert.Equal(new Position(4, 18), chain[0].Range.End);

        // 内层参数括号
        Assert.Contains(chain, node =>
            node.Range.Start.Line == 4 && node.Range.Start.Character == 13 &&
            node.Range.End.Character == 19);

        // 外层调用括号
        Assert.Contains(chain, node =>
            node.Range.Start.Line == 4 && node.Range.Start.Character == 9 &&
            node.Range.End.Character == 20);

        // 整条语句
        Assert.Contains(chain, node =>
            node.Range.Start.Line == 4 && node.Range.Start.Character == 0 &&
            node.Range.End.Line == 4 && node.Range.End.Character == 20);
    }

    [Fact]
    public async Task Handle_PositionInsideFunction_IncludesDeclarationBlock()
    {
        var handler = CreateHandler(out var uri);

        var result = await RequestAsync(handler, uri, new Position(1, 11));

        var chain = Flatten(Assert.Single(result!));

        // 函数体语句
        Assert.Contains(chain, node =>
            node.Range.Start.Line == 1 && node.Range.Start.Character == 4 &&
            node.Range.End.Line == 1);

        // 声明块从 func 开始，到右花括号结束
        var declaration = Assert.Single(chain, node =>
            node.Range.Start.Line == 0 && node.Range.Start.Character == 0 &&
            node.Range.End.Line == 2);
        Assert.Equal(2, declaration.Range.End.Line);

        // 最外层应覆盖全文
        Assert.Equal(0, chain[^1].Range.Start.Line);
        Assert.Equal(4, chain[^1].Range.End.Line);
    }

    [Fact]
    public async Task Handle_EachRangeContainsPreviousRange()
    {
        var handler = CreateHandler(out var uri);

        var result = await RequestAsync(handler, uri, new Position(1, 13));

        var chain = Flatten(Assert.Single(result!));

        for (var i = 1; i < chain.Count; i++)
        {
            var child = chain[i - 1].Range;
            var parent = chain[i].Range;

            Assert.True(parent.Start.Line < child.Start.Line ||
                        (parent.Start.Line == child.Start.Line && parent.Start.Character <= child.Start.Character));
            Assert.True(parent.End.Line > child.End.Line ||
                        (parent.End.Line == child.End.Line && parent.End.Character >= child.End.Character));
        }
    }

    [Fact]
    public async Task Handle_MultiplePositions_ReturnsOneChainPerPosition()
    {
        var handler = CreateHandler(out var uri);

        var result = await RequestAsync(handler, uri, new Position(1, 11), new Position(4, 17));

        Assert.Equal(2, result!.Count());
        Assert.All(result!, range => Assert.NotNull(range.Parent));
    }

    [Fact]
    public async Task Handle_PositionOnWhitespace_StillReturnsDocumentChain()
    {
        var handler = CreateHandler(out var uri);

        // 空行位置
        var result = await RequestAsync(handler, uri, new Position(3, 0));

        var range = Assert.Single(result!);
        var chain = Flatten(range);

        Assert.Equal(new Position(3, 0), chain[0].Range.Start);
        Assert.Equal(chain[0].Range.Start, chain[0].Range.End);
        Assert.True(chain.Count >= 2);
    }

    [Fact]
    public async Task Handle_UnknownDocument_ReturnsEmptyContainer()
    {
        var handler = new SelectionRangeHandler(new DocumentManager());

        var result = await RequestAsync(handler, "file:///missing.old8", new Position(0, 0));

        Assert.Empty(result!);
    }
}
