# Old8Lang VM Quick Performance Report

- GeneratedAt: 2026-04-25T11:30:40.7836220+08:00
- SourceCsv: `C:\Projects\RiderProjects\Old8Lang\BenchmarkDotNet.Artifacts\results\Old8Lang.Benchmarks.Benchmarks.VM.Suites.VMQuickPerformanceBenchmarks-report.csv`
- BaselineJson: `C:\Projects\RiderProjects\Old8Lang\Reports\VM_Quick_Performance_Report_20260425_111231.json`

## Concurrency

### Per-Job Details

| Scenario | Job | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_Concurrency_Channel_MPMC_4Workers | Job-EATLBP | 79.489 | 4.364 | N/A | 35350927 | 1258038.868 | 353.509 | -10.97 | 12.00 | PASS | N/A |
| VMXQ_Concurrency_MutexAtomicCounter_4Workers | Job-EATLBP | 225.311 | 12.290 | N/A | 62186496 | 1420259.739 | 194.333 | 3.55 | 12.00 | PASS | N/A |
| VMXQ_Concurrency_Channel_MPMC_4Workers | Job-LGHQEI | 77.912 | 2.076 | N/A | 35437251 | 1283504.275 | 354.373 | 7.18 | 12.00 | PASS | N/A |
| VMXQ_Concurrency_MutexAtomicCounter_4Workers | Job-LGHQEI | 225.267 | 5.582 | N/A | 62186527 | 1420535.258 | 194.333 | 7.97 | 12.00 | PASS | N/A |

### Scenario Aggregate (Median)

| Scenario | Jobs | MedianMean(ms) | MedianStdDev(ms) | MedianP95(ms) | MedianAllocated(bytes) | MedianThroughput(ops/s) | MedianAlloc/Op(bytes) | MedianMeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_Concurrency_Channel_MPMC_4Workers | 2 | 78.700 | 3.220 | N/A | 35394089 | 1270771.572 | 353.941 | -1.90 | 12.00 | PASS | N/A |
| VMXQ_Concurrency_MutexAtomicCounter_4Workers | 2 | 225.289 | 8.936 | N/A | 62186512 | 1420397.498 | 194.333 | 5.76 | 12.00 | PASS | N/A |

## Edge

### Per-Job Details

| Scenario | Job | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_Edge_DeepRecursion_NearLimit | Job-EATLBP | 0.311 | 0.013 | N/A | 30904 | 3215434.084 | 30.904 | 9.74 | 10.00 | WARN | N/A |
| VMXQ_Edge_HighArgCount_CallHotPath | Job-EATLBP | 82.652 | 2.454 | N/A | 7205366 | 362968.941 | 240.179 | -2.56 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighThrowRate_TryCatch | Job-EATLBP | 20.340 | 0.721 | N/A | 5044992 | 1966539.333 | 126.125 | 4.92 | 10.00 | PASS | N/A |
| VMXQ_Edge_LargeClosureCapture_HighFreq | Job-EATLBP | 71.338 | 4.836 | N/A | 14806794 | 700884.797 | 296.136 | 2.93 | 10.00 | PASS | N/A |
| VMXQ_Edge_DeepRecursion_NearLimit | Job-LGHQEI | 0.295 | 0.015 | N/A | 30904 | 3388681.803 | 30.904 | 6.77 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighArgCount_CallHotPath | Job-LGHQEI | 87.479 | 4.528 | N/A | 7205366 | 342941.016 | 240.179 | 1.76 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighThrowRate_TryCatch | Job-LGHQEI | 19.925 | 1.228 | N/A | 5044992 | 2007528.231 | 126.125 | 4.06 | 10.00 | PASS | N/A |
| VMXQ_Edge_LargeClosureCapture_HighFreq | Job-LGHQEI | 65.111 | 2.204 | N/A | 14806794 | 767920.579 | 296.136 | -0.14 | 10.00 | PASS | N/A |

### Scenario Aggregate (Median)

| Scenario | Jobs | MedianMean(ms) | MedianStdDev(ms) | MedianP95(ms) | MedianAllocated(bytes) | MedianThroughput(ops/s) | MedianAlloc/Op(bytes) | MedianMeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_Edge_DeepRecursion_NearLimit | 2 | 0.303 | 0.014 | N/A | 30904 | 3302057.943 | 30.904 | 8.25 | 10.00 | WARN | N/A |
| VMXQ_Edge_HighArgCount_CallHotPath | 2 | 85.065 | 3.491 | N/A | 7205366 | 352954.979 | 240.179 | -0.40 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighThrowRate_TryCatch | 2 | 20.133 | 0.975 | N/A | 5044992 | 1987033.782 | 126.125 | 4.49 | 10.00 | PASS | N/A |
| VMXQ_Edge_LargeClosureCapture_HighFreq | 2 | 68.225 | 3.520 | N/A | 14806794 | 734402.688 | 296.136 | 1.39 | 10.00 | PASS | N/A |

## LargeFile

### Per-Job Details

| Scenario | Job | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_LargeFile_CompileAndExecute_10k | Job-EATLBP | 28.720 | 2.718 | N/A | 2431990 | 348195.477 | 243.199 | 52.67 | 8.00 | FAIL | N/A |
| VMXQ_LargeFile_CompileAndExecute_50k_Generated | Job-EATLBP | 99.841 | 8.270 | N/A | 11679969 | 500797.269 | 233.599 | -25.63 | 8.00 | PASS | N/A |
| VMXQ_LargeFile_CompileAndExecute_10k | Job-LGHQEI | 15.223 | 0.663 | N/A | 2431990 | 656909.373 | 243.199 | 6.28 | 8.00 | PASS | N/A |
| VMXQ_LargeFile_CompileAndExecute_50k_Generated | Job-LGHQEI | 117.823 | 17.651 | N/A | 11679969 | 424366.442 | 233.599 | 43.00 | 8.00 | FAIL | N/A |

### Scenario Aggregate (Median)

| Scenario | Jobs | MedianMean(ms) | MedianStdDev(ms) | MedianP95(ms) | MedianAllocated(bytes) | MedianThroughput(ops/s) | MedianAlloc/Op(bytes) | MedianMeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_LargeFile_CompileAndExecute_10k | 2 | 21.971 | 1.691 | N/A | 2431990 | 502552.425 | 243.199 | 29.48 | 8.00 | FAIL | N/A |
| VMXQ_LargeFile_CompileAndExecute_50k_Generated | 2 | 108.832 | 12.960 | N/A | 11679969 | 462581.856 | 233.599 | 8.68 | 8.00 | FAIL | N/A |

## Warnings
- 检测到执行失败场景（FAIL），请优先查看 FailureReason 与 BenchmarkDotNet 日志。
