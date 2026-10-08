namespace Old8Lang.LangParser.Core;

using AST.Statement;

/// <summary>
/// 解析器共享上下文，管理 tokens、索引、源代码等状态
/// </summary>
public class ParserContext
{
    private readonly List<LangToken> _tokens;
    private TokenIndexCache? _tokenIndexCache; // Token 索引缓存
    private int _recursionDepth; // 递归深度计数器

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
    public string[] SourceLines { get; private set; } = Array.Empty<string>();

    /// <summary>
    /// 获取令牌列表
    /// </summary>
    public List<LangToken> Tokens => _tokens;

    /// <summary>
    /// 获取当前令牌
    /// </summary>
    public LangToken CurrentToken => CurrentIndex >= _tokens.Count
        ? CreateEndOfFileToken()
        : _tokens[CurrentIndex];

    /// <summary>
    /// 创建文件结束标记
    /// 注意：行号/列号必须取自最后一个真实标记，而不是标记下标，
    /// 否则在 EOF 处报错时会显示成 "第 N 行"（N 为标记数量）这类无意义的位置
    /// </summary>
    private LangToken CreateEndOfFileToken()
    {
        if (_tokens.Count == 0)
        {
            return new LangToken("", LangTokenType.EndOfFile, 1, 1);
        }

        var last = _tokens[^1];
        return new LangToken("", LangTokenType.EndOfFile, last.Line, last.Column + last.Value.Length);
    }

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
        _recursionDepth = 0;

        // 预先分割源代码行，避免在错误报告时重复分割
        if (!string.IsNullOrEmpty(sourceCode))
        {
            SourceLines = sourceCode.Split('\n');
        }
    }

    /// <summary>
    /// 查看后续 Token
    /// </summary>
    /// <param name="offset">偏移量（默认为1）</param>
    /// <returns>后续令牌</returns>
    public LangToken Peek(int offset = 1)
    {
        return CurrentIndex + offset >= _tokens.Count
            ? CreateEndOfFileToken()
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

        if (_tokenIndexCache is null)
        {
            _tokenIndexCache = new TokenIndexCache(_tokens);
            _tokenIndexCache.BuildIndex();
        }

        return _tokenIndexCache;
    }

    /// <summary>
    /// 进入递归层级（用于防止栈溢出）
    /// </summary>
    /// <exception cref="Error.SyntaxError">当递归深度超过限制时抛出</exception>
    public void EnterRecursion()
    {
        if (++_recursionDepth > 500)
        {
            throw new Error.SyntaxError(
                "表达式嵌套过深",
                0,
                0,
                "表达式嵌套层数超过最大限制（500层），可能存在无限递归");
        }
    }

    /// <summary>
    /// 退出递归层级
    /// </summary>
    public void ExitRecursion()
    {
        _recursionDepth--;
    }
}