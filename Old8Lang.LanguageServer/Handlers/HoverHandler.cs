using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Old8Lang.LanguageServer.Services;

namespace Old8Lang.LanguageServer.Handlers;

/// <summary>
/// 悬停提示处理器
/// </summary>
public class HoverHandler(DocumentManager documentManager) : IHoverHandler
{
    public HoverRegistrationOptions GetRegistrationOptions(
        HoverCapability capability,
        ClientCapabilities clientCapabilities)
    {
        return new HoverRegistrationOptions
        {
            DocumentSelector = TextDocumentSelector.ForLanguage("old8lang")
        };
    }

    public Task<Hover?> Handle(HoverParams request, CancellationToken cancellationToken)
    {
        var uri = request.TextDocument.Uri.ToString();
        var document = documentManager.GetDocument(uri);

        if (document?.SymbolTable == null)
        {
            return Task.FromResult<Hover?>(null);
        }

        // 查找光标位置的符号
        var line = request.Position.Line;
        var column = request.Position.Character;
        var symbol = Services.SymbolFinder.FindSymbolAtPosition(document, line, column);

        if (symbol == null)
        {
            return Task.FromResult<Hover?>(null);
        }

        // 构建悬停内容
        var content = BuildHoverContent(symbol);

        var hover = new Hover
        {
            Contents = new MarkedStringsOrMarkupContent(new MarkupContent
            {
                Kind = MarkupKind.Markdown,
                Value = content
            })
        };

        return Task.FromResult<Hover?>(hover);
    }

    /// <summary>
    /// 构建悬停提示内容
    /// </summary>
    private static string BuildHoverContent(Models.SymbolInfo symbol)
    {
        var lines = new List<string>();

        // 添加类型签名
        if (!string.IsNullOrEmpty(symbol.Type))
        {
            lines.Add("```old8lang");
            lines.Add(symbol.Type);
            lines.Add("```");
            lines.Add("");
        }

        // 添加文档注释
        if (!string.IsNullOrEmpty(symbol.Documentation))
        {
            lines.Add(symbol.Documentation);
            lines.Add("");
        }

        // 添加位置信息
        lines.Add($"*定义于 {symbol.Location.Uri}:{symbol.Location.Line + 1}*");

        return string.Join("\n", lines);
    }
}