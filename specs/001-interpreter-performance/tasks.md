# Tasks: 解释器模式性能优化

**Input**: Design documents from `/specs/001-interpreter-performance/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/performance-monitoring-api.md

**Tests**: 本特性采用测试优先开发（TDD）方法，所有优化都需要先建立基准测试和性能基线。

**Organization**: 任务按用户故事组织，每个故事可以独立实现和测试。

## Format: `[ID] [P?] [Story] Description`

- **[P]**: 可以并行运行（不同文件，无依赖）
- **[Story]**: 任务所属的用户故事（US1, US2, US3）
- 包含精确的文件路径

## Path Conventions

本项目采用单一项目结构：
- 核心代码: `Old8Lang/Interpreter/`
- 测试代码: `Old8Lang.Tests/Interpreter/Performance/`
- 基准测试: `Old8Lang.Benchmarks/`

---

## Phase 1: Setup (共享基础设施)

**Purpose**: 项目初始化和基础结构搭建

- [ ] T001 验证 Old8Lang.Benchmarks 项目配置，确保 BenchmarkDotNet 依赖已正确安装
- [ ] T002 [P] 创建性能测试目录结构 Old8Lang.Tests/Interpreter/Performance/
- [ ] T003 [P] 创建性能监控数据模型文件 Old8Lang/Interpreter/PerformanceMetrics.cs
- [ ] T004 [P] 创建性能监控配置文件 Old8Lang/Interpreter/PerformanceMonitorConfig.cs
- [ ] T005 创建测试脚本目录 TestScripts/Performance/ 用于存放性能测试用的 .old8 脚本

---

## Phase 2: Foundational (阻塞性前置条件)

**Purpose**: 核心基础设施，必须在任何用户故事实现之前完成

**⚠️ CRITICAL**: 在此阶段完成之前，不能开始任何用户故事的工作

### 基准测试基础设施

- [ ] T006 在 Old8Lang.Benchmarks/InterpreterBenchmarks.cs 中创建基准测试基类 InterpreterBenchmarkBase
- [ ] T007 [P] 创建变量查找基准测试 Old8Lang.Benchmarks/VariableLookupBenchmark.cs
- [ ] T008 [P] 创建循环执行基准测试 Old8Lang.Benchmarks/LoopExecutionBenchmark.cs
- [ ] T009 [P] 创建函数调用基准测试 Old8Lang.Benchmarks/FunctionCallBenchmark.cs
- [ ] T010 运行基准测试建立性能基线，保存结果到 specs/001-interpreter-performance/baseline-results.md

### 性能监控基础设施

- [ ] T011 实现 PerformanceMetrics 数据模型 Old8Lang/Interpreter/PerformanceMetrics.cs（包含所有字段和验证）
- [ ] T012 [P] 实现 FunctionMetrics 数据模型 Old8Lang/Interpreter/FunctionMetrics.cs
- [ ] T013 [P] 实现 ScopeMetrics 数据模型 Old8Lang/Interpreter/ScopeMetrics.cs
- [ ] T014 [P] 实现 ObjectPoolStats 数据模型 Old8Lang/Interpreter/ObjectPoolStats.cs
- [ ] T015 实现 IPerformanceMonitor 接口 Old8Lang/Interpreter/IPerformanceMonitor.cs
- [ ] T016 实现 PerformanceMonitor 基础类 Old8Lang/Interpreter/PerformanceMonitor.cs（启动、停止、重置功能）
- [ ] T017 在 LangInterpreter.cs 中集成 PerformanceMonitor（添加可选的 monitor 参数）

### 测试脚本准备

- [ ] T018 [P] 创建小型测试脚本 TestScripts/Performance/small-script-50lines.old8（50行，包含变量、运算、函数）
- [ ] T019 [P] 创建嵌套循环测试脚本 TestScripts/Performance/nested-loops.old8（100x100 循环）
- [ ] T020 [P] 创建变量查找测试脚本 TestScripts/Performance/variable-lookup.old8（大量变量查找）

**Checkpoint**: 基础设施就绪 - 用户故事实现现在可以并行开始

---

## Phase 3: User Story 1 - 快速执行小型脚本 (Priority: P1) 🎯 MVP

**Goal**: 优化小型脚本（10-100行）的执行性能，使50行脚本执行时间 <100ms，性能提升 ≥50%

**Independent Test**: 运行 small-script-50lines.old8 并测量执行时间，验证 <100ms 且比基线提升 ≥50%

### 基准测试（测试优先）

> **NOTE: 先编写这些测试，确保它们失败（性能未达标），然后再实现优化**

- [ ] T021 [P] [US1] 编写小型脚本性能测试 Old8Lang.Tests/Interpreter/Performance/SmallScriptPerformanceTests.cs
- [ ] T022 [P] [US1] 编写变量查找性能测试 Old8Lang.Tests/Interpreter/Performance/VariableLookupPerformanceTests.cs
- [ ] T023 [P] [US1] 编写循环执行性能测试 Old8Lang.Tests/Interpreter/Performance/LoopExecutionPerformanceTests.cs
- [ ] T024 [US1] 运行测试验证当前性能未达标（测试应该失败）

### 变量查找优化实现

- [ ] T025 [US1] 实现 VariableCache 类 Old8Lang/Interpreter/VariableCache.cs（LRU 缓存，最多1000条目）
- [ ] T026 [US1] 在 VariateManager.cs 中集成 VariableCache（查找时先检查缓存）
- [ ] T027 [US1] 实现作用域链扁平化优化 VariateManager.cs（缓存常用外层作用域变量）
- [ ] T028 [US1] 添加全局变量快速查找表 VariateManager.cs（专门的全局变量字典）
- [ ] T029 [US1] 在 PerformanceMonitor 中添加变量查找跟踪（RecordVariableLookup 方法）
- [ ] T030 [US1] 运行变量查找基准测试验证 ≥40% 性能提升

### 循环执行优化实现

- [ ] T031 [US1] 在 InterpreterVisitor.cs 中识别简单循环模式（for 循环计数器）
- [ ] T032 [US1] 实现简单循环特殊化执行路径 InterpreterVisitor.cs（优化的循环执行）
- [ ] T033 [US1] 实现循环不变量提升 InterpreterVisitor.cs（将不变计算移到循环外）
- [ ] T034 [US1] 在 PerformanceMonitor 中添加循环迭代计数（LoopIterationCount 字段）
- [ ] T035 [US1] 运行循环执行基准测试验证 ≥30% 性能提升

### 验证和集成

- [ ] T036 [US1] 运行所有 US1 性能测试验证性能目标达成
- [ ] T037 [US1] 运行现有解释器测试确保功能正确性未被破坏
- [ ] T038 [US1] 使用 small-script-50lines.old8 进行端到端测试，验证 <100ms 执行时间
- [ ] T039 [US1] 更新基准测试结果文档 specs/001-interpreter-performance/us1-results.md

**Checkpoint**: 此时 User Story 1 应该完全功能正常且可独立测试，小型脚本性能达标

---

## Phase 4: User Story 2 - 处理中等规模程序 (Priority: P2)

**Goal**: 优化中等规模程序（500-2000行）的执行性能，使1000行程序执行时间 <2s，性能提升 ≥40%

**Independent Test**: 运行包含10个类、50个函数的1000行程序，验证执行时间 <2s 且比基线提升 ≥40%

### 测试脚本和基准测试

- [ ] T040 [P] [US2] 创建中等规模测试程序 TestScripts/Performance/medium-program-1000lines.old8（1000行，10类，50函数）
- [ ] T041 [P] [US2] 创建递归测试脚本 TestScripts/Performance/recursive-calls.old8（递归深度100层）
- [ ] T042 [P] [US2] 编写中等规模程序性能测试 Old8Lang.Tests/Interpreter/Performance/MediumProgramPerformanceTests.cs
- [ ] T043 [P] [US2] 编写递归调用性能测试 Old8Lang.Tests/Interpreter/Performance/RecursiveCallPerformanceTests.cs
- [ ] T044 [US2] 运行测试验证当前性能未达标

### 函数调用优化实现

- [ ] T045 [US2] 优化函数调用栈管理 InterpreterVisitor.cs（减少栈帧创建开销）
- [ ] T046 [US2] 实现函数调用缓存 InterpreterVisitor.cs（缓存常用函数引用）
- [ ] T047 [US2] 优化闭包变量捕获 CapturedScope.cs（减少闭包创建开销）
- [ ] T048 [US2] 在 PerformanceMonitor 中添加函数调用跟踪（RecordFunctionCall 方法）
- [ ] T049 [US2] 运行函数调用基准测试验证性能提升

### 递归深度优化

- [ ] T050 [US2] 实现递归深度控制 InterpreterVisitor.cs（跟踪递归深度，支持至少100层）
- [ ] T051 [US2] 优化递归调用的栈使用 InterpreterVisitor.cs（减少每层递归的内存占用）
- [ ] T052 [US2] 添加递归深度监控 PerformanceMonitor.cs（FunctionMetrics.RecursionDepth）
- [ ] T053 [US2] 运行递归调用测试验证支持100层深度

### 对象创建优化

- [ ] T054 [US2] 扩展 ObjectPool 支持更多类型 ObjectPool.cs（添加常用 AST 节点类型）
- [ ] T055 [US2] 在 ObjectPoolManager 中添加作用域对象池（VariateScope 对象复用）
- [ ] T056 [US2] 实现对象池统计 ObjectPoolStats.cs（跟踪池使用情况）
- [ ] T057 [US2] 在 PerformanceMonitor 中集成对象池统计
- [ ] T058 [US2] 运行对象创建基准测试验证内存使用优化

### 验证和集成

- [ ] T059 [US2] 运行所有 US2 性能测试验证性能目标达成
- [ ] T060 [US2] 运行现有解释器测试确保功能正确性
- [ ] T061 [US2] 使用 medium-program-1000lines.old8 进行端到端测试，验证 <2s 执行时间
- [ ] T062 [US2] 更新基准测试结果文档 specs/001-interpreter-performance/us2-results.md

**Checkpoint**: 此时 User Stories 1 和 2 都应该独立工作，中等规模程序性能达标

---

## Phase 5: User Story 3 - 长时间运行的脚本 (Priority: P3)

**Goal**: 确保长时间运行脚本（5分钟以上）的内存使用稳定，增长率 <5%，无性能退化

**Independent Test**: 运行5分钟数据处理脚本，监控内存使用和执行速度，验证内存增长 <5%

### 测试脚本和基准测试

- [ ] T063 [P] [US3] 创建长时间运行测试脚本 TestScripts/Performance/long-running-5min.old8（5分钟数据处理）
- [ ] T064 [P] [US3] 创建大量迭代测试脚本 TestScripts/Performance/million-iterations.old8（100万次迭代）
- [ ] T065 [P] [US3] 编写内存稳定性测试 Old8Lang.Tests/Interpreter/Performance/MemoryStabilityTests.cs
- [ ] T066 [P] [US3] 编写长时间运行性能测试 Old8Lang.Tests/Interpreter/Performance/LongRunningPerformanceTests.cs
- [ ] T067 [US3] 运行测试验证当前内存使用情况

### 内存管理优化

- [ ] T068 [US3] 实现内存使用监控 PerformanceMonitor.cs（跟踪 MemoryUsageBytes）
- [ ] T069 [US3] 优化对象池回收策略 ObjectPoolManager.cs（定期清理未使用对象）
- [ ] T070 [US3] 实现作用域对象自动回收 VariateManager.cs（作用域退出时清理缓存）
- [ ] T071 [US3] 添加 GC 回收监控 PerformanceMonitor.cs（GCCollectionCount 字段）
- [ ] T072 [US3] 运行内存稳定性测试验证内存增长 <5%

### 性能稳定性优化

- [ ] T073 [US3] 实现缓存大小限制 VariableCache.cs（防止缓存无限增长）
- [ ] T074 [US3] 实现 LRU 淘汰策略 VariableCache.cs（自动清理最少使用的缓存条目）
- [ ] T075 [US3] 优化符号表缓存 SymbolTableCache.cs（添加大小限制和淘汰策略）
- [ ] T076 [US3] 添加性能退化检测 PerformanceMonitor.cs（跟踪执行速度变化）
- [ ] T077 [US3] 运行长时间运行测试验证性能稳定

### 验证和集成

- [ ] T078 [US3] 运行所有 US3 性能测试验证性能目标达成
- [ ] T079 [US3] 运行现有解释器测试确保功能正确性
- [ ] T080 [US3] 使用 long-running-5min.old8 进行端到端测试，验证内存稳定性
- [ ] T081 [US3] 更新基准测试结果文档 specs/001-interpreter-performance/us3-results.md

**Checkpoint**: 所有用户故事现在都应该独立功能正常，长时间运行脚本内存稳定

---

## Phase 6: 性能监控功能

**Purpose**: 实现完整的性能监控和报告功能（FR-005, FR-007）

### 性能报告实现

- [ ] T082 [P] 实现 IPerformanceReporter 接口 Old8Lang/Interpreter/IPerformanceReporter.cs
- [ ] T083 [P] 实现 PerformanceReporter 类 Old8Lang/Interpreter/PerformanceReporter.cs
- [ ] T084 [P] 实现文本格式报告生成 PerformanceReporter.cs（GenerateTextReport 方法）
- [ ] T085 [P] 实现 JSON 格式报告生成 PerformanceReporter.cs（GenerateJsonReport 方法）
- [ ] T086 [P] 实现 CSV 格式报告生成 PerformanceReporter.cs（GenerateCsvReport 方法）
- [ ] T087 实现报告保存功能 PerformanceReporter.cs（SaveReport 方法）

### 详细监控功能

- [ ] T088 实现详细监控配置 PerformanceMonitorConfig.cs（DetailedMonitoring 选项）
- [ ] T089 实现函数级别性能跟踪 PerformanceMonitor.cs（收集 FunctionMetrics）
- [ ] T090 实现作用域级别性能跟踪 PerformanceMonitor.cs（收集 ScopeMetrics）
- [ ] T091 实现采样率控制 PerformanceMonitor.cs（SampleRate 配置）
- [ ] T092 实现性能数据聚合 PerformanceMonitor.cs（计算平均值、最大值、最小值）

### CLI 集成

- [ ] T093 在 Old8Lang.App/Program.cs 中添加 --perf 命令行选项
- [ ] T094 在 Old8Lang.App/Program.cs 中添加 --perf-detailed 命令行选项
- [ ] T095 在 Old8Lang.App/Program.cs 中添加 --perf-output 命令行选项（指定报告输出文件）
- [ ] T096 实现 CLI 性能报告显示 Old8Lang.App/Commands/FromFileCommand.cs
- [ ] T097 编写 CLI 性能监控集成测试 Old8Lang.Tests/CLI/PerformanceMonitoringTests.cs

### 验证

- [ ] T098 编写性能监控单元测试 Old8Lang.Tests/Interpreter/Performance/PerformanceMonitorTests.cs
- [ ] T099 编写性能报告生成测试 Old8Lang.Tests/Interpreter/Performance/PerformanceReporterTests.cs
- [ ] T100 验证性能监控开销 <1%（运行基准测试对比启用/禁用监控）
- [ ] T101 测试所有报告格式（文本、JSON、CSV）的正确性

**Checkpoint**: 性能监控功能完整可用，开发者可以通过 CLI 查看性能指标

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: 跨用户故事的改进和文档更新

### 文档更新

- [ ] T102 [P] 更新 Docs/ARCHITECTURE.md 记录性能优化策略和实现细节
- [ ] T103 [P] 更新 Docs/CLI_GUIDE.md 记录性能监控 CLI 选项
- [ ] T104 [P] 创建性能优化最佳实践文档 Docs/PERFORMANCE_BEST_PRACTICES.md
- [ ] T105 [P] 更新 README.md 添加性能优化特性说明

### 代码质量

- [ ] T106 代码审查和重构（确保符合 C# 编码规范）
- [ ] T107 添加 XML 文档注释到所有公共 API
- [ ] T108 运行静态分析工具（Roslyn 分析器）确保无警告
- [ ] T109 性能回归测试集成到 CI/CD（在 CI 中运行基准测试）

### 最终验证

- [ ] T110 运行完整的测试套件（所有单元测试、集成测试、性能测试）
- [ ] T111 验证所有用户故事的接受标准都已满足
- [ ] T112 运行 quickstart.md 中的所有示例验证文档正确性
- [ ] T113 生成最终性能报告对比优化前后的性能提升

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: 无依赖 - 可以立即开始
- **Foundational (Phase 2)**: 依赖 Setup 完成 - 阻塞所有用户故事
- **User Stories (Phase 3-5)**: 都依赖 Foundational 完成
  - 用户故事可以并行进行（如果有足够人力）
  - 或按优先级顺序进行（P1 → P2 → P3）
- **性能监控 (Phase 6)**: 可以与 User Stories 并行，但建议在 US1 完成后开始
- **Polish (Phase 7)**: 依赖所有期望的用户故事完成

### User Story Dependencies

- **User Story 1 (P1)**: Foundational 完成后可以开始 - 无其他故事依赖
- **User Story 2 (P2)**: Foundational 完成后可以开始 - 建议 US1 完成后开始（可以复用 US1 的优化）
- **User Story 3 (P3)**: Foundational 完成后可以开始 - 建议 US1 和 US2 完成后开始（依赖内存优化）

### Within Each User Story

- 基准测试必须先编写并失败
- 优化实现按顺序进行（变量查找 → 循环执行 → 函数调用 → 内存管理）
- 每个优化完成后立即运行对应的基准测试验证
- 故事完成前运行完整验证

### Parallel Opportunities

- **Setup 阶段**: T002, T003, T004 可以并行
- **Foundational 阶段**:
  - T007, T008, T009 可以并行（不同的基准测试文件）
  - T012, T013, T014 可以并行（不同的数据模型文件）
  - T018, T019, T020 可以并行（不同的测试脚本）
- **User Story 1**: T021, T022, T023 可以并行（不同的测试文件）
- **User Story 2**: T040, T041 可以并行，T042, T043 可以并行
- **User Story 3**: T063, T064 可以并行，T065, T066 可以并行
- **性能监控**: T082-T086 可以并行（不同的报告格式）
- **文档更新**: T102, T103, T104, T105 可以并行

---

## Parallel Example: User Story 1

```bash
# 并行启动 User Story 1 的所有基准测试:
Task: "编写小型脚本性能测试 Old8Lang.Tests/Interpreter/Performance/SmallScriptPerformanceTests.cs"
Task: "编写变量查找性能测试 Old8Lang.Tests/Interpreter/Performance/VariableLookupPerformanceTests.cs"
Task: "编写循环执行性能测试 Old8Lang.Tests/Interpreter/Performance/LoopExecutionPerformanceTests.cs"

# 这些任务可以同时进行，因为它们操作不同的文件
```

---

## Implementation Strategy

### MVP First (仅 User Story 1)

1. 完成 Phase 1: Setup
2. 完成 Phase 2: Foundational（关键 - 阻塞所有故事）
3. 完成 Phase 3: User Story 1
4. **停止并验证**: 独立测试 User Story 1
5. 如果准备好，部署/演示

### Incremental Delivery

1. 完成 Setup + Foundational → 基础就绪
2. 添加 User Story 1 → 独立测试 → 部署/演示（MVP！）
3. 添加 User Story 2 → 独立测试 → 部署/演示
4. 添加 User Story 3 → 独立测试 → 部署/演示
5. 添加性能监控功能 → 完整功能
6. 每个故事都增加价值而不破坏之前的故事

### Parallel Team Strategy

如果有多个开发者:

1. 团队一起完成 Setup + Foundational
2. Foundational 完成后:
   - 开发者 A: User Story 1（变量查找和循环优化）
   - 开发者 B: User Story 2（函数调用和递归优化）
   - 开发者 C: 性能监控功能（Phase 6）
3. User Story 3 在 US1 和 US2 完成后开始（需要它们的优化基础）
4. 故事独立完成和集成

---

## Task Summary

**Total Tasks**: 113

**Tasks by Phase**:
- Phase 1 (Setup): 5 tasks
- Phase 2 (Foundational): 15 tasks
- Phase 3 (User Story 1): 19 tasks
- Phase 4 (User Story 2): 23 tasks
- Phase 5 (User Story 3): 19 tasks
- Phase 6 (性能监控): 20 tasks
- Phase 7 (Polish): 12 tasks

**Tasks by User Story**:
- User Story 1: 19 tasks
- User Story 2: 23 tasks
- User Story 3: 19 tasks
- 共享/基础设施: 52 tasks

**Parallel Opportunities**: 约 40 个任务标记为 [P]，可以并行执行

**Independent Test Criteria**:
- **US1**: 运行 small-script-50lines.old8，执行时间 <100ms，性能提升 ≥50%
- **US2**: 运行 medium-program-1000lines.old8，执行时间 <2s，性能提升 ≥40%
- **US3**: 运行 long-running-5min.old8，内存增长 <5%，无性能退化

**Suggested MVP Scope**: Phase 1 + Phase 2 + Phase 3 (User Story 1)
- 这将提供小型脚本的显著性能提升
- 可以独立验证和部署
- 为后续优化奠定基础

---

## Notes

- [P] 任务 = 不同文件，无依赖
- [Story] 标签将任务映射到特定用户故事以便追溯
- 每个用户故事都应该可以独立完成和测试
- 在实现前验证测试失败（TDD）
- 每个任务或逻辑组后提交
- 在任何检查点停止以独立验证故事
- 避免: 模糊任务、相同文件冲突、破坏独立性的跨故事依赖

---

## Format Validation

✅ 所有任务都遵循检查清单格式: `- [ ] [TaskID] [P?] [Story?] Description with file path`
✅ 任务按用户故事组织，支持独立实现
✅ 包含精确的文件路径
✅ 标记了并行机会
✅ 定义了独立测试标准
✅ 建议了 MVP 范围
