namespace Old8Lang;

/// <summary>
/// 表示源代码中的位置信息，用于错误报告和调试
/// </summary>
/// <remarks>
/// <para>行列基准并不一致，这是全语言统一的约定，不是历史遗留的错误，使用方需按位置分别处理：</para>
/// <list type="bullet">
/// <item><see cref="Line"/> 从 1 开始（与 <c>LangToken.Line</c> 一致）。</item>
/// <item><see cref="Column"/> 是相对于行首的 0 起始偏移量，直接来自
/// <c>LangToken.Column</c>（词法器按 <c>索引 - 行首索引</c> 计算）。</item>
/// </list>
/// <para>
/// LSP 协议里 <c>Position.Line</c> 与 <c>Position.Character</c> 都是 0 起始，
/// 因此从本结构体跨界到 LSP 时：<b>行号要减 1，列号原样使用</b>。
/// 反过来，<see cref="ToString"/> 面向人展示时才把列号转成 1 起始。
/// </para>
/// <para>
/// 也就是说「列号已经是从 0 开始的偏移量」是唯一正确的读法；曾经有若干处理器
/// （折叠、语义高亮、诊断、作用域分析）误以为列号 1 起始而多做了一次 <c>- 1</c>，
/// 造成整体左偏一列。<c>Old8Lang.Tests/LanguageServer/PositionBasisTests.cs</c>
/// 逐条钉住了这些出口的列号，改动列基准前请先看那里的约定说明。
/// </para>
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
