namespace Old8Lang.LanguageServer.Models;

/// <summary>
/// 符号信息 - 用于符号表
/// </summary>
public class SymbolInfo
{
    /// <summary>
    /// 符号名称
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// 符号类型（变量、函数、类等）
    /// </summary>
    public required SymbolKind Kind { get; set; }

    /// <summary>
    /// 符号的类型（数据类型）
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// 符号定义的位置
    /// </summary>
    public required SourceLocation Location { get; set; }

    /// <summary>
    /// 符号的文档注释
    /// </summary>
    public string? Documentation { get; set; }

    /// <summary>
    /// 符号的引用位置列表
    /// </summary>
    public List<SourceLocation> References { get; set; } = new();
}

/// <summary>
/// 符号类型
/// </summary>
public enum SymbolKind
{
    Variable,
    Function,
    Class,
    Method,
    Property,
    Parameter,
    Constant
}

/// <summary>
/// 源代码位置
/// </summary>
public class SourceLocation
{
    public required string Uri { get; set; }
    public required int Line { get; set; }
    public required int Column { get; set; }
    public int EndLine { get; set; }
    public int EndColumn { get; set; }
}
