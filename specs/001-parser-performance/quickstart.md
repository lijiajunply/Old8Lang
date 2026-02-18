# Quick Start: Old8Lang 解析器性能优化

**Feature**: 001-parser-performance
**Date**: 2026-02-17
**Audience**: Old8Lang 开发者和贡献者

## 1. 概述

本指南帮助您快速了解 Old8Lang 解析器性能优化项目，包括如何运行基准测试、验证性能改进、使用新的优化 API。

---

## 2. 前置条件

### 2.1 环境要求

- **.NET SDK**: 10.0 或更高版本
- **操作系统**: Windows, Linux, 或 macOS
- **内存**: 至少 8GB RAM
- **工具**: Git, 文本编辑器或 IDE（推荐 Rider 或 Visual Studio）

### 2.2 克隆仓库

```bash
git clone <repository-url>
cd Old8Lang
git checkout 001-parser-performance
```

### 2.3 构建项目

```bash
dotnet build Old8Lang.sln
```

---

## 3. 运行基准测试

### 3.1 运行所有基准测试

```bash
cd Old8Lang.Tests
dotnet run --project Old8Lang.Tests.csproj --configuration Release --filter "*ParserBenchmark*"
```

### 3.2 运行特定基准测试

```bash
# 小型脚本基准测试
dotnet run --project Old8Lang.Tests.csproj --configuration Release --filter "*ParseSimpleCode*"

# 中型项目基准测试
dotnet run --project Old8Lang.Tests.csproj --configuration Release --filter "*ParseMediumCode*"

# 大型脚本基准测试
dotnet run --project Old8Lang.Tests.csproj --configuration Release --filter "*ParseLargeCode*"
```

### 3.3 查看基准测试结果

基准测试完成后，结果将保存在 `BenchmarkDotNet.Artifacts/results/` 目录：

```bash
# 查看 Markdown 报告
cat BenchmarkDotNet.Artifacts/results/*-report.md

# 查看 HTML 报告（在浏览器中打开）
open BenchmarkDotNet.Artifacts/results/*-report.html
```

**示例输出**:
```
| Method              | Mean      | Error    | StdDev   | Gen0   | Gen1  | Allocated |
|-------------------- |----------:|---------:|---------:|-------:|------:|----------:|
| ParseSimpleCode     |  45.2 ms  | 0.8 ms   | 0.7 ms   | 1500.0 | 250.0 | 8.2 MB    |
| ParseMediumCode     | 280.5 ms  | 5.2 ms   | 4.9 ms   | 8000.0 | 1200.0| 45.3 MB   |
| ParseLargeCode      | 520.3 ms  | 9.8 ms   | 9.2 ms   | 12000.0| 2000.0| 65.7 MB   |
```

---

## 4. 验证性能改进

### 4.1 对比优化前后

```bash
# 1. 切换到优化前的分支
git checkout master

# 2. 运行基准测试并保存结果
dotnet run --project Old8Lang.Tests.csproj --configuration Release --filter "*ParserBenchmark*"
cp BenchmarkDotNet.Artifacts/results/*-report.md baseline-report.md

# 3. 切换到优化后的分支
git checkout 001-parser-performance

# 4. 运行基准测试并保存结果
dotnet run --project Old8Lang.Tests.csproj --configuration Release --filter "*ParserBenchmark*"
cp BenchmarkDotNet.Artifacts/results/*-report.md optimized-report.md

# 5. 对比结果
diff baseline-report.md optimized-report.md
```

### 4.2 使用性能指标 API

创建测试文件 `test-performance.old8`:

```old8
// 500 行测试代码
var x = 123;
var y = 456;
// ... 更多代码
```

运行性能测试：

```csharp
using Old8Lang.LangParser;
using Old8Lang.LangParser.Optimization;

var sourceCode = File.ReadAllText("test-performance.old8");

// 使用性能指标 API
var tokens = LangTokenizer.TokenizeWithMetrics(sourceCode, out var metrics);
Console.WriteLine(metrics);

// 输出:
// 解析性能指标:
// - 词法分析: 45ms
// - 语法分析: 120ms
// - 总时间: 165ms
// - Token 数量: 1250
// - Token/秒: 7575
// - 内存分配: 2.5 MB
```

### 4.3 验证成功标准

根据 `spec.md` 中的成功标准验证：

| 指标 | 目标 | 验证命令 |
|------|------|---------|
| 500 行脚本 < 100ms | ✅ | 查看 `ParseSimpleCode` 基准测试 |
| 3000 行项目 < 500ms | ✅ | 查看 `ParseMediumCode` 基准测试 |
| 5000 行脚本 < 800ms | ✅ | 查看 `ParseLargeCode` 基准测试 |
| 内存减少 20% | ✅ | 对比 `Allocated` 列 |
| 所有测试通过 | ✅ | 运行 `dotnet test` |

---

## 5. 使用优化 API

### 5.1 基本使用（无需修改现有代码）

```csharp
// 现有代码继续工作，自动受益于内部优化
var tokens = LangTokenizer.Tokenize(sourceCode);
var parser = new LangParserClass(tokens, sourceCode);
var ast = parser.ParseProgram();
```

### 5.2 使用优化版本（推荐）

```csharp
using Old8Lang.LangParser;
using Old8Lang.LangParser.Optimization;

// 创建字符串缓存（可重用）
var stringCache = new StringCache();

// 使用优化版本
var tokens = LangTokenizer.TokenizeOptimized(sourceCode.AsSpan(), stringCache);
var parser = new LangParserClass(tokens, sourceCode);
var ast = parser.ParseProgram();

// 查看缓存统计
var stats = stringCache.GetStatistics();
Console.WriteLine($"缓存命中率: {stats.HitRate:P2}");
```

### 5.3 批量解析（重用缓存）

```csharp
var stringCache = new StringCache();

foreach (var file in Directory.GetFiles("*.old8"))
{
    var sourceCode = File.ReadAllText(file);
    var tokens = LangTokenizer.TokenizeOptimized(sourceCode.AsSpan(), stringCache);
    // ... 处理 tokens
}

// 查看总体缓存效果
var stats = stringCache.GetStatistics();
Console.WriteLine($"总请求: {stats.TotalRequests}");
Console.WriteLine($"缓存命中: {stats.CacheHits}");
Console.WriteLine($"缓存未命中: {stats.CacheMisses}");
Console.WriteLine($"命中率: {stats.HitRate:P2}");
```

### 5.4 使用字符缓冲区池

```csharp
using Old8Lang.LangParser.Optimization;

// 租用字符缓冲区
using var buffer = CharBufferPool.Rent(256);

// 使用缓冲区
int length = 0;
foreach (char c in sourceCode)
{
    if (char.IsLetterOrDigit(c))
    {
        buffer.Span[length++] = c;
    }
}

// 创建字符串
var result = new string(buffer.Span.Slice(0, length));

// 自动归还缓冲区（通过 using 语句）
```

---

## 6. 常见问题

### Q1: 为什么我的基准测试结果与预期不符？

**A**: 确保：
1. 使用 `--configuration Release` 构建
2. 关闭其他占用 CPU 的程序
3. 运行多次取平均值
4. 检查是否有后台任务（如杀毒软件）

### Q2: 如何调试性能问题？

**A**: 使用性能分析工具：

```bash
# 使用 dotnet-trace
dotnet tool install --global dotnet-trace
dotnet trace collect --process-id <pid> --providers Microsoft-Windows-DotNETRuntime

# 使用 PerfView (Windows)
PerfView.exe collect -ThreadTime -CircularMB:1000
```

### Q3: 字符串缓存命中率低怎么办？

**A**: 检查：
1. 缓存大小是否足够（默认 10000）
2. 是否有大量唯一标识符
3. 是否正确重用缓存实例

```csharp
// 增加缓存大小
var cache = new StringCache(maxCacheSize: 50000);
```

### Q4: 如何验证向后兼容性？

**A**: 运行完整的测试套件：

```bash
dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj
```

所有测试应该通过，无回归问题。

### Q5: 优化后代码更复杂了，如何理解？

**A**: 参考文档：
- `research.md` - 优化原理和最佳实践
- `data-model.md` - 数据结构设计
- `contracts/tokenizer-api.md` - API 契约和使用示例

---

## 7. 下一步

### 7.1 深入了解

- 阅读 `research.md` 了解性能瓶颈分析
- 阅读 `data-model.md` 了解数据结构设计
- 阅读 `contracts/tokenizer-api.md` 了解 API 契约

### 7.2 贡献代码

1. 创建功能分支: `git checkout -b feature/my-optimization`
2. 实现优化
3. 运行测试: `dotnet test`
4. 运行基准测试: `dotnet run --project Old8Lang.Tests.csproj --configuration Release`
5. 提交 PR

### 7.3 报告问题

如果发现性能回归或 bug，请在 GitHub 上创建 Issue，包含：
- 复现步骤
- 基准测试结果
- 环境信息（OS, .NET 版本）

---

## 8. 快速参考

### 8.1 常用命令

```bash
# 构建项目
dotnet build Old8Lang.sln

# 运行测试
dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj

# 运行基准测试
dotnet run --project Old8Lang.Tests.csproj --configuration Release --filter "*ParserBenchmark*"

# 运行 Old8Lang 代码（解释模式）
dotnet run --project Old8Lang.App -- -f test.old8

# 运行 Old8Lang 代码（编译模式）
dotnet run --project Old8Lang.App -- -c test.old8

# 运行 Old8Lang 代码（VM 模式）
dotnet run --project Old8Lang.App -- -vm test.old8
```

### 8.2 性能目标

| 场景 | 目标时间 | 目标内存 |
|------|---------|---------|
| 500 行脚本 | < 100ms | < 5 MB |
| 3000 行项目 | < 500ms | < 30 MB |
| 5000 行脚本 | < 800ms | < 50 MB |

### 8.3 关键文件

| 文件 | 说明 |
|------|------|
| `Old8Lang/LangParser/LangToken.cs` | Tokenizer 实现 |
| `Old8Lang/LangParser/Optimization/` | 优化工具类 |
| `Old8Lang.Tests/PerformanceTests/` | 性能基准测试 |
| `specs/001-parser-performance/` | 设计文档 |

---

## 9. 示例代码

### 9.1 完整示例：性能监控

```csharp
using System;
using System.IO;
using Old8Lang.LangParser;
using Old8Lang.LangParser.Optimization;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("用法: program <file.old8>");
            return;
        }

        var sourceCode = File.ReadAllText(args[0]);

        // 使用性能指标 API
        var tokens = LangTokenizer.TokenizeWithMetrics(sourceCode, out var metrics);

        Console.WriteLine("=== 性能指标 ===");
        Console.WriteLine(metrics);

        // 解析 AST
        var parser = new LangParserClass(tokens, sourceCode);
        var ast = parser.ParseProgram();

        Console.WriteLine($"\n=== 解析结果 ===");
        Console.WriteLine($"AST 节点数: {CountNodes(ast)}");
    }

    static int CountNodes(object node)
    {
        // 递归计数 AST 节点
        // ... 实现细节
        return 0;
    }
}
```

### 9.2 完整示例：批量解析

```csharp
using System;
using System.IO;
using System.Linq;
using Old8Lang.LangParser;
using Old8Lang.LangParser.Optimization;

class BatchParser
{
    static void Main(string[] args)
    {
        var files = Directory.GetFiles(".", "*.old8");
        var stringCache = new StringCache();

        Console.WriteLine($"找到 {files.Length} 个文件");

        foreach (var file in files)
        {
            Console.Write($"解析 {Path.GetFileName(file)}... ");

            var sourceCode = File.ReadAllText(file);
            var tokens = LangTokenizer.TokenizeOptimized(sourceCode.AsSpan(), stringCache);

            Console.WriteLine($"完成 ({tokens.Count} tokens)");
        }

        // 显示缓存统计
        var stats = stringCache.GetStatistics();
        Console.WriteLine($"\n=== 缓存统计 ===");
        Console.WriteLine($"总请求: {stats.TotalRequests}");
        Console.WriteLine($"缓存命中: {stats.CacheHits}");
        Console.WriteLine($"缓存未命中: {stats.CacheMisses}");
        Console.WriteLine($"命中率: {stats.HitRate:P2}");
        Console.WriteLine($"缓存大小: {stats.CurrentSize}");
    }
}
```

---

## 10. 故障排除

### 10.1 基准测试失败

**症状**: 基准测试抛出异常或无法完成

**解决方案**:
1. 检查 .NET SDK 版本: `dotnet --version`
2. 清理并重新构建: `dotnet clean && dotnet build --configuration Release`
3. 检查测试文件是否存在
4. 查看详细错误信息: `dotnet run --verbosity detailed`

### 10.2 性能未达到目标

**症状**: 基准测试结果未达到预期性能目标

**解决方案**:
1. 确认使用 Release 配置: `--configuration Release`
2. 关闭调试器和性能分析工具
3. 检查系统资源使用情况
4. 运行多次取平均值
5. 使用性能分析工具定位瓶颈

### 10.3 内存泄漏

**症状**: 长时间运行后内存持续增长

**解决方案**:
1. 检查对象池是否正确归还对象
2. 检查字符串缓存大小限制
3. 使用内存分析工具（如 dotMemory）
4. 检查是否有循环引用

---

**快速入门指南完成日期**: 2026-02-17
**下一步**: 运行 update-agent-context 脚本更新 Claude 上下文
