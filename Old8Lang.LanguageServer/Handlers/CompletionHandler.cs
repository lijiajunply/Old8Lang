using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Old8Lang.LanguageServer.Services;

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

        // 添加关键字补全
        completionItems.AddRange(GetKeywordCompletions());

        // 添加符号补全
        if (document?.SymbolTable != null)
        {
            completionItems.AddRange(GetSymbolCompletions(document.SymbolTable));
        }

        return Task.FromResult(new CompletionList(completionItems, isIncomplete: false));
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