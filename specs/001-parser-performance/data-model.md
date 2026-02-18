# Data Model: 解析器性能优化

**Feature**: 001-parser-performance
**Date**: 2026-02-17
**Status**: Phase 1 Design

## 1. 核心实体

### 1.1 Token 结构

#### 当前实现
```csharp
// Old8Lang/LangParser/LangToken.cs
public readonly record struct LangToken(
    string Value,
    LangTokenType Type,
    int Line,
    int Column
);
```

**问题**: `readonly record struct` 不可变，无法使用对象池。

#### 优化后设计

**选项 A: 保持不可变（推荐）**
```csharp
// 保持当前设计，优化 Token 创建过程
public readonly record struct LangToken(
    string Value,
    LangTokenType Type,
    int Line,
    int Column
)
{
    // 添加工厂方法，使用字符串缓存
    public static LangToken Create(ReadOnlySpan<char> value, LangTokenType type, int line, int column)
    {
        var cachedValue = StringCache.GetOrAdd(value);
        return new LangToken(cachedValue, type, line, column);
    }
}
```

**选项 B: 可变类（用于对象池）**
```csharp
// 仅在性能关键路径使用
public sealed class MutableToken : IResettable
{
    public string Value { get; set; } = string.Empty;
    public LangTokenType Type { get; set; }
    public int Line { get; set; }
    public int Column { get; set; }

    public void Reset()
    {
        Value = string.Empty;
        Type = LangTokenType.EndOfFile;
        Line = 0;
        Column = 0;
    }

    public LangToken ToImmutable() => new(Value, Type, Line, Column);
}
```

**决策**: 使用选项 A，保持不可变性，通过字符串缓存优化。

---

### 1.2 字符串缓存

#### 实体定义
```csharp
public sealed class StringCache
{
    private readonly ConcurrentDictionary<string, string> _cache;
    private readonly int _maxCacheSize;
    private int _currentSize;

    public StringCache(int maxCacheSize = 10000)
    {
        _cache = new ConcurrentDictionary<string, string>();
        _maxCacheSize = maxCacheSize;
        _currentSize = 0;
    }

    public string GetOrAdd(ReadOnlySpan<char> value)
    {
        // 只缓存短字符串（<= 64 字符）
        if (value.Length > 64)
        {
            return new string(value);
        }

        var key = new string(value);

        if (_cache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        if (Interlocked.Increment(ref _currentSize) <= _maxCacheSize)
        {
            return _cache.GetOrAdd(key, key);
        }

        Interlocked.Decrement(ref _currentSize);
        return key;
    }

    public void Clear()
    {
        _cache.Clear();
        _currentSize = 0;
    }
}
```

**字段说明**:
- `_cache`: 字符串缓存字典
- `_maxCacheSize`: 最大缓存大小（默认 10000）
- `_currentSize`: 当前缓存大小

**状态转换**:
1. 初始状态: 空缓存
2. 添加字符串: 检查大小限制
3. 缓存满: 不再添加新字符串
4. 清空: 重置为初始状态

**验证规则**:
- 字符串长度 <= 64 字符
- 缓存大小 <= maxCacheSize
- 线程安全

---

### 1.3 字符缓冲区池

#### 实体定义
```csharp
public sealed class CharBufferPool
{
    private static readonly ArrayPool<char> Pool = ArrayPool<char>.Shared;

    public struct RentedBuffer : IDisposable
    {
        private char[] _buffer;
        private readonly int _length;

        internal RentedBuffer(char[] buffer, int length)
        {
            _buffer = buffer;
            _length = length;
        }

        public Span<char> Span => _buffer.AsSpan(0, _length);

        public void Dispose()
        {
            if (_buffer != null)
            {
                Pool.Return(_buffer);
                _buffer = null!;
            }
        }
    }

    public static RentedBuffer Rent(int minimumLength)
    {
        var buffer = Pool.Rent(minimumLength);
        return new RentedBuffer(buffer, minimumLength);
    }
}
```

**字段说明**:
- `_buffer`: 租用的字符数组
- `_length`: 有效长度

**生命周期**:
1. 租用: `Rent(minimumLength)` 从池中获取缓冲区
2. 使用: 通过 `Span` 属性访问
3. 归还: `Dispose()` 自动归还到池

**使用示例**:
```csharp
using var buffer = CharBufferPool.Rent(256);
// 使用 buffer.Span
// 自动归还
```

---

### 1.4 Token 列表池

#### 实体定义
```csharp
public sealed class TokenListPool
{
    private static readonly ObjectPool<List<LangToken>> Pool =
        new DefaultObjectPool<List<LangToken>>(new TokenListPolicy(), 50);

    private class TokenListPolicy : IPooledObjectPolicy<List<LangToken>>
    {
        public List<LangToken> Create() => new List<LangToken>(128);

        public bool Return(List<LangToken> obj)
        {
            if (obj.Count > 10000) // 不池化过大的列表
            {
                return false;
            }

            obj.Clear();
            return true;
        }
    }

    public static List<LangToken> Rent()
    {
        var list = Pool.Get();
        list.Clear();
        return list;
    }

    public static void Return(List<LangToken> list)
    {
        Pool.Return(list);
    }
}
```

**字段说明**:
- `Pool`: 对象池实例
- 初始容量: 128 个 token
- 最大池化大小: 10000 个 token

**状态转换**:
1. 租用: 从池中获取列表（已清空）
2. 使用: 添加 token
3. 归还: 清空并返回池

---

### 1.5 解析器上下文

#### 实体定义
```csharp
public sealed class ParserContext
{
    // 现有字段
    public string SourceCode { get; }
    public List<LangToken> Tokens { get; }
    public int CurrentIndex { get; set; }

    // 新增：性能优化字段
    private string[]? _sourceLines;
    private readonly Lazy<TokenIndexCache> _tokenIndexCache;
    private int _recursionDepth;

    // 新增：字符串缓存
    public StringCache StringCache { get; }

    public ParserContext(string sourceCode, List<LangToken> tokens)
    {
        SourceCode = sourceCode;
        Tokens = tokens;
        CurrentIndex = 0;

        // 预先分割源代码行
        _sourceLines = sourceCode.Split('\n');

        // 延迟初始化索引缓存
        _tokenIndexCache = new Lazy<TokenIndexCache>(() =>
        {
            var cache = new TokenIndexCache(tokens);
            cache.BuildIndex();
            return cache;
        });

        StringCache = new StringCache();
        _recursionDepth = 0;
    }

    public string[] SourceLines => _sourceLines ?? Array.Empty<string>();

    public TokenIndexCache TokenIndex => _tokenIndexCache.Value;

    // 递归深度管理
    public void EnterRecursion()
    {
        if (++_recursionDepth > 500)
        {
            throw new SyntaxError("表达式嵌套过深，超过最大递归深度限制（500层）");
        }
    }

    public void ExitRecursion()
    {
        _recursionDepth--;
    }
}
```

**字段说明**:
- `_sourceLines`: 预先分割的源代码行（避免重复分割）
- `_tokenIndexCache`: 延迟初始化的 token 索引
- `_recursionDepth`: 当前递归深度
- `StringCache`: 字符串缓存实例

**状态转换**:
1. 初始化: 分割源代码行，创建缓存
2. 解析中: 递增/递减递归深度
3. 完成: 释放资源

**验证规则**:
- 递归深度 <= 500
- SourceLines 只分割一次
- TokenIndex 延迟构建

---

### 1.6 性能指标

#### 实体定义
```csharp
public sealed class ParserPerformanceMetrics
{
    public long TokenizationTimeMs { get; set; }
    public long ParsingTimeMs { get; set; }
    public long TotalTimeMs { get; set; }

    public long TokenCount { get; set; }
    public long SourceCodeLength { get; set; }

    public long MemoryAllocatedBytes { get; set; }
    public long PeakMemoryUsageBytes { get; set; }

    public int GCGen0Collections { get; set; }
    public int GCGen1Collections { get; set; }
    public int GCGen2Collections { get; set; }

    public double TokensPerSecond => TokenCount / (TotalTimeMs / 1000.0);
    public double CharsPerSecond => SourceCodeLength / (TotalTimeMs / 1000.0);

    public override string ToString()
    {
        return $"""
            解析性能指标:
            - 词法分析: {TokenizationTimeMs}ms
            - 语法分析: {ParsingTimeMs}ms
            - 总时间: {TotalTimeMs}ms
            - Token 数量: {TokenCount}
            - 源代码长度: {SourceCodeLength} 字符
            - Token/秒: {TokensPerSecond:F0}
            - 字符/秒: {CharsPerSecond:F0}
            - 内存分配: {MemoryAllocatedBytes / 1024.0:F2} KB
            - 峰值内存: {PeakMemoryUsageBytes / 1024.0:F2} KB
            - GC 收集: Gen0={GCGen0Collections}, Gen1={GCGen1Collections}, Gen2={GCGen2Collections}
            """;
    }
}
```

**字段说明**:
- `TokenizationTimeMs`: 词法分析时间
- `ParsingTimeMs`: 语法分析时间
- `TotalTimeMs`: 总时间
- `TokenCount`: Token 数量
- `SourceCodeLength`: 源代码长度
- `MemoryAllocatedBytes`: 内存分配量
- `PeakMemoryUsageBytes`: 峰值内存使用
- `GCGen0/1/2Collections`: GC 收集次数

**计算属性**:
- `TokensPerSecond`: Token 处理速率
- `CharsPerSecond`: 字符处理速率

---

## 2. 关系图

```
┌─────────────────────────────────────────────────────────────┐
│                      ParserContext                          │
│  - SourceCode: string                                       │
│  - Tokens: List<LangToken>                                  │
│  - StringCache: StringCache                                 │
│  - _sourceLines: string[]                                   │
│  - _tokenIndexCache: Lazy<TokenIndexCache>                  │
│  - _recursionDepth: int                                     │
└─────────────────────────────────────────────────────────────┘
                    │                    │
                    │ uses               │ uses
                    ▼                    ▼
        ┌───────────────────┐   ┌──────────────────┐
        │   StringCache     │   │ TokenIndexCache  │
        │  - _cache: Dict   │   │  - _typeIndex    │
        │  - _maxCacheSize  │   │  - _valueIndex   │
        └───────────────────┘   └──────────────────┘
                    │
                    │ caches
                    ▼
        ┌───────────────────┐
        │    LangToken      │
        │  - Value: string  │
        │  - Type: enum     │
        │  - Line: int      │
        │  - Column: int    │
        └───────────────────┘
                    │
                    │ created by
                    ▼
        ┌───────────────────────────┐
        │   LangTokenizer           │
        │  + Tokenize()             │
        │  + TokenizeWithDirectives │
        └───────────────────────────┘
                    │
                    │ uses
                    ▼
        ┌───────────────────────────┐
        │   CharBufferPool          │
        │  + Rent()                 │
        │  + Return()               │
        └───────────────────────────┘
                    │
                    │ uses
                    ▼
        ┌───────────────────────────┐
        │   ArrayPool<char>         │
        │  (.NET Framework)         │
        └───────────────────────────┘

        ┌───────────────────────────┐
        │   TokenListPool           │
        │  + Rent()                 │
        │  + Return()               │
        └───────────────────────────┘
                    │
                    │ uses
                    ▼
        ┌───────────────────────────┐
        │   ObjectPool<List<T>>     │
        │  (.NET Extensions)        │
        └───────────────────────────┘

        ┌───────────────────────────┐
        │ ParserPerformanceMetrics  │
        │  - TokenizationTimeMs     │
        │  - ParsingTimeMs          │
        │  - MemoryAllocatedBytes   │
        │  + TokensPerSecond        │
        └───────────────────────────┘
```

---

## 3. 数据流

### 3.1 Tokenization 流程

```
源代码 (string)
    │
    ▼
CharBufferPool.Rent(256)  ◄─── ArrayPool<char>
    │
    ▼
逐字符扫描
    │
    ├─ 关键字 ──► KeywordsByFirstChar (FrozenDictionary)
    │
    ├─ 标识符 ──► StringCache.GetOrAdd()
    │
    ├─ 数字 ────► Span<char> 切片 ──► new string()
    │
    ├─ 字符串 ──► CharBufferPool ──► new string()
    │
    └─ 运算符 ──► 直接创建 Token
    │
    ▼
TokenListPool.Rent()  ◄─── ObjectPool<List<LangToken>>
    │
    ▼
List<LangToken> (结果)
    │
    ▼
TokenListPool.Return() (内部列表)
```

### 3.2 Parsing 流程

```
List<LangToken>
    │
    ▼
ParserContext 初始化
    │
    ├─ 分割 SourceLines (一次性)
    │
    ├─ 创建 StringCache
    │
    └─ 延迟初始化 TokenIndexCache
    │
    ▼
递归下降解析
    │
    ├─ EnterRecursion() (检查深度)
    │
    ├─ 解析表达式/语句
    │
    └─ ExitRecursion()
    │
    ▼
AST (BlockStatement)
    │
    ▼
性能指标收集 (ParserPerformanceMetrics)
```

---

## 4. 验证规则

### 4.1 StringCache 验证

- ✅ 只缓存长度 <= 64 的字符串
- ✅ 缓存大小 <= maxCacheSize
- ✅ 线程安全（使用 ConcurrentDictionary）
- ✅ 缓存命中率 > 50%（对于常见标识符）

### 4.2 CharBufferPool 验证

- ✅ 租用的缓冲区必须归还（使用 `using` 语句）
- ✅ 缓冲区大小 >= 请求的最小长度
- ✅ 归还后不能再使用（通过 `Dispose` 模式保证）

### 4.3 TokenListPool 验证

- ✅ 租用的列表已清空
- ✅ 归还前列表大小 <= 10000（避免池化过大列表）
- ✅ 归还后列表被清空

### 4.4 ParserContext 验证

- ✅ SourceLines 只分割一次
- ✅ 递归深度 <= 500
- ✅ TokenIndexCache 延迟构建（首次使用时）
- ✅ StringCache 在整个解析过程中共享

### 4.5 性能指标验证

- ✅ TokenizationTimeMs + ParsingTimeMs ≈ TotalTimeMs
- ✅ TokenCount > 0
- ✅ SourceCodeLength > 0
- ✅ TokensPerSecond > 0
- ✅ MemoryAllocatedBytes >= 0

---

## 5. 性能目标

### 5.1 内存分配目标

| 场景 | 当前分配 | 目标分配 | 减少比例 |
|------|---------|---------|---------|
| 500 行脚本 | ~8 MB | ~5 MB | 37.5% |
| 3000 行项目 | ~45 MB | ~30 MB | 33.3% |
| 5000 行脚本 | ~65 MB | <50 MB | 23.1% |

### 5.2 GC 收集目标

| 场景 | 当前 Gen0 | 目标 Gen0 | 减少比例 |
|------|----------|----------|---------|
| 500 行脚本 | ~15 次 | ~8 次 | 46.7% |
| 3000 行项目 | ~80 次 | ~45 次 | 43.8% |
| 5000 行脚本 | ~120 次 | ~70 次 | 41.7% |

### 5.3 处理速率目标

| 指标 | 当前值 | 目标值 | 提升比例 |
|------|--------|--------|---------|
| Tokens/秒 | ~50,000 | ~70,000 | 40% |
| 字符/秒 | ~200,000 | ~280,000 | 40% |

---

## 6. 测试数据

### 6.1 测试用例

**小型脚本（500 行）**:
```old8
// 包含：
// - 100 个函数定义
// - 200 个变量声明
// - 150 个表达式
// - 50 个控制流语句
```

**中型项目（3000 行）**:
```old8
// 包含：
// - 10 个类定义
// - 50 个函数定义
// - 1000 个变量声明
// - 1500 个表达式
// - 500 个控制流语句
```

**大型脚本（5000 行）**:
```old8
// 包含：
// - 20 个类定义
// - 100 个函数定义
// - 2000 个变量声明
// - 2500 个表达式
// - 500 个控制流语句
```

### 6.2 边界条件

- 深层嵌套表达式（50 层）
- 大量字符串字面量（1000+ 个）
- 大量文档注释（500+ 行）
- 超大型文件（10000+ 行）

---

**数据模型完成日期**: 2026-02-17
**下一步**: 生成 API 契约和快速入门指南
