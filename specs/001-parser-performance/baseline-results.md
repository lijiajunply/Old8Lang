# 基线性能测试结果

**测试日期**: 2026-02-18
**分支**: 001-parser-performance
**测试环境**:
- .NET 版本: 10.0.3
- 运行时: .NET 10.0.3 (10.0.3, 10.0.326.7603), X64 RyuJIT x86-64-v3
- GC: Concurrent Workstation
- 硬件: AVX2+BMI1+BMI2+F16C+FMA+LZCNT+MOVBE

## 1. 解析性能基线

### 1.1 小型脚本（500 行）

根据 research.md 中的数据：

| 指标 | 当前值 | 目标值 | 提升目标 |
|------|--------|--------|---------|
| 解析时间 | ~140ms | <100ms | 30% |
| 内存占用 | ~8 MB | ~5 MB | 37.5% |
| GC Gen0 收集 | ~15 次 | ~8 次 | 46.7% |

**注**: 具体的基准测试数据需要通过 BenchmarkDotNet 完整运行后更新。

### 1.2 中型项目（3000 行）

根据 research.md 中的数据：

| 指标 | 当前值 | 目标值 | 提升目标 |
|------|--------|--------|---------|
| 解析时间 | ~830ms | <500ms | 40% |
| 内存占用 | ~45 MB | ~30 MB | 33.3% |
| GC Gen0 收集 | ~80 次 | ~45 次 | 43.8% |

### 1.3 大型脚本（5000 行）

根据 research.md 中的数据：

| 指标 | 当前值 | 目标值 | 提升目标 |
|------|--------|--------|---------|
| 解析时间 | ~1330ms | <800ms | 40% |
| 内存占用 | ~65 MB | <50 MB | 23.1% |
| GC Gen0 收集 | ~120 次 | ~70 次 | 41.7% |

## 2. Token 处理速率基线

根据 research.md 中的数据：

| 指标 | 当前值 | 目标值 | 提升目标 |
|------|--------|--------|---------|
| Tokens/秒 | ~50,000 | ~70,000 | 40% |
| 字符/秒 | ~200,000 | ~280,000 | 40% |

## 3. 性能瓶颈识别

根据 research.md 的分析，主要性能瓶颈包括：

### 3.1 高优先级（P0）

1. **词法分析内存分配**
   - 位置: `Old8Lang/LangParser/LangToken.cs`
   - 问题: 大量 `StringBuilder` 和 `Substring()` 调用
   - 影响: 30-50% 的内存分配

2. **文档注释合并 O(n²)**
   - 位置: `Old8Lang/LangParser/LangToken.cs` 行 785-846
   - 问题: `InsertRange(0, ...)` 导致 O(n²) 复杂度
   - 影响: 对于大量文档注释的代码性能严重下降

### 3.2 中优先级（P1）

1. **递归表达式解析链**
   - 位置: `Old8Lang/LangParser/Parsers/ExpressionParser.cs`
   - 问题: 深层嵌套可能导致栈溢出
   - 影响: 5-10% 性能提升空间

2. **字符串操作**
   - 位置: `Old8Lang/LangParser/LangToken.cs` 行 968-975
   - 问题: 多次 `Substring()` 和 `Trim()`
   - 影响: 5-8% 性能提升空间

### 3.3 低优先级（P2-P3）

1. **SourceLines 重复分割**
2. **集合元素解析对象池**
3. **TokenIndexCache 延迟构建**
4. **关键字识别优化**

## 4. 优化策略

### Phase 1: 高优先级优化（P0）
- 目标: 20-30% 性能提升
- 技术: Span<T>, ArrayPool<T>, 字符串缓存

### Phase 2: 中优先级优化（P1）
- 目标: 额外 10-15% 性能提升
- 技术: 显式栈替代递归, ReadOnlySpan<char>

### Phase 3: 低优先级优化（P2-P3）
- 目标: 额外 5-10% 性能提升
- 技术: 对象池, 预先构建索引

## 5. 测试数据文件

测试数据文件已生成：
- `Old8Lang.Benchmarks/TestData/small_script_500.old8` (500 行)
- `Old8Lang.Benchmarks/TestData/medium_project_3000.old8` (3000 行)
- `Old8Lang.Benchmarks/TestData/large_script_5000.old8` (5000 行)

## 6. 下一步

1. 完成 Phase 3: User Story 1 实施（T013-T031）
2. 实施字符串缓存和字符缓冲区池
3. 优化 Tokenizer 内存分配
4. 修复文档注释合并算法
5. 运行基准测试验证改进

---

**注**: 本文档基于 research.md 中的性能分析数据。完整的 BenchmarkDotNet 测试结果将在优化实施后更新。
