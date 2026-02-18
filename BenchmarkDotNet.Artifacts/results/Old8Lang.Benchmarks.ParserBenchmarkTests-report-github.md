```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.7840/25H2/2025Update/HudsonValley2)
AMD Ryzen 7 5800H with Radeon Graphics 3.20GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.103
  [Host]     : .NET 10.0.3 (10.0.3, 10.0.326.7603), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.3 (10.0.3, 10.0.326.7603), X64 RyuJIT x86-64-v3


```
| Method                                 | Mean | Error |
|--------------------------------------- |-----:|------:|
| &#39;Tokenize Simple Code&#39;                 |   NA |    NA |
| &#39;Tokenize Medium Code&#39;                 |   NA |    NA |
| &#39;Tokenize Complex Code&#39;                |   NA |    NA |
| &#39;Tokenize Large File&#39;                  |   NA |    NA |
| &#39;Parse Simple Code&#39;                    |   NA |    NA |
| &#39;Parse Medium Code&#39;                    |   NA |    NA |
| &#39;Parse Complex Code&#39;                   |   NA |    NA |
| &#39;Parse Large File&#39;                     |   NA |    NA |
| &#39;Full Pipeline - Simple Code&#39;          |   NA |    NA |
| &#39;Full Pipeline - Medium Code&#39;          |   NA |    NA |
| &#39;Full Pipeline - Complex Code&#39;         |   NA |    NA |
| &#39;Parse Loop Intensive Code&#39;            |   NA |    NA |
| &#39;Parse Function Intensive Code&#39;        |   NA |    NA |
| &#39;Parse Expression Intensive Code&#39;      |   NA |    NA |
| &#39;Parse Class Intensive Code&#39;           |   NA |    NA |
| &#39;Multiple Parses - Simple Code&#39;        |   NA |    NA |
| &#39;Multiple Parses - Different Codes&#39;    |   NA |    NA |
| &#39;Parse Generic Syntax&#39;                 |   NA |    NA |
| &#39;Parse Lambda Expressions&#39;             |   NA |    NA |
| &#39;Parse LINQ Syntax&#39;                    |   NA |    NA |
| &#39;Parse Match Expressions&#39;              |   NA |    NA |
| &#39;Parse Small Script (500 lines)&#39;       |   NA |    NA |
| &#39;Tokenize Small Script (500 lines)&#39;    |   NA |    NA |
| &#39;Parse Medium Project (3000 lines)&#39;    |   NA |    NA |
| &#39;Tokenize Medium Project (3000 lines)&#39; |   NA |    NA |
| &#39;Parse Large Script (5000 lines)&#39;      |   NA |    NA |
| &#39;Tokenize Large Script (5000 lines)&#39;   |   NA |    NA |

Benchmarks with issues:
  ParserBenchmarkTests.'Tokenize Simple Code': DefaultJob
  ParserBenchmarkTests.'Tokenize Medium Code': DefaultJob
  ParserBenchmarkTests.'Tokenize Complex Code': DefaultJob
  ParserBenchmarkTests.'Tokenize Large File': DefaultJob
  ParserBenchmarkTests.'Parse Simple Code': DefaultJob
  ParserBenchmarkTests.'Parse Medium Code': DefaultJob
  ParserBenchmarkTests.'Parse Complex Code': DefaultJob
  ParserBenchmarkTests.'Parse Large File': DefaultJob
  ParserBenchmarkTests.'Full Pipeline - Simple Code': DefaultJob
  ParserBenchmarkTests.'Full Pipeline - Medium Code': DefaultJob
  ParserBenchmarkTests.'Full Pipeline - Complex Code': DefaultJob
  ParserBenchmarkTests.'Parse Loop Intensive Code': DefaultJob
  ParserBenchmarkTests.'Parse Function Intensive Code': DefaultJob
  ParserBenchmarkTests.'Parse Expression Intensive Code': DefaultJob
  ParserBenchmarkTests.'Parse Class Intensive Code': DefaultJob
  ParserBenchmarkTests.'Multiple Parses - Simple Code': DefaultJob
  ParserBenchmarkTests.'Multiple Parses - Different Codes': DefaultJob
  ParserBenchmarkTests.'Parse Generic Syntax': DefaultJob
  ParserBenchmarkTests.'Parse Lambda Expressions': DefaultJob
  ParserBenchmarkTests.'Parse LINQ Syntax': DefaultJob
  ParserBenchmarkTests.'Parse Match Expressions': DefaultJob
  ParserBenchmarkTests.'Parse Small Script (500 lines)': DefaultJob
  ParserBenchmarkTests.'Tokenize Small Script (500 lines)': DefaultJob
  ParserBenchmarkTests.'Parse Medium Project (3000 lines)': DefaultJob
  ParserBenchmarkTests.'Tokenize Medium Project (3000 lines)': DefaultJob
  ParserBenchmarkTests.'Parse Large Script (5000 lines)': DefaultJob
  ParserBenchmarkTests.'Tokenize Large Script (5000 lines)': DefaultJob
