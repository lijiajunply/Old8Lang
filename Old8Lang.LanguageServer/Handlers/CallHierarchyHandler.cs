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
/// 调用层次结构处理器：支持 textDocument/prepareCallHierarchy、callHierarchy/incomingCalls
/// 与 callHierarchy/outgoingCalls，覆盖单文件与跨文件函数、方法及顶层脚本调用。
/// </summary>
public class CallHierarchyHandler(DocumentManager documentManager, WorkspaceIndex? workspaceIndex = null)
    : CallHierarchyHandlerBase
{
    private WorkspaceIndex? EffectiveWorkspace => workspaceIndex ?? documentManager.WorkspaceIndex;

    protected override CallHierarchyRegistrationOptions CreateRegistrationOptions(
        CallHierarchyCapability capability,
        ClientCapabilities clientCapabilities)
    {
        return new CallHierarchyRegistrationOptions
        {
            DocumentSelector = TextDocumentSelector.ForLanguage("old8lang")
        };
    }

    public override Task<Container<CallHierarchyItem>?> Handle(
        CallHierarchyPrepareParams request,
        CancellationToken cancellationToken)
    {
        var uri = request.TextDocument.Uri.ToString();
        var document = documentManager.GetDocument(uri);
        if (document == null)
        {
            return Task.FromResult<Container<CallHierarchyItem>?>(null);
        }

        var symbol = SymbolFinder.FindSymbolAtPosition(
            document,
            (int)request.Position.Line,
            (int)request.Position.Character,
            EffectiveWorkspace);

        if (symbol == null)
        {
            return Task.FromResult<Container<CallHierarchyItem>?>(null);
        }

        // 支持函数、方法；若光标在类名上且有 init 构造器则返回 init，否则允许查看该类的构造调用
        if (symbol.Kind == ModelSymbolKind.Class &&
            symbol.Members.TryGetValue("init", out var initMethod))
        {
            symbol = initMethod;
        }

        if (symbol.Kind is not (ModelSymbolKind.Function or ModelSymbolKind.Method or ModelSymbolKind.Class))
        {
            return Task.FromResult<Container<CallHierarchyItem>?>(null);
        }

        var ownerScope = FindScopeForSymbol(symbol.Location.Uri, symbol);
        var item = CreateCallHierarchyItem(symbol, ownerScope?.Range);
        return Task.FromResult<Container<CallHierarchyItem>?>(new Container<CallHierarchyItem>(item));
    }

    public override Task<Container<CallHierarchyIncomingCall>?> Handle(
        CallHierarchyIncomingCallsParams request,
        CancellationToken cancellationToken)
    {
        var target = ResolveItemTarget(request.Item);
        if (target == null)
        {
            return Task.FromResult<Container<CallHierarchyIncomingCall>?>(new Container<CallHierarchyIncomingCall>());
        }

        var incoming = new List<CallHierarchyIncomingCall>();
        var files = CollectAllFiles();

        foreach (var file in files)
        {
            // 1. 检查所有函数与类方法内部的调用点
            foreach (var caller in EnumerateCallableSymbols(file))
            {
                if (caller.Calls.Count == 0)
                {
                    continue;
                }

                var matchedRanges = new List<LspRange>();
                foreach (var call in caller.Calls)
                {
                    if (CallMatchesTarget(call, target, caller, file))
                    {
                        matchedRanges.Add(ToLspRange(call.Location));
                    }
                }

                if (matchedRanges.Count > 0)
                {
                    var callerScope = FindScopeInTree(file.RootScope, caller);
                    incoming.Add(new CallHierarchyIncomingCall
                    {
                        From = CreateCallHierarchyItem(caller, callerScope?.Range),
                        FromRanges = new Container<LspRange>(matchedRanges)
                    });
                }
            }

            // 2. 检查顶层脚本作用域中的直接调用点
            if (file.TopLevelCalls.Count > 0)
            {
                var topRanges = new List<LspRange>();
                foreach (var call in file.TopLevelCalls)
                {
                    if (CallMatchesTarget(call, target, caller: null, file))
                    {
                        topRanges.Add(ToLspRange(call.Location));
                    }
                }

                if (topRanges.Count > 0)
                {
                    var scriptName = ExtractFileDisplayName(file.Uri);
                    var firstRange = topRanges[0];
                    incoming.Add(new CallHierarchyIncomingCall
                    {
                        From = new CallHierarchyItem
                        {
                            Name = scriptName,
                            Kind = LspSymbolKind.File,
                            Detail = "top-level",
                            Uri = DocumentUri.From(file.Uri),
                            Range = firstRange,
                            SelectionRange = firstRange,
                            Data = new JObject
                            {
                                ["isTopLevel"] = true,
                                ["uri"] = file.Uri
                            }
                        },
                        FromRanges = new Container<LspRange>(topRanges)
                    });
                }
            }
        }

        return Task.FromResult<Container<CallHierarchyIncomingCall>?>(new Container<CallHierarchyIncomingCall>(incoming));
    }

    public override Task<Container<CallHierarchyOutgoingCall>?> Handle(
        CallHierarchyOutgoingCallsParams request,
        CancellationToken cancellationToken)
    {
        var files = CollectAllFiles();
        var itemUri = request.Item.Uri.ToString();
        var file = files.FirstOrDefault(f => f.Uri == itemUri);

        if (file == null)
        {
            return Task.FromResult<Container<CallHierarchyOutgoingCall>?>(new Container<CallHierarchyOutgoingCall>());
        }

        List<CallSite> calls;
        SymbolInfo? caller = null;

        if (request.Item.Data is JObject dataObj && dataObj["isTopLevel"]?.Value<bool>() == true)
        {
            calls = file.TopLevelCalls;
        }
        else
        {
            caller = ResolveItemTarget(request.Item);
            if (caller == null)
            {
                return Task.FromResult<Container<CallHierarchyOutgoingCall>?>(new Container<CallHierarchyOutgoingCall>());
            }

            calls = caller.Calls;
        }

        if (calls.Count == 0)
        {
            return Task.FromResult<Container<CallHierarchyOutgoingCall>?>(new Container<CallHierarchyOutgoingCall>());
        }

        // 按被调用符号分组聚合调用点范围
        var grouped = new Dictionary<string, (SymbolInfo Target, List<LspRange> Ranges)>(StringComparer.Ordinal);

        foreach (var call in calls)
        {
            var callee = ResolveCalleeSymbol(call, caller, file);
            if (callee == null)
            {
                continue;
            }

            var key = $"{callee.Location.Uri}|{callee.Parent?.Name}|{callee.Name}|{callee.Location.Line}:{callee.Location.Column}";
            if (!grouped.TryGetValue(key, out var entry))
            {
                entry = (callee, []);
                grouped[key] = entry;
            }

            entry.Ranges.Add(ToLspRange(call.Location));
        }

        var outgoing = new List<CallHierarchyOutgoingCall>(grouped.Count);
        foreach (var (_, (callee, ranges)) in grouped)
        {
            var calleeScope = FindScopeForSymbol(callee.Location.Uri, callee);
            outgoing.Add(new CallHierarchyOutgoingCall
            {
                To = CreateCallHierarchyItem(callee, calleeScope?.Range),
                FromRanges = new Container<LspRange>(ranges)
            });
        }

        return Task.FromResult<Container<CallHierarchyOutgoingCall>?>(new Container<CallHierarchyOutgoingCall>(outgoing));
    }

    private bool CallMatchesTarget(CallSite call, SymbolInfo target, SymbolInfo? caller, IndexedFile file)
    {
        // 类构造调用 `MyClass(...)` 匹配类本身或其 `init` 构造器
        if (target.Kind == ModelSymbolKind.Method &&
            target.Name == "init" &&
            target.Parent != null &&
            !call.IsMemberCall &&
            call.SymbolName == target.Parent.Name)
        {
            return true;
        }

        if (call.SymbolName != target.Name)
        {
            return false;
        }

        if (target.Kind == ModelSymbolKind.Class)
        {
            return !call.IsMemberCall;
        }

        if (target.Kind == ModelSymbolKind.Function)
        {
            if (call.IsMemberCall)
            {
                return false;
            }

            // 若当前文件声明了同名非目标函数且不同 URI，避免误报
            if (file.Uri != target.Location.Uri && file.Symbols.TryGetValue(target.Name, out var localSameName) &&
                localSameName.Kind == ModelSymbolKind.Function)
            {
                return false;
            }

            return true;
        }

        if (target.Kind == ModelSymbolKind.Method)
        {
            var resolved = ResolveCalleeSymbol(call, caller, file);
            if (resolved != null)
            {
                return SymbolFinder.IsSameSymbol(resolved, target);
            }

            // 未能确定接收者动态类型时，成员调用按方法名宽松匹配（避免动态变量漏报）
            if (call.IsMemberCall)
            {
                return true;
            }

            // 同类或子类方法内裸名称调用
            if (caller?.Parent != null && target.Parent != null)
            {
                return caller.Parent.Name == target.Parent.Name ||
                       TypeResolver.IsDerivedFrom(caller.Parent, target.Parent.Name, file.Symbols, EffectiveWorkspace, file.Uri);
            }
        }

        return false;
    }

    private SymbolInfo? ResolveCalleeSymbol(CallSite call, SymbolInfo? caller, IndexedFile file)
    {
        var scope = file.RootScope?.FindInnermostScope(call.Location.Line, call.Location.Column);

        if (call.IsMemberCall)
        {
            SymbolInfo? receiverType = null;

            if (call.ReceiverName == "this")
            {
                receiverType = caller?.Parent ?? scope?.FindEnclosingClass();
            }
            else if (call.ReceiverName == "super")
            {
                var enclosing = caller?.Parent ?? scope?.FindEnclosingClass();
                if (enclosing?.BaseClassName != null)
                {
                    receiverType = TypeResolver.ResolveTypeSymbol(
                        enclosing.BaseClassName, file.Symbols, EffectiveWorkspace, file.Uri);
                }
            }
            else if (!string.IsNullOrEmpty(call.ReceiverName))
            {
                // 先检查是否为静态类名调用
                receiverType = TypeResolver.ResolveTypeSymbol(
                    call.ReceiverName, file.Symbols, EffectiveWorkspace, file.Uri);

                // 再按变量/参数/字段类型解析
                if (receiverType == null)
                {
                    var receiverSym = scope?.Resolve(call.ReceiverName, call.Location.Line, call.Location.Column)
                        ?? (file.Symbols.TryGetValue(call.ReceiverName, out var top) ? top : null);
                    if (receiverSym != null && !TypeResolver.IsDynamicType(receiverSym.Type))
                    {
                        receiverType = TypeResolver.ResolveTypeSymbol(
                            receiverSym.Type, file.Symbols, EffectiveWorkspace, file.Uri);
                    }
                }
            }

            if (receiverType != null)
            {
                var member = TypeResolver.FindMember(
                    receiverType, call.SymbolName, file.Symbols, EffectiveWorkspace, file.Uri);
                if (member != null)
                {
                    return member.Value.Member;
                }
            }

            return null;
        }

        // 1. 作用域链查找（优先局部函数或类方法内裸名称调用）
        if (scope != null)
        {
            var resolved = scope.Resolve(call.SymbolName, call.Location.Line, call.Location.Column);
            if (resolved is { Kind: ModelSymbolKind.Function or ModelSymbolKind.Method })
            {
                return resolved;
            }

            if (resolved is { Kind: ModelSymbolKind.Class })
            {
                return resolved.Members.TryGetValue("init", out var initSym) ? initSym : resolved;
            }

            var enclosingClass = caller?.Parent ?? scope.FindEnclosingClass();
            if (enclosingClass != null)
            {
                var inherited = TypeResolver.FindMember(
                    enclosingClass, call.SymbolName, file.Symbols, EffectiveWorkspace, file.Uri);
                if (inherited is { Member.Kind: ModelSymbolKind.Method })
                {
                    return inherited.Value.Member;
                }
            }
        }

        // 2. 当前文件顶层符号表
        if (file.Symbols.TryGetValue(call.SymbolName, out var topSymbol))
        {
            if (topSymbol.Kind == ModelSymbolKind.Class && topSymbol.Members.TryGetValue("init", out var initSym))
            {
                return initSym;
            }

            if (topSymbol.Kind is ModelSymbolKind.Function or ModelSymbolKind.Class)
            {
                return topSymbol;
            }
        }

        // 3. 工作区跨文件解析
        if (EffectiveWorkspace != null &&
            EffectiveWorkspace.TryResolveSymbol(call.SymbolName, file.Uri, out var remoteSymbol, out _) &&
            remoteSymbol != null)
        {
            if (remoteSymbol.Kind == ModelSymbolKind.Class && remoteSymbol.Members.TryGetValue("init", out var initSym))
            {
                return initSym;
            }

            if (remoteSymbol.Kind is ModelSymbolKind.Function or ModelSymbolKind.Class)
            {
                return remoteSymbol;
            }
        }

        return null;
    }

    private SymbolInfo? ResolveItemTarget(CallHierarchyItem item)
    {
        var uri = item.Uri.ToString();
        var name = item.Name;
        string? parentClass = null;

        if (item.Data is JObject data)
        {
            uri = data["uri"]?.Value<string>() ?? uri;
            name = data["name"]?.Value<string>() ?? name;
            parentClass = data["parentClass"]?.Value<string>();
        }

        var files = CollectAllFiles();
        var file = files.FirstOrDefault(f => f.Uri == uri);
        if (file == null)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(parentClass) &&
            file.Symbols.TryGetValue(parentClass, out var cls) &&
            cls.Members.TryGetValue(name, out var method))
        {
            return method;
        }

        if (file.Symbols.TryGetValue(name, out var top))
        {
            return top;
        }

        return EnumerateCallableSymbols(file).FirstOrDefault(s =>
            s.Name == name &&
            (string.IsNullOrEmpty(parentClass) || s.Parent?.Name == parentClass));
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

    private static IEnumerable<SymbolInfo> EnumerateCallableSymbols(IndexedFile file)
    {
        var yielded = new HashSet<SymbolInfo>(ReferenceEqualityComparer.Instance);

        if (file.RootScope != null)
        {
            foreach (var scope in file.RootScope.EnumerateAllScopes())
            {
                if (scope.OwnerSymbol is { Kind: ModelSymbolKind.Function or ModelSymbolKind.Method } owner &&
                    yielded.Add(owner))
                {
                    yield return owner;
                }
            }
        }

        foreach (var sym in file.Symbols.Values)
        {
            if (sym.Kind == ModelSymbolKind.Function && yielded.Add(sym))
            {
                yield return sym;
            }
            else if (TypeResolver.IsTypeDeclaration(sym))
            {
                foreach (var member in sym.Members.Values)
                {
                    if (member.Kind == ModelSymbolKind.Method && yielded.Add(member))
                    {
                        yield return member;
                    }
                }
            }
        }
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

    private static CallHierarchyItem CreateCallHierarchyItem(SymbolInfo symbol, SourceLocation? fullRange)
    {
        var selection = ToLspRange(symbol.Location);
        var range = fullRange is { EndLine: < int.MaxValue }
            ? ToLspRange(fullRange)
            : selection;

        var detail = symbol.Parent != null
            ? $"{symbol.Parent.Name}.{symbol.Name}"
            : symbol.Type;

        return new CallHierarchyItem
        {
            Name = symbol.Name,
            Kind = ConvertKind(symbol.Kind),
            Detail = detail,
            Uri = DocumentUri.From(symbol.Location.Uri),
            Range = range,
            SelectionRange = selection,
            Data = new JObject
            {
                ["name"] = symbol.Name,
                ["kind"] = symbol.Kind.ToString(),
                ["uri"] = symbol.Location.Uri,
                ["parentClass"] = symbol.Parent?.Name,
                ["line"] = symbol.Location.Line,
                ["column"] = symbol.Location.Column
            }
        };
    }

    private static LspSymbolKind ConvertKind(ModelSymbolKind kind) => kind switch
    {
        ModelSymbolKind.Method => LspSymbolKind.Method,
        ModelSymbolKind.Class => LspSymbolKind.Class,
        _ => LspSymbolKind.Function
    };

    private static LspRange ToLspRange(SourceLocation loc) => new(
        new Position(Math.Max(0, loc.Line), Math.Max(0, loc.Column)),
        new Position(Math.Max(0, loc.EndLine >= loc.Line ? loc.EndLine : loc.Line), Math.Max(0, loc.EndColumn)));

    private static string ExtractFileDisplayName(string uri)
    {
        try
        {
            var path = DocumentUri.From(uri).GetFileSystemPath();
            if (!string.IsNullOrEmpty(path))
            {
                return Path.GetFileName(path);
            }
        }
        catch
        {
            // 忽略解析异常，回退原始 URI 末段
        }

        var idx = uri.LastIndexOf('/');
        return idx >= 0 && idx < uri.Length - 1 ? uri[(idx + 1)..] : uri;
    }
}
