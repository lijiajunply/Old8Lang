using BenchmarkDotNet.Configs;

namespace Old8Lang.Benchmarks;

/// <summary>
/// VM Quick 基准测试配置（PR 快速反馈）
/// </summary>
public sealed class VMBenchmarkQuickConfig : ManualConfig
{
    public VMBenchmarkQuickConfig()
    {
        Options |= ConfigOptions.DisableOptimizationsValidator;
    }
}
