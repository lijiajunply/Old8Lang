# Old8Lang VM Hotspot Diagnostic Report

- GeneratedAt: 2026-04-25T11:26:34.3248765+08:00
- Runtime: `10.0.7`
- IterationsPerScenario: `6`

| Scenario | Phase | Median(ms) | MedianAllocated(bytes) | Gen0 | Gen1 | Gen2 |
|---|---|---:|---:|---:|---:|---:|
| LargeFile_10k | CompileToBytecode | 191.098 | 37546320 | 18 | 6 | 0 |
| LargeFile_10k | VMExecute | 42.114 | 2432056 | 0 | 0 | 0 |
| LargeFile_50k_Generated | CompileToBytecode | 1964.393 | 200563020 | 100 | 58 | 10 |
| LargeFile_50k_Generated | VMExecute | 102.154 | 11680016 | 0 | 0 | 0 |
| LargeClosureCapture_HighFreq | CompileToBytecode | 0.291 | 62360 | 0 | 0 | 0 |
| LargeClosureCapture_HighFreq | VMExecute | 79.374 | 14806784 | 6 | 0 | 0 |
