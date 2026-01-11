using Old8Lang.LanguageServer.Handlers;
using Old8Lang.LanguageServer.Services;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit.Abstractions;

namespace Old8Lang.Tests.LanguageServer.Completion.Core;

/// <summary>
/// 边界测试
/// 测试各种边界情况、极限情况和错误处理
/// </summary>
public class CompletionHandler_BoundaryTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    /// <summary>
    /// 测试空文档补全
    /// </summary>
    [Fact]
    public async Task TestEmptyDocumentCompletion()
    {
        // Arrange
        var code = "";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(0, 0)
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Items);

        _output.WriteLine($"空文档补全项数量: {result.Items.Count()}");
    }

    /// <summary>
    /// 测试只有空行的文档补全
    /// </summary>
    [Fact]
    public async Task TestOnlyEmptyLinesCompletion()
    {
        // Arrange
        var code = "\n\n\n";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(1, 0) // 第二行
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Items);

        _output.WriteLine($"空行文档补全项数量: {result.Items.Count()}");
    }

    /// <summary>
    /// 测试只有注释的文档补全
    /// </summary>
    [Fact]
    public async Task TestOnlyCommentsCompletion()
    {
        // Arrange
        var code = @"
// 这是注释
// 另一行注释
/// 文档注释
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(4, 0)
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Items);

        _output.WriteLine($"只有注释的文档补全项数量: {result.Items.Count()}");
    }

    /// <summary>
    /// 测试文件开始位置补全
    /// </summary>
    [Fact]
    public async Task TestFileStartPositionCompletion()
    {
        // Arrange
        var code = "x <- 123";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(0, 0) // 文件开始
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Items);

        _output.WriteLine($"文件开始位置补全项数量: {result.Items.Count()}");
    }

    /// <summary>
    /// 测试文件结束位置补全
    /// </summary>
    [Fact]
    public async Task TestFileEndPositionCompletion()
    {
        // Arrange
        var code = "x <- 123";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(0, 8) // 文件结束
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Items);

        _output.WriteLine($"文件结束位置补全项数量: {result.Items.Count()}");
    }

    /// <summary>
    /// 测试行开始位置补全
    /// </summary>
    [Fact]
    public async Task TestLineStartPositionCompletion()
    {
        // Arrange
        var code = @"
func test() -> void {
    PrintLine(""test"")
}
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(2, 0) // 行开始
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Items);

        _output.WriteLine($"行开始位置补全项数量: {result.Items.Count()}");
    }

    /// <summary>
    /// 测试极长标识符补全
    /// </summary>
    [Fact]
    public async Task TestVeryLongIdentifierCompletion()
    {
        // Arrange
        var longName = new string('a', 500); // 500 字符长的标识符
        var code = $"{longName} <- 123\n";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(1, 0)
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        // 应该能找到这个超长标识符
        var variables = result.Items.Where(item => item.Kind == CompletionItemKind.Variable).ToList();
        if (variables.Any(v => v.Label == longName))
        {
            _output.WriteLine($"✓ 找到长度为 {longName.Length} 的标识符");
        }
    }

    /// <summary>
    /// 测试极深嵌套补全
    /// </summary>
    [Fact]
    public async Task TestDeeplyNestedCompletion()
    {
        // Arrange
        var code = @"
if true {
    if true {
        if true {
            if true {
                if true {
                    x <- 123
                }
            }
        }
    }
}
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(6, 28) // 在最深层
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Items);

        _output.WriteLine($"深层嵌套补全项数量: {result.Items.Count()}");
    }

    /// <summary>
    /// 测试极多参数函数补全
    /// </summary>
    [Fact]
    public async Task TestManyParametersFunctionCompletion()
    {
        // Arrange
        var code = @"
func manyParams(a:int, b:int, c:int, d:int, e:int,
                f:int, g:int, h:int, i:int, j:int,
                k:int, l:int, m:int, n:int, o:int) -> void {
}

";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(5, 0)
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        // 应该能找到这个多参数函数
        var functions = result.Items.Where(item => item.Kind == CompletionItemKind.Function).ToList();
        Assert.Contains(functions, f => f.Label == "manyParams");

        _output.WriteLine("✓ 找到多参数函数");
    }

    /// <summary>
    /// 测试极长字符串字面量补全
    /// </summary>
    [Fact]
    public async Task TestVeryLongStringLiteralCompletion()
    {
        // Arrange
        var longString = new string('x', 1000);
        var code = $"x <- \"{longString}\"\n";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(1, 0)
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        _output.WriteLine($"长字符串场景补全项数量: {result.Items.Count()}");
    }

    /// <summary>
    /// 测试包含中文注释的文档补全
    /// </summary>
    [Fact]
    public async Task TestChineseCommentsCompletion()
    {
        // Arrange
        var code = @"
/// 这是一个中文文档注释
/// 用于测试 Unicode 支持
func 测试函数() -> void {
    // 中文注释
    x <- 123
}
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(7, 0)
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        _output.WriteLine($"中文注释场景补全项数量: {result.Items.Count()}");
    }

    /// <summary>
    /// 测试语法错误文档的补全
    /// </summary>
    [Fact]
    public async Task TestSyntaxErrorDocumentCompletion()
    {
        // Arrange
        var code = @"
func test( -> void {
    x <-
}
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(3, 0)
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        // 即使有语法错误，补全系统也应该尽力工作
        _output.WriteLine($"语法错误文档补全项数量: {result.Items.Count()}");
    }

    /// <summary>
    /// 测试不存在的文档补全
    /// </summary>
    [Fact]
    public async Task TestNonExistentDocumentCompletion()
    {
        // Arrange
        var documentManager = new DocumentManager();
        var uri = "file:///nonexistent.old8";

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(0, 0)
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        // 不存在的文档应该返回默认补全（关键字等）
        _output.WriteLine($"不存在文档的补全项数量: {result.Items.Count()}");
    }

    /// <summary>
    /// 测试超出范围的位置补全
    /// </summary>
    [Fact]
    public async Task TestOutOfBoundsPositionCompletion()
    {
        // Arrange
        var code = "x <- 123";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(100, 100) // 超出范围的位置
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        // 应该能够处理超出范围的位置
        _output.WriteLine($"超出范围位置补全项数量: {result.Items.Count()}");
    }

    /// <summary>
    /// 测试负数位置补全（虽然不应该发生，但要有防御性代码）
    /// </summary>
    [Fact]
    public async Task TestNegativePositionCompletion()
    {
        // Arrange
        var code = "x <- 123";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(-1, -1) // 负数位置（不合法）
        };

        // Act & Assert
        // 应该不抛出异常
        var result = await handler.Handle(request, CancellationToken.None);
        Assert.NotNull(result);

        _output.WriteLine($"负数位置补全项数量: {result.Items.Count()}");
    }

    /// <summary>
    /// 测试只有空格的行补全
    /// </summary>
    [Fact]
    public async Task TestOnlyWhitespaceLineCompletion()
    {
        // Arrange
        var code = @"
func test() -> void {

}
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(2, 8) // 空格中间
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Items);

        _output.WriteLine($"只有空格的行补全项数量: {result.Items.Count()}");
    }

    /// <summary>
    /// 测试特殊转义字符场景补全
    /// </summary>
    [Fact]
    public async Task TestEscapeCharactersCompletion()
    {
        // Arrange
        var code = @"
x <- ""line1\nline2\ttab\rcarriage\\backslash\""quote""
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(2, 0)
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        _output.WriteLine($"转义字符场景补全项数量: {result.Items.Count()}");
    }

    /// <summary>
    /// 测试大量符号表的补全性能
    /// </summary>
    [Fact]
    public async Task TestLargeSymbolTableCompletion()
    {
        // Arrange - 创建包含大量符号的文档
        var codeBuilder = new System.Text.StringBuilder();
        for (int i = 0; i < 100; i++)
        {
            codeBuilder.AppendLine($"func func{i}() -> void {{ }}");
            codeBuilder.AppendLine($"x{i} <- {i}");
        }

        var code = codeBuilder.ToString();
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(200, 0)
        };

        // Act
        var startTime = DateTime.Now;
        var result = await handler.Handle(request, CancellationToken.None);
        var elapsed = DateTime.Now - startTime;

        // Assert
        Assert.NotNull(result);

        _output.WriteLine($"大量符号表补全项数量: {result.Items.Count()}");
        _output.WriteLine($"补全耗时: {elapsed.TotalMilliseconds} ms");

        // 性能要求：应该在 1 秒内完成
        Assert.True(elapsed.TotalSeconds < 1, "补全响应时间应该在 1 秒内");
    }
}
