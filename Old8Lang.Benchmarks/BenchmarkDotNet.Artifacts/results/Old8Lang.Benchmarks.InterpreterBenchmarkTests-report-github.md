```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.1 (25B78) [Darwin 25.1.0]
Apple M4, 1 CPU, 10 logical and 10 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.0, 10.0.25.52411), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.0 (10.0.0, 10.0.25.52411), Arm64 RyuJIT armv8.0-a


```
| Method                         | Mean         | Error     | StdDev    |
|------------------------------- |-------------:|----------:|----------:|
| &#39;Loop Intensive Code&#39;          |     58.73 μs |  0.266 μs |  0.222 μs |
| &#39;Function Call Intensive Code&#39; |    738.42 μs |  2.570 μs |  2.146 μs |
| &#39;Mixed Intensive Code&#39;         | 13,545.34 μs | 61.217 μs | 51.119 μs |
| &#39;Multiple Executions&#39;          |    584.67 μs |  2.483 μs |  2.201 μs |
| &#39;Deep Recursion&#39;               |           NA |        NA |        NA |
| &#39;While Loop Performance&#39;       |  1,214.68 μs |  3.615 μs |  3.381 μs |

Benchmarks with issues:
  InterpreterBenchmarkTests.'Deep Recursion': DefaultJob
