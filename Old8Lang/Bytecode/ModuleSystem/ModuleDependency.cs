namespace Old8Lang.Bytecode.ModuleSystem;

/// <summary>
/// 模块依赖信息
/// </summary>
public class ModuleDependency
{
    /// <summary>
    /// 模块名称或路径
    /// </summary>
    public string ModuleName { get; set; }

    /// <summary>
    /// 导入的符号列表（null表示导入整个模块）
    /// </summary>
    public List<ImportedSymbol>? ImportedSymbols { get; set; }

    /// <summary>
    /// 是否导入所有符号（import * from "module"）
    /// </summary>
    public bool ImportAll { get; set; }

    /// <summary>
    /// 模块别名（import "module" as alias）
    /// </summary>
    public string? ModuleAlias { get; set; }

    public ModuleDependency(string moduleName)
    {
        ModuleName = moduleName;
        ImportedSymbols = null;
        ImportAll = false;
        ModuleAlias = null;
    }

    public override string ToString()
    {
        if (ImportAll)
            return $"import * from \"{ModuleName}\"";
        if (ImportedSymbols != null && ImportedSymbols.Count > 0)
            return $"import {{ {string.Join(", ", ImportedSymbols)} }} from \"{ModuleName}\"";
        if (ModuleAlias != null)
            return $"import \"{ModuleName}\" as {ModuleAlias}";
        return $"import \"{ModuleName}\"";
    }
}

/// <summary>
/// 导入的符号信息
/// </summary>
public class ImportedSymbol
{
    /// <summary>
    /// 原始符号名称
    /// </summary>
    public string OriginalName { get; set; }

    /// <summary>
    /// 别名（如果有）
    /// </summary>
    public string? Alias { get; set; }

    /// <summary>
    /// 实际使用的名称（别名或原始名称）
    /// </summary>
    public string EffectiveName => Alias ?? OriginalName;

    public ImportedSymbol(string originalName, string? alias = null)
    {
        OriginalName = originalName;
        Alias = alias;
    }

    public override string ToString()
    {
        return Alias != null ? $"{OriginalName} as {Alias}" : OriginalName;
    }
}
