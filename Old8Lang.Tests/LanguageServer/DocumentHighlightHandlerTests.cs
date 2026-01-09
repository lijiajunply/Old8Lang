using Old8Lang.LanguageServer.Services;
using Old8Lang.LanguageServer.Handlers;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit.Abstractions;

namespace Old8Lang.Tests.LanguageServer;

/// <summary>
/// 测试 DocumentHighlightHandler - 文档高亮功能
/// </summary>
public class DocumentHighlightHandlerTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task TestHighlight_Variable()
    {
        // Arrange
        var code = @"
x <- 10
y <- x + 20
z <- x * 2
PrintLine(x)
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new DocumentHighlightHandler(documentManager);
        var request = new DocumentHighlightParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(1, 0) // 在第一个 x 上
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var highlights = result.ToList();

        testOutputHelper.WriteLine($"Found {highlights.Count} highlights for variable 'x'");

        foreach (var highlight in highlights)
        {
            testOutputHelper.WriteLine($"Highlight at {highlight.Range}: {highlight.Kind}");
        }

        // 应该高亮所有 x 的出现
        Assert.True(highlights.Count >= 4, "Should find at least 4 occurrences of 'x'");
    }

    [Fact]
    public async Task TestHighlight_Function()
    {
        // Arrange
        var code = @"
func calculate(a:int) -> int {
    return a * 2
}

result <- calculate(10)
value <- calculate(20)
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new DocumentHighlightHandler(documentManager);
        var request = new DocumentHighlightParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(1, 5) // 在 calculate 函数定义上
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var highlights = result.ToList();

        testOutputHelper.WriteLine($"Found {highlights.Count} highlights for function 'calculate'");

        foreach (var highlight in highlights)
        {
            testOutputHelper.WriteLine($"Highlight at {highlight.Range}: {highlight.Kind}");
        }

        // 应该包含定义和两次调用
        Assert.True(highlights.Count >= 3);
    }

    [Fact]
    public async Task TestHighlight_ReadWriteKind()
    {
        // Arrange
        var code = @"
x <- 10
x <- x + 1
y <- x
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new DocumentHighlightHandler(documentManager);
        var request = new DocumentHighlightParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(1, 0)
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var highlights = result.ToList();

        testOutputHelper.WriteLine($"Found {highlights.Count} highlights");

        foreach (var highlight in highlights)
        {
            testOutputHelper.WriteLine($"Highlight at line {highlight.Range.Start.Line}: {highlight.Kind}");
        }

        // 应该包含写入和读取
        var writeHighlights = highlights.Where(h => h.Kind == DocumentHighlightKind.Write).ToList();
        var readHighlights = highlights.Where(h => h.Kind == DocumentHighlightKind.Read).ToList();

        testOutputHelper.WriteLine($"Write highlights: {writeHighlights.Count}, Read highlights: {readHighlights.Count}");

        Assert.NotEmpty(writeHighlights);
        Assert.NotEmpty(readHighlights);
    }

    [Fact]
    public async Task TestHighlight_ClassMember()
    {
        // Arrange
        var code = @"
class Person {
    public name:string

    public func setName(newName:string) -> void {
        this.name <- newName
    }

    public func getName() -> string {
        return this.name
    }
}
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new DocumentHighlightHandler(documentManager);
        var request = new DocumentHighlightParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(2, 11) // 在 name 属性上
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var highlights = result.ToList();

        testOutputHelper.WriteLine($"Found {highlights.Count} highlights for 'name' member");

        foreach (var highlight in highlights)
        {
            testOutputHelper.WriteLine($"Highlight at {highlight.Range}: {highlight.Kind}");
        }

        // 应该高亮 name 的所有出现（定义和引用）
        Assert.True(highlights.Count >= 3);
    }

    [Fact]
    public async Task TestNoHighlight_InvalidPosition()
    {
        // Arrange
        var code = @"
func test() -> void {
    a <- 10
}
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new DocumentHighlightHandler(documentManager);
        var request = new DocumentHighlightParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(1, 20) // 在空白位置
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert - 空白位置应该返回 null 或空
        if (result != null)
        {
            Assert.Empty(result);
        }
        testOutputHelper.WriteLine("No highlights at invalid position");
    }

    [Fact]
    public async Task TestHighlight_Parameter()
    {
        // Arrange
        var code = @"
func add(a:int, b:int) -> int {
    sum <- a + b
    return sum
}
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new DocumentHighlightHandler(documentManager);
        var request = new DocumentHighlightParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(1, 9) // 在参数 a 上
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var highlights = result.ToList();

        testOutputHelper.WriteLine($"Found {highlights.Count} highlights for parameter 'a'");

        foreach (var highlight in highlights)
        {
            testOutputHelper.WriteLine($"Highlight at {highlight.Range}");
        }

        // 应该高亮参数定义和使用
        Assert.True(highlights.Count >= 2);
    }

    [Fact]
    public async Task TestHighlight_MultipleOccurrences()
    {
        // Arrange
        var code = @"
count <- 0
count <- count + 1
count <- count + 1
count <- count + 1
PrintLine(count)
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new DocumentHighlightHandler(documentManager);
        var request = new DocumentHighlightParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(1, 0)
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var highlights = result.ToList();

        testOutputHelper.WriteLine($"Found {highlights.Count} highlights for 'count'");

        // 应该高亮所有 count 的出现
        Assert.True(highlights.Count >= 8); // 每行有2个count，共4行，加上最后一行
    }

    [Fact]
    public void TestHandlerConfiguration()
    {
        // Arrange
        var documentManager = new DocumentManager();
        var handler = new DocumentHighlightHandler(documentManager);

        // Assert - 能够创建Handler说明配置正确
        Assert.NotNull(handler);

        testOutputHelper.WriteLine("Document highlight handler configured correctly");
    }
}
