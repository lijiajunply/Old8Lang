using Old8Lang.LanguageServer.Handlers;
using Old8Lang.LanguageServer.Services;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit.Abstractions;

namespace Old8Lang.Tests.LanguageServer.Completion.Core;

/// <summary>
/// 类型补全测试
/// 测试所有 Old8Lang 类型的补全功能
/// </summary>
public class CompletionHandler_TypesTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    /// <summary>
    /// 测试基本类型关键字补全
    /// </summary>
    [Fact]
    public async Task TestBasicTypeKeywords()
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

        // 基本类型关键字：int, double, string, char, bool, void
        var basicTypes = new[] { "int", "double", "string", "char", "bool", "void" };

        foreach (var type in basicTypes)
        {
            Assert.Contains(keywords, item => item.Label == type);
            _output.WriteLine($"✓ 找到类型关键字: {type}");
        }
    }

    /// <summary>
    /// 测试变量类型注解的补全
    /// </summary>
    [Fact]
    public async Task TestVariableTypeAnnotationCompletion()
    {
        // Arrange
        var code = @"
x:
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(1, 2) // 在 "x:" 之后
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        // 应该包含类型关键字
        var typeKeywords = result.Items.Where(item =>
            item.Kind == CompletionItemKind.Keyword &&
            (item.Label == "int" || item.Label == "double" || item.Label == "string" ||
             item.Label == "bool" || item.Label == "char")).ToList();

        Assert.NotEmpty(typeKeywords);

        foreach (var type in typeKeywords)
        {
            _output.WriteLine($"✓ 类型注解补全: {type.Label}");
        }
    }

    /// <summary>
    /// 测试函数参数类型补全
    /// </summary>
    [Fact]
    public async Task TestFunctionParameterTypeCompletion()
    {
        // Arrange
        var code = @"
func test(param:
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(1, 16) // 在 "param:" 之后
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        var typeKeywords = result.Items.Where(item =>
            item.Kind == CompletionItemKind.Keyword).ToList();

        Assert.NotEmpty(typeKeywords);

        _output.WriteLine($"找到 {typeKeywords.Count} 个类型关键字");
    }

    /// <summary>
    /// 测试函数返回类型补全
    /// </summary>
    [Fact]
    public async Task TestFunctionReturnTypeCompletion()
    {
        // Arrange
        var code = @"
func test() ->
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(1, 14) // 在 "->" 之后
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        var typeKeywords = result.Items.Where(item =>
            item.Kind == CompletionItemKind.Keyword).ToList();

        // 应该包含 void 以及其他类型
        Assert.Contains(typeKeywords, item => item.Label == "void");

        _output.WriteLine($"找到 {typeKeywords.Count} 个返回类型关键字");
    }

    /// <summary>
    /// 测试可空类型补全
    /// </summary>
    [Fact]
    public async Task TestNullableTypeCompletion()
    {
        // Arrange - 测试在类型后面输入 ? 的场景
        var code = @"
x:int
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(1, 5) // 在 "int" 之后
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        _output.WriteLine($"可空类型场景补全项数量: {result.Items.Count()}");
    }

    /// <summary>
    /// 测试类类型补全
    /// </summary>
    [Fact]
    public async Task TestClassTypeCompletion()
    {
        // Arrange
        var code = @"
class MyClass {
    public value <- 0
}

func test() -> void {
    obj:M
}
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(6, 9) // 在 "    obj:M" 之后，M的后面
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        _output.WriteLine($"总补全项数量: {result.Items.Count()}");

        var doc = documentManager.GetDocument(uri);
        _output.WriteLine($"符号表项数量: {doc?.SymbolTable?.Count ?? 0}");

        // 打印诊断信息
        if (doc?.Diagnostics != null && doc.Diagnostics.Any())
        {
            _output.WriteLine("\n诊断信息:");
            foreach (var diag in doc.Diagnostics)
            {
                _output.WriteLine($"  [{diag.Severity}] Line {diag.Line}: {diag.Message}");
            }
        }

        // 打印符号表内容
        if (doc?.SymbolTable != null)
        {
            _output.WriteLine("\n符号表内容:");
            foreach (var symbol in doc.SymbolTable)
            {
                _output.WriteLine($"  {symbol.Key}: Kind={symbol.Value.Kind}, Type={symbol.Value.Type}");
            }
        }

        // 应该包含自定义类 MyClass
        var classCompletions = result.Items.Where(item =>
            item.Kind == CompletionItemKind.Class).ToList();

        _output.WriteLine($"\n找到 {classCompletions.Count} 个类类型补全");
        foreach (var cls in classCompletions)
        {
            _output.WriteLine($"  - {cls.Label}");
        }

        Assert.Contains(classCompletions, item => item.Label == "MyClass");
    }

    /// <summary>
    /// 测试泛型集合类型补全（list, array, dict）
    /// </summary>
    [Fact]
    public async Task TestGenericCollectionTypeCompletion()
    {
        // Arrange
        var code = @"
items:
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(1, 6) // 在 "items:" 之后
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        _output.WriteLine($"泛型集合类型场景补全项数量: {result.Items.Count()}");

        // 注意: 泛型类型语法如 list<int> 可能需要特殊处理
        // 这里只是验证补全系统能够正常工作
    }

    /// <summary>
    /// 测试类型转换场景的补全
    /// </summary>
    [Fact]
    public async Task TestTypeConversionCompletion()
    {
        // Arrange
        var code = @"
x <- 123
y <- x as
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(2, 9) // 在 "as" 之后
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        var typeKeywords = result.Items.Where(item =>
            item.Kind == CompletionItemKind.Keyword).ToList();

        // 应该包含可转换的类型
        Assert.Contains(typeKeywords, item => item.Label == "int");
        Assert.Contains(typeKeywords, item => item.Label == "double");
        Assert.Contains(typeKeywords, item => item.Label == "string");

        _output.WriteLine($"类型转换场景找到 {typeKeywords.Count} 个类型关键字");
    }

    /// <summary>
    /// 测试所有类型关键字的完整性
    /// </summary>
    [Fact]
    public async Task TestAllTypeKeywordsPresent()
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

        // 所有类型关键字（根据 Old8Lang_Grammar.md）
        var allTypeKeywords = new[] { "int", "double", "string", "bool", "char", "void", "var" };

        _output.WriteLine($"总共应有 {allTypeKeywords.Length} 个类型关键字");

        var foundTypeKeywords = keywords.Select(k => k.Label).ToHashSet();
        var missingTypeKeywords = allTypeKeywords.Where(k => !foundTypeKeywords.Contains(k)).ToList();

        if (missingTypeKeywords.Any())
        {
            _output.WriteLine("\n缺少的类型关键字:");
            foreach (var missing in missingTypeKeywords)
            {
                _output.WriteLine($"  - {missing}");
            }
        }

        // 验证所有类型关键字都存在
        foreach (var typeKeyword in allTypeKeywords)
        {
            Assert.Contains(keywords, item => item.Label == typeKeyword);
        }
    }

    /// <summary>
    /// 测试接口类型补全
    /// </summary>
    [Fact]
    public async Task TestInterfaceTypeCompletion()
    {
        // Arrange
        var code = @"
interface IDrawable {
    func draw() -> void
}

class MyClass implements
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(5, 24) // 在 "implements" 之后
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        // 应该包含接口 IDrawable
        var interfaceCompletions = result.Items.Where(item =>
            item.Label == "IDrawable").ToList();

        if (interfaceCompletions.Any())
        {
            _output.WriteLine("✓ 找到接口类型补全");
        }
    }

    /// <summary>
    /// 测试枚举类型补全
    /// </summary>
    [Fact]
    public async Task TestEnumTypeCompletion()
    {
        // Arrange
        var code = @"
enum Color {
    Red,
    Green,
    Blue
}

myColor:
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(7, 8) // 在 "myColor:" 之后
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        _output.WriteLine($"枚举类型场景补全项数量: {result.Items.Count()}");
    }
}
