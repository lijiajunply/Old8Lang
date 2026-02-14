# Tasks: 更新文档以支持三种执行模式

**Input**: Design documents from `/specs/001-update-docs-modes/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: 本功能不包含测试任务，因为这是纯文档更新项目。文档质量通过代码示例的实际运行验证。

**Organization**: 任务按用户故事组织，每个用户故事可以独立完成和验证。

## Format: `[ID] [P?] [Story] Description`

- **[P]**: 可以并行运行（不同文件，无依赖）
- **[Story]**: 任务所属的用户故事（US1, US2, US3）
- 包含具体文件路径

## Path Conventions

- 文档文件位于项目根目录和 `Docs/` 目录
- 参考代码位于 `Old8Lang/`, `Old8LangLib/` 等目录（只读，不修改）

---

## Phase 1: Setup (共享基础设施)

**目的**: 项目初始化和基础准备

- [x] T001 验证所有目标文档文件存在并可访问（CLAUDE.md, Docs/ARCHITECTURE.md, Docs/CLI_GUIDE.md, Docs/API_REFERENCE.md, Docs/LANGUAGE_FEATURES.md, Docs/PERFORMANCE_GUIDE.md）
- [x] T002 [P] 创建文档备份目录 `Docs/.backup/` 并备份所有待更新文档
- [x] T003 [P] 验证代码示例测试环境可用（dotnet build Old8Lang.sln 成功）

---

## Phase 2: Foundational (阻塞性前置条件)

**目的**: 所有用户故事依赖的核心基础工作

**⚠️ 关键**: 在此阶段完成前，不能开始任何用户故事的工作

- [x] T004 从 research.md 提取三种执行模式的完整对比信息（特点、性能、适用场景、命令参数）
- [x] T005 [P] 从 research.md 提取核心架构组件信息（Visitor、AST、Parser、TypeSystem）
- [x] T006 [P] 从 research.md 提取标准库模块清单（5个标准库项目的完整信息）
- [x] T007 [P] 从 research.md 提取包管理命令清单（所有执行、包管理、调试命令）
- [x] T008 创建执行模式对比表格（使用 contracts/execution-mode-comparison-table.md 模板）
- [x] T009 [P] 准备至少3个可运行的代码示例（每种执行模式至少1个）用于文档验证

**Checkpoint**: 基础信息收集完成 - 用户故事实施现在可以并行开始

---

## Phase 3: User Story 1 - 开发者了解三种执行模式的区别和使用场景 (Priority: P1) 🎯 MVP

**Goal**: 用户能够通过文档清楚理解三种执行模式的区别、特点和适用场景，并能选择合适的模式

**Independent Test**: 阅读更新后的文档，能够独立回答"我应该在什么情况下使用哪种模式"这个问题

### Implementation for User Story 1

- [x] T010 [P] [US1] 更新 CLAUDE.md 的"Run Old8Lang Code"章节，添加三种执行模式的说明和命令示例
- [x] T011 [P] [US1] 在 CLAUDE.md 的"High-Level Architecture"章节添加三种执行模式的核心处理管道流程图
- [x] T012 [US1] 在 Docs/ARCHITECTURE.md 创建新章节"Execution Modes"，详细说明三种模式的实现机制
- [x] T013 [US1] 在 Docs/ARCHITECTURE.md 的"Execution Modes"章节添加三种模式对比表格（使用 T008 创建的表格）
- [x] T014 [US1] 在 Docs/CLI_GUIDE.md 创建新章节"Execution Modes Comparison"，对比三种模式的命令行参数
- [x] T015 [US1] 在 Docs/CLI_GUIDE.md 添加每种模式的完整命令示例和使用场景说明
- [x] T016 [US1] 在 Docs/PERFORMANCE_GUIDE.md 创建新章节"Execution Mode Performance"，添加三种模式的性能对比和选择指南
- [x] T017 [US1] 在 Docs/LANGUAGE_FEATURES.md 添加"Mode-Specific Features"章节，说明不同模式下的功能差异（泛型、运算符重载、Python互操作）
- [x] T018 [US1] 为每种执行模式创建可运行的代码示例并添加到相关文档中
- [x] T019 [US1] 验证所有代码示例在对应的执行模式下可以成功运行（使用 dotnet run --project Old8Lang.App）
- [x] T020 [US1] 标注 Bytecode VM 模式为实验性功能，添加使用建议和风险警告

**Checkpoint**: 此时，用户故事1应该完全功能化并可独立测试 - 用户可以理解三种执行模式并做出选择

---

## Phase 4: User Story 2 - 开发者理解当前架构设计 (Priority: P2)

**Goal**: 贡献者和高级用户能够理解 Old8Lang 的最新架构设计，包括 Visitor 模式、AST 组织、Parser 结构和 TypeSystem

**Independent Test**: 阅读架构文档后，能够独立绘制出核心处理流程图或解释 Visitor 模式在项目中的应用

### Implementation for User Story 2

- [x] T021 [P] [US2] 更新 CLAUDE.md 的"Key Architectural Patterns"章节，详细说明 Visitor 模式的实现和三个主要 Visitor
- [x] T022 [P] [US2] 更新 CLAUDE.md 的"Directory Structure"章节，准确反映当前的项目组织方式（包括 Bytecode/ 目录）
- [x] T023 [US2] 在 Docs/ARCHITECTURE.md 更新"Visitor Pattern"章节，添加 IVisitor 接口定义和四个主要实现（InterpreterVisitor, CompilerVisitor, BytecodeVisitor, TypeInferenceVisitor）
- [x] T024 [US2] 在 Docs/ARCHITECTURE.md 更新"AST Node Organization"章节，说明 AST 节点的分类和组织结构（Expression/, Statement/）
- [x] T025 [US2] 在 Docs/ARCHITECTURE.md 更新"Parser Structure"章节，说明 Parser 的 Facade 模式和递归下降解析机制
- [x] T026 [US2] 在 Docs/ARCHITECTURE.md 更新"Type System"章节，说明 TypeChecker、TypeInferenceEngine、GenericTypeInference 的职责和使用场景
- [x] T027 [US2] 在 Docs/ARCHITECTURE.md 添加三种执行模式的详细实现位置（核心类、Visitor、文件路径）
- [x] T028 [US2] 在 Docs/ADVANCED_TOPICS.md 添加"Extending Old8Lang"章节，说明如何扩展 Visitor 模式和添加新的语言特性
- [x] T029 [US2] 更新所有架构文档中的代码位置引用，确保文件路径准确（格式：path/to/file.cs:line）
- [x] T030 [US2] 在 CLAUDE.md 的"Key Files to Understand"章节添加 Bytecode VM 相关的关键文件

**Checkpoint**: 此时，用户故事1和2都应该独立工作 - 用户可以理解执行模式并理解架构设计

---

## Phase 5: User Story 3 - 开发者查找最新的 API 方法和标准库 (Priority: P2)

**Goal**: 用户能够查找最新的 API 方法、标准库函数和包管理命令，并成功编写可运行的代码

**Independent Test**: 查找特定功能（如文件操作、网络请求）的 API 文档，并成功编写可运行的代码

### Implementation for User Story 3

- [ ] T031 [P] [US3] 在 Docs/API_REFERENCE.md 更新"Core Standard Library (Old8LangLib)"章节，添加所有12个模块的完整说明（Math, File, Crypto, Image, Regex, Terminal, ColorfulTerminal, Time, OS, CSV, Template, Vector）
- [ ] T032 [P] [US3] 在 Docs/API_REFERENCE.md 更新"Network Library (Old8Lang.NetLib)"章节，添加所有5个模块的完整说明（HTTP, WebSocket, MQTT, Socket, WebAPI）
- [ ] T033 [P] [US3] 在 Docs/API_REFERENCE.md 更新"Database Library (Old8Lang.DatabaseLib)"章节，添加所有5个模块的完整说明（MySQL, PostgreSQL, SQLite, InMemory, ORM）
- [ ] T034 [P] [US3] 在 Docs/API_REFERENCE.md 更新"Serialization Library (Old8Lang.SerializationLib)"章节，添加所有3个模块的完整说明（MessagePack, Protobuf, Factory）
- [ ] T035 [P] [US3] 在 Docs/API_REFERENCE.md 更新"Machine Learning Library (Old8Lang.MachineLearningLib)"章节，添加所有5个模块的完整说明（Classification, Regression, Clustering, DataLoader, Predictor）
- [ ] T036 [US3] 为每个标准库模块添加至少1个可运行的代码示例（使用 contracts/api-documentation-format.md 模板）
- [ ] T037 [US3] 在所有 API 文档中添加模式支持标注（✅ 解释模式 | ✅/❌ 编译模式 | ✅ VM 模式）
- [ ] T038 [US3] 在 Docs/CLI_GUIDE.md 更新"Package Management Commands"章节，添加所有11个包管理命令的详细说明（init, install, remove, restore, list, pack, unpack, sign, verify, cert, publish）
- [ ] T039 [US3] 在 Docs/CLI_GUIDE.md 添加"Debugging and Profiling Commands"章节，说明调试和性能分析命令（debug-start, debug-breakpoint, debug-control, profile）
- [ ] T040 [US3] 在 CLAUDE.md 的"Package Management Commands"章节更新命令示例，确保与最新实现一致
- [ ] T041 [US3] 验证所有 API 文档中的代码示例可以成功运行

**Checkpoint**: 所有用户故事现在都应该独立功能化 - 用户可以理解模式、架构和 API

---

## Phase 6: Polish & Cross-Cutting Concerns

**目的**: 影响多个用户故事的改进和最终验证

- [ ] T042 [P] 检查所有文档中的内部链接有效性（CLAUDE.md → Docs/*.md 的链接）
- [ ] T043 [P] 统一所有文档中的术语使用（中英文对照一致性）
- [ ] T044 [P] 统一所有文档中的代码块语言标注（old8lang, csharp, bash, text）
- [ ] T045 [P] 统一所有文档中的表格格式（对齐方式、列宽）
- [ ] T046 验证所有文档中的代码示例都有注释和预期输出
- [ ] T047 运行完整的文档质量检查（使用 data-model.md 中的验证规则）
- [ ] T048 [P] 更新 Docs/CHANGELOG.md，记录本次文档更新的所有变更
- [ ] T049 [P] 创建文档更新总结报告（覆盖率、变更文件列表、验证结果）
- [ ] T050 最终审查：确保所有功能需求（FR-001 到 FR-010）都已满足

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: 无依赖 - 可以立即开始
- **Foundational (Phase 2)**: 依赖 Setup 完成 - 阻塞所有用户故事
- **User Stories (Phase 3-5)**: 所有依赖 Foundational 阶段完成
  - 用户故事可以并行进行（如果有多人）
  - 或按优先级顺序进行（P1 → P2 → P2）
- **Polish (Phase 6)**: 依赖所有期望的用户故事完成

### User Story Dependencies

- **User Story 1 (P1)**: 可以在 Foundational (Phase 2) 后开始 - 不依赖其他故事
- **User Story 2 (P2)**: 可以在 Foundational (Phase 2) 后开始 - 不依赖其他故事（但建议在 US1 后进行，因为架构文档会引用执行模式）
- **User Story 3 (P2)**: 可以在 Foundational (Phase 2) 后开始 - 不依赖其他故事

### Within Each User Story

- 标记 [P] 的任务可以并行执行（不同文件）
- 未标记 [P] 的任务需要按顺序执行（同一文件或有依赖）
- 代码示例验证任务必须在相关文档编写完成后执行

### Parallel Opportunities

- Setup 阶段的所有 [P] 任务可以并行
- Foundational 阶段的所有 [P] 任务可以并行
- Foundational 完成后，所有用户故事可以并行开始（如果团队容量允许）
- 每个用户故事内的 [P] 任务可以并行
- Polish 阶段的所有 [P] 任务可以并行

---

## Parallel Example: User Story 1

```bash
# 并行启动 User Story 1 的所有 [P] 任务:
Task: "更新 CLAUDE.md 的 Run Old8Lang Code 章节"
Task: "更新 CLAUDE.md 的 High-Level Architecture 章节"

# 然后按顺序执行非并行任务:
Task: "在 Docs/ARCHITECTURE.md 创建 Execution Modes 章节"
Task: "添加三种模式对比表格"
# ... 等等
```

---

## Parallel Example: User Story 3

```bash
# 并行启动所有标准库文档更新:
Task: "更新 Core Standard Library 章节"
Task: "更新 Network Library 章节"
Task: "更新 Database Library 章节"
Task: "更新 Serialization Library 章节"
Task: "更新 Machine Learning Library 章节"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. 完成 Phase 1: Setup
2. 完成 Phase 2: Foundational（关键 - 阻塞所有故事）
3. 完成 Phase 3: User Story 1
4. **停止并验证**: 独立测试 User Story 1 - 用户能否理解三种执行模式？
5. 如果准备好，可以发布/演示

### Incremental Delivery

1. 完成 Setup + Foundational → 基础就绪
2. 添加 User Story 1 → 独立测试 → 发布/演示（MVP！）
3. 添加 User Story 2 → 独立测试 → 发布/演示
4. 添加 User Story 3 → 独立测试 → 发布/演示
5. 每个故事都增加价值而不破坏之前的故事

### Parallel Team Strategy

如果有多个开发者：

1. 团队一起完成 Setup + Foundational
2. Foundational 完成后：
   - 开发者 A: User Story 1（执行模式文档）
   - 开发者 B: User Story 2（架构文档）
   - 开发者 C: User Story 3（API 文档）
3. 故事独立完成并集成

---

## Task Summary

**Total Tasks**: 50

**Task Count per User Story**:
- Setup: 3 tasks
- Foundational: 6 tasks
- User Story 1 (P1): 11 tasks
- User Story 2 (P2): 10 tasks
- User Story 3 (P2): 11 tasks
- Polish: 9 tasks

**Parallel Opportunities Identified**:
- Setup: 2 parallel tasks (T002, T003)
- Foundational: 4 parallel tasks (T005, T006, T007, T009)
- User Story 1: 2 parallel tasks (T010, T011)
- User Story 2: 2 parallel tasks (T021, T022)
- User Story 3: 5 parallel tasks (T031-T035)
- Polish: 5 parallel tasks (T042-T045, T048, T049)

**Independent Test Criteria**:
- **US1**: 用户能够独立回答"我应该在什么情况下使用哪种模式"
- **US2**: 用户能够独立绘制核心处理流程图或解释 Visitor 模式应用
- **US3**: 用户能够查找 API 并成功编写可运行的代码

**Suggested MVP Scope**: User Story 1 only（执行模式文档）

---

## Format Validation

✅ **所有任务都遵循清单格式**:
- ✅ 每个任务都以 `- [ ]` 开头（markdown 复选框）
- ✅ 每个任务都有唯一的任务 ID（T001-T050）
- ✅ 并行任务标记 [P]
- ✅ 用户故事任务标记 [US1], [US2], [US3]
- ✅ 每个任务都包含具体的文件路径或明确的操作描述

---

## Notes

- [P] 任务 = 不同文件，无依赖
- [Story] 标签将任务映射到特定用户故事以便追溯
- 每个用户故事都应该可以独立完成和测试
- 在实施前验证代码示例失败（如果适用）
- 在每个任务或逻辑组后提交
- 在任何检查点停止以独立验证故事
- 避免：模糊任务、同文件冲突、破坏独立性的跨故事依赖
