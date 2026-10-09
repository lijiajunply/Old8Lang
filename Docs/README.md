# Old8Lang 文档中心

欢迎查阅 Old8Lang 文档！Old8Lang 是一门用 C#（.NET 10.0）实现的**动态类型**编程语言，
支持解释模式（`-f`）、IL 模式（`-il`）和虚拟机模式（`-vm`）三种执行模式。

本文件是 `Docs/` 下全部文档的索引。

## 📚 文档目录

### 🚀 入门指南

- **[语言特性 (LANGUAGE_FEATURES.md)](./LANGUAGE_FEATURES.md)**
  - 模式匹配、高级控制流、并发模型、运算符重载、高级类型系统、互操作性。
- **[CLI 指南 (CLI_GUIDE.md)](./CLI_GUIDE.md)**
  - 三种模式的运行与编译命令、性能监控、包管理（`pack`/`unpack`/`sign`/`verify`/`cert`/`publish`）、调试命令。
- **[常见问题 (FAQ.md)](./FAQ.md)**
  - 语法、类型系统、错误处理、并发、互操作、开发工具的常见疑问。

### 📖 参考手册

- **[语法参考 (Old8Lang_Grammar.md)](./Old8Lang_Grammar.md)**
  - 完整的语法说明与示例代码，每节带各模式的实测支持标记。
- **[EBNF 语法定义 (Old8Lang.ebnf)](./Old8Lang.ebnf)**
  - 形式化的语法定义，适合语言研究者和工具开发者。
- **[标准库 API 参考 (API_REFERENCE.md)](./API_REFERENCE.md)**
  - 全局函数、数学/字符串/集合/文件/JSON 操作、并发原语。

### 🧩 执行模式

- **[模式支持矩阵 (MODE_SUPPORT.md)](./MODE_SUPPORT.md)**
  - 三种模式逐行实测的特性支持矩阵、已知限制与推荐使用策略。**这是模式可用性的唯一权威说明。**
- **[API 模式实现对比 (API_Mode_Comparison.md)](./API_Mode_Comparison.md)**
  - 三种模式下全局函数、标准库、基本类型方法的内部实现机制差异。

### 🔧 高级主题

- **[高级主题 (ADVANCED_TOPICS.md)](./ADVANCED_TOPICS.md)**
  - Extern 功能工厂架构、渐进式类型推断系统、包开发与发布指南、扩展 Old8Lang。
- **[性能指南 (PERFORMANCE_GUIDE.md)](./PERFORMANCE_GUIDE.md)**
  - 上半部分讲怎么写得更快（模式选择、内存与并发优化、常见陷阱、基准测试）；
    下半部分讲运行时内部优化（解析器/解释器的优化技术、监控 API、性能目标）。
- **[环境配置 (ENVIRONMENT_GUIDE.md)](./ENVIRONMENT_GUIDE.md)**
  - `o8package.json` 环境段、`.old8env` 变量加载优先级、`env` 命令。
- **[开发工具 (DEVELOPER_TOOLS.md)](./DEVELOPER_TOOLS.md)**
  - Language Server 与 VS Code 扩展、内置调试器、性能分析器。

### 🛠️ 开发与贡献

- **[架构文档 (ARCHITECTURE.md)](./ARCHITECTURE.md)**
  - 模块划分、Visitor 模式、AST/Parser 结构、类型系统、目录结构。
- **[开发流程 (DEVELOPMENT_WORKFLOW.md)](./DEVELOPMENT_WORKFLOW.md)**
  - 新语法添加流程、AST 节点与错误处理规范、解析器/类型系统要点、调试与 IL 问题排查。
- **[测试指南 (TESTING_GUIDE.md)](./TESTING_GUIDE.md)**
  - 单元测试分档、`.old8` 测试文件规范、测试目录与测试报告要求。
- **[开发路线图 (ROADMAP.md)](./ROADMAP.md)**
  - 方向性的当前重点与各领域待办入口。
- **[变更日志 (CHANGELOG.md)](./CHANGELOG.md)**
  - 按日期的变更记录，含每项改动的起因、修复与已知限制。

### 📊 各领域待办

- **[VM 性能优化](../Todo.md)** - 虚拟机调用路径、异常路径与基准回归门禁。
- **[IL 模式测试覆盖](../Old8Lang.Tests/Compiler/TODO.md)** - IL 模式测试覆盖缺口。
- **[解释模式测试覆盖](../Old8Lang.Tests/Interpreter/TODO.md)** - 解释模式测试覆盖缺口。
- **[语言服务器 / LSP](../Old8Lang.LanguageServer/TODO.md)** - Language Server 与 VS Code 扩展的开发计划。

---

## 快速开始

运行一个 Old8Lang 脚本（示例见 [`examples/`](../examples/)）：

```bash
dotnet run --project Old8Lang.App -- -f examples/interpretation_mode_example.old8
```

另外两种模式：

```bash
dotnet run --project Old8Lang.App -- -il examples/compilation_mode_example.old8
dotnet run --project Old8Lang.App -- -vm examples/vm_mode_example.old8
```

更多命令请参考 [CLI 指南](./CLI_GUIDE.md)。
