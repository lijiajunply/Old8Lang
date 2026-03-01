using BenchmarkDotNet.Attributes;
using Old8Lang.Benchmarks.Benchmarks.VM.Config;

namespace Old8Lang.Benchmarks.Benchmarks.VM.Base;

/// <summary>
/// VM Quick 基准测试基类
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 1, iterationCount: 4)]
[Config(typeof(VMBenchmarkQuickConfig))]
public abstract class VMBenchmarkQuickBase : VMBenchmarkBase
{
}
