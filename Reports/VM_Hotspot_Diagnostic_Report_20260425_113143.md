# Old8Lang VM Hotspot Diagnostic Report

- GeneratedAt: 2026-04-25T11:31:43.9147331+08:00
- Runtime: `10.0.7`
- IterationsPerScenario: `6`

| Scenario | Phase | Median(ms) | MedianAllocated(bytes) | Gen0 | Gen1 | Gen2 |
|---|---|---:|---:|---:|---:|---:|
| LargeFile_10k | CompileToBytecode | 212.504 | 37533096 | 18 | 6 | 0 |
| LargeFile_10k | VMExecute | 40.886 | 1624472 | 0 | 0 | 0 |
| LargeFile_50k_Generated | CompileToBytecode | 1963.963 | 200564752 | 99 | 57 | 9 |
| LargeFile_50k_Generated | VMExecute | 97.996 | 8189504 | 0 | 0 | 0 |
| LargeClosureCapture_HighFreq | CompileToBytecode | 0.251 | 62360 | 0 | 0 | 0 |
| LargeClosureCapture_HighFreq | VMExecute | 77.399 | 14805456 | 6 | 0 | 0 |
