using Old8Lang.LanguageServer.Handlers;
using Old8Lang.LanguageServer.Services;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit.Abstractions;

namespace Old8Lang.Tests.LanguageServer.Completion.Protocol;

public class CompletionHandler_LspProtocolTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;
    private const string TestUri = "file:///test/document.old8";

    [Fact]
    public async Task TestPositionEncoding_ZeroBased()
    {
        var code = @"
using Math

func TestFunc() {
    PrintLine(""hello"")
}
";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(TestUri, code);
        var handler = new CompletionHandler(documentManager);

        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(TestUri) },
            Position = new Position(3, 4)
        };

        var result = await handler.Handle(request, CancellationToken.None);

        Assert.NotNull(result);
        var items = result.Items.ToList();
        Assert.NotEmpty(items);
        _output.WriteLine($"Completions at position (3, 4): {items.Count} items");
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(2, 0)]
    [InlineData(4, 10)]
    public async Task TestPositionEncoding_VariousPositions(int line, int character)
    {
        var code = @"
using Math

func TestFunc() {
    PrintLine(""hello"")
}
";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(TestUri, code);
        var handler = new CompletionHandler(documentManager);

        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(TestUri) },
            Position = new Position(line, character)
        };

        var result = await handler.Handle(request, CancellationToken.None);

        Assert.NotNull(result);
        var items = result.Items.ToList();
        Assert.NotNull(items);
        _output.WriteLine($"Completions at ({line}, {character}): {items.Count} items");
    }

    [Theory]
    [InlineData(".")]
    [InlineData(":")]
    [InlineData(" ")]
    public async Task TestTriggerCharacter_Behavior(string triggerChar)
    {
        var code = @$"
using Math

func TestFunc() {{
    Math.{triggerChar}
}}

class TestClass {{
    name{triggerChar} string
}}
";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(TestUri, code);
        var handler = new CompletionHandler(documentManager);

        var position = code.IndexOf(triggerChar) + 1;
        var line = code.Substring(0, position).Count(c => c == '\n');
        var column = position - code.LastIndexOf('\n', position) - 1;

        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(TestUri) },
            Position = new Position(line, column)
        };

        var result = await handler.Handle(request, CancellationToken.None);

        Assert.NotNull(result);
        _output.WriteLine($"Trigger '{triggerChar}': {result.Items.Count()} completions");
    }

    [Fact]
    public async Task TestCompletionItem_RequiredFields()
    {
        var code = @"
using Math

func TestFunc() {
    Print
}
";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(TestUri, code);
        var handler = new CompletionHandler(documentManager);

        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(TestUri) },
            Position = new Position(3, 4)
        };

        var result = await handler.Handle(request, CancellationToken.None);

        Assert.NotNull(result);
        var items = result.Items.ToList();
        Assert.NotEmpty(items);

        foreach (var item in items.Take(5))
        {
            Assert.NotNull(item.Label);
            Assert.False(string.IsNullOrWhiteSpace(item.Label));
            Assert.NotNull(item.Kind);
            _output.WriteLine($"Item: {item.Label}, Kind: {item.Kind}");
        }
    }

    [Fact]
    public async Task TestCompletionItemKind_Classification()
    {
        var code = @"
using Math

class TestClass {
    name string
}

func TestFunc() {

}
";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(TestUri, code);
        var handler = new CompletionHandler(documentManager);

        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(TestUri) },
            Position = new Position(6, 4)
        };

        var result = await handler.Handle(request, CancellationToken.None);

        Assert.NotNull(result);
        var items = result.Items.ToList();
        Assert.NotEmpty(items);

        var hasFunction = items.Any(i => i.Kind == CompletionItemKind.Function);
        var hasKeyword = items.Any(i => i.Kind == CompletionItemKind.Keyword);
        var hasSnippet = items.Any(i => i.Kind == CompletionItemKind.Snippet);

        Assert.True(hasKeyword || hasSnippet, "Expected keywords or snippets in completions");
        _output.WriteLine($"Functions: {hasFunction}, Keywords: {hasKeyword}, Snippets: {hasSnippet}");
    }

    [Fact]
    public async Task TestDocumentIdentifier_UriFormat()
    {
        var code = @"
using Math

func TestFunc() {

}
";
        var documentManager = new DocumentManager();
        var uri = "file:///absolute/path/to/document.old8";
        documentManager.UpdateDocument(uri, code);
        var handler = new CompletionHandler(documentManager);

        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(3, 4)
        };

        var result = await handler.Handle(request, CancellationToken.None);

        Assert.NotNull(result);
        _output.WriteLine($"URI format: {uri}");
    }

    [Fact]
    public async Task TestCancellationToken_Behavior()
    {
        var code = @"
using Math

func TestFunc() {
    Print
}
";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(TestUri, code);
        var handler = new CompletionHandler(documentManager);

        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(TestUri) },
            Position = new Position(3, 4)
        };

        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await handler.Handle(request, cts.Token);
        });
    }

    [Fact]
    public async Task TestEmptyDocument_Handling()
    {
        var code = "";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(TestUri, code);
        var handler = new CompletionHandler(documentManager);

        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(TestUri) },
            Position = new Position(0, 0)
        };

        var result = await handler.Handle(request, CancellationToken.None);

        Assert.NotNull(result);
        var items = result.Items.ToList();
        Assert.NotEmpty(items);
        _output.WriteLine($"Empty document: {items.Count} completions");
    }

    [Fact]
    public async Task TestMultibyteCharacters_Handling()
    {
        var code = @"
使用中文

func 测试函数() {
    打印
}
";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(TestUri, code);
        var handler = new CompletionHandler(documentManager);

        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(TestUri) },
            Position = new Position(4, 2)
        };

        var result = await handler.Handle(request, CancellationToken.None);

        Assert.NotNull(result);
        _output.WriteLine($"Multibyte chars: {result.Items.Count()} completions");
    }

    [Theory]
    [InlineData("Pri")]
    [InlineData("Mat")]
    [InlineData("str")]
    public async Task TestPartialInput_Filtering(string prefix)
    {
        var code = @$"
using Math

func TestFunc() {{
    {prefix}
}}
";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(TestUri, code);
        var handler = new CompletionHandler(documentManager);

        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(TestUri) },
            Position = new Position(3, prefix.Length)
        };

        var result = await handler.Handle(request, CancellationToken.None);

        Assert.NotNull(result);
        var items = result.Items.ToList();
        var hasMatches = items.Any(i =>
            i.Label.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

        _output.WriteLine($"Prefix '{prefix}': {items.Count} completions, Has matches: {hasMatches}");
    }

    [Fact]
    public async Task TestCompletionContext_Handling()
    {
        var code = @"
using Math

func TestFunc() {
    Print
}
";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(TestUri, code);
        var handler = new CompletionHandler(documentManager);

        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(TestUri) },
            Position = new Position(3, 4),
            Context = new CompletionContext
            {
                TriggerKind = CompletionTriggerKind.Invoked,
                TriggerCharacter = null
            }
        };

        var result = await handler.Handle(request, CancellationToken.None);

        Assert.NotNull(result);
        _output.WriteLine($"With context: {result.Items.Count()} completions");
    }

    [Fact]
    public async Task TestConsecutiveRequests_Independence()
    {
        var code = @"
using Math

func TestFunc() {
    Print
}
";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(TestUri, code);
        var handler = new CompletionHandler(documentManager);

        var request1 = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(TestUri) },
            Position = new Position(3, 4)
        };

        var request2 = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(TestUri) },
            Position = new Position(1, 0)
        };

        var result1 = await handler.Handle(request1, CancellationToken.None);
        var result2 = await handler.Handle(request2, CancellationToken.None);

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        var count1 = result1.Items.Count();
        var count2 = result2.Items.Count();

        _output.WriteLine($"Request 1: {count1}, Request 2: {count2}");
    }

    [Fact]
    public async Task TestDocumentUpdate_Consistency()
    {
        var initialCode = @"
using Math

func TestFunc() {
    Print
}
";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(TestUri, initialCode);
        var handler = new CompletionHandler(documentManager);

        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(TestUri) },
            Position = new Position(3, 4)
        };

        var result1 = await handler.Handle(request, CancellationToken.None);

        var updatedCode = @"
using Math

func NewFunc() {
    Print
}
";
        documentManager.UpdateDocument(TestUri, updatedCode);

        var result2 = await handler.Handle(request, CancellationToken.None);

        Assert.NotNull(result1);
        Assert.NotNull(result2);

        _output.WriteLine($"Before update: {result1.Items.Count()}, After update: {result2.Items.Count()}");
    }
}
