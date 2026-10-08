# Old8Lang VM Hotspot Diagnostic Report

- GeneratedAt: 2026-04-25T12:21:52.3260089+08:00
- Runtime: `10.0.7`
- IterationsPerScenario: `6`

| Scenario | Phase | Median(ms) | MedianAllocated(bytes) | Gen0 | Gen1 | Gen2 |
|---|---|---:|---:|---:|---:|---:|
| LargeFile_10k | CompileToBytecode | 403.982 | 39459856 | 18 | 6 | 0 |
| LargeFile_10k | VMExecute | 59.871 | 1624456 | 0 | 0 | 0 |
| LargeFile_50k_Generated | CompileToBytecode | 3143.120 | 212825132 | 115 | 67 | 13 |
| LargeFile_50k_Generated | VMExecute | 202.946 | 8189488 | 0 | 0 | 0 |
| LargeClosureCapture_HighFreq | CompileToBytecode | 0.334 | 62208 | 0 | 0 | 0 |
| LargeClosureCapture_HighFreq | VMExecute | 282.198 | 14805032 | 6 | 0 | 0 |
| ChannelTryReceive_Timeout0 | CompileToBytecode | 0.465 | 81392 | 0 | 0 | 0 |
| ChannelTryReceive_Timeout0 | VMExecute | 113.261 | 6834664 | 6 | 0 | 0 |
| ChannelTryReceive_Timeout10 | CompileToBytecode | 0.548 | 81504 | 0 | 0 | 0 |
| ChannelTryReceive_Timeout10 | VMExecute | 118.517 | 6833440 | 6 | 0 | 0 |
| SpawnJoin_10k | CompileToBytecode | 0.250 | 46184 | 0 | 0 | 0 |
| SpawnJoin_10k | VMExecute | 2095.998 | 31617012 | 131 | 123 | 0 |
| Await_10k | CompileToBytecode | 0.223 | 43296 | 0 | 0 | 0 |
| Await_10k | VMExecute | 160.987 | 4658684 | 12 | 6 | 0 |
