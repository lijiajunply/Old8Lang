namespace Old8Lang;

/// <summary>
/// 表示源代码中的位置信息，用于错误报告和调试
/// </summary>
/// <remarks>
/// 行列基准不一致，这是历史遗留问题，使用方需注意：
/// <list type="bullet">
/// <item><see cref="Line"/> 从 1 开始（与词法分析器一致）。</item>
/// <item><see cref="Column"/> 是相对于行首的 0 起始偏移量，直接来自
/// <c>LangToken.Column</c>（词法器按 <c>索引 - 行首索引</c> 计算）。</item>
/// </list>
/// <see cref="ToString"/> 会在展示时把列号转成 1 起始，因此打印出来的是人读的行列。
/// 注意 <c>Old8Lang.LanguageServer</c> 内部对列基准的假设并不统一（部分处理器按 0 起始使用，
/// 部分又做了 <c>- 1</c> 转换），修改列基准取值时需要一并核对。
/// </remarks>
/// <param name="line">行号（从1开始）</param>
/// <param name="column">列号（相对行首的0起始偏移量）</param>
/// <param name="fileName">文件名（可选）</param>
/// <param name="tokenValue">原始令牌值（可选，用于调试）</param>
public readonly struct SourcePosition(int line, int column, string? fileName = null, string? tokenValue = null)
{
    /// <summary>
    /// 获取源代码中的行号（从1开始计数）
    /// </summary>
    public readonly int Line = line;

    /// <summary>
    /// 获取源代码中的列号（相对行首的0起始偏移量）
    /// </summary>
    public readonly int Column = column;

    /// <summary>
    /// 获取源代码文件名（如果可用）
    /// </summary>
    public readonly string? FileName = fileName;

    /// <summary>
    /// 获取原始令牌值（用于调试目的）
    /// </summary>
    public readonly string? TokenValue = tokenValue;

    /// <summary>
    /// 将位置信息转换为字符串表示形式
    /// </summary>
    /// <returns>格式化的位置字符串，格式为 "文件名(行:列)" 或 "行:列"，行列均从1开始</returns>
    /// <remarks>
    /// 位置未知时（非正的行号，例如 <c>new SourcePosition(0, 0)</c>）原样输出，避免显示成 "0:1" 这类无意义的值。
    /// </remarks>
    public override string ToString()
    {
        if (Line <= 0)
        {
            return FileName is not null ? $"{FileName}({Line}:{Column})" : $"{Line}:{Column}";
        }

        var displayColumn = Column + 1;
        return FileName is not null ? $"{FileName}({Line}:{displayColumn})" : $"{Line}:{displayColumn}";
    }
}
