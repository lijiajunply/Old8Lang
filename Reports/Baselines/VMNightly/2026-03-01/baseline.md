# Old8Lang VM Nightly Performance Report

- GeneratedAt: 2026-03-02T00:10:49.5184210+08:00
- SourceCsv: `/private/tmp/old8lang-vm-baseline-20260302-run/BenchmarkDotNet.Artifacts/results/Old8Lang.Benchmarks.Benchmarks.VM.Suites.VMNightlyPerformanceBenchmarks-report.csv`
- BaselineJson: `/private/tmp/old8lang-vm-baseline-20260302-run/Reports/VM_Nightly_Performance_Report_20260302_001031.json`

## Concurrency

| Scenario | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | Threshold(%) | Status |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---|
| VMXN_Concurrency_Channel_MPMC_2Workers | N/A | N/A | N/A | N/A | N/A | N/A | N/A | 12.00 | N/A |
| VMXN_Concurrency_Channel_MPMC_4Workers | N/A | N/A | N/A | N/A | N/A | N/A | N/A | 12.00 | N/A |
| VMXN_Concurrency_Channel_MPMC_8Workers | N/A | N/A | N/A | N/A | N/A | N/A | N/A | 12.00 | N/A |
| VMXN_Concurrency_MutexAtomicCounter_2Workers | N/A | N/A | N/A | N/A | N/A | N/A | N/A | 12.00 | N/A |
| VMXN_Concurrency_MutexAtomicCounter_4Workers | N/A | N/A | N/A | N/A | N/A | N/A | N/A | 12.00 | N/A |
| VMXN_Concurrency_MutexAtomicCounter_8Workers | N/A | N/A | N/A | N/A | N/A | N/A | N/A | 12.00 | N/A |
| VMXN_Concurrency_Semaphore_Contention | N/A | N/A | N/A | N/A | N/A | N/A | N/A | 12.00 | N/A |
| VMXN_Concurrency_SpawnJoin_Throughput | N/A | N/A | N/A | N/A | N/A | N/A | N/A | 12.00 | N/A |

## Edge

| Scenario | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | Threshold(%) | Status |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---|
| VMXN_Edge_DeepRecursion_NearLimit | N/A | N/A | N/A | N/A | N/A | N/A | N/A | 10.00 | N/A |
| VMXN_Edge_HighArgCount_CallHotPath | N/A | N/A | N/A | N/A | N/A | N/A | N/A | 10.00 | N/A |
| VMXN_Edge_HighThrowRate_TryCatch | N/A | N/A | N/A | N/A | N/A | N/A | N/A | 10.00 | N/A |
| VMXN_Edge_LargeClosureCapture_HighFreq | N/A | N/A | N/A | N/A | N/A | N/A | N/A | 10.00 | N/A |

## LargeFile

| Scenario | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | Threshold(%) | Status |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---|
| VMXN_LargeFile_CompileAndExecute_10k | N/A | N/A | N/A | N/A | N/A | N/A | N/A | 8.00 | N/A |
| VMXN_LargeFile_CompileAndExecute_50k_Generated | N/A | N/A | N/A | N/A | N/A | N/A | N/A | 8.00 | N/A |
| VMXN_LargeFile_CompileAndExecute_100k_Generated | N/A | N/A | N/A | N/A | N/A | N/A | N/A | 8.00 | N/A |
| VMXN_LargeFile_ModuleStyle_ImportLike | N/A | N/A | N/A | N/A | N/A | N/A | N/A | 8.00 | N/A |

