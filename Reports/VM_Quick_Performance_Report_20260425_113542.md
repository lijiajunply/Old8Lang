# Old8Lang VM Quick Performance Report

- GeneratedAt: 2026-04-25T11:35:42.5675864+08:00
- SourceCsv: `C:\Projects\RiderProjects\Old8Lang\BenchmarkDotNet.Artifacts\results\Old8Lang.Benchmarks.Benchmarks.VM.Suites.VMQuickPerformanceBenchmarks-report.csv`
- BaselineJson: `C:\Projects\RiderProjects\Old8Lang\Reports\VM_Quick_Performance_Report_20260425_113040.json`

## Concurrency

### Per-Job Details

| Scenario | Job | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_Concurrency_Channel_MPMC_4Workers | Job-EATLBP | 81.318 | 4.396 | N/A | 34133473 | 1229746.082 | 341.335 | 2.30 | 12.00 | PASS | N/A |
| VMXQ_Concurrency_MutexAtomicCounter_4Workers | Job-EATLBP | 223.684 | 9.225 | N/A | 62184765 | 1430590.221 | 194.327 | -0.72 | 12.00 | PASS | N/A |
| VMXQ_Concurrency_Channel_MPMC_4Workers | Job-LGHQEI | 76.376 | 0.736 | N/A | 35114476 | 1309318.683 | 351.145 | -1.97 | 12.00 | PASS | N/A |
| VMXQ_Concurrency_MutexAtomicCounter_4Workers | Job-LGHQEI | 238.723 | 7.691 | N/A | 62184704 | 1340465.728 | 194.327 | 5.97 | 12.00 | PASS | N/A |

### Scenario Aggregate (Median)

| Scenario | Jobs | MedianMean(ms) | MedianStdDev(ms) | MedianP95(ms) | MedianAllocated(bytes) | MedianThroughput(ops/s) | MedianAlloc/Op(bytes) | MedianMeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_Concurrency_Channel_MPMC_4Workers | 2 | 78.847 | 2.566 | N/A | 34623975 | 1269532.382 | 346.240 | 0.16 | 12.00 | PASS | N/A |
| VMXQ_Concurrency_MutexAtomicCounter_4Workers | 2 | 231.203 | 8.458 | N/A | 62184735 | 1385527.975 | 194.327 | 2.63 | 12.00 | PASS | N/A |

## Edge

### Per-Job Details

| Scenario | Job | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_Edge_DeepRecursion_NearLimit | Job-EATLBP | 0.273 | 0.012 | N/A | 30904 | 3660322.108 | 30.904 | -12.15 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighArgCount_CallHotPath | Job-EATLBP | 86.751 | 4.518 | N/A | 7204925 | 345815.745 | 240.164 | 4.96 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighThrowRate_TryCatch | Job-EATLBP | 20.566 | 1.136 | N/A | 5044562 | 1944919.869 | 126.114 | 1.11 | 10.00 | PASS | N/A |
| VMXQ_Edge_LargeClosureCapture_HighFreq | Job-EATLBP | 63.536 | 1.052 | N/A | 14805463 | 786959.143 | 296.109 | -10.94 | 10.00 | PASS | N/A |
| VMXQ_Edge_DeepRecursion_NearLimit | Job-LGHQEI | 0.310 | 0.012 | N/A | 30904 | 3227888.961 | 30.904 | 4.98 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighArgCount_CallHotPath | Job-LGHQEI | 85.567 | 3.341 | N/A | 7204925 | 350600.813 | 240.164 | -2.18 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighThrowRate_TryCatch | Job-LGHQEI | 19.123 | 0.190 | N/A | 5044562 | 2091667.320 | 126.114 | -4.02 | 10.00 | PASS | N/A |
| VMXQ_Edge_LargeClosureCapture_HighFreq | Job-LGHQEI | 60.898 | 2.128 | N/A | 14805463 | 821042.330 | 296.109 | -6.47 | 10.00 | PASS | N/A |

### Scenario Aggregate (Median)

| Scenario | Jobs | MedianMean(ms) | MedianStdDev(ms) | MedianP95(ms) | MedianAllocated(bytes) | MedianThroughput(ops/s) | MedianAlloc/Op(bytes) | MedianMeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_Edge_DeepRecursion_NearLimit | 2 | 0.291 | 0.012 | N/A | 30904 | 3444105.534 | 30.904 | -3.59 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighArgCount_CallHotPath | 2 | 86.159 | 3.929 | N/A | 7204925 | 348208.279 | 240.164 | 1.39 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighThrowRate_TryCatch | 2 | 19.845 | 0.663 | N/A | 5044562 | 2018293.595 | 126.114 | -1.46 | 10.00 | PASS | N/A |
| VMXQ_Edge_LargeClosureCapture_HighFreq | 2 | 62.217 | 1.590 | N/A | 14805463 | 804000.736 | 296.109 | -8.70 | 10.00 | PASS | N/A |

## LargeFile

### Per-Job Details

| Scenario | Job | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_LargeFile_CompileAndExecute_10k | Job-EATLBP | 15.537 | 0.870 | N/A | 1624402 | 643633.181 | 162.440 | -45.90 | 8.00 | PASS | N/A |
| VMXQ_LargeFile_CompileAndExecute_50k_Generated | Job-EATLBP | 98.449 | 11.275 | N/A | 8189460 | 507876.659 | 163.789 | -1.39 | 8.00 | PASS | N/A |
| VMXQ_LargeFile_CompileAndExecute_10k | Job-LGHQEI | 14.506 | 0.437 | N/A | 1624402 | 689388.926 | 162.440 | -4.71 | 8.00 | PASS | N/A |
| VMXQ_LargeFile_CompileAndExecute_50k_Generated | Job-LGHQEI | 80.957 | 1.762 | N/A | 8189460 | 617608.004 | 163.789 | -31.29 | 8.00 | PASS | N/A |

### Scenario Aggregate (Median)

| Scenario | Jobs | MedianMean(ms) | MedianStdDev(ms) | MedianP95(ms) | MedianAllocated(bytes) | MedianThroughput(ops/s) | MedianAlloc/Op(bytes) | MedianMeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_LargeFile_CompileAndExecute_10k | 2 | 15.021 | 0.653 | N/A | 1624402 | 666511.053 | 162.440 | -25.31 | 8.00 | PASS | N/A |
| VMXQ_LargeFile_CompileAndExecute_50k_Generated | 2 | 89.703 | 6.519 | N/A | 8189460 | 562742.332 | 163.789 | -16.34 | 8.00 | PASS | N/A |

