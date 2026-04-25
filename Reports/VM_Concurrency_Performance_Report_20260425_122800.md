# Old8Lang VM Concurrency Performance Report

- GeneratedAt: 2026-04-25T12:28:00.9077066+08:00
- SourceCsv: `C:\Projects\RiderProjects\Old8Lang\BenchmarkDotNet.Artifacts\results\Old8Lang.Benchmarks.Benchmarks.VM.Suites.VMConcurrencyPerformanceBenchmarks-report.csv`
- BaselineJson: `C:\Projects\RiderProjects\Old8Lang\Reports\VM_Concurrency_Performance_Report_20260425_122528.json`

## Allocation

### Per-Job Details

| Scenario | Job | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXC_Allocation_Channel_SPSC_NoTimeout | Job-LGHQEI | 120.512 | 4.986 | N/A | 32568771 | 995751.460 | 271.406 | 13.53 | 5.00 | FAIL | N/A |

### Scenario Aggregate (Median)

| Scenario | Jobs | MedianMean(ms) | MedianStdDev(ms) | MedianP95(ms) | MedianAllocated(bytes) | MedianThroughput(ops/s) | MedianAlloc/Op(bytes) | MedianMeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXC_Allocation_Channel_SPSC_NoTimeout | 1 | 120.512 | 4.986 | N/A | 32568771 | 995751.460 | 271.406 | 13.53 | 5.00 | FAIL | N/A |

## Latency

### Per-Job Details

| Scenario | Job | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXC_Latency_SpawnJoin_ColdStart | Job-LGHQEI | 173.480 | 8.764 | N/A | 15340667 | 5764.353 | 15340.667 | -3.50 | 8.00 | PASS | N/A |
| VMXC_Latency_Task_NewTask_Await | Job-LGHQEI | 6.941 | 0.724 | N/A | 2306867 | 144071.459 | 2306.867 | N/A | 8.00 | N/A | N/A |

### Scenario Aggregate (Median)

| Scenario | Jobs | MedianMean(ms) | MedianStdDev(ms) | MedianP95(ms) | MedianAllocated(bytes) | MedianThroughput(ops/s) | MedianAlloc/Op(bytes) | MedianMeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXC_Latency_SpawnJoin_ColdStart | 1 | 173.480 | 8.764 | N/A | 15340667 | 5764.353 | 15340.667 | -3.50 | 8.00 | PASS | N/A |
| VMXC_Latency_Task_NewTask_Await | 1 | 6.941 | 0.724 | N/A | 2306867 | 144071.459 | 2306.867 | N/A | 8.00 | N/A | N/A |

## Throughput

### Per-Job Details

| Scenario | Job | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | Throughput(ops/s) | Alloc/Op(bytes) | MeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXC_Throughput_SpawnJoin_HotLoop | Job-LGHQEI | 2661.529 | 366.637 | N/A | 155315077 | 3757.239 | 15531.508 | 2.03 | 8.00 | PASS | N/A |
| VMXC_Throughput_Async_FanOutFanIn | Job-LGHQEI | 14.926 | 1.579 | N/A | 5127537 | 133994.372 | 2563.769 | -11.58 | 8.00 | PASS | N/A |
| VMXC_Throughput_Channel_MPMC_WithTimeout | Job-LGHQEI | 113.704 | 13.262 | N/A | 37885051 | 1055371.843 | 315.709 | 13.94 | 8.00 | FAIL | N/A |

### Scenario Aggregate (Median)

| Scenario | Jobs | MedianMean(ms) | MedianStdDev(ms) | MedianP95(ms) | MedianAllocated(bytes) | MedianThroughput(ops/s) | MedianAlloc/Op(bytes) | MedianMeanΔ(%) | Threshold(%) | Status | FailureReason |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|
| VMXC_Throughput_Async_FanOutFanIn | 1 | 14.926 | 1.579 | N/A | 5127537 | 133994.372 | 2563.769 | -11.58 | 8.00 | PASS | N/A |
| VMXC_Throughput_Channel_MPMC_WithTimeout | 1 | 113.704 | 13.262 | N/A | 37885051 | 1055371.843 | 315.709 | 13.94 | 8.00 | FAIL | N/A |
| VMXC_Throughput_SpawnJoin_HotLoop | 1 | 2661.529 | 366.637 | N/A | 155315077 | 3757.239 | 15531.508 | 2.03 | 8.00 | PASS | N/A |

## Warnings
- 检测到执行失败场景（FAIL），请优先查看 FailureReason 与 BenchmarkDotNet 日志。
