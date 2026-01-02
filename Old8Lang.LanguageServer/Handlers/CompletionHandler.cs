using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Old8Lang.LanguageServer.Services;
using Old8Lang.LangParser;

namespace Old8Lang.LanguageServer.Handlers;

/// <summary>
/// 自动补全处理器
/// </summary>
public class CompletionHandler(DocumentManager documentManager) : ICompletionHandler
{
    public CompletionRegistrationOptions GetRegistrationOptions(
        CompletionCapability capability,
        ClientCapabilities clientCapabilities)
    {
        return new CompletionRegistrationOptions
        {
            DocumentSelector = TextDocumentSelector.ForLanguage("old8lang"),
            TriggerCharacters = new[] { ".", "<" },
            ResolveProvider = false
        };
    }

    public Task<CompletionList> Handle(CompletionParams request, CancellationToken cancellationToken)
    {
        var uri = request.TextDocument.Uri.ToString();
        var document = documentManager.GetDocument(uri);

        var completionItems = new List<CompletionItem>();

        // 检查是否是成员访问补全（obj.）
        if (document?.Tokens != null && document.SymbolTable != null)
        {
            var memberCompletions = GetMemberCompletions(document, request.Position);
            if (memberCompletions != null && memberCompletions.Any())
            {
                // 如果是成员访问，只返回成员补全
                return Task.FromResult(new CompletionList(memberCompletions, isIncomplete: false));
            }
        }

        // 添加关键字补全
        completionItems.AddRange(GetKeywordCompletions());

        // 添加符号补全
        if (document?.SymbolTable != null)
        {
            completionItems.AddRange(GetSymbolCompletions(document.SymbolTable));
        }

        return Task.FromResult(new CompletionList(completionItems, isIncomplete: false));
    }

    /// <summary>
    /// 获取成员补全（obj. 后面的补全）
    /// </summary>
    private static List<CompletionItem>? GetMemberCompletions(
        Models.DocumentParseResult document,
        Position position)
    {
        var tokens = document.Tokens!;
        var line = position.Line + 1; // LSP 从 0 开始，token 从 1 开始
        var column = position.Character + 1;

        // 查找光标前的 token
        LangToken? dotToken = null;
        LangToken? objectToken = null;

        for (int i = tokens.Count - 1; i >= 1; i--)
        {
            var token = tokens[i];
            var prevToken = tokens[i - 1];

            // 查找光标位置附近的 dot token
            if (token.Line == line && token.Type == LangTokenType.Dot)
            {
                if (column >= token.Column)
                {
                    dotToken = token;
                    objectToken = prevToken;
                    break;
                }
            }
        }

        // 如果没找到点号，说明不是成员访问
        if (dotToken == null || objectToken == null ||
            objectToken.Value.Type != LangTokenType.Identifier)
        {
            return null;
        }

        var objectName = objectToken.Value.Value;
        if (objectName == null || document.SymbolTable == null)
        {
            return null;
        }

        // 查找对象的符号
        if (!document.SymbolTable.TryGetValue(objectName, out var objectSymbol))
        {
            return null;
        }

        // 如果对象不是类，不提供成员补全
        if (objectSymbol.Kind != Models.SymbolKind.Class)
        {
            return null;
        }

        // 返回类的所有成员
        return objectSymbol.Members.Values.Select(member => new CompletionItem
        {
            Label = member.Name,
            Kind = ConvertSymbolKind(member.Kind),
            Detail = member.Type ?? member.Kind.ToString(),
            Documentation = member.Documentation,
            InsertText = member.Name,
            // 添加访问修饰符信息
            LabelDetails = new CompletionItemLabelDetails
            {
                Description = member.IsStatic ? "static" : null
            }
        }).ToList();
    }

    private static IEnumerable<CompletionItem> GetKeywordCompletions()
    {
        var keywords = new[]
        {
            "func", "class", "if", "elif", "else", "for", "while", "for-in",
            "switch", "case", "default", "return", "break", "continue",
            "try", "catch", "finally", "throw", "import", "async", "await",
            "yield", "native", "public", "private", "static", "const",
            "int", "double", "string", "bool", "char", "void", "var"
        };

        return keywords.Select(keyword => new CompletionItem
        {
            Label = keyword,
            Kind = CompletionItemKind.Keyword,
            Detail = $"Old8Lang 关键字",
            InsertText = keyword
        });
    }

    private static IEnumerable<CompletionItem> GetSymbolCompletions(
        Dictionary<string, Models.SymbolInfo> symbolTable)
    {
        return symbolTable.Values.Select(symbol => new CompletionItem
        {
            Label = symbol.Name,
            Kind = ConvertSymbolKind(symbol.Kind),
            Detail = symbol.Type ?? symbol.Kind.ToString(),
            Documentation = symbol.Documentation,
            InsertText = symbol.Name
        });
    }

    private static CompletionItemKind ConvertSymbolKind(Models.SymbolKind kind)
    {
        return kind switch
        {
            Models.SymbolKind.Variable => CompletionItemKind.Variable,
            Models.SymbolKind.Function => CompletionItemKind.Function,
            Models.SymbolKind.Class => CompletionItemKind.Class,
            Models.SymbolKind.Method => CompletionItemKind.Method,
            Models.SymbolKind.Property => CompletionItemKind.Property,
            Models.SymbolKind.Parameter => CompletionItemKind.Variable,
            Models.SymbolKind.Constant => CompletionItemKind.Constant,
            _ => CompletionItemKind.Text
        };
    }
}