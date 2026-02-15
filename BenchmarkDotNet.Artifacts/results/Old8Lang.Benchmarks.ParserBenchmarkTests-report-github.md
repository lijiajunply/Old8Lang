```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.7623/25H2/2025Update/HudsonValley2)
AMD Ryzen 7 5800H with Radeon Graphics 3.20GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.103
  [Host]     : .NET 10.0.3 (10.0.3, 10.0.326.7603), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.3 (10.0.3, 10.0.326.7603), X64 RyuJIT x86-64-v3


```
| Method                              | Mean       | Error     | StdDev    | Median     | Gen0    | Gen1   | Allocated |
|------------------------------------ |-----------:|----------:|----------:|-----------:|--------:|-------:|----------:|
| &#39;Tokenize Simple Code&#39;              |   1.256 μs | 0.0848 μs | 0.2447 μs |   1.169 μs |  0.4902 | 0.0019 |   4.01 KB |
| &#39;Tokenize Medium Code&#39;              |   2.407 μs | 0.0478 μs | 0.0491 μs |   2.423 μs |  0.8850 | 0.0076 |   7.26 KB |
| &#39;Tokenize Complex Code&#39;             |   5.527 μs | 0.1073 μs | 0.1538 μs |   5.543 μs |  1.8082 | 0.0305 |  14.82 KB |
| &#39;Tokenize Large File&#39;               |  51.797 μs | 0.9778 μs | 1.1640 μs |  52.151 μs | 15.8081 | 2.3193 | 129.45 KB |
| &#39;Parse Simple Code&#39;                 |   2.864 μs | 0.0536 μs | 0.0596 μs |   2.878 μs |  0.9499 | 0.0076 |   7.79 KB |
| &#39;Parse Medium Code&#39;                 |   5.339 μs | 0.1022 μs | 0.0906 μs |   5.348 μs |  1.6479 | 0.0229 |  13.51 KB |
| &#39;Parse Complex Code&#39;                |  14.008 μs | 0.2741 μs | 0.3367 μs |  14.057 μs |  4.0588 | 0.1526 |   33.2 KB |
| &#39;Parse Large File&#39;                  | 120.662 μs | 2.2375 μs | 2.1975 μs | 121.066 μs | 30.2734 | 7.2021 | 247.48 KB |
| &#39;Full Pipeline - Simple Code&#39;       |   2.981 μs | 0.0596 μs | 0.1381 μs |   2.971 μs |  0.9499 | 0.0076 |   7.79 KB |
| &#39;Full Pipeline - Medium Code&#39;       |   5.291 μs | 0.1022 μs | 0.1329 μs |   5.322 μs |  1.6479 | 0.0229 |  13.51 KB |
| &#39;Full Pipeline - Complex Code&#39;      |  14.353 μs | 0.2622 μs | 0.3316 μs |  14.477 μs |  4.0588 | 0.1526 |   33.2 KB |
| &#39;Parse Loop Intensive Code&#39;         |  10.287 μs | 0.1931 μs | 0.1612 μs |  10.337 μs |  2.8076 | 0.0763 |  22.97 KB |
| &#39;Parse Function Intensive Code&#39;     |  14.393 μs | 0.2872 μs | 0.5395 μs |  14.494 μs |  4.6539 | 0.1526 |  38.09 KB |
| &#39;Parse Expression Intensive Code&#39;   |  17.014 μs | 0.3385 μs | 0.5748 μs |  17.151 μs |  5.1575 | 0.1526 |  42.33 KB |
| &#39;Parse Class Intensive Code&#39;        |  35.375 μs | 0.5121 μs | 0.4540 μs |  35.371 μs | 10.6201 | 0.9766 |  86.77 KB |
| &#39;Multiple Parses - Simple Code&#39;     |  28.825 μs | 0.5518 μs | 0.6776 μs |  28.774 μs |  9.5215 | 0.0916 |  77.89 KB |
| &#39;Multiple Parses - Different Codes&#39; |  22.911 μs | 0.4481 μs | 0.4981 μs |  22.955 μs |  6.6528 | 0.2136 |  54.49 KB |
| &#39;Parse Generic Syntax&#39;              |   9.047 μs | 0.1788 μs | 0.2195 μs |   9.037 μs |  2.8992 | 0.0763 |   23.8 KB |
| &#39;Parse Lambda Expressions&#39;          |  11.186 μs | 0.2115 μs | 0.2351 μs |  11.158 μs |  3.3264 | 0.1068 |  27.23 KB |
| &#39;Parse LINQ Syntax&#39;                 |   8.239 μs | 0.1639 μs | 0.1610 μs |   8.281 μs |  2.4109 | 0.0458 |  19.71 KB |
| &#39;Parse Match Expressions&#39;           |   8.016 μs | 0.1563 μs | 0.1305 μs |   8.034 μs |  2.3651 | 0.0458 |  19.42 KB |
