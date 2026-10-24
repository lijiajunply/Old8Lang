using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Old8Lang.LanguageServer.Services;

namespace Old8Lang.LanguageServer.Handlers;

/// <summary>
/// 选择范围处理器 - 返回 token、括号表达式、语句、声明块、文档逐级扩大的选择范围。
/// </summary>
public class SelectionRangeHandler(DocumentManager documentManager) : ISelectionRangeHandler
{
    public Task<Container<SelectionRange>?> Handle(SelectionRangeParams request, CancellationToken cancellationToken)
    {
        var document = documentManager.GetDocument(request.TextDocument.Uri.ToString());

        if (document?.Tokens == null)
        {
            return Task.FromResult<Container<SelectionRange>?>(new Container<SelectionRange>());
        }

        return Task.FromResult<Container<SelectionRange>?>(SelectionRangeBuilder.Build(document, request.Positions));
    }

    public SelectionRangeRegistrationOptions GetRegistrationOptions(
        SelectionRangeCapability capability,
        ClientCapabilities clientCapabilities)
    {
        return new SelectionRangeRegistrationOptions
        {
            DocumentSelector = TextDocumentSelector.ForLanguage("old8lang")
        };
    }
}
