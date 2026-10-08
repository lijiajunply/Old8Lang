# Old8Lang VM Quick Performance Report

- GeneratedAt: 2026-04-25T11:45:38.4105309+08:00
- SourceCsv: `C:\Projects\RiderProjects\Old8Lang\BenchmarkDotNet.Artifacts\results\Old8Lang.Benchmarks.Benchmarks.VM.Suites.VMQuickPerformanceBenchmarks-report.csv`
- BaselineJson: `C:\Projects\RiderProjects\Old8Lang\Reports\VM_Quick_Performance_Report_20260425_113542.json`

## Concurrency

### Per-Job Details

| Scenario | Job | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_Concurrency_Channel_MPMC_4Workers | Job-EATLBP | 80.372 | 3.755 | N/A | 35345121 | 1244212.855 | 353.451 | -1.16 | 12.00 | PASS | N/A |
| VMXQ_Concurrency_MutexAtomicCounter_4Workers | Job-EATLBP | 217.546 | 4.837 | N/A | 62177751 | 1470955.298 | 194.305 | -2.74 | 12.00 | PASS | N/A |
| VMXQ_Concurrency_Channel_MPMC_4Workers | Job-LGHQEI | 83.525 | 3.941 | N/A | 34699971 | 1197240.600 | 347.000 | 9.36 | 12.00 | PASS | N/A |
| VMXQ_Concurrency_MutexAtomicCounter_4Workers | Job-LGHQEI | 237.464 | 12.710 | N/A | 62184755 | 1347570.982 | 194.327 | -0.53 | 12.00 | PASS | N/A |

### Scenario Aggregate (Median)

| Scenario | Jobs | MedianMean(ms) | MedianStdDev(ms) | MedianP95(ms) | MedianAllocated(bytes) | MedianThroughput(ops/s) | MedianAlloc/Op(bytes) | MedianMeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_Concurrency_Channel_MPMC_4Workers | 2 | 81.949 | 3.848 | N/A | 35022546 | 1220726.727 | 350.225 | 4.10 | 12.00 | PASS | N/A |
| VMXQ_Concurrency_MutexAtomicCounter_4Workers | 2 | 227.505 | 8.774 | N/A | 62181253 | 1409263.140 | 194.316 | -1.64 | 12.00 | PASS | N/A |

## Edge

### Per-Job Details

| Scenario | Job | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_Edge_DeepRecursion_NearLimit | Job-EATLBP | 0.291 | 0.006 | N/A | 29932 | 3441156.228 | 29.932 | 6.37 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighArgCount_CallHotPath | Job-EATLBP | 86.404 | 2.956 | N/A | 7204884 | 347206.952 | 240.163 | -0.40 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighThrowRate_TryCatch | Job-EATLBP | 20.198 | 1.007 | N/A | 5044541 | 1980364.684 | 126.114 | -1.79 | 10.00 | PASS | N/A |
| VMXQ_Edge_LargeClosureCapture_HighFreq | Job-EATLBP | 62.976 | 1.730 | N/A | 14805176 | 793958.295 | 296.104 | -0.88 | 10.00 | PASS | N/A |
| VMXQ_Edge_DeepRecursion_NearLimit | Job-LGHQEI | 0.276 | 0.018 | N/A | 29932 | 3617945.007 | 29.932 | -10.78 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighArgCount_CallHotPath | Job-LGHQEI | 83.823 | 4.457 | N/A | 7204884 | 357897.424 | 240.163 | -2.04 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighThrowRate_TryCatch | Job-LGHQEI | 19.307 | 0.791 | N/A | 5044541 | 2071808.896 | 126.114 | 0.96 | 10.00 | PASS | N/A |
| VMXQ_Edge_LargeClosureCapture_HighFreq | Job-LGHQEI | 61.310 | 2.529 | N/A | 14805176 | 815523.656 | 296.104 | 0.68 | 10.00 | PASS | N/A |

### Scenario Aggregate (Median)

| Scenario | Jobs | MedianMean(ms) | MedianStdDev(ms) | MedianP95(ms) | MedianAllocated(bytes) | MedianThroughput(ops/s) | MedianAlloc/Op(bytes) | MedianMeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_Edge_DeepRecursion_NearLimit | 2 | 0.283 | 0.012 | N/A | 29932 | 3529550.618 | 29.932 | -2.21 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighArgCount_CallHotPath | 2 | 85.113 | 3.707 | N/A | 7204884 | 352552.188 | 240.163 | -1.22 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighThrowRate_TryCatch | 2 | 19.753 | 0.899 | N/A | 5044541 | 2026086.790 | 126.114 | -0.42 | 10.00 | PASS | N/A |
| VMXQ_Edge_LargeClosureCapture_HighFreq | 2 | 62.143 | 2.129 | N/A | 14805176 | 804740.975 | 296.104 | -0.10 | 10.00 | PASS | N/A |

## LargeFile

### Per-Job Details

| Scenario | Job | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_LargeFile_CompileAndExecute_10k | Job-EATLBP | 15.321 | 0.515 | N/A | 1624381 | 652703.170 | 162.438 | -1.39 | 8.00 | PASS | N/A |
| VMXQ_LargeFile_CompileAndExecute_50k_Generated | Job-EATLBP | 88.070 | 5.034 | N/A | 8189420 | 567726.991 | 163.788 | -10.54 | 8.00 | PASS | N/A |
| VMXQ_LargeFile_CompileAndExecute_10k | Job-LGHQEI | 14.643 | 0.771 | N/A | 1624381 | 682896.848 | 162.438 | 0.95 | 8.00 | PASS | N/A |
| VMXQ_LargeFile_CompileAndExecute_50k_Generated | Job-LGHQEI | 81.314 | 5.862 | N/A | 8189420 | 614897.995 | 163.788 | 0.44 | 8.00 | PASS | N/A |

### Scenario Aggregate (Median)

| Scenario | Jobs | MedianMean(ms) | MedianStdDev(ms) | MedianP95(ms) | MedianAllocated(bytes) | MedianThroughput(ops/s) | MedianAlloc/Op(bytes) | MedianMeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_LargeFile_CompileAndExecute_10k | 2 | 14.982 | 0.643 | N/A | 1624381 | 667800.009 | 162.438 | -0.22 | 8.00 | PASS | N/A |
| VMXQ_LargeFile_CompileAndExecute_50k_Generated | 2 | 84.692 | 5.448 | N/A | 8189420 | 591312.493 | 163.788 | -5.05 | 8.00 | PASS | N/A |

