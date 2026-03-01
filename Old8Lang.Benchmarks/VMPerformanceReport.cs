using System.Globalization;
using System.Text.Json;
using Microsoft.VisualBasic.FileIO;

namespace Old8Lang.Benchmarks;

/// <summary>
/// VM 性能报告生成器（仅解析 BenchmarkDotNet 产物）
/// </summary>
public static class VMPerformanceReport
{
    private sealed record VmScenarioResult(
        string Scenario,
        double? MeanMs,
        double? StdDevMs,
        double? P95Ms,
        long? AllocatedBytes,
        string RawMean,
        string RawStdDev,
        string RawAllocated);

    private sealed record VmReportModel(
        DateTime GeneratedAt,
        string SourceCsv,
        string Job,
        string Runtime,
        string WarmupCount,
        string IterationCount,
        IReadOnlyList<string> Warnings,
        IReadOnlyList<VmScenarioResult> Scenarios);

    /// <summary>
    /// 从 BenchmarkDotNet artifacts 解析并生成 VM 报告（md + json）
    /// </summary>
    /// <param name="artifactsDir">BenchmarkDotNet.Artifacts/results 目录</param>
    /// <param name="reportsDir">报告输出目录（通常为 Reports）</param>
    /// <returns>Markdown 与 JSON 报告路径</returns>
    public static (string MarkdownPath, string JsonPath) GenerateFromBenchmarkArtifacts(string artifactsDir, string reportsDir)
    {
        var csvPath = ResolveLatestVmCsv(artifactsDir);
        var report = ParseCsv(csvPath);

        Directory.CreateDirectory(reportsDir);
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        var markdownPath = Path.Combine(reportsDir, $"VM_Performance_Report_{timestamp}.md");
        var jsonPath = Path.Combine(reportsDir, $"VM_Performance_Report_{timestamp}.json");

        WriteMarkdown(report, markdownPath);
        WriteJson(report, jsonPath);
        return (markdownPath, jsonPath);
    }

    private static string ResolveLatestVmCsv(string artifactsDir)
    {
        if (!Directory.Exists(artifactsDir))
        {
            throw new DirectoryNotFoundException($"BenchmarkDotNet artifacts 目录不存在: {artifactsDir}");
        }

        var candidates = Directory
            .GetFiles(artifactsDir, "*VMModePerformanceBenchmarks*-report.csv", System.IO.SearchOption.TopDirectoryOnly)
            .Select(path => new FileInfo(path))
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .ToArray();

        if (candidates.Length == 0)
        {
            throw new FileNotFoundException($"未找到 VM BenchmarkDotNet 报告 CSV，目录: {artifactsDir}");
        }

        return candidates[0].FullName;
    }

    private static VmReportModel ParseCsv(string csvPath)
    {
        var warnings = new List<string>();
        var scenarios = new List<VmScenarioResult>();
        Dictionary<string, int>? headerIndex = null;
        string job = "N/A";
        string runtime = "N/A";
        string warmupCount = "N/A";
        string iterationCount = "N/A";

        using var parser = new TextFieldParser(csvPath)
        {
            TextFieldType = FieldType.Delimited,
            HasFieldsEnclosedInQuotes = true
        };
        parser.SetDelimiters(",");

        if (parser.EndOfData)
        {
            throw new InvalidDataException($"CSV 为空: {csvPath}");
        }

        var headers = parser.ReadFields();
        if (headers is null || headers.Length == 0)
        {
            throw new InvalidDataException($"CSV 头部为空: {csvPath}");
        }

        headerIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < headers.Length; i++)
        {
            var key = headers[i].Trim();
            if (!headerIndex.ContainsKey(key))
            {
                headerIndex[key] = i;
            }
        }

        var requiredColumns = new[] { "Method", "Mean", "StdDev", "Allocated" };
        foreach (var column in requiredColumns)
        {
            if (!headerIndex.ContainsKey(column))
            {
                throw new InvalidDataException($"CSV 缺少必要列: {column}");
            }
        }

        while (!parser.EndOfData)
        {
            var fields = parser.ReadFields();
            if (fields is null || fields.Length == 0)
            {
                continue;
            }

            var method = ReadField(fields, headerIndex, "Method");
            if (string.IsNullOrWhiteSpace(method))
            {
                continue;
            }

            var rawMean = ReadField(fields, headerIndex, "Mean");
            var rawStdDev = ReadField(fields, headerIndex, "StdDev");
            var rawAllocated = ReadField(fields, headerIndex, "Allocated");

            job = ReadField(fields, headerIndex, "Job", job);
            runtime = ReadField(fields, headerIndex, "Runtime", runtime);
            warmupCount = ReadField(fields, headerIndex, "WarmupCount", warmupCount);
            iterationCount = ReadField(fields, headerIndex, "IterationCount", iterationCount);

            var meanMs = ParseDurationToMilliseconds(rawMean, out var meanWarning);
            var stdDevMs = ParseDurationToMilliseconds(rawStdDev, out var stdDevWarning);
            var allocatedBytes = ParseBytes(rawAllocated, out var allocatedWarning);
            double? p95Ms = null; // CSV 默认不含 P95，统一标记 N/A

            if (meanWarning is not null)
            {
                warnings.Add($"{method}: Mean 解析警告 - {meanWarning}");
            }
            if (stdDevWarning is not null)
            {
                warnings.Add($"{method}: StdDev 解析警告 - {stdDevWarning}");
            }
            if (allocatedWarning is not null)
            {
                warnings.Add($"{method}: Allocated 解析警告 - {allocatedWarning}");
            }

            scenarios.Add(new VmScenarioResult(
                Scenario: method,
                MeanMs: meanMs,
                StdDevMs: stdDevMs,
                P95Ms: p95Ms,
                AllocatedBytes: allocatedBytes,
                RawMean: rawMean,
                RawStdDev: rawStdDev,
                RawAllocated: rawAllocated));
        }

        if (scenarios.Count == 0)
        {
            throw new InvalidDataException($"CSV 中没有可用的 benchmark 数据: {csvPath}");
        }

        return new VmReportModel(
            GeneratedAt: DateTime.Now,
            SourceCsv: csvPath,
            Job: job,
            Runtime: runtime,
            WarmupCount: warmupCount,
            IterationCount: iterationCount,
            Warnings: warnings.Distinct().ToArray(),
            Scenarios: scenarios);
    }

    private static string ReadField(string[] fields, IReadOnlyDictionary<string, int> headerIndex, string columnName, string fallback = "")
    {
        if (!headerIndex.TryGetValue(columnName, out var index))
        {
            return fallback;
        }

        if (index < 0 || index >= fields.Length)
        {
            return fallback;
        }

        var value = fields[index].Trim();
        return string.IsNullOrEmpty(value) ? fallback : value;
    }

    private static double? ParseDurationToMilliseconds(string raw, out string? warning)
    {
        warning = null;
        if (string.IsNullOrWhiteSpace(raw) || raw.Equals("NA", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var normalized = raw.Trim().Replace(",", "");
        var parts = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            warning = $"无法识别时间值: {raw}";
            return null;
        }

        if (!double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
        {
            warning = $"无法解析时间数值: {raw}";
            return null;
        }

        var unit = parts.Length > 1 ? parts[1].Trim() : "ms";
        return unit switch
        {
            "ns" => number / 1_000_000d,
            "us" => number / 1_000d,
            "μs" => number / 1_000d,
            "ms" => number,
            "s" => number * 1_000d,
            _ => ParseUnknownTimeUnit(raw, unit, number, out warning)
        };
    }

    private static double? ParseUnknownTimeUnit(string raw, string unit, double number, out string? warning)
    {
        warning = $"未知时间单位 {unit}，按 ms 处理（原始值: {raw}）";
        return number;
    }

    private static long? ParseBytes(string raw, out string? warning)
    {
        warning = null;
        if (string.IsNullOrWhiteSpace(raw) || raw.Equals("NA", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var normalized = raw.Trim().Replace(",", "");
        var parts = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            warning = $"无法识别内存值: {raw}";
            return null;
        }

        if (!double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
        {
            warning = $"无法解析内存数值: {raw}";
            return null;
        }

        var unit = parts.Length > 1 ? parts[1].Trim() : "B";
        var bytes = unit switch
        {
            "B" => number,
            "KB" => number * 1024d,
            "MB" => number * 1024d * 1024d,
            "GB" => number * 1024d * 1024d * 1024d,
            _ => ParseUnknownMemoryUnit(raw, unit, number, out warning)
        };

        return (long)Math.Round(bytes, MidpointRounding.AwayFromZero);
    }

    private static double ParseUnknownMemoryUnit(string raw, string unit, double number, out string? warning)
    {
        warning = $"未知内存单位 {unit}，按 B 处理（原始值: {raw}）";
        return number;
    }

    private static void WriteMarkdown(VmReportModel report, string markdownPath)
    {
        using var writer = new StreamWriter(markdownPath);
        writer.WriteLine("# Old8Lang VM Performance Report");
        writer.WriteLine();
        writer.WriteLine($"- GeneratedAt: {report.GeneratedAt:O}");
        writer.WriteLine($"- SourceCsv: `{report.SourceCsv}`");
        writer.WriteLine($"- Job: `{report.Job}`");
        writer.WriteLine($"- Runtime: `{report.Runtime}`");
        writer.WriteLine($"- WarmupCount: `{report.WarmupCount}`");
        writer.WriteLine($"- IterationCount: `{report.IterationCount}`");
        writer.WriteLine();
        writer.WriteLine("| Scenario | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | RawMean | RawAllocated |");
        writer.WriteLine("|---|---:|---:|---:|---:|---|---|");
        foreach (var scenario in report.Scenarios)
        {
            writer.WriteLine(
                $"| {scenario.Scenario} | {FormatNumber(scenario.MeanMs)} | {FormatNumber(scenario.StdDevMs)} | {FormatNumber(scenario.P95Ms)} | {FormatNumber(scenario.AllocatedBytes)} | {EscapeMarkdown(scenario.RawMean)} | {EscapeMarkdown(scenario.RawAllocated)} |");
        }

        if (report.Warnings.Count > 0)
        {
            writer.WriteLine();
            writer.WriteLine("## Warnings");
            foreach (var warning in report.Warnings)
            {
                writer.WriteLine($"- {warning}");
            }
        }
    }

    private static void WriteJson(VmReportModel report, string jsonPath)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        var json = JsonSerializer.Serialize(report, options);
        File.WriteAllText(jsonPath, json);
    }

    private static string FormatNumber(double? value)
    {
        return value.HasValue ? value.Value.ToString("F3", CultureInfo.InvariantCulture) : "N/A";
    }

    private static string FormatNumber(long? value)
    {
        return value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : "N/A";
    }

    private static string EscapeMarkdown(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "N/A" : value.Replace("|", "\\|");
    }
}
