# Old8Lang VM Quick Performance Report

- GeneratedAt: 2026-03-02T01:37:55.6133670+08:00
- SourceCsv: `/Users/luckyfish/Documents/Project/Old8LangProjects/Old8Lang/BenchmarkDotNet.Artifacts/results/Old8Lang.Benchmarks.Benchmarks.VM.Suites.VMQuickPerformanceBenchmarks-report.csv`
- BaselineJson: `/Users/luckyfish/Documents/Project/Old8LangProjects/Old8Lang/Reports/VM_Quick_Performance_Report_20260302_012717.json`

## Concurrency

| Scenario | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_Concurrency_Channel_MPMC_4Workers | 80.216 | 1.054 | N/A | 29589821 | 1246635.642 | 295.898 | 3.84 | 12.00 | PASS | N/A |
| VMXQ_Concurrency_MutexAtomicCounter_4Workers | 192.404 | 35.247 | N/A | 62180260 | 1663167.086 | 194.313 | -6.12 | 12.00 | PASS | N/A |
| VMXQ_Concurrency_Channel_MPMC_4Workers | 78.100 | 1.680 | N/A | 30503301 | 1280413.010 | 305.033 | 1.10 | 12.00 | PASS | N/A |
| VMXQ_Concurrency_MutexAtomicCounter_4Workers | 128.552 | 1.271 | N/A | 62176737 | 2489261.172 | 194.302 | -37.27 | 12.00 | PASS | N/A |

## Edge

| Scenario | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_Edge_DeepRecursion_NearLimit | 0.144 | 0.001 | N/A | 32963 | 6958942.241 | 32.963 | 0.28 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighArgCount_CallHotPath | 42.382 | 0.472 | N/A | 12963942 | 707849.341 | 432.131 | -12.50 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighThrowRate_TryCatch | 33.982 | 0.105 | N/A | 10083707 | 1177100.683 | 252.093 | 0.40 | 10.00 | PASS | N/A |
| VMXQ_Edge_LargeClosureCapture_HighFreq | 31.810 | 1.327 | N/A | 20004741 | 1571827.816 | 400.095 | 5.70 | 10.00 | PASS | N/A |
| VMXQ_Edge_DeepRecursion_NearLimit | 0.148 | 0.004 | N/A | 32963 | 6747638.327 | 32.963 | 3.42 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighArgCount_CallHotPath | 43.273 | 1.140 | N/A | 12963942 | 693268.135 | 432.131 | -10.66 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighThrowRate_TryCatch | 49.950 | 14.601 | N/A | 10083707 | 800804.007 | 252.093 | 47.57 | 10.00 | FAIL | N/A |
| VMXQ_Edge_LargeClosureCapture_HighFreq | 30.273 | 1.068 | N/A | 20004741 | 1651625.860 | 400.095 | 0.59 | 10.00 | PASS | N/A |

## LargeFile

| Scenario | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_LargeFile_CompileAndExecute_10k | 7.078 | 0.009 | N/A | 2482002 | 1412748.644 | 248.200 | -4.76 | 8.00 | PASS | N/A |
| VMXQ_LargeFile_CompileAndExecute_50k_Generated | 39.696 | 0.466 | N/A | 12523930 | 1259588.618 | 250.479 | 0.05 | 8.00 | PASS | N/A |
| VMXQ_LargeFile_CompileAndExecute_10k | 8.457 | 0.717 | N/A | 2482002 | 1182438.425 | 248.200 | 13.79 | 8.00 | FAIL | N/A |
| VMXQ_LargeFile_CompileAndExecute_50k_Generated | 40.666 | 0.369 | N/A | 12523930 | 1229537.423 | 250.479 | 2.49 | 8.00 | PASS | N/A |

## Warnings
- 检测到执行失败场景（FAIL），请优先查看 FailureReason 与 BenchmarkDotNet 日志。
