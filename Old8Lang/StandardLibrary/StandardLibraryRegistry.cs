using Old8Lang.DatabaseLib;
using Old8Lang.FirstUI;
using Old8Lang.FirstUI.Basic;
using Old8Lang.FirstUI.Core;
using Old8Lang.FirstUI.Layout;
using Old8Lang.MachineLearningLib;
using Old8Lang.NetLib;
using Old8Lang.SerializationLib;
using Old8LangLib;

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
        // 来自 Old8LangLib 的标准库（使用延迟加载）
        ["OS"] = StandardLibraryInfo.FromTypes(
            "Old8LangLib",
            TypeImportConfig.StaticClass(typeof(OS))
        ),

        ["File"] = StandardLibraryInfo.FromTypes(
            "Old8LangLib",
            TypeImportConfig.StaticClass(typeof(FileLib))
        ),

        ["Terminal"] = StandardLibraryInfo.FromTypes(
            "Old8LangLib",
            TypeImportConfig.StaticClass(typeof(Terminal))
        ),

        ["Time"] = StandardLibraryInfo.FromTypes(
            "Old8LangLib",
            TypeImportConfig.StaticClass(typeof(Time))
        ),

        ["MathLib"] = StandardLibraryInfo.FromTypes(
            "Old8LangLib",
            TypeImportConfig.StaticClass(typeof(MathLib))
        ),

        ["Math"] = StandardLibraryInfo.FromTypes(
            "Old8LangLib",
            TypeImportConfig.StaticClass(typeof(MathLib))
        ),

        ["Async"] = StandardLibraryInfo.FromTypes(
            "Old8LangLib",
            TypeImportConfig.StaticClass(typeof(AsyncLib))
        ),

        ["Crypto"] = StandardLibraryInfo.FromTypes(
            "Old8LangLib",
            TypeImportConfig.StaticClass(typeof(CryptoLib))
        ),

        ["CollectionLib"] = StandardLibraryInfo.FromTypes(
            "Old8LangLib",
            TypeImportConfig.StaticClass(typeof(CollectionLib))
        ),

        ["Json"] = StandardLibraryInfo.FromTypes(
            "Old8LangLib",
            TypeImportConfig.StaticClass(typeof(JsonLib))
        ),

        ["Csv"] = StandardLibraryInfo.FromTypes(
            "Old8LangLib",
            TypeImportConfig.StaticClass(typeof(Csv))
        ),

        ["Vector"] = StandardLibraryInfo.FromTypes(
            "Old8LangLib",
            TypeImportConfig.StaticClass(typeof(VectorLib))
        ),

        ["Regex"] = StandardLibraryInfo.FromTypes(
            "Old8LangLib",
            TypeImportConfig.StaticClass(typeof(RegexLib))
        ),

        ["ColorfulTerminal"] = StandardLibraryInfo.FromTypes(
            "Old8LangLib",
            TypeImportConfig.StaticClass(typeof(ColorfulTerminal))
        ),

        ["TemplateEngine"] = StandardLibraryInfo.FromTypes(
            "Old8LangLib",
            TypeImportConfig.StaticClass(typeof(TemplateEngine))
        ),

        ["Image"] = StandardLibraryInfo.FromTypes(
            "Old8LangLib",
            TypeImportConfig.StaticClass(typeof(ImageLib))
        ),

        // 来自 Old8Lang.NetLib 的网络库（多类库）
        ["Net"] = StandardLibraryInfo.FromTypes(
            "Old8Lang.NetLib",
            TypeImportConfig.NormalClass(typeof(SocketClient)),
            TypeImportConfig.NormalClass(typeof(HttpWebClient)),
            TypeImportConfig.NormalClass(typeof(MqttClientWrapper)),
            TypeImportConfig.NormalClass(typeof(WebSocketClient)),
            TypeImportConfig.NormalClass(typeof(WebApiClient))
        ),

        // 其他扩展库（延迟加载）
        ["MachineLearning"] = StandardLibraryInfo.FromTypes(
            "Old8Lang.MachineLearningLib",
            TypeImportConfig.StaticClass(
                typeof(MachineLearningLibBinding)
            )
        ),

        ["Serialization"] = StandardLibraryInfo.FromTypes(
            "Old8Lang.SerializationLib",
            TypeImportConfig.StaticClass(
                typeof(SerializationLibBinding)
            )
        ),

        ["Database"] = StandardLibraryInfo.FromTypes(
            "Old8Lang.DatabaseLib",
            TypeImportConfig.StaticClass(
                typeof(DatabaseLibBinding)
            )
        ),

        ["FirstUI"] = StandardLibraryInfo.FromTypes(
            "Old8Lang.FirstUI",
            TypeImportConfig.StaticClass(
                typeof(FirstUIBinding)
            ),
            TypeImportConfig.NormalClass(
                typeof(Container)
            ),
            TypeImportConfig.NormalClass(
                typeof(Column)
            ),
            TypeImportConfig.NormalClass(
                typeof(BuildContext)
            ),
            TypeImportConfig.NormalClass(
                typeof(StateManager)
            ),
            TypeImportConfig.NormalClass(
                typeof(Thickness)
            ),
            TypeImportConfig.NormalClass(
                typeof(Text)
            ),
            TypeImportConfig.NormalClass(
                typeof(Button)
            )
        )
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

    /// <summary>
    /// 获取所有标准库名称列表
    /// </summary>
    public static IEnumerable<string> GetAllLibraryNames() => Libraries.Keys;
}