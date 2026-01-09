using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Old8Lang.LanguageServer.Services;

namespace Old8Lang.LanguageServer.Handlers;

/// <summary>
/// 文档符号处理器 - 提供文档大纲视图
/// </summary>
public class DocumentSymbolHandler(DocumentManager documentManager) : IDocumentSymbolHandler
{
    public DocumentSymbolRegistrationOptions GetRegistrationOptions(
        DocumentSymbolCapability capability,
        ClientCapabilities clientCapabilities)
    {
        return new DocumentSymbolRegistrationOptions
        {
            DocumentSelector = TextDocumentSelector.ForLanguage("old8lang")
        };
    }

    public Task<SymbolInformationOrDocumentSymbolContainer> Handle(
        DocumentSymbolParams request,
        CancellationToken cancellationToken)
    {
        var uri = request.TextDocument.Uri.ToString();
        var document = documentManager.GetDocument(uri);

        if (document?.SymbolTable == null)
        {
            return Task.FromResult(new SymbolInformationOrDocumentSymbolContainer());
        }

        var symbols = new List<SymbolInformationOrDocumentSymbol>();

        // 遍历符号表，构建文档符号
        foreach (var (name, symbolInfo) in document.SymbolTable)
        {
            // 只显示顶层符号（排除成员）
            if (symbolInfo.Parent != null)
            {
                continue;
            }

            var documentSymbol = ConvertToDocumentSymbol(symbolInfo);
            symbols.Add(documentSymbol);
        }

        return Task.FromResult(new SymbolInformationOrDocumentSymbolContainer(symbols));
    }

    /// <summary>
    /// 将 SymbolInfo 转换为 DocumentSymbol
    /// </summary>
    private DocumentSymbol ConvertToDocumentSymbol(Models.SymbolInfo symbolInfo)
    {
        var symbol = new DocumentSymbol
        {
            Name = symbolInfo.Name,
            Kind = ConvertSymbolKind(symbolInfo.Kind),
            Detail = !string.IsNullOrEmpty(symbolInfo.Type) ? symbolInfo.Type : null,
            Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                new Position(symbolInfo.Location.Line, symbolInfo.Location.Column),
                new Position(symbolInfo.Location.EndLine, symbolInfo.Location.EndColumn)
            ),
            SelectionRange = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                new Position(symbolInfo.Location.Line, symbolInfo.Location.Column),
                new Position(symbolInfo.Location.Line, symbolInfo.Location.Column + symbolInfo.Name.Length)
            ),
            Children = symbolInfo.Members.Count > 0
                ? new Container<DocumentSymbol>(symbolInfo.Members.Values.Select(ConvertToDocumentSymbol).ToList())
                : null
        };

        return symbol;
    }

    /// <summary>
    /// 转换符号类型
    /// </summary>
    private SymbolKind ConvertSymbolKind(Models.SymbolKind kind)
    {
        return kind switch
        {
            Models.SymbolKind.Variable => SymbolKind.Variable,
            Models.SymbolKind.Function => SymbolKind.Function,
            Models.SymbolKind.Class => SymbolKind.Class,
            Models.SymbolKind.Method => SymbolKind.Method,
            Models.SymbolKind.Property => SymbolKind.Property,
            Models.SymbolKind.Parameter => SymbolKind.Variable,
            Models.SymbolKind.Constant => SymbolKind.Constant,
            _ => SymbolKind.Variable
        };
    }
}
