# Old8Lang VM Performance Report

- GeneratedAt: 2026-03-01T12:28:11.8145080+08:00
- SourceCsv: `/Users/luckyfish/Documents/Project/Old8LangProjects/Old8Lang/BenchmarkDotNet.Artifacts/results/Old8Lang.Benchmarks.VMModePerformanceBenchmarks-report.csv`
- Job: `Job-LGHQEI`
- Runtime: `.NET 10.0`
- WarmupCount: `3`
- IterationCount: `8`

| Scenario | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | RawMean | RawAllocated |
|---|---:|---:|---:|---:|---|---|
| VM_ArithmeticLoop | 7.920 | 0.223 | N/A | 3607101 | 7.920 ms | 3.44 MB |
| VM_DenseFunctionCall | 83.919 | 0.354 | N/A | 8399094 | 83.919 ms | 8.01 MB |
| VM_DefaultAndNamedArgs | 31.480 | 0.144 | N/A | 9835643 | 31.480 ms | 9.38 MB |
