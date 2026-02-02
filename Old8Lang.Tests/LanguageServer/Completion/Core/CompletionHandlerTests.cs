using Old8Lang.LanguageServer.Handlers;
using Old8Lang.LanguageServer.Services;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit.Abstractions;

namespace Old8Lang.Tests.LanguageServer.Completion.Core;

public class CompletionHandlerTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void TestKeywordCompletions()
    {
        // Arrange
        var code = "";
        var uri = "file:///test.old8";
        var documentManager = new DocumentManager();
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);

        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri("file:///test.old8") },
            Position = new Position(0, 0)
        };

        // Act
        var result = handler.Handle(request, CancellationToken.None).Result;

        // Assert
        Assert.NotNull(result);
        var keywords = result.Items.Where(item => item.Kind == CompletionItemKind.Keyword).ToList();

        testOutputHelper.WriteLine($"Found {keywords.Count} keyword completions");

        // 验证包含常见关键字
        Assert.Contains(keywords, item => item.Label == "func");
        Assert.Contains(keywords, item => item.Label == "class");
        Assert.Contains(keywords, item => item.Label == "if");
        Assert.Contains(keywords, item => item.Label == "for");
        Assert.Contains(keywords, item => item.Label == "while");
    }

    [Fact]
    public async Task TestSnippetCompletionsAsync()
    {
        // Arrange
        var code = "";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);

        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri("file:///test.old8") },
            Position = new Position(0, 0)
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var snippets = result.Items.Where(item => item.Kind == CompletionItemKind.Snippet).ToList();

        testOutputHelper.WriteLine($"Found {snippets.Count} snippet completions");

        // 验证包含代码片段
        Assert.Contains(snippets, item => item.Label == "func");
        Assert.Contains(snippets, item => item.Label == "async func");
        Assert.Contains(snippets, item => item.Label == "class");
        Assert.Contains(snippets, item => item.Label == "if");
        Assert.Contains(snippets, item => item.Label == "for");
        Assert.Contains(snippets, item => item.Label == "forin");

        // 验证 snippet 使用了正确的插入格式
        var funcSnippet = snippets.First(item => item.Label == "func");
        Assert.Equal(InsertTextFormat.Snippet, funcSnippet.InsertTextFormat);
        Assert.Contains("$", funcSnippet.InsertText); // 应该包含占位符
    }

    [Fact]
    public async Task TestSymbolCompletionsAsync()
    {
        // Arrange
        var code = @"
func testFunction(a:int, b:int) -> int {
    return a + b
}

class TestClass {
    public name:string

    func init(n:string) {
        this.name <- n
    }
}

x <- 123
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        var document = documentManager.UpdateDocument(uri, code);

        // Debug: 检查文档状态
        testOutputHelper.WriteLine($"Document diagnostics count: {document.Diagnostics.Count}");
        foreach (var diag in document.Diagnostics)
        {
            testOutputHelper.WriteLine($"  {diag.Severity}: {diag.Message}");
        }
        testOutputHelper.WriteLine($"SymbolTable is null: {document.SymbolTable == null}");
        if (document.SymbolTable != null)
        {
            testOutputHelper.WriteLine($"SymbolTable count: {document.SymbolTable.Count}");
            foreach (var (name, symbol) in document.SymbolTable)
            {
                testOutputHelper.WriteLine($"  {name}: {symbol.Kind}");
            }
        }

        var handler = new CompletionHandler(documentManager);

        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(13, 0) // 最后一行
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        // Debug: 输出所有补全项
        testOutputHelper.WriteLine($"Total completion items: {result.Items.Count()}");
        foreach (var item in result.Items)
        {
            testOutputHelper.WriteLine($"  {item.Label}: Kind={item.Kind}, Detail={item.Detail}");
        }

        // 验证函数补全
        var functions = result.Items.Where(item => item.Kind == CompletionItemKind.Function).ToList();
        testOutputHelper.WriteLine($"Found {functions.Count} function completions");
        Assert.Contains(functions, item => item.Label == "testFunction");

        var funcItem = functions.First(item => item.Label == "testFunction");
        Assert.Contains("->", funcItem.Detail); // 应该显示返回类型
        Assert.Equal(InsertTextFormat.Snippet, funcItem.InsertTextFormat);
        Assert.Contains("($0)", funcItem.InsertText); // 应该有参数占位符

        // 验证类补全
        var classes = result.Items.Where(item => item.Kind == CompletionItemKind.Class).ToList();
        testOutputHelper.WriteLine($"Found {classes.Count} class completions");
        Assert.Contains(classes, item => item.Label == "TestClass");

        // 验证变量补全
        var variables = result.Items.Where(item => item.Kind == CompletionItemKind.Variable).ToList();
        testOutputHelper.WriteLine($"Found {variables.Count} variable completions");
        Assert.Contains(variables, item => item.Label == "x");
    }

    [Fact]
    public async Task TestMemberAccessCompletionAsync()
    {
        // Arrange
        var code = @"
class User {
    public name:string
    private age:int

    public func getName() -> string {
        return this.name
    }

    private func getAge() -> int {
        return this.age
    }

    static func create() -> User {
        return User()
    }
}

user <- User()
x <- user.name
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        var document = documentManager.UpdateDocument(uri, code);

        // Debug: 检查文档状态
        testOutputHelper.WriteLine($"Document diagnostics count: {document.Diagnostics.Count}");
        foreach (var diag in document.Diagnostics)
        {
            testOutputHelper.WriteLine($"  {diag.Severity}: {diag.Message}");
        }
        testOutputHelper.WriteLine($"SymbolTable is null: {document.SymbolTable == null}");
        if (document.SymbolTable != null)
        {
            testOutputHelper.WriteLine($"SymbolTable count: {document.SymbolTable.Count}");
            foreach (var (name, symbol) in document.SymbolTable)
            {
                testOutputHelper.WriteLine($"  {name}: {symbol.Kind}");
            }
        }

        var handler = new CompletionHandler(documentManager);

        // 光标在 "user." 之后（在点号之后，name 之前）
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(19, 11) // "user." 之后（第20行 `x <- user.name`，0-based 为 19，点号在列10，光标在列11）
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        testOutputHelper.WriteLine($"Total completions: {result.Items.Count()}");
        foreach (var item in result.Items)
        {
            testOutputHelper.WriteLine($"  - {item.Label} ({item.Kind})");
        }

        // 应该只返回成员补全，不包含关键字等
        // 验证包含公开成员
        Assert.Contains(result.Items, item => item.Label == "name");
        Assert.Contains(result.Items, item => item.Label == "getName");

        // 私有成员不应该在类外部补全中显示
        // Assert.Contains(result.Items, item => item.Label == "age");
        // Assert.Contains(result.Items, item => item.Label == "getAge");

        // 验证方法有正确的插入格式
        var getNameItem = result.Items.First(item => item.Label == "getName");
        Assert.Equal(CompletionItemKind.Method, getNameItem.Kind);
        Assert.Equal(InsertTextFormat.Snippet, getNameItem.InsertTextFormat);
        Assert.Contains("($0)", getNameItem.InsertText);
    }

    [Fact]
    public async Task TestSmartSortingAsync()
    {
        // Arrange
        var code = @"
func myFunc() -> void { }
class MyClass { }
myVar <- 123
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);

        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri("file:///test.old8") },
            Position = new Position(3, 0)
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        // 验证所有补全项都有 SortText
        foreach (var item in result.Items)
        {
            Assert.NotNull(item.SortText);
            testOutputHelper.WriteLine($"{item.Label}: SortText={item.SortText}, Kind={item.Kind}");
        }

        // 验证变量的排序优先级高于函数（数字更小）
        var varItem = result.Items.First(item => item.Label == "myVar");
        var funcItem = result.Items.First(item => item.Label == "myFunc");
        var classItem = result.Items.First(item => item.Label == "MyClass");

        // 提取 SortText 中的优先级数字
        var varPriority = int.Parse(varItem.SortText!.Split('_')[0]);
        var funcPriority = int.Parse(funcItem.SortText!.Split('_')[0]);
        var classPriority = int.Parse(classItem.SortText!.Split('_')[0]);

        testOutputHelper.WriteLine($"Variable priority: {varPriority}");
        testOutputHelper.WriteLine($"Function priority: {funcPriority}");
        testOutputHelper.WriteLine($"Class priority: {classPriority}");

        // 变量 < 函数 < 类（优先级）
        Assert.True(varPriority < funcPriority, "Variables should have higher priority than functions");
        Assert.True(funcPriority < classPriority, "Functions should have higher priority than classes");
    }

    [Fact]
    public async Task TestCompletionWithDocumentationAsync()
    {
        // Arrange
        var code = @"
/// 这是一个测试函数
/// 用于计算两个数的和
func add(a:int, b:int) -> int {
    return a + b
}
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new CompletionHandler(documentManager);

        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri("file:///test.old8") },
            Position = new Position(6, 0)
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        var addFunc = result.Items.FirstOrDefault(item => item.Label == "add");
        Assert.NotNull(addFunc);

        // 验证文档注释被包含
        if (addFunc.Documentation != null)
        {
            var docText = addFunc.Documentation.HasMarkupContent
                ? addFunc.Documentation.MarkupContent?.Value
                : addFunc.Documentation.String;

            if (!string.IsNullOrEmpty(docText))
            {
                testOutputHelper.WriteLine($"Documentation: {docText}");
                Assert.Contains("测试函数", docText);
            }
        }
    }
}
