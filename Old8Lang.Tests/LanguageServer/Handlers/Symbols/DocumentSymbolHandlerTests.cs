using Old8Lang.LanguageServer.Services;
using Old8Lang.LanguageServer.Handlers;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit.Abstractions;

namespace Old8Lang.Tests.LanguageServer;

/// <summary>
/// 测试 DocumentSymbolHandler - 文档大纲视图功能
/// </summary>
public class DocumentSymbolHandlerTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task TestDocumentSymbols_WithFunctions()
    {
        // Arrange
        var code = @"
func add(a:int, b:int) -> int {
    return a + b
}

func subtract(x:int, y:int) -> int {
    return x - y
}
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new DocumentSymbolHandler(documentManager);
        var request = new DocumentSymbolParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) }
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        var symbols = result.Where(s => s.IsDocumentSymbol).Select(s => s.DocumentSymbol!).ToList();
        testOutputHelper.WriteLine($"Found {symbols.Count} symbols");

        foreach (var symbol in symbols)
        {
            testOutputHelper.WriteLine($"Symbol: {symbol.Name} ({symbol.Kind})");
        }

        Assert.Contains(symbols, s => s.Name == "add" && s.Kind == SymbolKind.Function);
        Assert.Contains(symbols, s => s.Name == "subtract" && s.Kind == SymbolKind.Function);
    }

    [Fact]
    public async Task TestDocumentSymbols_WithClasses()
    {
        // Arrange
        var code = @"
class Person {
    public name:string
    public age:int

    public func getName() -> string {
        return this.name
    }
}

class Animal {
    private species:string

    public func getSpecies() -> string {
        return this.species
    }
}
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new DocumentSymbolHandler(documentManager);
        var request = new DocumentSymbolParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) }
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        var symbols = result.Where(s => s.IsDocumentSymbol).Select(s => s.DocumentSymbol!).ToList();
        testOutputHelper.WriteLine($"Found {symbols.Count} top-level symbols");

        foreach (var symbol in symbols)
        {
            testOutputHelper.WriteLine($"Class: {symbol.Name} ({symbol.Kind})");
            if (symbol.Children != null)
            {
                foreach (var child in symbol.Children)
                {
                    testOutputHelper.WriteLine($"  Member: {child.Name} ({child.Kind})");
                }
            }
        }

        // 验证类
        var personClass = symbols.FirstOrDefault(s => s.Name == "Person");
        Assert.NotNull(personClass);
        Assert.Equal(SymbolKind.Class, personClass.Kind);

        var animalClass = symbols.FirstOrDefault(s => s.Name == "Animal");
        Assert.NotNull(animalClass);
        Assert.Equal(SymbolKind.Class, animalClass.Kind);

        // 验证 Person 类的成员
        if (personClass.Children != null)
        {
            var members = personClass.Children.ToList();
            testOutputHelper.WriteLine($"Person class has {members.Count} members");

            // 应该包含属性和方法
            Assert.Contains(members, m => m.Name == "name");
            Assert.Contains(members, m => m.Name == "age");
            Assert.Contains(members, m => m.Name == "getName");
        }
    }

    [Fact]
    public async Task TestDocumentSymbols_WithVariables()
    {
        // Arrange
        var code = @"
a:int <- 123
b:string <- ""hello""
c <- 3.14
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new DocumentSymbolHandler(documentManager);
        var request = new DocumentSymbolParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) }
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        var symbols = result.Where(s => s.IsDocumentSymbol).Select(s => s.DocumentSymbol!).ToList();
        testOutputHelper.WriteLine($"Found {symbols.Count} variable symbols");

        foreach (var symbol in symbols)
        {
            testOutputHelper.WriteLine($"Variable: {symbol.Name} ({symbol.Detail})");
        }

        Assert.Contains(symbols, s => s.Name == "a" && s.Kind == SymbolKind.Variable);
        Assert.Contains(symbols, s => s.Name == "b" && s.Kind == SymbolKind.Variable);
        Assert.Contains(symbols, s => s.Name == "c" && s.Kind == SymbolKind.Variable);
    }

    [Fact]
    public async Task TestDocumentSymbols_MixedSymbols()
    {
        // Arrange
        var code = @"
PI <- 3.14159

func calculate(x:int) -> double {
    return x * PI
}

class Calculator {
    public func add(a:int, b:int) -> int {
        return a + b
    }
}
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new DocumentSymbolHandler(documentManager);
        var request = new DocumentSymbolParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) }
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        var symbols = result.Where(s => s.IsDocumentSymbol).Select(s => s.DocumentSymbol!).ToList();
        testOutputHelper.WriteLine($"Found {symbols.Count} mixed symbols");

        foreach (var symbol in symbols)
        {
            testOutputHelper.WriteLine($"Symbol: {symbol.Name} (Kind: {symbol.Kind}, Detail: {symbol.Detail})");
        }

        // 应该包含变量、函数和类
        Assert.Contains(symbols, s => s.Name == "PI" && s.Kind == SymbolKind.Variable);
        Assert.Contains(symbols, s => s.Name == "calculate" && s.Kind == SymbolKind.Function);
        Assert.Contains(symbols, s => s.Name == "Calculator" && s.Kind == SymbolKind.Class);
    }

    [Fact]
    public async Task TestDocumentSymbols_EmptyDocument()
    {
        // Arrange
        var code = "";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new DocumentSymbolHandler(documentManager);
        var request = new DocumentSymbolParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) }
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        testOutputHelper.WriteLine("Empty document returns no symbols");
    }

    [Fact]
    public async Task TestDocumentSymbols_OnlyComments()
    {
        // Arrange
        var code = @"
// 这是一个注释
// 另一个注释
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new DocumentSymbolHandler(documentManager);
        var request = new DocumentSymbolParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) }
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        testOutputHelper.WriteLine($"Document with only comments has {result.Count()} symbols");

        // 注释不应该生成符号
        Assert.Empty(result);
    }

    [Fact]
    public async Task TestSymbolRanges()
    {
        // Arrange
        var code = @"
func test() -> void {
    a <- 1
}
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new DocumentSymbolHandler(documentManager);
        var request = new DocumentSymbolParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) }
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        var symbols = result.Where(s => s.IsDocumentSymbol).Select(s => s.DocumentSymbol!).ToList();
        var testFunc = symbols.FirstOrDefault(s => s.Name == "test");

        Assert.NotNull(testFunc);
        Assert.NotNull(testFunc.Range);
        Assert.NotNull(testFunc.SelectionRange);

        testOutputHelper.WriteLine($"Function range: {testFunc.Range}");
        testOutputHelper.WriteLine($"Selection range: {testFunc.SelectionRange}");

        // Range 应该包含整个函数
        Assert.True(testFunc.Range.Start.Line >= 0);
        Assert.True(testFunc.Range.End.Line >= testFunc.Range.Start.Line);
    }

    [Fact]
    public async Task TestNestedMembers()
    {
        // Arrange
        var code = @"
class OuterClass {
    public x:int

    public func method1() -> void {
        // 方法体
    }

    public static func method2() -> void {
        // 静态方法
    }
}
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new DocumentSymbolHandler(documentManager);
        var request = new DocumentSymbolParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) }
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        var symbols = result.Where(s => s.IsDocumentSymbol).Select(s => s.DocumentSymbol!).ToList();
        var outerClass = symbols.FirstOrDefault(s => s.Name == "OuterClass");

        Assert.NotNull(outerClass);
        Assert.NotNull(outerClass.Children);

        testOutputHelper.WriteLine($"OuterClass has {outerClass.Children.Count()} children");

        foreach (var child in outerClass.Children)
        {
            testOutputHelper.WriteLine($"  Child: {child.Name} ({child.Kind})");
        }

        // 应该包含属性和方法
        Assert.Contains(outerClass.Children, m => m.Name == "x");
        Assert.Contains(outerClass.Children, m => m.Name == "method1");
        Assert.Contains(outerClass.Children, m => m.Name == "method2");
    }

    [Fact]
    public async Task TestRegistrationOptions()
    {
        // Arrange
        var documentManager = new DocumentManager();
        var handler = new DocumentSymbolHandler(documentManager);

        // Act
        var options = handler.GetRegistrationOptions(
            new DocumentSymbolCapability(),
            new ClientCapabilities()
        );

        // Assert
        Assert.NotNull(options);
        Assert.NotNull(options.DocumentSelector);

        testOutputHelper.WriteLine($"Document selector: {options.DocumentSelector}");
    }
}
