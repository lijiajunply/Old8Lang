using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Old8Lang.LanguageServer.Services;

namespace Old8Lang.LanguageServer.Handlers;

/// <summary>
/// 跳转定义处理器
/// </summary>
public class DefinitionHandler(DocumentManager documentManager) : IDefinitionHandler
{
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
        var document = documentManager.GetDocument(uri);

        if (document?.SymbolTable == null)
        {
            return Task.FromResult<LocationOrLocationLinks?>(new LocationOrLocationLinks());
        }

        // 获取光标位置
        var line = request.Position.Line;
        var column = request.Position.Character;

        // 查找光标位置的符号
        var symbol = SymbolFinder.FindSymbolAtPosition(document, line, column);

        if (symbol == null)
        {
            return Task.FromResult<LocationOrLocationLinks?>(new LocationOrLocationLinks());
        }

        // 构建定义位置
        var location = new Location
        {
            Uri = symbol.Location.Uri,
            Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range
            {
                Start = new Position(symbol.Location.Line, symbol.Location.Column),
                End = new Position(symbol.Location.EndLine, symbol.Location.EndColumn)
            }
        };

        var locations = new List<LocationOrLocationLink> { location };

        return Task.FromResult<LocationOrLocationLinks?>(new LocationOrLocationLinks(locations));
    }
}

/// <summary>
/// 查找引用处理器
/// </summary>
public class ReferencesHandler(DocumentManager documentManager) : IReferencesHandler
{
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
        var document = documentManager.GetDocument(uri);

        if (document?.SymbolTable == null || document.Tokens == null)
        {
            return Task.FromResult<LocationContainer?>(null);
        }

        // 获取光标位置
        var line = request.Position.Line;
        var column = request.Position.Character;

        // 查找光标位置的符号
        var symbol = SymbolFinder.FindSymbolAtPosition(document, line, column);

        if (symbol == null)
        {
            return Task.FromResult<LocationContainer?>(new LocationContainer());
        }

        // 查找该符号的所有引用
        var references = SymbolFinder.FindReferences(document, symbol.Name);

        // 转换为 LSP Location 格式
        var locations = new List<Location>();

        foreach (var reference in references)
        {
            // 根据 request.Context.IncludeDeclaration 决定是否包含定义位置
            bool isDefinition = reference.Line == symbol.Location.Line &&
                               reference.Column == symbol.Location.Column;

            if (isDefinition && !request.Context.IncludeDeclaration)
            {
                continue; // 跳过定义位置
            }

            locations.Add(new Location
            {
                Uri = reference.Uri,
                Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range
                {
                    Start = new Position(reference.Line, reference.Column),
                    End = new Position(reference.EndLine, reference.EndColumn)
                }
            });
        }

        return Task.FromResult<LocationContainer?>(new LocationContainer(locations));
    }
}