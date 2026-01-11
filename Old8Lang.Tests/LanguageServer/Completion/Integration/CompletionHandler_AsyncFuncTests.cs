using Old8Lang.LanguageServer.Services;
using Old8Lang.LanguageServer.Handlers;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit.Abstractions;

namespace Old8Lang.Tests.LanguageServer;

/// <summary>
/// 异步函数关键字验证测试
/// 验证 asyncfunc 关键字的正确存在
/// </summary>
public class CompletionHandler_AsyncFuncTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public async Task AsyncFuncKeyword_ShouldBeAvailable()
    {
        var code = @"";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(0, 0)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        var asyncfuncKeyword = items.FirstOrDefault(i => i.Label == "asyncfunc");
        Assert.NotNull(asyncfuncKeyword);
        Assert.Equal(CompletionItemKind.Keyword, asyncfuncKeyword.Kind);

        _output.WriteLine($"Found asyncfunc keyword: {asyncfuncKeyword.Label}");
    }

    [Fact]
    public async Task AsyncFuncSnippet_ShouldBeAvailable()
    {
        var code = @"";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(0, 0)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        var asyncfuncSnippet = items.FirstOrDefault(i => i.Kind == CompletionItemKind.Snippet && i.Label == "async func");

        Assert.NotNull(asyncfuncSnippet);
        Assert.Contains(asyncfuncSnippet.InsertText, "async func");

        _output.WriteLine($"Found asyncfunc snippet");
    }
}
