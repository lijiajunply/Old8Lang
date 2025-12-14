```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.1 (25B78) [Darwin 25.1.0]
Apple M4, 1 CPU, 10 logical and 10 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.0, 10.0.25.52411), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.0 (10.0.0, 10.0.25.52411), Arm64 RyuJIT armv8.0-a


```
| Method                         | Mean         | Error     | StdDev    | Median       |
|------------------------------- |-------------:|----------:|----------:|-------------:|
| &#39;Loop Intensive Code&#39;          |     59.42 μs |  1.103 μs |  0.861 μs |     59.15 μs |
| &#39;Function Call Intensive Code&#39; |    783.25 μs | 16.560 μs | 45.888 μs |    768.12 μs |
| &#39;Mixed Intensive Code&#39;         | 13,768.17 μs | 63.173 μs | 52.753 μs | 13,750.17 μs |
| &#39;Multiple Executions&#39;          |    592.02 μs |  3.858 μs |  3.420 μs |    591.64 μs |
| &#39;Deep Recursion&#39;               |           NA |        NA |        NA |           NA |
| &#39;While Loop Performance&#39;       |  1,178.87 μs |  2.601 μs |  2.306 μs |  1,178.19 μs |

Benchmarks with issues:
  InterpreterBenchmarkTests.'Deep Recursion': DefaultJob
