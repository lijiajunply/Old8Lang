# Old8Lang 高级主题

本文档汇总 Old8Lang 的高级主题，包括 Extern 功能工厂架构、渐进式类型推断系统和包开发指南。

## 目录

- [Extern 功能工厂架构](#extern-功能工厂架构)
  - [架构设计](#架构设计)
  - [已实现的提供者](#已实现的提供者)
  - [工厂使用方式](#工厂使用方式)
  - [未来扩展示例](#未来扩展示例)
- [渐进式类型推断系统](#渐进式类型推断系统)
  - [功能特性](#功能特性)
  - [架构设计](#架构设计-1)
  - [使用方法](#使用方法)
  - [限制和已知问题](#限制和已知问题)
- [包开发和发布指南](#包开发和发布指南)
  - [包结构](#包结构)
  - [创建包](#创建包)
  - [package.json 配置](#packagejson-配置)
  - [测试包](#测试包)
  - [发布包](#发布包)
  - [最佳实践](#最佳实践)

---

## Extern 功能工厂架构

### 概述

Extern 功能已重构为基于工厂模式的可扩展架构，方便未来添加更多语言支持（如 JavaScript、Ruby、Lua 等）。

### 架构设计

#### 核心组件

```
┌─────────────────────────────────────────────────────┐
│              ExternStatement                        │
│      (使用工厂获取 Provider 并委托执行)               │
└──────────────────┬──────────────────────────────────┘
                   │
                   ▼
┌──────────────────────────────────────────────────────┐
│          ExternProviderFactory                       │
│     (根据 ExternType 创建对应的 Provider)             │
│  ├─ CreateProvider(ExternType) → IExternProvider    │
│  ├─ RegisterProvider(ExternType, Func<...>)         │
│  ├─ IsSupported(ExternType) → bool                  │
│  └─ GetSupportedTypes() → IEnumerable<ExternType>   │
└──────────────────┬───────────────────────────────────┘
                   │
       ┌─────────────┼─────────────┐
       ▼             ▼             ▼
┌──────────┐  ┌──────────┐  ┌──────────────┐
│IExtern   │  │IExtern   │  │IExtern       │
│Provider  │  │Provider  │  │Provider      │
└──────────┘  └──────────┘  └──────────────┘
       │             │             │
       ▼             ▼             ▼
┌──────────┐  ┌──────────┐  ┌──────────────┐
│NativeDll │  │Python    │  │(Future)      │
│Provider  │  │Provider  │  │JavaScript    │
│(P/Invoke)│  │          │  │Provider      │
└──────────┘  └──────────┘  └──────────────┘
```

#### 文件结构

```
Old8Lang/
├── ExternProviders/                    # 新增：Extern 提供者目录
│   ├── IExternProvider.cs              # 提供者接口
│   ├── ExternProviderFactory.cs        # 工厂类
│   ├── NativeDllProvider.cs            # C/C++ P/Invoke 提供者
│   ├── PythonProvider.cs               # Python 提供者
│   └── JavaScriptProvider.cs           # JavaScript 提供者（新增）
└── AST/
    ├── Expression/
    │   └── Value/
    │       ├── PythonFunctionLangValue.cs      # Python 函数包装器
    │       └── JavaScriptFunctionLangValue.cs  # JavaScript 函数包装器（新增）
    └── Statement/
        └── ExternStatement.cs          # 重构后：使用工厂模式
```

### 接口定义

#### IExternProvider

```csharp
public interface IExternProvider
{
    /// <summary>
    /// 解释模式执行：加载外部函数并注册到变量管理器
    /// </summary>
    void LoadFunctions(
        string source,
        List<ExternFunctionDeclaration> functions,
        CallingConventionType defaultCallingConvention,
        VariateManager manager);

    /// <summary>
    /// 编译模式：生成 IL 代码
    /// </summary>
    void GenerateIL(
        string source,
        List<ExternFunctionDeclaration> functions,
        CallingConventionType defaultCallingConvention,
        ILGenerator ilGenerator,
        LocalManager local);

    /// <summary>
    /// 是否支持编译模式
    /// </summary>
    bool SupportsCompilation { get; }
}
```

### 已实现的提供者

#### 1. NativeDllProvider（C/C++ P/Invoke）

**功能：**
- 加载原生 DLL 中的函数
- 支持三种调用约定：Cdecl、StdCall、WinApi
- 完全支持编译模式和解释模式

**实现特点：**
- 使用 `System.Runtime.InteropServices.NativeLibrary`
- 动态创建委托类型
- 高性能直接调用

**示例：**
```old8
native extern "msvcrt.dll" func abs(x:int) -> int
native extern "kernel32.dll" stdcall func GetCurrentThreadId() -> int
```

#### 2. PythonProvider

**功能：**
- 导入 Python 脚本文件（`.py` 或 `py:` 前缀）
- 导入 Python 全局模块（`pymodule:` 前缀）
- 自动检测和初始化 Python 运行时
- 支持跨平台（Windows/macOS/Linux）

**实现特点：**
- 使用 Python.NET (pythonnet 3.0.4)
- 自动 Python/Old8Lang 类型转换
- 仅支持解释模式（`SupportsCompilation = false`）

**示例：**
```old8
// Python 脚本文件
native extern "math_utils.py" {
    func add(a:int, b:int) -> int,
    func multiply(a:int, b:int) -> int
}

// Python 标准库模块
native extern "pymodule:math" {
    func sqrt(x:double) -> double,
    func pow(base:double, exp:double) -> double
}
```

#### 3. JavaScriptProvider

**功能：**
- 导入 JavaScript 脚本文件（`.js` 或 `js:` 前缀）
- 使用 Jint 引擎执行 JavaScript 代码
- 支持跨平台（Windows/macOS/Linux）

**实现特点：**
- 使用 Jint 4.2.0（纯 C# 实现的 JavaScript 解释器）
- 自动 JavaScript/Old8Lang 类型转换
- 仅支持解释模式（`SupportsCompilation = false`）
- 支持 ES5.1 标准

**示例：**
```old8
// JavaScript 脚本文件
native extern "math_utils.js" {
    func add(a:int, b:int) -> int,
    func multiply(a:int, b:int) -> int
}

// 使用 js: 前缀
native extern "js:utils.js" {
    func greet(name:string) -> string,
    func getArray() -> object
}
```

### 工厂使用方式

#### 创建提供者

```csharp
// 自动根据 ExternType 创建提供者
var provider = ExternProviderFactory.CreateProvider(ExternType.NativeDll);
var provider = ExternProviderFactory.CreateProvider(ExternType.PythonScript);
```

#### 注册自定义提供者

```csharp
// 未来添加新语言支持示例
ExternProviderFactory.RegisterProvider(
    ExternType.JavaScript,
    () => new JavaScriptProvider()
);
```

#### 检查支持

```csharp
// 检查是否支持某种类型
bool isSupported = ExternProviderFactory.IsSupported(ExternType.PythonModule);

// 获取所有已注册类型
var types = ExternProviderFactory.GetSupportedTypes();
```

### ExternStatement 重构

#### 重构前

```csharp
public override void Run(VariateManager manager)
{
    if (ExternType == ExternType.NativeDll)
    {
        RunNativeDll(manager);  // 硬编码分支
    }
    else if (ExternType is ExternType.PythonScript or ExternType.PythonModule)
    {
        RunPython(manager);     // 硬编码分支
    }
}
```

#### 重构后

```csharp
public override void Run(VariateManager manager)
{
    // 使用工厂创建对应的提供者
    var provider = ExternProviderFactory.CreateProvider(ExternType);

    // 委托给提供者执行
    provider.LoadFunctions(DllName, Functions, DefaultCallingConvention, manager);
}

public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
{
    // 使用工厂创建对应的提供者
    var provider = ExternProviderFactory.CreateProvider(ExternType);

    // 检查是否支持编译模式
    if (!provider.SupportsCompilation)
    {
        throw new NotSupportedException(
            $"{ExternType} 类型的 extern 函数不支持编译模式，仅支持解释模式执行。");
    }

    // 委托给提供者生成 IL 代码
    provider.GenerateIL(DllName, Functions, DefaultCallingConvention, ilGenerator, local);
}
```

### 优势

#### 1. 可扩展性
- **添加新语言**只需：
  1. 创建新的 `IExternProvider` 实现类
  2. 在工厂中注册（或运行时注册）
  3. 无需修改 `ExternStatement` 核心代码

#### 2. 解耦
- 每种语言的具体实现完全独立
- 核心逻辑（`ExternStatement`）与具体实现分离
- 便于单元测试和维护

#### 3. 统一接口
- 所有语言提供者遵循相同的接口契约
- 编译模式支持通过 `SupportsCompilation` 属性明确声明
- 错误处理统一规范

#### 4. 类型安全
- 工厂方法返回具体的 `IExternProvider` 接口
- 编译时类型检查
- 运行时类型转换由各提供者内部处理

### 未来扩展示例

#### 添加 JavaScript 支持

```csharp
// 1. 创建 JavaScriptProvider.cs
public class JavaScriptProvider : IExternProvider
{
    public bool SupportsCompilation => false;

    public void LoadFunctions(
        string source,
        List<ExternFunctionDeclaration> functions,
        CallingConventionType defaultCallingConvention,
        VariateManager manager)
    {
        // 使用 Jint 或其他 JS 引擎
        var engine = new Jint.Engine();
        engine.Execute(File.ReadAllText(source));

        foreach (var func in functions)
        {
            // 包装 JS 函数为 Old8Lang 函数
            // ...
        }
    }

    public void GenerateIL(...)
    {
        throw new NotSupportedException("JS 不支持编译模式");
    }
}

// 2. 在工厂中注册
ExternProviderFactory.RegisterProvider(
    ExternType.JavaScript,
    () => new JavaScriptProvider()
);

// 3. 在 ExternType 枚举中添加
public enum ExternType
{
    NativeDll,
    PythonScript,
    PythonModule,
    JavaScript  // 新增
}

// 4. 在 DetectExternType 中添加检测逻辑
public static ExternType DetectExternType(string dllName)
{
    if (dllName.EndsWith(".js", StringComparison.OrdinalIgnoreCase))
        return ExternType.JavaScript;
    // ...
}
```

#### 添加 Ruby 支持

类似方式，创建 `RubyProvider` 并注册即可。

### 测试

#### 构建测试

```bash
dotnet build Old8Lang.sln
```

#### 语法测试

```bash
dotnet run --project Old8Lang.App -- -s InterpreterTests/test_python_extern_basic.old8
```

#### 功能测试

```old8
// test_extern_factory.old8
native extern "pymodule:math" {
    func sqrt(x:double) -> double,
    func pow(base:double, exp:double) -> double
}

result <- sqrt(16.0)
PrintLine("sqrt(16.0) = " + result.ToStr())

result2 <- pow(2.0, 3.0)
PrintLine("pow(2.0, 3.0) = " + result2.ToStr())
```

```bash
dotnet run --project Old8Lang.App -- -f test_extern_factory.old8
```

### 兼容性

#### 向后兼容
- 重构完全向后兼容
- 所有现有的 extern 语句无需修改
- 语法和行为保持不变

#### 编译模式支持

| Provider | 解释模式 (-f) | 编译模式 (-c) |
|----------|--------------|--------------|
| NativeDllProvider | ✅ 支持 | ✅ 支持 |
| PythonProvider | ✅ 支持 | ❌ 不支持 |
| JavaScriptProvider | ✅ 支持 | ❌ 不支持 |

### 技术细节

#### 依赖注入
- 工厂使用简单的字典注册表
- 支持运行时动态注册新提供者
- 提供者实例由工厂方法创建

#### 错误处理
- 不支持的 ExternType：抛出 `NotSupportedException`
- 编译模式检查：在 `GenerateIl` 中验证 `SupportsCompilation`
- 具体错误由各提供者内部处理

#### 性能影响
- 工厂创建开销：极小（字典查找 + 委托调用）
- 运行时性能：与重构前相同
- P/Invoke 性能：无变化（直接调用）
- Python 性能：无变化（Python.NET 开销）

### 维护建议

1. **添加新语言时**：
   - 继承 `IExternProvider` 接口
   - 实现 `LoadFunctions` 和 `GenerateIL` 方法
   - 正确设置 `SupportsCompilation` 属性
   - 在工厂中注册

2. **修改现有提供者时**：
   - 只需修改对应的 Provider 类
   - 无需触及其他提供者或核心代码

3. **测试要求**：
   - 为每个新提供者添加单元测试
   - 测试解释模式和编译模式（如支持）
   - 测试错误处理和边界条件

---

## 渐进式类型推断系统

### 概述

Old8Lang 编译器现在支持 TypeScript 风格的渐进式类型推断系统，可以智能推断函数参数和返回值类型，减少编译模式下的类型注解负担。

### 功能特性

#### 1. 智能类型推断

系统可以从多个来源推断类型：

- **默认值推断**：从参数默认值推断类型
- **return 语句推断**：从函数体的 return 语句推断返回类型
- **函数调用推断**：从函数调用处的实参类型推断参数类型（计划中）
- **赋值推断**：从赋值表达式推断变量类型

#### 2. 类型约束求解

使用约束求解系统：

- **约束收集**：遍历 AST 收集类型约束
- **约束求解**：多轮迭代求解类型约束
- **类型精化**：合并多个约束选择最具体的类型
- **置信度机制**：每个约束都有置信度评分

#### 3. 渐进式类型系统

类似 TypeScript 的设计理念：

- **可选类型注解**：类型注解是可选的，推断失败时回退到 `object` 类型
- **显式优先**：显式类型注解优先级高于推断类型
- **向后兼容**：不影响现有代码的运行

### 架构设计

#### 核心组件

```
TypeInferenceEngine (引擎)
├── TypeConstraintCollector (约束收集器)
├── TypeConstraintSolver (约束求解器)
├── TypeInferenceContext (推断上下文)
└── TypeInferenceConfig (配置)
```

#### 类型约束种类

```csharp
public enum TypeConstraintKind
{
    Equality,      // 相等约束：T = SomeType
    Subtype,       // 子类型约束：T <: SomeType
    Call,          // 调用约束：从函数调用推断
    Assignment,    // 赋值约束：从赋值操作推断
    Return         // 返回约束：从return语句推断
}
```

### 使用方法

#### 1. 启用/禁用类型推断

```csharp
// 方式1：通过 Compiler 类
Compiler.EnableTypeInference = true;  // 启用（默认禁用）
Compiler.TypeInferenceDebugOutput = true;  // 启用调试输出

// 方式2：通过配置类
TypeInferenceConfig.Instance.EnableTypeInference = true;
TypeInferenceConfig.Instance.DebugOutput = true;
```

#### 2. 配置选项

```csharp
var config = TypeInferenceConfig.Instance;

// 启用/禁用类型推断
config.EnableTypeInference = true;

// 从函数调用处推断参数类型
config.InferParameterTypesFromCalls = true;

// 从return语句推断返回类型
config.InferReturnTypesFromBody = true;

// 无法推断时回退到动态类型
config.FallbackToDynamic = true;

// 置信度阈值（0.0-1.0）
config.MinimumConfidence = 0.5;

// 调试输出
config.DebugOutput = false;
```

#### 3. 编译器模式下使用

##### 无类型注解（需启用推断）

```old8
// 启用类型推断后，可省略类型注解
func add(a, b) -> int {
    return a + b
}

// 从调用处推断参数类型
result <- add(10, 20)  // 推断 a:int, b:int
```

##### 部分类型注解

```old8
// 混合使用：返回类型自动推断
func multiply(x:int, y:int) {
    return x * y  // 推断返回类型为 int
}
```

##### 默认值推断

```old8
// 从默认值推断参数类型
func greet(name:string, message: "Hello") -> void {
    // message 推断为 string 类型
    PrintLine(message + ", " + name)
}
```

### 实现细节

#### 约束收集阶段

1. **函数声明分析**
   - 收集显式类型注解
   - 收集默认值类型约束
   - 收集返回类型约束

2. **函数调用分析**
   - 从调用处实参推断形参类型
   - 记录函数调用信息

3. **赋值语句分析**
   - 从赋值右值推断左值类型

#### 约束求解阶段

1. **按置信度排序约束**
2. **多轮迭代求解**
   - 每轮处理可解决的约束
   - 传播约束信息
   - 类型精化
3. **验证解决方案**
4. **应用推断结果到 LocalManager**

#### 类型精化规则

- `object` 可被任何类型精化
- `int` 可被 `double` 精化
- 子类型优先于父类型
- 考虑类型兼容性（IsAssignableFrom）

### 限制和已知问题

#### 当前限制

1. **默认状态禁用**：类型推断默认禁用，需手动启用
2. **基础实现**：当前为第一版实现，功能有限
3. **调用处推断未完善**：从函数调用处推断参数类型的功能尚未完全实现

#### 未来改进

- [ ] 完善从调用处推断参数类型
- [ ] 支持泛型类型推断
- [ ] 支持联合类型 (Union Types)
- [ ] 改进多分支控制流的类型推断
- [ ] 添加类型缩窄 (Type Narrowing)

### 示例代码

#### 示例 1：简单类型推断

```old8
// 启用类型推断
// TypeInferenceConfig.Instance.EnableTypeInference = true;

func add(a, b) -> int {
    return a + b
}

result <- add(10, 20)
PrintLine("10 + 20 = " + result.ToStr())
```

#### 示例 2：多类型推断

```old8
// 从默认值推断
func calculate(x, y:int, operation:"add") {
    if operation == "add" {
        return x + y
    } elif operation == "multiply" {
        return x * y
    } else {
        return 0
    }
}

result1 <- calculate(10, 20)            // operation = "add"
result2 <- calculate(10, 20, "multiply")
```

#### 示例 3：混合注解

```old8
// 部分显式注解 + 部分推断
func max(a:int, b:int) {
    // 返回类型从 return 语句推断为 int
    if a > b {
        return a
    } else {
        return b
    }
}

result <- max(10, 20)
PrintLine("Max: " + result.ToStr())
```

### API 参考

#### TypeInferenceConfig

类型推断配置类（单例）。

**属性**：
- `EnableTypeInference: bool` - 启用类型推断
- `InferParameterTypesFromCalls: bool` - 从调用处推断参数
- `InferReturnTypesFromBody: bool` - 从函数体推断返回类型
- `FallbackToDynamic: bool` - 回退到动态类型
- `MinimumConfidence: double` - 最小置信度阈值
- `DebugOutput: bool` - 调试输出

#### TypeInferenceEngine

类型推断引擎。

**方法**：
- `InferTypes(IOldLangTree program): bool` - 对整个程序推断类型
- `InferFunctionTypes(FuncInit funcInit): bool` - 对单个函数推断类型
- `NeedsTypeInference(FuncInit funcInit): bool` - 检查是否需要推断
- `GetInferredType(string variableName): Type?` - 获取推断类型
- `GetStatistics(): (int, int, int)` - 获取统计信息

#### Compiler

编译器类的新增属性。

**属性**：
- `EnableTypeInference: bool` - 启用/禁用类型推断
- `TypeInferenceDebugOutput: bool` - 类型推断调试输出
- `TypeInferenceConfig: TypeInferenceConfig` - 获取配置实例

### 调试和诊断

#### 启用调试输出

```csharp
Compiler.TypeInferenceDebugOutput = true;
TypeInferenceConfig.Instance.DebugOutput = true;
```

#### 调试信息示例

```
=== 开始渐进式类型推断 ===
注册函数: add
分析函数调用: add

=== 类型推断迭代 1 ===
  ✓ 绑定类型: add$param$0$a = Int32 (置信度: 0.80)
  ✓ 绑定类型: add$param$1$b = Int32 (置信度: 0.80)
  ✓ 绑定类型: add$return = Int32 (置信度: 0.90)

=== 类型推断结果 ===
总约束数: 3
已解决类型变量数: 3

类型绑定:
  add$param$0$a = Int32
  add$param$1$b = Int32
  add$return = Int32

✓ 函数 add 类型推断成功
```

### 性能考虑

- 类型推断在编译阶段执行，不影响运行时性能
- 约束求解使用多轮迭代，最多 10 轮
- 可以通过禁用推断功能来跳过推断开销（默认禁用）

### 贡献指南

欢迎贡献改进类型推断系统：

1. 改进约束收集算法
2. 优化约束求解性能
3. 添加更多类型推断场景
4. 改进错误提示信息
5. 添加测试用例

---

## 包开发和发布指南

### 目录

1. [包结构](#包结构-1)
2. [创建包](#创建包-1)
3. [package.json 配置](#packagejson-配置-1)
4. [包依赖管理](#包依赖管理)
5. [测试包](#测试包-1)
6. [发布包](#发布包-1)
7. [最佳实践](#最佳实践-1)

### 包结构

一个标准的 Old8Lang 包应该包含以下结构：

```
MyPackage/
├── o8package.json         # 包元数据（必需）
├── index.old8           # 主入口文件（或在 package.json 中指定）
├── README.md            # 包文档
├── LICENSE              # 许可证（默认为 MIT）
├── src/                 # 源代码目录（可选）
│   ├── utils.old8
│   └── helper.old8
├── test/                # 测试目录（可选）
└── dll/                 # 预编译文件目录/需要的 C# 原生dll文件（可选）
```

### 创建包

#### 步骤 1：初始化包结构

```bash
mkdir MyPackage
cd MyPackage
old8lang package init
```

这会创建一个基本的 `package.json` 文件。

#### 步骤 2：编写主入口文件

创建 `index.old8`：

```old8
// MyPackage/index.old8

// 导出函数
func greet(name:string) -> string {
    return "Hello, " + name + "!"
}

// 导出常量
version:const <- "1.0.0"

// 导出类
class Helper {
    public func calculate(a:int, b:int) -> int {
        return a + b
    }
}

// 包初始化代码（可选）
PrintLine("MyPackage loaded successfully") //不会显示
```

### package.json 配置

#### 基本配置

```json
{
  "id": "MyPackage",
  "version": "1.0.0",
  "description": "一个示例 Old8Lang 包",
  "author": "Your Name",
  "license": "MIT",
  "main": "index.old8",
  "keywords": ["utility", "helper", "example"],
  "repository": {
    "type": "git",
    "url": "https://github.com/username/MyPackage"
  },
  "dependencies": []
}
```

#### 完整配置选项

```json
{
  "id": "MyPackage",                    // 包名（必需，唯一标识符）
  "version": "1.0.0",                   // 版本号（必需，遵循 SemVer）
  "description": "包描述",               // 简短描述
  "author": "作者名",                   // 作者信息
  "license": "MIT",                     // 许可证
  "main": "index.old8",                 // 主入口文件
  "keywords": ["关键词1", "关键词2"],    // 搜索关键词
  "homepage": "https://...",            // 项目主页
  "repository": {                       // 代码仓库
    "type": "git",
    "url": "https://github.com/..."
  },
  "dependencies": [                     // 依赖包列表
    {
      "id": "Logger",
      "version": "^1.2.0",
      "optional": false
    }
  ],
  "devDependencies": [                  // 开发依赖
    {
      "id": "TestFramework",
      "version": "^2.0.0"
    }
  ],
  "framework": "old8lang-1.0",          // 目标框架
  "engines": {                          // 运行环境要求
    "old8lang": ">=1.0.0"
  }
}
```

### 包依赖管理

#### 添加依赖

在 `package.json` 中添加依赖：

```json
{
  "dependencies": [
    {
      "id": "Logger",
      "version": "^1.2.0"
    },
    {
      "id": "HttpClient",
      "version": "~2.0.0"
    }
  ]
}
```

#### 版本范围说明

- `1.2.3` - 精确版本
- `^1.2.3` - 兼容版本（允许次版本和补丁版本更新）
- `~1.2.3` - 补丁版本（只允许补丁版本更新）
- `>=1.2.3` - 大于等于指定版本
- `*` - 任意版本（不推荐）

#### 在代码中使用依赖

```old8
// 导入依赖包
import "Logger"

// 使用包中的功能
Logger.info("这是一条日志消息")
```

### 测试包

#### 1. 本地测试

在开发过程中进行本地测试：

```bash
# 在包目录中
cd MyPackage

# 测试主入口文件
old8lang -f index.old8

# 测试特定功能
old8lang -f test/test_greet.old8
```

#### 2. 在其他项目中测试

```bash
# 在另一个项目中
old8lang init                    # 初始化项目
old8lang add ../MyPackage@1.0.0  # 添加本地包
old8lang restore                 # 恢复依赖
```

创建测试文件：

```old8
// test_usage.old8
import "MyPackage"

result <- MyPackage.greet("World")
PrintLine(result)  // 输出: Hello, World!
```

运行测试：

```bash
old8lang -f test_usage.old8
```

### 发布包

#### 准备发布

1. **更新版本号**（在 `package.json` 中）
2. **更新 README.md** - 确保文档完整
3. **检查依赖** - 确保所有依赖都已声明
4. **运行测试** - 确保所有测试通过

#### 发布到本地仓库

```bash
# 打包
cd MyPackage
old8lang package pack

# 发布到本地仓库
old8lang package publish --local

# 或发布到全局
old8lang package publish --global
```

#### 发布到远程仓库（未来功能）

```bash
# 登录包管理器
old8lang package login

# 发布包
old8lang package publish
```

### 最佳实践

#### 1. 命名规范

- **包名**：使用 PascalCase（如 `MyPackage`）
- **函数名**：使用 camelCase（如 `greet`, `calculateTotal`）
- **类名**：使用 PascalCase（如 `Helper`, `DataProcessor`）
- **常量**：使用 camelCase 或 UPPER_CASE

#### 2. 导出规范

明确标记公共 API：

```old8
// ✅ 好的做法
public func publicFunction() -> void {
    privateHelper()
}

func privateHelper() -> void {
    // 内部使用，不导出
}

// ❌ 不好的做法 - 所有内容都默认导出
func shouldBePrivate() -> void {
    // 这会被导出，可能造成命名冲突
}
```

#### 3. 依赖管理

- 尽量减少依赖数量
- 使用精确的版本范围
- 避免循环依赖

#### 4. 文档规范

在 README.md 中包含：

- 安装说明
- 使用示例
- API 文档
- 贡献指南
- 许可证信息

#### 5. 版本控制

遵循 [语义化版本](https://semver.org/lang/zh-CN/)：

- **主版本号**（Major）：不兼容的 API 变更
- **次版本号**（Minor）：向后兼容的功能添加
- **修订号**（Patch）：向后兼容的问题修正

---

## 总结

Old8Lang 提供了丰富的高级特性：

- **Extern 工厂架构**: 高可扩展的外部语言支持
- **渐进式类型推断**: TypeScript 风格的类型推断
- **包管理系统**: 完整的包开发和发布工具

通过掌握这些高级特性，开发者可以充分发挥 Old8Lang 的潜力。
