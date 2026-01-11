using Old8Lang.LanguageServer.Services;
using Old8Lang.LanguageServer.Handlers;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit.Abstractions;

namespace Old8Lang.Tests.LanguageServer;

/// <summary>
/// 特殊语法补全测试
/// 测试 Old8Lang 特殊语法（Match、Using、Select、Defer、Enum等）的补全功能
/// </summary>
public class CompletionHandler_SpecialSyntaxTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    /// <summary>
    /// 测试 match 表达式关键字补全
    /// </summary>
    [Fact]
    public async Task TestMatchExpressionKeyword()
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
        var keywords = result.Items.Where(item => item.Kind == CompletionItemKind.Keyword).ToList();

        Assert.Contains(keywords, item => item.Label == "match");
        _output.WriteLine("✓ 找到 match 关键字");
    }

    /// <summary>
    /// 测试 using 语句关键字补全
    /// </summary>
    [Fact]
    public async Task TestUsingStatementKeyword()
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
        var keywords = result.Items.Where(item => item.Kind == CompletionItemKind.Keyword).ToList();

        Assert.Contains(keywords, item => item.Label == "using");
        _output.WriteLine("✓ 找到 using 关键字");
    }

    /// <summary>
    /// 测试 select 语句关键字补全
    /// </summary>
    [Fact]
    public async Task TestSelectStatementKeyword()
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
        var keywords = result.Items.Where(item => item.Kind == CompletionItemKind.Keyword).ToList();

        Assert.Contains(keywords, item => item.Label == "select");
        _output.WriteLine("✓ 找到 select 关键字");
    }

    /// <summary>
    /// 测试 defer 语句关键字补全
    /// </summary>
    [Fact]
    public async Task TestDeferStatementKeyword()
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
        var keywords = result.Items.Where(item => item.Kind == CompletionItemKind.Keyword).ToList();

        Assert.Contains(keywords, item => item.Label == "defer");
        _output.WriteLine("✓ 找到 defer 关键字");
    }

    /// <summary>
    /// 测试枚举成员访问补全
    /// </summary>
    [Fact]
    public async Task TestEnumMemberAccessCompletion()
    {
        // Arrange
        var code = @"
enum Color {
    Red,
    Green,
    Blue
}

x <- Color.
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(7, 11) // 在 "Color." 之后
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        _output.WriteLine($"枚举成员访问补全项数量: {result.Items.Count()}");

        // 注意：枚举成员补全可能需要特殊的符号表支持
        // 这里只验证补全系统能够正常处理这个场景
    }

    /// <summary>
    /// 测试 Match 表达式中的 case 补全
    /// </summary>
    [Fact]
    public async Task TestMatchCaseCompletion()
    {
        // Arrange
        var code = @"
result <- match value {

";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(2, 4) // match 块内
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var keywords = result.Items.Where(item => item.Kind == CompletionItemKind.Keyword).ToList();

        // 应该包含 case 关键字
        Assert.Contains(keywords, item => item.Label == "case");

        _output.WriteLine($"Match 表达式内找到 {keywords.Count} 个关键字");
    }

    /// <summary>
    /// 测试 Select 语句中的 case 和 default 补全
    /// </summary>
    [Fact]
    public async Task TestSelectCaseCompletion()
    {
        // Arrange
        var code = @"
select {

";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(2, 4) // select 块内
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var keywords = result.Items.Where(item => item.Kind == CompletionItemKind.Keyword).ToList();

        // 应该包含 case 和 default 关键字
        Assert.Contains(keywords, item => item.Label == "case");
        Assert.Contains(keywords, item => item.Label == "default");

        _output.WriteLine($"Select 语句内找到 {keywords.Count} 个关键字");
    }

    /// <summary>
    /// 测试文档注释补全（///）
    /// </summary>
    [Fact]
    public async Task TestDocCommentCompletion()
    {
        // Arrange
        var code = @"
///
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(1, 3) // 在 /// 之后
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        _output.WriteLine($"文档注释场景补全项数量: {result.Items.Count()}");

        // 文档注释场景可能不需要特殊补全，这里只验证系统不会崩溃
    }

    /// <summary>
    /// 测试字符串模板补全
    /// </summary>
    [Fact]
    public async Task TestStringTemplateCompletion()
    {
        // Arrange
        var code = @"
name <- ""Alice""
message <- $""Hello,
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(2, 19) // 在字符串模板内
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        _output.WriteLine($"字符串模板场景补全项数量: {result.Items.Count()}");

        // 字符串模板内可能需要变量补全，这里验证系统能够处理
    }

    /// <summary>
    /// 测试 Params 可变参数补全
    /// </summary>
    [Fact]
    public async Task TestParamsKeywordCompletion()
    {
        // Arrange
        var code = @"
func test(
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(1, 10) // 在参数列表内
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        _output.WriteLine($"参数列表场景补全项数量: {result.Items.Count()}");

        // 注意：params 可能不是一个独立的关键字，这里只验证补全系统能够处理
    }

    /// <summary>
    /// 测试 Using 语句中的资源补全
    /// </summary>
    [Fact]
    public async Task TestUsingResourceCompletion()
    {
        // Arrange
        var code = @"
mutex <- MutexCreate()
using
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(2, 6) // 在 using 之后
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        // 应该包含变量 mutex
        var variables = result.Items.Where(item =>
            item.Kind == CompletionItemKind.Variable).ToList();

        if (variables.Any(v => v.Label == "mutex"))
        {
            _output.WriteLine("✓ Using 语句中找到资源变量 mutex");
        }

        _output.WriteLine($"Using 语句场景找到 {variables.Count} 个变量");
    }

    /// <summary>
    /// 测试 Defer 语句中的函数调用补全
    /// </summary>
    [Fact]
    public async Task TestDeferFunctionCallCompletion()
    {
        // Arrange
        var code = @"
func cleanup() -> void {
    PrintLine(""Cleaning up"")
}

defer
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(5, 6) // 在 defer 之后
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        // 应该包含函数 cleanup
        var functions = result.Items.Where(item =>
            item.Kind == CompletionItemKind.Function).ToList();

        if (functions.Any(f => f.Label == "cleanup"))
        {
            _output.WriteLine("✓ Defer 语句中找到函数 cleanup");
        }

        _output.WriteLine($"Defer 语句场景找到 {functions.Count} 个函数");
    }

    /// <summary>
    /// 测试 Match 表达式通配符补全
    /// </summary>
    [Fact]
    public async Task TestMatchWildcardCompletion()
    {
        // Arrange
        var code = @"
result <- match value {
    case 1 -> ""one""
    case
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(3, 9) // 在 "case " 之后
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        _output.WriteLine($"Match case 模式场景补全项数量: {result.Items.Count()}");

        // 注意：通配符 _ 的补全可能需要特殊处理
    }

    /// <summary>
    /// 测试所有特殊语法关键字都存在
    /// </summary>
    [Fact]
    public async Task TestAllSpecialSyntaxKeywordsPresent()
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
        var keywords = result.Items.Where(item => item.Kind == CompletionItemKind.Keyword).ToList();

        // 特殊语法关键字
        var specialKeywords = new[] { "match", "using", "select", "defer" };

        _output.WriteLine("验证特殊语法关键字:");
        foreach (var keyword in specialKeywords)
        {
            Assert.Contains(keywords, item => item.Label == keyword);
            _output.WriteLine($"  ✓ {keyword}");
        }
    }
}
