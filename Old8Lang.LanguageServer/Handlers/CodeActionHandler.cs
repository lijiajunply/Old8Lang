using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Old8Lang.LanguageServer.Services;

namespace Old8Lang.LanguageServer.Handlers;

/// <summary>
/// 代码操作处理器 - 提供快速修复和重构建议
/// </summary>
public class CodeActionHandler(DocumentManager documentManager) : ICodeActionHandler
{
    public CodeActionRegistrationOptions GetRegistrationOptions(
        CodeActionCapability capability,
        ClientCapabilities clientCapabilities)
    {
        return new CodeActionRegistrationOptions
        {
            DocumentSelector = TextDocumentSelector.ForLanguage("old8lang"),
            CodeActionKinds = new[]
            {
                CodeActionKind.QuickFix,
                CodeActionKind.Refactor,
                CodeActionKind.RefactorExtract,
                CodeActionKind.RefactorInline,
                CodeActionKind.Source,
                CodeActionKind.SourceOrganizeImports
            }
        };
    }

    public Task<CommandOrCodeActionContainer> Handle(CodeActionParams request, CancellationToken cancellationToken)
    {
        var uri = request.TextDocument.Uri.ToString();
        var document = documentManager.GetDocument(uri);

        if (document == null)
        {
            return Task.FromResult(new CommandOrCodeActionContainer());
        }

        var codeActions = new List<CommandOrCodeAction>();

        // 遍历诊断信息，生成快速修复
        foreach (var diagnostic in request.Context.Diagnostics)
        {
            var fixes = GenerateQuickFixes(document, diagnostic);
            codeActions.AddRange(fixes);
        }

        // 添加重构操作（基于光标位置）
        var refactorings = GenerateRefactorings(document, request.Range);
        codeActions.AddRange(refactorings);

        return Task.FromResult(new CommandOrCodeActionContainer(codeActions));
    }

    /// <summary>
    /// 生成快速修复
    /// </summary>
    private List<CommandOrCodeAction> GenerateQuickFixes(Models.DocumentParseResult document, Diagnostic diagnostic)
    {
        var actions = new List<CommandOrCodeAction>();

        // 检查是否是"未定义的符号"错误
        if (diagnostic.Message.Contains("未定义的符号"))
        {
            var symbolName = ExtractSymbolName(diagnostic.Message);
            if (symbolName != null)
            {
                // 建议1: 定义变量
                actions.Add(new CommandOrCodeAction(new CodeAction
                {
                    Title = $"定义变量 '{symbolName}'",
                    Kind = CodeActionKind.QuickFix,
                    Diagnostics = new Container<Diagnostic>(diagnostic),
                    Edit = new WorkspaceEdit
                    {
                        Changes = new Dictionary<DocumentUri, IEnumerable<TextEdit>>
                        {
                            [DocumentUri.From(document.Uri)] = new[]
                            {
                                new TextEdit
                                {
                                    Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                                        new Position(diagnostic.Range.Start.Line, 0),
                                        new Position(diagnostic.Range.Start.Line, 0)
                                    ),
                                    NewText = $"{symbolName} <- null\n"
                                }
                            }
                        }
                    }
                }));

                // 建议2: 定义函数
                actions.Add(new CommandOrCodeAction(new CodeAction
                {
                    Title = $"定义函数 '{symbolName}'",
                    Kind = CodeActionKind.QuickFix,
                    Diagnostics = new Container<Diagnostic>(diagnostic),
                    Edit = new WorkspaceEdit
                    {
                        Changes = new Dictionary<DocumentUri, IEnumerable<TextEdit>>
                        {
                            [DocumentUri.From(document.Uri)] = new[]
                            {
                                new TextEdit
                                {
                                    Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                                        new Position(diagnostic.Range.Start.Line, 0),
                                        new Position(diagnostic.Range.Start.Line, 0)
                                    ),
                                    NewText = $"func {symbolName}() -> void {{\n\t// TODO: 实现\n}}\n\n"
                                }
                            }
                        }
                    }
                }));
            }
        }

        return actions;
    }

    /// <summary>
    /// 生成重构操作
    /// </summary>
    private List<CommandOrCodeAction> GenerateRefactorings(Models.DocumentParseResult document, OmniSharp.Extensions.LanguageServer.Protocol.Models.Range range)
    {
        var actions = new List<CommandOrCodeAction>();

        // 提取方法（如果选中了代码）
        if (range.Start.Line != range.End.Line || range.Start.Character != range.End.Character)
        {
            actions.Add(new CommandOrCodeAction(new CodeAction
            {
                Title = "提取为函数",
                Kind = CodeActionKind.RefactorExtract,
                Edit = CreateExtractFunctionEdit(document, range)
            }));
        }

        return actions;
    }

    /// <summary>
    /// 创建提取函数的编辑
    /// </summary>
    private WorkspaceEdit CreateExtractFunctionEdit(Models.DocumentParseResult document, OmniSharp.Extensions.LanguageServer.Protocol.Models.Range range)
    {
        var lines = document.Text.Split('\n');
        var startLine = (int)range.Start.Line;
        var endLine = (int)range.End.Line;

        // 提取选中的代码
        var selectedLines = new List<string>();
        for (int i = startLine; i <= endLine && i < lines.Length; i++)
        {
            var line = lines[i];
            if (i == startLine && i == endLine)
            {
                // 单行选择
                var start = (int)range.Start.Character;
                var end = (int)range.End.Character;
                selectedLines.Add(line.Substring(start, end - start));
            }
            else if (i == startLine)
            {
                selectedLines.Add(line.Substring((int)range.Start.Character));
            }
            else if (i == endLine)
            {
                selectedLines.Add(line.Substring(0, (int)range.End.Character));
            }
            else
            {
                selectedLines.Add(line);
            }
        }

        var extractedCode = string.Join("\n", selectedLines);

        // 生成新函数
        var newFunction = $"func extractedFunction() -> void {{\n\t{extractedCode.Replace("\n", "\n\t")}\n}}\n\n";

        return new WorkspaceEdit
        {
            Changes = new Dictionary<DocumentUri, IEnumerable<TextEdit>>
            {
                [DocumentUri.From(document.Uri)] = new[]
                {
                    // 插入新函数
                    new TextEdit
                    {
                        Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                            new Position(0, 0),
                            new Position(0, 0)
                        ),
                        NewText = newFunction
                    },
                    // 替换选中代码为函数调用
                    new TextEdit
                    {
                        Range = range,
                        NewText = "extractedFunction()"
                    }
                }
            }
        };
    }

    /// <summary>
    /// 从错误消息中提取符号名称
    /// </summary>
    private string? ExtractSymbolName(string message)
    {
        // 匹配格式: "未定义的符号 'symbolName'"
        var match = System.Text.RegularExpressions.Regex.Match(message, @"'([^']+)'");
        if (match.Success)
        {
            return match.Groups[1].Value;
        }
        return null;
    }
}
