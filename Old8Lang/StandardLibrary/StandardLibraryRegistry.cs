namespace Old8Lang.StandardLibrary;

/// <summary>
/// 标准库注册表，管理所有内置的标准库
/// 替代原有的 LangInfo.json 配置文件
/// </summary>
public static class StandardLibraryRegistry
{
    /// <summary>
    /// 所有标准库的注册信息
    /// </summary>
    public static readonly Dictionary<string, StandardLibraryInfo> Libraries = new()
    {
        // 来自 Old8LangLib 的标准库
        ["OS"] = new StandardLibraryInfo(AssemblyName: "Old8LangLib",
            ClassName: "OS"
        ),

        ["File"] = new StandardLibraryInfo(AssemblyName: "Old8LangLib",
            ClassName: "FileLib"
        ),

        ["Terminal"] = new StandardLibraryInfo(AssemblyName: "Old8LangLib",
            ClassName: "Terminal"
        ),

        ["Time"] = new StandardLibraryInfo(AssemblyName: "Old8LangLib",
            ClassName: "Time"
        ),

        ["MathLib"] = new StandardLibraryInfo(AssemblyName: "Old8LangLib",
            ClassName: "MathLib"
        ),

        ["Async"] = new StandardLibraryInfo(AssemblyName: "Old8LangLib",
            ClassName: "AsyncLib"
        ),

        ["Crypto"] = new StandardLibraryInfo(AssemblyName: "Old8LangLib",
            ClassName: "CryptoLib"
        ),

        ["CollectionLib"] = new StandardLibraryInfo(AssemblyName: "Old8LangLib",
            ClassName: "CollectionLib"
        ),

        ["Json"] = new StandardLibraryInfo(AssemblyName: "Old8LangLib",
            ClassName: "JsonLib"
        ),

        ["Csv"] = new StandardLibraryInfo(AssemblyName: "Old8LangLib",
            ClassName: "Csv"
        ),

        ["Vector"] = new StandardLibraryInfo(AssemblyName: "Old8LangLib",
            ClassName: "VectorLib"
        ),

        ["Regex"] = new StandardLibraryInfo(AssemblyName: "Old8LangLib",
            ClassName: "RegexLib"
        ),

        ["Sort"] = new StandardLibraryInfo(AssemblyName: "Old8LangLib",
            ClassName: "SortLib"
        ),

        ["ColorfulTerminal"] = new StandardLibraryInfo(AssemblyName: "Old8LangLib",
            ClassName: "ColorfulTerminal"
        ),

        // 来自 Old8Lang.NetLib 的网络库（多类库）
        ["Net"] = new StandardLibraryInfo(AssemblyName: "Old8Lang.NetLib",
            ClassNames:
            [
                "SocketClient",
                "HttpWebClient",
                "MqttClientWrapper",
                "WebSocketClient",
                "WebApiClient"
            ]
        ),
        ["TemplateEngine"] = new StandardLibraryInfo(AssemblyName: "Old8LangLib",
            ClassName: "TemplateEngine"),
        ["ImageLib"] = new StandardLibraryInfo(AssemblyName: "Old8LangLib",
            ClassName: "ImageLib")
    };

    /// <summary>
    /// 检查指定名称是否为标准库
    /// </summary>
    public static bool IsStandardLibrary(string name) => Libraries.ContainsKey(name);

    /// <summary>
    /// 获取标准库信息
    /// </summary>
    public static StandardLibraryInfo? GetLibraryInfo(string name)
    {
        return Libraries.GetValueOrDefault(name);
    }
}