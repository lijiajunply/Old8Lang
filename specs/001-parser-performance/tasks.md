# Tasks: Old8Lang 语法解析性能优化

**Input**: Design documents from `/specs/001-parser-performance/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: 本项目遵循测试优先开发原则，所有优化必须先建立基准测试，然后进行实现。

**Organization**: 任务按用户故事组织，每个故事可以独立实现和测试。

**⚠️ 重要更新**: 所有性能基准测试使用现有的 `Old8Lang.Benchmarks/` 项目，而非创建新的测试目录。

## Format: `[ID] [P?] [Story] Description`

- **[P]**: 可以并行运行（不同文件，无依赖）
- **[Story]**: 任务所属的用户故事（US1, US2, US3）
- 包含精确的文件路径

## Path Conventions

本项目采用单项目结构：
- **核心实现**: `Old8Lang/LangParser/` - 解析器优化
- **优化工具**: `Old8Lang/LangParser/Optimization/` - 新增优化类
- **性能测试**: `Old8Lang.Benchmarks/` - 性能基准测试（现有项目）
- **单元测试**: `Old8Lang.Tests/` - 功能测试
- **文档**: `Docs/` - 架构文档更新

---

## Phase 1: Setup (基础设施)

**Purpose**: 项目初始化和基准测试框架准备

- [ ] T001 创建优化工具目录 Old8Lang/LangParser/Optimization/
- [ ] T002 验证 BenchmarkDotNet 已添加到 Old8Lang.Benchmarks 项目
- [ ] T003 [P] 创建测试数据生成器 Old8Lang.Benchmarks/TestDataGenerator.cs
- [ ] T004 [P] 使用 TestDataGenerator 生成小型测试脚本（500行）Old8Lang.Benchmarks/TestData/small_script_500.old8
- [ ] T005 [P] 使用 TestDataGenerator 生成中型测试脚本（3000行）Old8Lang.Benchmarks/TestData/medium_project_3000.old8
- [ ] T006 [P] 使用 TestDataGenerator 生成大型测试脚本（5000行）Old8Lang.Benchmarks/TestData/large_script_5000.old8

---

## Phase 2: Foundational (阻塞性前置条件)

**Purpose**: 建立性能基准测试，为所有用户故事提供验证基础

**⚠️ CRITICAL**: 必须完成此阶段才能开始任何用户故事的实现

- [ ] T007 扩展 ParserBenchmarkTests：添加小型脚本基准测试（500行）Old8Lang.Benchmarks/ParserBenchmarkTests.cs
- [ ] T008 [P] 扩展 ParserBenchmarkTests：添加中型项目基准测试（3000行）Old8Lang.Benchmarks/ParserBenchmarkTests.cs
- [ ] T009 [P] 扩展 ParserBenchmarkTests：添加大型脚本基准测试（5000行）Old8Lang.Benchmarks/ParserBenchmarkTests.cs
- [ ] T010 [P] 扩展 MemoryUsageTests：添加内存使用监控基准测试 Old8Lang.Benchmarks/MemoryUsageTests.cs
- [ ] T011 运行所有基准测试并记录基线性能数据（包括解析时间、内存使用、Token 处理速率、GC 收集次数）到 specs/001-parser-performance/baseline-results.md
- [ ] T012 验证所有现有单元测试通过 dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj

**Checkpoint**: 基准测试框架就绪，基线数据已记录，可以开始用户故事实现

---

## Phase 3: User Story 1 - 快速解析小型脚本 (Priority: P1) 🎯 MVP

**Goal**: 优化小型脚本（100-500行）的解析性能，目标解析时间 < 100ms，性能提升 30%

**Independent Test**: 运行扩展后的 ParserBenchmarkTests（500行测试），验证解析时间从 ~140ms 降至 <100ms

### 实现 User Story 1

#### 1.1 字符串缓存实现（P0 优化）

- [ ] T013 [P] [US1] 创建 StringCache 类 Old8Lang/LangParser/Optimization/StringCache.cs
- [ ] T014 [P] [US1] 创建 CacheStatistics 结构 Old8Lang/LangParser/Optimization/CacheStatistics.cs
- [ ] T015 [US1] 在 StringCache 中实现 GetOrAdd 方法（使用 ConcurrentDictionary）
- [ ] T016 [US1] 在 StringCache 中实现 GetStatistics 方法
- [ ] T017 [US1] 在 StringCache 中实现 Clear 方法

#### 1.2 字符缓冲区池实现（P0 优化）

- [ ] T018 [P] [US1] 创建 CharBufferPool 类 Old8Lang/LangParser/Optimization/CharBufferPool.cs
- [ ] T019 [US1] 实现 CharBufferPool.RentedBuffer 结构（IDisposable）
- [ ] T020 [US1] 实现 CharBufferPool.Rent 方法（使用 ArrayPool<char>）

#### 1.3 Tokenizer 优化（P0 优化）

- [ ] T021 [US1] 在 LangToken.cs 中添加 Create 工厂方法（使用 StringCache）Old8Lang/LangParser/LangToken.cs
- [ ] T022 [US1] 优化数字字面量解析：使用 Span<char> 替代 StringBuilder Old8Lang/LangParser/LangToken.cs (行 180-212)
- [ ] T023 [US1] 优化字符串字面量解析：使用 CharBufferPool Old8Lang/LangParser/LangToken.cs (行 256-350)
- [ ] T024 [US1] 优化标识符解析：使用 StringCache Old8Lang/LangParser/LangToken.cs (行 644-706)
- [ ] T025 [US1] 优化文件头指令解析：使用 ReadOnlySpan<char> 替代 Substring Old8Lang/LangParser/LangToken.cs (行 968-975)
- [ ] T026 [US1] 优化 Unicode 转义序列：使用 Span<char> Old8Lang/LangParser/LangToken.cs (行 1186-1263)

#### 1.4 文档注释合并优化（P0 优化）

- [ ] T027 [US1] 修复 MergeDocCommentsWithTokens 的 O(n²) 问题：使用反向遍历 Old8Lang/LangParser/LangToken.cs (行 785-846)

#### 1.5 测试和验证

- [ ] T028 [US1] 运行 ParserBenchmarkTests（500行测试）验证性能提升 dotnet run --project Old8Lang.Benchmarks --configuration Release
- [ ] T029 [US1] 运行所有现有测试验证向后兼容性 dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj
- [ ] T030 [US1] 验证 StringCache 命中率 > 50%（使用 GetStatistics）
- [ ] T031 [US1] 验证内存分配减少 30-50%（使用 MemoryDiagnoser）

**Checkpoint**: User Story 1 完成，小型脚本解析时间 < 100ms，可以独立测试和部署

---

## Phase 4: User Story 2 - 高效解析中大型项目 (Priority: P2)

**Goal**: 优化中大型项目（1000-5000行）的解析性能，目标 3000 行 < 500ms，5000 行 < 800ms

**Independent Test**: 运行扩展后的 ParserBenchmarkTests（3000行和5000行测试），验证性能提升 40%

### 实现 User Story 2

#### 2.1 递归表达式解析优化（P1 优化）

- [ ] T032 [US2] 在 ParserContext 中添加递归深度管理字段 Old8Lang/LangParser/Core/ParserContext.cs
- [ ] T033 [US2] 实现 EnterRecursion 和 ExitRecursion 方法 Old8Lang/LangParser/Core/ParserContext.cs
- [ ] T034 [US2] 优化幂运算解析：使用显式栈替代递归 Old8Lang/LangParser/Parsers/ExpressionParser.cs (行 201-230)
- [ ] T035 [US2] 优化三元表达式解析：减少递归深度 Old8Lang/LangParser/Parsers/ExpressionParser.cs (行 82-116)
- [ ] T036 [US2] 在所有递归解析方法中添加深度检查

#### 2.2 ParserContext 优化（P2 优化）

- [ ] T037 [US2] 预先分割 SourceLines 在构造函数中 Old8Lang/LangParser/Core/ParserContext.cs (行 42-55)
- [ ] T038 [US2] 优化 TokenIndexCache 初始化：在构造函数中预先构建 Old8Lang/LangParser/Core/TokenIndexCache.cs

#### 2.3 集合元素解析优化（P2 优化）

- [ ] T039 [P] [US2] 创建 TokenListPool 类 Old8Lang/LangParser/Optimization/TokenListPool.cs
- [ ] T040 [US2] 实现 TokenListPool.Rent 方法（使用 ObjectPool<List<LangToken>>）
- [ ] T041 [US2] 实现 TokenListPool.Return 方法
- [ ] T042 [US2] 在 Tokenizer 中使用 TokenListPool Old8Lang/LangParser/LangToken.cs (行 94)

#### 2.4 测试和验证

- [ ] T043 [US2] 运行 ParserBenchmarkTests（3000行测试）验证 < 500ms dotnet run --project Old8Lang.Benchmarks --configuration Release
- [ ] T044 [US2] 运行 ParserBenchmarkTests（5000行测试）验证 < 800ms dotnet run --project Old8Lang.Benchmarks --configuration Release
- [ ] T045 [US2] 验证深层嵌套表达式（50层）不会栈溢出
- [ ] T046 [US2] 运行所有现有测试验证向后兼容性 dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj

**Checkpoint**: User Story 2 完成，中大型项目解析性能达标，可以独立测试

---

## Phase 5: User Story 3 - 内存效率优化 (Priority: P3)

**Goal**: 优化内存占用，目标 5000 行脚本峰值内存 < 50MB，内存减少 20%

**Independent Test**: 运行扩展后的 MemoryUsageTests，验证内存占用从 ~65MB 降至 <50MB

### 实现 User Story 3

#### 3.1 性能指标监控（支持 US3）

- [ ] T047 [P] [US3] 创建 ParserPerformanceMetrics 类 Old8Lang/LangParser/Optimization/ParserPerformanceMetrics.cs
- [ ] T048 [US3] 实现性能指标收集方法（时间、内存、GC）
- [ ] T049 [US3] 实现 ToString 方法格式化输出性能指标

#### 3.2 优化 API 实现

- [ ] T050 [US3] 添加 TokenizeOptimized 方法到 LangTokenizer Old8Lang/LangParser/LangToken.cs
- [ ] T051 [US3] 添加 TokenizeWithMetrics 方法到 LangTokenizer Old8Lang/LangParser/LangToken.cs
- [ ] T052 [US3] 在 TokenizeOptimized 中集成 StringCache 和 CharBufferPool

#### 3.3 内存泄漏检测

- [ ] T053 [P] [US3] 创建内存泄漏检测测试 Old8Lang.Benchmarks/MemoryLeakTest.cs
- [ ] T054 [US3] 测试连续解析 100 个脚本后内存释放
- [ ] T055 [US3] 测试对象池归还逻辑（CharBufferPool, TokenListPool）

#### 3.4 测试和验证

- [ ] T056 [US3] 运行 MemoryUsageTests 验证 5000 行脚本内存 < 50MB dotnet run --project Old8Lang.Benchmarks --configuration Release
- [ ] T057 [US3] 验证 GC Gen0 收集次数减少 40%
- [ ] T058 [US3] 验证内存泄漏测试通过
- [ ] T059 [US3] 运行所有现有测试验证向后兼容性 dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj

**Checkpoint**: User Story 3 完成，内存效率达标，所有用户故事独立可测

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: 跨用户故事的改进和文档更新

### 6.1 文档更新

- [ ] T060 [P] 更新架构文档：添加性能优化章节 Docs/ARCHITECTURE.md
- [ ] T061 [P] 更新 CLI 指南：添加性能监控命令 Docs/CLI_GUIDE.md
- [ ] T062 [P] 创建性能优化指南 Docs/PERFORMANCE_OPTIMIZATION.md

### 6.2 代码质量

- [ ] T063 [P] 添加 XML 文档注释到所有优化类
- [ ] T064 代码审查和重构：确保代码可读性
- [ ] T065 运行静态分析工具（Roslyn Analyzers）

### 6.3 最终验证

- [ ] T066 运行完整基准测试套件并生成报告 dotnet run --project Old8Lang.Benchmarks --configuration Release
- [ ] T067 对比基线数据验证所有性能目标达成
- [ ] T068 运行 quickstart.md 中的所有验证步骤
- [ ] T069 运行完整测试套件 dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj
- [ ] T070 生成性能对比报告 specs/001-parser-performance/final-results.md

### 6.4 边界条件测试

- [ ] T071 [P] 添加极深嵌套表达式测试（50层）到 Old8Lang.Benchmarks/ParserBenchmarkTests.cs
- [ ] T072 [P] 添加大量重复模式测试（10000个赋值语句）到 Old8Lang.Benchmarks/ParserBenchmarkTests.cs
- [ ] T073 [P] 添加超大型文件测试（10000+行）到 Old8Lang.Benchmarks/ParserBenchmarkTests.cs
- [ ] T074 [P] 添加语法错误场景性能测试到 Old8Lang.Benchmarks/ParserBenchmarkTests.cs
- [ ] T075 [P] 添加并发解析场景测试到 Old8Lang.Benchmarks/ParserBenchmarkTests.cs

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: 无依赖 - 可以立即开始
- **Foundational (Phase 2)**: 依赖 Setup 完成 - 阻塞所有用户故事
- **User Stories (Phase 3-5)**: 所有依赖 Foundational 完成
  - User Story 1 (P1): 可以在 Foundational 后立即开始 - 无其他故事依赖
  - User Story 2 (P2): 可以在 Foundational 后立即开始 - 技术上无依赖，但建议在 US1 后执行以重用优化工具
  - User Story 3 (P3): 可以在 Foundational 后立即开始 - 技术上无依赖，但建议在 US1 和 US2 后执行以重用所有优化
- **Polish (Phase 6)**: 依赖所有用户故事完成

### User Story Dependencies

- **User Story 1 (P1)**: 在 Foundational 后可开始 - 无其他故事依赖
- **User Story 2 (P2)**: 在 Foundational 后可开始 - 技术上无依赖（可并行），但实践中建议在 US1 后执行
- **User Story 3 (P3)**: 在 Foundational 后可开始 - 技术上无依赖（可并行），但实践中建议在 US1 和 US2 后执行

**说明**: US2 和 US3 在技术上可以与 US1 并行开发（不同文件），但按顺序执行可以重用 US1 创建的优化工具（StringCache, CharBufferPool），减少重复工作。

### Within Each User Story

- **User Story 1**:
  - T013-T017 (StringCache) 可以并行
  - T018-T020 (CharBufferPool) 可以并行
  - T021-T026 (Tokenizer 优化) 依赖 StringCache 和 CharBufferPool
  - T027 (文档注释优化) 可以独立进行
  - T028-T031 (测试) 依赖所有实现完成

- **User Story 2**:
  - T032-T036 (递归优化) 可以独立进行
  - T037-T038 (ParserContext 优化) 可以独立进行
  - T039-T042 (TokenListPool) 可以独立进行
  - T043-T046 (测试) 依赖所有实现完成

- **User Story 3**:
  - T047-T049 (ParserPerformanceMetrics) 可以独立进行
  - T050-T052 (优化 API) 依赖 US1 和 US2 的优化工具
  - T053-T055 (内存泄漏检测) 可以独立进行
  - T056-T059 (测试) 依赖所有实现完成

### Parallel Opportunities

- **Setup (Phase 1)**: T003-T006 可以并行（不同文件）
- **Foundational (Phase 2)**: T008-T010 可以并行（不同基准测试方法）
- **User Story 1**: T013-T014, T018 可以并行（不同类）
- **User Story 2**: T039 可以与其他任务并行
- **User Story 3**: T047, T053 可以并行
- **Polish**: T060-T062, T071-T075 可以并行（不同文件）

---

## Parallel Example: User Story 1

```bash
# 并行创建优化工具类：
Task: "创建 StringCache 类 Old8Lang/LangParser/Optimization/StringCache.cs"
Task: "创建 CacheStatistics 结构 Old8Lang/LangParser/Optimization/CacheStatistics.cs"
Task: "创建 CharBufferPool 类 Old8Lang/LangParser/Optimization/CharBufferPool.cs"

# 等待上述完成后，并行优化 Tokenizer：
Task: "优化数字字面量解析 Old8Lang/LangParser/LangToken.cs (行 180-212)"
Task: "优化字符串字面量解析 Old8Lang/LangParser/LangToken.cs (行 256-350)"
Task: "优化标识符解析 Old8Lang/LangParser/LangToken.cs (行 644-706)"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. 完成 Phase 1: Setup (T001-T006)
2. 完成 Phase 2: Foundational (T007-T012) - **关键：建立基准测试**
3. 完成 Phase 3: User Story 1 (T013-T031)
4. **停止并验证**: 测试 User Story 1 独立工作
5. 如果准备好，部署/演示

**预期结果**: 小型脚本解析时间 < 100ms，性能提升 30%

### Incremental Delivery

1. 完成 Setup + Foundational → 基准测试就绪
2. 添加 User Story 1 → 独立测试 → 部署/演示（MVP！）
3. 添加 User Story 2 → 独立测试 → 部署/演示
4. 添加 User Story 3 → 独立测试 → 部署/演示
5. 每个故事增加价值而不破坏之前的故事

### Parallel Team Strategy

如果有多个开发者：

1. 团队一起完成 Setup + Foundational
2. Foundational 完成后：
   - 开发者 A: User Story 1（P0 优化）
   - 开发者 B: User Story 2（P1 优化）
   - 开发者 C: User Story 3（性能监控）
3. 故事独立完成和集成

---

## Performance Goals Summary

| 用户故事 | 性能目标 | 验证方法 |
|---------|---------|---------|
| US1 | 500 行脚本 < 100ms（提升 30%） | Old8Lang.Benchmarks/ParserBenchmarkTests.cs（500行测试） |
| US2 | 3000 行项目 < 500ms（提升 40%）<br>5000 行脚本 < 800ms（提升 40%） | Old8Lang.Benchmarks/ParserBenchmarkTests.cs（3000行和5000行测试） |
| US3 | 5000 行脚本内存 < 50MB（减少 20%）<br>GC Gen0 减少 40% | Old8Lang.Benchmarks/MemoryUsageTests.cs |

---

## Notes

- **[P] 任务** = 不同文件，无依赖，可以并行
- **[Story] 标签** = 将任务映射到特定用户故事以便追溯
- 每个用户故事应该可以独立完成和测试
- 在实现前验证基准测试失败（显示性能问题）
- 在每个任务或逻辑组后提交
- 在任何检查点停止以独立验证故事
- 避免：模糊任务、同文件冲突、破坏独立性的跨故事依赖
- **重要**: 所有性能测试使用现有的 `Old8Lang.Benchmarks/` 项目

---

**任务总数**: 75（从 77 减少到 75，合并了重复的目录创建任务）
**User Story 1 任务数**: 19 (T013-T031)
**User Story 2 任务数**: 15 (T032-T046)
**User Story 3 任务数**: 13 (T047-T059)
**并行机会**: 20+ 任务标记为 [P]
**建议 MVP 范围**: Phase 1 + Phase 2 + Phase 3 (User Story 1)

---

**生成日期**: 2026-02-17（修订版）
**分支**: 001-parser-performance
**状态**: 准备开始实施
**修订说明**: 修正性能测试路径，使用现有的 Old8Lang.Benchmarks/ 项目；明确 Token 处理速率测量；澄清用户故事依赖关系
