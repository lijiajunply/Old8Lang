using Old8Lang.LanguageServer.Handlers;
using Old8Lang.LanguageServer.Services;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit.Abstractions;

namespace Old8Lang.Tests.LanguageServer.Completion.Features;

/// <summary>
/// 联合类型和交叉类型补全功能测试
/// 测试联合类型（union types）和交叉类型（intersection types）的补全
/// </summary>
public class CompletionHandler_UnionTypesTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public async Task UnionTypeSyntax_InTypeAnnotation()
    {
        var code = @"func process(value:int? | string?) -> void {
}
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(1, 4)
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
    public async Task UnionTypeSyntax_InVariableDeclaration()
    {
        var code = @"result:int? | string? <- null
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(0, 4)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task UnionTypeSyntax_InFunctionParameter()
    {
        var code = @"func process(value:int? | double?) -> void {
}
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(1, 4)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task UnionTypeSyntax_InReturnType()
    {
        var code = @"func getValue() -> int? | string? {
}
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(1, 4)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task UnionTypeSyntax_InMatchExpression()
    {
        var code = @"result <- match value {
    case num:int? -> ""number""
    case str:string? -> ""string""
    case _ -> ""other""
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
        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task IntersectionTypeSyntax_InTypeAnnotation()
    {
        var code = @"func process(value: IComparable & ICloneable) -> void {
}
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(1, 4)
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
    public async Task IntersectionTypeSyntax_MultipleConstraints()
    {
        var code = @"func process(value: ISerializable & ICloneable & IComparable) -> void {
}
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(1, 4)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task IntersectionTypeSyntax_InGenericParameter()
    {
        var code = @"class Container<T: IComparable & ICloneable> {
    value:T
}
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(2, 4)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task UnionAndIntersectionSyntax_InReturnType()
    {
        var code = @"func get() -> (int? | string?) & IComparable {
}
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(1, 4)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task PipeOperatorSyntax_InExpression()
    {
        var code = @"result <- value | transform
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(1, 4)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task ComplexTypeAnnotation_Completion()
    {
        var code = @"data:(int? | list<string> & ISerializable) <- null
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(0, 4)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task UnionTypeInGenericClass()
    {
        var code = @"class Either<L, R> {
    left:L
    right:R
}
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(2, 4)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task NullableTypeKeyword_ShouldBeAvailable()
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
        var pipeChar = items.FirstOrDefault(i => i.Label == "|");

        Assert.NotNull(pipeChar);
        Assert.Equal(CompletionItemKind.Operator, pipeChar.Kind);

        _output.WriteLine($"Found {items.Count} items");
        _output.WriteLine($"| operator found: {pipeChar?.Label}");
    }

    [Fact]
    public async Task AmpersandOperator_ShouldBeAvailable()
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
        var ampersandChar = items.FirstOrDefault(i => i.Label == "&");

        Assert.NotNull(ampersandChar);
        Assert.Equal(CompletionItemKind.Operator, ampersandChar.Kind);

        _output.WriteLine($"Found {items.Count} items");
        _output.WriteLine($"& operator found: {ampersandChar?.Label}");
    }
}
