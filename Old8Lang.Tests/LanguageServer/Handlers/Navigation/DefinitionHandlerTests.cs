using Old8Lang.LanguageServer.Handlers;
using Old8Lang.LanguageServer.Services;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit.Abstractions;

namespace Old8Lang.Tests.LanguageServer.Handlers.Navigation;

public class DefinitionHandlerTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task TestGoToDefinition_Function()
    {
        // Arrange
        var code = @"
func testFunction(a:int, b:int) -> int {
    return a + b
}

result <- testFunction(1, 2)
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        var document = documentManager.UpdateDocument(uri, code);

        var handler = new DefinitionHandler(documentManager);

        // 光标位置在 testFunction 上
        var request = new DefinitionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(5, 10) // testFunction 开始位置
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        var location = result.First().Location!;
        Assert.Equal(uri, location.Uri.ToString());
        Assert.Equal(1, location.Range.Start.Line); // func 定义在第2行（0-based）
        Assert.Equal(5, location.Range.Start.Character); // func 关键字后面
    }

    [Fact]
    public async Task TestGoToDefinition_Class()
    {
        // Arrange
        var code = @"
class TestClass {
    public name:string
}

instance <- TestClass() // 光标在 TestClass 上
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new DefinitionHandler(documentManager);

        var request = new DefinitionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(5, 12) // TestClass 开始位置
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        
        var location = result.First().Location!;
        Assert.Equal(uri, location.Uri.ToString());
        Assert.Equal(1, location.Range.Start.Line); // class 定义在第2行（0-based）
    }

    [Fact]
    public async Task TestGoToDefinition_Variable()
    {
        // Arrange
        var code = @"
myVariable <- 42
result <- myVariable + 10 // 光标在 myVariable 上
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new DefinitionHandler(documentManager);

        var request = new DefinitionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(2, 10) // myVariable 开始位置
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        
        var location = result.First().Location!;
        Assert.Equal(uri, location.Uri.ToString());
        Assert.Equal(1, location.Range.Start.Line); // 变量定义在第2行（0-based）
    }

    [Fact]
    public async Task TestGoToDefinition_ClassMember()
    {
        // Arrange
        var code = @"
class User {
    public name:string
    
    func getName() -> string {
        return this.name
    }
}

user <- User()
result <- user.getName() // 光标在 getName 上
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        var document = documentManager.UpdateDocument(uri, code);

        // Debug info
        testOutputHelper.WriteLine($"SymbolTable count: {document.SymbolTable?.Count ?? 0}");
        if (document.SymbolTable != null)
        {
            foreach (var (name, symbol) in document.SymbolTable)
            {
                testOutputHelper.WriteLine($"  {name}: {symbol.Kind}");
                if (symbol.Members.Count > 0)
                {
                    foreach (var (memberName, member) in symbol.Members)
                    {
                        testOutputHelper.WriteLine($"    - {memberName}: {member.Kind}");
                    }
                }
            }
        }

        var handler = new DefinitionHandler(documentManager);

        var request = new DefinitionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(10, 15) // getName 开始位置
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        
        var location = result.First().Location!;
        Assert.Equal(uri, location.Uri.ToString());
        Assert.Equal(4, location.Range.Start.Line); // getName 方法定义在第5行（0-based）
    }

    [Fact]
    public async Task TestGoToDefinition_NotFound()
    {
        // Arrange
        var code = @"
result <- undefinedVar + 10 // 光标在未定义的变量上
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new DefinitionHandler(documentManager);

        var request = new DefinitionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(1, 9) // undefinedVar 开始位置
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result); // 没有找到定义
    }

    [Fact]
    public async Task TestGoToDefinition_BuiltInFunction()
    {
        // Arrange
        var code = @"
PrintLine(""Hello"") // 光标在 PrintLine 上
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new DefinitionHandler(documentManager);

        var request = new DefinitionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(1, 0) // PrintLine 开始位置
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result); // 内置函数没有定义位置
    }

    [Fact]
    public async Task TestGoToDefinition_EmptyDocument()
    {
        // Arrange
        var code = "";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new DefinitionHandler(documentManager);

        var request = new DefinitionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(0, 0)
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
}

/// <summary>
/// 查找引用处理器测试
/// </summary>
public class ReferencesHandlerTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task TestFindReferences_Function()
    {
        // Arrange
        var code = @"
func testFunction() -> int {
    return 42
}

result1 <- testFunction()
result2 <- testFunction()
result3 <- testFunction()
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new ReferencesHandler(documentManager);

        // 光标在函数定义的 testFunction 上
        var request = new ReferenceParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(1, 5), // testFunction 定义位置
            Context = new ReferenceContext { IncludeDeclaration = true }
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.Count()); // 1个定义 + 3个引用

        // 验证所有位置都包含 testFunction
        foreach (var location in result)
        {
            Assert.Equal(uri, location.Uri.ToString());
        }
    }

    [Fact]
    public async Task TestFindReferences_ExcludeDeclaration()
    {
        // Arrange
        var code = @"
func testFunction() -> int {
    return 42
}

result <- testFunction()
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new ReferencesHandler(documentManager);

        var request = new ReferenceParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(1, 5), // testFunction 定义位置
            Context = new ReferenceContext { IncludeDeclaration = false }
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result); // 只有1个引用，不包含定义
    }

    [Fact]
    public async Task TestFindReferences_Variable()
    {
        // Arrange
        var code = @"
myVar <- 42
result1 <- myVar + 1
result2 <- myVar * 2
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new ReferencesHandler(documentManager);

        var request = new ReferenceParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(1, 0), // myVar 定义位置
            Context = new ReferenceContext { IncludeDeclaration = true }
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count()); // 1个定义 + 2个引用
    }

    [Fact]
    public async Task TestFindReferences_NotFound()
    {
        // Arrange
        var code = @"
result <- undefinedVar + 1
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new ReferencesHandler(documentManager);

        var request = new ReferenceParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(1, 9), // undefinedVar 位置
            Context = new ReferenceContext { IncludeDeclaration = true }
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result); // 未定义的符号没有引用
    }
}