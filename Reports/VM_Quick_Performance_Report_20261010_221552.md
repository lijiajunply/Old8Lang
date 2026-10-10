# Old8Lang VM Quick Performance Report

- GeneratedAt: 2026-10-10T22:15:52.5228120+08:00
- SourceCsv: `/Users/luckyfish/Documents/Project/RiderProjects/Old8Lang/BenchmarkDotNet.Artifacts/results/Old8Lang.Benchmarks.Benchmarks.VM.Suites.VMQuickPerformanceBenchmarks-report.csv`
- BaselineJson: `/Users/luckyfish/Documents/Project/RiderProjects/Old8Lang/Reports/VM_Quick_Performance_Report_20260425_115227.json`

## Concurrency

### Per-Job Details

| Scenario | Job | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_Concurrency_Channel_MPMC_4Workers | Job-EATLBP | 62.124 | 1.412 | N/A | 24218890 | 1609691.631 | 242.189 | -53.55 | 12.00 | PASS | N/A |
| VMXQ_Concurrency_MutexAtomicCounter_4Workers | Job-EATLBP | 240.697 | 3.214 | N/A | 61470833 | 1329472.324 | 192.096 | -0.95 | 12.00 | PASS | N/A |
| VMXQ_Concurrency_Channel_MPMC_4Workers | Job-LGHQEI | 71.052 | 1.203 | N/A | 25182915 | 1407415.956 | 251.829 | -8.37 | 12.00 | PASS | N/A |
| VMXQ_Concurrency_MutexAtomicCounter_4Workers | Job-LGHQEI | 241.486 | 2.365 | N/A | 61477345 | 1325131.323 | 192.117 | 3.86 | 12.00 | PASS | N/A |

### Scenario Aggregate (Median)

| Scenario | Jobs | MedianMean(ms) | MedianStdDev(ms) | MedianP95(ms) | MedianAllocated(bytes) | MedianThroughput(ops/s) | MedianAlloc/Op(bytes) | MedianMeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_Concurrency_Channel_MPMC_4Workers | 2 | 66.588 | 1.307 | N/A | 24700903 | 1508553.794 | 247.009 | -30.96 | 12.00 | PASS | N/A |
| VMXQ_Concurrency_MutexAtomicCounter_4Workers | 2 | 241.091 | 2.789 | N/A | 61474089 | 1327301.823 | 192.107 | 1.45 | 12.00 | PASS | N/A |

## Edge

### Per-Job Details

| Scenario | Job | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_Edge_DeepRecursion_NearLimit | Job-EATLBP | 0.138 | 0.002 | N/A | 29583 | 7256894.049 | 29.583 | -54.07 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighArgCount_CallHotPath | Job-EATLBP | 38.020 | 0.068 | N/A | 7204547 | 789048.014 | 240.152 | -58.48 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighThrowRate_TryCatch | Job-EATLBP | 9.401 | 0.158 | N/A | 5044204 | 4255002.287 | 126.105 | -52.69 | 10.00 | PASS | N/A |
| VMXQ_Edge_LargeClosureCapture_HighFreq | Job-EATLBP | 23.126 | 0.505 | N/A | 14804736 | 2162040.620 | 296.095 | -47.30 | 10.00 | PASS | N/A |
| VMXQ_Edge_DeepRecursion_NearLimit | Job-LGHQEI | 0.139 | 0.002 | N/A | 29583 | 7209805.335 | 29.583 | -50.41 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighArgCount_CallHotPath | Job-LGHQEI | 37.734 | 0.256 | N/A | 7204547 | 795047.385 | 240.152 | -57.18 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighThrowRate_TryCatch | Job-LGHQEI | 10.463 | 0.214 | N/A | 5044204 | 3822922.242 | 126.105 | -46.28 | 10.00 | PASS | N/A |
| VMXQ_Edge_LargeClosureCapture_HighFreq | Job-LGHQEI | 22.832 | 0.371 | N/A | 14804736 | 2189928.083 | 296.095 | -47.06 | 10.00 | PASS | N/A |

### Scenario Aggregate (Median)

| Scenario | Jobs | MedianMean(ms) | MedianStdDev(ms) | MedianP95(ms) | MedianAllocated(bytes) | MedianThroughput(ops/s) | MedianAlloc/Op(bytes) | MedianMeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_Edge_DeepRecursion_NearLimit | 2 | 0.138 | 0.002 | N/A | 29583 | 7233349.692 | 29.583 | -52.24 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighArgCount_CallHotPath | 2 | 37.877 | 0.162 | N/A | 7204547 | 792047.699 | 240.152 | -57.83 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighThrowRate_TryCatch | 2 | 9.932 | 0.186 | N/A | 5044204 | 4038962.264 | 126.105 | -49.48 | 10.00 | PASS | N/A |
| VMXQ_Edge_LargeClosureCapture_HighFreq | 2 | 22.979 | 0.438 | N/A | 14804736 | 2175984.352 | 296.095 | -47.18 | 10.00 | PASS | N/A |

## LargeFile

### Per-Job Details

| Scenario | Job | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_LargeFile_CompileAndExecute_10k | Job-EATLBP | 6.320 | 0.058 | N/A | 1733140 | 1582228.410 | 173.314 | -62.74 | 8.00 | PASS | N/A |
| VMXQ_LargeFile_CompileAndExecute_50k_Generated | Job-EATLBP | 36.027 | 0.381 | N/A | 8664760 | 1387840.298 | 173.295 | -59.20 | 8.00 | PASS | N/A |
| VMXQ_LargeFile_CompileAndExecute_10k | Job-LGHQEI | 6.489 | 0.131 | N/A | 1733140 | 1541140.752 | 173.314 | -57.81 | 8.00 | PASS | N/A |
| VMXQ_LargeFile_CompileAndExecute_50k_Generated | Job-LGHQEI | 36.303 | 0.591 | N/A | 8664760 | 1377281.467 | 173.295 | -58.38 | 8.00 | PASS | N/A |

### Scenario Aggregate (Median)

| Scenario | Jobs | MedianMean(ms) | MedianStdDev(ms) | MedianP95(ms) | MedianAllocated(bytes) | MedianThroughput(ops/s) | MedianAlloc/Op(bytes) | MedianMeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_LargeFile_CompileAndExecute_10k | 2 | 6.404 | 0.094 | N/A | 1733140 | 1561684.581 | 173.314 | -60.27 | 8.00 | PASS | N/A |
| VMXQ_LargeFile_CompileAndExecute_50k_Generated | 2 | 36.165 | 0.486 | N/A | 8664760 | 1382560.883 | 173.295 | -58.79 | 8.00 | PASS | N/A |

