using BenchmarkDotNet.Attributes;

namespace Old8Lang.Benchmarks;

/// <summary>
/// VM Quick 基准测试基类
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 1, iterationCount: 4)]
[Config(typeof(VMBenchmarkQuickConfig))]
public abstract class VMBenchmarkQuickBase : VMBenchmarkBase
{
}
