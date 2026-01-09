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

                // 检查当前符号是否匹配查询
                bool symbolMatches = !hasQuery || MatchesQuery(symbolName, query);

                // 如果符号匹配查询，添加到结果中
                if (symbolMatches)
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
                }

                // 如果是类，检查其成员（即使类本身不匹配查询，成员仍可能匹配）
                if (symbolInfo.Kind == SymbolKind.Class)
                {
                    foreach (var (memberName, memberInfo) in symbolInfo.Members)
                    {
                        // 优化：如果没有查询且已经收集了足够多的符号，则跳过成员
                        if (!hasQuery && symbols.Count >= maxSymbolsWithoutQuery)
                        {
                            break;
                        }

                        // 检查成员是否匹配查询
                        if (!hasQuery || MatchesQuery(memberName, query))
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

    /// <summary>
    /// 检查符号名是否匹配查询（支持子字符串匹配、前缀匹配和模糊匹配）
    /// </summary>
    /// <param name="symbolName">符号名称</param>
    /// <param name="query">查询字符串（已转为小写）</param>
    /// <returns>是否匹配</returns>
    private static bool MatchesQuery(string symbolName, string query)
    {
        var lowerSymbolName = symbolName.ToLower();

        // 1. 子字符串匹配（已有的逻辑）
        if (lowerSymbolName.Contains(query))
        {
            return true;
        }

        // 2. 前缀匹配：查询是符号名的前缀
        // 例如：查询 "calc" 匹配 "Calculator"
        if (lowerSymbolName.StartsWith(query))
        {
            return true;
        }

        // 3. CamelCase 匹配：查询匹配驼峰命名的首字母
        // 例如：查询 "gur" 匹配 "getUserRole"
        if (MatchesCamelCase(symbolName, query))
        {
            return true;
        }

        // 4. 模糊匹配：查询的所有字符按顺序出现在符号名中
        // 例如：查询 "calculate" 匹配 "Calculator"（calc-u-lat-e -> calc-u-lat-or）
        if (FuzzyMatch(lowerSymbolName, query))
        {
            return true;
        }

        // 5. 相似度匹配：如果符号名和查询非常相似（允许少量字符差异）
        // 例如：查询 "calculate" 匹配 "Calculator"
        if (SimilarityMatch(lowerSymbolName, query))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 相似度匹配：检查符号名和查询是否足够相似
    /// 使用编辑距离阈值进行匹配
    /// </summary>
    /// <param name="symbolName">符号名称（小写）</param>
    /// <param name="query">查询字符串（小写）</param>
    /// <returns>是否匹配</returns>
    private static bool SimilarityMatch(string symbolName, string query)
    {
        // 如果长度差异太大，不匹配
        if (Math.Abs(symbolName.Length - query.Length) > 3)
        {
            return false;
        }

        // 计算编辑距离（Levenshtein距离）
        int distance = LevenshteinDistance(symbolName, query);

        // 如果编辑距离小于等于2，认为足够相似
        // 例如："calculator" 和 "calculate" 的编辑距离是 2 (删除 'e'，添加 'or')
        return distance <= 2;
    }

    /// <summary>
    /// 计算两个字符串的 Levenshtein 编辑距离
    /// </summary>
    private static int LevenshteinDistance(string s1, string s2)
    {
        int[,] d = new int[s1.Length + 1, s2.Length + 1];

        for (int i = 0; i <= s1.Length; i++)
        {
            d[i, 0] = i;
        }

        for (int j = 0; j <= s2.Length; j++)
        {
            d[0, j] = j;
        }

        for (int j = 1; j <= s2.Length; j++)
        {
            for (int i = 1; i <= s1.Length; i++)
            {
                int cost = (s1[i - 1] == s2[j - 1]) ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost
                );
            }
        }

        return d[s1.Length, s2.Length];
    }

    /// <summary>
    /// 模糊匹配：检查查询的所有字符是否按顺序出现在符号名中
    /// </summary>
    /// <param name="symbolName">符号名称（小写）</param>
    /// <param name="query">查询字符串（小写）</param>
    /// <returns>是否匹配</returns>
    private static bool FuzzyMatch(string symbolName, string query)
    {
        int queryIndex = 0;
        int symbolIndex = 0;

        while (queryIndex < query.Length && symbolIndex < symbolName.Length)
        {
            if (query[queryIndex] == symbolName[symbolIndex])
            {
                queryIndex++;
            }
            symbolIndex++;
        }

        // 如果查询的所有字符都找到了，则匹配成功
        return queryIndex == query.Length;
    }

    /// <summary>
    /// 检查查询是否匹配符号的驼峰命名首字母
    /// </summary>
    /// <param name="symbolName">符号名称（原始大小写）</param>
    /// <param name="query">查询字符串（小写）</param>
    /// <returns>是否匹配</returns>
    private static bool MatchesCamelCase(string symbolName, string query)
    {
        if (string.IsNullOrEmpty(symbolName) || string.IsNullOrEmpty(query))
        {
            return false;
        }

        var camelCaseLetters = new List<char>();

        // 提取驼峰命名的大写字母
        // 第一个字符总是包含（无论大小写）
        camelCaseLetters.Add(char.ToLower(symbolName[0]));

        // 遍历剩余字符，提取大写字母
        for (int i = 1; i < symbolName.Length; i++)
        {
            if (char.IsUpper(symbolName[i]))
            {
                camelCaseLetters.Add(char.ToLower(symbolName[i]));
            }
        }

        // 检查查询是否匹配驼峰首字母序列
        if (camelCaseLetters.Count < query.Length)
        {
            return false;
        }

        int queryIndex = 0;
        for (int i = 0; i < camelCaseLetters.Count && queryIndex < query.Length; i++)
        {
            if (camelCaseLetters[i] == query[queryIndex])
            {
                queryIndex++;
            }
        }

        return queryIndex == query.Length;
    }
}