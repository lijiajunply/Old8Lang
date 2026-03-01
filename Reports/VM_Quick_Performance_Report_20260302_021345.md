# Old8Lang VM Quick Performance Report

- GeneratedAt: 2026-03-02T02:13:45.0180710+08:00
- SourceCsv: `/Users/luckyfish/Documents/Project/Old8LangProjects/Old8Lang/BenchmarkDotNet.Artifacts/results/Old8Lang.Benchmarks.Benchmarks.VM.Suites.VMQuickPerformanceBenchmarks-report.csv`
- BaselineJson: `/Users/luckyfish/Documents/Project/Old8LangProjects/Old8Lang/Reports/VM_Quick_Performance_Report_20260302_013755.json`

## Concurrency

| Scenario | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_Concurrency_Channel_MPMC_4Workers | 82.997 | 5.755 | N/A | 29437901 | 1204861.375 | 294.379 | 6.27 | 12.00 | PASS | N/A |
| VMXQ_Concurrency_MutexAtomicCounter_4Workers | 132.196 | 4.821 | N/A | 62176737 | 2420646.297 | 194.302 | 2.83 | 12.00 | PASS | N/A |
| VMXQ_Concurrency_Channel_MPMC_4Workers | 95.703 | 1.903 | N/A | 31228979 | 1044899.324 | 312.290 | 22.54 | 12.00 | FAIL | N/A |
| VMXQ_Concurrency_MutexAtomicCounter_4Workers | 124.696 | 2.644 | N/A | 62174351 | 2566241.098 | 194.295 | -3.00 | 12.00 | PASS | N/A |

## Edge

| Scenario | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_Edge_DeepRecursion_NearLimit | 0.140 | 0.001 | N/A | 32963 | 7122507.123 | 32.963 | -5.26 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighArgCount_CallHotPath | 42.052 | 0.295 | N/A | 12963942 | 713400.758 | 432.131 | -2.82 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighThrowRate_TryCatch | 32.782 | 0.310 | N/A | 10083707 | 1220178.085 | 252.093 | -34.37 | 10.00 | PASS | N/A |
| VMXQ_Edge_LargeClosureCapture_HighFreq | 30.802 | 0.815 | N/A | 20004741 | 1623281.756 | 400.095 | 1.75 | 10.00 | PASS | N/A |
| VMXQ_Edge_DeepRecursion_NearLimit | 0.141 | 0.002 | N/A | 32963 | 7102272.727 | 32.963 | -4.99 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighArgCount_CallHotPath | 40.919 | 0.417 | N/A | 12963942 | 733148.580 | 432.131 | -5.44 | 10.00 | PASS | N/A |
| VMXQ_Edge_HighThrowRate_TryCatch | 32.442 | 0.735 | N/A | 10083707 | 1232984.810 | 252.093 | -35.05 | 10.00 | PASS | N/A |
| VMXQ_Edge_LargeClosureCapture_HighFreq | 30.709 | 1.253 | N/A | 20004741 | 1628213.687 | 400.095 | 1.44 | 10.00 | PASS | N/A |

## LargeFile

| Scenario | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXQ_LargeFile_CompileAndExecute_10k | 8.410 | 1.205 | N/A | 2482002 | 1189046.504 | 248.200 | -0.56 | 8.00 | PASS | N/A |
| VMXQ_LargeFile_CompileAndExecute_50k_Generated | 40.111 | 1.230 | N/A | 12523930 | 1246540.849 | 250.479 | -1.36 | 8.00 | PASS | N/A |
| VMXQ_LargeFile_CompileAndExecute_10k | 6.811 | 0.186 | N/A | 2482002 | 1468148.518 | 248.200 | -19.46 | 8.00 | PASS | N/A |
| VMXQ_LargeFile_CompileAndExecute_50k_Generated | 48.857 | 3.975 | N/A | 12523930 | 1023392.711 | 250.479 | 20.14 | 8.00 | FAIL | N/A |

## Warnings
- 检测到执行失败场景（FAIL），请优先查看 FailureReason 与 BenchmarkDotNet 日志。
