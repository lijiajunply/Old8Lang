namespace Old8Lang.LangParser.Core;

/// <summary>
/// 解析器共享上下文，管理 tokens、索引、源代码等状态
/// </summary>
public class ParserContext
{
    private readonly List<LangToken> _tokens;
    private string[]? _cachedSourceLines; // 缓存分割后的源代码行

    /// <summary>
    /// 源代码（用于错误上下文）
    /// </summary>
    public string? SourceCode { get; }

    /// <summary>
    /// 文件名（用于错误报告）
    /// </summary>
    public string? FileName { get; }

    /// <summary>
    /// 当前令牌索引
    /// </summary>
    public int CurrentIndex { get; set; }

    /// <summary>
    /// 获取缓存的源代码行（延迟初始化，避免在无错误时分割）
    /// 注意：保留空行以确保行号正确匹配
    /// </summary>
    public string[] SourceLines
    {
        get
        {
            if (_cachedSourceLines == null && !string.IsNullOrEmpty(SourceCode))
            {
                // 使用 '\n' 分割并保留空行，确保行号正确对应
                // 注意：Split by '\n' 会保留 '\r'，所以需要在使用时 Trim
                _cachedSourceLines = SourceCode.Split('\n');
            }
            return _cachedSourceLines ?? Array.Empty<string>();
        }
    }

    /// <summary>
    /// 获取令牌列表
    /// </summary>
    public List<LangToken> Tokens => _tokens;

    /// <summary>
    /// 获取当前令牌
    /// </summary>
    public LangToken CurrentToken => CurrentIndex >= _tokens.Count
        ? new LangToken("", LangTokenType.EndOfFile, CurrentIndex)
        : _tokens[CurrentIndex];

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="tokens">令牌列表</param>
    /// <param name="sourceCode">源代码</param>
    /// <param name="fileName">文件名</param>
    public ParserContext(List<LangToken> tokens, string? sourceCode = null, string? fileName = null)
    {
        _tokens = tokens;
        SourceCode = sourceCode;
        FileName = fileName;
        CurrentIndex = 0;
    }

    /// <summary>
    /// 查看后续 Token
    /// </summary>
    /// <param name="offset">偏移量（默认为1）</param>
    /// <returns>后续令牌</returns>
    public LangToken Peek(int offset = 1)
    {
        return CurrentIndex + offset >= _tokens.Count
            ? new LangToken("", LangTokenType.EndOfFile, CurrentIndex + offset)
            : _tokens[CurrentIndex + offset];
    }
}
