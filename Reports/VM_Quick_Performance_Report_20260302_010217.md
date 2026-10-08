# Old8Lang VM Quick Performance Report

- GeneratedAt: 2026-03-02T01:02:17.8182570+08:00
- SourceCsv: `/Users/luckyfish/Documents/Project/Old8LangProjects/Old8Lang/BenchmarkDotNet.Artifacts/results/Old8Lang.Benchmarks.Benchmarks.VM.Suites.VMQuickPerformanceBenchmarks-report.csv`
- BaselineJson: `/Users/luckyfish/Documents/Project/Old8LangProjects/Old8Lang/Reports/VM_Quick_Performance_Report_20260302_002147.json`

## Concurrency

| Scenario | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_Concurrency_Channel_MPMC_4Workers | N/A | N/A | N/A | N/A | N/A | N/A | N/A | 12.00 | FAIL | Benchmark result is NA (no measurable output). |
| VMXQ_Concurrency_MutexAtomicCounter_4Workers | 249.761 | 11.559 | N/A | 62177874 | 1281224.338 | 194.306 | 3.30 | 12.00 | PASS | N/A |
| VMXQ_Concurrency_Channel_MPMC_4Workers | 82.886 | 0.595 | N/A | 37724774 | 1206476.365 | 377.248 | N/A | 12.00 | N/A | N/A |
| VMXQ_Concurrency_MutexAtomicCounter_4Workers | 237.670 | 3.583 | N/A | 62180035 | 1346406.945 | 194.313 | -1.70 | 12.00 | PASS | N/A |

## Edge

| Scenario | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_Edge_DeepRecursion_NearLimit | 0.142 | 0.003 | N/A | 39782 | 7032348.805 | 39.782 | -60.43 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighArgCount_CallHotPath | 42.583 | 0.778 | N/A | 16323994 | 704506.493 | 544.133 | -88.45 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighThrowRate_TryCatch | 39.453 | 7.214 | N/A | 10083758 | 1013874.878 | 252.094 | -27.57 | 10.00 | PASS | N/A |
| VMXQ_Edge_LargeClosureCapture_HighFreq | 32.837 | 3.093 | N/A | 26005299 | 1522649.410 | 520.106 | -3.94 | 10.00 | PASS | N/A |
| VMXQ_Edge_DeepRecursion_NearLimit | 0.141 | 0.005 | N/A | 39782 | 7107320.540 | 39.782 | -60.85 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighArgCount_CallHotPath | 42.706 | 0.515 | N/A | 16323994 | 702479.049 | 544.133 | -88.41 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighThrowRate_TryCatch | 37.123 | 3.812 | N/A | 10083758 | 1077490.417 | 252.094 | -31.84 | 10.00 | PASS | N/A |
| VMXQ_Edge_LargeClosureCapture_HighFreq | 30.780 | 1.464 | N/A | 26005299 | 1624405.062 | 520.106 | -9.96 | 10.00 | PASS | N/A |

## LargeFile

| Scenario | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_LargeFile_CompileAndExecute_10k | 7.742 | 0.447 | N/A | 2800527 | 1291589.171 | 280.053 | -61.54 | 8.00 | PASS | N/A |
| VMXQ_LargeFile_CompileAndExecute_50k_Generated | 38.676 | 0.227 | N/A | 14122844 | 1292788.053 | 282.457 | -56.31 | 8.00 | PASS | N/A |
| VMXQ_LargeFile_CompileAndExecute_10k | 7.306 | 0.196 | N/A | 2800527 | 1368831.702 | 280.053 | -63.71 | 8.00 | PASS | N/A |
| VMXQ_LargeFile_CompileAndExecute_50k_Generated | 38.287 | 0.463 | N/A | 14122844 | 1305916.061 | 282.457 | -56.75 | 8.00 | PASS | N/A |

## Warnings
- 检测到执行失败场景（FAIL），请优先查看 FailureReason 与 BenchmarkDotNet 日志。
