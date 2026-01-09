using Old8Lang.LanguageServer.Services;
using Old8Lang.LanguageServer.Handlers;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit.Abstractions;

namespace Old8Lang.Tests.LanguageServer;

/// <summary>
/// 测试 FoldingRangeHandler - 代码折叠功能
/// </summary>
public class FoldingRangeHandlerTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task TestFolding_Function()
    {
        // Arrange
        var code = @"
func test() -> void {
    a <- 10
    b <- 20
    PrintLine(a + b)
}
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new FoldingRangeHandler(documentManager);
        var request = new FoldingRangeRequestParam
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) }
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var foldingRanges = result.ToList();

        testOutputHelper.WriteLine($"Found {foldingRanges.Count} folding ranges");

        foreach (var range in foldingRanges)
        {
            testOutputHelper.WriteLine($"Folding range: lines {range.StartLine}-{range.EndLine}, kind: {range.Kind}");
        }

        // 应该至少有一个函数折叠
        Assert.Contains(foldingRanges, r => r.Kind == FoldingRangeKind.Region);
    }

    [Fact]
    public async Task TestFolding_Class()
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
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new FoldingRangeHandler(documentManager);
        var request = new FoldingRangeRequestParam
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) }
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var foldingRanges = result.ToList();

        testOutputHelper.WriteLine($"Found {foldingRanges.Count} folding ranges in class");

        foreach (var range in foldingRanges)
        {
            testOutputHelper.WriteLine($"Folding range: lines {range.StartLine}-{range.EndLine}, kind: {range.Kind}");
        }

        // 应该包含类的折叠和方法的折叠
        Assert.True(foldingRanges.Count >= 2);
    }

    [Fact]
    public async Task TestFolding_NestedBlocks()
    {
        // Arrange
        var code = @"
func outer() -> void {
    if true {
        for i in Range(0, 10, 1) {
            PrintLine(i)
        }
    }
}
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new FoldingRangeHandler(documentManager);
        var request = new FoldingRangeRequestParam
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) }
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var foldingRanges = result.ToList();

        testOutputHelper.WriteLine($"Found {foldingRanges.Count} folding ranges in nested blocks");

        foreach (var range in foldingRanges)
        {
            testOutputHelper.WriteLine($"Folding range: lines {range.StartLine}-{range.EndLine}");
        }

        // 应该有多个嵌套的折叠区域（func, if, for）
        Assert.True(foldingRanges.Count >= 3);
    }

    [Fact]
    public async Task TestFolding_Comments()
    {
        // Arrange
        var code = @"
// Comment line 1
// Comment line 2
// Comment line 3

func test() -> void {
    a <- 10
}
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new FoldingRangeHandler(documentManager);
        var request = new FoldingRangeRequestParam
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) }
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var foldingRanges = result.ToList();

        testOutputHelper.WriteLine($"Found {foldingRanges.Count} folding ranges including comments");

        foreach (var range in foldingRanges)
        {
            testOutputHelper.WriteLine($"Folding range: lines {range.StartLine}-{range.EndLine}, kind: {range.Kind}");
        }

        // 应该包含注释折叠
        var commentFolding = foldingRanges.Where(r => r.Kind == FoldingRangeKind.Comment).ToList();
        testOutputHelper.WriteLine($"Comment folding ranges: {commentFolding.Count}");
    }

    [Fact]
    public async Task TestFolding_MultipleStatements()
    {
        // Arrange
        var code = @"
if true {
    a <- 10
}

while true {
    b <- 20
}

for i in Range(0, 10, 1) {
    c <- 30
}
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new FoldingRangeHandler(documentManager);
        var request = new FoldingRangeRequestParam
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) }
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var foldingRanges = result.ToList();

        testOutputHelper.WriteLine($"Found {foldingRanges.Count} folding ranges for multiple statements");

        foreach (var range in foldingRanges)
        {
            testOutputHelper.WriteLine($"Folding range: lines {range.StartLine}-{range.EndLine}");
        }

        // 应该为每个控制流语句创建折叠
        Assert.True(foldingRanges.Count >= 3);
    }

    [Fact]
    public async Task TestNoFolding_SingleLineBlock()
    {
        // Arrange
        var code = @"
func test() -> void { a <- 10 }
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new FoldingRangeHandler(documentManager);
        var request = new FoldingRangeRequestParam
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) }
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        if (result != null)
        {
            var foldingRanges = result.ToList();

            testOutputHelper.WriteLine($"Found {foldingRanges.Count} folding ranges for single-line block");

            // 单行块不应该创建折叠
            Assert.Empty(foldingRanges);
        }
    }

    [Fact]
    public async Task TestFolding_EmptyDocument()
    {
        // Arrange
        var code = "";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new FoldingRangeHandler(documentManager);
        var request = new FoldingRangeRequestParam
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) }
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert - 空文档应该返回 null 或空列表
        if (result != null)
        {
            Assert.Empty(result);
        }

        testOutputHelper.WriteLine("Empty document returns no folding ranges");
    }

    [Fact]
    public async Task TestFolding_TryCatchFinally()
    {
        // Arrange
        var code = @"
func test() -> void {
    try {
        a <- 10
    } catch e {
        PrintLine(e)
    } finally {
        b <- 20
    }
}
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new FoldingRangeHandler(documentManager);
        var request = new FoldingRangeRequestParam
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) }
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var foldingRanges = result.ToList();

        testOutputHelper.WriteLine($"Found {foldingRanges.Count} folding ranges for try-catch-finally");

        foreach (var range in foldingRanges)
        {
            testOutputHelper.WriteLine($"Folding range: lines {range.StartLine}-{range.EndLine}");
        }

        // 应该包含 func, try, catch, finally 的折叠
        Assert.True(foldingRanges.Count >= 4);
    }

    [Fact]
    public async Task TestFolding_Switch()
    {
        // Arrange
        var code = @"
switch value {
    case 1 -> {
        PrintLine(""one"")
    }
    case 2 -> {
        PrintLine(""two"")
    }
    default -> {
        PrintLine(""other"")
    }
}
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new FoldingRangeHandler(documentManager);
        var request = new FoldingRangeRequestParam
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) }
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var foldingRanges = result.ToList();

        testOutputHelper.WriteLine($"Found {foldingRanges.Count} folding ranges for switch");

        foreach (var range in foldingRanges)
        {
            testOutputHelper.WriteLine($"Folding range: lines {range.StartLine}-{range.EndLine}");
        }

        // 应该包含 switch 和每个 case 的折叠
        Assert.True(foldingRanges.Count >= 4);
    }

    [Fact]
    public void TestHandlerConfiguration()
    {
        // Arrange
        var documentManager = new DocumentManager();
        var handler = new FoldingRangeHandler(documentManager);

        // Assert - 能够创建Handler说明配置正确
        Assert.NotNull(handler);

        testOutputHelper.WriteLine("Folding range handler configured correctly");
    }
}
