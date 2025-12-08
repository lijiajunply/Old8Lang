namespace Old8Lang;

/// <summary>
/// 表示源代码中的位置信息
/// </summary>
public readonly struct SourcePosition
{
    /// <summary>
    /// 行号（从1开始）
    /// </summary>
    public readonly int Line;

    /// <summary>
    /// 列号（从1开始）
    /// </summary>
    public readonly int Column;

    /// <summary>
    /// 文件名（可选）
    /// </summary>
    public readonly string? FileName;

    /// <summary>
    /// 原始令牌值（可选）
    /// </summary>
    public readonly string? TokenValue;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="line">行号</param>
    /// <param name="column">列号</param>
    /// <param name="fileName">文件名</param>
    /// <param name="tokenValue">令牌值</param>
    public SourcePosition(int line, int column, string? fileName = null, string? tokenValue = null)
    {
        Line = line;
        Column = column;
        FileName = fileName;
        TokenValue = tokenValue;
    }

    /// <summary>
    /// 表示无位置信息
    /// </summary>
    public static readonly SourcePosition None = new(0, 0);

    public override string ToString()
    {
        return FileName is not null ? $"{FileName}({Line}:{Column})" : $"{Line}:{Column}";
    }
}