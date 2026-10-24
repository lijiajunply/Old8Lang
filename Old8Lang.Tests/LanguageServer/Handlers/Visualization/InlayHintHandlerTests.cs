using Old8Lang.LanguageServer.Handlers;
using Old8Lang.LanguageServer.Services;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;

namespace Old8Lang.Tests.LanguageServer.Handlers.Visualization;

/// <summary>
/// 参数名内联提示测试
/// </summary>
public class InlayHintHandlerTests : IDisposable
{
    private const string Source = """
                                 func add(a: int, b: int) -> int {
                                     return a + b
                                 }

                                 result <- add(1, 2)
                                 """;

    private readonly DocumentManager _documentManager = new();
    private readonly string _uri = "file:///inlay.old8";

    public InlayHintHandlerTests()
    {
        _documentManager.UpdateDocument(_uri, Source);
    }

    public void Dispose()
    {
        _documentManager.CloseDocument(_uri);
    }

    private Task<InlayHintContainer?> RequestAsync(InlayHintHandler handler, string uri, int endLine = 100)
    {
        return handler.Handle(new InlayHintParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(0, 0, endLine, 0)
        }, CancellationToken.None);
    }

    private static string LabelOf(InlayHint hint)
    {
        return hint.Label.HasString ? hint.Label.String ?? string.Empty : string.Empty;
    }

    [Fact]
    public async Task Handle_AnnotatesPositionalArgumentsWithParameterNames()
    {
        var handler = new InlayHintHandler(_documentManager);

        var result = await RequestAsync(handler, _uri);

        Assert.NotNull(result);
        var hints = result!.ToList();

        Assert.Equal(2, hints.Count);
        Assert.Equal("a:", LabelOf(hints[0]));
        Assert.Equal("b:", LabelOf(hints[1]));
        Assert.Equal(new Position(4, 14), hints[0].Position);
        Assert.Equal(new Position(4, 17), hints[1].Position);
        Assert.All(hints, hint => Assert.Equal(InlayHintKind.Parameter, hint.Kind));
        Assert.All(hints, hint => Assert.True(hint.PaddingRight));
    }

    [Fact]
    public async Task Handle_DoesNotAnnotateFunctionDeclaration()
    {
        var handler = new InlayHintHandler(_documentManager);

        var result = await RequestAsync(handler, _uri);

        Assert.All(result!, hint => Assert.Equal(4, hint.Position.Line));
    }

    [Fact]
    public async Task Handle_SkipsCallsWithMismatchedArgumentCount()
    {
        _documentManager.UpdateDocument(_uri, """
                                                 func add(a: int, b: int) -> int {
                                                     return a + b
                                                 }

                                                 add(1)
                                                 """);

        var handler = new InlayHintHandler(_documentManager);
        var result = await RequestAsync(handler, _uri);

        Assert.Empty(result!);
    }

    [Fact]
    public async Task Handle_AnnotatesConstructorArgumentsFromInit()
    {
        _documentManager.UpdateDocument(_uri, """
                                                 class Point {
                                                     public x: int
                                                     public func init(x: int) {
                                                         this.x <- x
                                                     }
                                                 }

                                                 p <- Point(3)
                                                 """);

        var handler = new InlayHintHandler(_documentManager);
        var result = await RequestAsync(handler, _uri);

        var hint = Assert.Single(result!);
        Assert.Equal("x:", LabelOf(hint));
        Assert.Equal(new Position(7, 11), hint.Position);
    }

    [Fact]
    public async Task Handle_ResolvesFunctionFromAnotherFile()
    {
        var root = Path.Combine(Path.GetTempPath(), "old8lang-inlay-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            File.WriteAllText(Path.Combine(root, "defs.old8"),
                "func combine(left: int, right: int) -> int {\n    return left + right\n}\n");

            const string callUri = "file:///inlay-call.old8";
            _documentManager.UpdateDocument(callUri, "combine(1, 2)\n");

            var index = new WorkspaceIndex(_documentManager);
            index.SetRoots([root]);

            var handler = new InlayHintHandler(_documentManager, index);
            var result = await RequestAsync(handler, callUri);

            Assert.NotNull(result);
            Assert.Equal(2, result!.Count());
            Assert.Equal("left:", LabelOf(result.First()));
            Assert.Equal("right:", LabelOf(result.Last()));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Handle_RangeFilterExcludesOutsideHints()
    {
        var handler = new InlayHintHandler(_documentManager);

        var result = await RequestAsync(handler, _uri, endLine: 2);

        Assert.Empty(result!);
    }
}
