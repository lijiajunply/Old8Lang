using Old8Lang.LanguageServer.Services;
using Old8Lang.LanguageServer.Handlers;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit.Abstractions;

namespace Old8Lang.Tests.LanguageServer;

/// <summary>
/// 代码片段补全测试
/// 测试所有 Old8Lang 代码片段的补全功能
/// </summary>
public class CompletionHandler_SnippetsTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    /// <summary>
    /// 测试函数定义代码片段
    /// </summary>
    [Fact]
    public async Task TestFunctionSnippet()
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
        var snippets = result.Items.Where(item => item.Kind == CompletionItemKind.Snippet).ToList();

        var funcSnippet = snippets.FirstOrDefault(s => s.Label == "func");
        Assert.NotNull(funcSnippet);

        // 验证代码片段属性
        Assert.Equal(CompletionItemKind.Snippet, funcSnippet.Kind);
        Assert.Equal(InsertTextFormat.Snippet, funcSnippet.InsertTextFormat);
        Assert.NotNull(funcSnippet.InsertText);
        Assert.Contains("$", funcSnippet.InsertText); // 应该包含占位符

        _output.WriteLine($"函数代码片段:");
        _output.WriteLine($"  Label: {funcSnippet.Label}");
        _output.WriteLine($"  Detail: {funcSnippet.Detail}");
        _output.WriteLine($"  InsertText: {funcSnippet.InsertText}");
    }

    /// <summary>
    /// 测试异步函数代码片段
    /// </summary>
    [Fact]
    public async Task TestAsyncFunctionSnippet()
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
        var snippets = result.Items.Where(item => item.Kind == CompletionItemKind.Snippet).ToList();

        var asyncFuncSnippet = snippets.FirstOrDefault(s => s.Label == "asyncfunc");
        Assert.NotNull(asyncFuncSnippet);

        // 验证包含 async 关键字
        Assert.Contains("async", asyncFuncSnippet.InsertText!);
        Assert.Equal(InsertTextFormat.Snippet, asyncFuncSnippet.InsertTextFormat);

        _output.WriteLine($"异步函数代码片段:");
        _output.WriteLine($"  Label: {asyncFuncSnippet.Label}");
        _output.WriteLine($"  InsertText: {asyncFuncSnippet.InsertText}");
    }

    /// <summary>
    /// 测试类定义代码片段
    /// </summary>
    [Fact]
    public async Task TestClassSnippet()
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
        var snippets = result.Items.Where(item => item.Kind == CompletionItemKind.Snippet).ToList();

        var classSnippet = snippets.FirstOrDefault(s => s.Label == "class");
        Assert.NotNull(classSnippet);

        Assert.Contains("class", classSnippet.InsertText!);
        Assert.Contains("{", classSnippet.InsertText!);
        Assert.Contains("}", classSnippet.InsertText!);

        _output.WriteLine($"类定义代码片段:");
        _output.WriteLine($"  InsertText: {classSnippet.InsertText}");
    }

    /// <summary>
    /// 测试 if 语句代码片段
    /// </summary>
    [Fact]
    public async Task TestIfSnippet()
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
        var snippets = result.Items.Where(item => item.Kind == CompletionItemKind.Snippet).ToList();

        var ifSnippet = snippets.FirstOrDefault(s => s.Label == "if");
        Assert.NotNull(ifSnippet);

        Assert.Contains("if", ifSnippet.InsertText!);
        Assert.Contains("$", ifSnippet.InsertText!); // 占位符

        _output.WriteLine($"if 语句代码片段:");
        _output.WriteLine($"  InsertText: {ifSnippet.InsertText}");
    }

    /// <summary>
    /// 测试 if-else 语句代码片段
    /// </summary>
    [Fact]
    public async Task TestIfElseSnippet()
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
        var snippets = result.Items.Where(item => item.Kind == CompletionItemKind.Snippet).ToList();

        var ifElseSnippet = snippets.FirstOrDefault(s => s.Label == "ifelse");
        Assert.NotNull(ifElseSnippet);

        Assert.Contains("if", ifElseSnippet.InsertText!);
        Assert.Contains("else", ifElseSnippet.InsertText!);

        _output.WriteLine($"if-else 语句代码片段:");
        _output.WriteLine($"  InsertText: {ifElseSnippet.InsertText}");
    }

    /// <summary>
    /// 测试 for 循环代码片段
    /// </summary>
    [Fact]
    public async Task TestForSnippet()
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
        var snippets = result.Items.Where(item => item.Kind == CompletionItemKind.Snippet).ToList();

        var forSnippet = snippets.FirstOrDefault(s => s.Label == "for");
        Assert.NotNull(forSnippet);

        // for 循环应该包含初始化、条件和递增部分
        Assert.Contains("for", forSnippet.InsertText!);
        Assert.Contains("<-", forSnippet.InsertText!); // 赋值运算符

        _output.WriteLine($"for 循环代码片段:");
        _output.WriteLine($"  InsertText: {forSnippet.InsertText}");
    }

    /// <summary>
    /// 测试 for-in 循环代码片段
    /// </summary>
    [Fact]
    public async Task TestForInSnippet()
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
        var snippets = result.Items.Where(item => item.Kind == CompletionItemKind.Snippet).ToList();

        var forInSnippet = snippets.FirstOrDefault(s => s.Label == "forin");
        Assert.NotNull(forInSnippet);

        Assert.Contains("for", forInSnippet.InsertText!);
        Assert.Contains("in", forInSnippet.InsertText!);

        _output.WriteLine($"for-in 循环代码片段:");
        _output.WriteLine($"  InsertText: {forInSnippet.InsertText}");
    }

    /// <summary>
    /// 测试 while 循环代码片段
    /// </summary>
    [Fact]
    public async Task TestWhileSnippet()
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
        var snippets = result.Items.Where(item => item.Kind == CompletionItemKind.Snippet).ToList();

        var whileSnippet = snippets.FirstOrDefault(s => s.Label == "while");
        Assert.NotNull(whileSnippet);

        Assert.Contains("while", whileSnippet.InsertText!);

        _output.WriteLine($"while 循环代码片段:");
        _output.WriteLine($"  InsertText: {whileSnippet.InsertText}");
    }

    /// <summary>
    /// 测试 try-catch 代码片段
    /// </summary>
    [Fact]
    public async Task TestTryCatchSnippet()
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
        var snippets = result.Items.Where(item => item.Kind == CompletionItemKind.Snippet).ToList();

        var trySnippet = snippets.FirstOrDefault(s => s.Label == "try");
        Assert.NotNull(trySnippet);

        Assert.Contains("try", trySnippet.InsertText!);
        Assert.Contains("catch", trySnippet.InsertText!);

        _output.WriteLine($"try-catch 代码片段:");
        _output.WriteLine($"  InsertText: {trySnippet.InsertText}");
    }

    /// <summary>
    /// 测试 switch 代码片段
    /// </summary>
    [Fact]
    public async Task TestSwitchSnippet()
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
        var snippets = result.Items.Where(item => item.Kind == CompletionItemKind.Snippet).ToList();

        var switchSnippet = snippets.FirstOrDefault(s => s.Label == "switch");
        Assert.NotNull(switchSnippet);

        Assert.Contains("switch", switchSnippet.InsertText!);
        Assert.Contains("case", switchSnippet.InsertText!);
        Assert.Contains("default", switchSnippet.InsertText!);

        _output.WriteLine($"switch 代码片段:");
        _output.WriteLine($"  InsertText: {switchSnippet.InsertText}");
    }

    /// <summary>
    /// 测试所有代码片段都存在
    /// </summary>
    [Fact]
    public async Task TestAllSnippetsPresent()
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
        var snippets = result.Items.Where(item => item.Kind == CompletionItemKind.Snippet).ToList();

        // 所有预期的代码片段
        var expectedSnippets = new[] {
            "func", "asyncfunc", "class", "if", "ifelse",
            "for", "forin", "while", "try", "switch"
        };

        _output.WriteLine($"总共应有 {expectedSnippets.Length} 个代码片段");
        _output.WriteLine($"实际找到 {snippets.Count} 个代码片段");

        var foundSnippets = snippets.Select(s => s.Label).ToHashSet();
        var missingSnippets = expectedSnippets.Where(s => !foundSnippets.Contains(s)).ToList();

        if (missingSnippets.Any())
        {
            _output.WriteLine("\n缺少的代码片段:");
            foreach (var missing in missingSnippets)
            {
                _output.WriteLine($"  - {missing}");
            }
        }

        // 验证所有代码片段都存在
        foreach (var snippet in expectedSnippets)
        {
            Assert.Contains(snippets, s => s.Label == snippet);
        }
    }

    /// <summary>
    /// 测试所有代码片段都使用了 Snippet 格式
    /// </summary>
    [Fact]
    public async Task TestAllSnippetsUseSnippetFormat()
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
        var snippets = result.Items.Where(item => item.Kind == CompletionItemKind.Snippet).ToList();

        foreach (var snippet in snippets)
        {
            Assert.Equal(InsertTextFormat.Snippet, snippet.InsertTextFormat);
            Assert.NotNull(snippet.InsertText);
            Assert.Contains("$", snippet.InsertText); // 应该包含占位符

            _output.WriteLine($"✓ {snippet.Label} 使用 Snippet 格式");
        }
    }
}
