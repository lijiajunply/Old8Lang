# API Contract: 优化后的 Tokenizer 接口

**Feature**: 001-parser-performance
**Date**: 2026-02-17
**Version**: 1.0.0

## 1. 概述

本契约定义了优化后的 Old8Lang Tokenizer 接口。优化重点是减少内存分配、提升解析速度，同时保持向后兼容性。

---

## 2. 核心接口

### 2.1 LangTokenizer (主接口)

```csharp
namespace Old8Lang.LangParser;

/// <summary>
/// Old8Lang 词法分析器（优化版本）
/// </summary>
public static class LangTokenizer
{
    /// <summary>
    /// 将源代码转换为 Token 列表（兼容接口）
    /// </summary>
    /// <param name="code">源代码</param>
    /// <param name="preprocessorSymbols">预处理器符号（可选）</param>
    /// <returns>Token 列表</returns>
    public static List<LangToken> Tokenize(
        string code,
        PreprocessorSymbols? preprocessorSymbols = null);

    /// <summary>
    /// 将源代码转换为 Token 列表，包含文件头指令和文档注释（兼容接口）
    /// </summary>
    /// <param name="code">源代码</param>
    /// <param name="preprocessorSymbols">预处理器符号（可选）</param>
    /// <returns>Token 列表、文件头指令、文档注释</returns>
    public static (List<LangToken> tokens, List<LangToken> headerDirectives, List<LangToken> docComments)
        TokenizeWithDirectivesAndDocs(
            string code,
            PreprocessorSymbols? preprocessorSymbols = null);

    /// <summary>
    /// 将源代码转换为 Token 列表（优化版本，使用 ReadOnlySpan）
    /// </summary>
    /// <param name="code">源代码（Span）</param>
    /// <param name="stringCache">字符串缓存（可选）</param>
    /// <param name="preprocessorSymbols">预处理器符号（可选）</param>
    /// <returns>Token 列表</returns>
    public static List<LangToken> TokenizeOptimized(
        ReadOnlySpan<char> code,
        StringCache? stringCache = null,
        PreprocessorSymbols? preprocessorSymbols = null);

    /// <summary>
    /// 将源代码转换为 Token 列表，包含性能指标
    /// </summary>
    /// <param name="code">源代码</param>
    /// <param name="metrics">性能指标输出</param>
    /// <param name="preprocessorSymbols">预处理器符号（可选）</param>
    /// <returns>Token 列表</returns>
    public static List<LangToken> TokenizeWithMetrics(
        string code,
        out ParserPerformanceMetrics metrics,
        PreprocessorSymbols? preprocessorSymbols = null);
}
```

---

### 2.2 StringCache (字符串缓存)

```csharp
namespace Old8Lang.LangParser.Optimization;

/// <summary>
/// 字符串缓存，用于减少重复字符串的内存分配
/// </summary>
public sealed class StringCache
{
    /// <summary>
    /// 创建字符串缓存
    /// </summary>
    /// <param name="maxCacheSize">最大缓存大小（默认 10000）</param>
    public StringCache(int maxCacheSize = 10000);

    /// <summary>
    /// 获取或添加字符串到缓存
    /// </summary>
    /// <param name="value">字符串值（Span）</param>
    /// <returns>缓存的字符串</returns>
    public string GetOrAdd(ReadOnlySpan<char> value);

    /// <summary>
    /// 清空缓存
    /// </summary>
    public void Clear();

    /// <summary>
    /// 获取缓存统计信息
    /// </summary>
    public CacheStatistics GetStatistics();
}

/// <summary>
/// 缓存统计信息
/// </summary>
public readonly record struct CacheStatistics(
    int TotalRequests,
    int CacheHits,
    int CacheMisses,
    int CurrentSize,
    double HitRate
);
```

---

### 2.3 CharBufferPool (字符缓冲区池)

```csharp
namespace Old8Lang.LangParser.Optimization;

/// <summary>
/// 字符缓冲区池，用于减少字符数组分配
/// </summary>
public static class CharBufferPool
{
    /// <summary>
    /// 租用字符缓冲区
    /// </summary>
    /// <param name="minimumLength">最小长度</param>
    /// <returns>租用的缓冲区（使用 using 语句自动归还）</returns>
    public static RentedBuffer Rent(int minimumLength);

    /// <summary>
    /// 租用的缓冲区（实现 IDisposable）
    /// </summary>
    public struct RentedBuffer : IDisposable
    {
        /// <summary>
        /// 获取缓冲区的 Span 视图
        /// </summary>
        public Span<char> Span { get; }

        /// <summary>
        /// 归还缓冲区到池
        /// </summary>
        public void Dispose();
    }
}
```

---

### 2.4 TokenListPool (Token 列表池)

```csharp
namespace Old8Lang.LangParser.Optimization;

/// <summary>
/// Token 列表池，用于减少列表分配
/// </summary>
public static class TokenListPool
{
    /// <summary>
    /// 租用 Token 列表
    /// </summary>
    /// <returns>已清空的 Token 列表</returns>
    public static List<LangToken> Rent();

    /// <summary>
    /// 归还 Token 列表到池
    /// </summary>
    /// <param name="list">要归还的列表</param>
    public static void Return(List<LangToken> list);
}
```

---

### 2.5 ParserPerformanceMetrics (性能指标)

```csharp
namespace Old8Lang.LangParser.Optimization;

/// <summary>
/// 解析器性能指标
/// </summary>
public sealed class ParserPerformanceMetrics
{
    // 时间指标
    public long TokenizationTimeMs { get; set; }
    public long ParsingTimeMs { get; set; }
    public long TotalTimeMs { get; set; }

    // 数量指标
    public long TokenCount { get; set; }
    public long SourceCodeLength { get; set; }

    // 内存指标
    public long MemoryAllocatedBytes { get; set; }
    public long PeakMemoryUsageBytes { get; set; }

    // GC 指标
    public int GCGen0Collections { get; set; }
    public int GCGen1Collections { get; set; }
    public int GCGen2Collections { get; set; }

    // 计算属性
    public double TokensPerSecond { get; }
    public double CharsPerSecond { get; }

    /// <summary>
    /// 格式化输出性能指标
    /// </summary>
    public override string ToString();
}
```

---

## 3. 使用示例

### 3.1 基本使用（兼容接口）

```csharp
// 使用现有接口（无需修改现有代码）
var tokens = LangTokenizer.Tokenize(sourceCode);
var parser = new LangParserClass(tokens, sourceCode);
var ast = parser.ParseProgram();
```

### 3.2 优化版本使用

```csharp
// 使用优化版本（推荐用于性能关键路径）
var stringCache = new StringCache();
var tokens = LangTokenizer.TokenizeOptimized(sourceCode.AsSpan(), stringCache);
var parser = new LangParserClass(tokens, sourceCode);
var ast = parser.ParseProgram();
```

### 3.3 性能监控

```csharp
// 获取性能指标
var tokens = LangTokenizer.TokenizeWithMetrics(sourceCode, out var metrics);
Console.WriteLine(metrics);

// 输出示例:
// 解析性能指标:
// - 词法分析: 45ms
// - 语法分析: 120ms
// - 总时间: 165ms
// - Token 数量: 1250
// - 源代码长度: 5432 字符
// - Token/秒: 7575
// - 字符/秒: 32921
// - 内存分配: 2.5 MB
// - 峰值内存: 4.8 MB
// - GC 收集: Gen0=5, Gen1=1, Gen2=0
```

### 3.4 字符串缓存使用

```csharp
// 创建字符串缓存
var cache = new StringCache(maxCacheSize: 10000);

// 多次解析时重用缓存
for (int i = 0; i < 100; i++)
{
    var tokens = LangTokenizer.TokenizeOptimized(sourceCode.AsSpan(), cache);
    // ... 处理 tokens
}

// 查看缓存统计
var stats = cache.GetStatistics();
Console.WriteLine($"缓存命中率: {stats.HitRate:P2}");
// 输出: 缓存命中率: 65.43%
```

### 3.5 字符缓冲区池使用

```csharp
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

### 3.6 Token 列表池使用

```csharp
// 租用 Token 列表
var tokens = TokenListPool.Rent();

try
{
    // 使用列表
    tokens.Add(new LangToken("var", LangTokenType.Var, 1, 0));
    tokens.Add(new LangToken("x", LangTokenType.Identifier, 1, 4));
    // ...

    // 处理 tokens
    ProcessTokens(tokens);
}
finally
{
    // 归还列表到池
    TokenListPool.Return(tokens);
}
```

---

## 4. 性能保证

### 4.1 时间复杂度

| 操作 | 复杂度 | 说明 |
|------|--------|------|
| `Tokenize()` | O(n) | n 为源代码长度 |
| `StringCache.GetOrAdd()` | O(1) 平均 | 使用哈希表 |
| `CharBufferPool.Rent()` | O(1) | 使用对象池 |
| `TokenListPool.Rent()` | O(1) | 使用对象池 |

### 4.2 空间复杂度

| 操作 | 复杂度 | 说明 |
|------|--------|------|
| `Tokenize()` | O(n) | n 为 token 数量 |
| `StringCache` | O(m) | m 为唯一字符串数量（<= 10000） |
| `CharBufferPool` | O(1) | 池大小固定 |
| `TokenListPool` | O(1) | 池大小固定 |

### 4.3 性能目标

| 指标 | 目标值 | 测量方法 |
|------|--------|---------|
| 500 行脚本解析时间 | < 100ms | BenchmarkDotNet |
| 3000 行项目解析时间 | < 500ms | BenchmarkDotNet |
| 5000 行脚本解析时间 | < 800ms | BenchmarkDotNet |
| 内存分配减少 | 30-50% | MemoryDiagnoser |
| GC 收集减少 | 40-60% | GC.CollectionCount |
| 字符串缓存命中率 | > 50% | CacheStatistics |

---

## 5. 错误处理

### 5.1 异常类型

```csharp
// 词法分析错误
public class LexicalException : Exception
{
    public int Line { get; }
    public int Column { get; }
    public string SourceCode { get; }

    public LexicalException(string message, int line, int column, string sourceCode)
        : base(message)
    {
        Line = line;
        Column = column;
        SourceCode = sourceCode;
    }
}

// 缓存溢出警告（非致命）
public class CacheOverflowWarning : Exception
{
    public int CurrentSize { get; }
    public int MaxSize { get; }

    public CacheOverflowWarning(int currentSize, int maxSize)
        : base($"字符串缓存已满 ({currentSize}/{maxSize})，新字符串将不被缓存")
    {
        CurrentSize = currentSize;
        MaxSize = maxSize;
    }
}
```

### 5.2 错误场景

| 场景 | 异常类型 | 处理方式 |
|------|---------|---------|
| 非法字符 | `LexicalException` | 抛出异常，包含位置信息 |
| 未闭合字符串 | `LexicalException` | 抛出异常，包含位置信息 |
| 缓存溢出 | 无异常 | 记录警告，继续执行 |
| 缓冲区不足 | 自动扩展 | 透明处理，无异常 |

---

## 6. 向后兼容性

### 6.1 兼容性保证

- ✅ 现有的 `Tokenize()` 和 `TokenizeWithDirectivesAndDocs()` 接口保持不变
- ✅ `LangToken` 结构保持不变（`readonly record struct`）
- ✅ 所有现有测试继续通过
- ✅ 错误信息格式保持一致

### 6.2 迁移路径

**阶段 1: 内部优化（无需用户修改）**
- 优化 Tokenizer 内部实现
- 使用 Span<T> 和 ArrayPool<T>
- 现有代码无需修改

**阶段 2: 可选优化（推荐但非必需）**
- 提供 `TokenizeOptimized()` 方法
- 用户可选择使用优化版本
- 现有代码继续工作

**阶段 3: 性能监控（可选）**
- 提供 `TokenizeWithMetrics()` 方法
- 用户可选择监控性能
- 不影响现有功能

---

## 7. 测试契约

### 7.1 功能测试

```csharp
[Fact]
public void Tokenize_ShouldProduceSameResultAsOriginal()
{
    var code = "var x = 123;";
    var originalTokens = LangTokenizer.Tokenize(code);
    var optimizedTokens = LangTokenizer.TokenizeOptimized(code.AsSpan());

    Assert.Equal(originalTokens.Count, optimizedTokens.Count);
    for (int i = 0; i < originalTokens.Count; i++)
    {
        Assert.Equal(originalTokens[i], optimizedTokens[i]);
    }
}
```

### 7.2 性能测试

```csharp
[Benchmark]
[MemoryDiagnoser]
public List<LangToken> Tokenize_Original()
{
    return LangTokenizer.Tokenize(SourceCode);
}

[Benchmark]
[MemoryDiagnoser]
public List<LangToken> Tokenize_Optimized()
{
    return LangTokenizer.TokenizeOptimized(SourceCode.AsSpan(), _stringCache);
}
```

### 7.3 缓存测试

```csharp
[Fact]
public void StringCache_ShouldReuseStrings()
{
    var cache = new StringCache();
    var str1 = cache.GetOrAdd("identifier".AsSpan());
    var str2 = cache.GetOrAdd("identifier".AsSpan());

    Assert.Same(str1, str2); // 引用相等
}
```

---

## 8. 版本历史

| 版本 | 日期 | 变更 |
|------|------|------|
| 1.0.0 | 2026-02-17 | 初始版本，定义优化后的 Tokenizer 接口 |

---

**契约完成日期**: 2026-02-17
**下一步**: 生成快速入门指南
