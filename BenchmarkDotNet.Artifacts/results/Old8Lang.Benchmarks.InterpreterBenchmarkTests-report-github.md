```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.1 (25B78) [Darwin 25.1.0]
Apple M4, 1 CPU, 10 logical and 10 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.0, 10.0.25.52411), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.0 (10.0.0, 10.0.25.52411), Arm64 RyuJIT armv8.0-a


```
| Method                         | Mean         | Error     | StdDev    | Median       |
|------------------------------- |-------------:|----------:|----------:|-------------:|
| &#39;Loop Intensive Code&#39;          |     65.46 μs |  0.511 μs |  0.453 μs |     65.49 μs |
| &#39;Function Call Intensive Code&#39; |    848.12 μs |  2.574 μs |  2.150 μs |    848.01 μs |
| &#39;Mixed Intensive Code&#39;         | 14,913.19 μs | 48.856 μs | 45.700 μs | 14,913.08 μs |
| &#39;Multiple Executions&#39;          |    657.53 μs |  3.136 μs |  2.934 μs |    656.36 μs |
| &#39;Deep Recursion&#39;               |           NA |        NA |        NA |           NA |
| &#39;While Loop Performance&#39;       |  1,417.88 μs | 26.589 μs | 34.573 μs |  1,397.53 μs |

Benchmarks with issues:
  InterpreterBenchmarkTests.'Deep Recursion': DefaultJob
