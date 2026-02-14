# Research: 更新文档以支持三种执行模式

**Date**: 2026-02-14
**Feature**: 001-update-docs-modes
**Status**: Complete

## 研究概述

本文档整合了对 Old8Lang 代码库的深入探索结果，为文档更新提供准确的技术信息。所有信息均来自实际代码分析，确保文档内容与实现一致。

---

## 1. 三种执行模式详细对比

### 1.1 解释模式 (Interpretation Mode)

**实现位置**:
- 核心类: `Old8Lang/Interpreter/LangInterpreter.cs`
- Visitor: `Old8Lang/AST/Visitor/InterpreterVisitor.cs`
- 变量管理: `Old8Lang/Interpreter/VariateManager.cs`

**工作机制**:
```
源代码 → LangParser (词法+语法分析) → AST → InterpreterVisitor → 直接执行
```

**执行方法**: 每个 AST 节点实现 `Run(VariateManager manager)` 方法

**特点**:
- ✅ 完全动态类型，运行时类型检查
- ✅ 支持类型推断
- ✅ 支持泛型函数和泛型类
- ✅ 支持运算符重载
- ✅ 支持 Python 互操作
- ✅ 快速启动，无编译开销
- ✅ 灵活的运行时行为

**完成度**: 90-95%

**适用场景**:
- 快速开发和原型验证
- 脚本和自动化任务
- 需要动态特性的场景
- 调试和学习语言特性

**命令行参数**: `-f <file.old8>` 或 `--file <file.old8>`

---

### 1.2 编译模式 (Compilation Mode)

**实现位置**:
- 核心类: `Old8Lang/Compiler/Compiler.cs` (静态类)
- Visitor: `Old8Lang/AST/Visitor/CompilerVisitor.cs`
- 局部变量管理: `Old8Lang/Compiler/CodeGeneration/LocalManager.cs`

**工作机制**:
```
源代码 → LangParser → AST → CompilerVisitor → IL 代码 → .NET Runtime 执行
```

**IL 生成方法**: 每个 AST 节点实现 `GenerateIl(ILGenerator ilGenerator, LocalManager local)` 和 `LoadIlValue(ILGenerator ilGenerator, LocalManager local)` 方法

**特点**:
- ✅ 更高的运行时性能
- ✅ 静态类型检查（需要完整类型注解）
- ❌ 不支持泛型函数和泛型类
- ❌ 不支持运算符重载
- ❌ 不支持 Python 互操作
- ⚠️ 需要完整的类型注解
- ⚠️ 类型检查更严格

**完成度**: 70-85%

**适用场景**:
- 生产环境部署
- 性能关键的应用
- 需要静态类型保证的场景
- 长时间运行的服务

**命令行参数**: `-c <file.old8>` 或 `--compile <file.old8>`

---

### 1.3 VM 模式 (Bytecode VM Mode)

**实现位置**:
- 核心类: `Old8Lang/Bytecode/VM/VirtualMachine.Core.cs`
- 编译器: `Old8Lang/Bytecode/BytecodeCompiler.cs`
- Visitor: `Old8Lang/AST/Visitor/BytecodeVisitor.cs`
- 操作码: `Old8Lang/Bytecode/Core/OpCode.cs`

**工作机制**:
```
源代码 → LangParser → AST → BytecodeVisitor → 字节码 → VirtualMachine 执行
```

**字节码生成**: 每个 AST 节点通过 BytecodeVisitor 生成 `Instruction` 序列

**特点**:
- ✅ 字节码可序列化和分发
- ✅ 跨平台执行（无需重新编译）
- ✅ 内置调试器支持（断点、单步、变量查看）
- ✅ 性能分析器支持
- ✅ 完整的异常处理
- ✅ 支持异步和生成器
- ✅ 独立的指令集架构
- ⚠️ 性能介于解释模式和编译模式之间

**完成度**: 90-95% ✅ **已完整实现**

**适用场景**:
- 跨平台分发（一次编译，到处运行）
- 需要调试和性能分析的场景
- 沙箱执行环境
- 实验性功能测试

**命令行参数**: `-vm <file.old8>` 或 `--vm <file.old8>`

**字节码操作**:
- 编译字节码: `compile-bytecode <file.old8> -o <output.o8bc>`
- 执行字节码: `execute-bytecode <file.o8bc>`

---

### 1.4 三种模式对比表格

| 特性 | 解释模式 | 编译模式 | VM 模式 |
|------|---------|---------|---------|
| **启动速度** | 快 | 慢（需编译） | 中等 |
| **运行性能** | 中等 | 高 | 中等偏高 |
| **类型系统** | 动态类型 | 静态类型 | 动态类型 |
| **类型注解** | 可选 | 必需 | 可选 |
| **泛型支持** | ✅ | ❌ | ✅ |
| **运算符重载** | ✅ | ❌ | ✅ |
| **Python 互操作** | ✅ | ❌ | ✅ |
| **调试支持** | 基础 | 基础 | 高级（内置调试器） |
| **性能分析** | 无 | 无 | ✅ |
| **跨平台分发** | 需源代码 | 需源代码 | ✅ 字节码 |
| **完成度** | 90-95% | 70-85% | 90-95% |
| **适用场景** | 开发/脚本 | 生产/性能 | 分发/调试 |

---

## 2. 核心架构组件

### 2.1 Visitor 模式实现

**接口定义**: `Old8Lang/AST/Visitor/Generated/IVisitor.generated.cs`

```csharp
public interface IVisitor<TResult>
{
    TResult VisitBinaryExpression(BinaryExpression node);
    TResult VisitFunctionCallExpression(FunctionCallExpression node);
    // ... 86 个 AST 节点类型
}
```

**主要实现**:

1. **InterpreterVisitor** (`Old8Lang/AST/Visitor/InterpreterVisitor.cs`)
   - 返回类型: `LangValueType`
   - 职责: 直接执行 AST 节点，返回运行时值
   - 使用: 解释模式

2. **CompilerVisitor** (`Old8Lang/AST/Visitor/CompilerVisitor.cs`)
   - 返回类型: `object?`
   - 职责: 生成 IL 代码
   - 使用: 编译模式

3. **BytecodeVisitor** (`Old8Lang/AST/Visitor/BytecodeVisitor.cs`)
   - 返回类型: `Instruction?`
   - 职责: 生成字节码指令
   - 使用: VM 模式

4. **TypeInferenceVisitor** (`Old8Lang/AST/Visitor/TypeInferenceVisitor.cs`)
   - 返回类型: `Type?`
   - 职责: 类型推断
   - 使用: 所有模式（辅助）

**扩展方式**: 实现 `IVisitor<TResult>` 接口，为所有 86 个 AST 节点提供访问方法

---

### 2.2 AST 节点组织

**基类**:
- **LangExpression** (`Old8Lang/AST/LangExpression.cs`) - 所有表达式的基类
  - 方法: `Run()`, `LoadIlValue()`, `OutputType()`, `Accept<TResult>()`
- **OldStatement** (`Old8Lang/AST/OldStatement.cs`) - 所有语句的基类
  - 方法: `Run()`, `GenerateIl()`, `Accept<TResult>()`

**节点分类**:

```
Old8Lang/AST/
├── Expression/
│   ├── Core/              # 基础表达式（二元、一元、字面量等）
│   ├── Value/             # 值类型表达式
│   ├── Advanced/          # 高级表达式（访问、模式匹配等）
│   ├── Async/             # 异步表达式
│   ├── Generators/        # 生成器表达式
│   ├── Generics/          # 泛型表达式
│   └── Linq/              # LINQ 表达式
└── Statement/             # 语句节点（30+ 种）
```

**主要节点类型**:
- 表达式: BinaryExpression, UnaryExpression, FunctionCallExpression, LambdaExpression, etc.
- 语句: IfStatement, ForStatement, WhileStatement, TryStatement, ClassStatement, etc.

---

### 2.3 Parser 组织结构

**门面类**: `Old8Lang/LangParser/LangParser.cs`
- 提供统一的解析入口: `Parse(string code)`
- 使用 Facade 模式隐藏内部复杂性

**解析器组件**:

```
Old8Lang/LangParser/
├── LangParser.cs          # 门面类
├── LangToken.cs           # 词法单元
├── LangTokenType.cs       # 词法单元类型
├── Core/
│   └── ParserContext.cs   # 共享上下文
└── Parsers/
    ├── StatementParser.cs (+ 5 个分部类)
    ├── ExpressionParser.cs
    ├── PrimaryParser.cs (+ 7 个分部类)
    ├── FunctionParser.cs
    ├── ClassParser.cs
    ├── LinqParser.cs
    └── ExtensionParser.cs
```

**解析机制**: 递归下降解析 (Recursive Descent Parsing)

---

### 2.4 TypeSystem 实现

**核心组件**:

1. **TypeChecker** (`Old8Lang/TypeSystem/TypeChecker.cs`)
   - 职责: 验证类型正确性
   - 使用: 编译模式（严格检查）

2. **TypeInferenceEngine** (`Old8Lang/TypeSystem/TypeInferenceEngine.cs`)
   - 职责: 从上下文推断类型
   - 使用: 解释模式和 VM 模式

3. **GenericTypeInference** (`Old8Lang/TypeSystem/GenericTypeInference.cs`)
   - 职责: 泛型类型参数推断
   - 使用: 解释模式和 VM 模式

4. **TypeAnnotationManager** (`Old8Lang/TypeSystem/TypeAnnotationManager.cs`)
   - 职责: 管理类型注解
   - 使用: 所有模式

**类型检查差异**:
- 解释模式: 运行时类型检查，宽松
- 编译模式: 编译时静态类型检查，严格
- VM 模式: 运行时类型检查，宽松

---

## 3. 标准库模块清单

### 3.1 核心标准库 (Old8LangLib)

**位置**: `Old8LangLib/`

**模块列表**:

| 模块 | 文件 | 功能 |
|------|------|------|
| Math | MathLib.cs | 数学函数（sin, cos, sqrt, pow, etc.） |
| File | FileLib.cs | 文件操作（read, write, exists, delete, etc.） |
| Crypto | CryptoLib.cs | 加密功能（hash, encrypt, decrypt, etc.） |
| Image | ImageLib.cs | 图像处理（load, resize, filter, etc.） |
| Regex | RegexLib.cs | 正则表达式（match, replace, split, etc.） |
| Terminal | Terminal.cs | 终端操作（print, input, clear, etc.） |
| ColorfulTerminal | ColorfulTerminal.cs | 彩色终端输出 |
| Time | Time.cs | 时间处理（now, format, parse, etc.） |
| OS | OS.cs | 操作系统接口（env, exec, platform, etc.） |
| CSV | Csv.cs | CSV 文件处理 |
| Template | TemplateEngine.cs | 模板引擎 |
| Vector | VectorLib.cs | 向量运算 |

---

### 3.2 网络库 (Old8Lang.NetLib)

**位置**: `Old8Lang.NetLib/`

**模块列表**:

| 模块 | 文件 | 功能 |
|------|------|------|
| HTTP | HttpWebClient.cs | HTTP 客户端（GET, POST, PUT, DELETE, etc.） |
| WebSocket | WebSocketClient.cs | WebSocket 客户端 |
| MQTT | MqttClientWrapper.cs | MQTT 客户端（发布/订阅） |
| Socket | SocketClient.cs | 原始 Socket 操作 |
| WebAPI | WebApiClient.cs | RESTful API 客户端 |

---

### 3.3 数据库库 (Old8Lang.DatabaseLib)

**位置**: `Old8Lang.DatabaseLib/`

**模块列表**:

| 模块 | 文件 | 功能 |
|------|------|------|
| MySQL | MySqlConnectionWrapper.cs | MySQL 数据库连接 |
| PostgreSQL | PostgresConnectionWrapper.cs | PostgreSQL 数据库连接 |
| SQLite | SqliteConnectionWrapper.cs | SQLite 数据库连接 |
| InMemory | InMemoryConnectionWrapper.cs | 内存数据库 |
| ORM | OrmWrapper.cs | ORM 支持 |

---

### 3.4 序列化库 (Old8Lang.SerializationLib)

**位置**: `Old8Lang.SerializationLib/`

**模块列表**:

| 模块 | 文件 | 功能 |
|------|------|------|
| MessagePack | MessagePackSerializer.cs | MessagePack 序列化 |
| Protobuf | ProtobufSerializer.cs | Protobuf 序列化 |
| Factory | SerializerFactory.cs | 序列化工厂 |

---

### 3.5 机器学习库 (Old8Lang.MachineLearningLib)

**位置**: `Old8Lang.MachineLearningLib/`

**模块列表**:

| 模块 | 文件 | 功能 |
|------|------|------|
| Classification | ClassificationTrainer.cs | 分类模型训练 |
| Regression | RegressionTrainer.cs | 回归模型训练 |
| Clustering | ClusteringTrainer.cs | 聚类模型训练 |
| DataLoader | DataLoader.cs | 数据加载 |
| Predictor | ModelPredictor.cs | 模型预测 |

---

## 4. 包管理系统

### 4.1 命令清单

**位置**: `Old8Lang.App/Commands/`

**执行命令**:

| 命令 | 参数 | 功能 | 实现文件 |
|------|------|------|---------|
| `-f` | `<file.old8>` | 解释模式执行 | FromFileCommand.cs |
| `-c` | `<file.old8>` | 编译模式执行 | CompilerCommand.cs |
| `-vm` | `<file.old8>` | VM 模式执行 | VMCommand.cs |
| `-s` | `<file.old8>` | 语法检查 | SyntaxTestCommand.cs |
| `run` | `<file.old8>` | 运行（自动选择模式） | RunCommand.cs |

**包管理命令**:

| 命令 | 参数 | 功能 | 实现文件 |
|------|------|------|---------|
| `init` | `<project-name>` | 初始化项目 | InitCommand.cs |
| `install` | `<package-name>` | 安装包 | InstallCommand.cs |
| `remove` | `<package-name>` | 移除包 | RemoveCommand.cs |
| `restore` | - | 恢复包 | RestoreCommand.cs |
| `list` | - | 列出已安装包 | ListCommand.cs |
| `pack` | `<directory>` | 打包 | PackCommand.cs |
| `unpack` | `<package>` | 解包 | UnpackCommand.cs |
| `sign` | `-c <cert> -p <password>` | 签名包 | SignCommand.cs |
| `verify` | `<package>` | 验证签名 | VerifyCommand.cs |
| `cert` | `<subcommand>` | 证书管理 | CertCommand.cs |
| `publish` | `-c <cert> -p <password>` | 发布包（打包+签名） | PublishCommand.cs |

**调试和分析命令**:

| 命令 | 参数 | 功能 | 实现文件 |
|------|------|------|---------|
| `debug-start` | `<file.old8>` | 启动调试 | DebugStartCommand.cs |
| `debug-breakpoint` | `<line>` | 设置断点 | DebugBreakpointCommand.cs |
| `debug-control` | `<action>` | 调试控制 | DebugControlCommand.cs |
| `profile` | `<file.old8>` | 性能分析 | ProfileCommand.cs |

---

### 4.2 包管理工作流

**项目模式 vs 全局模式**:

- **项目模式**: 检测到 `o8packages.json` 文件时启用
  - 包安装到项目本地 `.o8packages/` 目录
  - 依赖记录在 `o8packages.json` 中
  - 适用于应用程序开发

- **全局模式**: 无 `o8packages.json` 文件时使用
  - 包安装到全局目录
  - 所有项目共享
  - 适用于工具和库开发

**包格式**: `.o8pkg` (压缩归档格式)

**包签名**: 使用 X.509 证书签名和验证

---

## 5. 文档组织和风格约定

### 5.1 现有文档结构

**位置**: `Docs/`

**文档清单**:

| 文档 | 大小 | 内容 |
|------|------|------|
| README.md | 小 | 项目简介 |
| ARCHITECTURE.md | 9KB | 架构文档 |
| CLI_GUIDE.md | 14KB | CLI 命令参考 |
| LANGUAGE_FEATURES.md | 12KB | 语言特性 |
| API_REFERENCE.md | 23KB | API 参考 |
| Old8Lang_Grammar.md | 98KB | 完整语法规范 |
| Old8Lang.ebnf | 15KB | EBNF 语法 |
| MODE_COMPLETION_STATUS.md | 中 | 模式完成度统计 |
| Mode_Support_Summary.md | 12KB | 模式支持总结 |
| API_Mode_Comparison.md | 8KB | API 模式对比 |
| ADVANCED_TOPICS.md | 28KB | 高级主题 |
| DEVELOPER_TOOLS.md | 22KB | 开发工具 |
| PERFORMANCE_GUIDE.md | 13KB | 性能指南 |
| CONTRIBUTING.md | 13KB | 贡献指南 |
| FAQ.md | 12KB | 常见问题 |
| ROADMAP.md | 16KB | 开发路线图 |
| CHANGELOG.md | 37KB | 更新日志 |

---

### 5.2 Markdown 格式约定

**标题层级**:
- H1 (`#`): 文档标题（每个文档一个）
- H2 (`##`): 主要章节
- H3 (`###`): 子章节
- H4 (`####`): 详细说明

**代码块**:
```old8lang
// 使用 old8lang 语言标注
function example() {
    print("Hello, World!")
}
```

**表格格式**:
- 使用 Markdown 表格
- 对齐方式统一（左对齐或居中）
- 表头使用粗体

**链接**:
- 内部链接: `[文本](./relative/path.md)`
- 外部链接: `[文本](https://example.com)`
- 代码位置: `path/to/file.cs:123`

---

### 5.3 中英文混排处理

**原则**:
- 主要内容使用中文
- 技术术语保留英文原文
- 首次出现时提供中英文对照

**示例**:
- "访问者模式 (Visitor Pattern)"
- "抽象语法树 (Abstract Syntax Tree, AST)"
- "中间语言 (Intermediate Language, IL)"

---

## 6. 技术决策总结

### 6.1 文档更新策略

**决策**: 增量更新现有文档，不重构整体结构

**理由**:
- 保持用户熟悉的文档组织
- 降低更新风险
- 减少工作量

**替代方案**: 重构文档结构
- **拒绝原因**: 风险高，工作量大，用户需要重新适应

---

### 6.2 三种模式的文档组织

**决策**: 在每个相关文档中添加三种模式的对比说明

**理由**:
- 用户可以在一个地方看到完整对比
- 避免信息分散
- 便于维护一致性

**替代方案**: 为每种模式创建独立文档
- **拒绝原因**: 信息重复，维护困难，用户需要在多个文档间跳转

---

### 6.3 代码示例的验证方式

**决策**: 所有代码示例必须实际运行验证

**理由**:
- 确保示例代码的正确性
- 避免误导用户
- 符合宪章"测试优先"原则

**验证方法**: 使用 `dotnet run --project Old8Lang.App -- -f example.old8` 测试

---

### 6.4 API 文档的组织方式

**决策**: 按标准库模块组织，每个模块包含完整的方法签名和示例

**理由**:
- 符合用户的心智模型（按功能查找）
- 便于维护和更新
- 清晰的层次结构

**替代方案**: 按字母顺序组织所有 API
- **拒绝原因**: 难以浏览，缺乏上下文

---

## 7. 下一步行动

### 7.1 Phase 1 任务

1. ✅ 生成 `data-model.md` - 定义文档结构模型
2. ✅ 生成 `contracts/` - 创建文档模板和约定
3. ✅ 生成 `quickstart.md` - 编写文档更新快速指南
4. ✅ 更新 Agent Context - 运行脚本更新 Claude 上下文

### 7.2 Phase 2 准备

- 准备进入 `/speckit.tasks` 命令
- 基于本研究文档生成详细的实施任务

---

## 附录: 关键文件路径速查

| 组件 | 文件路径 |
|------|---------|
| 解释器核心 | `Old8Lang/Interpreter/LangInterpreter.cs` |
| 编译器核心 | `Old8Lang/Compiler/Compiler.cs` |
| VM 核心 | `Old8Lang/Bytecode/VM/VirtualMachine.Core.cs` |
| Visitor 接口 | `Old8Lang/AST/Visitor/Generated/IVisitor.generated.cs` |
| Parser 门面 | `Old8Lang/LangParser/LangParser.cs` |
| 类型系统 | `Old8Lang/TypeSystem/` |
| 命令注册 | `Old8Lang.App/Program.cs` |
| 操作码定义 | `Old8Lang/Bytecode/Core/OpCode.cs` |

---

**研究完成日期**: 2026-02-14
**下一阶段**: Phase 1 - Design & Contracts
