using Old8Lang.LanguageServer.Handlers;
using Old8Lang.LanguageServer.Services;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit.Abstractions;

namespace Old8Lang.Tests.LanguageServer.Completion.Integration;

/// <summary>
/// 异步函数补全测试
/// 验证 async 关键字和 async func 代码片段的正确存在
/// </summary>
public class CompletionHandler_AsyncFuncTests(ITestOutputHelper output)
{
    [Fact]
    public async Task AsyncKeyword_ShouldBeAvailable()
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
        var asyncKeyword = items.FirstOrDefault(i => i.Label == "async");
        Assert.NotNull(asyncKeyword);
        Assert.Equal(CompletionItemKind.Keyword, asyncKeyword.Kind);

        output.WriteLine($"Found async keyword: {asyncKeyword.Label}");
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
        Assert.Contains("async func", asyncfuncSnippet.InsertText);

        output.WriteLine($"Found async func snippet");
    }
}
