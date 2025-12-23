namespace Old8Lang.StandardLibrary;

/// <summary>
/// 表示标准库的元信息
/// </summary>
public record StandardLibraryInfo(
    string Name,
    string Version,
    string AssemblyName,
    string? ClassName = null,
    string[]? ClassNames = null)
{
    /// <summary>
    /// 库名称
    /// </summary>
    public string Name { get; init; } = Name;

    /// <summary>
    /// 库版本
    /// </summary>
    public string Version { get; init; } = Version;

    /// <summary>
    /// 程序集名称
    /// </summary>
    public string AssemblyName { get; init; } = AssemblyName;

    /// <summary>
    /// 类名（单类库）
    /// </summary>
    public string? ClassName { get; init; } = ClassName;

    /// <summary>
    /// 多个类名（多类库）
    /// </summary>
    public string[]? ClassNames { get; init; } = ClassNames;

    /// <summary>
    /// 是否为多类库
    /// </summary>
    public bool IsMultiClass => ClassNames is { Length: > 0 };
}
