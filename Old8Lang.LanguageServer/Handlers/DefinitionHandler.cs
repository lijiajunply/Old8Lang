using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Old8Lang.LanguageServer.Services;

namespace Old8Lang.LanguageServer.Handlers;

/// <summary>
/// 跳转定义处理器
/// </summary>
public class DefinitionHandler : IDefinitionHandler
{
    private readonly DocumentManager _documentManager;

    public DefinitionHandler(DocumentManager documentManager)
    {
        _documentManager = documentManager;
    }

    public DefinitionRegistrationOptions GetRegistrationOptions(
        DefinitionCapability capability,
        ClientCapabilities clientCapabilities)
    {
        return new DefinitionRegistrationOptions
        {
            DocumentSelector = TextDocumentSelector.ForLanguage("old8lang")
        };
    }

    public Task<LocationOrLocationLinks?> Handle(DefinitionParams request, CancellationToken cancellationToken)
    {
        var uri = request.TextDocument.Uri.ToString();
        var document = _documentManager.GetDocument(uri);

        if (document?.SymbolTable == null)
        {
            return Task.FromResult<LocationOrLocationLinks?>(new LocationOrLocationLinks());
        }

        // TODO: 实现根据位置查找符号定义的逻辑
        var locations = new List<LocationOrLocationLink>();

        return Task.FromResult<LocationOrLocationLinks?>(new LocationOrLocationLinks(locations));
    }
}

/// <summary>
/// 查找引用处理器
/// </summary>
public class ReferencesHandler : IReferencesHandler
{
    private readonly DocumentManager _documentManager;

    public ReferencesHandler(DocumentManager documentManager)
    {
        _documentManager = documentManager;
    }

    public ReferenceRegistrationOptions GetRegistrationOptions(
        ReferenceCapability capability,
        ClientCapabilities clientCapabilities)
    {
        return new ReferenceRegistrationOptions
        {
            DocumentSelector = TextDocumentSelector.ForLanguage("old8lang")
        };
    }

    public Task<LocationContainer?> Handle(ReferenceParams request, CancellationToken cancellationToken)
    {
        var uri = request.TextDocument.Uri.ToString();
        var document = _documentManager.GetDocument(uri);

        if (document?.SymbolTable == null)
        {
            return Task.FromResult<LocationContainer?>(null);
        }

        // TODO: 实现根据位置查找符号引用的逻辑
        var locations = new List<Location>();

        return Task.FromResult<LocationContainer?>(new LocationContainer(locations));
    }
}