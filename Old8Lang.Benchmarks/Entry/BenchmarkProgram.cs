using BenchmarkDotNet.Running;
using Old8Lang.Benchmarks.Benchmarks.Parser;
using Old8Lang.Benchmarks.Benchmarks.Reflection;
using Old8Lang.Benchmarks.Benchmarks.VM.Reports;
using Old8Lang.Benchmarks.Benchmarks.VM.Suites;
using Old8Lang.Benchmarks.Tools;

namespace Old8Lang.Benchmarks.Entry;

/// <summary>
/// Benchmark 项目入口逻辑。
/// </summary>
internal static class BenchmarkProgram
{
    public static int Run(string[] args)
    {
        Console.WriteLine("=== Old8Lang 性能基准测试 ===\n");

        var command = BenchmarkArgumentParser.Parse(args);
        return command switch
        {
            BenchmarkCommand.Validate => RunValidate(args),
            BenchmarkCommand.VmDiagnose => RunVmDiagnose(),
            BenchmarkCommand.QuickCompare => RunQuickCompare(args),
            BenchmarkCommand.Vm => RunVmBenchmarksAndOptionalReport(generateReport: false),
            BenchmarkCommand.VmReport => RunVmBenchmarksAndOptionalReport(generateReport: true),
            BenchmarkCommand.VmExtended => RunExtendedVmBenchmarksAndOptionalReport(generateReport: false),
            BenchmarkCommand.VmReportExtended => RunExtendedVmBenchmarksAndOptionalReport(generateReport: true),
            BenchmarkCommand.VmQuick => RunQuickVmBenchmarksAndOptionalReport(generateReport: false),
            BenchmarkCommand.VmReportQuick => RunQuickVmBenchmarksAndOptionalReport(generateReport: true),
            BenchmarkCommand.VmNightly => RunNightlyVmBenchmarksAndOptionalReport(generateReport: false),
            BenchmarkCommand.VmReportNightly => RunNightlyVmBenchmarksAndOptionalReport(generateReport: true),
            BenchmarkCommand.VmConcurrency => RunConcurrencyVmBenchmarksAndOptionalReport(generateReport: false),
            BenchmarkCommand.VmReportConcurrency => RunConcurrencyVmBenchmarksAndOptionalReport(generateReport: true),
            _ => RunDefaultParserBenchmarks()
        };
    }

    private static int RunValidate(string[] args)
    {
        Console.WriteLine("运行性能验证测试...\n");
        PerformanceValidator.Main(args);
        return 0;
    }

    private static int RunQuickCompare(string[] args)
    {
        Console.WriteLine("运行快速性能对比测试...\n");
        SimpleReflectionBenchmark.Main(args);
        return 0;
    }

    private static int RunVmDiagnose()
    {
        Console.WriteLine("运行 VM 热点诊断...\n");
        var reportsDir = GetReportsDir();
        var reportPath = VmHotspotDiagnosticRunner.Run(reportsDir);
        Console.WriteLine($"VM 热点诊断报告: {reportPath}");
        return 0;
    }

    private static int RunDefaultParserBenchmarks()
    {
        Console.WriteLine("测试词法分析和语法分析的性能\n");
        BenchmarkRunner.Run<ParserCoreBenchmark>();
        Console.WriteLine("\n✅ 所有性能测试已完成！");
        Console.WriteLine("结果已保存到: BenchmarkDotNet.Artifacts/results/");
        return 0;
    }

    private static int RunVmBenchmarksAndOptionalReport(bool generateReport)
    {
        Console.WriteLine("运行 VM 模式基准测试（BenchmarkDotNet）...\n");
        BenchmarkRunner.Run<VMModePerformanceBenchmarks>();

        if (!generateReport)
        {
            return 0;
        }

        GenerateVmReport(
            "从 BenchmarkDotNet artifacts 生成 VM 性能报告...\n",
            VMPerformanceReport.GenerateFromBenchmarkArtifacts,
            "VM");
        return 0;
    }

    private static int RunExtendedVmBenchmarksAndOptionalReport(bool generateReport)
    {
        Console.WriteLine("运行扩展 VM 模式基准测试（BenchmarkDotNet）...\n");
        BenchmarkRunner.Run<VMExtendedPerformanceBenchmarks>();

        if (!generateReport)
        {
            return 0;
        }

        GenerateVmReport(
            "从 BenchmarkDotNet artifacts 生成扩展 VM 性能报告...\n",
            VMPerformanceReport.GenerateExtendedFromBenchmarkArtifacts,
            "扩展 VM");
        return 0;
    }

    private static int RunQuickVmBenchmarksAndOptionalReport(bool generateReport)
    {
        Console.WriteLine("运行 VM Quick 基准测试（BenchmarkDotNet）...\n");
        BenchmarkRunner.Run<VMQuickPerformanceBenchmarks>();

        if (!generateReport)
        {
            return 0;
        }

        GenerateVmReport(
            "从 BenchmarkDotNet artifacts 生成 VM Quick 性能报告...\n",
            VMPerformanceReport.GenerateQuickFromBenchmarkArtifacts,
            "VM Quick");
        return 0;
    }

    private static int RunNightlyVmBenchmarksAndOptionalReport(bool generateReport)
    {
        Console.WriteLine("运行 VM Nightly 基准测试（BenchmarkDotNet）...\n");
        BenchmarkRunner.Run<VMNightlyPerformanceBenchmarks>();

        if (!generateReport)
        {
            return 0;
        }

        var (_, nightlyJsonPath) = GenerateVmReport(
            "从 BenchmarkDotNet artifacts 生成 VM Nightly 性能报告...\n",
            VMPerformanceReport.GenerateNightlyFromBenchmarkArtifacts,
            "VM Nightly");

        if (!VMPerformanceReport.HasFailStatus(nightlyJsonPath))
        {
            return 0;
        }

        Console.WriteLine("检测到 VM Nightly 性能回归 FAIL，返回非零退出码。");
        return 1;
    }

    private static int RunConcurrencyVmBenchmarksAndOptionalReport(bool generateReport)
    {
        Console.WriteLine("运行 VM 并发/异步专项基准测试（BenchmarkDotNet）...\n");
        BenchmarkRunner.Run<VMConcurrencyPerformanceBenchmarks>();

        if (!generateReport)
        {
            return 0;
        }

        GenerateVmReport(
            "从 BenchmarkDotNet artifacts 生成 VM 并发/异步专项报告...\n",
            VMPerformanceReport.GenerateConcurrencyFromBenchmarkArtifacts,
            "VM Concurrency");
        return 0;
    }

    private static (string markdownPath, string jsonPath) GenerateVmReport(
        string generationMessage,
        Func<string, string, (string markdownPath, string jsonPath)> generator,
        string reportLabel)
    {
        Console.WriteLine(generationMessage);
        var artifactsDir = GetArtifactsDir();
        var reportsDir = GetReportsDir();
        var (markdownPath, jsonPath) = generator(artifactsDir, reportsDir);

        Console.WriteLine($"{reportLabel} Markdown 报告: {markdownPath}");
        Console.WriteLine($"{reportLabel} JSON 报告: {jsonPath}");
        return (markdownPath, jsonPath);
    }

    private static string GetArtifactsDir()
    {
        return Path.Combine(Directory.GetCurrentDirectory(), "BenchmarkDotNet.Artifacts", "results");
    }

    private static string GetReportsDir()
    {
        return Path.Combine(Directory.GetCurrentDirectory(), "Reports");
    }
}
