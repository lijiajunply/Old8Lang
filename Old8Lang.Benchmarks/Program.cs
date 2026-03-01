using BenchmarkDotNet.Running;
using Old8Lang.Benchmarks;
using Old8Lang.PerformanceValidation;

Environment.ExitCode = BenchmarkProgram.Run(args);

public enum BenchmarkCommand
{
    DefaultParserBenchmark,
    Validate,
    QuickCompare,
    Vm,
    VmReport,
    VmExtended,
    VmReportExtended,
    VmQuick,
    VmReportQuick,
    VmNightly,
    VmReportNightly
}

internal static class BenchmarkArgumentParser
{
    public static BenchmarkCommand Parse(IReadOnlyList<string> args)
    {
        if (args.Count == 0)
        {
            return BenchmarkCommand.DefaultParserBenchmark;
        }

        return args[0] switch
        {
            "--validate" => BenchmarkCommand.Validate,
            "--quick" => BenchmarkCommand.QuickCompare,
            "--vm" => BenchmarkCommand.Vm,
            "--vm-report" => BenchmarkCommand.VmReport,
            "--vm-extended" => BenchmarkCommand.VmExtended,
            "--vm-report-extended" => BenchmarkCommand.VmReportExtended,
            "--vm-quick" => BenchmarkCommand.VmQuick,
            "--vm-report-quick" => BenchmarkCommand.VmReportQuick,
            "--vm-nightly" => BenchmarkCommand.VmNightly,
            "--vm-report-nightly" => BenchmarkCommand.VmReportNightly,
            _ => BenchmarkCommand.DefaultParserBenchmark
        };
    }
}

internal static class BenchmarkProgram
{
    public static int Run(string[] args)
    {
        Console.WriteLine("=== Old8Lang 性能基准测试 ===\n");

        var command = BenchmarkArgumentParser.Parse(args);
        return command switch
        {
            BenchmarkCommand.Validate => RunValidate(args),
            BenchmarkCommand.QuickCompare => RunQuickCompare(args),
            BenchmarkCommand.Vm => RunVmBenchmarksAndOptionalReport(generateReport: false),
            BenchmarkCommand.VmReport => RunVmBenchmarksAndOptionalReport(generateReport: true),
            BenchmarkCommand.VmExtended => RunExtendedVmBenchmarksAndOptionalReport(generateReport: false),
            BenchmarkCommand.VmReportExtended => RunExtendedVmBenchmarksAndOptionalReport(generateReport: true),
            BenchmarkCommand.VmQuick => RunQuickVmBenchmarksAndOptionalReport(generateReport: false),
            BenchmarkCommand.VmReportQuick => RunQuickVmBenchmarksAndOptionalReport(generateReport: true),
            BenchmarkCommand.VmNightly => RunNightlyVmBenchmarksAndOptionalReport(generateReport: false),
            BenchmarkCommand.VmReportNightly => RunNightlyVmBenchmarksAndOptionalReport(generateReport: true),
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

    private static int RunDefaultParserBenchmarks()
    {
        Console.WriteLine("测试词法分析和语法分析的性能\n");
        BenchmarkRunner.Run<ParserBenchmarkTests>();
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

        Console.WriteLine("从 BenchmarkDotNet artifacts 生成 VM 性能报告...\n");
        var artifactsDir = Path.Combine(Directory.GetCurrentDirectory(), "BenchmarkDotNet.Artifacts", "results");
        var reportsDir = Path.Combine(Directory.GetCurrentDirectory(), "Reports");
        var (markdownPath, jsonPath) = VMPerformanceReport.GenerateFromBenchmarkArtifacts(artifactsDir, reportsDir);

        Console.WriteLine($"VM Markdown 报告: {markdownPath}");
        Console.WriteLine($"VM JSON 报告: {jsonPath}");
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

        Console.WriteLine("从 BenchmarkDotNet artifacts 生成扩展 VM 性能报告...\n");
        var artifactsDir = Path.Combine(Directory.GetCurrentDirectory(), "BenchmarkDotNet.Artifacts", "results");
        var reportsDir = Path.Combine(Directory.GetCurrentDirectory(), "Reports");
        var (markdownPath, jsonPath) = VMPerformanceReport.GenerateExtendedFromBenchmarkArtifacts(artifactsDir, reportsDir);

        Console.WriteLine($"扩展 VM Markdown 报告: {markdownPath}");
        Console.WriteLine($"扩展 VM JSON 报告: {jsonPath}");
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

        Console.WriteLine("从 BenchmarkDotNet artifacts 生成 VM Quick 性能报告...\n");
        var artifactsDir = Path.Combine(Directory.GetCurrentDirectory(), "BenchmarkDotNet.Artifacts", "results");
        var reportsDir = Path.Combine(Directory.GetCurrentDirectory(), "Reports");
        var (markdownPath, jsonPath) = VMPerformanceReport.GenerateQuickFromBenchmarkArtifacts(artifactsDir, reportsDir);

        Console.WriteLine($"VM Quick Markdown 报告: {markdownPath}");
        Console.WriteLine($"VM Quick JSON 报告: {jsonPath}");
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

        Console.WriteLine("从 BenchmarkDotNet artifacts 生成 VM Nightly 性能报告...\n");
        var artifactsDir = Path.Combine(Directory.GetCurrentDirectory(), "BenchmarkDotNet.Artifacts", "results");
        var reportsDir = Path.Combine(Directory.GetCurrentDirectory(), "Reports");
        var (markdownPath, jsonPath) = VMPerformanceReport.GenerateNightlyFromBenchmarkArtifacts(artifactsDir, reportsDir);

        Console.WriteLine($"VM Nightly Markdown 报告: {markdownPath}");
        Console.WriteLine($"VM Nightly JSON 报告: {jsonPath}");

        var hasFail = VMPerformanceReport.HasFailStatus(jsonPath);
        if (hasFail)
        {
            Console.WriteLine("检测到 VM Nightly 性能回归 FAIL，返回非零退出码。");
            return 1;
        }

        return 0;
    }
}
