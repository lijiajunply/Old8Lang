```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.1 (25B78) [Darwin 25.1.0]
Apple M4, 1 CPU, 10 logical and 10 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.0, 10.0.25.52411), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.0 (10.0.0, 10.0.25.52411), Arm64 RyuJIT armv8.0-a


```
| Method                          | Mean | Error |
|-------------------------------- |-----:|------:|
| &#39;Compile Simple Code&#39;           |   NA |    NA |
| &#39;Compile Medium Code&#39;           |   NA |    NA |
| &#39;Compile Complex Code&#39;          |   NA |    NA |
| &#39;Generate IL for Simple Code&#39;   |   NA |    NA |
| &#39;Generate IL for Medium Code&#39;   |   NA |    NA |
| &#39;Generate IL for Complex Code&#39;  |   NA |    NA |
| &#39;Execute Compiled Simple Code&#39;  |   NA |    NA |
| &#39;Execute Compiled Complex Code&#39; |   NA |    NA |
| &#39;Multiple Compilations&#39;         |   NA |    NA |
| &#39;IL Verification Overhead&#39;      |   NA |    NA |
| &#39;No IL Verification&#39;            |   NA |    NA |

Benchmarks with issues:
  CompilerBenchmarkTests.'Compile Simple Code': DefaultJob
  CompilerBenchmarkTests.'Compile Medium Code': DefaultJob
  CompilerBenchmarkTests.'Compile Complex Code': DefaultJob
  CompilerBenchmarkTests.'Generate IL for Simple Code': DefaultJob
  CompilerBenchmarkTests.'Generate IL for Medium Code': DefaultJob
  CompilerBenchmarkTests.'Generate IL for Complex Code': DefaultJob
  CompilerBenchmarkTests.'Execute Compiled Simple Code': DefaultJob
  CompilerBenchmarkTests.'Execute Compiled Complex Code': DefaultJob
  CompilerBenchmarkTests.'Multiple Compilations': DefaultJob
  CompilerBenchmarkTests.'IL Verification Overhead': DefaultJob
  CompilerBenchmarkTests.'No IL Verification': DefaultJob
