# Old8Lang VM Quick Performance Report

- GeneratedAt: 2026-10-10T22:31:12.7118500+08:00
- SourceCsv: `/Users/luckyfish/Documents/Project/RiderProjects/Old8Lang/BenchmarkDotNet.Artifacts/results/Old8Lang.Benchmarks.Benchmarks.VM.Suites.VMQuickPerformanceBenchmarks-report.csv`
- BaselineJson: `/Users/luckyfish/Documents/Project/RiderProjects/Old8Lang/Reports/VM_Quick_Performance_Report_20261010_221552.json`

## Concurrency

### Per-Job Details

| Scenario | Job | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_Concurrency_Channel_MPMC_4Workers | Job-EATLBP | 60.522 | 0.862 | N/A | 23806177 | 1652298.554 | 238.062 | -2.58 | 12.00 | PASS | N/A |
| VMXQ_Concurrency_MutexAtomicCounter_4Workers | Job-EATLBP | 199.194 | 6.480 | N/A | 61472205 | 1606474.413 | 192.101 | -17.24 | 12.00 | PASS | N/A |
| VMXQ_Concurrency_Channel_MPMC_4Workers | Job-LGHQEI | 59.413 | 0.836 | N/A | 24007905 | 1683142.953 | 240.079 | -16.38 | 12.00 | PASS | N/A |
| VMXQ_Concurrency_MutexAtomicCounter_4Workers | Job-LGHQEI | 191.567 | 3.157 | N/A | 61477304 | 1670432.535 | 192.117 | -20.67 | 12.00 | PASS | N/A |

### Scenario Aggregate (Median)

| Scenario | Jobs | MedianMean(ms) | MedianStdDev(ms) | MedianP95(ms) | MedianAllocated(bytes) | MedianThroughput(ops/s) | MedianAlloc/Op(bytes) | MedianMeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_Concurrency_Channel_MPMC_4Workers | 2 | 59.967 | 0.849 | N/A | 23907041 | 1667720.753 | 239.070 | -9.48 | 12.00 | PASS | N/A |
| VMXQ_Concurrency_MutexAtomicCounter_4Workers | 2 | 195.381 | 4.819 | N/A | 61474755 | 1638453.474 | 192.109 | -18.96 | 12.00 | PASS | N/A |

## Edge

### Per-Job Details

| Scenario | Job | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_Edge_DeepRecursion_NearLimit | Job-EATLBP | 0.041 | 0.001 | N/A | 29583 | 24154589.372 | 29.583 | -69.96 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighArgCount_CallHotPath | Job-EATLBP | 11.228 | 0.569 | N/A | 7204547 | 2671789.377 | 240.152 | -70.47 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighThrowRate_TryCatch | Job-EATLBP | 5.617 | 0.277 | N/A | 5044204 | 7120744.687 | 126.105 | -40.24 | 10.00 | PASS | N/A |
| VMXQ_Edge_LargeClosureCapture_HighFreq | Job-EATLBP | 15.999 | 0.610 | N/A | 14804736 | 3125130.865 | 296.095 | -30.82 | 10.00 | PASS | N/A |
| VMXQ_Edge_DeepRecursion_NearLimit | Job-LGHQEI | 0.040 | 0.003 | N/A | 29583 | 24789291.026 | 29.583 | -70.92 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighArgCount_CallHotPath | Job-LGHQEI | 10.505 | 0.329 | N/A | 7204547 | 2855660.633 | 240.152 | -72.16 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighThrowRate_TryCatch | Job-LGHQEI | 5.385 | 0.093 | N/A | 5044204 | 7427433.970 | 126.105 | -48.53 | 10.00 | PASS | N/A |
| VMXQ_Edge_LargeClosureCapture_HighFreq | Job-LGHQEI | 15.684 | 0.159 | N/A | 14804736 | 3187966.320 | 296.095 | -31.31 | 10.00 | PASS | N/A |

### Scenario Aggregate (Median)

| Scenario | Jobs | MedianMean(ms) | MedianStdDev(ms) | MedianP95(ms) | MedianAllocated(bytes) | MedianThroughput(ops/s) | MedianAlloc/Op(bytes) | MedianMeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_Edge_DeepRecursion_NearLimit | 2 | 0.041 | 0.002 | N/A | 29583 | 24471940.199 | 29.583 | -70.44 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighArgCount_CallHotPath | 2 | 10.867 | 0.449 | N/A | 7204547 | 2763725.005 | 240.152 | -71.31 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighThrowRate_TryCatch | 2 | 5.501 | 0.185 | N/A | 5044204 | 7274089.329 | 126.105 | -44.39 | 10.00 | PASS | N/A |
| VMXQ_Edge_LargeClosureCapture_HighFreq | 2 | 15.842 | 0.384 | N/A | 14804736 | 3156548.592 | 296.095 | -31.06 | 10.00 | PASS | N/A |

## LargeFile

### Per-Job Details

| Scenario | Job | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_LargeFile_CompileAndExecute_10k | Job-EATLBP | 1.741 | 0.016 | N/A | 1733140 | 5743099.666 | 173.314 | -72.45 | 8.00 | PASS | N/A |
| VMXQ_LargeFile_CompileAndExecute_50k_Generated | Job-EATLBP | 13.971 | 0.160 | N/A | 8664883 | 3578798.340 | 173.298 | -61.22 | 8.00 | PASS | N/A |
| VMXQ_LargeFile_CompileAndExecute_10k | Job-LGHQEI | 1.980 | 0.175 | N/A | 1733140 | 5049663.440 | 173.314 | -69.48 | 8.00 | PASS | N/A |
| VMXQ_LargeFile_CompileAndExecute_50k_Generated | Job-LGHQEI | 13.353 | 0.496 | N/A | 8664801 | 3744552.612 | 173.296 | -63.22 | 8.00 | PASS | N/A |

### Scenario Aggregate (Median)

| Scenario | Jobs | MedianMean(ms) | MedianStdDev(ms) | MedianP95(ms) | MedianAllocated(bytes) | MedianThroughput(ops/s) | MedianAlloc/Op(bytes) | MedianMeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_LargeFile_CompileAndExecute_10k | 2 | 1.861 | 0.096 | N/A | 1733140 | 5396381.553 | 173.314 | -70.97 | 8.00 | PASS | N/A |
| VMXQ_LargeFile_CompileAndExecute_50k_Generated | 2 | 13.662 | 0.328 | N/A | 8664842 | 3661675.476 | 173.297 | -62.22 | 8.00 | PASS | N/A |

