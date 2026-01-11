using Old8Lang.LanguageServer.Handlers;
using Old8Lang.LanguageServer.Services;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit.Abstractions;

namespace Old8Lang.Tests.LanguageServer.Handlers.Symbols;

/// <summary>
/// 测试 WorkspaceSymbolHandler - 工作区符号搜索功能
/// </summary>
public class WorkspaceSymbolHandlerTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task TestWorkspaceSymbols_WithQuery()
    {
        // Arrange
        var documentManager = new DocumentManager();

        // 添加多个文档
        var file1 = "file:///test1.old8";
        var code1 = @"
func calculateSum(a:int, b:int) -> int {
    return a + b
}

class Calculator {
    public func calculate() -> void {
    }
}
";
        documentManager.UpdateDocument(file1, code1);

        var file2 = "file:///test2.old8";
        var code2 = @"
func calculateProduct(x:int, y:int) -> int {
    return x * y
}
";
        documentManager.UpdateDocument(file2, code2);

        var handler = new WorkspaceSymbolHandler(documentManager);
        var request = new WorkspaceSymbolParams
        {
            Query = "calculate" // 搜索包含 "calculate" 的符号
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var symbols = result.ToList();

        testOutputHelper.WriteLine($"Found {symbols.Count} symbols matching 'calculate'");

        foreach (var symbol in symbols)
        {
            testOutputHelper.WriteLine($"Symbol: {symbol.Name} ({symbol.Kind})");
        }

        // 应该找到 calculateSum, calculateProduct, Calculator, calculate 方法
        Assert.Contains(symbols, s => s.Name == "calculateSum");
        Assert.Contains(symbols, s => s.Name == "calculateProduct");
        Assert.Contains(symbols, s => s.Name == "Calculator");
        Assert.Contains(symbols, s => s.Name == "calculate");

        // 至少应该有 4 个符号
        Assert.True(symbols.Count >= 4);
    }

    [Fact]
    public async Task TestWorkspaceSymbols_CaseInsensitive()
    {
        // Arrange
        var documentManager = new DocumentManager();

        var uri = "file:///test.old8";
        var code = @"
func MyFunction() -> void {
}

class MyClass {
}
";
        documentManager.UpdateDocument(uri, code);

        var handler = new WorkspaceSymbolHandler(documentManager);

        // 使用小写查询
        var request = new WorkspaceSymbolParams
        {
            Query = "my"
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var symbols = result.ToList();

        testOutputHelper.WriteLine($"Found {symbols.Count} symbols matching 'my' (case insensitive)");

        foreach (var symbol in symbols)
        {
            testOutputHelper.WriteLine($"Symbol: {symbol.Name}");
        }

        // 不区分大小写，应该找到 MyFunction 和 MyClass
        Assert.Contains(symbols, s => s.Name == "MyFunction");
        Assert.Contains(symbols, s => s.Name == "MyClass");
    }

    [Fact]
    public async Task TestWorkspaceSymbols_EmptyQuery()
    {
        // Arrange
        var documentManager = new DocumentManager();

        var uri = "file:///test.old8";
        var code = @"
func func1() -> void {}
func func2() -> void {}
func func3() -> void {}
";
        documentManager.UpdateDocument(uri, code);

        var handler = new WorkspaceSymbolHandler(documentManager);
        var request = new WorkspaceSymbolParams
        {
            Query = "" // 空查询
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var symbols = result.ToList();

        testOutputHelper.WriteLine($"Found {symbols.Count} symbols with empty query");

        // 空查询应该返回所有符号（最多 100 个）
        Assert.NotEmpty(symbols);
        Assert.True(symbols.Count <= 100, "Should limit to 100 symbols without query");
    }

    [Fact]
    public async Task TestWorkspaceSymbols_MultipleDocuments()
    {
        // Arrange
        var documentManager = new DocumentManager();

        // 文档 1
        var file1 = "file:///file1.old8";
        documentManager.UpdateDocument(file1, "func funcA() -> void {}");

        // 文档 2
        var file2 = "file:///file2.old8";
        documentManager.UpdateDocument(file2, "func funcB() -> void {}");

        // 文档 3
        var file3 = "file:///file3.old8";
        documentManager.UpdateDocument(file3, "class ClassC {}");

        var handler = new WorkspaceSymbolHandler(documentManager);
        var request = new WorkspaceSymbolParams
        {
            Query = "" // 返回所有符号
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var symbols = result.ToList();

        testOutputHelper.WriteLine($"Found {symbols.Count} symbols across multiple documents");

        foreach (var symbol in symbols)
        {
            testOutputHelper.WriteLine($"Symbol: {symbol.Name} in {symbol.Location}");
        }

        // 应该从不同的文档中找到符号
        var uris = symbols.Select(s => s.Location).Distinct().ToList();
        testOutputHelper.WriteLine($"Symbols found in {uris.Count} different files");

        Assert.Contains(symbols, s => s.Name == "funcA");
        Assert.Contains(symbols, s => s.Name == "funcB");
        Assert.Contains(symbols, s => s.Name == "ClassC");
    }

    [Fact]
    public async Task TestWorkspaceSymbols_IncludesClassMembers()
    {
        // Arrange
        var documentManager = new DocumentManager();

        var uri = "file:///test.old8";
        var code = @"
class Person {
    public name:string
    public age:int

    public func getName() -> string {
        return this.name
    }

    public func getAge() -> int {
        return this.age
    }
}
";
        documentManager.UpdateDocument(uri, code);

        var handler = new WorkspaceSymbolHandler(documentManager);
        var request = new WorkspaceSymbolParams
        {
            Query = "" // 返回所有符号
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var symbols = result.ToList();

        testOutputHelper.WriteLine($"Found {symbols.Count} symbols including class members");

        foreach (var symbol in symbols)
        {
            testOutputHelper.WriteLine($"Symbol: {symbol.Name} ({symbol.Kind}), Container: {symbol.ContainerName}");
        }

        // 应该包含类和其成员
        Assert.Contains(symbols, s => s.Name == "Person" && s.Kind == SymbolKind.Class);
        Assert.Contains(symbols, s => s.Name == "name" && s.ContainerName == "Person");
        Assert.Contains(symbols, s => s.Name == "age" && s.ContainerName == "Person");
        Assert.Contains(symbols, s => s.Name == "getName" && s.ContainerName == "Person");
        Assert.Contains(symbols, s => s.Name == "getAge" && s.ContainerName == "Person");
    }

    [Fact]
    public async Task TestWorkspaceSymbols_FiltersByQuery()
    {
        // Arrange
        var documentManager = new DocumentManager();

        var uri = "file:///test.old8";
        var code = @"
func add(a:int, b:int) -> int {
    return a + b
}

func subtract(x:int, y:int) -> int {
    return x - y
}

func addMany(values) -> int {
    sum <- 0
    return sum
}
";
        documentManager.UpdateDocument(uri, code);

        var handler = new WorkspaceSymbolHandler(documentManager);
        var request = new WorkspaceSymbolParams
        {
            Query = "add" // 只搜索包含 "add" 的符号
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var symbols = result.ToList();

        testOutputHelper.WriteLine($"Found {symbols.Count} symbols matching 'add'");

        foreach (var symbol in symbols)
        {
            testOutputHelper.WriteLine($"Symbol: {symbol.Name}");
        }

        // 应该只包含 add 和 addMany，不包含 subtract
        Assert.Contains(symbols, s => s.Name == "add");
        Assert.Contains(symbols, s => s.Name == "addMany");
        Assert.DoesNotContain(symbols, s => s.Name == "subtract");
    }

    [Fact]
    public async Task TestWorkspaceSymbols_EmptyWorkspace()
    {
        // Arrange
        var documentManager = new DocumentManager();
        // 不添加任何文档

        var handler = new WorkspaceSymbolHandler(documentManager);
        var request = new WorkspaceSymbolParams
        {
            Query = "test"
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);

        testOutputHelper.WriteLine("Empty workspace returns no symbols");
    }

    [Fact]
    public async Task TestWorkspaceSymbols_SymbolKinds()
    {
        // Arrange
        var documentManager = new DocumentManager();

        var uri = "file:///test.old8";
        var code = @"
PI <- 3.14159

func calculate() -> void {
}

class MyClass {
    public prop:int

    public func method() -> void {
    }
}
";
        documentManager.UpdateDocument(uri, code);

        var handler = new WorkspaceSymbolHandler(documentManager);
        var request = new WorkspaceSymbolParams
        {
            Query = ""
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var symbols = result.ToList();

        testOutputHelper.WriteLine($"Found {symbols.Count} symbols");

        foreach (var symbol in symbols)
        {
            testOutputHelper.WriteLine($"Symbol: {symbol.Name} ({symbol.Kind})");
        }

        // 验证不同类型的符号
        Assert.Contains(symbols, s => s.Name == "PI" && s.Kind == SymbolKind.Variable);
        Assert.Contains(symbols, s => s.Name == "calculate" && s.Kind == SymbolKind.Function);
        Assert.Contains(symbols, s => s.Name == "MyClass" && s.Kind == SymbolKind.Class);
        Assert.Contains(symbols, s => s.Name == "prop" && s.Kind == SymbolKind.Property);
        Assert.Contains(symbols, s => s.Name == "method" && s.Kind == SymbolKind.Method);
    }

    [Fact]
    public async Task TestWorkspaceSymbols_LocationInformation()
    {
        // Arrange
        var documentManager = new DocumentManager();

        var uri = "file:///test.old8";
        var code = @"
func testFunc() -> void {
}
";
        documentManager.UpdateDocument(uri, code);

        var handler = new WorkspaceSymbolHandler(documentManager);
        var request = new WorkspaceSymbolParams
        {
            Query = "testFunc"
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var symbols = result.ToList();

        var testFunc = symbols.FirstOrDefault(s => s.Name == "testFunc");
        Assert.NotNull(testFunc);
        Assert.NotNull(testFunc.Location);

        testOutputHelper.WriteLine($"Symbol location: {testFunc.Location}");

        // Location 包含文件信息
        Assert.True(testFunc.Location != null);
    }
}
