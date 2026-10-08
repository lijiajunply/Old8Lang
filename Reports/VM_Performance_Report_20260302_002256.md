# Old8Lang VM Performance Report

- GeneratedAt: 2026-03-02T00:22:56.6728820+08:00
- SourceCsv: `/Users/luckyfish/Documents/Project/Old8LangProjects/Old8Lang/BenchmarkDotNet.Artifacts/results/Old8Lang.Benchmarks.Benchmarks.VM.Suites.VMModePerformanceBenchmarks-report.csv`
- Job: `Job-LGHQEI`
- Runtime: `.NET 10.0`
- WarmupCount: `3`
- IterationCount: `8`

| Scenario | Mean(ms) | StdDev(ms) | P95(ms) | Allocated(bytes) | RawMean | RawAllocated |
|---|---:|---:|---:|---:|---|---|
| VM_ArithmeticLoop | 7.846 | 0.029 | N/A | 3607101 | 7.846 ms | 3.44 MB |
| VM_DenseFunctionCall | 90.648 | 2.967 | N/A | 8399094 | 90.648 ms | 8.01 MB |
| VM_DefaultAndNamedArgs | 31.833 | 1.371 | N/A | 9835643 | 31.833 ms | 9.38 MB |
