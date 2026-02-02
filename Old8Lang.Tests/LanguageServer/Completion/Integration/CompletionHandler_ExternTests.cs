using Old8Lang.LanguageServer.Handlers;
using Old8Lang.LanguageServer.Services;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit.Abstractions;

namespace Old8Lang.Tests.LanguageServer.Completion.Integration;

/// <summary>
/// Extern 导入语法补全功能测试
/// 测试 extern、native、import 等关键字
/// </summary>
public class CompletionHandler_ExternTests(ITestOutputHelper output)
{
    [Fact]
    public async Task ExternKeyword_ShouldComplete()
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
        var externKeyword = items.FirstOrDefault(i => i.Label == "extern");
        Assert.NotNull(externKeyword);
        Assert.Equal(CompletionItemKind.Keyword, externKeyword.Kind);

        output.WriteLine($"Found {items.Count} items");
    }

    // native 关键字已被删除，此测试不再需要

    [Fact]
    public async Task ImportKeyword_ShouldComplete()
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
        var importKeyword = items.FirstOrDefault(i => i.Label == "import");
        Assert.NotNull(importKeyword);
        Assert.Equal(CompletionItemKind.Keyword, importKeyword.Kind);

        output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task FromKeyword_ShouldComplete()
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
        var fromKeyword = items.FirstOrDefault(i => i.Label == "from");
        Assert.NotNull(fromKeyword);
        Assert.Equal(CompletionItemKind.Keyword, fromKeyword.Kind);

        output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task AsKeyword_ShouldComplete()
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
        var asKeyword = items.FirstOrDefault(i => i.Label == "as");
        Assert.NotNull(asKeyword);
        Assert.Equal(CompletionItemKind.Keyword, asKeyword.Kind);

        output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task AllImportKeywords_ShouldBeAvailable()
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
        var importKeywords = new[] { "import", "from", "as", "extern" };

        foreach (var keyword in importKeywords)
        {
            var keywordItem = items.FirstOrDefault(i => i.Label == keyword);
            if (keywordItem == null)
            {
                output.WriteLine($"Keyword '{keyword}' not found in completions");
            }
            Assert.True(keywordItem != null, $"Keyword '{keyword}' should be available");
            Assert.Equal(CompletionItemKind.Keyword, keywordItem.Kind);
        }

        output.WriteLine($"Found {items.Count} items");
        output.WriteLine("All import keywords verified");
    }

    [Fact]
    public async Task ImportStatement_ShouldCompleteAfterImport()
    {
        var code = @"import ""MathLib""

func main() -> void {
}
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(3, 4)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();

        output.WriteLine($"Found {items.Count} items");
        foreach (var item in items.Take(10))
        {
            output.WriteLine($"  - {item.Label} ({item.Kind})");
        }
    }

    [Fact]
    public async Task NativeFunctionDeclaration_Completion()
    {
        var code = @"extern ""MyLib.dll"" {
    func CPrint(msg:string) -> void
}

func main() -> void {
    CPrint(""Hello from C"")
}
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(4, 4)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        var cprintFunc = items.FirstOrDefault(i => i.Label == "CPrint");
        Assert.NotNull(cprintFunc);
        Assert.Equal(CompletionItemKind.Function, cprintFunc.Kind);

        output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task ImportWithAlias_SyntaxCompletion()
    {
        var code = @"import ""Math"" as math

func main() -> void {
    result <- math.sqrt(16)
}
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(4, 4)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        var mathVar = items.FirstOrDefault(i => i.Label == "math");
        Assert.NotNull(mathVar);
        Assert.Equal(CompletionItemKind.Variable, mathVar.Kind);

        output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task MultipleImports_Completion()
    {
        var code = @"import ""MathLib""
import ""OS""
import ""File""

func main() -> void {
}
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(6, 4)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();

        output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task ExternVariable_ShouldComplete()
    {
        var code = @"extern ""MyLib.dll"" {
    func getGlobalCounter() -> int
}

func main() -> void {
    counter <- getGlobalCounter()
    globalCounter <- counter
}
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(6, 4)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        var globalCounterVar = items.FirstOrDefault(i => i.Label == "counter" || i.Label == "getGlobalCounter");
        Assert.NotNull(globalCounterVar);

        output.WriteLine($"Found {items.Count} items");
    }
}
