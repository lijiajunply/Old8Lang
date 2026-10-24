using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Old8Lang.LanguageServer.Services;
using Old8Lang.LanguageServer.Models;

namespace Old8Lang.LanguageServer.Handlers;

/// <summary>
/// 跳转定义处理器
/// </summary>
public class DefinitionHandler(DocumentManager documentManager, WorkspaceIndex? workspaceIndex = null) : IDefinitionHandler
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

        var line = (int)request.Position.Line;
        var column = (int)request.Position.Character;

        // 先在本文件符号表中查找
        var symbol = SymbolFinder.FindSymbolAtPosition(document, line, column);

        if (symbol == null)
        {
            // 本文件未声明该标识符时，尝试通过工作区索引解析到其他文件的顶层符号
            var name = SymbolFinder.FindIdentifierNameAtPosition(document, line, column);
            if (name != null && workspaceIndex != null &&
                workspaceIndex.TryResolveSymbol(name, uri, out var workspaceSymbol, out _) &&
                workspaceSymbol != null)
            {
                symbol = workspaceSymbol;
            }
        }

        if (symbol == null)
        {
            return Task.FromResult<LocationOrLocationLinks?>(new LocationOrLocationLinks());
        }

        var location = new Location
        {
            Uri = symbol.Location.Uri,
            Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range
            {
                Start = new Position(symbol.Location.Line, symbol.Location.Column),
                End = new Position(symbol.Location.EndLine, symbol.Location.EndColumn)
            }
        };

        return Task.FromResult<LocationOrLocationLinks?>(new LocationOrLocationLinks(location));
    }
}

/// <summary>
/// 查找引用处理器 - 顶层符号支持跨文件查找
/// </summary>
public class ReferencesHandler(DocumentManager documentManager, WorkspaceIndex? workspaceIndex = null) : IReferencesHandler
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

        var line = (int)request.Position.Line;
        var column = (int)request.Position.Character;

        var symbol = SymbolFinder.FindSymbolAtPosition(document, line, column);
        var symbolName = symbol?.Name ?? SymbolFinder.FindIdentifierNameAtPosition(document, line, column);

        if (symbolName == null)
        {
            return Task.FromResult<LocationContainer?>(new LocationContainer());
        }

        // 顶层符号（函数、类、全局变量）跨文件查找；成员与参数保持文件内作用域
        if (workspaceIndex != null &&
            workspaceIndex.TryResolveSymbol(symbolName, uri, out var workspaceSymbol, out var definitionFile) &&
            workspaceSymbol != null && definitionFile != null &&
            (symbol == null || IsSameDefinition(symbol, workspaceSymbol)))
        {
            var workspaceReferences = workspaceIndex.FindReferences(symbolName, definitionFile.Uri);
            return Task.FromResult<LocationContainer?>(
                new LocationContainer(BuildLocations(workspaceReferences, workspaceSymbol.Location,
                    request.Context.IncludeDeclaration)));
        }

        if (symbol == null)
        {
            return Task.FromResult<LocationContainer?>(new LocationContainer());
        }

        var references = SymbolFinder.FindReferences(document, symbol.Name);
        return Task.FromResult<LocationContainer?>(
            new LocationContainer(BuildLocations(references, symbol.Location, request.Context.IncludeDeclaration)));
    }

    private static IEnumerable<Location> BuildLocations(
        IEnumerable<SourceLocation> references,
        SourceLocation definition,
        bool includeDeclaration)
    {
        foreach (var reference in references)
        {
            var isDefinition = IsSameLocation(reference, definition);
            if (isDefinition && !includeDeclaration)
            {
                continue;
            }

            yield return new Location
            {
                Uri = reference.Uri,
                Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range
                {
                    Start = new Position(reference.Line, reference.Column),
                    End = new Position(reference.EndLine, reference.EndColumn)
                }
            };
        }
    }

    internal static bool IsSameDefinition(SymbolInfo left, SymbolInfo right)
    {
        return IsSameLocation(left.Location, right.Location);
    }

    internal static bool IsSameLocation(SourceLocation left, SourceLocation right)
    {
        return left.Uri == right.Uri && left.Line == right.Line && left.Column == right.Column;
    }
}
