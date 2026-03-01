# Old8Lang VM Quick Performance Report

- GeneratedAt: 2026-03-02T01:27:17.2688380+08:00
- SourceCsv: `/Users/luckyfish/Documents/Project/Old8LangProjects/Old8Lang/BenchmarkDotNet.Artifacts/results/Old8Lang.Benchmarks.Benchmarks.VM.Suites.VMQuickPerformanceBenchmarks-report.csv`
- BaselineJson: `/Users/luckyfish/Documents/Project/Old8LangProjects/Old8Lang/Reports/VM_Quick_Performance_Report_20260302_011433.json`

## Concurrency

| Scenario | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_Concurrency_Channel_MPMC_4Workers | 74.277 | 4.346 | N/A | 29047296 | 1346308.154 | 290.473 | -4.89 | 12.00 | PASS | N/A |
| VMXQ_Concurrency_MutexAtomicCounter_4Workers | 128.411 | 0.102 | N/A | 62171955 | 2491996.408 | 194.287 | 0.09 | 12.00 | PASS | N/A |
| VMXQ_Concurrency_Channel_MPMC_4Workers | 77.253 | 2.055 | N/A | 29855017 | 1294454.814 | 298.550 | -1.08 | 12.00 | PASS | N/A |
| VMXQ_Concurrency_MutexAtomicCounter_4Workers | 204.945 | 27.407 | N/A | 62179994 | 1561395.282 | 194.312 | 59.74 | 12.00 | FAIL | N/A |

## Edge

| Scenario | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_Edge_DeepRecursion_NearLimit | 0.160 | 0.011 | N/A | 39782 | 6242197.253 | 39.782 | -0.68 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighArgCount_CallHotPath | 44.846 | 2.126 | N/A | 16323994 | 668957.474 | 544.133 | -1.09 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighThrowRate_TryCatch | 35.695 | 3.475 | N/A | 10083758 | 1120617.684 | 252.094 | 1.45 | 10.00 | PASS | N/A |
| VMXQ_Edge_LargeClosureCapture_HighFreq | 31.308 | 0.571 | N/A | 26004787 | 1597035.901 | 520.096 | -1.40 | 10.00 | PASS | N/A |
| VMXQ_Edge_DeepRecursion_NearLimit | 0.143 | 0.003 | N/A | 39782 | 6978367.062 | 39.782 | -11.16 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighArgCount_CallHotPath | 48.434 | 3.905 | N/A | 16323994 | 619393.201 | 544.133 | 6.82 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighThrowRate_TryCatch | 33.847 | 0.943 | N/A | 10083758 | 1181778.163 | 252.094 | -3.80 | 10.00 | PASS | N/A |
| VMXQ_Edge_LargeClosureCapture_HighFreq | 30.096 | 1.198 | N/A | 26004787 | 1661355.866 | 520.096 | -5.22 | 10.00 | PASS | N/A |

## LargeFile

| Scenario | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_LargeFile_CompileAndExecute_10k | 7.009 | 0.222 | N/A | 2800527 | 1426818.480 | 280.053 | -23.31 | 8.00 | PASS | N/A |
| VMXQ_LargeFile_CompileAndExecute_50k_Generated | 44.713 | 4.491 | N/A | 14122844 | 1118240.516 | 282.457 | 2.24 | 8.00 | PASS | N/A |
| VMXQ_LargeFile_CompileAndExecute_10k | 7.432 | 0.366 | N/A | 2800527 | 1345460.417 | 280.053 | -18.67 | 8.00 | PASS | N/A |
| VMXQ_LargeFile_CompileAndExecute_50k_Generated | 39.676 | 0.349 | N/A | 14122844 | 1260191.801 | 282.457 | -9.27 | 8.00 | PASS | N/A |

## Warnings
- 检测到执行失败场景（FAIL），请优先查看 FailureReason 与 BenchmarkDotNet 日志。
