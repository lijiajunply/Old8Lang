using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.LanguageServer.Services;
using Old8Lang.LangParser;

namespace Old8Lang.LanguageServer.Handlers;

/// <summary>
/// 签名帮助处理器 - 提供函数参数提示
/// </summary>
public class SignatureHelpHandler(DocumentManager documentManager, WorkspaceIndex? workspaceIndex = null)
    : ISignatureHelpHandler
{
    public SignatureHelpRegistrationOptions GetRegistrationOptions(
        SignatureHelpCapability capability,
        ClientCapabilities clientCapabilities)
    {
        return new SignatureHelpRegistrationOptions
        {
            DocumentSelector = TextDocumentSelector.ForLanguage("old8lang"),
            TriggerCharacters = new[] { "(", "," },
            RetriggerCharacters = new[] { "," }
        };
    }

    public Task<SignatureHelp?> Handle(SignatureHelpParams request, CancellationToken cancellationToken)
    {
        var uri = request.TextDocument.Uri.ToString();
        var document = documentManager.GetDocument(uri);

        if (document?.Tokens == null || document.SymbolTable == null)
        {
            return Task.FromResult<SignatureHelp?>(null);
        }

        var line = request.Position.Line + 1; // LSP 行号从 0 开始，token 行号从 1 开始
        var column = request.Position.Character; // 两者的列号都是 0 起始，无需转换

        // 查找当前光标所在的函数或方法调用
        var functionCall = FindFunctionCall(document, line, column);

        if (functionCall == null)
        {
            return Task.FromResult<SignatureHelp?>(null);
        }

        var effectiveWorkspace = workspaceIndex ?? documentManager.WorkspaceIndex;
        var functionSymbol = ResolveCallableSymbol(document, functionCall, effectiveWorkspace);

        if (functionSymbol == null)
        {
            // 检查全局函数（支持重载）
            var globalSignatures = GetGlobalFunctionSignatures(functionCall.FunctionName);
            if (globalSignatures.Count > 0)
            {
                return Task.FromResult<SignatureHelp?>(new SignatureHelp
                {
                    Signatures = new Container<SignatureInformation>(globalSignatures),
                    ActiveSignature = 0,
                    ActiveParameter = functionCall.CurrentParameterIndex
                });
            }

            return Task.FromResult<SignatureHelp?>(null);
        }

        // 如果是类构造调用，优先使用 init 构造函数签名
        if (functionSymbol.Kind == Models.SymbolKind.Class)
        {
            var initFound = TypeResolver.FindMember(
                functionSymbol, "init", document.SymbolTable, effectiveWorkspace, document.Uri);
            if (initFound != null)
            {
                functionSymbol = initFound.Value.Member;
            }
        }

        // 构建函数签名
        var signature = BuildSignatureInformation(functionSymbol);

        var signatureHelp = new SignatureHelp
        {
            Signatures = new Container<SignatureInformation>(signature),
            ActiveSignature = 0,
            ActiveParameter = functionCall.CurrentParameterIndex
        };

        return Task.FromResult<SignatureHelp?>(signatureHelp);
    }

    private static Models.SymbolInfo? ResolveCallableSymbol(
        Models.DocumentParseResult document,
        FunctionCallContext call,
        WorkspaceIndex? workspaceIndex)
    {
        var tokens = document.Tokens!;
        var nameIndex = call.NameTokenIndex;

        // 成员方法调用（obj.method( / Class.staticMethod( / this.method( / super.method(）
        if (nameIndex > 1 && tokens[nameIndex - 1].Type == LangTokenType.Dot)
        {
            var analyzer = new MemberChainAnalyzer(document, new Position(call.Line - 1, call.Column), workspaceIndex);
            var chain = analyzer.AnalyzeChainAtDotIndex(nameIndex - 1);
            if (chain != null)
            {
                var found = TypeResolver.FindMember(
                    chain.TargetType, call.FunctionName, document.SymbolTable, workspaceIndex, document.Uri);
                if (found != null)
                {
                    return found.Value.Member;
                }
            }
        }

        // 作用域链查找
        var scoped = document.RootScope?.FindInnermostScope(call.Line - 1, call.Column)
            .Resolve(call.FunctionName, call.Line - 1, call.Column);
        if (scoped != null)
        {
            return scoped;
        }

        if (document.SymbolTable!.TryGetValue(call.FunctionName, out var local))
        {
            return local;
        }

        if (workspaceIndex != null &&
            workspaceIndex.TryResolveSymbol(call.FunctionName, document.Uri, out var remote, out _))
        {
            return remote;
        }

        return null;
    }

    /// <summary>
    /// 查找当前光标所在的函数调用
    /// </summary>
    private FunctionCallContext? FindFunctionCall(Models.DocumentParseResult document, int line, int column)
    {
        var tokens = document.Tokens!;
        int currentIndex = document.TokenIndex.FindLastTokenAtOrBefore(line - 1, column);

        if (currentIndex < 0)
        {
            return null;
        }

        // 如果当前token是右括号,从前一个token开始
        // 这样可以确保我们在函数调用内部查找
        int startIndex = currentIndex;
        if (currentIndex >= 0 && currentIndex < tokens.Count &&
            tokens[currentIndex].Type == LangTokenType.RightParen)
        {
            startIndex = currentIndex - 1;
        }

        // 向前查找最近的左括号和函数名
        int parenDepth = 0;
        string? functionName = null;
        int nameTokenIndex = -1;
        int parameterIndex = 0;
        int commaCount = 0;

        for (int i = startIndex; i >= 0; i--)
        {
            var token = tokens[i];

            if (token.Type == LangTokenType.RightParen)
            {
                parenDepth++;
            }
            else if (token.Type == LangTokenType.LeftParen)
            {
                if (parenDepth == 0)
                {
                    // 找到对应的左括号，查找函数名
                    if (i > 0 && tokens[i - 1].Type == LangTokenType.Identifier)
                    {
                        functionName = tokens[i - 1].Value;
                        nameTokenIndex = i - 1;
                        parameterIndex = commaCount;
                        break;
                    }
                }
                else
                {
                    parenDepth--;
                }
            }
            else if (token.Type == LangTokenType.Comma && parenDepth == 0)
            {
                commaCount++;
            }
        }

        if (functionName == null)
        {
            return null;
        }

        return new FunctionCallContext
        {
            FunctionName = functionName,
            CurrentParameterIndex = parameterIndex,
            NameTokenIndex = nameTokenIndex,
            Line = line,
            Column = column
        };
    }

    /// <summary>
    /// 构建函数签名信息
    /// </summary>
    private SignatureInformation BuildSignatureInformation(Models.SymbolInfo functionSymbol)
    {
        var parameters = new List<ParameterInformation>();
        var signatureLabel = $"{functionSymbol.Name}(";

        // 从 Parameters 列表中获取参数信息
        var paramSymbols = functionSymbol.Parameters;

        for (int i = 0; i < paramSymbols.Count; i++)
        {
            var param = paramSymbols[i];
            var paramLabel = !string.IsNullOrEmpty(param.Type)
                ? $"{param.Name}:{param.Type}"
                : param.Name;

            if (i > 0)
            {
                signatureLabel += ", ";
            }

            signatureLabel += paramLabel;

            parameters.Add(new ParameterInformation
            {
                Label = paramLabel,
                Documentation = param.Documentation
            });
        }

        signatureLabel += ")";

        // 从函数签名中提取返回类型
        var returnType = ExtractReturnTypeFromSignature(functionSymbol.Type);
        if (!string.IsNullOrEmpty(returnType))
        {
            signatureLabel += $" -> {returnType}";
        }

        return new SignatureInformation
        {
            Label = signatureLabel,
            Documentation = functionSymbol.Documentation,
            Parameters = new Container<ParameterInformation>(parameters)
        };
    }

    /// <summary>
    /// 从函数签名中提取返回类型
    /// </summary>
    private string? ExtractReturnTypeFromSignature(string? signature)
    {
        if (string.IsNullOrEmpty(signature))
            return null;

        // 查找 " -> " 后的返回类型
        var arrowIndex = signature.IndexOf(" -> ", StringComparison.Ordinal);
        if (arrowIndex >= 0)
        {
            return signature.Substring(arrowIndex + 4).Trim();
        }

        return null;
    }

    /// <summary>
    /// 从全局函数注册器获取函数签名（支持重载）
    /// </summary>
    private List<SignatureInformation> GetGlobalFunctionSignatures(string functionName)
    {
        // 确保全局函数已初始化
        GlobalFunctionInitializer.EnsureInitialized();

        var signatures = new List<SignatureInformation>();

        // 获取重载组
        var overloadGroup = GlobalFunctionRegistry.Instance.GetOverloadGroup(functionName);
        if (overloadGroup == null)
            return signatures;

        // 获取所有重载的签名信息
        var signatureInfos = overloadGroup.GetAllSignatures();

        foreach (var info in signatureInfos)
        {
            var parameters = info.Parameters.Select(p => new ParameterInformation
            {
                Label = p.Label,
                Documentation = p.Documentation
            }).ToList();

            signatures.Add(new SignatureInformation
            {
                Label = info.Signature,
                Documentation = info.Documentation,
                Parameters = new Container<ParameterInformation>(parameters)
            });
        }

        return signatures;
    }

    /// <summary>
    /// 函数调用上下文
    /// </summary>
    private class FunctionCallContext
    {
        public required string FunctionName { get; set; }
        public int CurrentParameterIndex { get; set; }
        public int NameTokenIndex { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
    }
}
