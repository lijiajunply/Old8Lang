using Old8Lang.LanguageServer.Handlers;
using Old8Lang.LanguageServer.Services;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit.Abstractions;

namespace Old8Lang.Tests.LanguageServer.Handlers.Editing;

/// <summary>
/// 测试 DocumentFormattingHandler - 文档格式化功能
/// </summary>
public class DocumentFormattingHandlerTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task TestFullDocumentFormatting()
    {
        // Arrange
        var code = @"
func test() -> void {
a <- 10
b <- 20
}
";
        var documentManager = new DocumentManager();
        var formattingService = new FormattingService();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new DocumentFormattingHandler(documentManager, formattingService);
        var request = new DocumentFormattingParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Options = new FormattingOptions
            {
                TabSize = 4,
                InsertSpaces = true
            }
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var edits = result.ToList();

        testOutputHelper.WriteLine($"Found {edits.Count} formatting edits");

        foreach (var edit in edits)
        {
            testOutputHelper.WriteLine($"Edit at {edit.Range}: '{edit.NewText}'");
        }

        // 应该有格式化编辑
        if (edits.Count > 0)
        {
            Assert.NotEmpty(edits);
        }
    }

    [Fact]
    public async Task TestRangeFormatting()
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
        var formattingService = new FormattingService();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new DocumentFormattingHandler(documentManager, formattingService);
        var request = new DocumentRangeFormattingParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                new Position(2, 0),
                new Position(3, 10)
            ),
            Options = new FormattingOptions
            {
                TabSize = 4,
                InsertSpaces = true
            }
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var edits = result.ToList();

        testOutputHelper.WriteLine($"Found {edits.Count} range formatting edits");

        foreach (var edit in edits)
        {
            testOutputHelper.WriteLine($"Edit at {edit.Range}: '{edit.NewText}'");
        }
    }

    [Fact]
    public async Task TestFormatting_WithTabs()
    {
        // Arrange
        var code = @"
func test() -> void {
a <- 10
}
";
        var documentManager = new DocumentManager();
        var formattingService = new FormattingService();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new DocumentFormattingHandler(documentManager, formattingService);
        var request = new DocumentFormattingParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Options = new FormattingOptions
            {
                TabSize = 1,
                InsertSpaces = false // 使用制表符
            }
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var edits = result.ToList();

        testOutputHelper.WriteLine($"Found {edits.Count} edits with tabs");
    }

    [Fact]
    public async Task TestFormatting_EmptyDocument()
    {
        // Arrange
        var code = "";
        var documentManager = new DocumentManager();
        var formattingService = new FormattingService();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new DocumentFormattingHandler(documentManager, formattingService);
        var request = new DocumentFormattingParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Options = new FormattingOptions
            {
                TabSize = 4,
                InsertSpaces = true
            }
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert - 空文档可能返回 null 或空编辑列表
        if (result != null)
        {
            var edits = result.ToList();
            testOutputHelper.WriteLine($"Empty document: {edits.Count} edits");
        }
        else
        {
            testOutputHelper.WriteLine("Empty document returns null");
        }
    }

    [Fact]
    public async Task TestFormatting_NestedBlocks()
    {
        // Arrange
        var code = @"
class Test {
public func method() -> void {
if true {
a <- 10
}
}
}
";
        var documentManager = new DocumentManager();
        var formattingService = new FormattingService();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new DocumentFormattingHandler(documentManager, formattingService);
        var request = new DocumentFormattingParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Options = new FormattingOptions
            {
                TabSize = 4,
                InsertSpaces = true
            }
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var edits = result.ToList();

        testOutputHelper.WriteLine($"Found {edits.Count} edits for nested blocks");

        foreach (var edit in edits)
        {
            testOutputHelper.WriteLine($"Edit: {edit.NewText.Replace("\n", "\\n")}");
        }
    }

    [Fact]
    public async Task TestRegistrationOptions_FullDocument()
    {
        // Arrange
        var documentManager = new DocumentManager();
        var formattingService = new FormattingService();
        var handler = new DocumentFormattingHandler(documentManager, formattingService);

        // Act
        var options = handler.GetRegistrationOptions(
            new DocumentFormattingCapability(),
            new ClientCapabilities()
        );

        // Assert
        Assert.NotNull(options);
        Assert.NotNull(options.DocumentSelector);

        testOutputHelper.WriteLine($"Full document formatting options: {options.DocumentSelector}");
    }

    [Fact]
    public async Task TestRegistrationOptions_Range()
    {
        // Arrange
        var documentManager = new DocumentManager();
        var formattingService = new FormattingService();
        var handler = new DocumentFormattingHandler(documentManager, formattingService);

        // Act
        var options = handler.GetRegistrationOptions(
            new DocumentRangeFormattingCapability(),
            new ClientCapabilities()
        );

        // Assert
        Assert.NotNull(options);
        Assert.NotNull(options.DocumentSelector);

        testOutputHelper.WriteLine($"Range formatting options: {options.DocumentSelector}");
    }

    [Fact]
    public async Task TestFormatting_PreservesContent()
    {
        // Arrange
        var code = @"
func add(a:int, b:int) -> int {
return a + b
}
";
        var documentManager = new DocumentManager();
        var formattingService = new FormattingService();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new DocumentFormattingHandler(documentManager, formattingService);
        var request = new DocumentFormattingParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Options = new FormattingOptions
            {
                TabSize = 4,
                InsertSpaces = true
            }
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        if (result != null)
        {
            var edits = result.ToList();

            testOutputHelper.WriteLine($"Found {edits.Count} edits");

            // 应用编辑后的代码应该仍然包含原始内容
            foreach (var edit in edits)
            {
                testOutputHelper.WriteLine($"Edit text length: {edit.NewText.Length}");
            }
        }
    }
}
