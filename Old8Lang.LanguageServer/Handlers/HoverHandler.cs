using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Old8Lang.LanguageServer.Services;

namespace Old8Lang.LanguageServer.Handlers;

/// <summary>
/// 悬停提示处理器
/// </summary>
public class HoverHandler : IHoverHandler
{
    private readonly DocumentManager _documentManager;

    public HoverHandler(DocumentManager documentManager)
    {
        _documentManager = documentManager;
    }

    public HoverRegistrationOptions GetRegistrationOptions(
        HoverCapability capability,
        ClientCapabilities clientCapabilities)
    {
        return new HoverRegistrationOptions
        {
            DocumentSelector = TextDocumentSelector.ForLanguage("old8lang")
        };
    }

    public Task<Hover?> Handle(HoverParams request, CancellationToken cancellationToken)
    {
        var uri = request.TextDocument.Uri.ToString();
        var document = _documentManager.GetDocument(uri);

        if (document?.SymbolTable == null)
        {
            return Task.FromResult<Hover?>(null);
        }

        // TODO: 实现根据位置查找符号并返回悬停信息的逻辑

        return Task.FromResult<Hover?>(null);
    }
}