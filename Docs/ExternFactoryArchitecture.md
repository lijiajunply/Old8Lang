# Extern 功能工厂架构设计文档

## 概述

Extern 功能已重构为基于工厂模式的可扩展架构，方便未来添加更多语言支持（如 JavaScript、Ruby、Lua 等）。

## 架构设计

### 核心组件

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

### 文件结构

```
Old8Lang/
├── ExternProviders/                    # 新增：Extern 提供者目录
│   ├── IExternProvider.cs              # 提供者接口
│   ├── ExternProviderFactory.cs        # 工厂类
│   ├── NativeDllProvider.cs            # C/C++ P/Invoke 提供者
│   └── PythonProvider.cs               # Python 提供者
└── AST/
    └── Statement/
        └── ExternStatement.cs          # 重构后：使用工厂模式
```

## 接口定义

### IExternProvider

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
        LocalManager localManager);

    /// <summary>
    /// 是否支持编译模式
    /// </summary>
    bool SupportsCompilation { get; }
}
```

## 已实现的提供者

### 1. NativeDllProvider（C/C++ P/Invoke）

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

### 2. PythonProvider

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

## 工厂使用方式

### 创建提供者

```csharp
// 自动根据 ExternType 创建提供者
var provider = ExternProviderFactory.CreateProvider(ExternType.NativeDll);
var provider = ExternProviderFactory.CreateProvider(ExternType.PythonScript);
```

### 注册自定义提供者

```csharp
// 未来添加新语言支持示例
ExternProviderFactory.RegisterProvider(
    ExternType.JavaScript,
    () => new JavaScriptProvider()
);
```

### 检查支持

```csharp
// 检查是否支持某种类型
bool isSupported = ExternProviderFactory.IsSupported(ExternType.PythonModule);

// 获取所有已注册类型
var types = ExternProviderFactory.GetSupportedTypes();
```

## ExternStatement 重构

### 重构前

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

### 重构后

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

## 优势

### 1. 可扩展性
- **添加新语言**只需：
  1. 创建新的 `IExternProvider` 实现类
  2. 在工厂中注册（或运行时注册）
  3. 无需修改 `ExternStatement` 核心代码

### 2. 解耦
- 每种语言的具体实现完全独立
- 核心逻辑（`ExternStatement`）与具体实现分离
- 便于单元测试和维护

### 3. 统一接口
- 所有语言提供者遵循相同的接口契约
- 编译模式支持通过 `SupportsCompilation` 属性明确声明
- 错误处理统一规范

### 4. 类型安全
- 工厂方法返回具体的 `IExternProvider` 接口
- 编译时类型检查
- 运行时类型转换由各提供者内部处理

## 未来扩展示例

### 添加 JavaScript 支持

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

### 添加 Ruby 支持

类似方式，创建 `RubyProvider` 并注册即可。

## 测试

### 构建测试

```bash
dotnet build Old8Lang.sln
```

### 语法测试

```bash
dotnet run --project Old8Lang.App -- -s InterpreterTests/test_python_extern_basic.old8
```

### 功能测试

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

## 兼容性

### 向后兼容
- 重构完全向后兼容
- 所有现有的 extern 语句无需修改
- 语法和行为保持不变

### 编译模式支持

| Provider | 解释模式 (-f) | 编译模式 (-c) |
|----------|--------------|--------------|
| NativeDllProvider | ✅ 支持 | ✅ 支持 |
| PythonProvider | ✅ 支持 | ❌ 不支持 |

## 技术细节

### 依赖注入
- 工厂使用简单的字典注册表
- 支持运行时动态注册新提供者
- 提供者实例由工厂方法创建

### 错误处理
- 不支持的 ExternType：抛出 `NotSupportedException`
- 编译模式检查：在 `GenerateIl` 中验证 `SupportsCompilation`
- 具体错误由各提供者内部处理

### 性能影响
- 工厂创建开销：极小（字典查找 + 委托调用）
- 运行时性能：与重构前相同
- P/Invoke 性能：无变化（直接调用）
- Python 性能：无变化（Python.NET 开销）

## 维护建议

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

## 总结

Extern 功能的工厂架构重构成功实现了：

✅ **高可扩展性**：轻松添加新语言支持
✅ **低耦合度**：各语言实现完全独立
✅ **统一接口**：遵循 SOLID 原则
✅ **向后兼容**：不破坏现有代码
✅ **类型安全**：编译时和运行时检查

这为 Old8Lang 未来支持更多外部语言（JavaScript、Ruby、Lua、Go 等）奠定了坚实的基础。
