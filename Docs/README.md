# Old8Lang 文档中心

欢迎查阅 Old8Lang 文档！Old8Lang 是一门基于 C# 实现的动态编程语言，支持解释模式和 IL 模式。

## 📚 文档目录

### 🚀 入门指南

- **[语言特性 (LANGUAGE_FEATURES.md)](./LANGUAGE_FEATURES.md)**
  - 了解 Old8Lang 的核心特性和设计理念。
- **[CLI 指南 (CLI_GUIDE.md)](./CLI_GUIDE.md)**
  - 学习如何运行、编译代码以及使用包管理工具。

### 📖 参考手册

- **[语法参考 (Old8Lang_Grammar.md)](./Old8Lang_Grammar.md)**
  - 完整的语法说明，包含示例代码。
- **[API 参考 (API_REFERENCE.md)](./API_REFERENCE.md)**
  - 标准库函数文档，包括数学、字符串、文件、并发原语等。
- **[EBNF 语法定义 (Old8Lang.ebnf)](./Old8Lang.ebnf)**
  - 形式化的语法定义，适合语言研究者和工具开发者。

### 🔧 高级主题

- **[高级主题 (ADVANCED_TOPICS.md)](./ADVANCED_TOPICS.md)**
  - 包含 Extern 工厂架构、渐进式类型推断和包开发指南。
- **[性能指南 (PERFORMANCE_GUIDE.md)](./PERFORMANCE_GUIDE.md)**
  - 性能优化建议和最佳实践。
- **[常见问题 (FAQ.md)](./FAQ.md)**
  - 常见疑问解答，涵盖语法、工具和错误处理。

### 🛠️ 开发与贡献

- **[架构文档 (ARCHITECTURE.md)](./ARCHITECTURE.md)**
  - 项目架构概览。
- **[贡献指南 (CONTRIBUTING.md)](./CONTRIBUTING.md)**
  - 如何参与 Old8Lang 的开发。
- **[测试指南 (TESTING_GUIDE.md)](./TESTING_GUIDE.md)**
  - 单元测试分档、`.old8` 测试文件规范、测试目录与测试报告要求。
- **[开发流程 (DEVELOPMENT_WORKFLOW.md)](./DEVELOPMENT_WORKFLOW.md)**
  - 新语法添加流程、AST 节点与错误处理规范、调试与 IL 问题排查。
- **[变更日志 (CHANGELOG.md)](./CHANGELOG.md)**
  - 查看版本更新历史。

### 📊 开发状态 (TODO)

- **[待办事项 (Todo.md)](../Todo.md)**
  - 虚拟机模式性能优化等待办。
- **[IL 模式测试待办 (TODO.md)](../Old8Lang.Tests/Compiler/TODO.md)**
  - IL 模式测试覆盖缺口。

---

## 快速开始

运行一个 Old8Lang 脚本：

```bash
dotnet run --project Old8Lang.App -- -f scripts/hello.old8
```

更多命令请参考 [CLI 指南](./CLI_GUIDE.md)。
