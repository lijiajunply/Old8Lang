<!--
Sync Impact Report:
Version: 0.0.0 → 1.0.0
Change Type: Initial constitution creation
Modified Principles: N/A (initial version)
Added Sections: All core principles, technical standards, development workflow, governance
Removed Sections: N/A
Templates Status:
  ✅ spec-template.md - Aligned (user stories, requirements, success criteria)
  ✅ tasks-template.md - Aligned (test-first, parallel execution, user story organization)
  ✅ plan-template.md - Aligned (constitution check gate, complexity tracking)
Follow-up TODOs: None
-->

# Old8Lang Constitution

## Core Principles

### I. 双模式对等性 (Dual-Mode Parity)

Old8Lang 支持解释模式、编译模式和VM模式三种执行方式。所有语言特性至少要在VM模式和解释模式下能正确工作。

**规则**:
- 每个 AST 节点必须同时实现 `Run(VariateManager)` 和 `GenerateIl(ILGenerator, LocalManager)` 方法
- 新增语言特性必须为两种模式编写独立的测试用例
- 行为差异必须明确记录并有充分理由（如编译模式的静态类型检查更严格）
- 不得为了一种模式的便利性而牺牲另一种模式的功能完整性

**理由**: 解释模式提供快速开发和调试体验，编译模式提供更好的性能。两者都是 Old8Lang 的核心价值主张，必须同等维护。

### II. 访问者模式一致性 (Visitor Pattern Consistency)

Old8Lang 使用访问者模式作为核心架构模式。所有 AST 节点必须正确实现访问者接口。

**规则**:
- 所有 AST 节点必须实现 `Accept<TResult>(IVisitor<TResult> visitor)` 方法
- 新增访问者实现（如 `InterpreterVisitor`, `CompilerVisitor`, `BytecodeVisitor`）必须处理所有 AST 节点类型
- 访问者方法命名必须遵循 `Visit[NodeType]` 约定
- 不得绕过访问者模式直接操作 AST 节点内部状态

**理由**: 访问者模式确保了代码的可扩展性和关注点分离，使得添加新的执行模式（如字节码 VM）或分析工具（如类型推断）变得简单。

### III. 测试优先开发 (Test-First Development) - 强制性

所有新功能必须先编写测试，测试失败后再实现功能。

**规则**:
- 新增语言特性必须先在 `Old8Lang.Tests` 项目中编写测试用例
- 测试必须覆盖解释模式（`InterpreterTests`）和编译模式（`CompilerTests`）
- 测试必须先运行并失败，然后再开始实现
- 边界条件和错误场景必须有对应的测试用例
- 测试代码必须清晰表达预期行为，作为功能文档的补充

**理由**: 编程语言的正确性至关重要。测试优先确保我们清楚地定义了预期行为，并且能够在重构时保持信心。

### IV. API 稳定性 (API Stability)

语言语法和标准库 API 必须保持向后兼容。

**规则**:
- 语法变更必须通过版本号明确标识（MAJOR 版本号变更）
- 标准库函数签名变更必须提供弃用期和迁移指南
- 新增关键字必须评估对现有代码的影响
- 破坏性变更必须在发布说明中突出显示
- 考虑提供兼容性模式或迁移工具

**理由**: 用户依赖 Old8Lang 编写的代码应该能够在新版本中继续运行。API 稳定性是语言成熟度和可信度的标志。

### V. 性能意识 (Performance Awareness)

编译模式的优化不得破坏解释模式的功能，性能改进必须有基准测试支持。

**规则**:
- 性能优化必须先建立基准测试
- 编译模式的 IL 生成优化不得影响解释模式的正确性
- 标准库函数的性能关键路径必须有性能测试
- 内存分配和 GC 压力必须在性能敏感场景中考虑
- 性能回归必须在 PR 审查中被识别和讨论

**理由**: Old8Lang 的双模式设计意味着性能优化更加复杂。我们需要确保优化是有效的，并且不会在某个模式中引入问题。

### VI. 文档完整性 (Documentation Completeness)

语言特性必须有完整的文档和示例代码。

**规则**:
- 新增语言特性必须在 `Docs/LANGUAGE_FEATURES.md` 中记录
- 标准库新增函数必须有 XML 文档注释
- 复杂特性必须提供示例代码（在 `Examples/` 目录或文档中）
- 架构变更必须更新 `Docs/ARCHITECTURE.md`
- CLI 命令变更必须更新 `Docs/CLI_GUIDE.md`

**理由**: 文档是用户学习和使用 Old8Lang 的主要途径。完整的文档降低了学习曲线，提高了语言的可用性。

## 技术标准 (Technical Standards)

### 代码质量

- **语言**: C# 10.0+，遵循 .NET 编码规范
- **目标框架**: .NET 10.0
- **代码风格**: 使用项目中的 `.editorconfig` 配置
- **静态分析**: 必须通过 Roslyn 分析器检查，无警告
- **异常处理**: 使用 `Old8Lang/Error/` 中定义的自定义异常类型

### 测试要求

- **测试框架**: xUnit
- **覆盖率目标**: 核心语言特性 >80%，标准库 >70%
- **测试组织**: 测试目录结构镜像主代码库结构
- **测试命名**: `[MethodName]_[Scenario]_[ExpectedBehavior]` 约定
- **集成测试**: 使用 `.old8` 文件进行端到端测试

### 依赖管理

- **最小化依赖**: 核心语言实现避免外部依赖
- **标准库模块化**: 可选功能（如 ML、数据库）放在独立项目中
- **版本锁定**: 使用确定性构建，锁定依赖版本

## 开发工作流 (Development Workflow)

### 功能开发流程

1. **需求明确**: 在 `.specify/specs/` 中创建功能规格说明
2. **设计审查**: 对于重大特性，先进行设计文档审查
3. **测试编写**: 在 `Old8Lang.Tests` 中编写失败的测试
4. **实现功能**:
   - 定义 AST 节点（`AST/Expression/` 或 `AST/Statement/`）
   - 实现三模式执行（`Run`,`GenerateIl`和`VM`）
   - 添加访问者支持
   - 更新解析器（`LangParser/Parsers/`）
5. **测试通过**: 确保所有测试通过
6. **文档更新**: 更新相关文档
7. **代码审查**: 提交 PR 进行审查

### 分支策略

- **主分支**: `master` - 稳定版本
- **功能分支**: `[issue-number]-feature-name` - 新功能开发
- **修复分支**: `[issue-number]-fix-description` - Bug 修复
- **发布分支**: `release/v[version]` - 发布准备

### 提交规范

- **格式**: `<type>(<scope>): <description>`
- **类型**: `feat`, `fix`, `refactor`, `test`, `docs`, `chore`
- **范围**: `parser`, `interpreter`, `compiler`, `stdlib`, `vm`, `cli`, 等
- **示例**: `feat(parser): 添加模式匹配语法支持`

## Governance

### 宪章权威性

本宪章是 Old8Lang 项目开发的最高指导原则，优先级高于所有其他实践和约定。

### 修订流程

1. **提议**: 在 GitHub Issue 中提出修订建议，说明理由和影响
2. **讨论**: 核心维护者和社区成员讨论提议
3. **批准**: 需要至少 2 名核心维护者批准
4. **迁移计划**: 对于影响现有代码的修订，必须提供迁移计划
5. **版本更新**: 更新宪章版本号和修订日期
6. **同步更新**: 更新所有依赖宪章的模板和文档

### 合规性审查

- 所有 PR 必须经过宪章合规性检查
- 违反核心原则的代码不得合并
- 复杂性增加必须有充分理由（参见 `plan-template.md` 中的复杂性跟踪）
- 定期审查（每季度）确保项目遵循宪章原则

### 运行时指导

开发过程中的具体指导和最佳实践参见 `CLAUDE.md` 文件。该文件提供了构建、测试、调试等实用信息，但不覆盖本宪章中的核心原则。

**Version**: 1.0.0 | **Ratified**: 2026-02-14 | **Last Amended**: 2026-02-14
