using BenchmarkDotNet.Running;
using Old8Lang.Benchmarks;

// 运行性能基准测试
Console.WriteLine("=== Old8Lang 性能基准测试 ===\n");

// 运行解释器性能测试
Console.WriteLine("正在运行解释器性能测试...");
BenchmarkRunner.Run<InterpreterBenchmarkTests>();

Console.WriteLine("\n正在运行编译器性能测试...");
BenchmarkRunner.Run<CompilerBenchmarkTests>();

Console.WriteLine("\n✅ 所有性能测试已完成！");
Console.WriteLine($"结果已保存到: BenchmarkDotNet.Artifacts/results/");
