using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Old8Lang.LanguageServer.Services;

namespace Old8Lang.LanguageServer.Handlers;

/// <summary>
/// 重命名处理器 - 顶层符号支持跨文件重命名
/// </summary>
public class RenameHandler(DocumentManager documentManager, WorkspaceIndex? workspaceIndex = null) : IRenameHandler
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

        var line = (int)request.Position.Line;
        var column = (int)request.Position.Character;
        var effectiveWorkspace = workspaceIndex ?? _documentManager.WorkspaceIndex;

        var symbol = SymbolFinder.FindSymbolAtPosition(document, line, column, effectiveWorkspace);
        var symbolName = symbol?.Name ?? SymbolFinder.FindIdentifierNameAtPosition(document, line, column);

        if (symbolName == null)
        {
            return Task.FromResult<WorkspaceEdit?>(null);
        }

        // 顶层符号跨文件重命名；局部变量/成员/参数保持作用域内精确重命名，避免误改同名符号
        if (effectiveWorkspace != null &&
            (symbol == null || symbol.IsTopLevel) &&
            effectiveWorkspace.TryResolveSymbol(symbolName, uri, out var workspaceSymbol, out var definitionFile) &&
            workspaceSymbol != null && definitionFile != null &&
            (symbol == null || ReferencesHandler.IsSameDefinition(symbol, workspaceSymbol)))
        {
            var grouped = effectiveWorkspace.FindRenameLocations(symbolName, definitionFile.Uri);
            if (grouped.Count == 0)
            {
                return Task.FromResult<WorkspaceEdit?>(null);
            }

            var changes = new Dictionary<DocumentUri, IEnumerable<TextEdit>>();
            foreach (var (fileUri, locations) in grouped)
            {
                changes[DocumentUri.From(fileUri)] = locations.Select(location => new TextEdit
                {
                    Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range
                    {
                        Start = new Position(location.Line, location.Column),
                        End = new Position(location.EndLine, location.EndColumn)
                    },
                    NewText = request.NewName
                }).ToList();
            }

            return Task.FromResult<WorkspaceEdit?>(new WorkspaceEdit { Changes = changes });
        }

        if (symbol == null)
        {
            return Task.FromResult<WorkspaceEdit?>(null);
        }

        var references = SymbolFinder.FindScopedReferences(document, symbol, effectiveWorkspace);
        if (references.Count == 0)
        {
            return Task.FromResult<WorkspaceEdit?>(null);
        }

        var textEdits = references.Select(reference => new TextEdit
        {
            Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range
            {
                Start = new Position(reference.Line, reference.Column),
                End = new Position(reference.EndLine, reference.EndColumn)
            },
            NewText = request.NewName
        }).ToList();

        var workspaceEdit = new WorkspaceEdit
        {
            Changes = new Dictionary<DocumentUri, IEnumerable<TextEdit>>
            {
                [DocumentUri.From(uri)] = textEdits
            }
        };

        return Task.FromResult<WorkspaceEdit?>(workspaceEdit);
    }
}
