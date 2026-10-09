# Old8Lang 代理开发指南

本文件是代理（AI）的入口索引：只放「必须知道的要点」和「必须遵守的硬规则」，细节一律在 `Docs/` 下。
**动手前请按需打开对应文档，不要凭记忆臆测**——尤其是测试与开发流程两类。

## 项目概述

Old8Lang 是一门用 C#（.NET 10.0）实现的**动态类型**编程语言，支持三种执行模式：

- **解释模式**（`-f`）：逐条执行 AST，启动最快，支持全部动态特性（泛型、运算符重载、Python 互操作）。用于开发与脚本。
- **IL 模式**（`-il`，`-c` 为等价别名）：编译成 .NET IL 再执行，运行性能最高，**要求完整类型注解**，不支持部分动态特性。用于生产与性能敏感场景。
- **虚拟机模式**（`-vm`）：编译成自定义字节码由 VM 执行，支持字节码序列化、跨平台分发与高级调试（实验性）。用于分发与调试。

语言特性包括函数、类、异常处理、async/await、泛型，以及一套完整的标准库。

## 常用命令

```bash
# 构建
dotnet build Old8Lang.sln

# 单元测试（默认档，自动排除性能/并发用例）
dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj

# 运行 Old8Lang 代码
dotnet run --project Old8Lang.App -- -f  <file.old8>   # 解释模式
dotnet run --project Old8Lang.App -- -il <file.old8>   # IL 模式
dotnet run --project Old8Lang.App -- -vm <file.old8>   # 虚拟机模式
dotnet run --project Old8Lang.App -- -s  <file.old8>   # 只做语法检查

# 加 -d 或 --log-level debug 打开调试输出
```

全量 CLI 命令（性能监控参数、包管理 init/install/pack/publish/sign/cert 等）见 [Docs/CLI_GUIDE.md](Docs/CLI_GUIDE.md)。

## 必须遵守的硬规则

1. **新增语法必须按顺序测试**：语法测试 → 解释模式 → IL 模式 → 虚拟机模式，逐级推进，前一级不过不进下一级。完整流程见 [Docs/DEVELOPMENT_WORKFLOW.md](Docs/DEVELOPMENT_WORKFLOW.md#1-新功能新语法添加流程)。
2. **测试用 `.old8` 文件按模式放目录**：语法 → `TestFiles/SyntaxTests/`，解释 → `TestFiles/InterpreterTests/`，IL → `TestFiles/CompilerTests/`，VM → `TestFiles/VirtualMachine/`。
3. **写 `.old8` 的约定**：注释用 `//` 而不是 `#`；用 `PrintLine` 打印结果；期望失败的用例在**文件最后一行**写 `error`。
4. **每次测试结束都要生成测试报告**，放到 `Reports/`，文件名用 `日期-小时-分钟-测试类型.md`。
5. **性能/并发用例**必须标 `[Trait("Category", "Performance")]`，它们只在 `performance.runsettings` 档下运行。
6. **测试中发现与本任务无关的错误**，不要顺手改，记录到 `Todo.md`；不再使用的测试文件及时删除。

以上规则的完整说明见 [Docs/TESTING_GUIDE.md](Docs/TESTING_GUIDE.md)。

## CHANGELOG 规则

**`Docs/CHANGELOG.md` 只记录「语法」与「API」两类变更，且只做精简介绍。**

- **要写的**：语言语法的新增与改动（关键字、运算符、语句形式）、
  API 的新增与改动（全局函数、实例方法、标准库模块方法、命令行参数、配置项格式）、
  跨模式的行为差异。
- **不要写的**：实现细节与重构（内部机制、生成的代码形态、数据结构）、
  测试数量与用例清单、文档整理、性能数据、文件路径级的技术说明——这些看 git log 即可。
- **写法**：按版本或日期分节，每节分「语法」「API」两组，用一两句话说清**使用者看到的变化**；
  不贴大段代码、不逐个罗列函数签名、不把已经能用的能力再数一遍。
- **反例**：写「`MakeClosure` 原先把外层局部变量的当前值快照进闭包环境……」是实现说明，
  应写成「闭包可写回外层局部变量」。

## 架构速览

`LangParser` 把源码解析成 AST；同一棵 AST 由不同 Visitor 处理成三种执行方式——`InterpreterVisitor`（解释）、`CompilerVisitor`（生成 IL）、`BytecodeVisitor`（生成字节码），另有 `TypeInferenceVisitor` 做类型推断。四个 Visitor 都在 `Old8Lang/AST/Visitor/`，`IVisitor` 接口是自动生成的，新增 AST 节点后需要重新生成。

详细内容见 [Docs/ARCHITECTURE.md](Docs/ARCHITECTURE.md)：

- 三种执行模式对比与选择 → [§1.3 执行模式](Docs/ARCHITECTURE.md#13-执行模式-execution-modes)
- Visitor 模式与四个实现 → [§4 Visitor 模式详解](Docs/ARCHITECTURE.md#4-visitor-模式详解)
- 模块划分与目录结构 → [§2 模块架构](Docs/ARCHITECTURE.md#2-模块架构)、[§11 项目目录结构](Docs/ARCHITECTURE.md#11-项目目录结构)
- 类型系统 → [§7 Type System](Docs/ARCHITECTURE.md#7-type-system-类型系统)

## 主要项目

| 项目 | 说明 |
|------|------|
| `Old8Lang/` | 核心语言实现（AST、解析器、解释器、编译器、VM、类型系统） |
| `Old8Lang.App/` | 命令行应用 |
| `Old8LangLib/` | 标准库（数学、字符串、文件、集合） |
| `Old8Lang.Tests/` | xUnit 单元测试 |
| `Old8Lang.Benchmarks/` | 性能基准测试 |
| `Old8Lang.NetLib/`、`Old8Lang.SerializationLib/`、`Old8Lang.DatabaseLib/`、`Old8Lang.MachineLearningLib/` | 扩展库 |
| `Old8Lang.LanguageServer/`、`vscode-old8lang/` | LSP 服务与 VS Code 扩展 |

## 文档索引

| 文档 | 内容 |
|------|------|
| [Docs/TESTING_GUIDE.md](Docs/TESTING_GUIDE.md) | **测试必读**：单测分档、`.old8` 文件规范、测试目录、测试报告 |
| [Docs/DEVELOPMENT_WORKFLOW.md](Docs/DEVELOPMENT_WORKFLOW.md) | **开发必读**：新语法流程、AST 节点与错误处理规范、解析器/类型系统要点、调试与 IL 排查、代码规范与提交规范 |
| [Docs/ARCHITECTURE.md](Docs/ARCHITECTURE.md) | 架构总览 |
| [Docs/CLI_GUIDE.md](Docs/CLI_GUIDE.md) | CLI 全量命令、包管理 |
| [Docs/LANGUAGE_FEATURES.md](Docs/LANGUAGE_FEATURES.md) | 语言特性 |
| [Docs/Old8Lang_Grammar.md](Docs/Old8Lang_Grammar.md)、[Docs/Old8Lang.ebnf](Docs/Old8Lang.ebnf) | 语法参考 |
| [Docs/API_REFERENCE.md](Docs/API_REFERENCE.md) | **API 唯一出处**：全局函数、实例方法、标准库模块方法 |
| [Docs/MODE_SUPPORT.md](Docs/MODE_SUPPORT.md) | **模式必读**：三种模式逐行实测的支持矩阵与已知限制 |
| [Docs/PERFORMANCE_GUIDE.md](Docs/PERFORMANCE_GUIDE.md) | 性能优化 |
| [Docs/CHANGELOG.md](Docs/CHANGELOG.md) | 语法与 API 变更记录（收录规则见上文） |
| [Docs/README.md](Docs/README.md) | 文档中心（全部文档的索引） |
