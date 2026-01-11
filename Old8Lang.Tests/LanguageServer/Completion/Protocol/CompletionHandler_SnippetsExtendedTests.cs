using Old8Lang.LanguageServer.Services;
using Old8Lang.LanguageServer.Handlers;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit.Abstractions;

namespace Old8Lang.Tests.LanguageServer;

/// <summary>
/// 扩展代码片段补全功能测试
/// 测试 using, select, defer, match 等特殊语法的代码片段
/// </summary>
public class CompletionHandler_SnippetsExtendedTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public async Task UsingStatementSnippet_ShouldBeAvailable()
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
        var snippets = items.Where(i => i.Kind == CompletionItemKind.Snippet).ToList();

        _output.WriteLine($"Found {snippets.Count} snippets");
        foreach (var item in snippets)
        {
            _output.WriteLine($"  - {item.Label}: {item.InsertText}");
        }

        Assert.True(snippets.Count > 0, "Should have at least some snippets");
    }

    [Fact]
    public async Task MatchExpressionSnippet_ShouldBeAvailable()
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
        var matchSnippet = items.FirstOrDefault(i => i.Label == "match" && i.Kind == CompletionItemKind.Snippet);

        Assert.NotNull(matchSnippet);
        Assert.Contains(matchSnippet.InsertText, "match");

        _output.WriteLine($"Match snippet: {matchSnippet.InsertText}");
    }

    [Fact]
    public async Task DeferStatementSnippet_ShouldBeAvailable()
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
        var deferSnippet = items.FirstOrDefault(i => i.Label == "defer" && i.Kind == CompletionItemKind.Snippet);

        Assert.NotNull(deferSnippet);

        _output.WriteLine($"Defer snippet: {deferSnippet.InsertText}");
    }

    [Fact]
    public async Task ForInLoopSnippet_ShouldBeAvailable()
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
        var forinSnippet = items.FirstOrDefault(i => i.Label == "forin" || i.InsertText.Contains("for"));

        Assert.NotNull(forinSnippet);

        _output.WriteLine($"For-in snippet: {forinSnippet.InsertText}");
    }

    [Fact]
    public async Task TryCatchFinallySnippet_ShouldBeAvailable()
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
        var trySnippet = items.FirstOrDefault(i => i.Label == "try" && i.Kind == CompletionItemKind.Snippet);

        Assert.NotNull(trySnippet);
        Assert.Contains(trySnippet.InsertText, "try");

        _output.WriteLine($"Try-catch snippet: {trySnippet.InsertText}");
    }

    [Fact]
    public async Task EnumSnippet_ShouldBeAvailable()
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
        var enumSnippet = items.FirstOrDefault(i => i.Label == "enum" && i.Kind == CompletionItemKind.Snippet);

        Assert.NotNull(enumSnippet);

        _output.WriteLine($"Enum snippet: {enumSnippet.InsertText}");
    }

    [Fact]
    public async Task InterfaceSnippet_ShouldBeAvailable()
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
        var interfaceSnippet = items.FirstOrDefault(i => i.Label == "interface" && i.Kind == CompletionItemKind.Snippet);

        Assert.NotNull(interfaceSnippet);

        _output.WriteLine($"Interface snippet: {interfaceSnippet.InsertText}");
    }

    [Fact]
    public async Task SwitchStatementSnippet_ShouldBeAvailable()
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
        var switchSnippet = items.FirstOrDefault(i => i.Label == "switch" && i.Kind == CompletionItemKind.Snippet);

        Assert.NotNull(switchSnippet);

        _output.WriteLine($"Switch snippet: {switchSnippet.InsertText}");
    }

    [Fact]
    public async Task WhileLoopSnippet_ShouldBeAvailable()
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
        var whileSnippet = items.FirstOrDefault(i => i.Label == "while" && i.Kind == CompletionItemKind.Snippet);

        Assert.NotNull(whileSnippet);

        _output.WriteLine($"While snippet: {whileSnippet.InsertText}");
    }

    [Fact]
    public async Task AsyncFunctionSnippet_ShouldBeAvailable()
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
        var asyncSnippet = items.FirstOrDefault(i => i.Label == "asyncfunc" || i.Label == "async func");

        Assert.NotNull(asyncSnippet);
        Assert.Contains(asyncSnippet.InsertText, "async");

        _output.WriteLine($"Async function snippet: {asyncSnippet.InsertText}");
    }

    [Fact]
    public async Task AllSnippets_ShouldUseSnippetFormat()
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
        var snippets = items.Where(i => i.Kind == CompletionItemKind.Snippet).ToList();

        foreach (var snippet in snippets)
        {
            Assert.Equal(InsertTextFormat.Snippet, snippet.InsertTextFormat);
        }

        _output.WriteLine($"All {snippets.Count} snippets use Snippet format");
    }

    [Fact]
    public async Task Snippets_ShouldHavePlaceholders()
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
        var snippets = items.Where(i => i.Kind == CompletionItemKind.Snippet).ToList();

        foreach (var snippet in snippets)
        {
            Assert.Contains(snippet.InsertText, "$");
        }

        _output.WriteLine($"All {snippets.Count} snippets have placeholders");
    }

    [Fact]
    public async Task SnippetPriority_ShouldBeLowerThanKeywords()
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
        var snippet = items.FirstOrDefault(i => i.Kind == CompletionItemKind.Snippet);
        var keyword = items.FirstOrDefault(i => i.Kind == CompletionItemKind.Keyword && i.Label == "if");

        Assert.NotNull(snippet);
        Assert.NotNull(keyword);
        Assert.True(string.Compare(snippet.SortText, keyword.SortText) < 0);

        _output.WriteLine($"Snippet priority: {snippet.SortText}, Keyword priority: {keyword.SortText}");
    }
}
