namespace Old8Lang.LangParser.Core;

using Old8Lang.AST.Statement;

/// <summary>
/// 解析器共享上下文，管理 tokens、索引、源代码等状态
/// </summary>
public class ParserContext
{
    private readonly List<LangToken> _tokens;
    private string[]? _cachedSourceLines; // 缓存分割后的源代码行
    private TokenIndexCache? _tokenIndexCache; // Token 索引缓存

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
    /// 文件头指令集合
    /// </summary>
    public FileHeaderDirectives HeaderDirectives { get; } = new();

    /// <summary>
    /// 是否启用 Token 索引缓存（默认启用）
    /// </summary>
    public bool EnableTokenIndexCache { get; set; } = true;

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

    /// <summary>
    /// 获取 Token 索引缓存（延迟初始化）
    /// </summary>
    /// <returns>Token 索引缓存实例</returns>
    public TokenIndexCache GetTokenIndexCache()
    {
        if (!EnableTokenIndexCache)
        {
            // 如果未启用缓存，返回一个新的未构建索引的实例
            return new TokenIndexCache(_tokens);
        }

        if (_tokenIndexCache == null)
        {
            _tokenIndexCache = new TokenIndexCache(_tokens);
            _tokenIndexCache.BuildIndex();
        }

        return _tokenIndexCache;
    }

    /// <summary>
    /// 在指定范围内查找下一个指定类型的 Token（使用缓存优化）
    /// </summary>
    /// <param name="type">Token 类型</param>
    /// <param name="startIndex">开始索引（默认从当前位置）</param>
    /// <param name="endIndex">结束索引（-1 表示到结尾）</param>
    /// <returns>Token 索引，未找到返回 -1</returns>
    public int FindNextToken(LangTokenType type, int? startIndex = null, int endIndex = -1)
    {
        int actualStartIndex = startIndex ?? CurrentIndex;
        return GetTokenIndexCache().FindNextToken(type, actualStartIndex, endIndex);
    }

    /// <summary>
    /// 统计指定范围内指定类型的 Token 数量（使用缓存优化）
    /// </summary>
    /// <param name="type">Token 类型</param>
    /// <param name="startIndex">开始索引（默认从当前位置）</param>
    /// <param name="endIndex">结束索引（-1 表示到结尾）</param>
    /// <returns>Token 数量</returns>
    public int CountTokens(LangTokenType type, int? startIndex = null, int endIndex = -1)
    {
        int actualStartIndex = startIndex ?? CurrentIndex;
        return GetTokenIndexCache().CountTokens(type, actualStartIndex, endIndex);
    }
}
