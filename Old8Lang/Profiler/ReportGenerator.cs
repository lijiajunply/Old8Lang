using System.Text;

namespace Old8Lang.Profiler;

/// <summary>
/// 报告生成器 - 简化版本
/// </summary>
public class ReportGenerator
{
    /// <summary>
    /// 生成性能报告
    /// </summary>
    /// <param name="summary">性能摘要</param>
    /// <param name="format">报告格式</param>
    /// <returns>报告文本</returns>
    public string GenerateReport(PerformanceSummary summary, ReportFormat format = ReportFormat.Text)
    {
        return format switch
        {
            ReportFormat.Text => GenerateTextReport(summary),
            ReportFormat.Json => GenerateJsonReport(summary),
            ReportFormat.Csv => GenerateCsvReport(summary),
            ReportFormat.Markdown => GenerateMarkdownReport(summary),
            _ => throw new ArgumentException($"不支持的报告格式: {format}")
        };
    }

    /// <summary>
    /// 生成文本格式报告
    /// </summary>
    private static string GenerateTextReport(PerformanceSummary summary)
    {
        var sb = new StringBuilder();
        var session = summary.Session;

        // 标题
        sb.AppendLine("========================================================================");
        sb.AppendLine("Old8Lang 性能分析报告");
        sb.AppendLine("========================================================================");
        sb.AppendLine();

        // 基本信息
        sb.AppendLine("基本信息:");
        sb.AppendLine($"  会话ID: {session.SessionId}");
        sb.AppendLine($"  会话名称: {session.Name}");
        sb.AppendLine($"  源文件: {session.SourceFilePath ?? "N/A"}");
        sb.AppendLine($"  执行模式: {session.ExecutionMode ?? "N/A"}");
        sb.AppendLine($"  开始时间: {session.StartTime:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"  结束时间: {session.EndTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "进行中"}");
        sb.AppendLine($"  总执行时间: {session.DurationMs:F2}ms");
        sb.AppendLine($"  性能分数: {summary.FormattedScore}");
        sb.AppendLine();

        // 关键指标
        sb.AppendLine("关键指标:");
        var totalCalls = session.FunctionStats.Values.Sum(f => f.CallCount);
        sb.AppendLine($"  函数调用总数: {totalCalls:N0}");
        sb.AppendLine($"  独特函数数量: {session.FunctionStats.Count}");

        if (session.MemoryHistory.Count > 0)
        {
            var maxMemory = session.MemoryHistory.Max(m => m.ManagedMemoryMb);
            var avgMemory = session.MemoryHistory.Average(m => m.ManagedMemoryMb);
            var totalGc = session.MemoryHistory.Last().TotalGcCollections;
            sb.AppendLine($"  峰值内存使用: {maxMemory:F2}MB");
            sb.AppendLine($"  平均内存使用: {avgMemory:F2}MB");
            sb.AppendLine($"  垃圾回收次数: {totalGc:N0}");
        }

        sb.AppendLine();

        // 热点函数
        sb.AppendLine("热点函数 (按总执行时间排序):");
        var hotspots = session.GetHotspotFunctions();
        for (int i = 0; i < hotspots.Count; i++)
        {
            var func = hotspots[i];
            sb.AppendLine(
                $"  {(i + 1)}. {func.FunctionName} - {func.TotalExecutionTimeMs:F2}ms ({func.CallCount:N0} calls)");
        }

        sb.AppendLine();

        // 性能瓶颈
        if (summary.Bottlenecks.Count > 0)
        {
            sb.AppendLine("性能瓶颈:");
            foreach (var bottleneck in summary.Bottlenecks.Take(5))
            {
                sb.AppendLine($"  - [{bottleneck.Type}] {bottleneck.Description}");
                sb.AppendLine($"    严重程度: {bottleneck.Severity}/10");
                if (!string.IsNullOrEmpty(bottleneck.Suggestion))
                {
                    sb.AppendLine($"    建议: {bottleneck.Suggestion}");
                }

                sb.AppendLine();
            }
        }
        else
        {
            sb.AppendLine("🎉 未发现明显性能瓶颈!");
            sb.AppendLine();
        }

        // 建议
        sb.AppendLine("总体建议:");
        sb.AppendLine($"  {summary.Recommendation}");
        sb.AppendLine();

        // 页脚
        sb.AppendLine("========================================================================");
        sb.AppendLine($"报告生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("Old8Lang 性能分析器 v1.0");
        sb.AppendLine("========================================================================");

        return sb.ToString();
    }

    /// <summary>
    /// 生成JSON格式报告
    /// </summary>
    private static string GenerateJsonReport(PerformanceSummary summary)
    {
        var report = new
        {
            metadata = new
            {
                generatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                profilerVersion = "1.0"
            },
            session = new
            {
                id = summary.Session.SessionId.ToString(),
                name = summary.Session.Name,
                sourceFile = summary.Session.SourceFilePath,
                executionMode = summary.Session.ExecutionMode,
                startTime = summary.Session.StartTime.ToString("yyyy-MM-dd HH:mm:ss"),
                endTime = summary.Session.EndTime?.ToString("yyyy-MM-dd HH:mm:ss"),
                durationMs = summary.Session.DurationMs
            },
            summary = new
            {
                overallScore = summary.OverallScore,
                grade = summary.Grade.ToString(),
                recommendation = summary.Recommendation,
                totalFunctionCalls = summary.Session.FunctionStats.Values.Sum(f => f.CallCount),
                uniqueFunctions = summary.Session.FunctionStats.Count
            },
            bottlenecks = summary.Bottlenecks.Select(b => new
            {
                type = b.Type.ToString(),
                description = b.Description,
                severity = b.Severity,
                functionName = b.FunctionName,
                suggestion = b.Suggestion,
                metrics = b.Metrics
            }),
            functions = summary.Session.FunctionStats.Values.Select(f => new
            {
                name = f.FunctionName,
                callCount = f.CallCount,
                totalTimeMs = f.TotalExecutionTimeMs,
                avgTimeMs = f.AverageExecutionTimeMs,
                minTimeMs = f.MinExecutionTimeMs,
                maxTimeMs = f.MaxExecutionTimeMs,
                sourceFile = f.SourceFile,
                lineNumber = f.LineNumber
            })
        };

        return System.Text.Json.JsonSerializer.Serialize(report, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        });
    }

    /// <summary>
    /// 生成CSV格式报告
    /// </summary>
    private static string GenerateCsvReport(PerformanceSummary summary)
    {
        var sb = new StringBuilder();

        // 函数性能数据
        sb.AppendLine("Function,CallCount,TotalTimeMs,AvgTimeMs,MinTimeMs,MaxTimeMs,SourceFile,LineNumber");
        foreach (var func in summary.Session.FunctionStats.Values)
        {
            sb.AppendLine($"{EscapeCsvField(func.FunctionName)},{func.CallCount},{func.TotalExecutionTimeMs:F2}," +
                          $"{func.AverageExecutionTimeMs:F2},{func.MinExecutionTimeMs:F2},{func.MaxExecutionTimeMs:F2}," +
                          $"{EscapeCsvField(func.SourceFile ?? "")},{func.LineNumber ?? 0}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// 生成Markdown格式报告
    /// </summary>
    private static string GenerateMarkdownReport(PerformanceSummary summary)
    {
        var sb = new StringBuilder();
        var session = summary.Session;

        // 标题
        sb.AppendLine("# Old8Lang 性能分析报告");
        sb.AppendLine();

        // 基本信息
        sb.AppendLine("## 基本信息");
        sb.AppendLine();
        sb.AppendLine($"- **会话ID**: `{session.SessionId}`");
        sb.AppendLine($"- **源文件**: `{session.SourceFilePath ?? "N/A"}`");
        sb.AppendLine($"- **执行模式**: `{session.ExecutionMode ?? "N/A"}`");
        sb.AppendLine($"- **执行时间**: `{session.DurationMs:F2}ms`");
        sb.AppendLine($"- **性能分数**: `{summary.FormattedScore}`");
        sb.AppendLine();

        // 热点函数
        sb.AppendLine("## 🔥 热点函数");
        sb.AppendLine();
        sb.AppendLine("| 排名 | 函数名 | 总时间 | 调用次数 | 平均时间 |");
        sb.AppendLine("|------|--------|--------|----------|----------|");

        var hotspots = session.GetHotspotFunctions();
        for (int i = 0; i < hotspots.Count; i++)
        {
            var func = hotspots[i];
            sb.AppendLine(
                $"| {i + 1} | `{func.FunctionName}` | `{func.TotalExecutionTimeMs:F2}ms` | `{func.CallCount:N0}` | `{func.AverageExecutionTimeMs:F2}ms` |");
        }

        sb.AppendLine();

        // 性能瓶颈
        if (summary.Bottlenecks.Count > 0)
        {
            sb.AppendLine("## ⚠️ 性能瓶颈");
            sb.AppendLine();
            foreach (var bottleneck in summary.Bottlenecks)
            {
                var severityIcon = bottleneck.Severity >= 7 ? "🔴" : bottleneck.Severity >= 4 ? "🟡" : "🟢";
                sb.AppendLine($"### {severityIcon} {bottleneck.Type}");
                sb.AppendLine();
                sb.AppendLine($"**描述**: {bottleneck.Description}");
                sb.AppendLine($"**严重程度**: {bottleneck.Severity}/10");
                if (!string.IsNullOrEmpty(bottleneck.Suggestion))
                {
                    sb.AppendLine($"**建议**: {bottleneck.Suggestion}");
                }

                sb.AppendLine();
            }
        }
        else
        {
            sb.AppendLine("## 🎉 性能表现");
            sb.AppendLine();
            sb.AppendLine("✅ 未发现明显性能瓶颈！");
            sb.AppendLine();
        }

        // 建议
        sb.AppendLine("## 💡 总体建议");
        sb.AppendLine();
        sb.AppendLine(summary.Recommendation);
        sb.AppendLine();

        // 页脚
        sb.AppendLine("---");
        sb.AppendLine($"*报告生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}*");
        sb.AppendLine("*Old8Lang 性能分析器 v1.0*");

        return sb.ToString();
    }

    /// <summary>
    /// 转义CSV字段
    /// </summary>
    private static string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
            return "";

        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }

        return field;
    }

    /// <summary>
    /// 将报告保存到文件
    /// </summary>
    public async Task SaveReportAsync(PerformanceSummary summary, string filePath,
        ReportFormat format = ReportFormat.Text)
    {
        var report = GenerateReport(summary, format);
        await File.WriteAllTextAsync(filePath, report, Encoding.UTF8);
    }
}