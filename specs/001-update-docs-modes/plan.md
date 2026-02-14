# Implementation Plan: 更新文档以支持三种执行模式

**Branch**: `001-update-docs-modes` | **Date**: 2026-02-14 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-update-docs-modes/spec.md`

## Summary

更新 Old8Lang 项目文档，使其完整覆盖三种执行模式（VM模式、解释模式、编译模式）的说明，并更新到最新的架构设计和 API 方法。主要工作包括：更新 CLAUDE.md 和 Docs/ 目录下的核心文档，添加三种模式的对比表格和使用指南，更新架构说明以反映 Visitor 模式和双模式执行机制，以及更新标准库 API 参考。

**技术方法**: 这是一个纯文档更新任务，不涉及代码实现。通过代码探索收集当前实现的准确信息，然后更新现有 Markdown 文档。

## Technical Context

**Language/Version**: C# 10.0+, .NET 10.0 (文档描述对象)
**Primary Dependencies**: N/A (纯文档更新，无代码依赖)
**Storage**: Markdown 文件存储在 `Docs/` 目录和项目根目录
**Testing**: 文档验证通过代码示例的实际运行测试
**Target Platform**: 文档面向所有平台用户（Windows, Linux, macOS）
**Project Type**: 文档项目（更新现有文档）
**Performance Goals**: 用户能在 5 分钟内理解三种模式差异，3 分钟内找到所需 API 文档
**Constraints**:
- 必须保持现有文档结构和风格
- 必须准确反映当前代码实现（不能虚构功能）
- 文档主要使用中文，保留技术术语英文原文
- 所有代码示例必须可运行
**Scale/Scope**:
- 更新 5-7 个主要文档文件
- 涵盖 3 种执行模式、86 个 AST 节点类型、5+ 个标准库模块
- 文档总量约 200KB+

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### 核心原则合规性检查

✅ **I. 双模式对等性 (Dual-Mode Parity)**
- **状态**: 合规
- **说明**: 文档更新将平等覆盖三种执行模式，不偏向任何一种模式
- **验证**: 每个模式都有完整的特点、场景、命令说明

✅ **II. 访问者模式一致性 (Visitor Pattern Consistency)**
- **状态**: 合规
- **说明**: 架构文档将准确描述 Visitor 模式的实现和三个主要 Visitor（InterpreterVisitor, CompilerVisitor, BytecodeVisitor）
- **验证**: 架构文档包含 Visitor 模式的完整说明

✅ **III. 测试优先开发 (Test-First Development)**
- **状态**: 合规
- **说明**: 文档中的所有代码示例将在编写后进行实际运行测试
- **验证**: 所有代码示例都能在对应的执行模式下成功运行（SC-007）

✅ **IV. API 稳定性 (API Stability)**
- **状态**: 合规
- **说明**: 文档更新不涉及 API 变更，仅记录现有 API
- **验证**: 文档准确反映当前 API，不引入破坏性变更

✅ **V. 性能意识 (Performance Awareness)**
- **状态**: 合规
- **说明**: 文档将包含三种模式的性能对比指导（FR-009）
- **验证**: 文档包含性能对比表格和选择指南

✅ **VI. 文档完整性 (Documentation Completeness)**
- **状态**: 合规 - 这正是本功能的核心目标
- **说明**: 更新文档以达到 100% 覆盖率（SC-002, SC-004）
- **验证**: 所有三种模式、架构组件、API 方法都有完整文档

### 技术标准合规性检查

✅ **代码质量**
- **状态**: N/A（纯文档更新，无代码变更）

✅ **测试要求**
- **状态**: 合规
- **说明**: 文档中的代码示例将通过实际运行验证
- **验证**: 使用 `dotnet run --project Old8Lang.App` 测试所有示例

✅ **依赖管理**
- **状态**: N/A（纯文档更新，无依赖变更）

### 开发工作流合规性检查

✅ **功能开发流程**
- **状态**: 合规
- **说明**: 已创建功能规格说明（spec.md），正在进行设计规划（plan.md）
- **验证**: 遵循 speckit 工作流

✅ **分支策略**
- **状态**: 合规
- **说明**: 使用功能分支 `001-update-docs-modes`
- **验证**: 分支命名符合 `[number]-feature-name` 约定

✅ **提交规范**
- **状态**: 待执行
- **说明**: 提交将使用 `docs(documentation): <description>` 格式
- **验证**: 提交时遵循约定

### 合规性总结

**所有宪章原则检查通过** ✅

本功能是纯文档更新，不涉及代码实现变更，因此大部分技术标准为 N/A。文档更新将严格遵循宪章第 VI 条"文档完整性"原则，确保所有语言特性、架构组件和 API 方法都有完整准确的文档说明。

## Project Structure

### Documentation (this feature)

```text
specs/001-update-docs-modes/
├── plan.md              # This file (/speckit.plan command output)
├── spec.md              # Feature specification
├── research.md          # Phase 0 output - 技术信息收集
├── data-model.md        # Phase 1 output - 文档结构模型
├── quickstart.md        # Phase 1 output - 文档更新快速指南
├── contracts/           # Phase 1 output - 文档模板和结构约定
└── checklists/
    └── requirements.md  # 规范质量检查清单
```

### Source Code (repository root)

```text
# 文档更新目标文件
Docs/
├── ARCHITECTURE.md          # 更新：架构设计、Visitor 模式、三种执行模式
├── CLI_GUIDE.md             # 更新：三种模式的命令行参数和示例
├── API_REFERENCE.md         # 更新：标准库 API 完整参考
├── LANGUAGE_FEATURES.md     # 更新：不同模式下的功能差异
├── MODE_COMPLETION_STATUS.md # 参考：三种模式完成度（已存在）
├── Mode_Support_Summary.md   # 参考：模式支持总结（已存在）
├── API_Mode_Comparison.md    # 参考：API 模式对比（已存在）
├── PERFORMANCE_GUIDE.md      # 更新：三种模式性能对比
└── ADVANCED_TOPICS.md        # 更新：高级架构主题

CLAUDE.md                     # 更新：添加三种模式说明和最新架构概述

# 参考代码（用于提取准确信息，不修改）
Old8Lang/
├── AST/
│   ├── LangExpression.cs
│   ├── OldStatement.cs
│   └── Visitor/
│       ├── InterpreterVisitor.cs
│       ├── CompilerVisitor.cs
│       ├── BytecodeVisitor.cs
│       └── Generated/IVisitor.generated.cs
├── Interpreter/
│   └── LangInterpreter.cs
├── Compiler/
│   └── Compiler.cs
├── Bytecode/
│   ├── BytecodeCompiler.cs
│   └── VM/VirtualMachine.Core.cs
├── LangParser/
│   └── LangParser.cs
└── TypeSystem/

Old8LangLib/                  # 核心标准库
Old8Lang.NetLib/              # 网络库
Old8Lang.DatabaseLib/         # 数据库库
Old8Lang.SerializationLib/    # 序列化库
Old8Lang.MachineLearningLib/  # 机器学习库

Old8Lang.App/
└── Commands/                 # 包管理和执行命令
```

**Structure Decision**: 这是一个文档更新项目，主要工作在 `Docs/` 目录和 `CLAUDE.md` 文件中进行。不涉及源代码修改，仅从源代码中提取准确的技术信息用于文档编写。

## Complexity Tracking

> **本功能无宪章违规，此部分为空**

本功能是纯文档更新，完全符合宪章所有原则，无需复杂性跟踪。

---

## Phase 0: Outline & Research

### 研究任务清单

基于技术上下文中的信息需求，需要进行以下研究：

1. **三种执行模式的详细对比**
   - 解释模式、编译模式、VM 模式的实现机制
   - 性能特征对比（启动时间、运行时性能、内存占用）
   - 功能支持差异（泛型、运算符重载、Python 互操作等）
   - 适用场景和选择指南

2. **核心架构组件的工作原理**
   - Visitor 模式的实现细节和扩展方式
   - AST 节点的组织结构和主要类型
   - Parser 的递归下降解析机制
   - TypeSystem 的类型检查和推断流程

3. **标准库模块的完整清单**
   - 每个标准库项目的功能范围
   - 主要类和方法的签名
   - 使用示例和最佳实践

4. **包管理系统的命令和工作流**
   - 所有包管理命令的详细说明
   - 项目模式 vs 全局模式
   - 包签名和验证机制

5. **文档组织和风格约定**
   - 现有文档的结构模式
   - Markdown 格式约定
   - 代码示例的编写规范
   - 中英文混排的处理方式

### 研究方法

- **代码探索**: 使用 Explore agent 深入分析代码库（已完成）
- **文档审查**: 阅读现有文档了解结构和风格
- **实际测试**: 运行三种模式的代码示例验证行为
- **性能测试**: 对比三种模式的性能特征

### 研究输出

所有研究结果将整合到 `research.md` 文件中，包括：
- 技术决策和理由
- 三种模式的详细对比表格
- 架构组件的说明和图示
- 标准库 API 清单
- 文档更新的具体内容规划

---

## Phase 1: Design & Contracts

### 数据模型 (data-model.md)

文档更新的"数据模型"是文档结构模型，定义：

1. **文档实体**
   - 执行模式文档（三种模式的说明）
   - 架构组件文档（Visitor、AST、Parser、TypeSystem）
   - API 参考文档（标准库模块和方法）
   - 命令行参考文档（CLI 命令和参数）

2. **文档关系**
   - CLAUDE.md 提供高层概述，链接到详细文档
   - ARCHITECTURE.md 详细说明架构，引用代码位置
   - CLI_GUIDE.md 提供命令参考，包含三种模式的使用示例
   - API_REFERENCE.md 提供 API 详细说明，按模块组织

3. **文档属性**
   - 标题层级（H1-H6）
   - 代码块（语言标注）
   - 表格（对比信息）
   - 链接（内部引用和外部链接）
   - 示例代码（可运行的 Old8Lang 代码）

### 文档契约 (contracts/)

定义文档更新的结构约定：

1. **执行模式对比表格模板**
   - 列：模式名称、特点、性能、适用场景、命令参数
   - 格式：Markdown 表格，对齐方式统一

2. **架构流程图约定**
   - 使用文本流程图（ASCII art 或 Mermaid）
   - 清晰标注数据流向和组件交互

3. **API 文档格式**
   - 方法签名格式：`function_name(param1: type1, param2: type2) -> return_type`
   - 参数说明：表格形式，包含名称、类型、说明
   - 示例代码：完整可运行的代码块

4. **代码示例约定**
   - 使用 ```old8lang 语言标注
   - 包含注释说明关键步骤
   - 提供预期输出
   - 标注适用的执行模式

### 快速开始指南 (quickstart.md)

为文档更新任务提供快速指南：

1. **文档更新流程**
   - 从代码中提取信息
   - 编写文档内容
   - 验证代码示例
   - 审查和修订

2. **工具和命令**
   - 代码探索：使用 Glob、Grep、Read 工具
   - 示例测试：`dotnet run --project Old8Lang.App -- -f example.old8`
   - 文档预览：Markdown 预览工具

3. **质量检查清单**
   - 准确性：信息与代码实现一致
   - 完整性：覆盖所有必需内容
   - 可读性：结构清晰，语言简洁
   - 可测试性：代码示例可运行

### Agent Context Update

运行 `.specify/scripts/powershell/update-agent-context.ps1 -AgentType claude` 更新 Claude 的上下文文件，添加：

- 三种执行模式的技术细节
- 核心架构组件的位置和职责
- 标准库模块的清单
- 文档更新的约定和模板

---

## Phase 2: Tasks (NOT created by /speckit.plan)

Phase 2 的任务生成由 `/speckit.tasks` 命令完成，不在本计划中创建。

预期任务类别：

1. **研究和信息收集任务**
   - 收集三种模式的详细信息
   - 提取标准库 API 清单
   - 分析现有文档结构

2. **文档编写任务**
   - 更新 CLAUDE.md
   - 更新 ARCHITECTURE.md
   - 更新 CLI_GUIDE.md
   - 更新 API_REFERENCE.md
   - 更新 LANGUAGE_FEATURES.md
   - 更新 PERFORMANCE_GUIDE.md

3. **验证和测试任务**
   - 测试所有代码示例
   - 审查文档准确性
   - 检查文档完整性

4. **最终审查任务**
   - 整体一致性检查
   - 格式和风格统一
   - 链接有效性验证

---

## Implementation Notes

### 关键技术信息（来自代码探索）

#### 三种执行模式实现位置

1. **解释模式 (Interpretation Mode)**
   - 核心类：`LangInterpreter` (`Old8Lang/Interpreter/LangInterpreter.cs`)
   - Visitor：`InterpreterVisitor` (`Old8Lang/AST/Visitor/InterpreterVisitor.cs`)
   - 执行方法：`Run(VariateManager manager)`
   - 完成度：90-95%

2. **编译模式 (Compilation Mode)**
   - 核心类：`Compiler` (`Old8Lang/Compiler/Compiler.cs`)
   - Visitor：`CompilerVisitor` (`Old8Lang/AST/Visitor/CompilerVisitor.cs`)
   - IL 生成：`GenerateIl(ILGenerator ilGenerator, LocalManager local)`
   - 完成度：70-85%

3. **VM 模式 (Bytecode VM Mode)**
   - 核心类：`VirtualMachine` (`Old8Lang/Bytecode/VM/VirtualMachine.Core.cs`)
   - 编译器：`BytecodeCompiler` (`Old8Lang/Bytecode/BytecodeCompiler.cs`)
   - Visitor：`BytecodeVisitor` (`Old8Lang/AST/Visitor/BytecodeVisitor.cs`)
   - 完成度：90-95%（已完整实现 ✅）

#### Visitor 模式实现

- 接口：`IVisitor<TResult>` (`Old8Lang/AST/Visitor/Generated/IVisitor.generated.cs`)
- 支持 86 个 AST 节点类型
- 三个主要实现：InterpreterVisitor, CompilerVisitor, BytecodeVisitor
- 一个辅助实现：TypeInferenceVisitor

#### 标准库模块

1. **Old8LangLib** - 核心库（Math, File, Crypto, Image, Regex, Terminal, Time, OS, CSV, Template, Vector）
2. **Old8Lang.NetLib** - 网络库（HTTP, WebSocket, MQTT, Socket, WebAPI）
3. **Old8Lang.DatabaseLib** - 数据库库（MySQL, PostgreSQL, SQLite, InMemory, ORM）
4. **Old8Lang.SerializationLib** - 序列化库（MessagePack, Protobuf）
5. **Old8Lang.MachineLearningLib** - 机器学习库（Classification, Regression, Clustering）

#### 包管理命令

执行命令：`-f` (解释), `-c` (编译), `-vm` (VM), `-s` (语法检查)
包管理：`init`, `install`, `remove`, `restore`, `list`, `pack`, `unpack`, `sign`, `verify`, `cert`, `publish`
调试分析：`debug-start`, `debug-breakpoint`, `debug-control`, `profile`

### 文档更新策略

1. **保持现有结构**：不重构文档组织，仅更新内容
2. **增量更新**：在现有章节中添加三种模式的说明
3. **对比表格**：使用表格清晰对比三种模式
4. **代码示例**：为每种模式提供可运行的示例
5. **交叉引用**：在文档间建立清晰的链接关系

### 质量保证

1. **准确性验证**：所有技术信息与代码实现一致
2. **示例测试**：所有代码示例实际运行验证
3. **完整性检查**：覆盖所有功能需求（FR-001 到 FR-010）
4. **可读性审查**：确保文档清晰易懂
5. **一致性检查**：术语、格式、风格统一

---

## Success Metrics

根据功能规范中的成功标准，文档更新完成后应达到：

- ✅ SC-001: 新用户能够在 5 分钟内理解三种执行模式的区别
- ✅ SC-002: 文档覆盖率 100%（所有三种模式都有完整说明）
- ✅ SC-003: 贡献者能够在 15 分钟内理解核心处理流程
- ✅ SC-004: API 文档完整性 100%（所有标准库模块都有文档）
- ✅ SC-005: 用户 90% 的情况下能在 3 分钟内找到所需信息
- ✅ SC-006: 用户问题减少 60%
- ✅ SC-007: 所有代码示例都能成功运行

---

## Next Steps

1. ✅ **Phase 0 完成**: 代码探索已完成，技术信息已收集
2. ⏭️ **生成 research.md**: 整合探索结果，形成详细的技术决策文档
3. ⏭️ **生成 data-model.md**: 定义文档结构模型
4. ⏭️ **生成 contracts/**: 创建文档模板和约定
5. ⏭️ **生成 quickstart.md**: 编写文档更新快速指南
6. ⏭️ **更新 Agent Context**: 运行脚本更新 Claude 上下文
7. ⏭️ **Re-check Constitution**: 验证设计符合宪章原则
8. ⏭️ **Ready for /speckit.tasks**: 准备进入任务生成阶段
