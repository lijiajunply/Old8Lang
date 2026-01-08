```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.2 (25C56) [Darwin 25.2.0]
Apple M4, 1 CPU, 10 logical and 10 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.0, 10.0.25.52411), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.0 (10.0.0, 10.0.25.52411), Arm64 RyuJIT armv8.0-a


```
| Method                              | Mean        | Error     | StdDev    | Gen0    | Gen1   | Allocated |
|------------------------------------ |------------:|----------:|----------:|--------:|-------:|----------:|
| &#39;Tokenize Simple Code&#39;              |    632.5 ns |  10.97 ns |   9.16 ns |  0.4892 | 0.0029 |      4 KB |
| &#39;Tokenize Medium Code&#39;              |  1,293.1 ns |   6.30 ns |   5.26 ns |  0.8850 | 0.0076 |   7.23 KB |
| &#39;Tokenize Complex Code&#39;             |  3,010.4 ns |   9.36 ns |   8.30 ns |  1.8044 | 0.0420 |  14.76 KB |
| &#39;Tokenize Large File&#39;               | 22,253.3 ns | 373.20 ns | 547.03 ns | 13.6719 | 1.7090 | 112.18 KB |
| &#39;Parse Simple Code&#39;                 |  1,611.1 ns |  31.43 ns |  47.05 ns |  0.9384 | 0.0076 |   7.68 KB |
| &#39;Parse Medium Code&#39;                 |  2,751.1 ns |  11.71 ns |  10.96 ns |  1.6327 | 0.0267 |  13.35 KB |
| &#39;Parse Complex Code&#39;                |  7,562.6 ns |  78.27 ns |  65.36 ns |  4.0665 | 0.1297 |  33.26 KB |
| &#39;Parse Large File&#39;                  |          NA |        NA |        NA |      NA |     NA |        NA |
| &#39;Full Pipeline - Simple Code&#39;       |  1,583.0 ns |  18.43 ns |  20.49 ns |  0.9384 | 0.0076 |   7.68 KB |
| &#39;Full Pipeline - Medium Code&#39;       |  2,717.5 ns |   9.21 ns |   8.16 ns |  1.6327 | 0.0267 |  13.35 KB |
| &#39;Full Pipeline - Complex Code&#39;      |  7,531.4 ns |  26.50 ns |  22.13 ns |  4.0665 | 0.1297 |  33.26 KB |
| &#39;Parse Loop Intensive Code&#39;         |  5,366.4 ns |  30.22 ns |  25.24 ns |  2.7924 | 0.0763 |  22.81 KB |
| &#39;Parse Function Intensive Code&#39;     |  7,851.6 ns |  33.64 ns |  31.46 ns |  4.6234 | 0.1831 |  37.85 KB |
| &#39;Parse Expression Intensive Code&#39;   |  9,675.6 ns |  29.94 ns |  26.54 ns |  5.0201 | 0.2594 |  41.09 KB |
| &#39;Parse Class Intensive Code&#39;        | 18,644.9 ns |  78.86 ns |  65.85 ns | 10.6506 | 0.8545 |  87.02 KB |
| &#39;Multiple Parses - Simple Code&#39;     | 15,511.2 ns |  51.73 ns |  48.39 ns |  9.3994 | 0.0916 |   76.8 KB |
| &#39;Multiple Parses - Different Codes&#39; | 12,120.7 ns |  85.06 ns |  75.40 ns |  6.6376 | 0.1984 |  54.29 KB |
| &#39;Parse Generic Syntax&#39;              |          NA |        NA |        NA |      NA |     NA |        NA |
| &#39;Parse Lambda Expressions&#39;          |          NA |        NA |        NA |      NA |     NA |        NA |
| &#39;Parse LINQ Syntax&#39;                 |  4,286.3 ns |  12.68 ns |  11.24 ns |  2.3956 | 0.0610 |  19.58 KB |
| &#39;Parse Match Expressions&#39;           |          NA |        NA |        NA |      NA |     NA |        NA |~~_~~_

Benchmarks with issues:
  ParserBenchmarkTests.'Parse Large File': DefaultJob
  ParserBenchmarkTests.'Parse Generic Syntax': DefaultJob
  ParserBenchmarkTests.'Parse Lambda Expressions': DefaultJob
  ParserBenchmarkTests.'Parse Match Expressions': DefaultJob
