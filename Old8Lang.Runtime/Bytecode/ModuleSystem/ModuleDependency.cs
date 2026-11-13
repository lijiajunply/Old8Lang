namespace Old8Lang.Bytecode.ModuleSystem;

/// <summary>
/// 模块依赖信息
/// </summary>
public class ModuleDependency(string moduleName)
{
    /// <summary>
    /// 模块名称或路径
    /// </summary>
    public string ModuleName { get; set; } = moduleName;

    /// <summary>
    /// 导入的符号列表（null表示导入整个模块）
    /// </summary>
    public List<ImportedSymbol>? ImportedSymbols { get; set; } = null;

    /// <summary>
    /// 是否导入所有符号（import * from "module"）
    /// </summary>
    public bool ImportAll { get; set; } = false;

    /// <summary>
    /// 模块别名（import "module" as alias）
    /// </summary>
    public string? ModuleAlias { get; set; } = null;

    public override string ToString()
    {
        if (ImportAll)
            return $"import * from \"{ModuleName}\"";
        if (ImportedSymbols is { Count: > 0 })
            return $"import {{ {string.Join(", ", ImportedSymbols)} }} from \"{ModuleName}\"";
        if (ModuleAlias != null)
            return $"import \"{ModuleName}\" as {ModuleAlias}";
        return $"import \"{ModuleName}\"";
    }
}

/// <summary>
/// 导入的符号信息
/// </summary>
public class ImportedSymbol(string originalName, string? alias = null)
{
    /// <summary>
    /// 原始符号名称
    /// </summary>
    public string OriginalName { get; set; } = originalName;

    /// <summary>
    /// 别名（如果有）
    /// </summary>
    public string? Alias { get; set; } = alias;

    /// <summary>
    /// 实际使用的名称（别名或原始名称）
    /// </summary>
    public string EffectiveName => Alias ?? OriginalName;

    public override string ToString()
    {
        return Alias != null ? $"{OriginalName} as {Alias}" : OriginalName;
    }
}
