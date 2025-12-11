namespace Old8Lang;

/// <summary>
/// 表示源代码中的位置信息
/// </summary>
/// <remarks>
/// 构造函数
/// </remarks>
/// <param name="line">行号</param>
/// <param name="column">列号</param>
/// <param name="fileName">文件名</param>
/// <param name="tokenValue">令牌值</param>
public readonly struct SourcePosition(int line, int column, string? fileName = null, string? tokenValue = null)
{
    /// <summary>
    /// 行号（从1开始）
    /// </summary>
    public readonly int Line = line;

    /// <summary>
    /// 列号（从1开始）
    /// </summary>
    public readonly int Column = column;

    /// <summary>
    /// 文件名（可选）
    /// </summary>
    public readonly string? FileName = fileName;

    /// <summary>
    /// 原始令牌值（可选）
    /// </summary>
    public readonly string? TokenValue = tokenValue;

    public override string ToString()
    {
        return FileName is not null ? $"{FileName}({Line}:{Column})" : $"{Line}:{Column}";
    }
}