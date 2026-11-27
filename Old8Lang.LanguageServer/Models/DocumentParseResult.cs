using Old8Lang.LangParser;
using Old8Lang.AST.Statement;

namespace Old8Lang.LanguageServer.Models;

/// <summary>
/// 表示一个文档的解析结果
/// </summary>
public class DocumentParseResult
{
    private List<LangToken>? _tokens;
    private TokenLineIndex? _tokenIndex;

    /// <summary>
    /// 文档 URI
    /// </summary>
    public required string Uri { get; set; }

    /// <summary>
    /// 文档文本内容
    /// </summary>
    public required string Text { get; set; }

    /// <summary>
    /// 文档版本号（由 DocumentManager 递增分配，用于后台诊断竞态过滤）
    /// </summary>
    public long Version { get; set; }

    /// <summary>
    /// 词法分析的 Token 列表（赋值时自动失效并按需重建按行索引）
    /// </summary>
    public List<LangToken>? Tokens
    {
        get => _tokens;
        set
        {
            _tokens = value;
            _tokenIndex = null;
        }
    }

    /// <summary>
    /// 按行建立的 Token 索引（O(1) 行定位 + O(log K) 列查找）
    /// </summary>
    public TokenLineIndex TokenIndex
    {
        get
        {
            if (_tokenIndex != null)
            {
                return _tokenIndex;
            }

            _tokenIndex = _tokens is { Count: > 0 }
                ? new TokenLineIndex(_tokens)
                : TokenLineIndex.Empty;
            return _tokenIndex;
        }
    }

    /// <summary>
    /// 语法分析的 AST
    /// </summary>
    public BlockStatement? Ast { get; set; }

    /// <summary>
    /// 顶层符号表（保持向后兼容，仅包含全局作用域符号）
    /// </summary>
    public Dictionary<string, SymbolInfo>? SymbolTable { get; set; }

    /// <summary>
    /// 词法作用域树根结点（全局作用域，包含嵌套的函数、类、方法、块作用域）
    /// </summary>
    public SymbolScope? RootScope { get; set; }

    /// <summary>
    /// 顶层脚本作用域中的直接调用点列表（供 Call Hierarchy 查询）
    /// </summary>
    public List<CallSite> TopLevelCalls { get; set; } = [];

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
