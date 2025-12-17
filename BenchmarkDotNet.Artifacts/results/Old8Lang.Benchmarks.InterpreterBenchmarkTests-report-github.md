```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.1 (25B78) [Darwin 25.1.0]
Apple M4, 1 CPU, 10 logical and 10 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.0, 10.0.25.52411), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.0 (10.0.0, 10.0.25.52411), Arm64 RyuJIT armv8.0-a


```
| Method                         | Mean         | Error      | StdDev     | Median       |
|------------------------------- |-------------:|-----------:|-----------:|-------------:|
| &#39;Loop Intensive Code&#39;          |     67.68 μs |   0.419 μs |   0.327 μs |     67.69 μs |
| &#39;Function Call Intensive Code&#39; |  1,194.91 μs |  22.498 μs |  41.701 μs |  1,188.15 μs |
| &#39;Mixed Intensive Code&#39;         | 19,736.49 μs | 372.395 μs | 413.916 μs | 19,619.80 μs |
| &#39;Multiple Executions&#39;          |    702.63 μs |  13.068 μs |  11.584 μs |    698.72 μs |
| &#39;Deep Recursion&#39;               |    593.91 μs |  11.813 μs |  26.176 μs |    583.98 μs |
| &#39;While Loop Performance&#39;       |  1,616.55 μs |   9.457 μs |   8.383 μs |  1,613.90 μs |
