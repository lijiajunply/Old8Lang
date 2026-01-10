using Old8Lang.LanguageServer.Services;
using Old8Lang.LanguageServer.Handlers;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit.Abstractions;

namespace Old8Lang.Tests.LanguageServer;

/// <summary>
/// 文件头指令补全功能测试
/// 测试 #! 开头的元数据和配置指令
/// </summary>
public class CompletionHandler_DirectivesTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public async Task EncodingDirective_ShouldComplete()
    {
        var code = @"#!encoding
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(0, 10)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        _output.WriteLine($"Found {items.Count} items");

        foreach (var item in items.Take(10))
        {
            _output.WriteLine($"  - {item.Label} ({item.Kind})");
        }
    }

    [Fact]
    public async Task AuthorDirective_ShouldComplete()
    {
        var code = @"#!author
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(0, 9)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task VersionDirective_ShouldComplete()
    {
        var code = @"#!version
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(0, 9)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task DateDirective_ShouldComplete()
    {
        var code = @"#!date
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(0, 6)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task DescriptionDirective_ShouldComplete()
    {
        var code = @"#!description
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(0, 14)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task DebugDirective_ShouldComplete()
    {
        var code = @"#!debug
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(0, 7)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task VerifyIlDirective_ShouldComplete()
    {
        var code = @"#!verify-il
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(0, 11)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task TypeInferenceDirective_ShouldComplete()
    {
        var code = @"#!type-inference
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(0, 16)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task TypeInferenceDebugDirective_ShouldComplete()
    {
        var code = @"#!type-inference-debug
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(0, 22)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task OptimizeDirective_ShouldComplete()
    {
        var code = @"#!optimize
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(0, 9)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task MultipleDirectives_Completion()
    {
        var code = @"#!encoding utf-8
#!author 张三
#!version 1.0.0
#!date 2025-12-28

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

        Assert.Contains(items, i => i.Label == "main");

        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task DirectiveAtFileStart_ShouldComplete()
    {
        var code = @"#
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(0, 1)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();

        _output.WriteLine($"Found {items.Count} items at file start with '#'");
        foreach (var item in items.Take(10))
        {
            _output.WriteLine($"  - {item.Label} ({item.Kind})");
        }
    }

    [Fact]
    public async Task DirectiveInMiddleOfFile_ShouldStillComplete()
    {
        var code = @"func main() -> void {
}

#!version 2.0
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(3, 0)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        _output.WriteLine($"Found {items.Count} items");

        foreach (var item in items.Take(10))
        {
            _output.WriteLine($"  - {item.Label} ({item.Kind})");
        }
    }

    [Fact]
    public async Task DirectiveAfterComment_ShouldComplete()
    {
        var code = @"// File header comment
#!encoding utf-8

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
            Position = new Position(1, 0)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        _output.WriteLine($"Found {items.Count} items");

        foreach (var item in items.Take(10))
        {
            _output.WriteLine($"  - {item.Label} ({item.Kind})");
        }
    }

    [Fact]
    public async Task DirectivesWithEmptyLines_ShouldComplete()
    {
        var code = @"


#!author Author Name


#!description File description


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
            Position = new Position(2, 0)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        _output.WriteLine($"Found {items.Count} items");

        foreach (var item in items.Take(10))
        {
            _output.WriteLine($"  - {item.Label} ({item.Kind})");
        }
    }
}
