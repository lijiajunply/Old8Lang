namespace Old8Lang.Error;

/// <summary>
/// 错误报告中展示的源代码上下文窗口
/// </summary>
/// <remarks>
/// 仅凭行数组无法还原窗口对应的真实行号：窗口在文件头/尾被裁剪后不再是
/// 以错误行为中心的固定长度，此时用 "错误行号 - 长度/2" 反推会整体错位，
/// 导致行号标注和脱字符落在错误的行上。因此把起始行号随行数组一起传递。
/// </remarks>
/// <param name="Lines">窗口内的源代码行（按文件顺序，包含空行）</param>
/// <param name="StartLine">窗口中第一行对应的真实行号（从1开始）</param>
public readonly struct SourceContextWindow(string[] lines, int startLine)
{
    /// <summary>
    /// 空窗口
    /// </summary>
    public static readonly SourceContextWindow Empty = new([], 0);

    /// <summary>
    /// 窗口内的源代码行（按文件顺序，包含空行）
    /// </summary>
    public string[] Lines { get; } = lines;

    /// <summary>
    /// 窗口中第一行对应的真实行号（从1开始）；为0表示未知
    /// </summary>
    public int StartLine { get; } = startLine;

    /// <summary>
    /// 是否为空窗口
    /// </summary>
    public bool IsEmpty => Lines.Length == 0;

    /// <summary>
    /// 已知起始行号时返回该行号，否则按"窗口以错误行为中心"的旧假设估算
    /// </summary>
    /// <param name="errorLine">错误所在行号</param>
    /// <returns>窗口中第一行的行号</returns>
    public int ResolveStartLine(int errorLine)
    {
        return StartLine > 0 ? StartLine : errorLine - Lines.Length / 2;
    }

    /// <summary>
    /// 由行数组构造窗口，起始行号未知
    /// </summary>
    public static SourceContextWindow FromLines(string[]? lines)
    {
        return lines is { Length: > 0 } ? new SourceContextWindow(lines, 0) : Empty;
    }
}
