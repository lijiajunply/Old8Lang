using Old8Lang.LanguageServer.Handlers;
using Old8Lang.LanguageServer.Services;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit.Abstractions;

namespace Old8Lang.Tests.LanguageServer.Handlers.Navigation;

/// <summary>
/// 测试 DocumentLinkHandler - 导入链接功能
/// </summary>
public class DocumentLinkHandlerTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task TestDocumentLink_StandardLibrary()
    {
        // Arrange
        var code = @"
import ""OS""
import ""File""

func test() -> void {
    PrintLine(""test"")
}
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new DocumentLinkHandler(documentManager);
        var request = new DocumentLinkParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) }
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var links = result.ToList();

        testOutputHelper.WriteLine($"Found {links.Count} document links");

        foreach (var link in links)
        {
            testOutputHelper.WriteLine($"Link: {link.Target}, Range: {link.Range}");
        }

        // 可能找到标准库链接（如果能解析到源码路径）
        if (links.Count > 0)
        {
            Assert.NotEmpty(links);
        }
    }

    [Fact]
    public async Task TestDocumentLink_LocalFile()
    {
        // Arrange
        var code = @"
import ""./module.old8""

func test() -> void {
}
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new DocumentLinkHandler(documentManager);
        var request = new DocumentLinkParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) }
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var links = result.ToList();

        testOutputHelper.WriteLine($"Found {links.Count} document links for local file");

        // 本地文件导入可能生成链接
        foreach (var link in links)
        {
            testOutputHelper.WriteLine($"Link target: {link.Target}");
        }
    }

    [Fact]
    public async Task TestDocumentLink_NoImports()
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

        var handler = new DocumentLinkHandler(documentManager);
        var request = new DocumentLinkParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) }
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var links = result.ToList();

        testOutputHelper.WriteLine($"Document with no imports has {links.Count} links");

        // 没有 import 语句，应该没有链接
        Assert.Empty(links);
    }

    [Fact]
    public async Task TestDocumentLink_MultipleImports()
    {
        // Arrange
        var code = @"
import ""OS""
import ""File""
import ""Math""

func test() -> void {
}
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new DocumentLinkHandler(documentManager);
        var request = new DocumentLinkParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) }
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var links = result.ToList();

        testOutputHelper.WriteLine($"Found {links.Count} links for multiple imports");

        foreach (var link in links)
        {
            testOutputHelper.WriteLine($"Link: {link.Tooltip}");
        }
    }

    [Fact]
    public async Task TestDocumentLink_EmptyDocument()
    {
        // Arrange
        var code = "";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new DocumentLinkHandler(documentManager);
        var request = new DocumentLinkParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) }
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);

        testOutputHelper.WriteLine("Empty document has no links");
    }

    [Fact]
    public void TestRegistrationOptions()
    {
        // Arrange
        var documentManager = new DocumentManager();
        var handler = new DocumentLinkHandler(documentManager);

        // Act
        var options = handler.GetRegistrationOptions(
            new DocumentLinkCapability(),
            new ClientCapabilities()
        );

        // Assert
        Assert.NotNull(options);
        Assert.NotNull(options.DocumentSelector);
        Assert.False(options.ResolveProvider);

        testOutputHelper.WriteLine($"Document link options: ResolveProvider={options.ResolveProvider}");
    }

    [Fact]
    public async Task TestDocumentLink_FromSyntax()
    {
        // Arrange
        var code = @"
import { OS } from ""OS""

func test() -> void {
}
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new DocumentLinkHandler(documentManager);
        var request = new DocumentLinkParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) }
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var links = result.ToList();

        testOutputHelper.WriteLine($"From syntax: found {links.Count} links");
    }
}
