namespace Old8Lang.Modules.Abstractions;

/// <summary>
/// 描述一个由宿主显式注册的 Old8Lang 标准库模块。
/// </summary>
public interface IStandardLibraryModule
{
    /// <summary>
    /// 将模块的所有导出注册到指定注册表。
    /// </summary>
    /// <param name="registry">当前宿主的标准库注册表。</param>
    void Register(IStandardLibraryRegistry registry);
}

/// <summary>
/// 提供标准库模块的注册入口。
/// </summary>
public interface IStandardLibraryRegistry
{
    /// <summary>
    /// 注册一个标准库模块描述。
    /// </summary>
    /// <param name="descriptor">模块的程序集和导出类型描述。</param>
    void Register(StandardLibraryModuleDescriptor descriptor);
}

/// <summary>
/// 标准库模块的稳定描述，不要求语言核心引用模块程序集。
/// </summary>
/// <param name="Name">Old8Lang 中使用的模块名。</param>
/// <param name="AssemblyName">包含导出类型的程序集名称。</param>
/// <param name="Exports">模块公开给 Old8Lang 的类型。</param>
public sealed record StandardLibraryModuleDescriptor(
    string Name,
    string AssemblyName,
    IReadOnlyList<StandardLibraryTypeExport> Exports);

/// <summary>
/// 标准库模块中的一个类型导出。
/// </summary>
/// <param name="Type">要导入的 CLR 类型。</param>
/// <param name="ImportStaticMembers">是否将静态成员直接作为模块成员导入。</param>
/// <param name="UseNativeStaticAny">是否使用 NativeStaticAny 包装静态成员。</param>
public sealed record StandardLibraryTypeExport(
    Type Type,
    bool ImportStaticMembers,
    bool UseNativeStaticAny = false)
{
    /// <summary>
    /// 创建静态类导出。
    /// </summary>
    /// <param name="type">静态类类型。</param>
    /// <param name="useNativeStaticAny">是否使用 NativeStaticAny 包装。</param>
    /// <returns>静态成员导出描述。</returns>
    public static StandardLibraryTypeExport StaticClass(Type type, bool useNativeStaticAny = false)
        => new(type, true, useNativeStaticAny);

    /// <summary>
    /// 创建普通 CLR 类导出。
    /// </summary>
    /// <param name="type">普通类类型。</param>
    /// <returns>普通类型导出描述。</returns>
    public static StandardLibraryTypeExport NormalClass(Type type)
        => new(type, false);
}
