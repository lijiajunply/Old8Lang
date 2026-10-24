using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Old8Lang.LangParser;
using Old8Lang.LanguageServer.Models;
using Old8Lang.LanguageServer.Services;
using SymbolKind = Old8Lang.LanguageServer.Models.SymbolKind;

namespace Old8Lang.LanguageServer.Handlers;

/// <summary>
/// 参数名内联提示处理器 - 在调用点把位置参数标注成 <c>a:</c>、<c>b:</c>。
///
/// 只对能解析到唯一函数的调用生效：形参与实参数量不一致、构造函数没有 <c>init</c>、
/// 或者调用名无法解析时都不产生提示，避免误导。
/// </summary>
public class InlayHintHandler(DocumentManager documentManager, WorkspaceIndex? workspaceIndex = null)
    : IInlayHintsHandler
{
    public Task<InlayHintContainer?> Handle(InlayHintParams request, CancellationToken cancellationToken)
    {
        var uri = request.TextDocument.Uri.ToString();
        var document = documentManager.GetDocument(uri);
        var hints = new List<InlayHint>();

        if (document?.Tokens == null || document.SymbolTable == null)
        {
            return Task.FromResult<InlayHintContainer?>(new InlayHintContainer(hints));
        }

        var tokens = document.Tokens;
        var resolved = new Dictionary<string, List<SymbolInfo>?>(StringComparer.Ordinal);

        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            if (token.Type != LangTokenType.Identifier ||
                i + 1 >= tokens.Count ||
                tokens[i + 1].Type != LangTokenType.LeftParen)
            {
                continue;
            }

            // 跳过声明本身（func foo(...)、class Foo(...)）
            if (i > 0 && tokens[i - 1].Type is LangTokenType.Func or LangTokenType.Class)
            {
                continue;
            }

            // 只为普通调用生成提示；成员调用需要类型推断，暂不处理
            if (i > 0 && tokens[i - 1].Type == LangTokenType.Dot)
            {
                continue;
            }

            if (!resolved.TryGetValue(token.Value, out var parameters))
            {
                parameters = ResolveParameters(document, token.Value);
                resolved[token.Value] = parameters;
            }

            if (parameters is not { Count: > 0 })
            {
                continue;
            }

            var openIndex = i + 1;
            var closeIndex = FindMatchingParen(tokens, openIndex);
            if (closeIndex < 0)
            {
                continue;
            }

            var argumentStarts = SplitArguments(tokens, openIndex, closeIndex);
            if (argumentStarts.Count != parameters.Count)
            {
                continue;
            }

            for (var argumentIndex = 0; argumentIndex < argumentStarts.Count; argumentIndex++)
            {
                var argumentToken = tokens[argumentStarts[argumentIndex]];
                var position = new Position(argumentToken.Line - 1, argumentToken.Column);

                if (!IsInRequestedRange(position, request.Range))
                {
                    continue;
                }

                var parameter = parameters[argumentIndex];
                var label = $"{parameter.Name}:";

                hints.Add(new InlayHint
                {
                    Position = position,
                    Label = new StringOrInlayHintLabelParts(label),
                    Kind = InlayHintKind.Parameter,
                    PaddingRight = true,
                    Tooltip = string.IsNullOrEmpty(parameter.Type)
                        ? null
                        : $"{parameter.Name}: {parameter.Type}"
                });
            }
        }

        return Task.FromResult<InlayHintContainer?>(new InlayHintContainer(hints));
    }

    public InlayHintRegistrationOptions GetRegistrationOptions(
        InlayHintClientCapabilities capability,
        ClientCapabilities clientCapabilities)
    {
        return new InlayHintRegistrationOptions
        {
            DocumentSelector = TextDocumentSelector.ForLanguage("old8lang"),
            ResolveProvider = false
        };
    }

    private List<SymbolInfo>? ResolveParameters(DocumentParseResult document, string name)
    {
        if (document.SymbolTable != null && document.SymbolTable.TryGetValue(name, out var local))
        {
            var parameters = GetParameters(local);
            if (parameters != null)
            {
                return parameters;
            }
        }

        if (workspaceIndex != null &&
            workspaceIndex.TryResolveSymbol(name, document.Uri, out var workspaceSymbol, out _) &&
            workspaceSymbol != null)
        {
            return GetParameters(workspaceSymbol);
        }

        return null;
    }

    private static List<SymbolInfo>? GetParameters(SymbolInfo symbol)
    {
        if (symbol.Kind is SymbolKind.Function or SymbolKind.Method)
        {
            return symbol.Parameters;
        }

        if (symbol.Kind == SymbolKind.Class &&
            symbol.Members.TryGetValue("init", out var initializer))
        {
            return initializer.Parameters;
        }

        return null;
    }

    private static int FindMatchingParen(List<LangToken> tokens, int openIndex)
    {
        var depth = 0;

        for (var i = openIndex; i < tokens.Count; i++)
        {
            switch (tokens[i].Type)
            {
                case LangTokenType.LeftParen:
                    depth++;
                    break;
                case LangTokenType.RightParen:
                    depth--;
                    if (depth == 0)
                    {
                        return i;
                    }

                    break;
            }
        }

        return -1;
    }

    /// <summary>按顶层逗号切分实参，返回每个实参的起始 token 下标</summary>
    private static List<int> SplitArguments(List<LangToken> tokens, int openIndex, int closeIndex)
    {
        var starts = new List<int>();
        var parenDepth = 0;
        var bracketDepth = 0;
        var braceDepth = 0;
        var expectArgument = true;

        for (var i = openIndex + 1; i < closeIndex; i++)
        {
            var token = tokens[i];

            if (expectArgument && token.Type != LangTokenType.Comma)
            {
                starts.Add(i);
                expectArgument = false;
            }

            switch (token.Type)
            {
                case LangTokenType.LeftParen:
                    parenDepth++;
                    break;
                case LangTokenType.RightParen:
                    parenDepth--;
                    break;
                case LangTokenType.LeftBracket:
                    bracketDepth++;
                    break;
                case LangTokenType.RightBracket:
                    bracketDepth--;
                    break;
                case LangTokenType.LeftBrace:
                    braceDepth++;
                    break;
                case LangTokenType.RightBrace:
                    braceDepth--;
                    break;
                case LangTokenType.Comma when parenDepth == 0 && bracketDepth == 0 && braceDepth == 0:
                    expectArgument = true;
                    break;
            }
        }

        return starts;
    }

    private static bool IsInRequestedRange(Position position, OmniSharp.Extensions.LanguageServer.Protocol.Models.Range range)
    {
        if (position.Line < range.Start.Line || position.Line > range.End.Line)
        {
            return false;
        }

        if (position.Line == range.Start.Line && position.Character < range.Start.Character)
        {
            return false;
        }

        if (position.Line == range.End.Line && position.Character > range.End.Character)
        {
            return false;
        }

        return true;
    }
}
