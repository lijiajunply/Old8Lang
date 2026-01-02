using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Old8Lang.LanguageServer.Services;

namespace Old8Lang.LanguageServer.Handlers;

/// <summary>
/// 重命名处理器
/// </summary>
public class RenameHandler(DocumentManager documentManager) : IRenameHandler
{
    private readonly DocumentManager _documentManager = documentManager;

    public RenameRegistrationOptions GetRegistrationOptions(
        RenameCapability capability,
        ClientCapabilities clientCapabilities)
    {
        return new RenameRegistrationOptions
        {
            DocumentSelector = TextDocumentSelector.ForLanguage("old8lang"),
            PrepareProvider = false
        };
    }

    public Task<WorkspaceEdit?> Handle(RenameParams request, CancellationToken cancellationToken)
    {
        var uri = request.TextDocument.Uri.ToString();
        var document = _documentManager.GetDocument(uri);

        if (document?.SymbolTable == null || document.Tokens == null)
        {
            return Task.FromResult<WorkspaceEdit?>(null);
        }

        // 获取光标位置
        var line = request.Position.Line;
        var column = request.Position.Character;

        // 查找光标位置的符号
        var symbol = SymbolFinder.FindSymbolAtPosition(document, line, column);

        if (symbol == null)
        {
            return Task.FromResult<WorkspaceEdit?>(null);
        }

        // 查找该符号的所有引用（包括定义位置）
        var references = SymbolFinder.FindReferences(document, symbol.Name);

        if (references.Count == 0)
        {
            return Task.FromResult<WorkspaceEdit?>(null);
        }

        // 创建 WorkspaceEdit，包含所有需要修改的文本编辑
        var textEdits = new List<TextEdit>();

        foreach (var reference in references)
        {
            textEdits.Add(new TextEdit
            {
                Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range
                {
                    Start = new Position(reference.Line, reference.Column),
                    End = new Position(reference.EndLine, reference.EndColumn)
                },
                NewText = request.NewName
            });
        }

        // 创建 WorkspaceEdit
        var workspaceEdit = new WorkspaceEdit
        {
            Changes = new Dictionary<OmniSharp.Extensions.LanguageServer.Protocol.DocumentUri, IEnumerable<TextEdit>>
            {
                [OmniSharp.Extensions.LanguageServer.Protocol.DocumentUri.From(uri)] = textEdits
            }
        };

        return Task.FromResult<WorkspaceEdit?>(workspaceEdit);
    }
}
