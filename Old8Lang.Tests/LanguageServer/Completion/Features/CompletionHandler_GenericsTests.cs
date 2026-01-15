using Old8Lang.LanguageServer.Handlers;
using Old8Lang.LanguageServer.Services;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit.Abstractions;

namespace Old8Lang.Tests.LanguageServer.Completion.Features;

/// <summary>
/// 泛型补全功能测试
/// 测试泛型函数、泛型类、泛型约束等特性
/// </summary>
public class CompletionHandler_GenericsTests(ITestOutputHelper output)
{
    [Fact]
    public async Task GenericFunctionDefinition_ShouldComplete()
    {
        var code = @"func identity<T>(value:T) -> T {
    return value
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
        var valueVar = items.FirstOrDefault(i => i.Label == "value");
        Assert.NotNull(valueVar);
        Assert.Equal(CompletionItemKind.Variable, valueVar.Kind);

        output.WriteLine($"Found {items.Count} items");
        foreach (var item in items.Take(10))
        {
            output.WriteLine($"  - {item.Label} ({item.Kind})");
        }
    }

    [Fact]
    public async Task GenericFunctionCall_ShouldComplete()
    {
        var code = @"func identity<T>(value:T) -> T {
    return value
}

func main() -> void {
    result <- identity<int>($1)
}
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(6, 30)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        var identityFunc = items.FirstOrDefault(i => i.Label == "identity");
        Assert.NotNull(identityFunc);
        Assert.Equal(CompletionItemKind.Function, identityFunc.Kind);

        output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task GenericClassDefinition_ShouldComplete()
    {
        var code = @"class Box<T> {
    private value:T

    func set(v:T) -> void {
        this.value <- v
    }

    func get() -> T {
        return this.value
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
            Position = new Position(3, 8)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        var valueField = items.FirstOrDefault(i => i.Label == "value");
        var thisKeyword = items.FirstOrDefault(i => i.Label == "this");
        Assert.NotNull(valueField);
        Assert.Equal(CompletionItemKind.Field, valueField.Kind);
        Assert.NotNull(thisKeyword);
        Assert.Equal(CompletionItemKind.Keyword, thisKeyword.Kind);

        output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task GenericClassInstantiation_ShouldComplete()
    {
        var code = @"class Box<T> {
    private value:T
    func init(v:T) {
        this.value <- v
    }
}

func main() -> void {
    box <- Box<int>($1)
}
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(9, 4)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        var boxClass = items.FirstOrDefault(i => i.Label == "Box");
        Assert.NotNull(boxClass);
        Assert.Equal(CompletionItemKind.Class, boxClass.Kind);

        output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task MultipleTypeParameters_ShouldComplete()
    {
        var code = @"class Pair<K, V> {
    private key:K
    private value:V

    func init(k:K, v:V) {
        this.key <- k
        this.value <- v
    }

    func getKey() -> K {
        return this.key
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
            Position = new Position(4, 8)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        Assert.Contains(items, i => i.Label == "this");
        Assert.Contains(items, i => i.Label == "key");
        Assert.Contains(items, i => i.Label == "value");

        output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task GenericConstraint_ColonSyntax_ShouldComplete()
    {
        var code = @"interface IComparable {
    func compareTo(other: this) -> int
}

func sort<T: IComparable>(items: list) -> list {
    return items
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
        Assert.Contains(items, i => i.Label == "items");

        output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task GenericConstraint_WhereSyntax_ShouldComplete()
    {
        var code = @"func process<T>(items: list) -> list where T: IComparable {
    return items
}

func main() -> void {
    result <- process<int>({1, 2, 3})
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
        var itemsVar = items.FirstOrDefault(i => i.Label == "items");
        Assert.NotNull(itemsVar);

        output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task NullableTypeParameter_ShouldComplete()
    {
        var code = @"class Box<T?> {
    private value:T?

    func init(v: T?) {
        this.value <- v
    }

    func getValue() -> T? {
        return this.value
    }
}

func main() -> void {
    box <- new Box(123)
    box2 <- new Box(null)
}
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(13, 4)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        Assert.Contains(items, i => i.Label == "box");
        Assert.Contains(items, i => i.Label == "box2");

        output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task GenericMemberAccess_ShouldComplete()
    {
        var code = """
                   class Stack<T> {
                       private items:list

                       func init() {
                           this.items <- {}
                       }

                       func push(item:T) -> void {
                           this.items <- this.items.Add(item)
                       }

                       func pop() -> T {
                           lastIndex <- this.items.Count() -1
                           item <- this.items[lastIndex]
                           this.items.RemoveAt(lastIndex)
                           return item
                       }

                       func peek() -> T {
                           return this.items[-1]
                       }
                   }

                   func main() -> void {
                       stack <- Stack<string>()
                       stack.push("first")
                       stack.push("second")
                       value <- stack.peek()
                   }

                   """;
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(29, 17)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        Assert.Contains(items, i => i.Label == "push");
        Assert.Contains(items, i => i.Label == "pop");
        Assert.Contains(items, i => i.Label == "peek");
        Assert.All(items, item => Assert.Equal(CompletionItemKind.Method, item.Kind));

        output.WriteLine($"Found {items.Count} member items:");
        foreach (var item in items)
        {
            output.WriteLine($"  - {item.Label}: {item.Detail}");
        }
    }

    [Fact]
    public async Task GenericInheritance_ShouldComplete()
    {
        var code = """
                   class List<T> {
                       private items: array<T>
                   }

                   class SortedList<T> extends List<T> {
                       func sort() -> void {
                       }
                   }

                   func main() -> void {
                       list <- SortedList<int>()
                   }

                   """;
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(11, 4)
        };

        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        var items = result.Items.ToList();
        Assert.Contains(items, i => i.Label == "list");
        Assert.Contains(items, i => i.Label == "List");
        Assert.Contains(items, i => i.Label == "SortedList");

        output.WriteLine($"Found {items.Count} items");
    }

    [Fact]
    public async Task AllGenericKeywords_ShouldBeAvailable()
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
        var basicTypes = new[] { "int", "double", "string", "bool", "char", "void", "var", "any" };

        foreach (var type in basicTypes)
        {
            var typeItem = items.FirstOrDefault(i => i.Label == type);
            if (typeItem == null)
            {
                output.WriteLine($"Type '{type}' not found in completions");
            }
            Assert.True(typeItem != null, $"Type '{type}' should be available");
            Assert.Equal(CompletionItemKind.Keyword, typeItem.Kind);
        }

        output.WriteLine($"Found {items.Count} items");
        output.WriteLine("All basic types verified");
    }
}
