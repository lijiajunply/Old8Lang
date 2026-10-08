# Old8Lang VM Hotspot Diagnostic Report

- GeneratedAt: 2026-04-25T11:41:21.4991588+08:00
- Runtime: `10.0.7`
- IterationsPerScenario: `6`

| Scenario | Phase | Median(ms) | MedianAllocated(bytes) | Gen0 | Gen1 | Gen2 |
|---|---|---:|---:|---:|---:|---:|
| LargeFile_10k | CompileToBytecode | 491.522 | 38077136 | 18 | 6 | 0 |
| LargeFile_10k | VMExecute | 170.958 | 1624456 | 0 | 0 | 0 |
| LargeFile_50k_Generated | CompileToBytecode | 1949.040 | 200562988 | 100 | 58 | 10 |
| LargeFile_50k_Generated | VMExecute | 109.804 | 8189488 | 0 | 0 | 0 |
| LargeClosureCapture_HighFreq | CompileToBytecode | 0.217 | 62360 | 0 | 0 | 0 |
| LargeClosureCapture_HighFreq | VMExecute | 73.184 | 14805184 | 6 | 0 | 0 |
