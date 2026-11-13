using Old8Lang.Modules.Abstractions;

namespace Old8Lang.ModuleSystem.StandardLibrary;

/// <summary>
/// 默认宿主使用的标准库目录。
/// 新的嵌入式宿主应创建 <see cref="StandardLibraryCatalog"/> 并通过 Hosting 项目显式安装；
/// 此静态入口仅用于 CLI 与尚未迁移的执行路径。
/// </summary>
public static class StandardLibraryRegistry
{
    private static StandardLibraryCatalog _defaultCatalog = new();

    /// <summary>
    /// 当前默认模块目录。
    /// </summary>
    public static StandardLibraryCatalog DefaultCatalog => Volatile.Read(ref _defaultCatalog);

    /// <summary>
    /// 当前默认目录中的模块只读视图。
    /// </summary>
    public static IReadOnlyDictionary<string, StandardLibraryInfo> Libraries => DefaultCatalog.Libraries;

    /// <summary>
    /// 替换默认模块目录。仅应用组合根应调用此方法。
    /// </summary>
    /// <param name="catalog">要安装的目录。</param>
    public static void SetDefaultCatalog(StandardLibraryCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        Interlocked.Exchange(ref _defaultCatalog, catalog);
    }

    /// <summary>
    /// 向默认目录注册模块。
    /// </summary>
    /// <param name="module">模块实现。</param>
    public static void Register(IStandardLibraryModule module)
    {
        DefaultCatalog.Register(module);
    }

    /// <summary>
    /// 向默认目录注册模块描述。
    /// </summary>
    /// <param name="descriptor">模块描述。</param>
    public static void Register(StandardLibraryModuleDescriptor descriptor)
    {
        DefaultCatalog.Register(descriptor);
    }

    /// <summary>
    /// 判断模块名是否已注册。
    /// </summary>
    /// <param name="name">Old8Lang 模块名。</param>
    /// <returns>已注册时为 true。</returns>
    public static bool IsStandardLibrary(string name) => DefaultCatalog.IsStandardLibrary(name);

    /// <summary>
    /// 获取默认目录中的模块信息。
    /// </summary>
    /// <param name="name">Old8Lang 模块名。</param>
    /// <returns>模块信息；未注册时为 null。</returns>
    public static StandardLibraryInfo? GetLibraryInfo(string name) => DefaultCatalog.GetLibraryInfo(name);

    /// <summary>
    /// 获取默认目录中的全部模块名。
    /// </summary>
    /// <returns>模块名序列。</returns>
    public static IEnumerable<string> GetAllLibraryNames() => DefaultCatalog.GetAllLibraryNames();
}
