using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Old8Lang.LanguageServer.Models;
using Old8Lang.LanguageServer.Services;
using SymbolKind = Old8Lang.LanguageServer.Models.SymbolKind;

namespace Old8Lang.LanguageServer.Handlers;

/// <summary>
/// 代码镜头处理器 - 在函数、类和方法的声明上方显示引用数量。
///
/// 顶层符号（函数、类）使用工作区索引统计跨文件引用；类方法保持文件内统计，
/// 避免把其他类中的同名方法算进来。
/// 点击镜头执行 <c>old8lang.showReferences</c>，由 VS Code 扩展转发到内置的
/// <c>editor.action.showReferences</c>。
/// </summary>
public class CodeLensHandler(DocumentManager documentManager, WorkspaceIndex? workspaceIndex = null) : ICodeLensHandler
{
    /// <summary>客户端执行的命令名，需与 vscode-old8lang/src/extension.ts 保持一致</summary>
    public const string ShowReferencesCommand = "old8lang.showReferences";

    public Task<CodeLensContainer?> Handle(CodeLensParams request, CancellationToken cancellationToken)
    {
        var uri = request.TextDocument.Uri.ToString();
        var document = documentManager.GetDocument(uri);

        if (document?.SymbolTable == null)
        {
            return Task.FromResult<CodeLensContainer?>(new CodeLensContainer());
        }

        var lenses = new List<CodeLens>();

        foreach (var (_, symbol) in document.SymbolTable)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.Function:
                case SymbolKind.Class:
                {
                    var references = GetTopLevelReferences(document, symbol);
                    AddLens(lenses, symbol, references);
                    break;
                }
            }

            if (symbol.Kind != SymbolKind.Class)
            {
                continue;
            }

            foreach (var member in symbol.Members.Values)
            {
                if (member.Kind != SymbolKind.Method)
                {
                    continue;
                }

                var references = GetLocalReferences(document, member);
                AddLens(lenses, member, references);
            }
        }

        return Task.FromResult<CodeLensContainer?>(new CodeLensContainer(lenses));
    }

    public CodeLensRegistrationOptions GetRegistrationOptions(
        CodeLensCapability capability,
        ClientCapabilities clientCapabilities)
    {
        return new CodeLensRegistrationOptions
        {
            DocumentSelector = TextDocumentSelector.ForLanguage("old8lang"),
            ResolveProvider = false
        };
    }

    private List<SourceLocation> GetTopLevelReferences(DocumentParseResult document, SymbolInfo symbol)
    {
        if (workspaceIndex != null)
        {
            return workspaceIndex.FindReferences(symbol.Name, symbol.Location.Uri);
        }

        return SymbolFinder.FindReferences(document, symbol.Name);
    }

    private static List<SourceLocation> GetLocalReferences(DocumentParseResult document, SymbolInfo symbol)
    {
        // 方法的作用域是文件：先找 token 再排除成员访问造成的误报
        if (document.Tokens == null)
        {
            return [];
        }

        var file = new IndexedFile
        {
            Uri = document.Uri,
            Text = document.Text,
            Tokens = document.Tokens,
            Symbols = document.SymbolTable ?? new Dictionary<string, SymbolInfo>()
        };

        return WorkspaceIndex.CollectIdentifierOccurrences(file, symbol.Name, includeMemberAccess: true);
    }

    private static void AddLens(List<CodeLens> lenses, SymbolInfo symbol, List<SourceLocation> references)
    {
        if (symbol.Location.EndLine < symbol.Location.Line)
        {
            return;
        }

        var count = references.Count;

        // 镜头参数只带前若干条，避免大工作区里单个符号几千条引用撑爆消息体
        var positions = new JArray(
            references.Take(200).Select(location => (object)new JObject
            {
                ["uri"] = location.Uri,
                ["range"] = new JObject
                {
                    ["start"] = new JObject
                    {
                        ["line"] = location.Line,
                        ["character"] = location.Column
                    },
                    ["end"] = new JObject
                    {
                        ["line"] = location.EndLine,
                        ["character"] = location.EndColumn
                    }
                }
            }));

        var command = new Command
        {
            Title = count == 1 ? "1 处引用" : $"{count} 处引用",
            Name = ShowReferencesCommand,
            Arguments = new JArray(
                symbol.Location.Uri,
                new JObject
                {
                    ["line"] = symbol.Location.Line,
                    ["character"] = symbol.Location.Column
                },
                positions)
        };

        lenses.Add(new CodeLens
        {
            Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range
            {
                Start = new Position(symbol.Location.Line, symbol.Location.Column),
                End = new Position(symbol.Location.EndLine, symbol.Location.EndColumn)
            },
            Command = command
        });
    }
}
