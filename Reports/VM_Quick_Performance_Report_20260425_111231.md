# Old8Lang VM Quick Performance Report

- GeneratedAt: 2026-04-25T11:12:31.2342393+08:00
- SourceCsv: `C:\Projects\RiderProjects\Old8Lang\BenchmarkDotNet.Artifacts\results\Old8Lang.Benchmarks.Benchmarks.VM.Suites.VMQuickPerformanceBenchmarks-report.csv`
- BaselineJson: `C:\Projects\RiderProjects\Old8Lang\Reports\VM_Quick_Performance_Report_20260424_191358.json`

## Concurrency

### Per-Job Details

| Scenario | Job | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_Concurrency_Channel_MPMC_4Workers | Job-EATLBP | 89.288 | 20.325 | N/A | 35189146 | 1119976.346 | 351.891 | 3.55 | 12.00 | PASS | N/A |
| VMXQ_Concurrency_MutexAtomicCounter_4Workers | Job-EATLBP | 217.584 | 14.798 | N/A | 62184724 | 1470698.402 | 194.327 | -6.65 | 12.00 | PASS | N/A |
| VMXQ_Concurrency_Channel_MPMC_4Workers | Job-LGHQEI | 72.693 | 3.831 | N/A | 35306895 | 1375644.489 | 353.069 | -15.90 | 12.00 | PASS | N/A |
| VMXQ_Concurrency_MutexAtomicCounter_4Workers | Job-LGHQEI | 208.647 | 9.923 | N/A | 62184663 | 1533688.670 | 194.327 | -24.81 | 12.00 | PASS | N/A |

### Scenario Aggregate (Median)

| Scenario | Jobs | MedianMean(ms) | MedianStdDev(ms) | MedianP95(ms) | MedianAllocated(bytes) | MedianThroughput(ops/s) | MedianAlloc/Op(bytes) | MedianMeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_Concurrency_Channel_MPMC_4Workers | 2 | 80.990 | 12.078 | N/A | 35248021 | 1247810.418 | 352.480 | -6.18 | 12.00 | PASS | N/A |
| VMXQ_Concurrency_MutexAtomicCounter_4Workers | 2 | 213.115 | 12.361 | N/A | 62184694 | 1502193.536 | 194.327 | -15.73 | 12.00 | PASS | N/A |

## Edge

### Per-Job Details

| Scenario | Job | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_Edge_DeepRecursion_NearLimit | Job-EATLBP | 0.283 | 0.014 | N/A | 29932 | 3528581.510 | 29.932 | -1.32 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighArgCount_CallHotPath | Job-EATLBP | 84.820 | 1.292 | N/A | 7204915 | 353691.001 | 240.164 | -0.81 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighThrowRate_TryCatch | Job-EATLBP | 19.387 | 0.675 | N/A | 5044552 | 2063195.684 | 126.114 | 9.33 | 10.00 | WARN | N/A |
| VMXQ_Edge_LargeClosureCapture_HighFreq | Job-EATLBP | 69.310 | 3.303 | N/A | 14808146 | 721392.461 | 296.163 | 11.74 | 10.00 | FAIL | N/A |
| VMXQ_Edge_DeepRecursion_NearLimit | Job-LGHQEI | 0.276 | 0.014 | N/A | 29932 | 3617945.007 | 29.932 | -0.25 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighArgCount_CallHotPath | Job-LGHQEI | 85.966 | 4.082 | N/A | 7204905 | 348973.958 | 240.163 | 4.05 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighThrowRate_TryCatch | Job-LGHQEI | 19.147 | 0.504 | N/A | 5044552 | 2089100.120 | 126.114 | 5.02 | 10.00 | PASS | N/A |
| VMXQ_Edge_LargeClosureCapture_HighFreq | Job-LGHQEI | 65.203 | 1.501 | N/A | 14808146 | 766832.354 | 296.163 | 2.85 | 10.00 | PASS | N/A |

### Scenario Aggregate (Median)

| Scenario | Jobs | MedianMean(ms) | MedianStdDev(ms) | MedianP95(ms) | MedianAllocated(bytes) | MedianThroughput(ops/s) | MedianAlloc/Op(bytes) | MedianMeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_Edge_DeepRecursion_NearLimit | 2 | 0.280 | 0.014 | N/A | 29932 | 3573263.259 | 29.932 | -0.79 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighArgCount_CallHotPath | 2 | 85.393 | 2.687 | N/A | 7204910 | 351332.480 | 240.164 | 1.62 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighThrowRate_TryCatch | 2 | 19.267 | 0.590 | N/A | 5044552 | 2076147.902 | 126.114 | 7.17 | 10.00 | WARN | N/A |
| VMXQ_Edge_LargeClosureCapture_HighFreq | 2 | 67.257 | 2.402 | N/A | 14808146 | 744112.407 | 296.163 | 7.29 | 10.00 | FAIL | N/A |

## LargeFile

### Per-Job Details

| Scenario | Job | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_LargeFile_CompileAndExecute_10k | Job-EATLBP | 18.811 | 1.651 | N/A | 1624392 | 531606.675 | 162.439 | 23.43 | 8.00 | FAIL | N/A |
| VMXQ_LargeFile_CompileAndExecute_50k_Generated | Job-EATLBP | 134.254 | 35.824 | N/A | 8189450 | 372428.659 | 163.789 | 70.81 | 8.00 | FAIL | N/A |
| VMXQ_LargeFile_CompileAndExecute_10k | Job-LGHQEI | 14.324 | 0.692 | N/A | 1624381 | 698138.762 | 162.438 | 3.42 | 8.00 | PASS | N/A |
| VMXQ_LargeFile_CompileAndExecute_50k_Generated | Job-LGHQEI | 82.394 | 4.046 | N/A | 8189440 | 606843.250 | 163.789 | 6.24 | 8.00 | PASS | N/A |

### Scenario Aggregate (Median)

| Scenario | Jobs | MedianMean(ms) | MedianStdDev(ms) | MedianP95(ms) | MedianAllocated(bytes) | MedianThroughput(ops/s) | MedianAlloc/Op(bytes) | MedianMeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_LargeFile_CompileAndExecute_10k | 2 | 16.567 | 1.172 | N/A | 1624387 | 614872.718 | 162.439 | 13.42 | 8.00 | FAIL | N/A |
| VMXQ_LargeFile_CompileAndExecute_50k_Generated | 2 | 108.324 | 19.935 | N/A | 8189445 | 489635.955 | 163.789 | 38.52 | 8.00 | FAIL | N/A |

## Warnings
- 检测到执行失败场景（FAIL），请优先查看 FailureReason 与 BenchmarkDotNet 日志。
