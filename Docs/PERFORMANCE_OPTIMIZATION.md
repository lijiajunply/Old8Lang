# Old8Lang 性能优化指南

本文档提供 Old8Lang 解析器性能优化的详细指南，包括优化技术、最佳实践和性能调优建议。

## 目录

1. [性能优化概述](#性能优化概述)
2. [优化技术详解](#优化技术详解)
3. [性能监控 API](#性能监控-api)
4. [最佳实践](#最佳实践)
5. [性能调优](#性能调优)
6. [常见问题](#常见问题)

## 性能优化概述

### 优化成果

Old8Lang 解析器经过系统性的性能优化，实现了显著的性能提升：

| 指标 | 优化前 | 优化后 | 提升幅度 |
|------|--------|--------|---------|
| 小型脚本（500行） | ~140ms | 126ms | 10% |
| 中型项目（3000行） | ~830ms | 32ms | 96.4% |
| 大型脚本（5000行） | ~1330ms | 30ms | 97.7% |
| 内存使用（5000行） | ~65MB | 4.87MB | 92.5% |
| GC 收集 | 多次 | 0 次 | 100% |
| StringCache 命中率 | N/A | 90% | N/A |

### 优化阶段

优化工作分为三个主要阶段（User Story）：

1. **User Story 1**: 小型脚本优化（Span<T>、内存池化、StringCache）
2. **User Story 2**: 中大型项目优化（递归深度保护、算法优化）
3. **User Story 3**: 内存效率优化（性能监控、内存泄漏防护）

## 优化技术详解

### 1. 零拷贝技术 (Zero-Copy)

#### 原理

使用 `Span<T>` 和 `ReadOnlySpan<char>` 直接在原始字符串上进行切片操作，避免创建新的字符串对象。

#### 实现位置

`Old8Lang/LangParser/LangToken.cs`

#### 代码示例

**优化前**:
```csharp
// 使用 StringBuilder 和 Substring
var sb = new StringBuilder();
for (int i = startIndex; i <= endIndex; i++)
{
    sb.Append(code[i]);
}
var numberStr = sb.ToString();
```

**优化后**:
```csharp
// 使用 Span<T> 零拷贝
var numberSpan = code.AsSpan(startIndex, endIndex - startIndex + 1);
var numberStr = new string(numberSpan);
```

#### 性能影响

- 内存分配减少 90%+
- 词法分析速度提升 30-40%
- 消除 StringBuilder 和 Substring 的开销

#### 适用场景

- 数字解析
- 字符串字面量解析
- 标识符解析
- 文件头指令解析

### 2. 内存池化 (Memory Pooling)

#### 原理

使用对象池重用频繁创建和销毁的对象，减少 GC 压力。

#### CharBufferPool

**实现位置**: `Old8Lang/LangParser/Optimization/CharBufferPool.cs`

**使用方法**:
```csharp
using var buffer = CharBufferPool.Rent(256);
var bufferSpan = buffer.Span;

// 使用缓冲区进行字符串操作
for (int i = 0; i < length; i++)
{
    bufferSpan[i] = processChar(input[i]);
}

var result = new string(bufferSpan.Slice(0, length));
// Dispose 时自动归还缓冲区
```

**性能影响**:
- 减少数组分配开销
- 降低 GC 压力
- 适用于临时字符缓冲区

#### TokenListPool

**实现位置**: `Old8Lang/LangParser/Optimization/TokenListPool.cs`

**使用方法**:
```csharp
var list = TokenListPool.Rent();
try
{
    // 使用列表
    list.Add(token1);
    list.Add(token2);
    // ...
}
finally
{
    TokenListPool.Return(list);
}
```

**性能影响**:
- 减少列表对象分配
- 自动清空列表内容
- 适用于内部临时列表

#### 最佳实践

1. **使用 using 模式**: 确保资源正确归还
2. **避免长期持有**: 对象池适用于短期临时对象
3. **合理设置容量**: 根据实际需求租用合适大小的缓冲区

### 3. 字符串缓存 (String Caching)

#### 原理

使用 `ConcurrentDictionary` 缓存短字符串（≤64 字符），避免重复分配相同的字符串。

#### 实现位置

`Old8Lang/LangParser/Optimization/StringCache.cs`

#### 使用方法

```csharp
var cache = new StringCache(maxCacheSize: 10000);

// 获取或添加字符串
var cachedStr = cache.GetOrAdd("identifier".AsSpan());

// 获取缓存统计
var stats = cache.GetStatistics();
Console.WriteLine($"命中率: {stats.HitRate:P2}");
Console.WriteLine($"缓存大小: {stats.CurrentSize}");
```

#### 缓存策略

- **只缓存短字符串**: 长度 ≤ 64 字符
- **大小限制**: 默认最多 10000 条目
- **线程安全**: 使用 `ConcurrentDictionary` 和 `Interlocked`
- **自动限制**: 缓存满时拒绝新条目

#### 性能影响

- 缓存命中率：90%
- 重复字符串零分配
- 内存使用减少 20%+

#### 适用场景

- 标识符（变量名、函数名）
- 关键字
- 常用字符串字面量

### 4. 算法优化

#### O(n²) 算法修复

**问题**: 文档注释合并使用 `InsertRange(0, group)` 导致 O(n²) 复杂度

**实现位置**: `Old8Lang/LangParser/LangToken.cs:818-879`

**优化前**:
```csharp
// O(n²) 复杂度
foreach (var group in docCommentGroups)
{
    relevantDocs.InsertRange(0, group); // 每次插入都需要移动所有元素
}
```

**优化后**:
```csharp
// O(n) 复杂度
foreach (var group in docCommentGroups)
{
    relevantDocs.AddRange(group); // 直接追加
}
// 最后一次反转
relevantDocs.Reverse();
```

**性能影响**:
- 大文件解析速度提升 96%+
- 3000 行项目：从 830ms 降至 32ms
- 5000 行脚本：从 1330ms 降至 30ms

### 5. 递归深度保护

#### 原理

在递归解析方法中添加深度计数器，防止栈溢出。

#### 实现位置

- `Old8Lang/LangParser/Core/ParserContext.cs`
- `Old8Lang/LangParser/Parsers/ExpressionParser.cs`

#### 使用方法

```csharp
public LangExpression ParsePower()
{
    Context.EnterRecursion();
    try
    {
        // 递归解析逻辑
        if (CurrentToken.Type == LangTokenType.Minus)
        {
            var operand = ParsePower(); // 递归调用
            return new Operation(null, LangTokenType.Minus, operand, position);
        }
        // ...
    }
    finally
    {
        Context.ExitRecursion();
    }
}
```

#### 配置

- **默认限制**: 500 层
- **错误消息**: "表达式嵌套过深，超过最大限制（500层）"
- **性能影响**: 对正常代码无影响

### 6. 预计算优化

#### SourceLines 预分割

**问题**: 延迟初始化导致错误报告时重复分割字符串

**实现位置**: `Old8Lang/LangParser/Core/ParserContext.cs`

**优化前**:
```csharp
public string[] SourceLines => _sourceLines ??= SourceCode.Split('\n');
```

**优化后**:
```csharp
public ParserContext(string sourceCode, ...)
{
    if (!string.IsNullOrEmpty(sourceCode))
    {
        SourceLines = sourceCode.Split('\n');
    }
}
```

**性能影响**:
- 减少冗余计算
- 提升错误报告性能
- 内存使用略微增加（可接受）

## 性能监控 API

### ParserPerformanceMetrics

#### 功能

收集和报告解析器的性能数据，包括时间、内存、GC 等指标。

#### 实现位置

`Old8Lang/LangParser/Optimization/ParserPerformanceMetrics.cs`

#### 使用方法

```csharp
// 开始收集性能指标
var (metrics, stopwatch, memoryBefore, gc0Before, gc1Before, gc2Before) =
    ParserPerformanceMetrics.BeginCollection(sourceCode.Length);

// 执行解析
var tokens = LangTokenizer.Tokenize(sourceCode);

// 结束收集并计算指标
ParserPerformanceMetrics.EndCollection(
    metrics,
    stopwatch,
    memoryBefore,
    gc0Before,
    gc1Before,
    gc2Before,
    tokens.Count);

// 输出性能指标
Console.WriteLine(metrics.ToString());
```

#### 指标说明

| 指标 | 说明 |
|------|------|
| TokenizationTimeMs | 词法分析时间（毫秒） |
| ParsingTimeMs | 语法分析时间（毫秒） |
| TotalTimeMs | 总时间（毫秒） |
| TokenCount | Token 数量 |
| SourceCodeLength | 源代码长度（字符） |
| MemoryAllocatedBytes | 内存分配量（字节） |
| PeakMemoryUsageBytes | 峰值内存使用（字节） |
| GCGen0Collections | Gen0 GC 收集次数 |
| GCGen1Collections | Gen1 GC 收集次数 |
| GCGen2Collections | Gen2 GC 收集次数 |
| TokensPerSecond | Token 处理速度（个/秒） |
| CharsPerSecond | 字符处理速度（个/秒） |

### 优化 API

#### TokenizeOptimized

高性能词法分析，集成所有优化技术。

```csharp
var tokens = LangTokenizer.TokenizeOptimized(code);
```

**特点**:
- 自动使用 StringCache
- 自动使用 CharBufferPool
- 零额外开销
- 适用于生产环境

#### TokenizeWithMetrics

带性能指标收集的词法分析。

```csharp
var (tokens, metrics) = LangTokenizer.TokenizeWithMetrics(code);

Console.WriteLine($"解析速度: {metrics.TokensPerSecond:F0} tokens/秒");
Console.WriteLine($"内存使用: {metrics.PeakMemoryUsageBytes / 1024.0 / 1024.0:F2} MB");
Console.WriteLine($"GC 收集: Gen0={metrics.GCGen0Collections}");
```

**特点**:
- 返回 Token 列表和性能指标
- 适用于性能测试和监控
- 轻微的性能开销（GC 强制收集）

## 最佳实践

### 1. 选择合适的执行模式

| 模式 | 适用场景 | 性能特征 |
|------|---------|---------|
| 解释模式 (`-f`) | 开发、调试、脚本 | 快速启动，中等性能 |
| IL 模式 (`-il`) | 生产、性能关键 | 慢启动，高性能 |
| VM 模式 (`-vm`) | 跨平台分发、调试 | 中等启动，中等性能 |

### 2. 优化代码结构

#### 避免深层嵌套

```csharp
// 不推荐：深层嵌套
var result = a ? (b ? (c ? d : e) : (f ? g : h)) : (i ? j : k);

// 推荐：使用中间变量
var temp1 = c ? d : e;
var temp2 = b ? temp1 : (f ? g : h);
var result = a ? temp2 : (i ? j : k);
```

#### 减少重复字符串

```csharp
// 不推荐：重复字符串
var x1 = "identifier";
var x2 = "identifier";
var x3 = "identifier";

// 推荐：使用常量
const string ID = "identifier";
var x1 = ID;
var x2 = ID;
var x3 = ID;
```

### 3. 使用性能监控

#### 定期运行基准测试

```bash
# 运行完整基准测试
dotnet run --project Old8Lang.Benchmarks --configuration Release

# 运行 VM Quick（PR 快速回归）
dotnet run --project Old8Lang.Benchmarks --configuration Release -- --vm-report-quick

# 运行 VM Nightly（全量回归，FAIL 时返回非零退出码）
dotnet run --project Old8Lang.Benchmarks --configuration Release -- --vm-report-nightly

# 运行性能验证
dotnet test Old8Lang.Benchmarks --filter "FullyQualifiedName~PerformanceValidator"
```

#### 监控内存使用

```csharp
var (tokens, metrics) = LangTokenizer.TokenizeWithMetrics(code);

if (metrics.PeakMemoryUsageBytes > 50 * 1024 * 1024) // 50MB
{
    Console.WriteLine("警告：内存使用过高");
}

if (metrics.GCGen0Collections > 10)
{
    Console.WriteLine("警告：GC 收集过于频繁");
}
```

### 4. 内存管理

#### 使用对象池

```csharp
// 推荐：使用对象池
using var buffer = CharBufferPool.Rent(256);
// 使用缓冲区...
// Dispose 时自动归还

// 不推荐：频繁创建数组
var buffer = new char[256]; // 每次都分配新数组
```

#### 限制缓存大小

```csharp
// 推荐：设置合理的缓存大小
var cache = new StringCache(maxCacheSize: 10000);

// 不推荐：无限制缓存
var cache = new StringCache(maxCacheSize: int.MaxValue);
```

## 性能调优

### 1. 识别性能瓶颈

#### 使用 TokenizeWithMetrics

```csharp
var (tokens, metrics) = LangTokenizer.TokenizeWithMetrics(code);

// 分析时间分布
Console.WriteLine($"词法分析: {metrics.TokenizationTimeMs}ms");
Console.WriteLine($"语法分析: {metrics.ParsingTimeMs}ms");

// 分析吞吐量
Console.WriteLine($"Token/秒: {metrics.TokensPerSecond:F0}");
Console.WriteLine($"字符/秒: {metrics.CharsPerSecond:F0}");
```

#### 使用 BenchmarkDotNet

```csharp
[Benchmark]
public void ParseLargeFile()
{
    var tokens = LangTokenizer.Tokenize(largeCode);
    var ast = LangParser.Parse(tokens);
}
```

### 2. 优化策略

#### 策略 1: 减少内存分配

- 使用 Span<T> 替代 string
- 使用对象池替代 new
- 使用 StringCache 缓存重复字符串

#### 策略 2: 优化算法复杂度

- 避免 O(n²) 算法
- 使用合适的数据结构
- 预计算可重用的结果

#### 策略 3: 减少 GC 压力

- 减少对象分配
- 使用值类型（struct）
- 及时释放大对象

### 3. 性能目标

| 场景 | 目标时间 | 目标内存 | 目标 GC |
|------|---------|---------|---------|
| 小型脚本（500行） | < 100ms | < 5MB | < 5 次 |
| 中型项目（3000行） | < 500ms | < 20MB | < 10 次 |
| 大型脚本（5000行） | < 800ms | < 50MB | < 20 次 |

## 常见问题

### Q1: 为什么小型脚本性能没有显著提升？

**A**: 小型脚本的解析时间主要受以下因素影响：
- JIT 编译开销（首次运行）
- 系统负载
- 冷启动效应

**建议**:
- 运行多次取平均值
- 使用 BenchmarkDotNet 进行准确测量
- 关注中大型文件的性能提升

### Q2: 如何选择合适的缓存大小？

**A**: StringCache 的缓存大小取决于：
- 代码中唯一标识符的数量
- 可用内存大小
- 缓存命中率要求

**建议**:
- 默认 10000 条目适用于大多数场景
- 监控缓存命中率（目标 > 50%）
- 根据实际情况调整

### Q3: 对象池何时归还资源？

**A**: 对象池资源归还时机：
- CharBufferPool: Dispose 时自动归还（using 模式）
- TokenListPool: 手动调用 Return() 方法

**建议**:
- 优先使用 using 模式
- 确保 finally 块中归还资源
- 避免长期持有池化对象

### Q4: 如何处理递归深度限制？

**A**: 递归深度限制（500 层）足够处理正常代码。如果遇到限制：
- 检查代码是否有过深的嵌套
- 重构代码，减少嵌套层次
- 使用中间变量简化表达式

**不建议**: 增加递归深度限制（可能导致栈溢出）

### Q5: 性能优化是否影响向后兼容性？

**A**: 不影响。所有优化都是内部实现，API 保持不变：
- 现有代码无需修改
- 所有测试继续通过
- 行为完全一致

## 参考资料

- [架构文档](./ARCHITECTURE.md#性能优化架构)
- [CLI 指南](./CLI_GUIDE.md#性能监控和基准测试)
- [性能优化规范](../specs/001-parser-performance/)
- [基准测试代码](../Old8Lang.Benchmarks/)

---

**最后更新**: 2026-02-18
**版本**: 1.0.0
