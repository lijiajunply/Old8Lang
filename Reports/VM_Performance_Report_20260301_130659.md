# Old8Lang VM Performance Report

- GeneratedAt: 2026-03-01T13:06:59.6420980+08:00
- SourceCsv: `/Users/luckyfish/Documents/Project/Old8LangProjects/Old8Lang/BenchmarkDotNet.Artifacts/results/Old8Lang.Benchmarks.VMModePerformanceBenchmarks-report.csv`
- Job: `Job-LGHQEI`
- Runtime: `.NET 10.0`
- WarmupCount: `3`
- IterationCount: `8`

| Scenario | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | RawMean | RawAllocated |
|---|---:|---:|---:|---:|---|---|
| VM_ArithmeticLoop | 7.711 | 0.063 | N/A | 3607101 | 7.711 ms | 3.44 MB |
| VM_DenseFunctionCall | 109.525 | 15.910 | N/A | 8399094 | 109.525 ms | 8.01 MB |
| VM_DefaultAndNamedArgs | 33.240 | 0.565 | N/A | 9835643 | 33.240 ms | 9.38 MB |
