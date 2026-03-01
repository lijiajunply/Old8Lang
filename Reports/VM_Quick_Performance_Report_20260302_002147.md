# Old8Lang VM Quick Performance Report

- GeneratedAt: 2026-03-02T00:21:47.3938360+08:00
- SourceCsv: `/Users/luckyfish/Documents/Project/Old8LangProjects/Old8Lang/BenchmarkDotNet.Artifacts/results/Old8Lang.Benchmarks.Benchmarks.VM.Suites.VMQuickPerformanceBenchmarks-report.csv`
- BaselineJson: `N/A`

## Concurrency

| Scenario | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | Threshold(%) | Status |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---|
| VMXQ_Concurrency_Channel_MPMC_4Workers | N/A | N/A | N/A | N/A | N/A | N/A | N/A | 12.00 | N/A |
| VMXQ_Concurrency_MutexAtomicCounter_4Workers | 252.553 | 2.972 | N/A | 62182605 | 1267060.775 | 194.321 | N/A | 12.00 | N/A |
| VMXQ_Concurrency_Channel_MPMC_4Workers | N/A | N/A | N/A | N/A | N/A | N/A | N/A | 12.00 | N/A |
| VMXQ_Concurrency_MutexAtomicCounter_4Workers | 241.784 | 1.292 | N/A | 62187919 | 1323494.256 | 194.337 | N/A | 12.00 | N/A |

## Edge

| Scenario | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | Threshold(%) | Status |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---|
| VMXQ_Edge_DeepRecursion_NearLimit | 0.273 | 0.025 | N/A | 39782 | 3658982.803 | 39.782 | N/A | 10.00 | N/A |
| VMXQ_Edge_HighArgCount_CallHotPath | 286.979 | 14.406 | N/A | 16323994 | 104537.266 | 544.133 | N/A | 10.00 | N/A |
| VMXQ_Edge_HighThrowRate_TryCatch | 36.792 | 0.662 | N/A | 10003763 | 1087204.688 | 250.094 | N/A | 10.00 | N/A |
| VMXQ_Edge_LargeClosureCapture_HighFreq | 39.347 | 1.737 | N/A | 26005299 | 1270754.599 | 520.106 | N/A | 10.00 | N/A |
| VMXQ_Edge_DeepRecursion_NearLimit | 0.359 | 0.060 | N/A | 39782 | 2782415.136 | 39.782 | N/A | 10.00 | N/A |
| VMXQ_Edge_HighArgCount_CallHotPath | 368.526 | 46.692 | N/A | 16323994 | 81405.338 | 544.133 | N/A | 10.00 | N/A |
| VMXQ_Edge_HighThrowRate_TryCatch | 54.469 | 12.435 | N/A | 10003763 | 734366.710 | 250.094 | N/A | 10.00 | N/A |
| VMXQ_Edge_LargeClosureCapture_HighFreq | 34.186 | 3.895 | N/A | 26005299 | 1462587.024 | 520.106 | N/A | 10.00 | N/A |

## LargeFile

| Scenario | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | Threshold(%) | Status |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---|
| VMXQ_LargeFile_CompileAndExecute_10k | 15.575 | 0.372 | N/A | 2800527 | 642046.330 | 280.053 | N/A | 8.00 | N/A |
| VMXQ_LargeFile_CompileAndExecute_50k_Generated | 87.982 | 2.869 | N/A | 14122844 | 568300.645 | 282.457 | N/A | 8.00 | N/A |
| VMXQ_LargeFile_CompileAndExecute_10k | 20.130 | 1.671 | N/A | 2800527 | 496766.053 | 280.053 | N/A | 8.00 | N/A |
| VMXQ_LargeFile_CompileAndExecute_50k_Generated | 88.528 | 3.852 | N/A | 14122844 | 564793.698 | 282.457 | N/A | 8.00 | N/A |

## Warnings
- 未找到历史基线报告，回归对比状态将显示 N/A。
