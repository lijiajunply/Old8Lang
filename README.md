# Old8Lang

Old8Lang 是一门基于 .NET 10 实现的现代多模式编程语言，集动态脚本的灵活性、IL 编译的高性能与自定义虚拟机的可分发、可调试能力于一体。

> 为西安建筑科技大学（“老八校”之一）而生的编程语言项目。

## 核心特性

- **三种执行模式**
  - **解释模式（`-f`）**：直接执行 AST，启动最快，动态特性最完整（支持运算符重载、Python/JS/C# 互操作等），适合日常脚本与快速原型。
  - **IL 模式（`-il` / `-c`）**：编译为 .NET IL 执行，运行性能最高，提供静态类型检查与异步状态机非阻塞挂起，适合性能敏感场景。
  - **虚拟机模式（`-vm`）**：编译为自定义字节码由 VM 执行，支持字节码序列化（`-compile` / `-execute`）、跨平台分发与标准 DAP 调试。
- **现代语言能力**：函数、闭包与写回、高阶函数、类/继承/接口/Mixin、枚举、泛型函数与泛型类（含 `where` 约束）、可空/联合/交叉类型、函数装饰器、扩展方法、模式匹配（`match`）、LINQ 查询、列表推导式、步长与字符范围（`[1~10:2]`、`['A'~'Z']`）。
- **异步与并发**：`async` / `await`、异步生成器（`yield`）与 `async for-in`、`Task` API、`spawn` 线程、`Channel`、`select`、`Mutex`、`Semaphore`、`AtomicInt`、`ReadWriteLock`、`CountDownLatch`、`CyclicBarrier` 等并发原语。
- **内置标准库与扩展库**：`Math`、`OS`、`File`、`Terminal`、`Time`、`String`、`Collection`、`Crypto`、`Csv`、`Json`、`Regex`、`Vector`、`Net`、`Database`、`MachineLearning` 等模块。
- **包管理与项目系统**：基于 `o8package.json` 的项目级隔离与全局包模式，统一对接 `Old8Lang.PackageManager.Server`（支持 `init` / `install` / `restore` / `remove` / `list` / `search` / `registry` / `pack` / `sign` / `verify` / `publish`）。
- **开发工具链**：内置 Language Server（`Old8Lang.LanguageServer`）、虚拟机 DAP 调试器（`Old8Lang.App --dap`）与 VS Code 扩展（`vscode-old8lang/`）。

## 环境要求

- [.NET 10.0 SDK](https://dotnet.microsoft.com/)

## 快速开始

### 构建与运行

```bash
# 构建解决方案
dotnet build Old8Lang.sln

# 查看版本与帮助
dotnet run --project Old8Lang.App -- --version
dotnet run --project Old8Lang.App -- --help

# 以三种模式运行脚本
dotnet run --project Old8Lang.App -- -f  example.old8   # 解释模式
dotnet run --project Old8Lang.App -- -il example.old8   # IL 模式
dotnet run --project Old8Lang.App -- -vm example.old8   # 虚拟机模式
dotnet run --project Old8Lang.App -- -s  example.old8   # 语法检查

# 编译并执行字节码
dotnet run --project Old8Lang.App -- -compile example.old8 example.o8c
dotnet run --project Old8Lang.App -- -execute example.o8c
```

### 语法示例

```old8
import "Math"

func fib(n: int) -> int {
    if n <= 1 {
        return n
    }
    return fib(n - 1) + fib(n - 2)
}

nums: list<int> <- {1, 2, 3, 4, 5}
squares <- nums.Map((x: int) -> x * x)
PrintLine($"fib(10) = {fib(10)}, squares = {squares}")
PrintLine($"sqrt(16) = {Math.Sqrt(16)}")
```

### 包管理

```bash
# 初始化项目
old8lang init myproject
cd myproject

# 配置包仓库并搜索、安装、发布
old8lang registry set https://your-registry.example.com
old8lang search math
old8lang install demo_math
old8lang list
old8lang publish
```

## 文档导航

- [完整文档索引](./Docs/README.md)
- [语法参考](./Docs/Old8Lang_Grammar.md) · [EBNF 规范](./Docs/Old8Lang.ebnf)
- [三种模式支持矩阵与已知限制](./Docs/MODE_SUPPORT.md)
- [标准库与内置 API 参考](./Docs/API_REFERENCE.md)
- [命令行与包管理指南](./Docs/CLI_GUIDE.md)
- [架构设计](./Docs/ARCHITECTURE.md)
- [开发者工具（LSP / DAP / VS Code）](./Docs/DEVELOPER_TOOLS.md)
- [更新记录（CHANGELOG）](./Docs/CHANGELOG.md)

## 1.0.0 已知边界

- **IL 模式**：函数参数与返回值需显式类型注解；`int` 映射 CLR 32 位 `unchecked Int32`；不支持用户定义运算符重载与同步生成器（`yield`）。
- **外部互操作（`extern`）**：Python / JavaScript / C# DLL 互操作仅在解释模式完整支持；IL 与虚拟机模式支持原生 C 库（P/Invoke）。
- **工具链**：字节码反汇编器与 VM Profiler 暂未开放命令行入口；详细支持矩阵见 [Docs/MODE_SUPPORT.md](./Docs/MODE_SUPPORT.md)。

## 开发人员与许可

- 开发人员：LuckyFish
- 许可证：[MIT License](./LICENSE)

本项目归 LuckyFish 和西安建筑科技大学 iOS 众创空间俱乐部所有。
