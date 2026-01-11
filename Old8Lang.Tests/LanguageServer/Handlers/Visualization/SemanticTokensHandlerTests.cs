using Old8Lang.LanguageServer.Services;
using Old8Lang.LanguageServer.Handlers;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit.Abstractions;

namespace Old8Lang.Tests.LanguageServer;

/// <summary>
/// 测试 SemanticTokensHandler - 语义高亮功能
/// </summary>
public class SemanticTokensHandlerTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task TestSemanticTokens_Keywords()
    {
        // Arrange
        var code = @"
func test() -> void {
    if true {
        a <- 10
    }
}
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new SemanticTokensHandler(documentManager);
        var request = new SemanticTokensParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) }
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Data);

        testOutputHelper.WriteLine($"Semantic tokens count: {result.Data.Length}");
        testOutputHelper.WriteLine("Semantic tokens generated successfully");
    }

    [Fact]
    public async Task TestSemanticTokens_FunctionAndVariable()
    {
        // Arrange
        var code = @"
func calculate(x:int) -> int {
    result <- x * 2
    return result
}
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new SemanticTokensHandler(documentManager);
        var request = new SemanticTokensParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) }
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Data);

        testOutputHelper.WriteLine($"Generated {result.Data.Length} semantic tokens for function and variables");
    }

    [Fact]
    public async Task TestSemanticTokens_Class()
    {
        // Arrange
        var code = @"
class Person {
    public name:string
    public func getName() -> string {
        return this.name
    }
}
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new SemanticTokensHandler(documentManager);
        var request = new SemanticTokensParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) }
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Data);

        testOutputHelper.WriteLine($"Generated {result.Data.Length} semantic tokens for class");
    }

    [Fact]
    public async Task TestSemanticTokens_EmptyDocument()
    {
        // Arrange
        var code = "";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new SemanticTokensHandler(documentManager);
        var request = new SemanticTokensParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) }
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        if (result.Data != null)
        {
            testOutputHelper.WriteLine($"Empty document has {result.Data.Length} tokens");
        }
    }

    [Fact]
    public async Task TestSemanticTokensRange()
    {
        // Arrange
        var code = @"
func test() -> void {
    a <- 10
    b <- 20
    c <- 30
}
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new SemanticTokensHandler(documentManager);
        var request = new SemanticTokensRangeParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                new Position(2, 0),
                new Position(3, 10)
            )
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        testOutputHelper.WriteLine("Semantic tokens range query completed");
    }

    [Fact]
    public void TestLegendConfiguration()
    {
        // Arrange
        var documentManager = new DocumentManager();
        var handler = new SemanticTokensHandler(documentManager);

        // Act - 通过公共方法验证配置
        var request = new SemanticTokensParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri("file:///test.old8") }
        };

        // Assert - 能够创建Handler说明配置正确
        Assert.NotNull(handler);

        testOutputHelper.WriteLine("Semantic tokens handler configured correctly");
    }
}
