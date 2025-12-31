using System.Reflection;

namespace Old8Lang.StandardLibrary;

/// <summary>
/// 标准库导入模式
/// </summary>
public enum StandardLibraryImportMode
{
    /// <summary>
    /// 整个程序集导入（全局导入）
    /// 使用 a.b 来访问子命名空间
    /// </summary>
    Assembly,

    /// <summary>
    /// 导入指定类型
    /// </summary>
    Types
}

/// <summary>
/// 类型导入配置
/// </summary>
public class TypeImportConfig
{
    /// <summary>
    /// 类型对象
    /// </summary>
    public Type Type { get; }

    /// <summary>
    /// 是否为静态类直接导入（导入其静态成员）
    /// 如果为 false，则导入为 NativeAnyLangValue
    /// </summary>
    public bool ImportStaticMembers { get; }

    /// <summary>
    /// 是否使用 NativeStaticAny（仅当 ImportStaticMembers 为 true 时有效）
    /// </summary>
    public bool UseNativeStaticAny { get; }

    public TypeImportConfig(Type type, bool importStaticMembers, bool useNativeStaticAny = false)
    {
        Type = type;
        ImportStaticMembers = importStaticMembers;
        UseNativeStaticAny = useNativeStaticAny;
    }

    /// <summary>
    /// 创建静态类导入配置（直接导入静态成员）
    /// </summary>
    public static TypeImportConfig StaticClass(Type type, bool useNativeStaticAny = false)
        => new(type, importStaticMembers: true, useNativeStaticAny);

    /// <summary>
    /// 创建普通类导入配置（导入为 NativeAnyLangValue）
    /// </summary>
    public static TypeImportConfig NormalClass(Type type)
        => new(type, importStaticMembers: false, useNativeStaticAny: false);
}

/// <summary>
/// 表示标准库的元信息
/// </summary>
public class StandardLibraryInfo
{
    /// <summary>
    /// 程序集对象
    /// </summary>
    public Assembly? Assembly { get; }

    /// <summary>
    /// 程序集名称（用于延迟加载）
    /// </summary>
    public string? AssemblyName { get; }

    /// <summary>
    /// 导入模式
    /// </summary>
    public StandardLibraryImportMode ImportMode { get; }

    /// <summary>
    /// 类型导入配置列表（仅在 ImportMode 为 Types 时使用）
    /// </summary>
    public List<TypeImportConfig>? TypeConfigs { get; }

    /// <summary>
    /// 是否支持子命名空间导入（仅在 ImportMode 为 Assembly 时使用）
    /// </summary>
    public bool EnableSubNamespaceImport { get; }

    /// <summary>
    /// 根命名空间（用于筛选程序集中的类型）
    /// </summary>
    public string? RootNamespace { get; }

    // 私有构造函数，强制使用工厂方法
    private StandardLibraryInfo(
        Assembly? assembly,
        string? assemblyName,
        StandardLibraryImportMode importMode,
        List<TypeImportConfig>? typeConfigs,
        bool enableSubNamespaceImport,
        string? rootNamespace)
    {
        Assembly = assembly;
        AssemblyName = assemblyName;
        ImportMode = importMode;
        TypeConfigs = typeConfigs;
        EnableSubNamespaceImport = enableSubNamespaceImport;
        RootNamespace = rootNamespace;
    }

    /// <summary>
    /// 创建程序集导入配置
    /// </summary>
    public static StandardLibraryInfo FromAssembly(
        Assembly assembly,
        bool enableSubNamespaceImport = true,
        string? rootNamespace = null)
    {
        return new StandardLibraryInfo(
            assembly: assembly,
            assemblyName: assembly.GetName().Name,
            importMode: StandardLibraryImportMode.Assembly,
            typeConfigs: null,
            enableSubNamespaceImport: enableSubNamespaceImport,
            rootNamespace: rootNamespace ?? assembly.GetName().Name);
    }

    /// <summary>
    /// 创建程序集导入配置（通过程序集名称，延迟加载）
    /// </summary>
    public static StandardLibraryInfo FromAssemblyName(
        string assemblyName,
        bool enableSubNamespaceImport = true,
        string? rootNamespace = null)
    {
        return new StandardLibraryInfo(
            assembly: null,
            assemblyName: assemblyName,
            importMode: StandardLibraryImportMode.Assembly,
            typeConfigs: null,
            enableSubNamespaceImport: enableSubNamespaceImport,
            rootNamespace: rootNamespace ?? assemblyName);
    }

    /// <summary>
    /// 创建类型导入配置
    /// </summary>
    public static StandardLibraryInfo FromTypes(
        string assemblyName,
        params TypeImportConfig[] typeConfigs)
    {
        return new StandardLibraryInfo(
            assembly: null,
            assemblyName: assemblyName,
            importMode: StandardLibraryImportMode.Types,
            typeConfigs: typeConfigs.ToList(),
            enableSubNamespaceImport: false,
            rootNamespace: null);
    }

    /// <summary>
    /// 创建类型导入配置（直接传入类型）
    /// </summary>
    public static StandardLibraryInfo FromTypes(
        params TypeImportConfig[] typeConfigs)
    {
        // 从第一个类型获取程序集名称
        var assemblyName = typeConfigs.FirstOrDefault()?.Type.Assembly.GetName().Name;
        return new StandardLibraryInfo(
            assembly: null,
            assemblyName: assemblyName,
            importMode: StandardLibraryImportMode.Types,
            typeConfigs: typeConfigs.ToList(),
            enableSubNamespaceImport: false,
            rootNamespace: null);
    }

    /// <summary>
    /// 获取或加载程序集
    /// </summary>
    public Assembly GetAssembly()
    {
        if (Assembly != null)
            return Assembly;

        if (string.IsNullOrEmpty(AssemblyName))
            throw new InvalidOperationException("程序集名称为空");

        // 使用 StandardLibraryLoader 的方法来加载程序集
        return StandardLibraryLoader.GetOrLoadAssembly(AssemblyName);
    }
}
