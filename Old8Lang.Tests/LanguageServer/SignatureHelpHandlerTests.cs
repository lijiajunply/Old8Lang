using Old8Lang.LanguageServer.Services;
using Old8Lang.LanguageServer.Handlers;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit.Abstractions;

namespace Old8Lang.Tests.LanguageServer;

/// <summary>
/// 测试 SignatureHelpHandler - 函数参数提示功能
/// </summary>
public class SignatureHelpHandlerTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task TestFunctionSignatureHelp_WithUserDefinedFunction()
    {
        // Arrange
        var code = @"
func add(a:int, b:int) -> int {
    return a + b
}

result <- add(
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new SignatureHelpHandler(documentManager);
        var request = new SignatureHelpParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(5, 14) // 在 add( 后
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Signatures);

        var signature = result.Signatures.First();
        testOutputHelper.WriteLine($"Signature: {signature.Label}");

        Assert.Contains("add", signature.Label);
        Assert.Contains("a:int", signature.Label);
        Assert.Contains("b:int", signature.Label);
        Assert.Contains("-> int", signature.Label);
        Assert.Equal(0, result.ActiveParameter);
    }

    [Fact]
    public async Task TestFunctionSignatureHelp_WithMultipleParameters()
    {
        // Arrange
        var code = @"
func calculate(x:int, y:int, operation:string) -> int {
    return x + y
}

result <- calculate(10,
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new SignatureHelpHandler(documentManager);
        var request = new SignatureHelpParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(5, 24) // 在第一个逗号后
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Signatures);

        var signature = result.Signatures.First();
        testOutputHelper.WriteLine($"Signature: {signature.Label}");
        testOutputHelper.WriteLine($"Active parameter: {result.ActiveParameter}");

        // 第二个参数应该是活跃的
        Assert.Equal(1, result.ActiveParameter);
        Assert.Equal(3, signature.Parameters.Count());
    }

    [Fact]
    public async Task TestBuiltInFunctionSignature_PrintLine()
    {
        // Arrange
        var code = "PrintLine(";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new SignatureHelpHandler(documentManager);
        var request = new SignatureHelpParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(0, 10)
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Signatures);

        var signature = result.Signatures.First();
        testOutputHelper.WriteLine($"Signature: {signature.Label}");
        testOutputHelper.WriteLine($"Documentation: {signature.Documentation}");

        Assert.Contains("PrintLine", signature.Label);
        Assert.Contains("打印一行并换行", signature.Documentation?.ToString() ?? "");
        Assert.Equal(0, result.ActiveParameter);
    }

    [Fact]
    public async Task TestBuiltInFunctionSignature_Input()
    {
        // Arrange
        var code = "name <- Input(";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new SignatureHelpHandler(documentManager);
        var request = new SignatureHelpParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(0, 14)
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Signatures);

        var signature = result.Signatures.First();
        testOutputHelper.WriteLine($"Signature: {signature.Label}");

        Assert.Contains("Input", signature.Label);
        Assert.Contains("prompt:string", signature.Label);
        Assert.Contains("-> string", signature.Label);
    }

    [Fact]
    public async Task TestBuiltInFunctionSignature_Range()
    {
        // Arrange
        var code = "r <- Range(1, 10, ";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new SignatureHelpHandler(documentManager);
        var request = new SignatureHelpParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(0, 18) // 在第二个逗号后
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Signatures);

        var signature = result.Signatures.First();
        testOutputHelper.WriteLine($"Signature: {signature.Label}");
        testOutputHelper.WriteLine($"Active parameter: {result.ActiveParameter}");

        Assert.Contains("Range", signature.Label);
        Assert.Equal(2, result.ActiveParameter); // 第三个参数
        Assert.Equal(3, signature.Parameters.Count());
    }

    [Fact]
    public async Task TestNoSignatureHelp_WithoutFunctionCall()
    {
        // Arrange
        var code = "a <- 123";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new SignatureHelpHandler(documentManager);
        var request = new SignatureHelpParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(0, 5)
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task TestNoSignatureHelp_WithUndefinedFunction()
    {
        // Arrange
        var code = "unknownFunc(";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new SignatureHelpHandler(documentManager);
        var request = new SignatureHelpParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(0, 12)
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task TestNestedFunctionCalls()
    {
        // Arrange
        var code = @"
func outer(x:int) -> int {
    return x
}

func inner(y:int) -> int {
    return y
}

result <- outer(inner(
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new SignatureHelpHandler(documentManager);
        var request = new SignatureHelpParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(9, 22) // 在 inner( 后
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        var signature = result.Signatures.First();
        testOutputHelper.WriteLine($"Signature: {signature.Label}");

        // 应该显示 inner 函数的签名
        Assert.Contains("inner", signature.Label);
        Assert.Contains("y:int", signature.Label);
    }

    [Fact]
    public async Task TestFunctionWithDocumentation()
    {
        // Arrange
        var code = @"
// 计算两个数的和
// @param a 第一个加数
// @param b 第二个加数
// @return 两数之和
func add(a:int, b:int) -> int {
    return a + b
}

result <- add(
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new SignatureHelpHandler(documentManager);
        var request = new SignatureHelpParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(9, 14)
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        var signature = result.Signatures.First();
        testOutputHelper.WriteLine($"Signature: {signature.Label}");
        testOutputHelper.WriteLine($"Documentation: {signature.Documentation}");

        Assert.Contains("add", signature.Label);

        // 如果文档注释已解析，应该包含文档
        if (signature.Documentation != null)
        {
            var doc = signature.Documentation.ToString();
            testOutputHelper.WriteLine($"Found documentation: {doc}");
        }
    }

    [Fact]
    public async Task TestEmptyDocument()
    {
        // Arrange
        var code = "";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new SignatureHelpHandler(documentManager);
        var request = new SignatureHelpParams
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
    public async Task TestRegistrationOptions()
    {
        // Arrange
        var documentManager = new DocumentManager();
        var handler = new SignatureHelpHandler(documentManager);

        // Act
        var options = handler.GetRegistrationOptions(
            new SignatureHelpCapability(),
            new ClientCapabilities()
        );

        // Assert
        Assert.NotNull(options);
        Assert.Contains("(", options.TriggerCharacters);
        Assert.Contains(",", options.TriggerCharacters);
        Assert.Contains(",", options.RetriggerCharacters);

        testOutputHelper.WriteLine($"Trigger characters: {string.Join(", ", options.TriggerCharacters)}");
        testOutputHelper.WriteLine($"Retrigger characters: {string.Join(", ", options.RetriggerCharacters)}");
    }
}
