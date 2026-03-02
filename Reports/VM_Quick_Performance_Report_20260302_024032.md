# Old8Lang VM Quick Performance Report

- GeneratedAt: 2026-03-02T02:40:32.8995350+08:00
- SourceCsv: `/Users/luckyfish/Documents/Project/Old8LangProjects/Old8Lang/BenchmarkDotNet.Artifacts/results/Old8Lang.Benchmarks.Benchmarks.VM.Suites.VMQuickPerformanceBenchmarks-report.csv`
- BaselineJson: `/Users/luckyfish/Documents/Project/Old8LangProjects/Old8Lang/Reports/VM_Quick_Performance_Report_20260302_023424.json`

## Concurrency

### Per-Job Details

| Scenario | Job | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_Concurrency_Channel_MPMC_4Workers | Job-EATLBP | 67.266 | 0.500 | N/A | 29642650 | 1486626.310 | 296.426 | -6.93 | 12.00 | PASS | N/A |
| VMXQ_Concurrency_MutexAtomicCounter_4Workers | Job-EATLBP | 113.994 | 0.625 | N/A | 62181775 | 2807165.289 | 194.318 | -0.73 | 12.00 | PASS | N/A |
| VMXQ_Concurrency_Channel_MPMC_4Workers | Job-LGHQEI | 63.530 | 1.588 | N/A | 29462036 | 1574061.977 | 294.620 | -14.39 | 12.00 | PASS | N/A |
| VMXQ_Concurrency_MutexAtomicCounter_4Workers | Job-LGHQEI | 124.511 | 5.943 | N/A | 62173921 | 2570058.180 | 194.294 | -3.46 | 12.00 | PASS | N/A |

### Scenario Aggregate (Median)

| Scenario | Jobs | MedianMean(ms) | MedianStdDev(ms) | MedianP95(ms) | MedianAllocated(bytes) | MedianThroughput(ops/s) | MedianAlloc/Op(bytes) | MedianMeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_Concurrency_Channel_MPMC_4Workers | 2 | 65.398 | 1.044 | N/A | 29552343 | 1530344.143 | 295.523 | -10.66 | 12.00 | PASS | N/A |
| VMXQ_Concurrency_MutexAtomicCounter_4Workers | 2 | 119.252 | 3.284 | N/A | 62177848 | 2688611.735 | 194.306 | -2.10 | 12.00 | PASS | N/A |

## Edge

### Per-Job Details

| Scenario | Job | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_Edge_DeepRecursion_NearLimit | Job-EATLBP | 0.146 | 0.008 | N/A | 29092 | 6849315.068 | 29.092 | -0.07 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighArgCount_CallHotPath | Job-EATLBP | 40.049 | 0.291 | N/A | 10323948 | 749086.115 | 344.132 | -1.57 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighThrowRate_TryCatch | Job-EATLBP | 34.412 | 1.721 | N/A | 10083707 | 1162398.726 | 252.093 | 10.22 | 10.00 | FAIL | N/A |
| VMXQ_Edge_LargeClosureCapture_HighFreq | Job-EATLBP | 30.199 | 1.225 | N/A | 20007148 | 1655667.515 | 400.143 | 8.13 | 10.00 | WARN | N/A |
| VMXQ_Edge_DeepRecursion_NearLimit | Job-LGHQEI | 0.150 | 0.007 | N/A | 29092 | 6653359.947 | 29.092 | 5.03 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighArgCount_CallHotPath | Job-LGHQEI | 41.023 | 0.678 | N/A | 10323948 | 731298.860 | 344.132 | -10.79 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighThrowRate_TryCatch | Job-LGHQEI | 33.950 | 1.835 | N/A | 10083707 | 1178185.888 | 252.093 | 4.83 | 10.00 | PASS | N/A |
| VMXQ_Edge_LargeClosureCapture_HighFreq | Job-LGHQEI | 28.053 | 0.280 | N/A | 20007148 | 1782315.156 | 400.143 | -1.91 | 10.00 | PASS | N/A |

### Scenario Aggregate (Median)

| Scenario | Jobs | MedianMean(ms) | MedianStdDev(ms) | MedianP95(ms) | MedianAllocated(bytes) | MedianThroughput(ops/s) | MedianAlloc/Op(bytes) | MedianMeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_Edge_DeepRecursion_NearLimit | 2 | 0.148 | 0.008 | N/A | 29092 | 6751337.508 | 29.092 | 2.48 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighArgCount_CallHotPath | 2 | 40.536 | 0.484 | N/A | 10323948 | 740192.487 | 344.132 | -6.18 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighThrowRate_TryCatch | 2 | 34.181 | 1.778 | N/A | 10083707 | 1170292.307 | 252.093 | 7.52 | 10.00 | FAIL | N/A |
| VMXQ_Edge_LargeClosureCapture_HighFreq | 2 | 29.126 | 0.752 | N/A | 20007148 | 1718991.336 | 400.143 | 3.11 | 10.00 | WARN | N/A |

## LargeFile

### Per-Job Details

| Scenario | Job | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_LargeFile_CompileAndExecute_10k | Job-EATLBP | 8.156 | 0.768 | N/A | 2282967 | 1226046.124 | 228.297 | 17.28 | 8.00 | FAIL | N/A |
| VMXQ_LargeFile_CompileAndExecute_50k_Generated | Job-EATLBP | 37.475 | 0.741 | N/A | 11524659 | 1334226.376 | 230.493 | 1.88 | 8.00 | PASS | N/A |
| VMXQ_LargeFile_CompileAndExecute_10k | Job-LGHQEI | 6.470 | 0.039 | N/A | 2282967 | 1545523.391 | 228.297 | -4.98 | 8.00 | PASS | N/A |
| VMXQ_LargeFile_CompileAndExecute_50k_Generated | Job-LGHQEI | 36.163 | 0.146 | N/A | 11524659 | 1382628.654 | 230.493 | -5.84 | 8.00 | PASS | N/A |

### Scenario Aggregate (Median)

| Scenario | Jobs | MedianMean(ms) | MedianStdDev(ms) | MedianP95(ms) | MedianAllocated(bytes) | MedianThroughput(ops/s) | MedianAlloc/Op(bytes) | MedianMeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_LargeFile_CompileAndExecute_10k | 2 | 7.313 | 0.403 | N/A | 2282967 | 1385784.758 | 228.297 | 6.15 | 8.00 | FAIL | N/A |
| VMXQ_LargeFile_CompileAndExecute_50k_Generated | 2 | 36.819 | 0.443 | N/A | 11524659 | 1358427.515 | 230.493 | -1.98 | 8.00 | PASS | N/A |

## Warnings
- 检测到执行失败场景（FAIL），请优先查看 FailureReason 与 BenchmarkDotNet 日志。
