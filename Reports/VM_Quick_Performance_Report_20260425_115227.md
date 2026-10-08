# Old8Lang VM Quick Performance Report

- GeneratedAt: 2026-04-25T11:52:27.9222243+08:00
- SourceCsv: `C:\Projects\RiderProjects\Old8Lang\BenchmarkDotNet.Artifacts\results\Old8Lang.Benchmarks.Benchmarks.VM.Suites.VMQuickPerformanceBenchmarks-report.csv`
- BaselineJson: `C:\Projects\RiderProjects\Old8Lang\Reports\VM_Quick_Performance_Report_20260425_114538.json`

## Concurrency

### Per-Job Details

| Scenario | Job | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_Concurrency_Channel_MPMC_4Workers | Job-EATLBP | 133.740 | 35.033 | N/A | 35904829 | 747720.015 | 359.048 | 66.40 | 12.00 | FAIL | N/A |
| VMXQ_Concurrency_MutexAtomicCounter_4Workers | Job-EATLBP | 243.004 | 17.761 | N/A | 62184724 | 1316850.751 | 194.327 | 11.70 | 12.00 | WARN | N/A |
| VMXQ_Concurrency_Channel_MPMC_4Workers | Job-LGHQEI | 77.539 | 1.027 | N/A | 35489300 | 1289673.584 | 354.893 | -7.17 | 12.00 | PASS | N/A |
| VMXQ_Concurrency_MutexAtomicCounter_4Workers | Job-LGHQEI | 232.521 | 13.531 | N/A | 62184755 | 1376220.966 | 194.327 | -2.08 | 12.00 | PASS | N/A |

### Scenario Aggregate (Median)

| Scenario | Jobs | MedianMean(ms) | MedianStdDev(ms) | MedianP95(ms) | MedianAllocated(bytes) | MedianThroughput(ops/s) | MedianAlloc/Op(bytes) | MedianMeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_Concurrency_Channel_MPMC_4Workers | 2 | 105.639 | 18.030 | N/A | 35697065 | 1018696.799 | 356.971 | 29.62 | 12.00 | FAIL | N/A |
| VMXQ_Concurrency_MutexAtomicCounter_4Workers | 2 | 237.762 | 15.646 | N/A | 62184740 | 1346535.859 | 194.327 | 4.81 | 12.00 | WARN | N/A |

## Edge

### Per-Job Details

| Scenario | Job | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_Edge_DeepRecursion_NearLimit | Job-EATLBP | 0.300 | 0.005 | N/A | 29932 | 3333333.333 | 29.932 | 3.23 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighArgCount_CallHotPath | Job-EATLBP | 91.570 | 3.188 | N/A | 7204915 | 327619.647 | 240.164 | 5.98 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighThrowRate_TryCatch | Job-EATLBP | 19.869 | 0.514 | N/A | 5044552 | 2013216.768 | 126.114 | -1.63 | 10.00 | PASS | N/A |
| VMXQ_Edge_LargeClosureCapture_HighFreq | Job-EATLBP | 43.884 | 3.044 | N/A | 14805187 | 1139357.038 | 296.104 | -30.32 | 10.00 | PASS | N/A |
| VMXQ_Edge_DeepRecursion_NearLimit | Job-LGHQEI | 0.280 | 0.012 | N/A | 29932 | 3575259.206 | 29.932 | 1.19 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighArgCount_CallHotPath | Job-LGHQEI | 88.118 | 4.352 | N/A | 7204884 | 340452.961 | 240.163 | 5.12 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighThrowRate_TryCatch | Job-LGHQEI | 19.476 | 1.039 | N/A | 5044541 | 2053862.545 | 126.114 | 0.87 | 10.00 | PASS | N/A |
| VMXQ_Edge_LargeClosureCapture_HighFreq | Job-LGHQEI | 43.129 | 1.502 | N/A | 14805176 | 1159326.200 | 296.104 | -29.66 | 10.00 | PASS | N/A |

### Scenario Aggregate (Median)

| Scenario | Jobs | MedianMean(ms) | MedianStdDev(ms) | MedianP95(ms) | MedianAllocated(bytes) | MedianThroughput(ops/s) | MedianAlloc/Op(bytes) | MedianMeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_Edge_DeepRecursion_NearLimit | 2 | 0.290 | 0.009 | N/A | 29932 | 3454296.270 | 29.932 | 2.21 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighArgCount_CallHotPath | 2 | 89.844 | 3.770 | N/A | 7204900 | 334036.304 | 240.163 | 5.55 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighThrowRate_TryCatch | 2 | 19.672 | 0.777 | N/A | 5044547 | 2033539.657 | 126.114 | -0.38 | 10.00 | PASS | N/A |
| VMXQ_Edge_LargeClosureCapture_HighFreq | 2 | 43.506 | 2.273 | N/A | 14805182 | 1149341.619 | 296.104 | -29.99 | 10.00 | PASS | N/A |

## LargeFile

### Per-Job Details

| Scenario | Job | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_LargeFile_CompileAndExecute_10k | Job-EATLBP | 16.961 | 1.163 | N/A | 1624392 | 589577.450 | 162.439 | 10.71 | 8.00 | FAIL | N/A |
| VMXQ_LargeFile_CompileAndExecute_50k_Generated | Job-EATLBP | 88.299 | 1.605 | N/A | 8189440 | 566257.187 | 163.789 | 0.26 | 8.00 | PASS | N/A |
| VMXQ_LargeFile_CompileAndExecute_10k | Job-LGHQEI | 15.380 | 0.478 | N/A | 1624381 | 650216.197 | 162.438 | 5.03 | 8.00 | PASS | N/A |
| VMXQ_LargeFile_CompileAndExecute_50k_Generated | Job-LGHQEI | 87.218 | 5.215 | N/A | 8189440 | 573276.816 | 163.789 | 7.26 | 8.00 | WARN | N/A |

### Scenario Aggregate (Median)

| Scenario | Jobs | MedianMean(ms) | MedianStdDev(ms) | MedianP95(ms) | MedianAllocated(bytes) | MedianThroughput(ops/s) | MedianAlloc/Op(bytes) | MedianMeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_LargeFile_CompileAndExecute_10k | 2 | 16.170 | 0.820 | N/A | 1624387 | 619896.823 | 162.439 | 7.87 | 8.00 | FAIL | N/A |
| VMXQ_LargeFile_CompileAndExecute_50k_Generated | 2 | 87.758 | 3.410 | N/A | 8189440 | 569767.002 | 163.789 | 3.76 | 8.00 | WARN | N/A |

## Warnings
- 检测到执行失败场景（FAIL），请优先查看 FailureReason 与 BenchmarkDotNet 日志。
