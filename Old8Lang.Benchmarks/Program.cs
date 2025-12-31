using BenchmarkDotNet.Running;
using Old8Lang.Benchmarks;

// 运行性能基准测试
Console.WriteLine("=== Old8Lang 性能基准测试 ===\n");

// 如果传入 --quick 参数，运行快速对比测试
if (args.Length > 0 && args[0] == "--quick")
{
    Console.WriteLine("运行快速性能对比测试...\n");
    SimpleReflectionBenchmark.Main(args);
    return;
}

// 运行反射性能基准测试（新增）
Console.WriteLine("正在运行反射性能基准测试...");
Console.WriteLine("对比优化前后的性能差异\n");
BenchmarkRunner.Run<ReflectionPerformanceBenchmark>();

Console.WriteLine("正在运行高级性能测试...");
BenchmarkRunner.Run<AdvancedPerformanceTests>();

// 运行解释器性能测试
Console.WriteLine("正在运行解释器性能测试...");
BenchmarkRunner.Run<InterpreterBenchmarkTests>();

Console.WriteLine("\n正在运行编译器性能测试...");
BenchmarkRunner.Run<CompilerBenchmarkTests>();

Console.WriteLine("\n✅ 所有性能测试已完成！");
Console.WriteLine($"结果已保存到: BenchmarkDotNet.Artifacts/results/");
