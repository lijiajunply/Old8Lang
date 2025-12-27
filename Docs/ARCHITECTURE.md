# Old8Lang 项目架构文档

## 1. 项目概述

### 1.1 项目简介
Old8Lang 是一种简单的动态类型编程语言，支持解释模式和编译模式两种运行方式。它设计简洁，易于学习和使用，同时提供了完整的编程语言特性，包括函数、类、异常处理等。

### 1.2 技术栈
- **开发语言**: C#
- **目标框架**: .NET 10.0
- **外部库**:
  - Microsoft.CodeAnalysis.Common (5.0.0)
  - Microsoft.CodeAnalysis.CSharp (5.0.0)
  - dnlib (4.5.0)
  - Colorful.Console (1.2.15)
  - YamlDotNet (16.3.0)
  - MQTTnet (4.3.7.1207)

### 1.3 运行模式

#### 解释模式
解释模式下，Old8Lang 代码会逐条解释执行（运行 `Run` 方法），无需编译，适合快速开发和调试。

#### 编译模式
编译模式下，Old8Lang 代码会先被编译成中间代码（运行 `GenerateIl` 方法），然后再执行，适合性能要求较高的场景。

## 2. 模块架构

### 2.1 模块关系图

```
┌─────────────────┐     ┌─────────────────┐
│  Old8Lang.App   │────▶│   Old8Lang      │
└─────────────────┘     └─────────────────┘
                              ▲
                              │
┌─────────────────┐     ┌─────────────────┐
│ Old8Lang.Tests  │────▶│  Old8LangLib    │
└─────────────────┘     └─────────────────┘
                              ▲
                              │
                        ┌─────────────────┐
                        │ Old8Lang.NetLib │
                        └─────────────────┘
```

### 2.2 各模块详细描述

#### 2.2.1 Old8Lang（核心语言实现）
**功能**: 实现Old8Lang语言的核心功能，包括词法分析、语法分析、AST生成、解释执行和编译。

**组成模块**:
- **AST**: 抽象语法树定义，包括表达式和语句的各种节点类型
- **Compiler**: 编译成中间代码的实现
- **Error**: 错误类型和异常处理
- **LangParser**: 词法分析、语法分析和解释执行

#### 2.2.2 Old8Lang.App（命令行应用）
**功能**: 提供命令行界面，用于运行和测试Old8Lang代码。

**主要命令**:
- `-f <file>`: 解释模式运行Old8Lang代码
- `-c <file>`: 编译模式运行Old8Lang代码
- `-s <file>`: 语法测试Old8Lang代码

#### 2.2.3 Old8LangLib（标准库）
**功能**: 提供Old8Lang语言的标准库，包括数学、文件操作、字符串处理等常用功能。

**主要组件**:
- CollectionLib: 集合操作
- FileLib: 文件操作
- JsonLib: JSON处理
- MathLib: 数学函数
- StringLib: 字符串处理
- VectorLib: 向量运算

#### 2.2.4 Old8Lang.NetLib（网络库）
**功能**: 提供网络相关功能，包括HTTP、MQTT、Socket等。

**主要组件**:
- HttpClient: HTTP客户端
- MqttClientWrapper: MQTT客户端封装
- SocketClient: Socket客户端
- WebApiClient: Web API客户端
- WebSocketClient: WebSocket客户端

#### 2.2.5 Old8Lang.Tests（测试项目）
**功能**: 包含Old8Lang语言的各种测试，确保语言功能的正确性和稳定性。

**测试类型**:
- Integration: 集成测试
- Language: 语言特性测试
- Library: 标准库测试
- Parser: 解析器测试
- Performance: 性能测试

## 3. 核心模块内部结构

### 3.1 AST模块
AST（抽象语法树）模块定义了Old8Lang语言的语法结构，包括表达式和语句的各种节点类型。

**主要组成**:
- **Expression**: 表达式节点，包括常量、变量、函数调用、操作符等
- **Statement**: 语句节点，包括赋值、条件、循环、函数定义等
- **Intermediates**: 中间值类型，包括数组、字典、列表等
- **Value**: 各种值类型的定义

### 3.2 Compiler模块
Compiler模块负责将Old8Lang代码编译成中间代码。

**主要组件**:
- **Compiler**: 编译器主类，负责生成中间代码
- **ILVerifier**: IL代码验证器，确保生成的IL代码正确
- **LocalManager**: 局部变量管理器
- **TypeConversion**: 类型转换处理

### 3.3 Error模块
Error模块定义了Old8Lang语言的错误类型和异常处理机制。

**主要错误类型**:
- **CompilerException**: 编译时异常
- **RuntimeError**: 运行时异常
- **SyntaxError**: 语法错误
- **TypeError**: 类型错误
- **NameError**: 名称错误
- **IndexError**: 索引错误

### 3.4 LangParser模块
LangParser模块负责Old8Lang代码的词法分析、语法分析和解释执行。

**主要组件**:
- **Core**: 解析器核心类
- **ParserHelpers**: 解析器辅助类
- **Parsers**: 各种解析器，包括类解析器、表达式解析器、函数解析器等
- **LangInterpreter**: 解释器主类
- **LangParser**: 解析器主类
- **LangToken**: 词法单元定义
- **VariateManager**: 变量管理器

## 4. 依赖关系

### 4.1 项目间依赖

| 项目 | 依赖项目 |
|------|----------|
| Old8Lang.App | Old8Lang |
| Old8LangLib | Old8Lang.NetLib |
| Old8Lang.Tests | Old8Lang, Old8LangLib |

### 4.2 外部库依赖

| 项目 | 外部库 |
|------|--------|
| Old8Lang | Microsoft.CodeAnalysis.Common, Microsoft.CodeAnalysis.CSharp, dnlib |
| Old8Lang.App | Colorful.Console |
| Old8LangLib | Colorful.Console, YamlDotNet |
| Old8Lang.NetLib | MQTTnet |
| Old8Lang.Tests | BenchmarkDotNet, coverlet.collector, Microsoft.NET.Test.Sdk, xunit, xunit.runner.visualstudio |

## 5. 工作流程

### 5.1 解释模式工作流程

1. **命令行输入**: 用户通过Old8Lang.App输入命令，指定要运行的Old8Lang文件
2. **文件读取**: 读取Old8Lang代码文件
3. **词法分析**: LangParser将代码转换为词法单元流
4. **语法分析**: 将词法单元流转换为抽象语法树(AST)
5. **解释执行**: LangInterpreter逐条解释执行AST节点
6. **输出结果**: 将执行结果输出到控制台

### 5.2 编译模式工作流程

1. **命令行输入**: 用户通过Old8Lang.App输入命令，指定要编译运行的Old8Lang文件
2. **文件读取**: 读取Old8Lang代码文件
3. **词法分析**: LangParser将代码转换为词法单元流
4. **语法分析**: 将词法单元流转换为抽象语法树(AST)
5. **中间代码生成**: Compiler将AST转换为IL代码
6. **IL代码验证**: ILVerifier验证生成的IL代码是否正确
7. **执行中间代码**: 执行生成的IL代码
8. **输出结果**: 将执行结果输出到控制台

## 6. 测试项目结构

### 6.1 测试目录

- **CompilerTests**: 编译模式测试用例
- **InterpreterTests**: 解释模式测试用例
- **SyntaxTests**: 语法测试用例
- **Old8Lang.Tests**: 单元测试和集成测试

### 6.2 测试运行方式

```bash
# 解释模式测试
dotnet run --project Old8Lang.App -- -f <path-to-test-file.old8>

# 编译模式测试
dotnet run --project Old8Lang.App -- -c <path-to-test-file.old8>

# 语法测试
dotnet run --project Old8Lang.App -- -s <path-to-test-file.old8>
```

## 7. 项目目录结构

```
Old8Lang/
├── .cursor/             # Cursor编辑器配置
├── .idea/               # IDEA编辑器配置
├── .trae/               # Trae配置和文档
├── .vs/                 # Visual Studio配置
├── CompilerTests/       # 编译模式测试用例
├── InterpreterTests/    # 解释模式测试用例
├── Old8Lang/            # 核心语言实现
├── Old8Lang.App/        # 命令行应用
├── Old8Lang.NetLib/     # 网络库
├── Old8Lang.Tests/      # 测试项目
├── Old8LangLib/         # 标准库
├── Reports/             # 测试报告
├── SyntaxTests/         # 语法测试用例
├── CHANGELOG.md         # 更新日志
├── LICENSE              # 许可证
├── Old8Lang.sln         # 解决方案文件
├── Old8Lang_Grammar.md  # 语法文档
├── README.md            # 项目说明
```

## 8. 开发流程

1. **语法设计**: 在Old8Lang.ebnf中定义新语法
2. **解析器实现**: 在LangParser中实现语法解析
3. **AST节点定义**: 在AST模块中定义相应的节点类型
4. **解释器实现**: 在LangInterpreter中实现解释执行
5. **编译器实现**: 在Compiler中实现编译生成IL代码
6. **测试编写**: 编写测试用例验证功能
7. **文档更新**: 更新相关文档

## 9. 代码规范

- 使用有意义的、描述性的名称
- 遵循C#命名规范
- 函数应该只做一件事
- 保持适当的抽象层次
- 为公共API提供清晰的文档
- 测试覆盖率应达到较高水平

## 10. 未来发展方向

- 完善语言特性
- 优化性能
- 扩展标准库
- 改进错误处理和调试支持
- 提供IDE插件支持
- 支持更多平台
