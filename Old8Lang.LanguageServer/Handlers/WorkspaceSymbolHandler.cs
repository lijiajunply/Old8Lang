using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;
using Old8Lang.LanguageServer.Services;
using Old8Lang.LanguageServer.Models;
using OmniSharp.Extensions.LanguageServer.Protocol;
using SymbolKind = Old8Lang.LanguageServer.Models.SymbolKind;

namespace Old8Lang.LanguageServer.Handlers;

/// <summary>
/// 工作区符号搜索处理器 - 支持跨文件符号搜索（已优化性能）
/// </summary>
public class WorkspaceSymbolHandler(DocumentManager documentManager) : WorkspaceSymbolsHandlerBase
{
    public override Task<Container<WorkspaceSymbol>?> Handle(WorkspaceSymbolParams request,
        CancellationToken cancellationToken)
    {
        var query = request.Query?.ToLower() ?? "";
        var symbols = new List<WorkspaceSymbol>();

        // 优化：提前检查是否有查询，如果没有查询则限制结果数量
        const int maxSymbolsWithoutQuery = 100;
        bool hasQuery = !string.IsNullOrEmpty(query);

        // 遍历所有已打开的文档
        foreach (var (uri, parseResult) in documentManager.GetAllDocuments())
        {
            if (parseResult?.SymbolTable == null)
                continue;

            // 遍历符号表
            foreach (var (symbolName, symbolInfo) in parseResult.SymbolTable)
            {
                // 优化：如果没有查询且已经收集了足够多的符号，则停止
                if (!hasQuery && symbols.Count >= maxSymbolsWithoutQuery)
                {
                    break;
                }

                // 如果查询为空，返回所有符号
                // 如果查询不为空，检查符号名是否包含查询字符串（已优化为不区分大小写）
                if (!hasQuery || symbolName.ToLower().Contains(query))
                {
                    // 创建工作区符号
                    var workspaceSymbol = new WorkspaceSymbol
                    {
                        Name = symbolInfo.Name,
                        Kind = ConvertSymbolKind(symbolInfo.Kind),
                        Location = new Location
                        {
                            Uri = DocumentUri.From(symbolInfo.Location.Uri),
                            Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                                new Position(symbolInfo.Location.Line, symbolInfo.Location.Column),
                                new Position(symbolInfo.Location.EndLine, symbolInfo.Location.EndColumn)
                            )
                        },
                        ContainerName = GetContainerName(symbolInfo)
                    };

                    symbols.Add(workspaceSymbol);

                    // 如果是类，也添加其成员（仅当查询匹配时）
                    if (symbolInfo.Kind == SymbolKind.Class)
                    {
                        foreach (var (memberName, memberInfo) in symbolInfo.Members)
                        {
                            // 优化：如果没有查询且已经收集了足够多的符号，则跳过成员
                            if (!hasQuery && symbols.Count >= maxSymbolsWithoutQuery)
                            {
                                break;
                            }

                            if (!hasQuery || memberName.ToLower().Contains(query))
                            {
                                var memberSymbol = new WorkspaceSymbol
                                {
                                    Name = memberInfo.Name,
                                    Kind = ConvertSymbolKind(memberInfo.Kind),
                                    Location = new Location
                                    {
                                        Uri = DocumentUri.From(memberInfo.Location.Uri),
                                        Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                                            new Position(memberInfo.Location.Line, memberInfo.Location.Column),
                                            new Position(memberInfo.Location.EndLine, memberInfo.Location.EndColumn)
                                        )
                                    },
                                    ContainerName = symbolInfo.Name // 成员的容器是类名
                                };

                                symbols.Add(memberSymbol);
                            }
                        }
                    }
                }
            }

            // 优化：如果没有查询且已经收集了足够多的符号，则停止遍历文档
            if (!hasQuery && symbols.Count >= maxSymbolsWithoutQuery)
            {
                break;
            }
        }

        return Task.FromResult<Container<WorkspaceSymbol>?>(symbols);
    }

    protected override WorkspaceSymbolRegistrationOptions CreateRegistrationOptions(
        WorkspaceSymbolCapability capability, ClientCapabilities clientCapabilities)
    {
        return new WorkspaceSymbolRegistrationOptions
        {
            WorkDoneProgress = false
        };
    }

    /// <summary>
    /// 转换符号类型
    /// </summary>
    private static OmniSharp.Extensions.LanguageServer.Protocol.Models.SymbolKind ConvertSymbolKind(SymbolKind kind)
    {
        return kind switch
        {
            SymbolKind.Variable => OmniSharp.Extensions.LanguageServer.Protocol.Models.SymbolKind.Variable,
            SymbolKind.Function => OmniSharp.Extensions.LanguageServer.Protocol.Models.SymbolKind.Function,
            SymbolKind.Class => OmniSharp.Extensions.LanguageServer.Protocol.Models.SymbolKind.Class,
            SymbolKind.Method => OmniSharp.Extensions.LanguageServer.Protocol.Models.SymbolKind.Method,
            SymbolKind.Property => OmniSharp.Extensions.LanguageServer.Protocol.Models.SymbolKind.Property,
            SymbolKind.Parameter => OmniSharp.Extensions.LanguageServer.Protocol.Models.SymbolKind.Variable,
            SymbolKind.Constant => OmniSharp.Extensions.LanguageServer.Protocol.Models.SymbolKind.Constant,
            _ => OmniSharp.Extensions.LanguageServer.Protocol.Models.SymbolKind.Variable
        };
    }

    /// <summary>
    /// 获取符号的容器名称
    /// </summary>
    private static string? GetContainerName(SymbolInfo symbolInfo)
    {
        // 如果有父符号，返回父符号的名称
        return symbolInfo.Parent?.Name;
    }
}