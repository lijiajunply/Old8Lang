using Old8Lang.LanguageServer.Services;
using Old8Lang.LanguageServer.Handlers;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit.Abstractions;

namespace Old8Lang.Tests.LanguageServer;

/// <summary>
/// 表达式和运算符补全功能测试
/// 测试各种运算符和表达式的补全行为
/// </summary>
public class CompletionHandler_ExpressionsTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public async Task ArithmeticOperators_CompletionAvailable()
    {
        var code = @"func main() -> void {
    a <- 10
    b <- 20
    result <- a + b
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

        Assert.Contains(items, i => i.Label == "a");
        Assert.Contains(items, i => i.Label == "b");
        Assert.Contains(items, i => i.Label == "result");

        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task ComparisonOperators_InContext()
    {
        var code = @"func main() -> void {
    x <- 10
    if x > 5 {
        PrintLine(""x is greater"")
    }
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
        var xVar = items.FirstOrDefault(i => i.Label == "x");
        Assert.NotNull(xVar);
        Assert.Equal(CompletionItemKind.Variable, xVar.Kind);

        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task LogicalOperators_InExpression()
    {
        var code = @"func main() -> void {
    flag1 <- true
    flag2 <- false
    result <- flag1 and flag2
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
        Assert.Contains(items, i => i.Label == "flag1");
        Assert.Contains(items, i => i.Label == "flag2");

        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task MemberAccess_DotOperator()
    {
        var code = @"class Person {
    public name <- ""
    func greet() -> void {
        PrintLine(""Hello, "" + name)
    }
}

func main() -> void {
    person <- new Person()
    person.$1greet()
}
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(12, 12)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        Assert.Contains(items, i => i.Label == "name");
        Assert.Contains(items, i => i.Label == "greet");

        var nameItem = items.FirstOrDefault(i => i.Label == "name");
        Assert.NotNull(nameItem);
        Assert.Equal(CompletionItemKind.Property, nameItem.Kind);

        var greetItem = items.FirstOrDefault(i => i.Label == "greet");
        Assert.NotNull(greetItem);
        Assert.Equal(CompletionItemKind.Method, greetItem.Kind);

        _output.WriteLine($"Found {items.Count} member items");
        foreach (var item in items)
        {
            _output.WriteLine($"  - {item.Label}: {item.Kind}");
        }
    }

    [Fact]
    public async Task ArrayAccess_IndexOperator()
    {
        var code = @"func main() -> void {
    arr <- [1, 2, 3, 4, 5]
    element <- arr[$10]
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
        var arrVar = items.FirstOrDefault(i => i.Label == "arr");
        Assert.NotNull(arrVar);
        Assert.Equal(CompletionItemKind.Variable, arrVar.Kind);

        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task DictionaryAccess_StringKey()
    {
        var code = @"func main() -> void {
    dict <- {""name"": ""Alice"", ""age"": 30}
    name <- dict[""$1""]
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
        Assert.Contains(items, i => i.Label == "dict");

        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task TypeConversion_AsOperator()
    {
        var code = @"func main() -> void {
    a <- 123
    b <- a as double
    c <- b as int
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
        Assert.Contains(items, i => i.Label == "a");
        Assert.Contains(items, i => i.Label == "b");
        Assert.Contains(items, i => i.Label == "c");

        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task TernaryOperator_InExpression()
    {
        var code = @"func main() -> void {
    x <- 10
    max <- x > 5 ? x : 5
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
        var xVar = items.FirstOrDefault(i => i.Label == "x");
        Assert.NotNull(xVar);

        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task StringTemplate_Interpolation()
    {
        var code = @"func main() -> void {
    name <- ""Alice""
    age <- 30
    message <- $""My name is {$1} and I'm {$2} years old.""
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
        Assert.Contains(items, i => i.Label == "name");
        Assert.Contains(items, i => i.Label == "age");

        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task LambdaExpression_InAssignment()
    {
        var code = @"func main() -> void {
    square <- (x:int) -> x * x
    result <- square(5)
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
        var squareVar = items.FirstOrDefault(i => i.Label == "square");
        Assert.NotNull(squareVar);
        Assert.Equal(CompletionItemKind.Variable, squareVar.Kind);

        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task MatchExpression_PatternCompletion()
    {
        var code = @"func main() -> void {
    value <- 42
    result <- match value {
        case 0 -> ""zero""
        case x -> ""The value is "" + x.ToStr()
        case _ -> ""other""
    }
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
        var valueVar = items.FirstOrDefault(i => i.Label == "value");
        Assert.NotNull(valueVar);

        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task ChainedMemberAccess_ShouldComplete()
    {
        var code = @"class Outer {
    public inner <- null

    func init() {
        inner <- new Inner()
    }
}

class Inner {
    public value <- 0
    func getValue() -> int {
        return value
    }
}

func main() -> void {
    outer <- new Outer()
    result <- outer.$1inner.$2getValue()
}
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(24, 20)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        Assert.Contains(items, i => i.Label == "value");
        Assert.Contains(items, i => i.Label == "getValue");

        _output.WriteLine($"Found {items.Count} items in chained access");
        foreach (var item in items)
        {
            _output.WriteLine($"  - {item.Label}: {item.Kind}");
        }
    }

    [Fact]
    public async Task ComplexExpression_NestedOperations()
    {
        var code = @"func main() -> void {
    a <- 10
    b <- 20
    c <- 30
    result <- (a + b) * c / 100
}
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(5, 4)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        Assert.Contains(items, i => i.Label == "a");
        Assert.Contains(items, i => i.Label == "b");
        Assert.Contains(items, i => i.Label == "c");

        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task FunctionCallWithArguments_Completion()
    {
        var code = @"func calculate(x:int, y:int) -> int {
    return x + y
}

func main() -> void {
    result <- calculate($1, $2)
}
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(7, 4)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        var calculateFunc = items.FirstOrDefault(i => i.Label == "calculate");
        Assert.NotNull(calculateFunc);
        Assert.Equal(CompletionItemKind.Function, calculateFunc.Kind);

        _output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task SliceOperation_ArraySlicing()
    {
        var code = @"func main() -> void {
    arr <- [1, 2, 3, 4, 5]
    slice <- arr[1:3]
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
        var arrVar = items.FirstOrDefault(i => i.Label == "arr");
        Assert.NotNull(arrVar);

        _output.WriteLine($"Found {items.Count} items");
    }
}
