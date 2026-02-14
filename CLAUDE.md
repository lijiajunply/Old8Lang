# CLAUDE.md

use Chinese to answer the questions

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Old8Lang is a dynamically-typed programming language implemented in C# (.NET 10.0). It supports three execution modes:
- **Interpretation Mode**: Direct AST execution for rapid development and debugging
- **Compilation Mode**: IL (Intermediate Language) code generation for better performance
- **Bytecode VM Mode**: Bytecode-based execution for cross-platform distribution and advanced debugging

The language features functions, classes, exception handling, async/await, generics, and a comprehensive standard library.

## Build and Run Commands

### Build the Project
```bash
dotnet build Old8Lang.sln
```

### Run Tests
```bash
# Run all tests
dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj

# Run specific test
dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj --filter "FullyQualifiedName~TestName"
```

### Run Old8Lang Code

Old8Lang 支持三种执行模式，每种模式有不同的特点和适用场景：

#### 三种执行模式对比

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
| **跨平台分发** | 需源代码 | 需源代码 | ✅ 字节码 |
| **适用场景** | 开发/脚本 | 生产/性能 | 分发/调试 |

#### 模式选择指南

- **解释模式** (`-f`): 用于快速开发、原型验证、脚本任务
- **编译模式** (`-c`): 用于生产环境、性能关键应用、长时间运行的服务
- **VM 模式** (`-vm`): 用于跨平台分发、调试分析、实验性功能测试（⚠️ 实验性）

#### 命令示例

**Interpretation Mode** (fast development, flexible):
```bash
dotnet run --project Old8Lang.App -- -f <file.old8>
```

**Compilation Mode** (better performance, stricter type checking):
```bash
dotnet run --project Old8Lang.App -- -c <file.old8>
```

**Bytecode VM Mode** (⚠️ experimental - cross-platform, advanced debugging):
```bash
dotnet run --project Old8Lang.App -- -vm <file.old8>
```

**Syntax Check Only**:
```bash
dotnet run --project Old8Lang.App -- -s <file.old8>
```

**Debug Mode** (enable verbose logging):
```bash
dotnet run --project Old8Lang.App -- -f <file.old8> -d
# Or with specific log level: --log-level debug|info|warning|error
```

### Package Management Commands

```bash
# Initialize a new project
dotnet run --project Old8Lang.App -- init <project-name>

# Install a package
dotnet run --project Old8Lang.App -- install <package-name>

# Remove a package
dotnet run --project Old8Lang.App -- remove <package-name>

# List installed packages
dotnet run --project Old8Lang.App -- list

# Publish a package (one-step: pack + sign)
dotnet run --project Old8Lang.App -- publish -c cert.pfx -p password
```

## High-Level Architecture

### Core Processing Pipeline

Old8Lang 支持三种执行模式，每种模式有不同的处理流程：

```
Source Code (.old8)
    ↓
LangParser (Tokenization + Parsing)
    ↓
Abstract Syntax Tree (AST)
    ├→ Interpretation Mode (-f)
    │   ↓
    │   InterpreterVisitor
    │   ↓
    │   Direct Execution (VariateManager)
    │   ↓
    │   Result
    │
    ├→ Compilation Mode (-c)
    │   ↓
    │   CompilerVisitor
    │   ↓
    │   IL Code Generation (ILGenerator)
    │   ↓
    │   .NET Runtime Execution
    │   ↓
    │   Result
    │
    └→ Bytecode VM Mode (-vm) ⚠️ Experimental
        ↓
        BytecodeVisitor
        ↓
        Bytecode Instructions
        ↓
        VirtualMachine Execution
        ↓
        Result
```

**模式特点**:
- **解释模式**: 最快的启动速度，支持完整的动态特性（泛型、运算符重载、Python互操作）
- **编译模式**: 最高的运行性能，需要完整类型注解，不支持某些动态特性
- **VM 模式**: 平衡性能和灵活性，支持字节码序列化、跨平台分发和高级调试功能

### Key Architectural Patterns

**1. Visitor Pattern (核心设计模式)**

Old8Lang 使用 Visitor 模式实现三种执行模式的统一处理。所有 AST 节点都实现 `Accept<TResult>(IVisitor<TResult> visitor)` 方法，允许不同的 Visitor 实现对同一 AST 进行不同的处理。

**IVisitor 接口定义** (`Old8Lang/AST/Visitor/IVisitor.cs`):
```csharp
public interface IVisitor<out TResult>
{
    TResult VisitExpression(LangExpression expression);
    TResult VisitStatement(OldStatement statement);
    TResult VisitFunctionDeclaration(FunctionDeclarationStatement function);
    TResult VisitClassDeclaration(ClassDeclarationStatement classDecl);
    // ... 其他 Visit 方法
}
```

**四个主要 Visitor 实现**:

1. **InterpreterVisitor** (`Old8Lang/Interpreter/InterpreterVisitor.cs`)
   - **用途**: 解释模式执行
   - **返回类型**: `object` (运行时值)
   - **特点**:
     - 直接执行 AST 节点
     - 使用 `VariateManager` 管理变量作用域
     - 支持完整的动态特性（泛型、运算符重载、Python 互操作）
     - 最快的启动速度
   - **核心方法**: `Visit(LangExpression expr)` → 直接返回计算结果

2. **CompilerVisitor** (`Old8Lang/Compiler/CompilerVisitor.cs`)
   - **用途**: 编译模式 IL 代码生成
   - **返回类型**: `void` (生成 IL 指令)
   - **特点**:
     - 将 AST 转换为 .NET IL 代码
     - 使用 `ILGenerator` 生成字节码
     - 需要完整类型注解
     - 最高的运行性能
   - **核心方法**: `Visit(LangExpression expr)` → 生成 IL 指令到 ILGenerator

3. **BytecodeVisitor** (`Old8Lang/Bytecode/BytecodeVisitor.cs`)
   - **用途**: VM 模式字节码生成
   - **返回类型**: `BytecodeInstruction[]` (字节码指令序列)
   - **特点**:
     - 将 AST 转换为自定义字节码
     - 支持字节码序列化和跨平台分发
     - 平衡性能和灵活性
     - 支持高级调试功能
   - **核心方法**: `Visit(LangExpression expr)` → 生成字节码指令

4. **TypeInferenceVisitor** (`Old8Lang/TypeSystem/TypeInferenceVisitor.cs`)
   - **用途**: 类型推断和检查
   - **返回类型**: `TypeInfo` (推断的类型信息)
   - **特点**:
     - 在编译前进行类型分析
     - 支持泛型类型推断
     - 为编译模式提供类型信息
   - **核心方法**: `Visit(LangExpression expr)` → 返回表达式的类型信息

**Visitor 模式的优势**:
- **关注点分离**: 执行逻辑与 AST 结构解耦
- **易于扩展**: 添加新的执行模式只需实现新的 Visitor
- **代码复用**: 同一 AST 可用于多种处理方式
- **类型安全**: 泛型接口提供编译时类型检查

**2. 三模式执行架构**

Old8Lang 的每个 AST 节点支持三种执行方式：

1. **解释执行** (`Run` 方法):
   ```csharp
   public object Run(VariateManager manager)
   {
       // 直接执行并返回结果
       return result;
   }
   ```

2. **编译执行** (`GenerateIl` 方法):
   ```csharp
   public void GenerateIl(ILGenerator ilGenerator, LocalManager local)
   {
       // 生成 IL 指令
       ilGenerator.Emit(OpCodes.Ldloc, localIndex);
   }
   ```

3. **VM 执行** (`Accept` 方法 + BytecodeVisitor):
   ```csharp
   public TResult Accept<TResult>(IVisitor<TResult> visitor)
   {
       return visitor.VisitExpression(this);
   }
   ```

这种设计允许同一 AST 在不同模式下执行，无需重新解析源代码。

**3. Symbol Table Management (符号表管理)**

- **VariateManager** (`Old8Lang/Interpreter/VariateManager.cs`)
  - 管理解释模式下的变量和作用域
  - 支持嵌套作用域和闭包
  - 提供变量查找和赋值操作

- **SymbolTableCache** (`Old8Lang/TypeSystem/SymbolTableCache.cs`)
  - 缓存符号信息以提高性能
  - 用于编译模式的符号解析

- **TypeAnnotationManager** (`Old8Lang/TypeSystem/TypeAnnotationManager.cs`)
  - 管理类型注解信息
  - 支持类型推断和验证

**4. Type System (类型系统)**

- **TypeChecker** (`Old8Lang/TypeSystem/TypeChecker.cs`)
  - 验证类型正确性
  - 编译模式下强制类型检查

- **TypeInferenceEngine** (`Old8Lang/TypeSystem/TypeInferenceEngine.cs`)
  - 从上下文推断类型
  - 减少显式类型注解需求

- **GenericTypeInference** (`Old8Lang/TypeSystem/GenericTypeInference.cs`)
  - 处理泛型类型参数
  - 支持 `list<T>`, `array<T>`, `dict<K,V>` 等泛型类型

**类型系统特点**:
- 解释模式: 动态类型 + 运行时检查
- 编译模式: 静态类型 + 编译时检查
- VM 模式: 动态类型 + 字节码验证

### Directory Structure

**Core Language Implementation** (`Old8Lang/`):

- **`AST/`** - Abstract Syntax Tree node definitions
  - `Expression/` - Expression nodes (literals, operations, function calls)
    - `LiteralExpression.cs` - 字面量表达式（数字、字符串、布尔值）
    - `BinaryExpression.cs` - 二元运算表达式
    - `FunctionCallExpression.cs` - 函数调用表达式
    - `LambdaExpression.cs` - Lambda 表达式
  - `Statement/` - Statement nodes (assignments, loops, conditionals)
    - `AssignmentStatement.cs` - 赋值语句
    - `IfStatement.cs` - 条件语句
    - `ForStatement.cs` - 循环语句
    - `FunctionDeclarationStatement.cs` - 函数声明
    - `ClassDeclarationStatement.cs` - 类声明
  - `Visitor/` - Visitor pattern implementations
    - `IVisitor.cs` - Visitor 接口定义
    - `BaseVisitor.cs` - Visitor 基类实现

- **`LangParser/`** - Lexical and syntax analysis
  - `Core/` - Parser core logic
    - `LangParser.cs` - 主解析器入口（Facade 模式）
    - `LangToken.cs` - Token 定义
    - `LangTokenType.cs` - Token 类型枚举
  - `Parsers/` - Specialized parsers for different constructs
    - `ExpressionParser.cs` - 表达式解析器
    - `StatementParser.cs` - 语句解析器
    - `FunctionParser.cs` - 函数解析器
    - `ClassParser.cs` - 类解析器
  - **递归下降解析**: 每个语法结构有对应的解析方法

- **`Compiler/`** - IL code generation for compilation mode
  - `Compiler.cs` - 编译器主类
  - `CompilerVisitor.cs` - IL 代码生成 Visitor
  - `LocalManager.cs` - 局部变量管理
  - `ILGeneratorExtensions.cs` - IL 生成辅助方法
  - **实现位置**: `Old8Lang/Compiler/`
  - **核心类**: `CompilerVisitor` 实现 `IVisitor<void>`

- **`Interpreter/`** - Runtime execution for interpretation mode
  - `LangInterpreter.cs` - 解释器主类
  - `InterpreterVisitor.cs` - 解释执行 Visitor
  - `VariateManager.cs` - 变量和作用域管理
  - `RuntimeContext.cs` - 运行时上下文
  - **实现位置**: `Old8Lang/Interpreter/`
  - **核心类**: `InterpreterVisitor` 实现 `IVisitor<object>`

- **`TypeSystem/`** - Type checking and inference
  - `TypeChecker.cs` - 类型检查器
  - `TypeInferenceEngine.cs` - 类型推断引擎
  - `TypeInferenceVisitor.cs` - 类型推断 Visitor
  - `GenericTypeInference.cs` - 泛型类型推断
  - `TypeAnnotationManager.cs` - 类型注解管理
  - `SymbolTableCache.cs` - 符号表缓存
  - **职责**: 编译前类型分析、泛型推断、类型验证

- **`Bytecode/`** - Bytecode VM implementation (⚠️ 实验性)
  - `BytecodeVisitor.cs` - 字节码生成 Visitor
  - `VirtualMachine.Core.cs` - VM 核心执行引擎
  - `BytecodeInstruction.cs` - 字节码指令定义
  - `BytecodeSerializer.cs` - 字节码序列化
  - `VMStack.cs` - VM 栈管理
  - `VMDebugger.cs` - VM 调试器
  - **实现位置**: `Old8Lang/Bytecode/`
  - **核心类**: `BytecodeVisitor` 实现 `IVisitor<BytecodeInstruction[]>`
  - **特性**: 支持字节码序列化、跨平台分发、高级调试

- **`ModuleSystem/`** - Module loading and resolution
  - `ModuleLoader.cs` - 模块加载器
  - `ModuleResolver.cs` - 模块解析器
  - `ModuleCache.cs` - 模块缓存
  - **解析顺序**: 项目包 → 全局包 → 标准库

- **`GlobalFunctions/`** - Built-in global functions
  - `Print.cs` - 输出函数
  - `Input.cs` - 输入函数
  - `TypeConversion.cs` - 类型转换函数
  - `CollectionFunctions.cs` - 集合操作函数

- **`StandardLibrary/`** - Standard library implementation
  - `Math/` - 数学函数
  - `String/` - 字符串操作
  - `File/` - 文件操作
  - `Collection/` - 集合操作

- **`Error/`** - Error types and exception handling
  - `LangException.cs` - 语言异常基类
  - `ParseException.cs` - 解析异常
  - `RuntimeException.cs` - 运行时异常
  - `TypeException.cs` - 类型异常

**Application Layer** (`Old8Lang.App/`):
- `Program.cs` - CLI entry point with command registry
- `Commands/` - Command implementations (FromFileCommand, CompilerCommand, etc.)

**Standard Libraries**:
- `Old8LangLib/` - Core standard library (Math, String, File, Collection)
- `Old8Lang.NetLib/` - Network functionality (HTTP, MQTT, WebSocket)
- `Old8Lang.SerializationLib/` - Serialization support
- `Old8Lang.MachineLearningLib/` - ML capabilities
- `Old8Lang.DatabaseLib/` - Database operations
- `Old8Lang.LanguageServer/` - LSP server for IDE integration
- `Old8Lang.FirstUI/` - UI framework

**Testing** (`Old8Lang.Tests/`):
- Uses xUnit framework
- Test directories mirror the main codebase structure

## Important Implementation Details

### Adding New Language Features

1. **Define AST Node**: Create new node class in `AST/Expression/` or `AST/Statement/`
2. **Implement Dual Execution**:
   - `Run(VariateManager manager)` for interpretation mode
   - `GenerateIl(ILGenerator ilGenerator, LocalManager local)` for compilation mode
3. **Add Visitor Support**: Implement `Accept(IVisitor visitor)` method
4. **Update Parser**: Add parsing logic in appropriate parser class in `LangParser/Parsers/`
5. **Add Tests**: Create test cases in both `InterpreterTests/` and `CompilerTests/`

### Working with the Parser

- Entry point: `LangParser.Parse(string code)` returns AST
- Token definitions: `LangToken.cs` and `LangTokenType.cs`
- Parser uses recursive descent parsing
- Each language construct has a dedicated parser in `LangParser/Parsers/`

### Type System Considerations

- Interpretation mode is dynamically typed with runtime type checking
- Compilation mode requires type annotations and performs static type checking
- Generic types are supported: `list<T>`, `array<T>`, `dict<K,V>`
- Type inference engine can infer types from context in many cases

### Module System

- Modules are loaded via `import "ModuleName"` syntax
- Module resolution checks:
  1. Project-level packages (if `o8packages.json` exists)
  2. Global packages
  3. Standard library
- Module caching prevents duplicate loading

### Package Management

- Project mode: Detected by presence of `o8packages.json`
- Global mode: Used when no project configuration exists
- Packages are stored in `.o8pkg` format (compressed archives)
- Package signing and verification supported via certificates

## Common Development Patterns

### Reading Code Before Modifying

Always read the relevant files before making changes. The codebase uses consistent patterns:
- AST nodes follow a standard structure with `Run()` and `GenerateIl()` methods
- Parsers follow recursive descent pattern
- Error handling uses custom exception types in `Error/` directory

### Testing Changes

Test both execution modes when adding features:
```bash
# Test interpretation mode
dotnet run --project Old8Lang.App -- -f path/to/test.old8

# Test compilation mode
dotnet run --project Old8Lang.App -- -c path/to/test.old8

# Run unit tests
dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj
```

### Debugging

Enable debug output to see detailed execution logs:
```bash
dotnet run --project Old8Lang.App -- -f test.old8 -d
```

This shows:
- Token stream from lexer
- AST structure
- IL generation steps (in compilation mode)
- Variable scope changes
- Type inference decisions

## Key Files to Understand

**核心解析和执行**:
- `Old8Lang/LangParser/LangParser.cs` - Main parser entry point (Facade 模式)
- `Old8Lang/Interpreter/LangInterpreter.cs` - Interpretation mode executor
- `Old8Lang/Compiler/Compiler.cs` - Compilation mode IL generator
- `Old8Lang/AST/LangExpression.cs` - Base class for all expressions
- `Old8Lang/AST/OldStatement.cs` - Base class for all statements
- `Old8Lang/Interpreter/VariateManager.cs` - Variable/scope management
- `Old8Lang.App/Program.cs` - CLI command registry and entry point

**Visitor 模式实现**:
- `Old8Lang/AST/Visitor/IVisitor.cs` - Visitor 接口定义
- `Old8Lang/Interpreter/InterpreterVisitor.cs` - 解释模式 Visitor (返回 object)
- `Old8Lang/Compiler/CompilerVisitor.cs` - 编译模式 Visitor (生成 IL)
- `Old8Lang/Bytecode/BytecodeVisitor.cs` - VM 模式 Visitor (生成字节码)
- `Old8Lang/TypeSystem/TypeInferenceVisitor.cs` - 类型推断 Visitor (返回 TypeInfo)

**Bytecode VM 实现** (⚠️ 实验性):
- `Old8Lang/Bytecode/VirtualMachine.Core.cs` - VM 核心执行引擎
- `Old8Lang/Bytecode/BytecodeInstruction.cs` - 字节码指令定义
- `Old8Lang/Bytecode/BytecodeSerializer.cs` - 字节码序列化/反序列化
- `Old8Lang/Bytecode/VMStack.cs` - VM 栈管理
- `Old8Lang/Bytecode/VMDebugger.cs` - VM 调试器支持

**类型系统**:
- `Old8Lang/TypeSystem/TypeChecker.cs` - 类型检查器
- `Old8Lang/TypeSystem/TypeInferenceEngine.cs` - 类型推断引擎
- `Old8Lang/TypeSystem/GenericTypeInference.cs` - 泛型类型推断

## Documentation

Comprehensive documentation is available in the `Docs/` directory:
- `ARCHITECTURE.md` - Detailed architecture documentation
- `CLI_GUIDE.md` - Complete CLI command reference
- `LANGUAGE_FEATURES.md` - Language feature documentation
- `API_REFERENCE.md` - API documentation
- `Old8Lang_Grammar.md` - Formal grammar specification
