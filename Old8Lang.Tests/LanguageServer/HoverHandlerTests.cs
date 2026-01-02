using Xunit;
using Old8Lang.LangParser;
using Old8Lang.LanguageServer.Services;
using Old8Lang.LanguageServer.Models;
using Old8Lang.LanguageServer.Handlers;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Threading;
using Xunit.Abstractions;

namespace Old8Lang.Tests.LanguageServer;

public class HoverHandlerTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task TestHover_Function()
    {
        // Arrange
        var code = @"
func add(a:int, b:int) -> int {
    return a + b
}

result <- add(1, 2) // 光标在 add 上
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new HoverHandler(documentManager);

        var request = new HoverParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(5, 9) // add 函数调用位置
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Contents);
        
        var markupContent = result.Contents.MarkupContent;
        Assert.NotNull(markupContent);
        Assert.Equal(MarkupKind.Markdown, markupContent.Kind);
        Assert.Contains("func add", markupContent.Value);
        Assert.Contains("-> int", markupContent.Value);
    }

    [Fact]
    public async Task TestHover_Class()
    {
        // Arrange
        var code = @"
class User {
    public name:string
    
    func getName() -> string {
        return this.name
    }
}

user <- User() // 光标在 User 上
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new HoverHandler(documentManager);

        var request = new HoverParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(11, 9) // User 类实例化位置
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Contents);
        
        var markupContent = result.Contents.MarkupContent;
        Assert.NotNull(markupContent);
        Assert.Equal(MarkupKind.Markdown, markupContent.Kind);
        Assert.Contains("class User", markupContent.Value);
    }

    [Fact]
    public async Task TestHover_Variable()
    {
        // Arrange
        var code = @"
myVariable <- 42
result <- myVariable + 10 // 光标在 myVariable 上
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new HoverHandler(documentManager);

        var request = new HoverParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(2, 9) // myVariable 使用位置
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Contents);
        
        var markupContent = result.Contents.MarkupContent;
        Assert.NotNull(markupContent);
        Assert.Equal(MarkupKind.Markdown, markupContent.Kind);
        Assert.Contains("myVariable", markupContent.Value);
    }

    [Fact]
    public async Task TestHover_ClassMember()
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

        var handler = new HoverHandler(documentManager);

        var request = new HoverParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(12, 15) // getName 方法调用位置
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Contents);
        
        var markupContent = result.Contents.MarkupContent;
        Assert.NotNull(markupContent);
        Assert.Equal(MarkupKind.Markdown, markupContent.Kind);
        Assert.Contains("getName", markupContent.Value);
        Assert.Contains("User", markupContent.Value); // 应该显示所属类
    }

    [Fact]
    public async Task TestHover_WithDocumentation()
    {
        // Arrange
        var code = @"
/// 这是一个加法函数
/// 计算两个整数的和
func calculate(a:int, b:int) -> int {
    return a + b
}

result <- calculate(1, 2) // 光标在 calculate 上
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new HoverHandler(documentManager);

        var request = new HoverParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(9, 9) // calculate 函数调用位置
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Contents);
        
        var markupContent = result.Contents.MarkupContent;
        Assert.NotNull(markupContent);
        Assert.Equal(MarkupKind.Markdown, markupContent.Kind);
        
        // Should contain both function signature and documentation
        Assert.Contains("calculate", markupContent.Value);
        Assert.Contains("加法函数", markupContent.Value);
        Assert.Contains("-> int", markupContent.Value);
    }

    [Fact]
    public async Task TestHover_StaticMethod()
    {
        // Arrange
        var code = @"
class MathUtil {
    static func add(a:int, b:int) -> int {
        return a + b
    }
}

result <- MathUtil.add(1, 2) // 光标在 add 上
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new HoverHandler(documentManager);

        var request = new HoverParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(8, 19) // add 方法调用位置
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Contents);
        
        var markupContent = result.Contents.MarkupContent;
        Assert.NotNull(markupContent);
        Assert.Equal(MarkupKind.Markdown, markupContent.Kind);
        Assert.Contains("static", markupContent.Value);
        Assert.Contains("add", markupContent.Value);
    }

    [Fact]
    public async Task TestHover_PrivateMethod()
    {
        // Arrange
        var code = @"
class User {
    private name:string
    
    private func getName() -> string {
        return this.name
    }
}
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new HoverHandler(documentManager);

        // Try hovering on private method name (in definition)
        var request = new HoverParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(4, 12) // getName 定义位置
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Contents);
        
        var markupContent = result.Contents.MarkupContent;
        Assert.NotNull(markupContent);
        Assert.Equal(MarkupKind.Markdown, markupContent.Kind);
        Assert.Contains("private", markupContent.Value);
        Assert.Contains("getName", markupContent.Value);
    }

    [Fact]
    public async Task TestHover_NotFound()
    {
        // Arrange
        var code = @"
result <- undefinedVar + 10 // 光标在未定义的变量上
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new HoverHandler(documentManager);

        var request = new HoverParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(1, 9) // undefinedVar 位置
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.Null(result); // 未找到符号时不显示悬停信息
    }

    [Fact]
    public async Task TestHover_BuiltInFunction()
    {
        // Arrange
        var code = @"
PrintLine(""Hello"") // 光标在 PrintLine 上
";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new HoverHandler(documentManager);

        var request = new HoverParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(1, 0) // PrintLine 开始位置
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.Null(result); // 内置函数没有悬停信息
    }

    [Fact]
    public async Task TestHover_EmptyDocument()
    {
        // Arrange
        var code = "";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new HoverHandler(documentManager);

        var request = new HoverParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(0, 0)
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task TestHover_PositionInformation()
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

        var handler = new HoverHandler(documentManager);

        var request = new HoverParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(1, 5) // testFunction 定义位置
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Contents);
        
        var markupContent = result.Contents.MarkupContent;
        Assert.NotNull(markupContent);
        Assert.Contains("testFunction", markupContent.Value);
        Assert.Contains(uri, markupContent.Value); // Should contain URI
        Assert.Contains("2", markupContent.Value); // Should contain line number (0-based + 1)
    }
}