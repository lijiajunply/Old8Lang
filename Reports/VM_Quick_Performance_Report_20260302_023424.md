# Old8Lang VM Quick Performance Report

- GeneratedAt: 2026-03-02T02:34:24.7788290+08:00
- SourceCsv: `/Users/luckyfish/Documents/Project/Old8LangProjects/Old8Lang/BenchmarkDotNet.Artifacts/results/Old8Lang.Benchmarks.Benchmarks.VM.Suites.VMQuickPerformanceBenchmarks-report.csv`
- BaselineJson: `/Users/luckyfish/Documents/Project/Old8LangProjects/Old8Lang/Reports/VM_Quick_Performance_Report_20260302_021345.json`

## Concurrency

### Per-Job Details

| Scenario | Job | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_Concurrency_Channel_MPMC_4Workers | Job-EATLBP | 72.276 | 3.267 | N/A | 29676114 | 1383588.974 | 296.761 | -19.11 | 12.00 | PASS | N/A |
| VMXQ_Concurrency_MutexAtomicCounter_4Workers | Job-EATLBP | 114.836 | 1.163 | N/A | 62174351 | 2786577.752 | 194.295 | -10.60 | 12.00 | PASS | N/A |
| VMXQ_Concurrency_Channel_MPMC_4Workers | Job-LGHQEI | 74.212 | 1.607 | N/A | 29811272 | 1347494.603 | 298.113 | -16.94 | 12.00 | PASS | N/A |
| VMXQ_Concurrency_MutexAtomicCounter_4Workers | Job-LGHQEI | 128.970 | 7.486 | N/A | 62173921 | 2481206.797 | 194.294 | 0.41 | 12.00 | PASS | N/A |

### Scenario Aggregate (Median)

| Scenario | Jobs | MedianMean(ms) | MedianStdDev(ms) | MedianP95(ms) | MedianAllocated(bytes) | MedianThroughput(ops/s) | MedianAlloc/Op(bytes) | MedianMeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_Concurrency_Channel_MPMC_4Workers | 2 | 73.244 | 2.437 | N/A | 29743693 | 1365541.789 | 297.437 | -18.03 | 12.00 | PASS | N/A |
| VMXQ_Concurrency_MutexAtomicCounter_4Workers | 2 | 121.903 | 4.325 | N/A | 62174136 | 2633892.274 | 194.294 | -5.09 | 12.00 | PASS | N/A |

## Edge

### Per-Job Details

| Scenario | Job | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_Edge_DeepRecursion_NearLimit | Job-EATLBP | 0.146 | 0.005 | N/A | 29092 | 6844626.968 | 29.092 | 3.91 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighArgCount_CallHotPath | Job-EATLBP | 40.689 | 0.996 | N/A | 10323948 | 737292.759 | 344.132 | -1.92 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighThrowRate_TryCatch | Job-EATLBP | 31.221 | 0.497 | N/A | 10083707 | 1281168.426 | 252.093 | -4.26 | 10.00 | PASS | N/A |
| VMXQ_Edge_LargeClosureCapture_HighFreq | Job-EATLBP | 27.927 | 0.366 | N/A | 20007148 | 1790350.013 | 400.143 | -9.19 | 10.00 | PASS | N/A |
| VMXQ_Edge_DeepRecursion_NearLimit | Job-LGHQEI | 0.143 | 0.003 | N/A | 29092 | 6988120.196 | 29.092 | 1.78 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighArgCount_CallHotPath | Job-LGHQEI | 45.983 | 2.370 | N/A | 10323948 | 652412.185 | 344.132 | 10.84 | 10.00 | FAIL | N/A |
| VMXQ_Edge_HighThrowRate_TryCatch | Job-LGHQEI | 32.386 | 0.356 | N/A | 10083707 | 1235086.333 | 252.093 | -0.69 | 10.00 | PASS | N/A |
| VMXQ_Edge_LargeClosureCapture_HighFreq | Job-LGHQEI | 28.601 | 0.373 | N/A | 20007148 | 1748196.735 | 400.143 | -7.00 | 10.00 | PASS | N/A |

### Scenario Aggregate (Median)

| Scenario | Jobs | MedianMean(ms) | MedianStdDev(ms) | MedianP95(ms) | MedianAllocated(bytes) | MedianThroughput(ops/s) | MedianAlloc/Op(bytes) | MedianMeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_Edge_DeepRecursion_NearLimit | 2 | 0.145 | 0.004 | N/A | 29092 | 6916373.582 | 29.092 | 2.84 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighArgCount_CallHotPath | 2 | 43.336 | 1.683 | N/A | 10323948 | 694852.472 | 344.132 | 4.46 | 10.00 | FAIL | N/A |
| VMXQ_Edge_HighThrowRate_TryCatch | 2 | 31.804 | 0.426 | N/A | 10083707 | 1258127.379 | 252.093 | -2.48 | 10.00 | PASS | N/A |
| VMXQ_Edge_LargeClosureCapture_HighFreq | 2 | 28.264 | 0.369 | N/A | 20007148 | 1769273.374 | 400.143 | -8.10 | 10.00 | PASS | N/A |

## LargeFile

### Per-Job Details

| Scenario | Job | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_LargeFile_CompileAndExecute_10k | Job-EATLBP | 6.955 | 0.124 | N/A | 2282967 | 1437855.869 | 228.297 | -8.62 | 8.00 | PASS | N/A |
| VMXQ_LargeFile_CompileAndExecute_50k_Generated | Job-EATLBP | 36.783 | 0.241 | N/A | 11524659 | 1359305.123 | 230.493 | -17.31 | 8.00 | PASS | N/A |
| VMXQ_LargeFile_CompileAndExecute_10k | Job-LGHQEI | 6.810 | 0.165 | N/A | 2282967 | 1468515.038 | 228.297 | -10.53 | 8.00 | PASS | N/A |
| VMXQ_LargeFile_CompileAndExecute_50k_Generated | Job-LGHQEI | 38.407 | 1.084 | N/A | 11524659 | 1301829.070 | 230.493 | -13.66 | 8.00 | PASS | N/A |

### Scenario Aggregate (Median)

| Scenario | Jobs | MedianMean(ms) | MedianStdDev(ms) | MedianP95(ms) | MedianAllocated(bytes) | MedianThroughput(ops/s) | MedianAlloc/Op(bytes) | MedianMeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_LargeFile_CompileAndExecute_10k | 2 | 6.882 | 0.145 | N/A | 2282967 | 1453185.453 | 228.297 | -9.57 | 8.00 | PASS | N/A |
| VMXQ_LargeFile_CompileAndExecute_50k_Generated | 2 | 37.596 | 0.663 | N/A | 11524659 | 1330567.097 | 230.493 | -15.49 | 8.00 | PASS | N/A |

## Warnings
- 检测到执行失败场景（FAIL），请优先查看 FailureReason 与 BenchmarkDotNet 日志。
