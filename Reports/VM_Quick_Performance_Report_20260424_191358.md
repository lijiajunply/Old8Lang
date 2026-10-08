# Old8Lang VM Quick Performance Report

- GeneratedAt: 2026-04-24T19:13:58.5512329+08:00
- SourceCsv: `C:\Projects\RiderProjects\Old8Lang\BenchmarkDotNet.Artifacts\results\Old8Lang.Benchmarks.Benchmarks.VM.Suites.VMQuickPerformanceBenchmarks-report.csv`
- BaselineJson: `C:\Projects\RiderProjects\Old8Lang\Reports\VM_Quick_Performance_Report_20260302_023424.json`

## Concurrency

### Per-Job Details

| Scenario | Job | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_Concurrency_Channel_MPMC_4Workers | Job-EATLBP | 86.229 | 13.984 | N/A | 35046543 | 1159697.273 | 350.465 | 19.31 | 12.00 | FAIL | N/A |
| VMXQ_Concurrency_MutexAtomicCounter_4Workers | Job-EATLBP | 233.078 | 8.868 | N/A | 62184694 | 1372930.361 | 194.327 | 102.97 | 12.00 | FAIL | N/A |
| VMXQ_Concurrency_Channel_MPMC_4Workers | Job-LGHQEI | 86.439 | 8.327 | N/A | 34292890 | 1156881.187 | 342.929 | 16.48 | 12.00 | FAIL | N/A |
| VMXQ_Concurrency_MutexAtomicCounter_4Workers | Job-LGHQEI | 277.493 | 7.366 | N/A | 62184724 | 1153183.489 | 194.327 | 115.16 | 12.00 | FAIL | N/A |

### Scenario Aggregate (Median)

| Scenario | Jobs | MedianMean(ms) | MedianStdDev(ms) | MedianP95(ms) | MedianAllocated(bytes) | MedianThroughput(ops/s) | MedianAlloc/Op(bytes) | MedianMeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_Concurrency_Channel_MPMC_4Workers | 2 | 86.334 | 11.156 | N/A | 34669717 | 1158289.230 | 346.697 | 17.89 | 12.00 | FAIL | N/A |
| VMXQ_Concurrency_MutexAtomicCounter_4Workers | 2 | 255.285 | 8.117 | N/A | 62184709 | 1263056.925 | 194.327 | 109.06 | 12.00 | FAIL | N/A |

## Edge

### Per-Job Details

| Scenario | Job | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_Edge_DeepRecursion_NearLimit | Job-EATLBP | 0.287 | 0.008 | N/A | 29932 | 3481894.150 | 29.932 | 96.58 | 10.00 | FAIL | N/A |
| VMXQ_Edge_HighArgCount_CallHotPath | Job-EATLBP | 85.513 | 0.422 | N/A | 7204905 | 350825.082 | 240.163 | 110.16 | 10.00 | FAIL | N/A |
| VMXQ_Edge_HighThrowRate_TryCatch | Job-EATLBP | 17.733 | 0.551 | N/A | 5044552 | 2255643.338 | 126.114 | -43.20 | 10.00 | PASS | N/A |
| VMXQ_Edge_LargeClosureCapture_HighFreq | Job-EATLBP | 62.027 | 1.485 | N/A | 14808146 | 806101.869 | 296.163 | 122.10 | 10.00 | FAIL | N/A |
| VMXQ_Edge_DeepRecursion_NearLimit | Job-LGHQEI | 0.277 | 0.015 | N/A | 29932 | 3608805.485 | 29.932 | 93.64 | 10.00 | FAIL | N/A |
| VMXQ_Edge_HighArgCount_CallHotPath | Job-LGHQEI | 82.622 | 2.114 | N/A | 7204905 | 363099.856 | 240.163 | 79.68 | 10.00 | FAIL | N/A |
| VMXQ_Edge_HighThrowRate_TryCatch | Job-LGHQEI | 18.232 | 0.267 | N/A | 5044552 | 2193908.613 | 126.114 | -43.70 | 10.00 | PASS | N/A |
| VMXQ_Edge_LargeClosureCapture_HighFreq | Job-LGHQEI | 63.399 | 1.590 | N/A | 14808136 | 788658.460 | 296.163 | 121.67 | 10.00 | FAIL | N/A |

### Scenario Aggregate (Median)

| Scenario | Jobs | MedianMean(ms) | MedianStdDev(ms) | MedianP95(ms) | MedianAllocated(bytes) | MedianThroughput(ops/s) | MedianAlloc/Op(bytes) | MedianMeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_Edge_DeepRecursion_NearLimit | 2 | 0.282 | 0.011 | N/A | 29932 | 3545349.818 | 29.932 | 95.11 | 10.00 | FAIL | N/A |
| VMXQ_Edge_HighArgCount_CallHotPath | 2 | 84.067 | 1.268 | N/A | 7204905 | 356962.469 | 240.163 | 94.92 | 10.00 | FAIL | N/A |
| VMXQ_Edge_HighThrowRate_TryCatch | 2 | 17.983 | 0.409 | N/A | 5044552 | 2224775.975 | 126.114 | -43.45 | 10.00 | PASS | N/A |
| VMXQ_Edge_LargeClosureCapture_HighFreq | 2 | 62.713 | 1.538 | N/A | 14808141 | 797380.165 | 296.163 | 121.88 | 10.00 | FAIL | N/A |

## LargeFile

### Per-Job Details

| Scenario | Job | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_LargeFile_CompileAndExecute_10k | Job-EATLBP | 15.241 | 0.812 | N/A | 1624392 | 656142.147 | 162.439 | 119.14 | 8.00 | FAIL | N/A |
| VMXQ_LargeFile_CompileAndExecute_50k_Generated | Job-EATLBP | 78.599 | 1.047 | N/A | 8189440 | 636142.028 | 163.789 | 113.68 | 8.00 | FAIL | N/A |
| VMXQ_LargeFile_CompileAndExecute_10k | Job-LGHQEI | 13.850 | 0.099 | N/A | 1624381 | 722026.874 | 162.438 | 103.39 | 8.00 | FAIL | N/A |
| VMXQ_LargeFile_CompileAndExecute_50k_Generated | Job-LGHQEI | 77.555 | 1.012 | N/A | 8189440 | 644702.927 | 163.789 | 101.93 | 8.00 | FAIL | N/A |

### Scenario Aggregate (Median)

| Scenario | Jobs | MedianMean(ms) | MedianStdDev(ms) | MedianP95(ms) | MedianAllocated(bytes) | MedianThroughput(ops/s) | MedianAlloc/Op(bytes) | MedianMeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_LargeFile_CompileAndExecute_10k | 2 | 14.545 | 0.456 | N/A | 1624387 | 689084.510 | 162.439 | 111.26 | 8.00 | FAIL | N/A |
| VMXQ_LargeFile_CompileAndExecute_50k_Generated | 2 | 78.077 | 1.030 | N/A | 8189440 | 640422.477 | 163.789 | 107.80 | 8.00 | FAIL | N/A |

## Warnings
- 检测到执行失败场景（FAIL），请优先查看 FailureReason 与 BenchmarkDotNet 日志。
