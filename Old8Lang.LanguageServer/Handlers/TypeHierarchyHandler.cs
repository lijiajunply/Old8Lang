using Newtonsoft.Json.Linq;
using Old8Lang.LanguageServer.Models;
using Old8Lang.LanguageServer.Services;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using LspRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;
using LspSymbolKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.SymbolKind;
using ModelSymbolKind = Old8Lang.LanguageServer.Models.SymbolKind;

namespace Old8Lang.LanguageServer.Handlers;

/// <summary>
/// 类型层次结构处理器：支持 textDocument/prepareTypeHierarchy、typeHierarchy/supertypes
/// 与 typeHierarchy/subtypes，覆盖单文件与跨文件的类继承（extends）、接口实现（implements）与混入（with）。
/// </summary>
public class TypeHierarchyHandler(DocumentManager documentManager, WorkspaceIndex? workspaceIndex = null)
    : TypeHierarchyHandlerBase
{
    private WorkspaceIndex? EffectiveWorkspace => workspaceIndex ?? documentManager.WorkspaceIndex;

    protected override TypeHierarchyRegistrationOptions CreateRegistrationOptions(
        TypeHierarchyCapability capability,
        ClientCapabilities clientCapabilities)
    {
        return new TypeHierarchyRegistrationOptions
        {
            DocumentSelector = TextDocumentSelector.ForLanguage("old8lang")
        };
    }

    public override Task<Container<TypeHierarchyItem>?> Handle(
        TypeHierarchyPrepareParams request,
        CancellationToken cancellationToken)
    {
        var uri = request.TextDocument.Uri.ToString();
        var document = documentManager.GetDocument(uri);
        if (document == null)
        {
            return Task.FromResult<Container<TypeHierarchyItem>?>(null);
        }

        var symbol = SymbolFinder.FindSymbolAtPosition(
            document,
            (int)request.Position.Line,
            (int)request.Position.Character,
            EffectiveWorkspace);

        if (symbol == null)
        {
            return Task.FromResult<Container<TypeHierarchyItem>?>(null);
        }

        // 若光标位于变量/参数/字段上，尝试解析其声明类型以便从使用处直接打开类型层次
        if (!TypeResolver.IsTypeDeclaration(symbol) && !TypeResolver.IsDynamicType(symbol.Type))
        {
            symbol = TypeResolver.ResolveTypeSymbol(
                symbol.Type, document.SymbolTable, EffectiveWorkspace, document.Uri);
        }

        if (symbol == null || !TypeResolver.IsTypeDeclaration(symbol))
        {
            return Task.FromResult<Container<TypeHierarchyItem>?>(null);
        }

        var scope = FindScopeForSymbol(symbol.Location.Uri, symbol);
        var item = CreateTypeHierarchyItem(symbol, scope?.Range);
        return Task.FromResult<Container<TypeHierarchyItem>?>(new Container<TypeHierarchyItem>(item));
    }

    public override Task<Container<TypeHierarchyItem>?> Handle(
        TypeHierarchySupertypesParams request,
        CancellationToken cancellationToken)
    {
        var target = ResolveTypeFromItem(request.Item, out var ownerFile);
        if (target == null)
        {
            return Task.FromResult<Container<TypeHierarchyItem>?>(new Container<TypeHierarchyItem>());
        }

        var parentNames = new List<string>();
        if (!string.IsNullOrWhiteSpace(target.BaseClassName))
        {
            parentNames.Add(target.BaseClassName);
        }
        parentNames.AddRange(target.InterfaceNames.Where(n => !string.IsNullOrWhiteSpace(n)));
        parentNames.AddRange(target.MixinNames.Where(n => !string.IsNullOrWhiteSpace(n)));

        var items = new List<TypeHierarchyItem>();
        var seen = new HashSet<string>(StringComparer.Ordinal) { target.Name };

        foreach (var rawParent in parentNames)
        {
            var normalized = TypeResolver.NormalizeTypeName(rawParent);
            if (string.IsNullOrEmpty(normalized) || !seen.Add(normalized))
            {
                continue;
            }

            var superSym = TypeResolver.ResolveTypeSymbol(
                normalized,
                ownerFile?.Symbols,
                EffectiveWorkspace,
                ownerFile?.Uri ?? target.Location.Uri);

            // 若未注入 WorkspaceIndex，也在 DocumentManager 的其他打开文档中查找
            superSym ??= ResolveTypeInOpenDocuments(normalized);

            if (superSym != null)
            {
                var scope = FindScopeForSymbol(superSym.Location.Uri, superSym);
                items.Add(CreateTypeHierarchyItem(superSym, scope?.Range));
            }
        }

        return Task.FromResult<Container<TypeHierarchyItem>?>(new Container<TypeHierarchyItem>(items));
    }

    public override Task<Container<TypeHierarchyItem>?> Handle(
        TypeHierarchySubtypesParams request,
        CancellationToken cancellationToken)
    {
        var target = ResolveTypeFromItem(request.Item, out _);
        if (target == null)
        {
            return Task.FromResult<Container<TypeHierarchyItem>?>(new Container<TypeHierarchyItem>());
        }

        var targetName = target.Name;
        var items = new List<TypeHierarchyItem>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var file in CollectAllFiles())
        {
            foreach (var sym in file.Symbols.Values)
            {
                if (!TypeResolver.IsTypeDeclaration(sym) || sym.Name == targetName)
                {
                    continue;
                }

                var isDirectSubtype =
                    (!string.IsNullOrEmpty(sym.BaseClassName) &&
                     TypeResolver.NormalizeTypeName(sym.BaseClassName) == targetName) ||
                    sym.InterfaceNames.Any(n => TypeResolver.NormalizeTypeName(n) == targetName) ||
                    sym.MixinNames.Any(n => TypeResolver.NormalizeTypeName(n) == targetName);

                if (!isDirectSubtype)
                {
                    continue;
                }

                var key = $"{sym.Location.Uri}|{sym.Name}";
                if (!seen.Add(key))
                {
                    continue;
                }

                var scope = FindScopeInTree(file.RootScope, sym);
                items.Add(CreateTypeHierarchyItem(sym, scope?.Range));
            }
        }

        return Task.FromResult<Container<TypeHierarchyItem>?>(new Container<TypeHierarchyItem>(items));
    }

    private SymbolInfo? ResolveTypeFromItem(TypeHierarchyItem item, out IndexedFile? ownerFile)
    {
        var uri = item.Uri.ToString();
        var name = item.Name;

        if (item.Data is JObject data)
        {
            uri = data["uri"]?.Value<string>() ?? uri;
            name = data["name"]?.Value<string>() ?? name;
        }

        var files = CollectAllFiles();
        ownerFile = files.FirstOrDefault(f => f.Uri == uri);
        if (ownerFile != null && ownerFile.Symbols.TryGetValue(name, out var localSym) &&
            TypeResolver.IsTypeDeclaration(localSym))
        {
            return localSym;
        }

        foreach (var file in files)
        {
            if (file.Symbols.TryGetValue(name, out var sym) && TypeResolver.IsTypeDeclaration(sym))
            {
                ownerFile = file;
                return sym;
            }
        }

        return null;
    }

    private SymbolInfo? ResolveTypeInOpenDocuments(string typeName)
    {
        foreach (var (_, doc) in documentManager.GetAllDocuments())
        {
            if (doc.SymbolTable != null &&
                doc.SymbolTable.TryGetValue(typeName, out var sym) &&
                TypeResolver.IsTypeDeclaration(sym))
            {
                return sym;
            }
        }

        return null;
    }

    private List<IndexedFile> CollectAllFiles()
    {
        if (EffectiveWorkspace != null)
        {
            return EffectiveWorkspace.GetFiles().ToList();
        }

        var list = new List<IndexedFile>();
        foreach (var (uri, doc) in documentManager.GetAllDocuments())
        {
            list.Add(new IndexedFile
            {
                Uri = uri,
                Text = doc.Text,
                Tokens = doc.Tokens ?? [],
                Symbols = doc.SymbolTable ?? new Dictionary<string, SymbolInfo>(),
                RootScope = doc.RootScope,
                TopLevelCalls = doc.TopLevelCalls,
                IsOpen = true,
                Parsed = doc.Ast != null
            });
        }

        return list;
    }

    private SymbolScope? FindScopeForSymbol(string uri, SymbolInfo symbol)
    {
        var openDoc = documentManager.GetDocument(uri);
        if (openDoc?.RootScope != null)
        {
            return FindScopeInTree(openDoc.RootScope, symbol);
        }

        var indexed = EffectiveWorkspace?.GetFiles().FirstOrDefault(f => f.Uri == uri);
        return indexed?.RootScope != null ? FindScopeInTree(indexed.RootScope, symbol) : null;
    }

    private static SymbolScope? FindScopeInTree(SymbolScope? root, SymbolInfo symbol)
    {
        if (root == null) return null;

        foreach (var scope in root.EnumerateAllScopes())
        {
            if (scope.OwnerSymbol != null && SymbolFinder.IsSameSymbol(scope.OwnerSymbol, symbol))
            {
                return scope;
            }
        }

        return null;
    }

    private static TypeHierarchyItem CreateTypeHierarchyItem(SymbolInfo symbol, SourceLocation? fullRange)
    {
        var selection = ToLspRange(symbol.Location);
        var range = fullRange is { EndLine: < int.MaxValue }
            ? ToLspRange(fullRange)
            : selection;

        var detailParts = new List<string>();
        if (symbol.IsAbstract) detailParts.Add("abstract");
        if (symbol.IsInterface) detailParts.Add("interface");
        else if (symbol.IsMixin) detailParts.Add("mixin");
        else if (symbol.Kind == ModelSymbolKind.Enum) detailParts.Add("enum");
        else detailParts.Add("class");

        if (!string.IsNullOrWhiteSpace(symbol.BaseClassName))
        {
            detailParts.Add($"extends {symbol.BaseClassName}");
        }
        if (symbol.InterfaceNames.Count > 0)
        {
            detailParts.Add($"implements {string.Join(", ", symbol.InterfaceNames)}");
        }
        if (symbol.MixinNames.Count > 0)
        {
            detailParts.Add($"with {string.Join(", ", symbol.MixinNames)}");
        }

        return new TypeHierarchyItem
        {
            Name = symbol.Name,
            Kind = ConvertKind(symbol),
            Detail = string.Join(" ", detailParts),
            Uri = DocumentUri.From(symbol.Location.Uri),
            Range = range,
            SelectionRange = selection,
            Data = new JObject
            {
                ["name"] = symbol.Name,
                ["uri"] = symbol.Location.Uri,
                ["line"] = symbol.Location.Line,
                ["column"] = symbol.Location.Column
            }
        };
    }

    private static LspSymbolKind ConvertKind(SymbolInfo symbol)
    {
        if (symbol.IsInterface || symbol.Kind == ModelSymbolKind.Interface)
        {
            return LspSymbolKind.Interface;
        }

        if (symbol.Kind == ModelSymbolKind.Enum)
        {
            return LspSymbolKind.Enum;
        }

        return LspSymbolKind.Class;
    }

    private static LspRange ToLspRange(SourceLocation loc) => new(
        new Position(Math.Max(0, loc.Line), Math.Max(0, loc.Column)),
        new Position(Math.Max(0, loc.EndLine >= loc.Line ? loc.EndLine : loc.Line), Math.Max(0, loc.EndColumn)));
}
