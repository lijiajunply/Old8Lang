# Old8Lang 开发路线图

**最后更新**: 2026-10-09

本文档只保留**方向性的当前重点**。逐条待办不写在这里——每个领域都有自己的待办文件，
那些文件在动手时会同步更新。历史上这里抄过一份完整计划，九个月后全部失效
（旧版的「IL 模式 75% 完成」「37 个异步测试失败」「P0：修复 IL 代码生成」等结论与现状均不符），
因此不再重复维护。

## 当前重点

### 1. IL 模式的既有 IL 校验失败（最高优先）

IL 模式在一批用例上抛 `Common Language Runtime detected an invalid program`（创建委托失败），
落点集中在类型、集合、异步等评测点上。

**2026-10-09 实测**：`dotnet test Old8Lang.Tests --filter "FullyQualifiedName~Compiler"`
→ **737 失败 / 446 通过 / 25 跳过，共 1208 例**。

这不是某一个特性的问题，需要单独定位生成的 IL 在哪几类形态上不符合 CLR 校验规则。
基础形态本身正常（普通带类型注解的函数在 `-il` 下编译执行通过），问题出在更复杂的代码上。

- 影响面与复核边界：[MODE_SUPPORT.md](./MODE_SUPPORT.md) 的「复核说明」
- 排查手段：ILSpy 反编译对照，见 [DEVELOPMENT_WORKFLOW.md §7](./DEVELOPMENT_WORKFLOW.md#7-排查-il-模式的代码生成问题)
- 覆盖缺口清单：[Old8Lang.Tests/Compiler/TODO.md](../Old8Lang.Tests/Compiler/TODO.md)

### 2. 虚拟机模式待补能力

- 泛型类（泛型函数已可用，但必须显式给出类型参数）
- 直接 `print` 集合会输出 .NET 类型名（改用 `.ToStr()` 正常）
- 静态类：TestRunner / Mock / TaskScheduler / TaskCompletionSource / CancellationTokenSource；
  `Task.Factory`，以及 Task 实例方法 `Then` / `Catch` / `Finally` / `ContinueWith`
- 原生函数无法区分「无返回值」与 `null`（`print(Sleep(1))` 在虚拟机输出 `null`，解释器不输出）

详见 [MODE_SUPPORT.md](./MODE_SUPPORT.md) 的「已知限制 · 虚拟机模式」。

### 3. 文档已描述、但实现尚不可用的功能

下面两项在语法文档里都有完整章节，实际却跑不起来。文档侧已加「复核说明」标明，
但**功能本身仍是缺口**。

#### 泛型扩展方法与泛型约束：运行时语义未实现

解析器已接受 `extension list<T> { ... }`、带 `new()`/`class`/`struct` 约束的形式，以及
`where` 子句和多类型参数声明。当前 AST 仍只保存基础目标类型名，类型参数不会参与扩展方法
注册，约束也不会在运行时校验；方法体中依赖 `T`/`K`/`V` 的泛型绑定仍未实现。

**非泛型**扩展是正常的（`extension list` / `extension int` / `extension string` 均可用），
`Old8Lang.Tests` 里也只有非泛型用例。

- 解析实现位置：`Old8Lang/LangParser/Parsers/ExtensionParser.cs`——`ParseExtensionDeclaration`
  消费类型参数与约束语法，但保留基础目标类型名
- 文档：`Old8Lang_Grammar.md` 的「泛型扩展方法」一节已标为语法草案

#### Python 互操作：进程无法正常退出（已修复）

`extern "pymodule:..." { ... }` 声明之后，程序逻辑与输出都正常，
此前进程会卡在退出阶段，现已在 CLI 收尾时释放 Python.NET 运行时并正常退出：

```bash
$ Old8Lang.App -f TestFiles/InterpreterTests/test_python_simple.old8
Result: 30
Done!
```

**2026-10-09 修复验证**：`test_python_simple.old8` 输出 `Result: 30`、`Done!` 后以退出码 `0` 返回，
未再出现收尾挂起。

- 环境：pythonnet 3.0.5 + Python 3.13.9（pythonnet 3.0.x 官方支持到 Python 3.12）
- 实现位置：`Old8Lang/ExternProviders/PythonProvider.cs` 与 CLI 主流程；csproj 已引用 `pythonnet` 3.0.5
- 兼容说明：pythonnet 3.0.5 在 .NET 10 的 `PythonEngine.Shutdown()` 会触发已移除的
  `BinaryFormatter`；收尾逻辑捕获该兼容性异常并调用 pythonnet 的原生 Python finalizer。

### 4. 跨模式语义未对齐（已确认的分歧）

- **脚本顶层的 C 风格 for 循环变量**：`for i <- 0, i < 3, i++` 在顶层被声明为全局变量，
  循环体内创建的闭包读到最终值（虚拟机得 `333`，解释器得 `012`）。函数内的同种循环已对齐。
- **解释器侧 lambda 写回**：lambda 写回外层变量不生效，等价的具名嵌套函数却生效。

### 5. VM 性能优化

闭包调用路径、高异常率 `try/catch` 路径、基准基线回归门禁。
详见 [Todo.md](../Todo.md)。

## 各领域待办入口

| 领域 | 待办文件 |
|------|----------|
| VM 性能优化 | [Todo.md](../Todo.md) |
| IL 模式测试覆盖 | [Old8Lang.Tests/Compiler/TODO.md](../Old8Lang.Tests/Compiler/TODO.md) |
| 解释模式测试覆盖 | [Old8Lang.Tests/Interpreter/TODO.md](../Old8Lang.Tests/Interpreter/TODO.md) |
| 语言服务器 / LSP | [Old8Lang.LanguageServer/TODO.md](../Old8Lang.LanguageServer/TODO.md) |
| 模式支持矩阵 | [MODE_SUPPORT.md](./MODE_SUPPORT.md) |
| 已完成与已知限制 | [CHANGELOG.md](./CHANGELOG.md) |
