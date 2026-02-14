# Quick Start: 文档更新指南

**Feature**: 001-update-docs-modes
**Date**: 2026-02-14

## 概述

本指南为文档更新任务提供快速参考，帮助您高效完成文档更新工作。

---

## 1. 文档更新流程

### Step 1: 信息收集

**目标**: 从代码中提取准确的技术信息

**工具**:
- `Glob` - 查找文件
- `Grep` - 搜索代码
- `Read` - 读取文件内容

**示例**:
```bash
# 查找所有 Visitor 实现
Glob: "Old8Lang/AST/Visitor/*Visitor.cs"

# 搜索特定方法
Grep: "Run\(VariateManager" in "Old8Lang/AST/"

# 读取文件
Read: "Old8Lang/Interpreter/LangInterpreter.cs"
```

---

### Step 2: 编写文档内容

**目标**: 按照模板和契约编写文档

**参考**:
- `data-model.md` - 文档结构模型
- `contracts/` - 文档模板和格式约定
- `research.md` - 技术信息汇总

**要点**:
- 使用标准模板
- 遵循格式约定
- 保持术语一致
- 添加代码示例

---

### Step 3: 验证代码示例

**目标**: 确保所有代码示例可运行

**命令**:
```bash
# 解释模式测试
dotnet run --project Old8Lang.App -- -f example.old8

# 编译模式测试
dotnet run --project Old8Lang.App -- -c example.old8

# VM 模式测试
dotnet run --project Old8Lang.App -- -vm example.old8
```

**检查清单**:
- [ ] 代码语法正确
- [ ] 代码可以运行
- [ ] 输出符合预期
- [ ] 注释清晰明了

---

### Step 4: 审查和修订

**目标**: 确保文档质量

**检查项**:
- [ ] 准确性: 信息与代码一致
- [ ] 完整性: 覆盖所有必需内容
- [ ] 一致性: 术语和格式统一
- [ ] 可读性: 结构清晰，语言简洁

---

## 2. 常用工具和命令

### 代码探索

```bash
# 查找文件
Glob: "**/*.cs"

# 搜索内容
Grep: "pattern" in "directory/"

# 读取文件
Read: "path/to/file.cs"
```

### 示例测试

```bash
# 创建测试文件
echo 'print("Hello, World!")' > test.old8

# 解释模式
dotnet run --project Old8Lang.App -- -f test.old8

# 编译模式
dotnet run --project Old8Lang.App -- -c test.old8

# VM 模式
dotnet run --project Old8Lang.App -- -vm test.old8

# 清理
rm test.old8
```

### 文档预览

- 使用 Markdown 预览工具（VS Code, Typora, etc.）
- 检查表格渲染
- 验证链接有效性

---

## 3. 文档更新清单

### CLAUDE.md

- [ ] 添加三种执行模式概述
- [ ] 更新"Run Old8Lang Code"章节
- [ ] 添加执行模式对比表格
- [ ] 更新"High-Level Architecture"章节
- [ ] 添加 Visitor 模式说明
- [ ] 更新"Key Files to Understand"章节

### Docs/ARCHITECTURE.md

- [ ] 更新"Core Processing Pipeline"
- [ ] 添加三种执行模式详细说明
- [ ] 更新 Visitor 模式章节
- [ ] 添加 AST 节点组织结构
- [ ] 更新 Parser 说明
- [ ] 更新 TypeSystem 说明

### Docs/CLI_GUIDE.md

- [ ] 添加三种执行模式命令对比
- [ ] 更新执行命令章节
- [ ] 添加模式选择指南
- [ ] 更新包管理命令
- [ ] 添加调试和分析命令

### Docs/API_REFERENCE.md

- [ ] 更新标准库模块清单
- [ ] 添加模式支持标注
- [ ] 更新 API 方法签名
- [ ] 添加使用示例
- [ ] 标注实验性功能

### Docs/LANGUAGE_FEATURES.md

- [ ] 添加不同模式下的功能差异
- [ ] 更新泛型支持说明
- [ ] 更新运算符重载说明
- [ ] 添加 Python 互操作说明

### Docs/PERFORMANCE_GUIDE.md

- [ ] 添加三种模式性能对比
- [ ] 添加模式选择指南
- [ ] 更新性能优化建议

---

## 4. 质量检查清单

### 准确性

- [ ] 所有技术信息与代码实现一致
- [ ] 文件路径和行号准确
- [ ] 版本号和完成度准确
- [ ] 代码示例可运行

### 完整性

- [ ] 所有三种执行模式都有说明
- [ ] 所有标准库模块都有文档
- [ ] 所有 CLI 命令都有参考
- [ ] 所有架构组件都有说明

### 一致性

- [ ] 术语使用一致（中英文对照）
- [ ] 格式风格统一
- [ ] 链接有效
- [ ] 代码块语言标注正确

### 可读性

- [ ] 标题层级清晰
- [ ] 段落长度适中
- [ ] 表格易于阅读
- [ ] 代码示例有注释

---

## 5. 常见问题

### Q: 如何确定代码示例的正确性？

A: 实际运行代码示例，验证输出是否符合预期。使用三种执行模式分别测试（如果适用）。

### Q: 如何处理实验性功能？

A: 明确标注为"实验性"，说明当前状态和使用建议，警告用户不要在生产环境中使用。

### Q: 如何保持文档与代码同步？

A: 在代码变更时同步更新文档，使用代码位置引用（`file.cs:line`）便于追踪。

### Q: 如何处理中英文混排？

A: 主要内容使用中文，技术术语保留英文原文，首次出现时提供中英文对照。

---

## 6. 参考资源

### 项目文档

- `spec.md` - 功能规范
- `plan.md` - 实施计划
- `research.md` - 技术研究
- `data-model.md` - 文档结构模型
- `contracts/` - 文档模板

### 代码位置

- 解释器: `Old8Lang/Interpreter/LangInterpreter.cs`
- 编译器: `Old8Lang/Compiler/Compiler.cs`
- VM: `Old8Lang/Bytecode/VM/VirtualMachine.Core.cs`
- Visitor: `Old8Lang/AST/Visitor/`
- Parser: `Old8Lang/LangParser/`

### 现有文档

- `Docs/MODE_COMPLETION_STATUS.md` - 模式完成度
- `Docs/Mode_Support_Summary.md` - 模式支持总结
- `Docs/API_Mode_Comparison.md` - API 模式对比

---

## 7. 快速命令参考

```bash
# 构建项目
dotnet build Old8Lang.sln

# 运行测试
dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj

# 解释模式
dotnet run --project Old8Lang.App -- -f <file.old8>

# 编译模式
dotnet run --project Old8Lang.App -- -c <file.old8>

# VM 模式
dotnet run --project Old8Lang.App -- -vm <file.old8>

# 语法检查
dotnet run --project Old8Lang.App -- -s <file.old8>

# 调试模式
dotnet run --project Old8Lang.App -- -f <file.old8> -d
```

---

## 8. 下一步

完成文档更新后：

1. 运行所有代码示例验证
2. 检查所有链接有效性
3. 审查文档一致性
4. 提交文档更新
5. 更新 CHANGELOG.md

---

**创建日期**: 2026-02-14
**维护者**: Old8Lang 文档团队
