using Old8Lang.LanguageServer.Handlers;
using Old8Lang.LanguageServer.Services;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit.Abstractions;

namespace Old8Lang.Tests.LanguageServer.Completion.Core;

/// <summary>
/// 关键字补全测试
/// 测试所有 Old8Lang 关键字的补全功能
/// </summary>
public class CompletionHandler_KeywordsTests(ITestOutputHelper output)
{
    /// <summary>
    /// 测试控制流关键字补全
    /// </summary>
    [Fact]
    public async Task TestControlFlowKeywords()
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

        // 控制流关键字：if, elif, else, for, while, switch, case, default
        var controlFlowKeywords = new[] { "if", "elif", "else", "for", "while", "switch", "case", "default" };

        foreach (var keyword in controlFlowKeywords)
        {
            Assert.Contains(keywords, item => item.Label == keyword);
            output.WriteLine($"✓ 找到关键字: {keyword}");
        }
    }

    /// <summary>
    /// 测试函数相关关键字补全
    /// </summary>
    [Fact]
    public async Task TestFunctionKeywords()
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

        // 函数关键字：func, return, yield
        var functionKeywords = new[] { "func", "return", "yield" };

        foreach (var keyword in functionKeywords)
        {
            Assert.Contains(keywords, item => item.Label == keyword);
            output.WriteLine($"✓ 找到关键字: {keyword}");
        }
    }

    /// <summary>
    /// 测试异步相关关键字补全
    /// </summary>
    [Fact]
    public async Task TestAsyncKeywords()
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

        // 异步关键字：async, await（注意：spawn 是内置函数，不是关键字）
        var asyncKeywords = new[] { "async", "await" };

        foreach (var keyword in asyncKeywords)
        {
            Assert.Contains(keywords, item => item.Label == keyword);
            output.WriteLine($"✓ 找到关键字: {keyword}");
        }
    }

    /// <summary>
    /// 测试面向对象关键字补全
    /// </summary>
    [Fact]
    public async Task TestOOPKeywords()
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

        // 面向对象关键字：class, interface, mixin, enum, extends, implements, with
        var oopKeywords = new[] { "class", "interface", "mixin", "enum", "extends", "implements", "with" };

        foreach (var keyword in oopKeywords)
        {
            Assert.Contains(keywords, item => item.Label == keyword);
            output.WriteLine($"✓ 找到关键字: {keyword}");
        }
    }

    /// <summary>
    /// 测试异常处理关键字补全
    /// </summary>
    [Fact]
    public async Task TestExceptionKeywords()
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

        // 异常处理关键字：try, catch, finally, throw
        var exceptionKeywords = new[] { "try", "catch", "finally", "throw" };

        foreach (var keyword in exceptionKeywords)
        {
            Assert.Contains(keywords, item => item.Label == keyword);
            output.WriteLine($"✓ 找到关键字: {keyword}");
        }
    }

    /// <summary>
    /// 测试导入相关关键字补全
    /// </summary>
    [Fact]
    public async Task TestImportKeywords()
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

        // 导入关键字：import, from, as, extern (native 已删除)
        var importKeywords = new[] { "import", "from", "as", "extern" };

        foreach (var keyword in importKeywords)
        {
            Assert.Contains(keywords, item => item.Label == keyword);
            output.WriteLine($"✓ 找到关键字: {keyword}");
        }
    }

    /// <summary>
    /// 测试逻辑运算符关键字补全
    /// </summary>
    [Fact]
    public async Task TestLogicalOperatorKeywords()
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

        // 逻辑运算符关键字：and, or, xor, not, in
        var logicalKeywords = new[] { "and", "or", "xor", "not", "in" };

        foreach (var keyword in logicalKeywords)
        {
            Assert.Contains(keywords, item => item.Label == keyword);
            output.WriteLine($"✓ 找到关键字: {keyword}");
        }
    }

    /// <summary>
    /// 测试访问修饰符关键字补全
    /// </summary>
    [Fact]
    public async Task TestAccessModifierKeywords()
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

        // 访问修饰符关键字：public, private, static
        var accessModifierKeywords = new[] { "public", "private", "static" };

        foreach (var keyword in accessModifierKeywords)
        {
            Assert.Contains(keywords, item => item.Label == keyword);
            output.WriteLine($"✓ 找到关键字: {keyword}");
        }
    }

    /// <summary>
    /// 测试其他特殊关键字补全
    /// </summary>
    [Fact]
    public async Task TestMiscellaneousKeywords()
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

        // 其他关键字：this, super, true, false, null, match, using, select, defer, break, continue
        var miscKeywords = new[] { "this", "super", "true", "false", "null", "match", "using", "select", "defer", "break", "continue" };

        foreach (var keyword in miscKeywords)
        {
            Assert.Contains(keywords, item => item.Label == keyword);
            output.WriteLine($"✓ 找到关键字: {keyword}");
        }
    }

    /// <summary>
    /// 测试所有关键字是否都存在（全面检查）
    /// </summary>
    [Fact]
    public async Task TestAllKeywordsPresent()
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

        // 所有 Old8Lang 关键字（根据 KeywordType 枚举和 Old8Lang_Grammar.md）
        // 注意：spawn 是内置函数，不是关键字
        var allKeywords = new[]
        {
            // 控制流
            "if", "elif", "else", "for", "while", "switch", "case", "default",
            // 函数
            "func", "async", "return", "break", "continue", "throw", "yield",
            // 面向对象
            "class", "enum", "mixin", "interface", "extends", "implements", "with",
            // 异常处理
            "try", "catch", "finally",
            // 导入
            "import", "from", "as", "extern",
            // 逻辑运算符
            "and", "or", "xor", "not", "in",
            // 访问修饰符
            "public", "private", "static",
            // 异步
            "await",
            // 其他
            "this", "super", "true", "false", "null", "match", "using", "select", "defer"
        };

        output.WriteLine($"总共应有 {allKeywords.Length} 个关键字");
        output.WriteLine($"实际找到 {keywords.Count} 个关键字补全项");

        var foundKeywords = keywords.Select(k => k.Label).ToHashSet();
        var missingKeywords = allKeywords.Where(k => !foundKeywords.Contains(k)).ToList();

        if (missingKeywords.Any())
        {
            output.WriteLine("\n缺少的关键字:");
            foreach (var missing in missingKeywords)
            {
                output.WriteLine($"  - {missing}");
            }
        }

        // 验证所有关键字都存在
        foreach (var keyword in allKeywords)
        {
            Assert.Contains(keywords, item => item.Label == keyword);
        }
    }

    /// <summary>
    /// 测试关键字补全的详细信息
    /// </summary>
    [Fact]
    public async Task TestKeywordCompletionDetails()
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

        // 验证关键字补全项的属性
        foreach (var keyword in keywords.Take(10)) // 只检查前10个，避免输出过多
        {
            Assert.NotNull(keyword.Label);
            Assert.Equal(CompletionItemKind.Keyword, keyword.Kind);
            Assert.NotNull(keyword.InsertText);
            Assert.Equal(keyword.Label, keyword.InsertText); // 关键字的插入文本应该就是关键字本身

            output.WriteLine($"关键字: {keyword.Label}");
            output.WriteLine($"  Kind: {keyword.Kind}");
            output.WriteLine($"  Detail: {keyword.Detail}");
            output.WriteLine($"  InsertText: {keyword.InsertText}");
            output.WriteLine("");
        }
    }
}
