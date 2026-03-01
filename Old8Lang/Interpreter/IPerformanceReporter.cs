namespace Old8Lang.Interpreter;

/// <summary>
/// 性能报告生成器接口
/// </summary>
public interface IPerformanceReporter
{
    /// <summary>
    /// 生成文本格式报告
    /// </summary>
    string GenerateTextReport(PerformanceMetrics metrics);

    /// <summary>
    /// 生成 JSON 格式报告
    /// </summary>
    string GenerateJsonReport(PerformanceMetrics metrics);

    /// <summary>
    /// 生成 CSV 格式报告
    /// </summary>
    string GenerateCsvReport(PerformanceMetrics metrics);

    /// <summary>
    /// 保存报告到文件
    /// </summary>
    void SaveReport(string content, string filePath);
}
