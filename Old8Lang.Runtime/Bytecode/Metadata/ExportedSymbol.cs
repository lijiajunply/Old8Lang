namespace Old8Lang.Bytecode.ModuleSystem;

/// <summary>
/// 导出符号类型
/// </summary>
public enum ExportedSymbolType
{
    Function,
    Class,
    Variable,
    Interface,
    Mixin,
    Enum
}

/// <summary>
/// 导出符号信息
/// </summary>
public class ExportedSymbol(string name, ExportedSymbolType type, object? value = null, int metadataIndex = -1)
{
    /// <summary>
    /// 符号名称
    /// </summary>
    public string Name { get; set; } = name;

    /// <summary>
    /// 符号类型
    /// </summary>
    public ExportedSymbolType Type { get; set; } = type;

    /// <summary>
    /// 符号值（可能是函数、类定义等）
    /// </summary>
    public object? Value { get; set; } = value;

    /// <summary>
    /// 元数据索引（在BytecodeFile中的索引）
    /// </summary>
    public int MetadataIndex { get; set; } = metadataIndex;

    public override string ToString()
    {
        return $"{Type} {Name}";
    }
}
