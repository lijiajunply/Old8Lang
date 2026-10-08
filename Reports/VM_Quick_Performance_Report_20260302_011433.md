# Old8Lang VM Quick Performance Report

- GeneratedAt: 2026-03-02T01:14:33.2949530+08:00
- SourceCsv: `/Users/luckyfish/Documents/Project/Old8LangProjects/Old8Lang/BenchmarkDotNet.Artifacts/results/Old8Lang.Benchmarks.Benchmarks.VM.Suites.VMQuickPerformanceBenchmarks-report.csv`
- BaselineJson: `/Users/luckyfish/Documents/Project/Old8LangProjects/Old8Lang/Reports/VM_Quick_Performance_Report_20260302_010825.json`

## Concurrency

| Scenario | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_Concurrency_Channel_MPMC_4Workers | 77.675 | 1.211 | N/A | 28843694 | 1287412.198 | 288.437 | 5.30 | 12.00 | PASS | N/A |
| VMXQ_Concurrency_MutexAtomicCounter_4Workers | 146.315 | 13.171 | N/A | 62174659 | 2187060.666 | 194.296 | -41.69 | 12.00 | PASS | N/A |
| VMXQ_Concurrency_Channel_MPMC_4Workers | 78.093 | 1.155 | N/A | 29963295 | 1280529.422 | 299.633 | 5.87 | 12.00 | PASS | N/A |
| VMXQ_Concurrency_MutexAtomicCounter_4Workers | 128.296 | 4.138 | N/A | 62171955 | 2494230.144 | 194.287 | -48.87 | 12.00 | PASS | N/A |

## Edge

| Scenario | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_Edge_DeepRecursion_NearLimit | 0.144 | 0.001 | N/A | 39782 | 6920415.225 | 39.782 | -4.37 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighArgCount_CallHotPath | 44.217 | 1.517 | N/A | 16323994 | 678469.012 | 544.133 | 0.33 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighThrowRate_TryCatch | 36.198 | 1.649 | N/A | 10083758 | 1105039.533 | 252.094 | 3.51 | 10.00 | PASS | N/A |
| VMXQ_Edge_LargeClosureCapture_HighFreq | 33.498 | 0.666 | N/A | 26005299 | 1492626.425 | 520.106 | 6.64 | 10.00 | PASS | N/A |
| VMXQ_Edge_DeepRecursion_NearLimit | 0.161 | 0.013 | N/A | 39782 | 6199628.022 | 39.782 | 6.75 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighArgCount_CallHotPath | 45.342 | 1.316 | N/A | 16323994 | 661644.053 | 544.133 | 2.88 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighThrowRate_TryCatch | 35.185 | 1.533 | N/A | 10083758 | 1136848.089 | 252.094 | 0.61 | 10.00 | PASS | N/A |
| VMXQ_Edge_LargeClosureCapture_HighFreq | 31.753 | 2.485 | N/A | 26005299 | 1574659.322 | 520.106 | 1.08 | 10.00 | PASS | N/A |

## LargeFile

| Scenario | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_LargeFile_CompileAndExecute_10k | 7.316 | 0.059 | N/A | 2800527 | 1366867.141 | 280.053 | -3.84 | 8.00 | PASS | N/A |
| VMXQ_LargeFile_CompileAndExecute_50k_Generated | 41.976 | 1.230 | N/A | 14122844 | 1191151.176 | 282.457 | -6.29 | 8.00 | PASS | N/A |
| VMXQ_LargeFile_CompileAndExecute_10k | 9.138 | 2.182 | N/A | 2800527 | 1094283.463 | 280.053 | 20.12 | 8.00 | FAIL | N/A |
| VMXQ_LargeFile_CompileAndExecute_50k_Generated | 43.733 | 1.184 | N/A | 14122844 | 1143314.469 | 282.457 | -2.37 | 8.00 | PASS | N/A |

## Warnings
- 检测到执行失败场景（FAIL），请优先查看 FailureReason 与 BenchmarkDotNet 日志。
