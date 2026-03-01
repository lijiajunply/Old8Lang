using BenchmarkDotNet.Configs;

namespace Old8Lang.Benchmarks.Benchmarks.VM.Config;

/// <summary>
/// VM Nightly 基准测试配置（夜间完整回归）
/// </summary>
public sealed class VMBenchmarkNightlyConfig : ManualConfig
{
    public VMBenchmarkNightlyConfig()
    {
        Options |= ConfigOptions.DisableOptimizationsValidator;
    }
}
