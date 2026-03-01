using BenchmarkDotNet.Running;
using Old8Lang.Benchmarks;
using Old8Lang.PerformanceValidation;

// 运行性能基准测试
Console.WriteLine("=== Old8Lang 性能基准测试 ===\n");

// 如果传入 --validate 参数，运行性能验证
if (args.Length > 0 && args[0] == "--validate")
{
    Console.WriteLine("运行性能验证测试...\n");
    PerformanceValidator.Main(args);
    return;
}

// 如果传入 --quick 参数，运行快速对比测试
if (args.Length > 0 && args[0] == "--quick")
{
    Console.WriteLine("运行快速性能对比测试...\n");
    SimpleReflectionBenchmark.Main(args);
    return;
}

// 如果传入 --vm 或 --vm-report 参数，统一走 BenchmarkDotNet 流程
if (args.Length > 0 && (args[0] == "--vm" || args[0] == "--vm-report"))
{
    var generateReport = args[0] == "--vm-report";
    RunVmBenchmarksAndOptionalReport(generateReport);
    return;
}

// // 运行反射性能基准测试（新增）
// Console.WriteLine("正在运行反射性能基准测试...");
// Console.WriteLine("对比优化前后的性能差异\n");
// BenchmarkRunner.Run<ReflectionPerformanceBenchmark>();
//
// Console.WriteLine("正在运行高级性能测试...");
// BenchmarkRunner.Run<AdvancedPerformanceTests>();
//
// // 运行解释器性能测试
// Console.WriteLine("正在运行解释器性能测试...");
// BenchmarkRunner.Run<InterpreterBenchmarkTests>();

// Console.WriteLine("\n正在运行编译器性能测试...");
// BenchmarkRunner.Run<CompilerBenchmarkTests>();

Console.WriteLine("测试词法分析和语法分析的性能\n");
BenchmarkRunner.Run<ParserBenchmarkTests>();

Console.WriteLine("\n✅ 所有性能测试已完成！");
Console.WriteLine($"结果已保存到: BenchmarkDotNet.Artifacts/results/");
return;

static void RunVmBenchmarksAndOptionalReport(bool generateReport)
{
    Console.WriteLine("运行 VM 模式基准测试（BenchmarkDotNet）...\n");
    BenchmarkRunner.Run<VMModePerformanceBenchmarks>();

    if (!generateReport)
    {
        return;
    }

    Console.WriteLine("从 BenchmarkDotNet artifacts 生成 VM 性能报告...\n");
    var artifactsDir = Path.Combine(Directory.GetCurrentDirectory(), "BenchmarkDotNet.Artifacts", "results");
    var reportsDir = Path.Combine(Directory.GetCurrentDirectory(), "Reports");
    var (markdownPath, jsonPath) = VMPerformanceReport.GenerateFromBenchmarkArtifacts(artifactsDir, reportsDir);

    Console.WriteLine($"VM Markdown 报告: {markdownPath}");
    Console.WriteLine($"VM JSON 报告: {jsonPath}");
}
