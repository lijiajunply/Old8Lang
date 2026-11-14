# Old8Lang 代理开发指南

本文件是代理（AI）的入口索引：只放「必须知道的要点」和「必须遵守的硬规则」，细节一律在 `Docs/` 下。
**动手前请按需打开对应文档，不要凭记忆臆测**——尤其是测试与开发流程两类。
要读细节时，先用文末「[文档索引与按需阅读](#文档索引与按需阅读)」定位到**具体文档的具体章节**，只读那一节，不要通读整个 `Docs/`。

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
4. **测试报告进对应的 change 文件夹**：报告写成 `changes/<日期>-<简述>/test.md`，**不要**再往 `Reports/` 里放新报告；没有建 change 的一次性小改动，在当次回复里给出测试结论即可。
5. **性能/并发用例**必须标 `[Trait("Category", "Performance")]`，它们只在 `performance.runsettings` 档下运行。
   例外：关键场景的「粗阈值」用例（阈值放到实测值 5~10 倍）标 `[Trait("Category", "PerformanceSmoke")]`，
   它们会随默认档一起跑。详见 [Docs/TESTING_GUIDE.md](Docs/TESTING_GUIDE.md#11-性能并发用例分档)。
6. **测试中发现与本任务无关的错误**，不要顺手改，记录到 `changes/Todo.md`；不再使用的测试文件及时删除。
7. **新增代码先定程序集**：语法节点/解析器进 `Old8Lang.Syntax`，运行时值与共享契约进 `Old8Lang.Runtime`，
   各执行能力进各自后端；Runtime 不引用后端，三个后端互不引用，官方模块只由 `Old8Lang.Hosting` 的
   组合根注册。对照表与约束见 [Docs/DEVELOPMENT_WORKFLOW.md §4.1](Docs/DEVELOPMENT_WORKFLOW.md#41-程序集边界与宿主模块迁移后)，
   改动模块装配或作用域派生后跑 `dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj --filter "FullyQualifiedName~Old8Lang.Tests.Architecture"`。
8. **长期开发必须留痕**：跨多次会话/多阶段推进、或需要先定方案再动手的任务，先在 `changes/<日期>-<简述>/` 下
   按 `spec` → `design` → `plan` → `task` → `test` 建文件，产物与测试报告都写在里面；废弃的 change 整体
   **移动**到 `archive/`，不删除。规则见下文「长期开发：changes 目录」。
9. **细节先查索引再读文档**：涉及语法、API、模式支持、测试流程、程序集归属的问题，一律按
   「[文档索引与按需阅读](#文档索引与按需阅读)」定位到对应文档章节后回答，不凭记忆；读到够回答问题就停。

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

## 长期开发：changes 目录

**什么时候建**：任务预计跨多次会话或多阶段推进、需要先定方案再动手、或要留下决策依据时，先建一个 change 文件夹。
单点的文本/配置修改、一次就能验收完的小改动不建。

**命名**：`changes/<日期>-<简述>/`，日期用 `YYYY-MM-DD`，例如 `changes/2026-11-14-解释后端拆分/`。
同一天有多个任务时靠简述区分，不要在日期后追加时分。
`changes/Todo.md` 是全局待办清单，**唯一不放在子文件夹里的文件**，不要给它建同名文件夹。

**里面放什么**（按需写，不要求每份都满）：

| 文件 | 内容 |
|------|------|
| `spec.md` | 需求与约束：要解决什么问题、范围与明确不做的部分、验收标准 |
| `design.md` | 方案与取舍：备选方案、选定理由、受影响的程序集与接口 |
| `plan.md` | 阶段划分：每步做什么、依赖关系、回滚点 |
| `task.md` | 可勾选的执行清单与当前进度 |
| `test.md` | 测试计划与实测记录（即测试报告）：命令、结果、失败项与结论 |

**测试报告**一律写进 `changes/<日期>-<简述>/test.md`，不再在 `Reports/` 下新建报告文件。
`Reports/` 仅剩 VM 性能基准的输出目录用途（`Old8Lang.Benchmarks` 写入），历史功能测试报告已归档到 `archive/Reports/`。

**归档**：任务完成已久、被新方案取代、或决定不做时，把整个 change 文件夹**移动**到仓库根的
`archive/<原文件夹名>/`，保持原文件名以便追溯；不要删除。归档目录不参与当前任务，也不要往里面补写内容。

同一个任务里发现的、与本次无关的遗留问题仍按硬规则 6 记到 `changes/Todo.md`，不要散落在 change 文件夹里。

## 架构速览

`Old8Lang.Syntax/LangParser/LangParser.cs` 把源码解析成 AST，语法节点在 `Old8Lang.Syntax/AST/Expressions/`、
`Old8Lang.Syntax/AST/Statements/`，基类 `LangExpression`、`OldStatement` 在 `Old8Lang.Syntax/AST/`。
同一棵 AST 由不同 Visitor 处理成三种执行方式，**四个 Visitor 分属不同程序集**：

| Visitor | 位置 | 作用 |
|---------|------|------|
| `InterpreterVisitor` | `Old8Lang.Interpreter/Visitors/` | 解释执行 |
| `CompilerVisitor` | `Old8Lang.Compiler/Visitors/` | 生成 IL |
| `BytecodeVisitor` | `Old8Lang.VirtualMachine/Visitors/` | 生成字节码 |
| `TypeInferenceVisitor` | `Old8Lang.Runtime/AST/Visitor/` | 类型推断（仍在 Runtime） |

`IVisitor<TResult>` 接口是自动生成的（`Old8Lang.Runtime/AST/Visitor/Generated/IVisitor.generated.cs`），
新增 AST 节点后必须用 `Old8Lang.CodeGen` 重新生成；扫描目录与输出目录见 `Old8Lang.CodeGen/visitor-codegen.json`。

程序集分层（箭头指向被引用方，下层不得引用上层，三个后端互不引用）：

```text
Old8Lang.App / LanguageServer / Benchmarks / Tests
        ↓
Old8Lang.Hosting（组合根：显式注册后端与官方模块）+ Old8Lang.Modules.Official（官方模块绑定）
        ↓
Old8Lang.Interpreter / Old8Lang.Compiler / Old8Lang.VirtualMachine
        ↓
Old8Lang.Runtime（运行时值、内置函数、模块/类型系统、字节码格式、共享契约）
        ↓
Old8Lang.Syntax（语法节点 + 解析器）→ Old8Lang.Core（源位置、诊断、词法、预处理）
```

侧向不依赖执行后端的程序集：`Old8Lang.Concurrency`、`Old8Lang.Observability`、`Old8Lang.ProjectSystem`、
`Old8Lang.Modules.Abstractions`。

详细内容见 [Docs/ARCHITECTURE.md](Docs/ARCHITECTURE.md)：

- 三种执行模式对比与选择 → [§1.3 执行模式](Docs/ARCHITECTURE.md#13-执行模式-execution-modes)
- Visitor 模式与四个实现 → [§4 Visitor 模式详解](Docs/ARCHITECTURE.md#4-visitor-模式详解)
- 模块划分与目录结构 → [§2 模块架构](Docs/ARCHITECTURE.md#2-模块架构)、[§11 项目目录结构](Docs/ARCHITECTURE.md#11-项目目录结构)
- 类型系统 → [§7 Type System](Docs/ARCHITECTURE.md#7-type-system-类型系统)

## 主要项目

解决方案共 22 个程序集，按层列出（依赖方向见上方「架构速览」的分层图）：

| 层 | 项目 | 说明 |
|----|------|------|
| 基础 | `Old8Lang.Core/` | 源位置、语言元数据、`Old8Exception`/`SyntaxError`、词法与预处理器、文档注释模型（只引用 BCL） |
| 语法 | `Old8Lang.Syntax/` | `LangParser` 与全部专用解析器、AST 语法节点、`IVisitor` 生成模型（只引用 Core） |
| 运行时 | `Old8Lang.Runtime/` | 运行时值（`LangValueType` 及子类）、内置函数、实例方法与模块/类型系统、字节码格式、共享契约与执行状态（`RuntimeVisitors`、`ILangInterpreter`、`IVirtualMachine`、`IBytecodeCompiler`）、`TypeInferenceVisitor` |
| 后端 | `Old8Lang.Interpreter/` | `InterpreterVisitor`、`LangInterpreter`、解释器调试器 |
| 后端 | `Old8Lang.Compiler/` | `CompilerVisitor`、`Compiler` 驱动、IL 辅助与验证、异步状态机 |
| 后端 | `Old8Lang.VirtualMachine/` | `BytecodeVisitor`、`BytecodeCompiler`、VM 引擎、VM 调试/分析/Interop |
| 装配 | `Old8Lang.Hosting/` | 组合根：`Old8HostBuilder`/`Old8Host`，逐宿主装配三个后端与官方模块目录（`StandardLibraryCatalog`，类型在 Runtime） |
| 装配 | `Old8Lang.Modules.Abstractions/` | 模块描述接口（不含任何扩展程序集绑定） |
| 装配 | `Old8Lang.Modules.Official/` | 官方模块的具体类型绑定（标准库与扩展库） |
| 侧向 | `Old8Lang.Concurrency/` | 并发资源、线程调度与同步原语（不依赖执行后端） |
| 侧向 | `Old8Lang.Observability/` | 性能监控、分析与报告模型（对象池统计由 Runtime 注入） |
| 侧向 | `Old8Lang.ProjectSystem/` | `o8package.json` 与环境配置（不含运行时包加载） |
| 工具 | `Old8Lang.CodeGen/` | 生成 `IVisitor` 接口、标记 `partial`、生成 Visitor 骨架 |
| 应用 | `Old8Lang.App/` | 命令行应用 |
| 应用 | `Old8Lang.LanguageServer/`、`vscode-old8lang/` | LSP 服务与 VS Code 扩展 |
| 扩展库 | `Old8LangLib/` | 标准库（数学、字符串、文件、集合、JSON、向量） |
| 扩展库 | `Old8Lang.NetLib/`、`Old8Lang.SerializationLib/`、`Old8Lang.DatabaseLib/`、`Old8Lang.MachineLearningLib/` | 网络、序列化、数据库、机器学习 |
| 测试 | `Old8Lang.Tests/` | xUnit 单元测试（含 `Architecture/` 程序集边界测试） |
| 测试 | `Old8Lang.Benchmarks/` | 性能基准测试 |

## 文档索引与按需阅读

本文件只放**入口索引**与**硬规则**；任何细节都在 `Docs/` 里。**不要凭记忆回答，也不要一次通读整个 `Docs/`。**

按需阅读三条：

1. 先用下表定位到**具体文档的具体章节**，只读那一节。
2. 读到能回答当前问题就停；不要顺手翻相邻文档、旧快照或无关章节（那属于发散）。
3. 语法、API、模式支持的结论必须来自文件或实测命令，记忆不算依据；查不到就直说查不到。

| 你要做的事 | 读哪里 | 重点 |
|---|---|---|
| 加/改语法、加 AST 节点 | [DEVELOPMENT_WORKFLOW §1–§2](Docs/DEVELOPMENT_WORKFLOW.md#1-新功能新语法添加流程) | 四级测试顺序、节点规范、重生成 `IVisitor` |
| 判断代码该放哪个程序集 | [DEVELOPMENT_WORKFLOW §4.1](Docs/DEVELOPMENT_WORKFLOW.md#41-程序集边界与宿主模块迁移后) | 程序集对照表与跨层约束 |
| 跑测试、用例分档、`.old8` 放哪 | [TESTING_GUIDE §1–§4](Docs/TESTING_GUIDE.md#1-单元测试xunit) | 性能分档、目录归属、测试顺序 |
| 某语法在三种模式支不支持 | [MODE_SUPPORT 支持矩阵](Docs/MODE_SUPPORT.md#支持矩阵)、[已知限制](Docs/MODE_SUPPORT.md#已知限制) | **唯一权威**，逐行实测 |
| 函数/方法签名 | [API_REFERENCE](Docs/API_REFERENCE.md) | **API 唯一出处**，三种调用形式 |
| CLI 参数、包管理 | [CLI_GUIDE](Docs/CLI_GUIDE.md) | 运行命令、`pack`/`publish`/`sign`/`cert` |
| 三种模式怎么选、执行流程 | [ARCHITECTURE §1.3](Docs/ARCHITECTURE.md#13-执行模式-execution-modes) | 模式对比与选择 |
| Visitor 结构、重生成接口 | [ARCHITECTURE §4](Docs/ARCHITECTURE.md#4-visitor-模式详解) | 接口生成与四个实现 |
| 模块划分、目录结构 | [ARCHITECTURE §2](Docs/ARCHITECTURE.md#2-模块架构)、[§11](Docs/ARCHITECTURE.md#11-项目目录结构) | 模块图与目录树 |
| 类型系统 | [ARCHITECTURE §7](Docs/ARCHITECTURE.md#7-type-system-类型系统) | 推断、检查、泛型 |
| 性能优化与基准 | [PERFORMANCE_GUIDE](Docs/PERFORMANCE_GUIDE.md) | 写法优化 + 运行时内部优化 |
| 环境变量、`o8package.json` 环境段 | [ENVIRONMENT_GUIDE](Docs/ENVIRONMENT_GUIDE.md) | 加载优先级、`env` 命令 |
| LSP、调试器、性能分析器 | [DEVELOPER_TOOLS](Docs/DEVELOPER_TOOLS.md) | 编辑器集成与调试工具 |
| extern、包开发、扩展语言 | [ADVANCED_TOPICS](Docs/ADVANCED_TOPICS.md) | Extern 工厂、包发布、扩展点 |
| 语法怎么写 | [Old8Lang_Grammar](Docs/Old8Lang_Grammar.md)、[EBNF](Docs/Old8Lang.ebnf) | 每节带模式标记 |
| 语言特性清单 | [LANGUAGE_FEATURES](Docs/LANGUAGE_FEATURES.md) | 特性总览 |
| 语法/API 变更史 | [CHANGELOG](Docs/CHANGELOG.md) | 只收语法与 API（规则见上文） |
| 待办与已知缺口 | [Todo.md](changes/Todo.md)、[ROADMAP](Docs/ROADMAP.md) | 未完成项与方向 |
| 全部文档地图 | [Docs/README.md](Docs/README.md) | 完整索引 |

表里没有的问题：先看 [Docs/README.md](Docs/README.md) 找到对应文档，再回到上表的读法。**新写文档时同步在本表补一行**，否则等于没写。
