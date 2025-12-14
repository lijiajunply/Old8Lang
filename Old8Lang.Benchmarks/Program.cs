using BenchmarkDotNet.Running;
using Old8Lang.Benchmarks;

Console.WriteLine("Running Old8Lang Performance Benchmarks...");
Console.WriteLine("========================================");

Console.WriteLine();
// 运行编译器基准测试
Console.WriteLine("1. Running Compiler Benchmark Tests:");
BenchmarkRunner.Run<CompilerBenchmarkTests>();
Console.WriteLine();

// 运行解释器基准测试
Console.WriteLine("2. Running Interpreter Benchmark Tests:");
BenchmarkRunner.Run<InterpreterBenchmarkTests>();

Console.WriteLine();
Console.WriteLine("Benchmark tests completed!");