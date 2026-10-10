using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.FileIO;

namespace Old8Lang.Benchmarks.Benchmarks.VM.Reports;

/// <summary>
/// VM 性能报告生成器（仅解析 BenchmarkDotNet 产物）
/// </summary>
public static class VMPerformanceReport
{
    private sealed record VmScenarioResult(
        string Scenario,
        string Job,
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

    private sealed record TieredScenarioResult(
        string Scenario,
        string Job,
        string Category,
        double? MeanMs,
        double? StdDevMs,
        double? P95Ms,
        long? AllocatedBytes,
        double? ThroughputOpsPerSec,
        double? AllocatedBytesPerOp,
        double? BaselineMeanMs,
        double? MeanDeltaPercent,
        double? RegressionThresholdPercent,
        string Status,
        string? FailureReason,
        string RawMean,
        string RawAllocated);

    private sealed record TieredAggregatedScenarioResult(
        string Scenario,
        string Category,
        int JobCount,
        double? MedianMeanMs,
        double? MedianStdDevMs,
        double? MedianP95Ms,
        long? MedianAllocatedBytes,
        double? MedianThroughputOpsPerSec,
        double? MedianAllocatedBytesPerOp,
        double? MedianBaselineMeanMs,
        double? MedianMeanDeltaPercent,
        double? RegressionThresholdPercent,
        string Status,
        string? FailureReason);

    private sealed record TieredReportModel(
        DateTime GeneratedAt,
        string SourceCsv,
        string? BaselineJson,
        IReadOnlyList<string> Warnings,
        IReadOnlyList<TieredScenarioResult> Scenarios,
        IReadOnlyList<TieredAggregatedScenarioResult> AggregatedScenarios);

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
    public static (string MarkdownPath, string JsonPath) GenerateExtendedFromBenchmarkArtifacts(string artifactsDir, string reportsDir)
    {
        return GenerateTieredFromBenchmarkArtifacts(
            artifactsDir,
            reportsDir,
            csvPattern: "*VMExtendedPerformanceBenchmarks*-report.csv",
            logPattern: "*VMExtendedPerformanceBenchmarks-*.log",
            notFoundMessage: "未找到扩展 VM BenchmarkDotNet 报告 CSV",
            scenarioPrefix: "VMX_",
            reportFilePrefix: "VM_Extended_Performance_Report",
            reportTitle: "Old8Lang Extended VM Performance Report");
    }

    /// <summary>
    /// 从 BenchmarkDotNet artifacts 解析并生成 VM Quick 报告（md + json）
    /// </summary>
    public static (string MarkdownPath, string JsonPath) GenerateQuickFromBenchmarkArtifacts(string artifactsDir, string reportsDir)
    {
        return GenerateTieredFromBenchmarkArtifacts(
            artifactsDir,
            reportsDir,
            csvPattern: "*VMQuickPerformanceBenchmarks*-report.csv",
            logPattern: "*VMQuickPerformanceBenchmarks-*.log",
            notFoundMessage: "未找到 VM Quick BenchmarkDotNet 报告 CSV",
            scenarioPrefix: "VMXQ_",
            reportFilePrefix: "VM_Quick_Performance_Report",
            reportTitle: "Old8Lang VM Quick Performance Report",
            fallbackBaselineFileName: "vm_quick_baseline.json");
    }

    /// <summary>
    /// 从 BenchmarkDotNet artifacts 解析并生成 VM Nightly 报告（md + json）
    /// </summary>
    public static (string MarkdownPath, string JsonPath) GenerateNightlyFromBenchmarkArtifacts(string artifactsDir, string reportsDir)
    {
        return GenerateTieredFromBenchmarkArtifacts(
            artifactsDir,
            reportsDir,
            csvPattern: "*VMNightlyPerformanceBenchmarks*-report.csv",
            logPattern: "*VMNightlyPerformanceBenchmarks-*.log",
            notFoundMessage: "未找到 VM Nightly BenchmarkDotNet 报告 CSV",
            scenarioPrefix: "VMXN_",
            reportFilePrefix: "VM_Nightly_Performance_Report",
            reportTitle: "Old8Lang VM Nightly Performance Report");
    }

    /// <summary>
    /// 从 BenchmarkDotNet artifacts 解析并生成 VM 并发/异步专项报告（md + json）
    /// </summary>
    public static (string MarkdownPath, string JsonPath) GenerateConcurrencyFromBenchmarkArtifacts(string artifactsDir, string reportsDir)
    {
        return GenerateTieredFromBenchmarkArtifacts(
            artifactsDir,
            reportsDir,
            csvPattern: "*VMConcurrencyPerformanceBenchmarks*-report.csv",
            logPattern: "*VMConcurrencyPerformanceBenchmarks-*.log",
            notFoundMessage: "未找到 VM 并发/异步专项 BenchmarkDotNet 报告 CSV",
            scenarioPrefix: "VMXC_",
            reportFilePrefix: "VM_Concurrency_Performance_Report",
            reportTitle: "Old8Lang VM Concurrency Performance Report");
    }

    /// <summary>
    /// 指定回归基线报告的环境变量名。
    /// </summary>
    /// <remarks>
    /// 设置后只使用该文件作为基线（文件不存在则视为没有基线，回归状态为 N/A），
    /// 不再回退到历史报告或静态基线。CI 用它把基线固定到与当前 runner 同类的机器上，
    /// 避免跨机器比较产生无意义的回归判定。
    /// </remarks>
    public const string ExplicitBaselineEnvironmentVariable = "OLD8LANG_VM_REPORT_BASELINE";

    /// <summary>
    /// 检查报告 JSON 是否包含 FAIL 状态
    /// </summary>
    public static bool HasFailStatus(string reportJsonPath)
    {
        return GetFailureSummary(reportJsonPath).Count > 0;
    }

    /// <summary>
    /// 汇总报告 JSON 中的 FAIL 场景（用于基准门禁输出可读的阻断原因）
    /// </summary>
    /// <param name="reportJsonPath">报告 JSON 路径</param>
    /// <returns>每个 FAIL 场景一行描述；没有 FAIL 时为空列表</returns>
    public static IReadOnlyList<string> GetFailureSummary(string reportJsonPath)
    {
        var failures = new List<string>();
        if (string.IsNullOrWhiteSpace(reportJsonPath) || !File.Exists(reportJsonPath))
        {
            return failures;
        }

        using var document = JsonDocument.Parse(File.ReadAllText(reportJsonPath));
        var root = document.RootElement;
        CollectFailures(root, "Scenarios", "job", failures);
        CollectFailures(root, "AggregatedScenarios", "aggregate", failures);
        return failures;
    }

    private static void CollectFailures(JsonElement root, string propertyName, string view, List<string> failures)
    {
        if (!root.TryGetProperty(propertyName, out var scenariosElement) ||
            scenariosElement.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var scenario in scenariosElement.EnumerateArray())
        {
            if (!TryReadString(scenario, "Status", out var status) ||
                !string.Equals(status, "FAIL", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var scenarioName = TryReadString(scenario, "Scenario", out var name) ? name : "<unknown>";
            var jobSuffix = TryReadString(scenario, "Job", out var job) ? $" [{job}]" : string.Empty;
            var delta = scenario.TryGetProperty("MeanDeltaPercent", out var deltaElement) &&
                        deltaElement.ValueKind == JsonValueKind.Number
                ? $"{deltaElement.GetDouble():+0.00;-0.00;0.00}%"
                : "N/A";
            var reason = TryReadString(scenario, "FailureReason", out var failureReason)
                ? $" {failureReason}"
                : string.Empty;

            failures.Add($"{view}: {scenarioName}{jobSuffix} MeanΔ={delta}{reason}");
        }
    }

    private static bool TryReadString(JsonElement element, string propertyName, out string value)
    {
        value = string.Empty;
        if (!element.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var raw = property.GetString();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        value = raw;
        return true;
    }

    private static (string MarkdownPath, string JsonPath) GenerateTieredFromBenchmarkArtifacts(
        string artifactsDir,
        string reportsDir,
        string csvPattern,
        string logPattern,
        string notFoundMessage,
        string scenarioPrefix,
        string reportFilePrefix,
        string reportTitle,
        string? fallbackBaselineFileName = null)
    {
        var csvPath = ResolveLatestCsvByPattern(artifactsDir, csvPattern, notFoundMessage);
        var parsed = ParseCsv(csvPath);
        var failureReasonByScenarioAndJob = LoadFailureReasons(artifactsDir, logPattern, scenarioPrefix);
        var failureReasonByScenario = failureReasonByScenarioAndJob
            .GroupBy(pair => pair.Key.Scenario, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First().Value, StringComparer.Ordinal);

        Directory.CreateDirectory(reportsDir);
        var baselineJson = ResolveTieredBaselineJson(reportsDir, reportFilePrefix, fallbackBaselineFileName);
        var baselineLookup = LoadBaselineLookup(baselineJson);

        var scenarios = parsed.Scenarios
            .Where(s => s.Scenario.StartsWith(scenarioPrefix, StringComparison.Ordinal))
            .Select(s =>
            {
                string? failureReason = null;
                if (failureReasonByScenarioAndJob.TryGetValue((s.Scenario, s.Job), out var matchedReason))
                {
                    failureReason = matchedReason;
                }
                else if (failureReasonByScenario.TryGetValue(s.Scenario, out var fallbackReason))
                {
                    failureReason = fallbackReason;
                }

                return ConvertToTieredScenario(s, baselineLookup, failureReason);
            })
            .ToArray();

        var aggregatedScenarios = scenarios
            .GroupBy(s => s.Scenario, StringComparer.Ordinal)
            .Select(AggregateTieredScenario)
            .OrderBy(s => s.Category, StringComparer.Ordinal)
            .ThenBy(s => s.Scenario, StringComparer.Ordinal)
            .ToArray();

        if (scenarios.Length == 0)
        {
            throw new InvalidDataException($"CSV 中没有 {scenarioPrefix} 场景: {csvPath}");
        }

        var warnings = parsed.Warnings.ToList();
        if (baselineJson is null)
        {
            warnings.Add("未找到历史基线报告，回归对比状态将显示 N/A。");
        }
        if (scenarios.Any(s => string.Equals(s.Status, "FAIL", StringComparison.OrdinalIgnoreCase)))
        {
            warnings.Add("检测到执行失败场景（FAIL），请优先查看 FailureReason 与 BenchmarkDotNet 日志。");
        }

        var report = new TieredReportModel(
            GeneratedAt: DateTime.Now,
            SourceCsv: csvPath,
            BaselineJson: baselineJson,
            Warnings: warnings,
            Scenarios: scenarios,
            AggregatedScenarios: aggregatedScenarios);

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        var markdownPath = Path.Combine(reportsDir, $"{reportFilePrefix}_{timestamp}.md");
        var jsonPath = Path.Combine(reportsDir, $"{reportFilePrefix}_{timestamp}.json");

        WriteTieredMarkdown(report, markdownPath, reportTitle);
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

    private static Dictionary<(string Scenario, string Job), string> LoadFailureReasons(
        string artifactsDir,
        string logPattern,
        string scenarioPrefix)
    {
        var result = new Dictionary<(string Scenario, string Job), string>();
        if (!Directory.Exists(artifactsDir))
        {
            return result;
        }

        var logCandidates = Directory
            .GetFiles(artifactsDir, logPattern, System.IO.SearchOption.TopDirectoryOnly)
            .Select(path => new FileInfo(path))
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .ToArray();

        if (logCandidates.Length == 0)
        {
            return result;
        }

        var lines = File.ReadAllLines(logCandidates[0].FullName);
        var benchmarkPattern = new Regex(
            "^// Benchmark:\\s+[^.]+\\.(?<scenario>[^:]+):\\s+(?<job>[^\\(]+)\\(",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        for (var i = 0; i < lines.Length; i++)
        {
            var line = StripAnsi(lines[i]).Trim();
            var match = benchmarkPattern.Match(line);
            if (!match.Success)
            {
                continue;
            }

            var scenario = match.Groups["scenario"].Value.Trim();
            if (!scenario.StartsWith(scenarioPrefix, StringComparison.Ordinal))
            {
                continue;
            }

            var job = match.Groups["job"].Value.Trim();
            var key = (scenario, job);
            if (result.ContainsKey(key))
            {
                continue;
            }

            var reason = ExtractFailureReason(lines, i + 1);
            if (!string.IsNullOrWhiteSpace(reason))
            {
                result[key] = reason;
            }
        }

        return result;
    }

    private static string? ExtractFailureReason(IReadOnlyList<string> lines, int startIndex)
    {
        var endIndex = Math.Min(lines.Count - 1, startIndex + 220);
        for (var i = startIndex; i <= endIndex; i++)
        {
            var line = StripAnsi(lines[i]).Trim();
            if (line.StartsWith("// Benchmark:", StringComparison.Ordinal))
            {
                break;
            }

            if (!line.Contains("--->", StringComparison.Ordinal))
            {
                continue;
            }

            var separatorIndex = line.IndexOf("--->", StringComparison.Ordinal);
            if (separatorIndex < 0 || separatorIndex + 3 >= line.Length)
            {
                continue;
            }

            var exceptionSegment = line[(separatorIndex + 3)..].Trim();
            if (exceptionSegment.Contains("TargetInvocationException", StringComparison.Ordinal) &&
                exceptionSegment.Contains("target of an invocation", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var colonIndex = exceptionSegment.IndexOf(':');
            if (colonIndex <= 0 || colonIndex + 1 >= exceptionSegment.Length)
            {
                continue;
            }

            var exceptionType = exceptionSegment[..colonIndex].Trim();
            var message = exceptionSegment[(colonIndex + 1)..].Trim();
            if (string.IsNullOrWhiteSpace(exceptionType) || string.IsNullOrWhiteSpace(message))
            {
                continue;
            }

            return $"{exceptionType}: {message}";
        }

        return null;
    }

    private static string StripAnsi(string input)
    {
        return string.IsNullOrEmpty(input)
            ? string.Empty
            : Regex.Replace(input, "\\x1B\\[[0-9;]*m", string.Empty);
    }

    private static VmReportModel ParseCsv(string csvPath)
    {
        var warnings = new List<string>();
        var scenarios = new List<VmScenarioResult>();
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

        var headerIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < headers.Length; i++)
        {
            var key = headers[i].Trim();
            if (!headerIndex.ContainsKey(key))
            {
                headerIndex[key] = i;
            }
        }

        var requiredColumns = new[] { "Method", "Mean" };
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
            var rawStdDev = ReadFieldWithFallback(fields, headerIndex, "StdDev", "Error");
            var rawAllocated = ReadField(fields, headerIndex, "Allocated", "NA");

            var currentJob = ReadField(fields, headerIndex, "Job", job);
            job = currentJob;
            runtime = ReadField(fields, headerIndex, "Runtime", runtime);
            warmupCount = ReadField(fields, headerIndex, "WarmupCount", warmupCount);
            iterationCount = ReadField(fields, headerIndex, "IterationCount", iterationCount);

            var meanMs = ParseDurationToMilliseconds(rawMean, out var meanWarning);
            var stdDevMs = ParseDurationToMilliseconds(rawStdDev, out var stdDevWarning);
            var allocatedBytes = ParseBytes(rawAllocated, out var allocatedWarning);
            double? p95Ms = null;

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
                Job: currentJob,
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

    private static string ReadFieldWithFallback(
        string[] fields,
        IReadOnlyDictionary<string, int> headerIndex,
        string primaryColumn,
        string fallbackColumn,
        string fallback = "")
    {
        var value = ReadField(fields, headerIndex, primaryColumn, string.Empty);
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return ReadField(fields, headerIndex, fallbackColumn, fallback);
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

    private static void WriteTieredMarkdown(TieredReportModel report, string markdownPath, string reportTitle)
    {
        using var writer = new StreamWriter(markdownPath);
        writer.WriteLine($"# {reportTitle}");
        writer.WriteLine();
        writer.WriteLine($"- GeneratedAt: {report.GeneratedAt:O}");
        writer.WriteLine($"- SourceCsv: `{report.SourceCsv}`");
        writer.WriteLine($"- BaselineJson: `{report.BaselineJson ?? "N/A"}`");
        writer.WriteLine();

        foreach (var group in report.Scenarios.GroupBy(s => s.Category).OrderBy(g => g.Key))
        {
            writer.WriteLine($"## {group.Key}");
            writer.WriteLine();
            writer.WriteLine("### Per-Job Details");
            writer.WriteLine();
            writer.WriteLine("| Scenario | Job | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | Threshold(%) | Status | FailureReason |");
            writer.WriteLine("|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|");

            foreach (var scenario in group)
            {
                writer.WriteLine(
                    $"| {scenario.Scenario} | {EscapeMarkdown(scenario.Job)} | {FormatNumber(scenario.MeanMs)} | {FormatNumber(scenario.StdDevMs)} | {FormatNumber(scenario.P95Ms)} | {FormatNumber(scenario.AllocatedBytes)} | {FormatNumber(scenario.ThroughputOpsPerSec)} | {FormatNumber(scenario.AllocatedBytesPerOp)} | {FormatPercent(scenario.MeanDeltaPercent)} | {FormatPercent(scenario.RegressionThresholdPercent)} | {scenario.Status} | {EscapeMarkdown(scenario.FailureReason ?? "N/A")} |");
            }

            writer.WriteLine();

            writer.WriteLine("### Scenario Aggregate (Median)");
            writer.WriteLine();
            writer.WriteLine("| Scenario | Jobs | MedianMean(ms) | MedianStdDev(ms) | MedianP95(ms) | MedianAllocated(bytes) | MedianThroughput(ops/s) | MedianAlloc/Op(bytes) | MedianMeanΔ(%) | Threshold(%) | Status | FailureReason |");
            writer.WriteLine("|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|");

            foreach (var scenario in report.AggregatedScenarios
                         .Where(s => string.Equals(s.Category, group.Key, StringComparison.Ordinal))
                         .OrderBy(s => s.Scenario, StringComparer.Ordinal))
            {
                writer.WriteLine(
                    $"| {scenario.Scenario} | {scenario.JobCount} | {FormatNumber(scenario.MedianMeanMs)} | {FormatNumber(scenario.MedianStdDevMs)} | {FormatNumber(scenario.MedianP95Ms)} | {FormatNumber(scenario.MedianAllocatedBytes)} | {FormatNumber(scenario.MedianThroughputOpsPerSec)} | {FormatNumber(scenario.MedianAllocatedBytesPerOp)} | {FormatPercent(scenario.MedianMeanDeltaPercent)} | {FormatPercent(scenario.RegressionThresholdPercent)} | {scenario.Status} | {EscapeMarkdown(scenario.FailureReason ?? "N/A")} |");
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

    private static void WriteJson(VmReportModel report, string jsonPath)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        var json = JsonSerializer.Serialize(report, options);
        File.WriteAllText(jsonPath, json);
    }

    private static void WriteJson(TieredReportModel report, string jsonPath)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        var json = JsonSerializer.Serialize(report, options);
        File.WriteAllText(jsonPath, json);
    }

    /// <summary>
    /// 解析回归对比使用的基线报告。
    /// </summary>
    /// <remarks>
    /// 若设置了 <see cref="ExplicitBaselineEnvironmentVariable"/>，只使用该文件：
    /// 文件不存在时视为「没有基线」（状态为 N/A），不再回退到历史报告或静态基线，
    /// 这样 CI 在缓存未命中时不会因为跨机器比较而误报 FAIL。
    /// </remarks>
    private static string? ResolveTieredBaselineJson(
        string reportsDir,
        string reportFilePrefix,
        string? fallbackBaselineFileName)
    {
        var explicitBaseline = Environment.GetEnvironmentVariable(ExplicitBaselineEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(explicitBaseline))
        {
            return File.Exists(explicitBaseline) ? Path.GetFullPath(explicitBaseline) : null;
        }

        return ResolveLatestTieredBaselineJson(reportsDir, reportFilePrefix, fallbackBaselineFileName);
    }

    private static string? ResolveLatestTieredBaselineJson(string reportsDir, string reportFilePrefix, string? fallbackBaselineFileName = null)
    {
        if (Directory.Exists(reportsDir))
        {
            var pattern = $"{reportFilePrefix}_*.json";
            // 报告文件名内嵌 yyyyMMdd_HHmmss 时间戳，按文件名倒序即为时间倒序；
            // 仅按 mtime 排序在文件同时检出（mtime 相同）时结果不确定，会导致基线漂移。
            var candidates = Directory
                .GetFiles(reportsDir, pattern, System.IO.SearchOption.TopDirectoryOnly)
                .OrderByDescending(path => Path.GetFileNameWithoutExtension(path), StringComparer.Ordinal)
                .ThenByDescending(path => File.GetLastWriteTimeUtc(path))
                .ToArray();

            if (candidates.Length > 0)
            {
                return Path.GetFullPath(candidates[0]);
            }
        }

        // 静态回退：当 reportsDir 中无历史报告时，尝试加载预设基线文件
        if (!string.IsNullOrWhiteSpace(fallbackBaselineFileName))
        {
            var fallbackPath = Path.Combine(reportsDir, "Baselines", fallbackBaselineFileName);
            if (File.Exists(fallbackPath))
            {
                return fallbackPath;
            }
        }

        return null;
    }

    private sealed record BaselineLookup(
        IReadOnlyDictionary<(string Scenario, string Job), double?> ByScenarioAndJob,
        IReadOnlyDictionary<string, double?> ByScenario);

    private static BaselineLookup LoadBaselineLookup(string? baselineJsonPath)
    {
        var entries = new List<(string Scenario, string? Job, double? MeanMs)>();
        if (string.IsNullOrWhiteSpace(baselineJsonPath) || !File.Exists(baselineJsonPath))
        {
            return new BaselineLookup(
                ByScenarioAndJob: new Dictionary<(string Scenario, string Job), double?>(),
                ByScenario: new Dictionary<string, double?>(StringComparer.Ordinal));
        }

        using var document = JsonDocument.Parse(File.ReadAllText(baselineJsonPath));
        if (!document.RootElement.TryGetProperty("Scenarios", out var scenariosElement) ||
            scenariosElement.ValueKind != JsonValueKind.Array)
        {
            return new BaselineLookup(
                ByScenarioAndJob: new Dictionary<(string Scenario, string Job), double?>(),
                ByScenario: new Dictionary<string, double?>(StringComparer.Ordinal));
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
            if (item.TryGetProperty("MeanMs", out var meanElement) && meanElement.ValueKind == JsonValueKind.Number)
            {
                mean = meanElement.GetDouble();
            }

            string? job = null;
            if (item.TryGetProperty("Job", out var jobElement) && jobElement.ValueKind == JsonValueKind.String)
            {
                job = jobElement.GetString();
            }

            entries.Add((scenario, job, mean));
        }

        var byScenarioAndJob = entries
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Job))
            .GroupBy(entry => (entry.Scenario, Job: entry.Job!), tuple => tuple.MeanMs)
            .ToDictionary(group => group.Key, group => SelectMedian(group), comparer: EqualityComparer<(string Scenario, string Job)>.Default);

        var byScenario = entries
            .GroupBy(entry => entry.Scenario, entry => entry.MeanMs, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => SelectMedian(group), StringComparer.Ordinal);

        return new BaselineLookup(byScenarioAndJob, byScenario);
    }

    private static TieredScenarioResult ConvertToTieredScenario(
        VmScenarioResult source,
        BaselineLookup baselineLookup,
        string? failureReason)
    {
        var category = GetTieredCategory(source.Scenario);
        var operationCount = GetOperationCount(source.Scenario);

        double? throughput = source.MeanMs.HasValue && operationCount.HasValue && source.MeanMs.Value > 0
            ? operationCount.Value * 1000d / source.MeanMs.Value
            : null;
        double? allocatedPerOp = source.AllocatedBytes.HasValue && operationCount.HasValue && operationCount.Value > 0
            ? source.AllocatedBytes.Value / (double)operationCount.Value
            : null;

        if (!baselineLookup.ByScenarioAndJob.TryGetValue((source.Scenario, source.Job), out var baselineMeanMs))
        {
            baselineLookup.ByScenario.TryGetValue(source.Scenario, out baselineMeanMs);
        }

        var meanDeltaPercent = ComputeRegressionPercent(baselineMeanMs, source.MeanMs);
        var threshold = GetRegressionThresholdPercent(category);
        var status = EvaluateScenarioStatus(source, meanDeltaPercent, threshold, ref failureReason);

        return new TieredScenarioResult(
            Scenario: source.Scenario,
            Job: source.Job,
            Category: category,
            MeanMs: source.MeanMs,
            StdDevMs: source.StdDevMs,
            P95Ms: source.P95Ms,
            AllocatedBytes: source.AllocatedBytes,
            ThroughputOpsPerSec: throughput,
            AllocatedBytesPerOp: allocatedPerOp,
            BaselineMeanMs: baselineMeanMs,
            MeanDeltaPercent: meanDeltaPercent,
            RegressionThresholdPercent: threshold,
            Status: status,
            FailureReason: failureReason,
            RawMean: source.RawMean,
            RawAllocated: source.RawAllocated);
    }

    private static TieredAggregatedScenarioResult AggregateTieredScenario(IGrouping<string, TieredScenarioResult> group)
    {
        var list = group.OrderBy(item => item.Job, StringComparer.Ordinal).ToList();
        var first = list[0];
        var status = EvaluateWorstStatus(list.Select(item => item.Status));
        var failureReason = list
            .Select(item => item.FailureReason)
            .FirstOrDefault(reason => !string.IsNullOrWhiteSpace(reason));

        return new TieredAggregatedScenarioResult(
            Scenario: first.Scenario,
            Category: first.Category,
            JobCount: list.Count,
            MedianMeanMs: SelectMedian(list.Select(item => item.MeanMs)),
            MedianStdDevMs: SelectMedian(list.Select(item => item.StdDevMs)),
            MedianP95Ms: SelectMedian(list.Select(item => item.P95Ms)),
            MedianAllocatedBytes: SelectMedianLong(list.Select(item => item.AllocatedBytes)),
            MedianThroughputOpsPerSec: SelectMedian(list.Select(item => item.ThroughputOpsPerSec)),
            MedianAllocatedBytesPerOp: SelectMedian(list.Select(item => item.AllocatedBytesPerOp)),
            MedianBaselineMeanMs: SelectMedian(list.Select(item => item.BaselineMeanMs)),
            MedianMeanDeltaPercent: SelectMedian(list.Select(item => item.MeanDeltaPercent)),
            RegressionThresholdPercent: first.RegressionThresholdPercent,
            Status: status,
            FailureReason: failureReason);
    }

    private static string GetTieredCategory(string scenario)
    {
        if (scenario.Contains("_Throughput_", StringComparison.Ordinal))
        {
            return "Throughput";
        }

        if (scenario.Contains("_Latency_", StringComparison.Ordinal))
        {
            return "Latency";
        }

        if (scenario.Contains("_Allocation_", StringComparison.Ordinal))
        {
            return "Allocation";
        }

        if (scenario.Contains("_LargeFile_", StringComparison.Ordinal))
        {
            return "LargeFile";
        }

        if (scenario.Contains("_Edge_", StringComparison.Ordinal))
        {
            return "Edge";
        }

        if (scenario.Contains("_Concurrency_", StringComparison.Ordinal))
        {
            return "Concurrency";
        }

        return "Core";
    }

    private static double GetRegressionThresholdPercent(string category)
    {
        return category switch
        {
            "Throughput" => 8d,
            "Latency" => 8d,
            "Allocation" => 5d,
            "LargeFile" => 8d,
            "Edge" => 10d,
            "Concurrency" => 12d,
            _ => 10d
        };
    }

    private static string EvaluateScenarioStatus(
        VmScenarioResult source,
        double? meanDeltaPercent,
        double thresholdPercent,
        ref string? failureReason)
    {
        if (!source.MeanMs.HasValue)
        {
            failureReason ??= "Benchmark result is NA (no measurable output).";
            return "FAIL";
        }

        return EvaluateRegressionStatus(meanDeltaPercent, thresholdPercent);
    }

    private static string EvaluateRegressionStatus(double? meanDeltaPercent, double thresholdPercent)
    {
        if (!meanDeltaPercent.HasValue)
        {
            return "N/A";
        }

        if (meanDeltaPercent.Value > thresholdPercent)
        {
            return "FAIL";
        }

        // 接近阈值时给出预警，便于观察趋势但不阻断。
        if (meanDeltaPercent.Value > thresholdPercent * 0.8d)
        {
            return "WARN";
        }

        return "PASS";
    }

    private static long? GetOperationCount(string scenario)
    {
        return scenario switch
        {
            // Extended
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

            // Quick
            "VMXQ_LargeFile_CompileAndExecute_10k" => 10_000,
            "VMXQ_LargeFile_CompileAndExecute_50k_Generated" => 50_000,
            "VMXQ_Edge_DeepRecursion_NearLimit" => 1_000,
            "VMXQ_Edge_HighArgCount_CallHotPath" => 30_000,
            "VMXQ_Edge_HighThrowRate_TryCatch" => 40_000,
            "VMXQ_Edge_LargeClosureCapture_HighFreq" => 50_000,
            "VMXQ_Concurrency_Channel_MPMC_4Workers" => 100_000,
            "VMXQ_Concurrency_MutexAtomicCounter_4Workers" => 320_000,

            // Nightly
            "VMXN_LargeFile_CompileAndExecute_10k" => 10_000,
            "VMXN_LargeFile_CompileAndExecute_50k_Generated" => 50_000,
            "VMXN_LargeFile_CompileAndExecute_100k_Generated" => 100_000,
            "VMXN_LargeFile_ModuleStyle_ImportLike" => 60_000,
            "VMXN_Edge_DeepRecursion_NearLimit" => 1_000,
            "VMXN_Edge_HighArgCount_CallHotPath" => 70_000,
            "VMXN_Edge_HighThrowRate_TryCatch" => 120_000,
            "VMXN_Edge_LargeClosureCapture_HighFreq" => 150_000,
            "VMXN_Concurrency_Channel_MPMC_2Workers" => 100_000,
            "VMXN_Concurrency_Channel_MPMC_4Workers" => 200_000,
            "VMXN_Concurrency_Channel_MPMC_8Workers" => 400_000,
            "VMXN_Concurrency_MutexAtomicCounter_2Workers" => 300_000,
            "VMXN_Concurrency_MutexAtomicCounter_4Workers" => 600_000,
            "VMXN_Concurrency_MutexAtomicCounter_8Workers" => 1_200_000,
            "VMXN_Concurrency_Semaphore_Contention" => 120_000,
            "VMXN_Concurrency_SpawnJoin_Throughput" => 20_000,

            // Concurrency/Async focused
            "VMXC_Latency_SpawnJoin_ColdStart" => 1_000,
            "VMXC_Throughput_SpawnJoin_HotLoop" => 10_000,
            "VMXC_Latency_Task_NewTask_Await" => 1_000,
            "VMXC_Throughput_Async_FanOutFanIn" => 2_000,
            "VMXC_Allocation_Channel_SPSC_NoTimeout" => 120_000,
            "VMXC_Throughput_Channel_MPMC_WithTimeout" => 120_000,
            _ => null
        };
    }

    private static double? ComputeRegressionPercent(double? baselineValue, double? currentValue)
    {
        if (!baselineValue.HasValue || !currentValue.HasValue || baselineValue.Value == 0)
        {
            return null;
        }

        // 正值表示回归（变慢），负值表示提升。
        return (currentValue.Value - baselineValue.Value) / baselineValue.Value * 100d;
    }

    private static string EvaluateWorstStatus(IEnumerable<string> statuses)
    {
        var worst = "PASS";
        var worstScore = GetStatusSeverityScore(worst);
        foreach (var status in statuses)
        {
            var score = GetStatusSeverityScore(status);
            if (score > worstScore)
            {
                worstScore = score;
                worst = status;
            }
        }

        return worst;
    }

    private static int GetStatusSeverityScore(string? status)
    {
        return status?.ToUpperInvariant() switch
        {
            "FAIL" => 3,
            "WARN" => 2,
            "N/A" => 1,
            _ => 0
        };
    }

    private static double? SelectMedian(IEnumerable<double?> values)
    {
        var ordered = values
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .OrderBy(v => v)
            .ToArray();

        if (ordered.Length == 0)
        {
            return null;
        }

        var middle = ordered.Length / 2;
        if (ordered.Length % 2 == 1)
        {
            return ordered[middle];
        }

        return (ordered[middle - 1] + ordered[middle]) / 2d;
    }

    private static long? SelectMedianLong(IEnumerable<long?> values)
    {
        var ordered = values
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .OrderBy(v => v)
            .ToArray();

        if (ordered.Length == 0)
        {
            return null;
        }

        var middle = ordered.Length / 2;
        if (ordered.Length % 2 == 1)
        {
            return ordered[middle];
        }

        var avg = (ordered[middle - 1] + ordered[middle]) / 2d;
        return (long)Math.Round(avg, MidpointRounding.AwayFromZero);
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
