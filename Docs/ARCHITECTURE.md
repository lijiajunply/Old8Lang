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

### 1.3 执行模式 (Execution Modes)

Old8Lang 支持三种执行模式，每种模式有不同的实现机制、性能特征和适用场景：

#### 1.3.1 解释模式 (Interpretation Mode)

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

#### 1.3.2 编译模式 (Compilation Mode)

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

#### 1.3.3 VM 模式 (Bytecode VM Mode) ⚠️ 实验性

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

**⚠️ 注意**: VM 模式目前处于实验阶段，虽然功能已完整实现，但建议在生产环境中谨慎使用。适合用于开发、测试和跨平台分发场景。

#### 1.3.4 三种模式对比

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

## 4. Visitor 模式详解

### 4.1 Visitor 模式概述

Old8Lang 使用 Visitor 模式作为核心设计模式，实现了对 AST 的多种处理方式。这种设计允许在不修改 AST 节点类的情况下，添加新的操作和执行模式。

### 4.2 IVisitor 接口定义

**位置**: `Old8Lang/AST/Visitor/IVisitor.cs`

```csharp
public interface IVisitor<out TResult>
{
    // 表达式访问
    TResult VisitLiteralExpression(LiteralExpression expr);
    TResult VisitBinaryExpression(BinaryExpression expr);
    TResult VisitFunctionCallExpression(FunctionCallExpression expr);
    TResult VisitLambdaExpression(LambdaExpression expr);
    TResult VisitArrayExpression(ArrayExpression expr);
    TResult VisitDictionaryExpression(DictionaryExpression expr);

    // 语句访问
    TResult VisitAssignmentStatement(AssignmentStatement stmt);
    TResult VisitIfStatement(IfStatement stmt);
    TResult VisitForStatement(ForStatement stmt);
    TResult VisitWhileStatement(WhileStatement stmt);
    TResult VisitFunctionDeclarationStatement(FunctionDeclarationStatement stmt);
    TResult VisitClassDeclarationStatement(ClassDeclarationStatement stmt);
    TResult VisitReturnStatement(ReturnStatement stmt);
    TResult VisitTryStatement(TryStatement stmt);

    // ... 其他 Visit 方法
}
```

### 4.3 四个主要 Visitor 实现

#### 4.3.1 InterpreterVisitor (解释执行)

**位置**: `Old8Lang/Interpreter/InterpreterVisitor.cs`

**返回类型**: `object` (运行时值)

**职责**:
- 直接执行 AST 节点并返回结果
- 管理运行时变量作用域
- 处理动态类型转换
- 支持泛型和运算符重载

**核心实现示例**:
```csharp
public object VisitBinaryExpression(BinaryExpression expr)
{
    var left = expr.Left.Accept(this);
    var right = expr.Right.Accept(this);

    switch (expr.Operator)
    {
        case "+": return Add(left, right);
        case "-": return Subtract(left, right);
        // ... 其他运算符
    }
}
```

**使用场景**: 解释模式 (`-f` 参数)

#### 4.3.2 CompilerVisitor (IL 代码生成)

**位置**: `Old8Lang/Compiler/CompilerVisitor.cs`

**返回类型**: `void` (生成 IL 指令到 ILGenerator)

**职责**:
- 将 AST 转换为 .NET IL 代码
- 管理局部变量和栈操作
- 执行静态类型检查
- 生成优化的机器码

**核心实现示例**:
```csharp
public void VisitBinaryExpression(BinaryExpression expr)
{
    expr.Left.Accept(this);  // 生成左操作数的 IL
    expr.Right.Accept(this); // 生成右操作数的 IL

    switch (expr.Operator)
    {
        case "+":
            ilGenerator.Emit(OpCodes.Add);
            break;
        case "-":
            ilGenerator.Emit(OpCodes.Sub);
            break;
        // ... 其他运算符
    }
}
```

**使用场景**: 编译模式 (`-c` 参数)

#### 4.3.3 BytecodeVisitor (字节码生成)

**位置**: `Old8Lang/Bytecode/BytecodeVisitor.cs`

**返回类型**: `List<Instruction>` (字节码指令序列)

**职责**:
- 将 AST 转换为自定义字节码
- 生成可序列化的指令序列
- 支持跨平台分发
- 提供调试信息

**核心实现示例**:
```csharp
public List<Instruction> VisitBinaryExpression(BinaryExpression expr)
{
    var instructions = new List<Instruction>();

    instructions.AddRange(expr.Left.Accept(this));
    instructions.AddRange(expr.Right.Accept(this));

    switch (expr.Operator)
    {
        case "+":
            instructions.Add(new Instruction(OpCode.Add));
            break;
        case "-":
            instructions.Add(new Instruction(OpCode.Sub));
            break;
        // ... 其他运算符
    }

    return instructions;
}
```

**使用场景**: VM 模式 (`-vm` 参数)

#### 4.3.4 TypeInferenceVisitor (类型推断)

**位置**: `Old8Lang/TypeSystem/TypeInferenceVisitor.cs`

**返回类型**: `TypeInfo` (推断的类型信息)

**职责**:
- 分析表达式的类型
- 推断泛型类型参数
- 验证类型兼容性
- 为编译模式提供类型信息

**核心实现示例**:
```csharp
public TypeInfo VisitBinaryExpression(BinaryExpression expr)
{
    var leftType = expr.Left.Accept(this);
    var rightType = expr.Right.Accept(this);

    switch (expr.Operator)
    {
        case "+":
            if (leftType.IsNumeric && rightType.IsNumeric)
                return TypeInfo.Number;
            if (leftType.IsString || rightType.IsString)
                return TypeInfo.String;
            break;
        // ... 其他运算符
    }

    throw new TypeException($"Cannot apply {expr.Operator} to {leftType} and {rightType}");
}
```

**使用场景**: 编译前类型分析、IDE 类型提示

### 4.4 Visitor 模式的优势

1. **关注点分离**: 执行逻辑与 AST 结构解耦
2. **易于扩展**: 添加新的执行模式只需实现新的 Visitor
3. **代码复用**: 同一 AST 可用于多种处理方式
4. **类型安全**: 泛型接口提供编译时类型检查
5. **维护性**: 修改执行逻辑不影响 AST 定义

## 5. AST 节点组织

### 5.1 AST 节点分类

Old8Lang 的 AST 节点分为两大类：

#### 5.1.1 Expression (表达式节点)

**位置**: `Old8Lang/AST/Expression/`

**基类**: `LangExpression`

**特点**: 有返回值，可以嵌套组合

**主要节点类型**:

| 节点类型 | 文件 | 描述 | 示例 |
|---------|------|------|------|
| LiteralExpression | LiteralExpression.cs | 字面量 | `42`, `"hello"`, `true` |
| VariableExpression | VariableExpression.cs | 变量引用 | `x`, `count` |
| BinaryExpression | BinaryExpression.cs | 二元运算 | `a + b`, `x * y` |
| UnaryExpression | UnaryExpression.cs | 一元运算 | `-x`, `!flag` |
| FunctionCallExpression | FunctionCallExpression.cs | 函数调用 | `print("hello")` |
| LambdaExpression | LambdaExpression.cs | Lambda 表达式 | `(x) => x * 2` |
| ArrayExpression | ArrayExpression.cs | 数组字面量 | `[1, 2, 3]` |
| DictionaryExpression | DictionaryExpression.cs | 字典字面量 | `{"key": "value"}` |
| IndexExpression | IndexExpression.cs | 索引访问 | `arr[0]`, `dict["key"]` |
| MemberAccessExpression | MemberAccessExpression.cs | 成员访问 | `obj.property` |
| TernaryExpression | TernaryExpression.cs | 三元运算 | `x > 0 ? 1 : -1` |

#### 5.1.2 Statement (语句节点)

**位置**: `Old8Lang/AST/Statement/`

**基类**: `OldStatement`

**特点**: 无返回值，控制程序流程

**主要节点类型**:

| 节点类型 | 文件 | 描述 | 示例 |
|---------|------|------|------|
| AssignmentStatement | AssignmentStatement.cs | 赋值语句 | `x <- 10` |
| IfStatement | IfStatement.cs | 条件语句 | `if x > 0 { ... }` |
| ForStatement | ForStatement.cs | For 循环 | `for i <- 0, i < 10, i <- i + 1 { ... }` |
| WhileStatement | WhileStatement.cs | While 循环 | `while x > 0 { ... }` |
| FunctionDeclarationStatement | FunctionDeclarationStatement.cs | 函数声明 | `func add(a, b) { return a + b }` |
| ClassDeclarationStatement | ClassDeclarationStatement.cs | 类声明 | `class Point { ... }` |
| ReturnStatement | ReturnStatement.cs | 返回语句 | `return result` |
| TryStatement | TryStatement.cs | 异常处理 | `try { ... } catch (e) { ... }` |
| ImportStatement | ImportStatement.cs | 模块导入 | `import "Math"` |
| BreakStatement | BreakStatement.cs | 跳出循环 | `break` |
| ContinueStatement | ContinueStatement.cs | 继续循环 | `continue` |

### 5.2 AST 节点接口

所有 AST 节点都实现以下接口：

```csharp
public abstract class LangExpression
{
    // Visitor 模式支持
    public abstract TResult Accept<TResult>(IVisitor<TResult> visitor);

    // 解释模式执行
    public abstract object Run(VariateManager manager);

    // 编译模式 IL 生成
    public abstract void GenerateIl(ILGenerator ilGenerator, LocalManager local);

    // 位置信息（用于错误报告）
    public SourceLocation Location { get; set; }
}

public abstract class OldStatement
{
    // Visitor 模式支持
    public abstract TResult Accept<TResult>(IVisitor<TResult> visitor);

    // 解释模式执行
    public abstract void Execute(VariateManager manager);

    // 编译模式 IL 生成
    public abstract void GenerateIl(ILGenerator ilGenerator, LocalManager local);

    // 位置信息（用于错误报告）
    public SourceLocation Location { get; set; }
}
```

### 5.3 AST 构建流程

```
源代码
    ↓
LangParser.Parse()
    ↓
Token 流
    ↓
递归下降解析
    ↓
AST 节点构建
    ↓
完整 AST 树
```

## 6. Parser 结构

### 6.1 Parser 架构

Old8Lang 的 Parser 使用 **Facade 模式** 和 **递归下降解析** 实现。

**主入口**: `Old8Lang/LangParser/LangParser.cs`

### 6.2 Parser 组件

#### 6.2.1 LangParser (Facade)

**位置**: `Old8Lang/LangParser/Core/LangParser.cs`

**职责**:
- 提供统一的解析入口
- 协调各个专用解析器
- 管理 Token 流
- 处理解析错误

**核心方法**:
```csharp
public class LangParser
{
    public static List<OldStatement> Parse(string code)
    {
        var tokens = Tokenize(code);
        var statements = new List<OldStatement>();

        while (!IsAtEnd())
        {
            statements.Add(ParseStatement());
        }

        return statements;
    }
}
```

#### 6.2.2 专用解析器

**位置**: `Old8Lang/LangParser/Parsers/`

| 解析器 | 文件 | 职责 |
|--------|------|------|
| ExpressionParser | ExpressionParser.cs | 解析表达式（运算符优先级、括号） |
| StatementParser | StatementParser.cs | 解析语句（赋值、控制流） |
| FunctionParser | FunctionParser.cs | 解析函数声明和调用 |
| ClassParser | ClassParser.cs | 解析类声明和成员 |
| TypeParser | TypeParser.cs | 解析类型注解 |
| ImportParser | ImportParser.cs | 解析 import 语句 |

#### 6.2.3 Token 定义

**位置**: `Old8Lang/LangParser/LangToken.cs`, `LangTokenType.cs`

**Token 类型**:
```csharp
public enum LangTokenType
{
    // 字面量
    Number, String, True, False, Null,

    // 标识符和关键字
    Identifier, Func, Class, If, Else, For, While, Return,

    // 运算符
    Plus, Minus, Star, Slash, Percent,
    Equal, NotEqual, Less, Greater, LessEqual, GreaterEqual,
    And, Or, Not,

    // 分隔符
    LeftParen, RightParen, LeftBrace, RightBrace, LeftBracket, RightBracket,
    Comma, Dot, Colon, Arrow, Assign,

    // 特殊
    Newline, EOF
}
```

### 6.3 递归下降解析机制

Old8Lang 使用递归下降解析，每个语法结构有对应的解析方法：

```csharp
// 解析表达式（处理运算符优先级）
Expression ParseExpression()
{
    return ParseTernary();
}

Expression ParseTernary()
{
    var expr = ParseLogicalOr();
    if (Match(TokenType.Question))
    {
        var trueExpr = ParseExpression();
        Consume(TokenType.Colon);
        var falseExpr = ParseExpression();
        return new TernaryExpression(expr, trueExpr, falseExpr);
    }
    return expr;
}

Expression ParseLogicalOr()
{
    var expr = ParseLogicalAnd();
    while (Match(TokenType.Or))
    {
        var op = Previous();
        var right = ParseLogicalAnd();
        expr = new BinaryExpression(expr, op, right);
    }
    return expr;
}

// ... 其他优先级层次
```

**运算符优先级** (从低到高):
1. 三元运算符 `? :`
2. 逻辑或 `||`
3. 逻辑与 `&&`
4. 相等性 `==`, `!=`
5. 比较 `<`, `>`, `<=`, `>=`
6. 加减 `+`, `-`
7. 乘除模 `*`, `/`, `%`
8. 一元 `-`, `!`
9. 调用和访问 `()`, `[]`, `.`
10. 基本表达式（字面量、变量、括号）

### 6.4 错误恢复

Parser 实现了错误恢复机制，在遇到语法错误时：
1. 记录错误信息（位置、消息）
2. 跳过错误的 Token
3. 同步到下一个语句边界
4. 继续解析后续代码

## 7. Type System (类型系统)

### 7.1 类型系统概述

Old8Lang 支持两种类型系统模式：
- **解释模式**: 动态类型 + 运行时检查
- **编译模式**: 静态类型 + 编译时检查

### 7.2 核心组件

#### 7.2.1 TypeChecker (类型检查器)

**位置**: `Old8Lang/TypeSystem/TypeChecker.cs`

**职责**:
- 验证类型正确性
- 检查类型兼容性
- 强制编译模式的类型注解

**核心方法**:
```csharp
public class TypeChecker
{
    public void CheckType(LangExpression expr, TypeInfo expectedType)
    {
        var actualType = InferType(expr);
        if (!IsCompatible(actualType, expectedType))
        {
            throw new TypeException($"Expected {expectedType}, got {actualType}");
        }
    }

    public bool IsCompatible(TypeInfo from, TypeInfo to)
    {
        // 类型兼容性规则
        if (from.Equals(to)) return true;
        if (to.IsAny) return true;
        if (from.IsNull && to.IsNullable) return true;
        // ... 其他规则
        return false;
    }
}
```

**使用场景**: 编译模式的类型验证

#### 7.2.2 TypeInferenceEngine (类型推断引擎)

**位置**: `Old8Lang/TypeSystem/TypeInferenceEngine.cs`

**职责**:
- 从上下文推断类型
- 减少显式类型注解需求
- 支持局部类型推断

**核心方法**:
```csharp
public class TypeInferenceEngine
{
    public TypeInfo InferType(LangExpression expr)
    {
        return expr.Accept(new TypeInferenceVisitor());
    }

    public TypeInfo InferFromContext(LangExpression expr, VariateManager manager)
    {
        // 从变量作用域推断
        if (expr is VariableExpression varExpr)
        {
            var value = manager.GetVariable(varExpr.Name);
            return TypeInfo.FromValue(value);
        }

        // 从函数签名推断
        if (expr is FunctionCallExpression callExpr)
        {
            var func = manager.GetFunction(callExpr.Name);
            return func.ReturnType;
        }

        // ... 其他推断规则
    }
}
```

**使用场景**: 解释模式的类型提示、编译模式的类型推断

#### 7.2.3 GenericTypeInference (泛型类型推断)

**位置**: `Old8Lang/TypeSystem/GenericTypeInference.cs`

**职责**:
- 推断泛型类型参数
- 处理泛型约束
- 支持泛型函数和泛型类

**核心方法**:
```csharp
public class GenericTypeInference
{
    public Dictionary<string, TypeInfo> InferTypeArguments(
        FunctionDeclarationStatement genericFunc,
        List<LangExpression> arguments)
    {
        var typeArgs = new Dictionary<string, TypeInfo>();

        for (int i = 0; i < arguments.Count; i++)
        {
            var paramType = genericFunc.Parameters[i].Type;
            var argType = InferType(arguments[i]);

            if (paramType.IsGenericParameter)
            {
                typeArgs[paramType.Name] = argType;
            }
        }

        return typeArgs;
    }
}
```

**支持的泛型类型**:
- `list<T>` - 泛型列表
- `array<T>` - 泛型数组
- `dict<K, V>` - 泛型字典
- 用户定义的泛型类和函数

**示例**:
```old8lang
// 泛型函数
func map<T, R>(list: list<T>, fn: func(T) -> R) -> list<R> {
    result <- []
    for item <- list {
        result.add(fn(item))
    }
    return result
}

// 类型推断：T = int, R = string
numbers <- [1, 2, 3]
strings <- map(numbers, (x) => "Number: " + x)
```

### 7.3 类型系统特点

| 特性 | 解释模式 | 编译模式 |
|------|---------|---------|
| **类型检查时机** | 运行时 | 编译时 |
| **类型注解** | 可选 | 必需 |
| **类型推断** | 支持 | 支持 |
| **泛型支持** | ✅ 完整支持 | ❌ 不支持 |
| **运算符重载** | ✅ 支持 | ❌ 不支持 |
| **隐式转换** | ✅ 支持 | ⚠️ 有限支持 |
| **类型错误** | 运行时异常 | 编译时错误 |

### 7.4 执行模式实现位置

#### 7.4.1 解释模式

**核心文件**:
- `Old8Lang/Interpreter/LangInterpreter.cs` - 解释器主类
- `Old8Lang/Interpreter/InterpreterVisitor.cs` - 解释执行 Visitor
- `Old8Lang/Interpreter/VariateManager.cs` - 变量管理

**执行流程**:
```
AST → InterpreterVisitor.Visit() → 直接执行 → 返回结果
```

#### 7.4.2 编译模式

**核心文件**:
- `Old8Lang/Compiler/Compiler.cs` - 编译器主类
- `Old8Lang/Compiler/CompilerVisitor.cs` - IL 生成 Visitor
- `Old8Lang/Compiler/LocalManager.cs` - 局部变量管理

**执行流程**:
```
AST → CompilerVisitor.Visit() → IL 代码 → .NET Runtime → 执行
```

#### 7.4.3 VM 模式

**核心文件**:
- `Old8Lang/Bytecode/VirtualMachine.Core.cs` - VM 核心
- `Old8Lang/Bytecode/BytecodeVisitor.cs` - 字节码生成 Visitor
- `Old8Lang/Bytecode/BytecodeInstruction.cs` - 指令定义

**执行流程**:
```
AST → BytecodeVisitor.Visit() → 字节码 → VirtualMachine.Execute() → 结果
```

## 8. 依赖关系

### 8.1 项目间依赖

| 项目 | 依赖项目 |
|------|----------|
| Old8Lang.App | Old8Lang |
| Old8LangLib | Old8Lang.NetLib |
| Old8Lang.Tests | Old8Lang, Old8LangLib |

### 8.2 外部库依赖

| 项目 | 外部库 |
|------|--------|
| Old8Lang | Microsoft.CodeAnalysis.Common, Microsoft.CodeAnalysis.CSharp, dnlib |
| Old8Lang.App | Colorful.Console |
| Old8LangLib | Colorful.Console, YamlDotNet |
| Old8Lang.NetLib | MQTTnet |
| Old8Lang.Tests | BenchmarkDotNet, coverlet.collector, Microsoft.NET.Test.Sdk, xunit, xunit.runner.visualstudio |

## 9. 工作流程

### 9.1 解释模式工作流程

1. **命令行输入**: 用户通过Old8Lang.App输入命令，指定要运行的Old8Lang文件
2. **文件读取**: 读取Old8Lang代码文件
3. **词法分析**: LangParser将代码转换为词法单元流
4. **语法分析**: 将词法单元流转换为抽象语法树(AST)
5. **解释执行**: LangInterpreter逐条解释执行AST节点
6. **输出结果**: 将执行结果输出到控制台

### 9.2 编译模式工作流程

1. **命令行输入**: 用户通过Old8Lang.App输入命令，指定要编译运行的Old8Lang文件
2. **文件读取**: 读取Old8Lang代码文件
3. **词法分析**: LangParser将代码转换为词法单元流
4. **语法分析**: 将词法单元流转换为抽象语法树(AST)
5. **中间代码生成**: Compiler将AST转换为IL代码
6. **IL代码验证**: ILVerifier验证生成的IL代码是否正确
7. **执行中间代码**: 执行生成的IL代码
8. **输出结果**: 将执行结果输出到控制台

## 10. 测试项目结构

### 10.1 测试目录

- **CompilerTests**: 编译模式测试用例
- **InterpreterTests**: 解释模式测试用例
- **SyntaxTests**: 语法测试用例
- **Old8Lang.Tests**: 单元测试和集成测试

### 10.2 测试运行方式

```bash
# 解释模式测试
dotnet run --project Old8Lang.App -- -f <path-to-test-file.old8>

# 编译模式测试
dotnet run --project Old8Lang.App -- -c <path-to-test-file.old8>

# 语法测试
dotnet run --project Old8Lang.App -- -s <path-to-test-file.old8>
```

## 11. 项目目录结构

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

## 12. 开发流程

1. **语法设计**: 在Old8Lang.ebnf中定义新语法
2. **解析器实现**: 在LangParser中实现语法解析
3. **AST节点定义**: 在AST模块中定义相应的节点类型
4. **解释器实现**: 在LangInterpreter中实现解释执行
5. **编译器实现**: 在Compiler中实现编译生成IL代码
6. **测试编写**: 编写测试用例验证功能
7. **文档更新**: 更新相关文档

## 13. 代码规范

- 使用有意义的、描述性的名称
- 遵循C#命名规范
- 函数应该只做一件事
- 保持适当的抽象层次
- 为公共API提供清晰的文档
- 测试覆盖率应达到较高水平

## 14. 未来发展方向

- 完善语言特性
- 优化性能
- 扩展标准库
- 改进错误处理和调试支持
- 提供IDE插件支持
- 支持更多平台
