namespace Old8Lang.StandardLibrary;

/// <summary>
/// 表示标准库的元信息
/// </summary>
public record StandardLibraryInfo(
    string AssemblyName,
    string? ClassName = null,
    string[]? ClassNames = null)
{
    /// <summary>
    /// 程序集名称
    /// </summary>
    public string AssemblyName { get; } = AssemblyName;

    /// <summary>
    /// 类名（单类库）
    /// </summary>
    public string? ClassName { get; } = ClassName;

    /// <summary>
    /// 多个类名（多类库）
    /// </summary>
    public string[]? ClassNames { get; } = ClassNames;

    /// <summary>
    /// 是否为多类库
    /// </summary>
    public bool IsMultiClass => ClassNames is { Length: > 0 };
}
