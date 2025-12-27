using Old8Lang.LangParser;
using Old8Lang.AST.Statement;

namespace Old8Lang.LanguageServer.Models;

/// <summary>
/// 表示一个文档的解析结果
/// </summary>
public class DocumentParseResult
{
    /// <summary>
    /// 文档 URI
    /// </summary>
    public required string Uri { get; set; }

    /// <summary>
    /// 文档文本内容
    /// </summary>
    public required string Text { get; set; }

    /// <summary>
    /// 词法分析的 Token 列表
    /// </summary>
    public List<LangToken>? Tokens { get; set; }

    /// <summary>
    /// 语法分析的 AST
    /// </summary>
    public BlockStatement? Ast { get; set; }

    /// <summary>
    /// 符号表
    /// </summary>
    public Dictionary<string, SymbolInfo>? SymbolTable { get; set; }

    /// <summary>
    /// 诊断信息列表
    /// </summary>
    public required List<DiagnosticInfo> Diagnostics { get; set; }
}

/// <summary>
/// 诊断信息
/// </summary>
public class DiagnosticInfo
{
    public required DiagnosticSeverity Severity { get; set; }
    public required string Message { get; set; }
    public required int Line { get; set; }
    public required int Column { get; set; }
    public required string Source { get; set; }
}

/// <summary>
/// 诊断严重级别
/// </summary>
public enum DiagnosticSeverity
{
    Error = 1,
    Warning = 2,
    Information = 3,
    Hint = 4
}
