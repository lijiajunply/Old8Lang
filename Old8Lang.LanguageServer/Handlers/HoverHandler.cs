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
        var symbol = SymbolFinder.FindSymbolAtPosition(document, line, column);
        
        // Debug: 调试悬停查找
        System.Diagnostics.Debug.WriteLine($"Hover请求: Line={line}, Column={column}");
        if (symbol != null)
        {
            System.Diagnostics.Debug.WriteLine($"找到符号: Name={symbol.Name}, Type={symbol.Type}, Kind={symbol.Kind}");
        }

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

        // 添加访问修饰符和static标记
        var modifiers = new List<string>();
        if (symbol.AccessModifier != Models.AccessModifier.Public)
        {
            modifiers.Add(symbol.AccessModifier.ToString().ToLower());
        }
        if (symbol.IsStatic)
        {
            modifiers.Add("static");
        }

        // 构建显示类型 - 优先使用符号类型信息
        string displayType;
        if (!string.IsNullOrEmpty(symbol.Type))
        {
            // 对于变量，显示 "变量名: 类型" 的格式
            displayType = symbol.Kind == Models.SymbolKind.Variable ? $"{symbol.Name}: {symbol.Type}" :
                // 对于函数、类、方法等，Type字段已经包含完整签名，直接使用
                symbol.Type;
        }
        else
        {
            // 如果没有类型信息，使用符号名称和类型构建
            displayType = symbol.Kind switch
            {
                Models.SymbolKind.Variable => $"var {symbol.Name}",
                Models.SymbolKind.Function => $"func {symbol.Name}",
                Models.SymbolKind.Class => $"class {symbol.Name}",
                Models.SymbolKind.Method => $"func {symbol.Name}",
                Models.SymbolKind.Property => $"{symbol.Name}",
                _ => symbol.Name
            };
        }

        // 添加类型签名
        lines.Add("```old8lang");
        if (modifiers.Count > 0)
        {
            lines.Add($"// {string.Join(" ", modifiers)}");
        }
        lines.Add(displayType);
        lines.Add("```");
        lines.Add("");

        // 如果是成员，显示所属类
        if (symbol.Parent != null)
        {
            lines.Add($"*属于类: `{symbol.Parent.Name}`*");
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