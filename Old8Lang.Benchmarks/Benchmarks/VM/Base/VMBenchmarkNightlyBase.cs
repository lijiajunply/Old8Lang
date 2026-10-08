using BenchmarkDotNet.Attributes;
using Old8Lang.Benchmarks.Benchmarks.VM.Config;

namespace Old8Lang.Benchmarks.Benchmarks.VM.Base;

/// <summary>
/// VM Nightly 基准测试基类
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 8)]
[Config(typeof(VMBenchmarkNightlyConfig))]
public abstract class VMBenchmarkNightlyBase : VMBenchmarkBase
{
}
