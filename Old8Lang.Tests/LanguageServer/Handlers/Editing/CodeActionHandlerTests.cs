using Old8Lang.LanguageServer.Handlers;
using Old8Lang.LanguageServer.Services;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit.Abstractions;

namespace Old8Lang.Tests.LanguageServer.Handlers.Editing;

/// <summary>
/// 测试 CodeActionHandler - 快速修复和重构功能
/// </summary>
public class CodeActionHandlerTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task TestQuickFix_DefineVariable()
    {
        // Arrange
        var code = "result <- unknownVar + 10";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new CodeActionHandler(documentManager);

        // 模拟诊断错误
        var diagnostic = new Diagnostic
        {
            Message = "未定义的符号 'unknownVar'",
            Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                new Position(0, 10),
                new Position(0, 20)
            ),
            Severity = DiagnosticSeverity.Error
        };

        var request = new CodeActionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = DocumentUri.From(uri) },
            Range = diagnostic.Range,
            Context = new CodeActionContext
            {
                Diagnostics = new Container<Diagnostic>(diagnostic)
            }
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var codeActions = result.Where(ca => ca.IsCodeAction).Select(ca => ca.CodeAction!).ToList();

        testOutputHelper.WriteLine($"Found {codeActions.Count} code actions");

        foreach (var action in codeActions)
        {
            testOutputHelper.WriteLine($"Action: {action.Title} ({action.Kind})");
        }

        // 应该有定义变量的快速修复
        Assert.Contains(codeActions, ca =>
            ca.Title.Contains("定义变量") &&
            ca.Title.Contains("unknownVar") &&
            ca.Kind == CodeActionKind.QuickFix
        );
    }

    [Fact]
    public async Task TestQuickFix_DefineFunction()
    {
        // Arrange
        var code = "result <- calculate(10, 20)";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new CodeActionHandler(documentManager);

        var diagnostic = new Diagnostic
        {
            Message = "未定义的符号 'calculate'",
            Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                new Position(0, 10),
                new Position(0, 19)
            ),
            Severity = DiagnosticSeverity.Error
        };

        var request = new CodeActionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = DocumentUri.From(uri) },
            Range = diagnostic.Range,
            Context = new CodeActionContext
            {
                Diagnostics = new Container<Diagnostic>(diagnostic)
            }
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var codeActions = result.Where(ca => ca.IsCodeAction).Select(ca => ca.CodeAction!).ToList();

        testOutputHelper.WriteLine($"Found {codeActions.Count} code actions");

        foreach (var action in codeActions)
        {
            testOutputHelper.WriteLine($"Action: {action.Title} ({action.Kind})");
        }

        // 应该有定义函数的快速修复
        Assert.Contains(codeActions, ca =>
            ca.Title.Contains("定义函数") &&
            ca.Title.Contains("calculate") &&
            ca.Kind == CodeActionKind.QuickFix
        );
    }

    [Fact]
    public async Task TestQuickFix_BothVariableAndFunction()
    {
        // Arrange
        var code = "x <- mySymbol";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new CodeActionHandler(documentManager);

        var diagnostic = new Diagnostic
        {
            Message = "未定义的符号 'mySymbol'",
            Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                new Position(0, 5),
                new Position(0, 13)
            ),
            Severity = DiagnosticSeverity.Error
        };

        var request = new CodeActionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = DocumentUri.From(uri) },
            Range = diagnostic.Range,
            Context = new CodeActionContext
            {
                Diagnostics = new Container<Diagnostic>(diagnostic)
            }
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var codeActions = result.Where(ca => ca.IsCodeAction).Select(ca => ca.CodeAction!).ToList();

        testOutputHelper.WriteLine($"Found {codeActions.Count} code actions");

        // 应该同时包含定义变量和定义函数的选项
        Assert.Contains(codeActions, ca => ca.Title.Contains("定义变量"));
        Assert.Contains(codeActions, ca => ca.Title.Contains("定义函数"));

        // 至少应该有 2 个快速修复
        Assert.True(codeActions.Count >= 2);
    }

    [Fact]
    public async Task TestRefactoring_ExtractFunction()
    {
        // Arrange
        var code = @"
a <- 10
b <- 20
c <- a + b
PrintLine(c)
";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new CodeActionHandler(documentManager);

        // 选中要提取的代码（第 3、4 行）
        var request = new CodeActionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = DocumentUri.From(uri) },
            Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                new Position(2, 0),
                new Position(3, 10)
            ),
            Context = new CodeActionContext
            {
                Diagnostics = new Container<Diagnostic>()
            }
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var codeActions = result.Where(ca => ca.IsCodeAction).Select(ca => ca.CodeAction!).ToList();

        testOutputHelper.WriteLine($"Found {codeActions.Count} code actions");

        foreach (var action in codeActions)
        {
            testOutputHelper.WriteLine($"Action: {action.Title} ({action.Kind})");
        }

        // 应该有提取函数的重构操作
        Assert.Contains(codeActions, ca =>
            ca.Title.Contains("提取为函数") &&
            ca.Kind == CodeActionKind.RefactorExtract
        );
    }

    [Fact]
    public async Task TestRefactoring_NoExtractForEmptySelection()
    {
        // Arrange
        var code = "a <- 10";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new CodeActionHandler(documentManager);

        // 空选择（光标位置）
        var request = new CodeActionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = DocumentUri.From(uri) },
            Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                new Position(0, 5),
                new Position(0, 5) // 相同的位置，表示没有选择
            ),
            Context = new CodeActionContext
            {
                Diagnostics = new Container<Diagnostic>()
            }
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var codeActions = result.Where(ca => ca.IsCodeAction).Select(ca => ca.CodeAction!).ToList();

        testOutputHelper.WriteLine($"Found {codeActions.Count} code actions");

        // 空选择不应该有提取函数的选项
        Assert.DoesNotContain(codeActions, ca => ca.Title.Contains("提取为函数"));
    }

    [Fact]
    public async Task TestNoCodeActions_WithoutDiagnostics()
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

        var handler = new CodeActionHandler(documentManager);

        var request = new CodeActionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = DocumentUri.From(uri) },
            Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                new Position(2, 0),
                new Position(2, 0)
            ),
            Context = new CodeActionContext
            {
                Diagnostics = new Container<Diagnostic>() // 没有诊断
            }
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        var quickFixes = result
            .Where(ca => ca.IsCodeAction)
            .Select(ca => ca.CodeAction!)
            .Where(ca => ca.Kind == CodeActionKind.QuickFix)
            .ToList();

        testOutputHelper.WriteLine($"Found {quickFixes.Count} quick fixes without diagnostics");

        // 没有诊断就不应该有快速修复
        Assert.Empty(quickFixes);
    }

    [Fact]
    public async Task TestCodeActionEdit_DefineVariableFormat()
    {
        // Arrange
        var code = "result <- unknownVar";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new CodeActionHandler(documentManager);

        var diagnostic = new Diagnostic
        {
            Message = "未定义的符号 'unknownVar'",
            Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                new Position(0, 10),
                new Position(0, 20)
            ),
            Severity = DiagnosticSeverity.Error
        };

        var request = new CodeActionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = DocumentUri.From(uri) },
            Range = diagnostic.Range,
            Context = new CodeActionContext
            {
                Diagnostics = new Container<Diagnostic>(diagnostic)
            }
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        var defineVarAction = result
            .Where(ca => ca.IsCodeAction)
            .Select(ca => ca.CodeAction!)
            .FirstOrDefault(ca => ca.Title.Contains("定义变量"));

        Assert.NotNull(defineVarAction);
        Assert.NotNull(defineVarAction.Edit);
        Assert.NotNull(defineVarAction.Edit.Changes);

        testOutputHelper.WriteLine($"Edit changes count: {defineVarAction.Edit.Changes.Count}");

        var changes = defineVarAction.Edit.Changes.First().Value.ToList();
        Assert.NotEmpty(changes);

        var textEdit = changes.First();
        testOutputHelper.WriteLine($"New text: {textEdit.NewText}");

        // 新文本应该包含变量名和赋值
        Assert.Contains("unknownVar", textEdit.NewText);
        Assert.Contains("<-", textEdit.NewText);
    }

    [Fact]
    public async Task TestRegistrationOptions()
    {
        // Arrange
        var documentManager = new DocumentManager();
        var handler = new CodeActionHandler(documentManager);

        // Act
        var options = handler.GetRegistrationOptions(
            new CodeActionCapability(),
            new ClientCapabilities()
        );

        // Assert
        Assert.NotNull(options);
        Assert.NotNull(options.CodeActionKinds);

        testOutputHelper.WriteLine($"Supported code action kinds:");
        foreach (var kind in options.CodeActionKinds)
        {
            testOutputHelper.WriteLine($"  - {kind}");
        }

        // 应该支持多种代码操作类型
        Assert.Contains(CodeActionKind.QuickFix, options.CodeActionKinds);
        Assert.Contains(CodeActionKind.Refactor, options.CodeActionKinds);
        Assert.Contains(CodeActionKind.RefactorExtract, options.CodeActionKinds);
    }

    [Fact]
    public async Task TestMultipleDiagnostics_MultipleQuickFixes()
    {
        // Arrange
        var code = "result <- unknownA + unknownB";
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);

        var handler = new CodeActionHandler(documentManager);

        var diagnostics = new[]
        {
            new Diagnostic
            {
                Message = "未定义的符号 'unknownA'",
                Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                    new Position(0, 10),
                    new Position(0, 18)
                ),
                Severity = DiagnosticSeverity.Error
            },
            new Diagnostic
            {
                Message = "未定义的符号 'unknownB'",
                Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                    new Position(0, 21),
                    new Position(0, 29)
                ),
                Severity = DiagnosticSeverity.Error
            }
        };

        var request = new CodeActionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = DocumentUri.From(uri) },
            Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                new Position(0, 0),
                new Position(0, 29)
            ),
            Context = new CodeActionContext
            {
                Diagnostics = new Container<Diagnostic>(diagnostics)
            }
        };

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var codeActions = result.Where(ca => ca.IsCodeAction).Select(ca => ca.CodeAction!).ToList();

        testOutputHelper.WriteLine($"Found {codeActions.Count} code actions for multiple diagnostics");

        foreach (var action in codeActions)
        {
            testOutputHelper.WriteLine($"Action: {action.Title}");
        }

        // 应该为每个未定义的符号生成快速修复
        Assert.Contains(codeActions, ca => ca.Title.Contains("unknownA"));
        Assert.Contains(codeActions, ca => ca.Title.Contains("unknownB"));
    }
}
