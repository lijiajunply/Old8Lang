# 宿主模块目录审计（阶段 4.1）

日期：2026-11-14  
范围：[plan.md](plan.md) 阶段 4 第 1 项，验收依据 [spec.md](spec.md) 第 3 条。

## 结论

进程级默认目录 `StandardLibraryRegistry` 只保留为**兼容边界**：由 CLI、语言服务器、
基准与测试的组合根通过 `Old8Host.InstallAsDefault()` 显式安装，供它们的兼容构造器使用。
宿主创建的解释器、字节码编译器和虚拟机只使用宿主自己的 `StandardLibraryCatalog`；
由执行上下文克隆出的作用域、生成器与闭包捕获沿用该目录。没有宿主上下文的独立辅助作用域
使用冻结空目录（失败关闭），不会回退到进程默认目录。

`StandardLibraryCatalog.Empty` 是共享的冻结空目录。没有宿主上下文的临时管理器使用它，
既避免了跨宿主泄漏，也不会被意外注册污染（注册会抛 `InvalidOperationException`）。

## 入口清单

| 入口 | 原行为 | 现行为 |
|---|---|---|
| `Old8HostBuilder` / `Old8Host` | 每个宿主私有目录；`InstallAsDefault` 安装为进程默认 | 不变；`InstallAsDefault` 明确标注为兼容安装 |
| CLI `Program.Main`、LSP `Program.Main`、基准与测试的 `ModuleInitializer` | 组合根显式注册官方模块并安装默认目录 | 不变（CLI/LSP/基准/测试需要兼容构造器） |
| `LangInterpreter()`、`LangInterpreter(IPerformanceMonitor?)` | 使用进程默认目录 | 不变，文档标注为兼容构造器；宿主路径用 `Old8Host.CreateInterpreter` |
| `VariateManager.StandardLibraries` | 默认进程默认目录 | 默认冻结空目录；`Clone`/`NewManger`/`CaptureForClosure`/`CloneForGenerator`/`CapturedScope` 显式继承宿主目录；类实例的 `InstanceScope` 在字段初始化时继承宿主目录 |
| 临时管理器池 `GetTemp`/`ReturnTemp` | 归还时保留原目录 | `Clear()` 额外重置为空目录（当前仓库内无调用点，属防御性修正） |
| `ModuleResolver()`、`ModuleResolutionResult` 名称建议 | 使用进程默认目录 | 使用传入目录，缺省为空目录；建议列表只基于当前目录 |
| `StandardLibraryLoader.ValidateStandardLibrary` | 缺省进程默认目录 | 缺省空目录 |
| `CompilerVisitor.VisitImportStatement` | 解释器缺失时回退进程默认目录 | 解释器缺失时使用空目录（`Compiler.Compile` 始终要求解释器） |
| `DllPathResolver.ResolveDllPath` / `GetSearchPaths` | 直接读取进程默认目录 | 新增带目录参数的重载；解释器、编译器与 VM 的调用点传入各自目录；旧签名保留为兼容重载 |
| `ModuleLoader` | 缺省进程默认目录 | 缺省空目录；VM 始终传入自己的目录 |
| `BytecodeCompiler.StandardLibraries` | 属性默认进程默认目录 | 属性默认空目录；无参构造器显式使用进程默认目录（兼容路径，文档标注） |
| `VirtualMachine(...)` 无目录参数 | 进程默认目录 | 不变，文档标注为兼容路径；`Old8Host.CreateVirtualMachine` 传入宿主目录 |
| LSP `DocumentLinkHandler` 标准库源码链接 | 读取进程默认目录 | 不变；源码链接是进程级 LSP 能力，组合根启动时已安装默认目录，代码中注明来源 |
| `OfficialModules.RegisterAll()` | 向进程默认目录注册 | 不变，文档标注为兼容入口；宿主用 `RegisterAll(IStandardLibraryRegistry)` |

## 覆盖测试

`Old8Lang.Tests/Architecture/HostModuleIsolationTests.cs`（11 项）：

- 解释器、IL 编译路径、VM 三条路径：只有注册了自定义模块的宿主能导入；
  空宿主在三条路径上分别抛 `ImportError`（VM 断言到 `ImportError` 与“加载模块”消息）。
- 两个各自注册了不同模块的宿主互不可见（执行级用例）。
- 宿主自定义模块不进入其他宿主，也不进入进程默认目录。
- 宿主创建的实例不引用进程默认目录；兼容构造器仍使用进程默认目录。
- 无宿主上下文的管理器使用冻结空目录；空目录拒绝注册（模块与描述符两个入口）。
- `Clone`、`NewManger`、`CloneForGenerator` 与闭包捕获往返都保留宿主目录。
- 函数体与生成器体内的 `import` 使用宿主目录（实际执行旧8代码）。
- `ModuleResolver` 与 `StandardLibraryLoader.ValidateStandardLibrary` 的缺省目录为空目录。

`ModuleCompositionTests` 原有的“两个宿主的目录隔离”“宿主解释器/VM 工具使用自己的目录”用例继续通过。

## 已知边界

以下位置没有宿主上下文，因此使用空目录（失败关闭），不会继承某个执行实例的目录：

- `TaskLangValue.Run` 的临时管理器（函数值通常自带 `CapturedScope`，实际执行走捕获作用域）；
- `Assert`/`TestRunner` 运行回调时传入的裸管理器（回调本身通常是带捕获作用域的闭包）；
- VM `ExtensionMethodWrapper.ExecuteInVM` 的临时管理器；
- 各实例方法辅助类中用于运行回调的临时管理器。

这些路径不执行模块目录查询；若将来要在其中支持 `import` 或原生库定位，应按调用点传入
外层宿主目录，而不是让它们读取进程默认目录。

## 非目标

本次不删除 `StandardLibraryRegistry`：CLI、LSP 与大量既有测试仍通过无参构造器使用它。
若将来移除该兼容层，需要先把所有兼容构造器改为强制传入目录，并同步更新调用方与
[`csharp-api-migration.md`](csharp-api-migration.md)。
