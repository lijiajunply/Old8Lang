using Old8Lang.Modules.Abstractions;

namespace Old8Lang.ModuleSystem.StandardLibrary;

/// <summary>
/// 一个独立的标准库模块目录。
/// 宿主可创建多个目录，避免模块注册在测试或嵌入式运行时之间泄漏。
/// </summary>
public sealed class StandardLibraryCatalog : IStandardLibraryRegistry
{
    /// <summary>
    /// 共享的空目录。
    /// </summary>
    /// <remarks>
    /// 用于没有宿主上下文的临时执行状态，使这些状态不会隐式看到进程默认目录。
    /// 这是共享实例，不应向其中注册模块；需要注册时请通过宿主创建独立目录。
    /// </remarks>
    public static StandardLibraryCatalog Empty { get; } = new(isFrozen: true);

    private readonly Dictionary<string, StandardLibraryInfo> _libraries = new(StringComparer.Ordinal);
    private readonly bool _isFrozen;
    private readonly Lock _registrationLock = new();

    /// <summary>
    /// 创建宿主私有的标准库目录。
    /// </summary>
    public StandardLibraryCatalog()
    {
    }

    private StandardLibraryCatalog(bool isFrozen)
    {
        _isFrozen = isFrozen;
    }

    /// <summary>
    /// 已注册标准库的只读视图。
    /// </summary>
    public IReadOnlyDictionary<string, StandardLibraryInfo> Libraries => _libraries;

    /// <summary>
    /// 注册模块中的所有标准库描述。
    /// </summary>
    /// <param name="module">由宿主提供的标准库模块。</param>
    public void Register(IStandardLibraryModule module)
    {
        ArgumentNullException.ThrowIfNull(module);
        if (_isFrozen)
        {
            throw new InvalidOperationException("共享的空标准库目录不可注册模块；请使用宿主创建的标准库目录。");
        }

        module.Register(this);
    }

    /// <inheritdoc />
    public void Register(StandardLibraryModuleDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        if (_isFrozen)
        {
            throw new InvalidOperationException("共享的空标准库目录不可注册模块；请使用宿主创建的标准库目录。");
        }

        var info = StandardLibraryInfo.FromTypes(
            descriptor.AssemblyName,
            descriptor.Exports.Select(export => new TypeImportConfig(
                export.Type,
                export.ImportStaticMembers,
                export.UseNativeStaticAny)).ToArray());

        lock (_registrationLock)
        {
            if (_libraries.TryGetValue(descriptor.Name, out var existing))
            {
                if (HasSameConfiguration(existing, info))
                {
                    return;
                }

                throw new InvalidOperationException($"标准库 '{descriptor.Name}' 已注册为不同的模块描述。");
            }

            _libraries.Add(descriptor.Name, info);
        }
    }

    /// <summary>
    /// 判断模块名是否已注册。
    /// </summary>
    /// <param name="name">Old8Lang 模块名。</param>
    /// <returns>已注册时为 true。</returns>
    public bool IsStandardLibrary(string name) => _libraries.ContainsKey(name);

    /// <summary>
    /// 获取模块信息。
    /// </summary>
    /// <param name="name">Old8Lang 模块名。</param>
    /// <returns>模块信息；未注册时为 null。</returns>
    public StandardLibraryInfo? GetLibraryInfo(string name) => _libraries.GetValueOrDefault(name);

    /// <summary>
    /// 获取全部模块名。
    /// </summary>
    /// <returns>当前目录中的模块名。</returns>
    public IEnumerable<string> GetAllLibraryNames() => _libraries.Keys;

    private static bool HasSameConfiguration(StandardLibraryInfo first, StandardLibraryInfo second)
    {
        if (!string.Equals(first.AssemblyName, second.AssemblyName, StringComparison.Ordinal) ||
            first.ImportMode != second.ImportMode ||
            first.TypeConfigs is null ||
            second.TypeConfigs is null ||
            first.TypeConfigs.Count != second.TypeConfigs.Count)
        {
            return false;
        }

        return first.TypeConfigs.Zip(second.TypeConfigs).All(pair =>
            pair.First.Type == pair.Second.Type &&
            pair.First.ImportStaticMembers == pair.Second.ImportStaticMembers &&
            pair.First.UseNativeStaticAny == pair.Second.UseNativeStaticAny);
    }
}
