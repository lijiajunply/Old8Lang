```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.7840/25H2/2025Update/HudsonValley2)
AMD Ryzen 7 5800H with Radeon Graphics 3.20GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.103
  [Host]     : .NET 10.0.3 (10.0.3, 10.0.326.7603), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.3 (10.0.3, 10.0.326.7603), X64 RyuJIT x86-64-v3


```
| Method                                         | Mean           | Error         | StdDev        | Median         | Gen0      | Gen1      | Gen2     | Allocated  |
|----------------------------------------------- |---------------:|--------------:|--------------:|---------------:|----------:|----------:|---------:|-----------:|
| &#39;Tokenize Simple Code&#39;                         |       920.4 ns |      18.32 ns |      50.75 ns |       918.5 ns |    0.2899 |         - |        - |    2.38 KB |
| &#39;Tokenize Medium Code&#39;                         |     2,125.3 ns |      70.17 ns |     204.69 ns |     2,125.6 ns |    0.5951 |    0.0038 |        - |    4.88 KB |
| &#39;Tokenize Complex Code&#39;                        |     4,498.2 ns |      88.88 ns |      95.10 ns |     4,501.0 ns |    1.1978 |    0.0229 |        - |    9.82 KB |
| &#39;Tokenize Large File&#39;                          |    40,585.8 ns |     745.91 ns |   1,183.10 ns |    40,458.9 ns |    9.3384 |    1.2817 |        - |   76.82 KB |
| &#39;Parse Simple Code&#39;                            |     2,645.5 ns |      48.80 ns |      47.92 ns |     2,659.3 ns |    0.7896 |    0.0038 |        - |    6.46 KB |
| &#39;Parse Medium Code&#39;                            |     5,030.3 ns |      97.61 ns |     123.45 ns |     5,042.0 ns |    1.4572 |    0.0305 |        - |   11.91 KB |
| &#39;Parse Complex Code&#39;                           |    13,015.4 ns |     257.82 ns |     538.16 ns |    12,906.6 ns |    3.6774 |    0.1526 |        - |   30.05 KB |
| &#39;Parse Large File&#39;                             |   106,925.8 ns |   2,117.43 ns |   2,438.43 ns |   107,411.5 ns |   25.6348 |    5.2490 |        - |  209.52 KB |
| &#39;Full Pipeline - Simple Code&#39;                  |     2,544.1 ns |      51.25 ns |      42.80 ns |     2,557.0 ns |    0.7896 |    0.0038 |        - |    6.46 KB |
| &#39;Full Pipeline - Medium Code&#39;                  |     6,161.8 ns |     432.81 ns |   1,276.16 ns |     5,705.9 ns |    1.4572 |    0.0305 |        - |   11.91 KB |
| &#39;Full Pipeline - Complex Code&#39;                 |    14,110.3 ns |     275.58 ns |     257.78 ns |    14,098.3 ns |    3.6774 |    0.1526 |        - |   30.05 KB |
| &#39;Parse Loop Intensive Code&#39;                    |     9,420.0 ns |     182.01 ns |     488.97 ns |     9,398.5 ns |    2.4414 |    0.0458 |        - |   19.99 KB |
| &#39;Parse Function Intensive Code&#39;                |    12,930.2 ns |     243.41 ns |     578.49 ns |    12,737.7 ns |    3.9368 |    0.1831 |        - |   32.23 KB |
| &#39;Parse Expression Intensive Code&#39;              |    16,121.2 ns |     317.70 ns |     424.12 ns |    16,251.3 ns |    4.3640 |    0.2441 |        - |   35.82 KB |
| &#39;Parse Class Intensive Code&#39;                   |    34,218.6 ns |     765.34 ns |   2,244.62 ns |    33,796.9 ns |    9.9487 |    1.0376 |        - |   81.57 KB |
| &#39;Multiple Parses - Simple Code&#39;                |    27,541.1 ns |     545.48 ns |     849.25 ns |    27,855.9 ns |    7.9041 |    0.0610 |        - |   64.61 KB |
| &#39;Multiple Parses - Different Codes&#39;            |    22,730.1 ns |     448.21 ns |     498.18 ns |    22,855.0 ns |    5.9204 |    0.1831 |        - |   48.42 KB |
| &#39;Parse Generic Syntax&#39;                         |     8,238.2 ns |     164.61 ns |     364.75 ns |     8,209.2 ns |    2.5787 |    0.0763 |        - |   21.16 KB |
| &#39;Parse Lambda Expressions&#39;                     |    10,597.6 ns |     401.22 ns |   1,131.65 ns |    10,336.1 ns |    2.7161 |    0.0916 |        - |   22.27 KB |
| &#39;Parse LINQ Syntax&#39;                            |     7,900.7 ns |     133.85 ns |     318.11 ns |     7,883.6 ns |    2.0142 |    0.0458 |        - |   16.54 KB |
| &#39;Parse Match Expressions&#39;                      |     8,064.9 ns |     160.58 ns |     309.38 ns |     8,075.4 ns |    2.0905 |    0.0534 |        - |   17.13 KB |
| &#39;Parse Small Script (500 lines)&#39;               |             NA |            NA |            NA |             NA |        NA |        NA |       NA |         NA |
| &#39;Tokenize Small Script (500 lines)&#39;            |   227,606.7 ns |   4,522.35 ns |  10,747.85 ns |   224,918.1 ns |   62.2559 |   62.2559 |  62.2559 |  581.36 KB |
| &#39;Parse Medium Project (3000 lines)&#39;            |             NA |            NA |            NA |             NA |        NA |        NA |       NA |         NA |
| &#39;Tokenize Medium Project (3000 lines)&#39;         | 1,859,294.6 ns |  36,894.34 ns |  97,838.57 ns | 1,854,568.4 ns |  832.0313 |  820.3125 | 689.4531 | 4211.38 KB |
| &#39;Parse Large Script (5000 lines)&#39;              |             NA |            NA |            NA |             NA |        NA |        NA |       NA |         NA |
| &#39;Tokenize Large Script (5000 lines)&#39;           | 2,326,364.2 ns |  26,022.42 ns |  24,341.39 ns | 2,328,133.2 ns |  996.0938 |  996.0938 | 996.0938 | 4982.22 KB |
| &#39;Parse Deeply Nested Expression (50 layers)&#39;   |    34,966.6 ns |     573.45 ns |     508.35 ns |    34,928.8 ns |    8.5449 |    0.6104 |        - |   69.88 KB |
| &#39;Parse Massive Assignments (10000 statements)&#39; | 9,550,957.8 ns | 257,415.08 ns | 734,419.77 ns | 9,485,451.6 ns | 1171.8750 | 1109.3750 | 531.2500 | 7965.47 KB |
| &#39;Parse Extra Large File (10000+ lines)&#39;        | 7,269,869.3 ns | 342,068.90 ns | 942,157.17 ns | 7,063,418.4 ns | 1093.7500 | 1070.3125 | 500.0000 | 7254.45 KB |
| &#39;Parse with Syntax Errors&#39;                     |    16,023.1 ns |     311.09 ns |     291.00 ns |    16,050.9 ns |    1.1597 |         - |        - |     9.6 KB |
| &#39;Concurrent Parsing (4 threads)&#39;               |     8,876.6 ns |     176.03 ns |     156.04 ns |     8,871.1 ns |    4.1351 |    0.1831 |        - |   33.46 KB |

Benchmarks with issues:
  ParserBenchmarkTests.'Parse Small Script (500 lines)': DefaultJob
  ParserBenchmarkTests.'Parse Medium Project (3000 lines)': DefaultJob
  ParserBenchmarkTests.'Parse Large Script (5000 lines)': DefaultJob
