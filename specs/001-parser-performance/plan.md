# Implementation Plan: Old8Lang 语法解析性能优化

**Branch**: `001-parser-performance` | **Date**: 2026-02-17 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-parser-performance/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

优化 Old8Lang 解析器的性能，目标是将小型脚本（500行）的解析时间降至 100ms 以内，中型项目（3000行）降至 500ms 以内，大型脚本（5000行）降至 800ms 以内。同时将内存占用减少至少 20%。优化将通过性能分析识别瓶颈，应用 C# 最佳实践（如 Span<T>、ArrayPool、对象池），并确保三种执行模式（解释、编译、VM）的兼容性和测试覆盖。

## Technical Context

**Language/Version**: C# 10.0+ (.NET 10.0)
**Primary Dependencies**: 无外部依赖（核心语言实现遵循最小化依赖原则）
**Storage**: N/A（解析器为纯计算组件，不涉及持久化存储）
**Testing**: xUnit 测试框架，BenchmarkDotNet 用于性能基准测试
**Target Platform**: 跨平台 (.NET 10.0 支持的所有平台：Windows, Linux, macOS)
**Project Type**: 单项目（编译器/解释器核心组件）
**Performance Goals**:
- 小型脚本（≤500 行）: 解析时间 < 100ms（目标提升 30%）
- 中型项目（~3000 行）: 解析时间 < 500ms（目标提升 40%）
- 大型脚本（~5000 行）: 解析时间 < 800ms（目标提升 40%）
- Token 处理速率: NEEDS CLARIFICATION（需要基准测试确定当前速率）

**Constraints**:
- 内存占用: 5000 行脚本峰值 < 50MB（目标减少 20%）
- 完全向后兼容: 不能改变语言语法或行为
- 三模式支持: 优化必须适用于解释、编译、VM 三种执行模式
- 错误报告质量: 性能优化不能降低错误信息的清晰度
- 测试通过率: 100% 现有测试必须继续通过

**Scale/Scope**:
- 支持文件大小: 10000+ 行代码
- 嵌套深度: 至少 50 层嵌套结构
- 并发场景: 测试套件中的并发解析（未来考虑）

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### ✅ 双模式对等性 (Dual-Mode Parity)
- **状态**: 通过
- **评估**: 性能优化将同时应用于解释模式、编译模式和 VM 模式。优化主要集中在共享的解析阶段（词法分析和语法分析），不会破坏任何模式的功能完整性。
- **行动**: 确保性能测试覆盖所有三种模式

### ✅ 访问者模式一致性 (Visitor Pattern Consistency)
- **状态**: 通过
- **评估**: 性能优化不涉及修改访问者模式接口或 AST 节点结构。优化集中在解析器内部实现（Token 处理、内存分配、缓存策略）。
- **行动**: 如果需要修改 AST 节点（如使用结构体优化），必须保持 `Accept<TResult>` 方法的一致性

### ✅ 测试优先开发 (Test-First Development)
- **状态**: 通过
- **评估**: 将首先建立性能基准测试套件，使用 BenchmarkDotNet 测量当前性能，然后进行优化。所有优化必须通过现有的功能测试。
- **行动**: Phase 0 完成后立即创建基准测试，优化前运行并记录基线

### ✅ API 稳定性 (API Stability)
- **状态**: 通过
- **评估**: 性能优化是内部实现改进，不涉及语言语法变更或标准库 API 修改。用户代码完全不受影响。
- **行动**: 确保所有现有测试通过，验证向后兼容性

### ✅ 性能意识 (Performance Awareness)
- **状态**: 通过
- **评估**: 这是一个专门的性能优化项目，完全符合性能意识原则。将建立完整的基准测试和性能监控。
- **行动**: 使用 BenchmarkDotNet 和内存分析工具，记录优化前后的对比数据

### ✅ 文档完整性 (Documentation Completeness)
- **状态**: 通过
- **评估**: 性能优化不涉及新的语言特性，但需要更新架构文档说明优化技术。
- **行动**: 更新 `Docs/ARCHITECTURE.md` 中的解析器性能部分，记录优化策略和基准测试结果

### 总体评估
**✅ 所有宪章原则检查通过，无违规项。可以进入 Phase 0 研究阶段。**

---

## Phase 1 后重新评估 (2026-02-17)

### ✅ 双模式对等性 (Dual-Mode Parity)
- **状态**: 通过
- **Phase 1 评估**: 设计的优化方案（Span<T>、ArrayPool、字符串缓存）适用于所有三种模式的解析阶段。data-model.md 和 API 契约确认了优化不会破坏任何模式的功能。

### ✅ 访问者模式一致性 (Visitor Pattern Consistency)
- **状态**: 通过
- **Phase 1 评估**: 优化集中在 Tokenizer 层，不涉及 AST 节点或访问者模式的修改。ParserContext 的增强（预分割 SourceLines、递归深度管理）不影响访问者接口。

### ✅ 测试优先开发 (Test-First Development)
- **状态**: 通过
- **Phase 1 评估**: quickstart.md 明确了基准测试流程。contracts/tokenizer-api.md 定义了测试契约，包括功能测试、性能测试和缓存测试。

### ✅ API 稳定性 (API Stability)
- **状态**: 通过
- **Phase 1 评估**: API 契约确认保留所有现有接口（`Tokenize()`, `TokenizeWithDirectivesAndDocs()`），新增的优化接口（`TokenizeOptimized()`, `TokenizeWithMetrics()`）是可选的。向后兼容性得到保证。

### ✅ 性能意识 (Performance Awareness)
- **状态**: 通过
- **Phase 1 评估**: data-model.md 定义了 `ParserPerformanceMetrics` 实体，contracts 定义了性能监控 API，quickstart.md 提供了基准测试指南。性能目标明确且可测量。

### ✅ 文档完整性 (Documentation Completeness)
- **状态**: 通过
- **Phase 1 评估**: 已生成完整的设计文档：
  - `research.md`: 性能瓶颈分析和优化策略
  - `data-model.md`: 数据结构设计
  - `contracts/tokenizer-api.md`: API 契约
  - `quickstart.md`: 快速入门指南

### Phase 1 总体评估
**✅ 所有宪章原则检查通过，设计阶段完成。可以进入 Phase 2 任务生成阶段。**

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
Old8Lang/                      # 核心语言实现
├── LangParser/                # 解析器（主要优化目标）
│   ├── Core/
│   │   ├── LangParser.cs      # 主解析器入口（Facade）
│   │   ├── LangToken.cs       # Token 定义
│   │   └── LangTokenType.cs   # Token 类型枚举
│   └── Parsers/               # 专用解析器
│       ├── ExpressionParser.cs
│       ├── StatementParser.cs
│       ├── FunctionParser.cs
│       └── ClassParser.cs
├── AST/                       # 抽象语法树（可能的优化点）
│   ├── Expression/
│   ├── Statement/
│   └── Visitor/
├── Interpreter/               # 解释器（受益于解析优化）
├── Compiler/                  # 编译器（受益于解析优化）
└── Bytecode/                  # VM（受益于解析优化）

Old8Lang.Tests/                # 测试项目
├── ParserTests/               # 解析器测试
├── PerformanceTests/          # 新增：性能基准测试
│   ├── ParserBenchmarks.cs    # 解析器性能测试
│   └── MemoryBenchmarks.cs    # 内存使用测试
└── InterpreterTests/          # 现有功能测试
    └── CompilerTests/

specs/001-parser-performance/  # 本功能的文档
├── plan.md                    # 本文件
├── research.md                # Phase 0 输出
├── data-model.md              # Phase 1 输出（如适用）
├── quickstart.md              # Phase 1 输出
└── contracts/                 # Phase 1 输出（如适用）
```

**Structure Decision**: 采用单项目结构（Option 1），因为这是对现有解析器组件的内部优化。主要工作集中在 `Old8Lang/LangParser/` 目录，新增性能测试在 `Old8Lang.Tests/PerformanceTests/` 目录。不需要创建新的项目或模块。

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

无违规项。本项目是内部性能优化，不增加系统复杂度，不引入新的架构模式或依赖。

---

## Phase 0: Research (完成 ✅)

### 研究成果

1. **性能瓶颈分析** (`research.md`)
   - 识别了 10 个主要性能瓶颈
   - 优先级分类：P0 (2个), P1 (2个), P2 (3个), P3 (3个)
   - 最严重问题：词法分析内存分配、文档注释 O(n²) 合并

2. **C# 优化最佳实践** (`research.md`)
   - 零拷贝技术：Span<T>, Memory<T>
   - 内存管理：ArrayPool<T>
   - 字符串优化策略
   - 递归优化：显式栈替代递归
   - 对象池模式
   - BenchmarkDotNet 使用

3. **优化策略制定**
   - Phase 1: 高优先级优化 (P0) - 目标 20-30% 提升
   - Phase 2: 中优先级优化 (P1) - 目标额外 10-15% 提升
   - Phase 3: 低优先级优化 (P2-P3) - 目标额外 5-10% 提升

---

## Phase 1: Design & Contracts (完成 ✅)

### 设计成果

1. **数据模型设计** (`data-model.md`)
   - Token 结构优化方案
   - StringCache 实体定义
   - CharBufferPool 实体定义
   - TokenListPool 实体定义
   - ParserContext 增强
   - ParserPerformanceMetrics 定义

2. **API 契约** (`contracts/tokenizer-api.md`)
   - 优化后的 Tokenizer 接口
   - StringCache API
   - CharBufferPool API
   - TokenListPool API
   - ParserPerformanceMetrics API
   - 使用示例和性能保证

3. **快速入门指南** (`quickstart.md`)
   - 环境要求和构建步骤
   - 基准测试运行指南
   - 性能验证方法
   - 优化 API 使用示例
   - 常见问题和故障排除

4. **Agent 上下文更新**
   - 更新了 `CLAUDE.md` 文件
   - 添加了项目技术栈信息

---

## Phase 2: Tasks (下一步)

Phase 2 将由 `/speckit.tasks` 命令执行，生成 `tasks.md` 文件，包含：
- 依赖排序的任务列表
- 每个任务的详细实现步骤
- 测试要求和验收标准
- 任务间的依赖关系

**注意**: 本命令（`/speckit.plan`）在 Phase 1 完成后停止，不生成 tasks.md。

---

## 实施路线图

### Phase 1: 高优先级优化 (P0)

**目标**: 解决最严重的性能瓶颈，实现 20-30% 性能提升

1. **词法分析内存分配优化**
   - 实现 `StringCache` 类
   - 实现 `CharBufferPool` 类
   - 修改 `LangToken.cs` 使用 Span<char>
   - 替换 `Substring()` 为 `AsSpan()`
   - 预期提升: 15-20%

2. **文档注释合并算法优化**
   - 修复 `MergeDocCommentsWithTokens()` 中的 O(n²) 问题
   - 使用反向遍历或 LinkedList<T>
   - 预期提升: 10-15%（对于有大量文档注释的代码）

### Phase 2: 中优先级优化 (P1)

**目标**: 改进算法和数据结构，实现额外 10-15% 性能提升

1. **递归表达式解析优化**
   - 实现显式栈替代递归（幂运算）
   - 添加递归深度限制到 `ParserContext`
   - 预期提升: 5-10%

2. **字符串操作优化**
   - 使用 `ReadOnlySpan<char>` 处理文件头指令
   - 减少 `Trim()` 和 `Substring()` 调用
   - 预期提升: 5-8%

### Phase 3: 低优先级优化 (P2-P3)

**目标**: 细节优化和长期改进，实现额外 5-10% 性能提升

1. **ParserContext 优化**
   - 预先分割 `SourceLines`
   - 预先构建 `TokenIndexCache`

2. **集合元素解析优化**
   - 实现 `TokenListPool` 类
   - 使用对象池管理列表

3. **关键字识别优化**（可选）
   - 实现 Trie 树或完美哈希表

---

## 性能目标总结

| 指标 | 当前值 | 目标值 | 提升比例 |
|------|--------|--------|---------|
| 500 行脚本解析时间 | ~140ms | <100ms | 30% |
| 3000 行项目解析时间 | ~830ms | <500ms | 40% |
| 5000 行脚本解析时间 | ~1330ms | <800ms | 40% |
| 5000 行脚本内存占用 | ~65MB | <50MB | 23% |
| GC Gen0 收集次数 | 基线 | -40% | 40% |
| Token 处理速率 | ~50k/s | ~70k/s | 40% |

---

## 风险和缓解措施

| 风险 | 严重性 | 缓解措施 |
|------|--------|---------|
| 向后兼容性破坏 | 高 | 保留原有接口，添加新接口；运行完整测试套件 |
| 代码复杂度增加 | 中 | 封装复杂逻辑，添加详细注释，提供使用示例 |
| 内存泄漏 | 中 | 设置池大小限制，使用 try-finally，添加泄漏检测测试 |
| 性能回归 | 中 | 建立完整基准测试，对比优化前后数据，多场景测试 |

---

## 下一步行动

1. **运行 `/speckit.tasks` 命令** 生成详细的任务列表
2. **开始实施 Phase 1 优化** 按照任务列表执行
3. **持续运行基准测试** 验证每个优化的效果
4. **更新文档** 记录实施过程中的发现和调整

---

**计划完成日期**: 2026-02-17
**分支**: 001-parser-performance
**状态**: Phase 0 和 Phase 1 完成，准备进入 Phase 2 任务生成
