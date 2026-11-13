using Old8Lang.DatabaseLib;
using Old8Lang.MachineLearningLib;
using Old8Lang.ModuleSystem.StandardLibrary;
using Old8Lang.Modules.Abstractions;
using Old8Lang.NetLib;
using Old8Lang.SerializationLib;
using Old8LangLib;

namespace Old8Lang.Modules.Official;

/// <summary>
/// 官方发行版随附的标准库模块集合。
/// </summary>
public static class OfficialModules
{
    private static readonly IStandardLibraryModule Module = new OfficialStandardLibraryModule();

    /// <summary>
    /// 向当前进程的默认运行时注册全部官方模块。
    /// 可重复调用；重复注册相同模块不会改变已注册的描述。
    /// </summary>
    public static void RegisterAll()
    {
        StandardLibraryRegistry.Register(Module);
    }

    /// <summary>
    /// 向指定注册表注册全部官方模块。
    /// </summary>
    /// <param name="registry">宿主提供的模块注册表。</param>
    public static void RegisterAll(IStandardLibraryRegistry registry)
    {
        Module.Register(registry);
    }

    private sealed class OfficialStandardLibraryModule : IStandardLibraryModule
    {
        public void Register(IStandardLibraryRegistry registry)
        {
            registry.Register(new StandardLibraryModuleDescriptor("OS", "Old8LangLib",
                [StandardLibraryTypeExport.StaticClass(typeof(OS))]));
            registry.Register(new StandardLibraryModuleDescriptor("File", "Old8LangLib",
                [StandardLibraryTypeExport.StaticClass(typeof(FileLib))]));
            registry.Register(new StandardLibraryModuleDescriptor("Terminal", "Old8LangLib",
                [StandardLibraryTypeExport.StaticClass(typeof(Terminal))]));
            registry.Register(new StandardLibraryModuleDescriptor("Time", "Old8LangLib",
                [StandardLibraryTypeExport.StaticClass(typeof(Time))]));
            registry.Register(new StandardLibraryModuleDescriptor("Math", "Old8LangLib",
                [StandardLibraryTypeExport.StaticClass(typeof(MathLib))]));
            registry.Register(new StandardLibraryModuleDescriptor("Crypto", "Old8LangLib",
                [StandardLibraryTypeExport.StaticClass(typeof(CryptoLib))]));
            registry.Register(new StandardLibraryModuleDescriptor("Csv", "Old8LangLib",
                [StandardLibraryTypeExport.StaticClass(typeof(Csv))]));
            registry.Register(new StandardLibraryModuleDescriptor("Vector", "Old8LangLib",
            [
                StandardLibraryTypeExport.NormalClass(typeof(Vector2)),
                StandardLibraryTypeExport.NormalClass(typeof(Vector3)),
                StandardLibraryTypeExport.NormalClass(typeof(Vector4)),
                StandardLibraryTypeExport.NormalClass(typeof(VectorN))
            ]));
            registry.Register(new StandardLibraryModuleDescriptor("Regex", "Old8LangLib",
                [StandardLibraryTypeExport.StaticClass(typeof(RegexLib))]));
            registry.Register(new StandardLibraryModuleDescriptor("ColorfulTerminal", "Old8LangLib",
                [StandardLibraryTypeExport.StaticClass(typeof(ColorfulTerminal))]));
            registry.Register(new StandardLibraryModuleDescriptor("TemplateEngine", "Old8LangLib",
                [StandardLibraryTypeExport.StaticClass(typeof(TemplateEngine))]));
            registry.Register(new StandardLibraryModuleDescriptor("Net", "Old8Lang.NetLib",
            [
                StandardLibraryTypeExport.NormalClass(typeof(SocketClient)),
                StandardLibraryTypeExport.NormalClass(typeof(HttpWebClient)),
                StandardLibraryTypeExport.NormalClass(typeof(MqttClientWrapper)),
                StandardLibraryTypeExport.NormalClass(typeof(WebSocketClient)),
                StandardLibraryTypeExport.NormalClass(typeof(WebApiClient))
            ]));
            registry.Register(new StandardLibraryModuleDescriptor("MachineLearning", "Old8Lang.MachineLearningLib",
                [StandardLibraryTypeExport.StaticClass(typeof(MachineLearningLibBinding))]));
            registry.Register(new StandardLibraryModuleDescriptor("Serialization", "Old8Lang.SerializationLib",
                [StandardLibraryTypeExport.StaticClass(typeof(SerializationLibBinding))]));
            registry.Register(new StandardLibraryModuleDescriptor("Database", "Old8Lang.DatabaseLib",
                [StandardLibraryTypeExport.StaticClass(typeof(DatabaseLibBinding))]));
        }
    }
}
