# Old8Lang VM Quick Performance Report

- GeneratedAt: 2026-03-02T01:08:25.5119890+08:00
- SourceCsv: `/Users/luckyfish/Documents/Project/Old8LangProjects/Old8Lang/BenchmarkDotNet.Artifacts/results/Old8Lang.Benchmarks.Benchmarks.VM.Suites.VMQuickPerformanceBenchmarks-report.csv`
- BaselineJson: `/Users/luckyfish/Documents/Project/Old8LangProjects/Old8Lang/Reports/VM_Quick_Performance_Report_20260302_010217.json`

## Concurrency

| Scenario | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_Concurrency_Channel_MPMC_4Workers | 78.191 | 0.434 | N/A | 29276692 | 1278922.840 | 292.767 | -5.66 | 12.00 | PASS | N/A |
| VMXQ_Concurrency_MutexAtomicCounter_4Workers | 279.923 | 25.923 | N/A | 62179400 | 1143173.557 | 194.311 | 17.78 | 12.00 | FAIL | N/A |
| VMXQ_Concurrency_Channel_MPMC_4Workers | 73.765 | 2.749 | N/A | 29616179 | 1355649.125 | 296.162 | -11.00 | 12.00 | PASS | N/A |
| VMXQ_Concurrency_MutexAtomicCounter_4Workers | 250.942 | 9.205 | N/A | 62187919 | 1275193.032 | 194.337 | 5.58 | 12.00 | PASS | N/A |

## Edge

| Scenario | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_Edge_DeepRecursion_NearLimit | 0.148 | 0.005 | N/A | 39782 | 6752194.463 | 39.782 | 5.26 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighArgCount_CallHotPath | 44.960 | 0.473 | N/A | 16323994 | 667258.302 | 544.133 | 5.28 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighThrowRate_TryCatch | 34.443 | 0.345 | N/A | 10083758 | 1161325.537 | 252.094 | -7.22 | 10.00 | PASS | N/A |
| VMXQ_Edge_LargeClosureCapture_HighFreq | 31.343 | 0.394 | N/A | 26005299 | 1595277.977 | 520.106 | 1.83 | 10.00 | PASS | N/A |
| VMXQ_Edge_DeepRecursion_NearLimit | 0.151 | 0.003 | N/A | 39782 | 6618133.686 | 39.782 | 7.39 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighArgCount_CallHotPath | 44.073 | 0.581 | N/A | 16323994 | 680685.768 | 544.133 | 3.20 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighThrowRate_TryCatch | 34.971 | 1.341 | N/A | 10083758 | 1143808.138 | 252.094 | -5.80 | 10.00 | PASS | N/A |
| VMXQ_Edge_LargeClosureCapture_HighFreq | 31.413 | 1.193 | N/A | 26005299 | 1591687.571 | 520.106 | 2.06 | 10.00 | PASS | N/A |

## LargeFile

| Scenario | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_LargeFile_CompileAndExecute_10k | 8.556 | 0.538 | N/A | 2800527 | 1168729.474 | 280.053 | 17.12 | 8.00 | FAIL | N/A |
| VMXQ_LargeFile_CompileAndExecute_50k_Generated | 41.955 | 0.355 | N/A | 14122844 | 1191755.909 | 282.457 | 9.58 | 8.00 | FAIL | N/A |
| VMXQ_LargeFile_CompileAndExecute_10k | 7.608 | 0.121 | N/A | 2800527 | 1314440.443 | 280.053 | 4.14 | 8.00 | PASS | N/A |
| VMXQ_LargeFile_CompileAndExecute_50k_Generated | 44.794 | 1.858 | N/A | 14122844 | 1116215.939 | 282.457 | 16.99 | 8.00 | FAIL | N/A |

## Warnings
- 检测到执行失败场景（FAIL），请优先查看 FailureReason 与 BenchmarkDotNet 日志。
