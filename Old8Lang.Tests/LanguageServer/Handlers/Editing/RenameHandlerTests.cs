using Old8Lang.LanguageServer.Services;
using Old8Lang.LanguageServer.Handlers;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit.Abstractions;

namespace Old8Lang.Tests.LanguageServer;

public class RenameHandlerTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task TestRename_Function()
    {
        // Arrange
        var code = @"
func testFunction() -> int {
    return 42
}

result1 <- testFunction()
result2 <- testFunction()
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new RenameHandler(documentManager);

        // 光标在函数定义的 testFunction 上
        var request = new RenameParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(1, 5), // testFunction 定义位置的第一个字符
            NewName = "newFunctionName"
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Changes);
        Assert.True(result.Changes.ContainsKey(new Uri(uri)));
        
        var textEdits = result.Changes[new Uri(uri)].ToList();
        Assert.Equal(3, textEdits.Count); // 1个定义 + 2个引用

        // 验证所有编辑都将 testFunction 替换为 newFunctionName
        foreach (var edit in textEdits)
        {
            Assert.Equal("newFunctionName", edit.NewText);
        }

        // 验证编辑位置覆盖了所有的 testFunction
        var positions = textEdits.Select(e => (e.Range.Start.Line, e.Range.Start.Character)).OrderBy(p => p.Line).ThenBy(p => p.Character).ToList();
        Assert.Contains((1, 5), positions); // 定义位置
        Assert.Contains((5, 11), positions); // 第一个调用
        Assert.Contains((6, 11), positions); // 第二个调用
    }

    [Fact]
    public async Task TestRename_Variable()
    {
        // Arrange
        var code = @"
myVariable <- 42
result1 <- myVariable + 1
result2 <- myVariable * 2
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new RenameHandler(documentManager);

        // 光标在变量定义的 myVariable 上
        var request = new RenameParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(2, 11), // myVariable 定义位置
            NewName = "newVariableName"
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Changes);
        Assert.True(result.Changes.ContainsKey(new Uri(uri)));
        
        var textEdits = result.Changes[new Uri(uri)].ToList();
        Assert.Equal(3, textEdits.Count); // 1个定义 + 2个引用

        // 验证所有编辑都将 myVariable 替换为 newVariableName
        foreach (var edit in textEdits)
        {
            Assert.Equal("newVariableName", edit.NewText);
        }
    }

    [Fact]
    public async Task TestRename_Class()
    {
        // Arrange
        var code = @"
class TestClass {
    public name:string
}

instance1 <- TestClass()
instance2 <- TestClass()
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new RenameHandler(documentManager);

        // 光标在类定义的 TestClass 上
        var request = new RenameParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(1, 6), // TestClass 定义位置
            NewName = "NewClassName"
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Changes);
        Assert.True(result.Changes.ContainsKey(new Uri(uri)));
        
        var textEdits = result.Changes[new Uri(uri)].ToList();
        Assert.Equal(3, textEdits.Count); // 1个定义 + 2个实例化

        // 验证所有编辑都将 TestClass 替换为 NewClassName
        foreach (var edit in textEdits)
        {
            Assert.Equal("NewClassName", edit.NewText);
        }
    }

    [Fact]
    public async Task TestRename_ClassMember()
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
result1 <- user.name
result2 <- user.getName()
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        var document = documentManager.UpdateDocument(uri, code);

        // Debug info
        testOutputHelper.WriteLine($"SymbolTable count: {document.SymbolTable?.Count ?? 0}");

        var handler = new RenameHandler(documentManager);

        // 光标在 name 属性的引用上
        var request = new RenameParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(10, 16), // user.name 中的 name  (Line 10, 'name' starts at column 16)
            NewName = "fullName"
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Changes);
        Assert.True(result.Changes.ContainsKey(new Uri(uri)));
        
        var textEdits = result.Changes[new Uri(uri)].ToList();
        Assert.Equal(3, textEdits.Count); // 1个定义 + 2个引用 (this.name 和 user.name)

        // 验证所有编辑都将 name 替换为 fullName
        foreach (var edit in textEdits)
        {
            Assert.Equal("fullName", edit.NewText);
        }
    }

    [Fact]
    public async Task TestRename_Method()
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
result <- user.getName()
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new RenameHandler(documentManager);

        // 光标在 getName 方法的调用上
        var request = new RenameParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(10, 15), // user.getName() 中的 getName (Line 10, 'getName' starts at column 15)
            NewName = "getFullName"
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Changes);
        Assert.True(result.Changes.ContainsKey(new Uri(uri)));
        
        var textEdits = result.Changes[new Uri(uri)].ToList();
        Assert.Equal(2, textEdits.Count); // 1个定义 + 1个引用

        // 验证所有编辑都将 getName 替换为 getFullName
        foreach (var edit in textEdits)
        {
            Assert.Equal("getFullName", edit.NewText);
        }
    }

    [Fact]
    public async Task TestRename_NotFound()
    {
        // Arrange
        var code = @"
result <- undefinedVar + 10
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new RenameHandler(documentManager);

        // 光标在未定义的变量上
        var request = new RenameParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(1, 9), // undefinedVar 位置
            NewName = "newName"
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.Null(result); // 未找到符号时返回 null
    }

    [Fact]
    public async Task TestRename_EmptyDocument()
    {
        // Arrange
        var code = "";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new RenameHandler(documentManager);

        var request = new RenameParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(0, 0),
            NewName = "newName"
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task TestRename_SameName()
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

        var handler = new RenameHandler(documentManager);

        // 尝试重命名为相同的名称
        var request = new RenameParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(1, 5), // testFunction 定义位置
            NewName = "testFunction" // 相同的名称
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Changes);
        Assert.True(result.Changes.ContainsKey(new Uri(uri)));
        
        var textEdits = result.Changes[new Uri(uri)].ToList();
        Assert.Equal(2, textEdits.Count); // 仍然会返回编辑，但实际上内容不变

        // 验证所有编辑的 NewText 都是新名称（即使相同）
        foreach (var edit in textEdits)
        {
            Assert.Equal("testFunction", edit.NewText);
        }
    }

    [Fact]
    public async Task TestRename_InvalidPosition()
    {
        // Arrange
        var code = @"
func testFunction() -> int {
    return 42
}
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new RenameHandler(documentManager);

        // 光标位置在空格或标点符号上
        var request = new RenameParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(1, 4), // func 后面的空格位置
            NewName = "newName"
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.Null(result); // 没有找到符号时返回 null
    }

    [Fact]
    public async Task TestRename_BuiltInFunction()
    {
        // Arrange
        var code = @"
PrintLine(""Hello"")
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new RenameHandler(documentManager);

        // 光标在内置函数 PrintLine 上
        var request = new RenameParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(1, 0), // PrintLine 位置
            NewName = "CustomPrint"
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.Null(result); // 内置函数不在符号表中，无法重命名
    }
}