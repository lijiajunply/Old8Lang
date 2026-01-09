using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Old8Lang.LanguageServer.Services;

namespace Old8Lang.LanguageServer.Handlers;

/// <summary>
/// 文档格式化处理器 - 提供代码格式化功能
/// </summary>
public class DocumentFormattingHandler : IDocumentFormattingHandler, IDocumentRangeFormattingHandler
{
    private readonly DocumentManager _documentManager;
    private readonly FormattingService _formattingService;

    public DocumentFormattingHandler(DocumentManager documentManager, FormattingService formattingService)
    {
        _documentManager = documentManager;
        _formattingService = formattingService;
    }

    public DocumentFormattingRegistrationOptions GetRegistrationOptions(
        DocumentFormattingCapability capability,
        ClientCapabilities clientCapabilities)
    {
        return new DocumentFormattingRegistrationOptions
        {
            DocumentSelector = TextDocumentSelector.ForLanguage("old8lang")
        };
    }

    public DocumentRangeFormattingRegistrationOptions GetRegistrationOptions(
        DocumentRangeFormattingCapability capability,
        ClientCapabilities clientCapabilities)
    {
        return new DocumentRangeFormattingRegistrationOptions
        {
            DocumentSelector = TextDocumentSelector.ForLanguage("old8lang")
        };
    }

    public Task<TextEditContainer?> Handle(DocumentFormattingParams request, CancellationToken cancellationToken)
    {
        var uri = request.TextDocument.Uri.ToString();
        var document = _documentManager.GetDocument(uri);

        if (document == null)
        {
            return Task.FromResult<TextEditContainer?>(null);
        }

        var edits = _formattingService.FormatDocument(document.Text, request.Options);

        return Task.FromResult<TextEditContainer?>(new TextEditContainer(edits));
    }

    public Task<TextEditContainer?> Handle(DocumentRangeFormattingParams request, CancellationToken cancellationToken)
    {
        var uri = request.TextDocument.Uri.ToString();
        var document = _documentManager.GetDocument(uri);

        if (document == null)
        {
            return Task.FromResult<TextEditContainer?>(null);
        }

        var edits = _formattingService.FormatRange(document.Text, request.Range, request.Options);

        return Task.FromResult<TextEditContainer?>(new TextEditContainer(edits));
    }
}
