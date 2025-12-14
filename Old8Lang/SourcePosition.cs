namespace Old8Lang;

/// <summary>
/// 表示源代码中的位置信息，用于错误报告和调试
/// </summary>
/// <param name="line">行号（从1开始）</param>
/// <param name="column">列号（从1开始）</param>
/// <param name="fileName">文件名（可选）</param>
/// <param name="tokenValue">原始令牌值（可选，用于调试）</param>
public readonly struct SourcePosition(int line, int column, string? fileName = null, string? tokenValue = null)
{
    /// <summary>
    /// 获取源代码中的行号（从1开始计数）
    /// </summary>
    public readonly int Line = line;

    /// <summary>
    /// 获取源代码中的列号（从1开始计数）
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
    /// <returns>格式化的位置字符串，格式为 "文件名(行:列)" 或 "行:列"</returns>
    public override string ToString()
    {
        return FileName is not null ? $"{FileName}({Line}:{Column})" : $"{Line}:{Column}";
    }
}