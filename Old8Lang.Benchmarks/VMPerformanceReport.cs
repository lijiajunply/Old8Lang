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

    private sealed record ExtendedScenarioResult(
        string Scenario,
        string Category,
        double? MeanMs,
        double? StdDevMs,
        double? P95Ms,
        long? AllocatedBytes,
        double? ThroughputOpsPerSec,
        double? AllocatedBytesPerOp,
        string SoftGateStatus,
        double? BaselineMeanMs,
        double? MeanChangePct,
        long? BaselineAllocatedBytes,
        double? AllocatedChangePct,
        string RawMean,
        string RawAllocated);

    private sealed record ExtendedReportModel(
        DateTime GeneratedAt,
        string SourceCsv,
        string? BaselineJson,
        IReadOnlyList<string> Warnings,
        IReadOnlyList<ExtendedScenarioResult> Scenarios);

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

    /// <summary>
    /// 从 BenchmarkDotNet artifacts 解析并生成扩展 VM 报告（md + json）
    /// </summary>
    /// <param name="artifactsDir">BenchmarkDotNet.Artifacts/results 目录</param>
    /// <param name="reportsDir">报告输出目录（通常为 Reports）</param>
    /// <returns>Markdown 与 JSON 报告路径</returns>
    public static (string MarkdownPath, string JsonPath) GenerateExtendedFromBenchmarkArtifacts(string artifactsDir, string reportsDir)
    {
        var csvPath = ResolveLatestCsvByPattern(artifactsDir, "*VMExtendedPerformanceBenchmarks*-report.csv",
            "未找到扩展 VM BenchmarkDotNet 报告 CSV");
        var parsed = ParseCsv(csvPath);
        Directory.CreateDirectory(reportsDir);
        var baselineJson = ResolveLatestExtendedBaselineJson(reportsDir);
        var baselineMap = LoadBaselineMap(baselineJson);

        var scenarios = parsed.Scenarios
            .Where(s => s.Scenario.StartsWith("VMX_", StringComparison.Ordinal))
            .Select(s => ConvertToExtendedScenario(s, baselineMap))
            .ToArray();

        if (scenarios.Length == 0)
        {
            throw new InvalidDataException($"扩展 CSV 中没有 VMX 场景: {csvPath}");
        }

        var warnings = parsed.Warnings.ToList();
        if (baselineJson is null)
        {
            warnings.Add("未找到历史扩展基线报告，软门禁对比已跳过。");
        }

        var report = new ExtendedReportModel(
            GeneratedAt: DateTime.Now,
            SourceCsv: csvPath,
            BaselineJson: baselineJson,
            Warnings: warnings,
            Scenarios: scenarios);

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        var markdownPath = Path.Combine(reportsDir, $"VM_Extended_Performance_Report_{timestamp}.md");
        var jsonPath = Path.Combine(reportsDir, $"VM_Extended_Performance_Report_{timestamp}.json");
        WriteExtendedMarkdown(report, markdownPath);
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

    private static string ResolveLatestCsvByPattern(string artifactsDir, string pattern, string notFoundMessage)
    {
        if (!Directory.Exists(artifactsDir))
        {
            throw new DirectoryNotFoundException($"BenchmarkDotNet artifacts 目录不存在: {artifactsDir}");
        }

        var candidates = Directory
            .GetFiles(artifactsDir, pattern, System.IO.SearchOption.TopDirectoryOnly)
            .Select(path => new FileInfo(path))
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .ToArray();

        if (candidates.Length == 0)
        {
            throw new FileNotFoundException($"{notFoundMessage}，目录: {artifactsDir}");
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

    private static void WriteJson(ExtendedReportModel report, string jsonPath)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        var json = JsonSerializer.Serialize(report, options);
        File.WriteAllText(jsonPath, json);
    }

    private static string? ResolveLatestExtendedBaselineJson(string reportsDir)
    {
        if (!Directory.Exists(reportsDir))
        {
            return null;
        }

        var candidates = Directory
            .GetFiles(reportsDir, "VM_Extended_Performance_Report_*.json", System.IO.SearchOption.TopDirectoryOnly)
            .Select(path => new FileInfo(path))
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .ToArray();

        return candidates.Length == 0 ? null : candidates[0].FullName;
    }

    private static Dictionary<string, (double? MeanMs, long? AllocatedBytes)> LoadBaselineMap(string? baselineJsonPath)
    {
        var map = new Dictionary<string, (double? MeanMs, long? AllocatedBytes)>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(baselineJsonPath) || !File.Exists(baselineJsonPath))
        {
            return map;
        }

        using var document = JsonDocument.Parse(File.ReadAllText(baselineJsonPath));
        if (!document.RootElement.TryGetProperty("Scenarios", out var scenariosElement) ||
            scenariosElement.ValueKind != JsonValueKind.Array)
        {
            return map;
        }

        foreach (var item in scenariosElement.EnumerateArray())
        {
            if (!item.TryGetProperty("Scenario", out var scenarioElement))
            {
                continue;
            }

            var scenario = scenarioElement.GetString();
            if (string.IsNullOrWhiteSpace(scenario))
            {
                continue;
            }

            double? mean = null;
            long? allocated = null;

            if (item.TryGetProperty("MeanMs", out var meanElement) && meanElement.ValueKind == JsonValueKind.Number)
            {
                mean = meanElement.GetDouble();
            }
            if (item.TryGetProperty("AllocatedBytes", out var allocatedElement) &&
                allocatedElement.ValueKind == JsonValueKind.Number)
            {
                allocated = allocatedElement.GetInt64();
            }

            map[scenario] = (mean, allocated);
        }

        return map;
    }

    private static ExtendedScenarioResult ConvertToExtendedScenario(
        VmScenarioResult source,
        IReadOnlyDictionary<string, (double? MeanMs, long? AllocatedBytes)> baselineMap)
    {
        var category = GetExtendedCategory(source.Scenario);
        var operationCount = GetOperationCount(source.Scenario);
        double? throughput = source.MeanMs.HasValue && operationCount.HasValue && source.MeanMs.Value > 0
            ? operationCount.Value * 1000d / source.MeanMs.Value
            : null;
        double? allocatedPerOp = source.AllocatedBytes.HasValue && operationCount.HasValue && operationCount.Value > 0
            ? source.AllocatedBytes.Value / (double)operationCount.Value
            : null;

        baselineMap.TryGetValue(source.Scenario, out var baseline);
        var meanChangePct = ComputeChangePct(baseline.MeanMs, source.MeanMs, decreaseIsGood: true);
        var allocatedChangePct = ComputeChangePct(baseline.AllocatedBytes, source.AllocatedBytes, decreaseIsGood: true);
        var gate = EvaluateSoftGate(category, meanChangePct, allocatedChangePct);

        return new ExtendedScenarioResult(
            Scenario: source.Scenario,
            Category: category,
            MeanMs: source.MeanMs,
            StdDevMs: source.StdDevMs,
            P95Ms: source.P95Ms,
            AllocatedBytes: source.AllocatedBytes,
            ThroughputOpsPerSec: throughput,
            AllocatedBytesPerOp: allocatedPerOp,
            SoftGateStatus: gate,
            BaselineMeanMs: baseline.MeanMs,
            MeanChangePct: meanChangePct,
            BaselineAllocatedBytes: baseline.AllocatedBytes,
            AllocatedChangePct: allocatedChangePct,
            RawMean: source.RawMean,
            RawAllocated: source.RawAllocated);
    }

    private static string GetExtendedCategory(string scenario)
    {
        if (scenario.StartsWith("VMX_LargeFile_", StringComparison.Ordinal))
        {
            return "LargeFile";
        }
        if (scenario.StartsWith("VMX_Edge_", StringComparison.Ordinal))
        {
            return "Edge";
        }
        if (scenario.StartsWith("VMX_Concurrency_", StringComparison.Ordinal))
        {
            return "Concurrency";
        }
        return "Core";
    }

    private static long? GetOperationCount(string scenario)
    {
        return scenario switch
        {
            "VMX_LargeFile_CompileOnly_10k" => 10_000,
            "VMX_LargeFile_CompileAndExecute_10k" => 10_000,
            "VMX_LargeFile_CompileOnly_50k_Generated" => 50_000,
            "VMX_LargeFile_CompileAndExecute_50k_Generated" => 50_000,
            "VMX_Edge_DeepRecursion_NearLimit" => 1_000,
            "VMX_Edge_HighArgCount_CallHotPath" => 30_000,
            "VMX_Edge_HeavyTryCatch_LowThrowRate" => 100_000,
            "VMX_Edge_LargeClosureCapture" => 20_000,
            "VMX_Edge_DefaultAndNamedArgs_Stress" => 25_000,
            "VMX_Concurrency_SpawnJoin_Throughput" => 10_000,
            "VMX_Concurrency_Channel_SPSC_Throughput" => 200_000,
            "VMX_Concurrency_Channel_MPMC_Throughput" => 200_000,
            "VMX_Concurrency_Semaphore_Contention" => 100_000,
            "VMX_Concurrency_MutexAtomicCounter" => 1_000_000,
            "VMX_Concurrency_AsyncFanOutFanIn" => 2_000,
            _ => null
        };
    }

    private static double? ComputeChangePct(double? baselineValue, double? currentValue, bool decreaseIsGood)
    {
        if (!baselineValue.HasValue || !currentValue.HasValue || baselineValue.Value == 0)
        {
            return null;
        }

        var raw = (currentValue.Value - baselineValue.Value) / baselineValue.Value * 100d;
        return decreaseIsGood ? -raw : raw;
    }

    private static double? ComputeChangePct(long? baselineValue, long? currentValue, bool decreaseIsGood)
    {
        if (!baselineValue.HasValue || !currentValue.HasValue || baselineValue.Value == 0)
        {
            return null;
        }

        var raw = (currentValue.Value - baselineValue.Value) / (double)baselineValue.Value * 100d;
        return decreaseIsGood ? -raw : raw;
    }

    private static string EvaluateSoftGate(string category, double? meanImprovePct, double? allocatedImprovePct)
    {
        if (!meanImprovePct.HasValue && !allocatedImprovePct.HasValue)
        {
            return "N/A";
        }

        var meanThreshold = category switch
        {
            "Core" => -15d,
            "LargeFile" => -25d,
            "Concurrency" => -20d,
            "Edge" => -30d,
            _ => -15d
        };

        var allocThreshold = -20d;

        var meanWarn = meanImprovePct.HasValue && meanImprovePct.Value < meanThreshold;
        var allocWarn = allocatedImprovePct.HasValue && allocatedImprovePct.Value < allocThreshold;
        return meanWarn || allocWarn ? "WARN" : "PASS";
    }

    private static void WriteExtendedMarkdown(ExtendedReportModel report, string markdownPath)
    {
        using var writer = new StreamWriter(markdownPath);
        writer.WriteLine("# Old8Lang Extended VM Performance Report");
        writer.WriteLine();
        writer.WriteLine($"- GeneratedAt: {report.GeneratedAt:O}");
        writer.WriteLine($"- SourceCsv: `{report.SourceCsv}`");
        writer.WriteLine($"- BaselineJson: `{report.BaselineJson ?? "N/A"}`");
        writer.WriteLine();

        foreach (var group in report.Scenarios.GroupBy(s => s.Category).OrderBy(g => g.Key))
        {
            writer.WriteLine($"## {group.Key}");
            writer.WriteLine();
            writer.WriteLine("| Scenario | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | AllocΔ(%) | Gate |");
            writer.WriteLine("|---|---:|---:|---:|---:|---:|---:|---:|---:|---|");

            foreach (var scenario in group)
            {
                writer.WriteLine(
                    $"| {scenario.Scenario} | {FormatNumber(scenario.MeanMs)} | {FormatNumber(scenario.StdDevMs)} | {FormatNumber(scenario.P95Ms)} | {FormatNumber(scenario.AllocatedBytes)} | {FormatNumber(scenario.ThroughputOpsPerSec)} | {FormatNumber(scenario.AllocatedBytesPerOp)} | {FormatPercent(scenario.MeanChangePct)} | {FormatPercent(scenario.AllocatedChangePct)} | {scenario.SoftGateStatus} |");
            }

            writer.WriteLine();
        }

        if (report.Warnings.Count > 0)
        {
            writer.WriteLine("## Warnings");
            foreach (var warning in report.Warnings)
            {
                writer.WriteLine($"- {warning}");
            }
        }
    }

    private static string FormatNumber(double? value)
    {
        return value.HasValue ? value.Value.ToString("F3", CultureInfo.InvariantCulture) : "N/A";
    }

    private static string FormatPercent(double? value)
    {
        return value.HasValue ? value.Value.ToString("F2", CultureInfo.InvariantCulture) : "N/A";
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
