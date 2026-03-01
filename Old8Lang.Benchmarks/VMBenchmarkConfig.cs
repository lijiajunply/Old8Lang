using BenchmarkDotNet.Configs;

namespace Old8Lang.Benchmarks;

/// <summary>
/// VM 基准测试配置
/// </summary>
public sealed class VMBenchmarkConfig : ManualConfig
{
    public VMBenchmarkConfig()
    {
        // VM 基准在本仓库经常以 Debug 依赖运行，禁用优化校验以保证可执行。
        Options |= ConfigOptions.DisableOptimizationsValidator;
    }
}
