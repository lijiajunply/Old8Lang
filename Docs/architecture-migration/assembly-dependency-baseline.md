# 阶段 2 程序集依赖固定基线

日期：2026-11-14  
依据：[spec.md](spec.md)、[design.md](design.md)

本文记录阶段 2 开始前由 `*.csproj` 得到的编译期引用图。它是拆分前的事实基线，不是目标架构已经完成的声明。

## 当前引用图

```text
Old8Lang.App / LanguageServer / Benchmarks / Tests
                     ↓
          Hosting / Modules.Official
                     ↓
              Old8Lang.Runtime
        ↙       ↓       ↓       ↘
 Core  Concurrency  Modules.Abstractions  Observability / ProjectSystem
```

`Old8Lang.Runtime` 目前直接引用 `Core`、`Concurrency`、`Modules.Abstractions`、`Observability` 和 `ProjectSystem`；五个共享项目均不反向引用 Runtime。官方模块位于 `Modules.Official`，仅由组合根/宿主引用 Runtime 的方向组合，Runtime 不引用官方模块。

## 阶段 1 残留的执行代码

基线时 31 个 `Execution/Nodes/*.Split.cs` 已归档到
`Old8Lang.Runtime/Execution/LegacyNodeExtensions/`，按控制流、声明、表达式和运行时值分类。
迁移 `LangId`、`AwaitExpression` 与 `Operation` 时，三个 partial 已改为独立的 Runtime 辅助类，剩余 28 个仍通过 partial 类型进入 Runtime，因此**不能**被编入未来的语法程序集；它们是后续按后端迁出的清单，而不是可复制到 Syntax 的源码。

## 后续允许的方向

本阶段已引入只依赖 `Core` 的 `Old8Lang.Syntax`，由 Runtime 依赖它。首批内容是标量
`LiteralSyntax`、`LiteralKind` 与 `ILiteralSyntax`；Syntax 不得引用 Runtime、Hosting、官方模块或任何执行后端。共享运行时契约将在实际循环被定位后从 Runtime 抽出；在此之前不得以 Runtime 的反向引用伪造独立程序集。

`ArchitectureProjectDependencyTests` 将当前 Runtime 共享层引用、官方模块方向以及 partial 归档位置固定为可执行门禁。


## 阶段 2 完成时的实际依赖（2026-11-14）

```text
Old8Lang.App / LanguageServer / Benchmarks / Tests
                     ↓
         Hosting / Modules.Official
                     ↓
              Old8Lang.Runtime
        ↙      ↓      ↓      ↘
  Syntax  Concurrency  Modules.Abstractions  Observability / ProjectSystem
     ↓
   Core
```

`Old8Lang.Syntax` 现在同时包含语法节点与 `LangParser`，只引用 `Old8Lang.Core`；`ScopeLayer`
等解释器作用域类型已移回 `Old8Lang.Runtime`。`SyntaxParserBoundaryTests` 固定三条门禁：
解析器类型位于 Syntax、Syntax 仅引用 Core、Syntax 源码不出现执行后端类型。

## 阶段 3.1：解释后端程序集（2026-11-14）

```text
Old8Lang.App / LanguageServer / Benchmarks / Tests
                     ↓
                   Hosting
          ↙                     ↘
Old8Lang.Interpreter      Old8Lang.Runtime
          ↘                     ↙
   Core / Concurrency / Modules.Abstractions / Observability / ProjectSystem / Syntax
```

`Old8Lang.Interpreter` 只引用 `Old8Lang.Runtime`，不再引用 IL、VM 后端或官方模块；
Runtime 通过 `Old8Lang.AST.Visitor.RuntimeVisitors` 契约接收解释执行访问器，不再在编译期
引用 `InterpreterVisitor`。`BackendAssemblyBoundaryTests` 固定了这两条门禁以及「Runtime 源码
不得直接构造解释访问器」。Hosting 作为组合根显式调用 `InterpreterBackend.Register()`。

`LangInterpreter`、`VariateManager`、`ScopeLayer` 仍位于 Runtime：它们是运行时值与实例方法
共同依赖的解释期状态，尚未完成接口化。IL 与 VM 后端仍在 Runtime 中。

## 阶段 3.2：IL 后端程序集（2026-11-14）

```text
Old8Lang.App / LanguageServer / Benchmarks / Tests
                     ↓
                   Hosting
        ↙            ↓             ↘
Old8Lang.Interpreter  Old8Lang.Compiler  Old8Lang.Runtime
        ↘             ↓             ↙
   Core / Concurrency / Modules.Abstractions / Observability / ProjectSystem / Syntax
```

`Old8Lang.Compiler` 只引用 `Old8Lang.Runtime`，不引用解释、VM 后端或官方模块；Runtime 通过
`RuntimeVisitors.CreateCompiler` 契约接收 IL 访问器，不再直接构造 `CompilerVisitor`。
Hosting 组合根同时注册解释与 IL 后端。`BackendAssemblyBoundaryTests` 固化三个后端的
「只依赖 Runtime」与「Runtime 不直接构造访问器」门禁。

`Compiler/` 引擎（`Compiler.Compile`、`LocalManager`、`RuntimeGlobals`、异步状态机、
特化/验证辅助）仍在 Runtime：`LocalManager` 是 `IInstanceMethod` 与类型推断共用的编译契约，
运行时值也保留 IL 辅助方法。完整引擎迁移需要先抽象这些契约。VM 后端仍在 Runtime 中。

## 阶段 3.3：三个后端访问器独立（2026-11-14）

```text
Old8Lang.App / LanguageServer / Benchmarks / Tests
                     ↓
                   Hosting
        ↙            ↓             ↘
Old8Lang.Interpreter  Old8Lang.Compiler  Old8Lang.VirtualMachine
        ↘             ↓             ↙
                 Old8Lang.Runtime
        ↘             ↓             ↙
   Core / Concurrency / Modules.Abstractions / Observability / ProjectSystem / Syntax
```

三个后端程序集都只引用 `Old8Lang.Runtime`，互不引用；Runtime 通过
`RuntimeVisitors` 的解释/IL/字节码工厂契约创建访问器，源码中不再直接构造
`InterpreterVisitor`、`CompilerVisitor`、`BytecodeVisitor`。`Old8Lang.VirtualMachine`
的注册类位于 `Old8Lang.BytecodeBackend` 命名空间，避免新增 `Old8Lang.VirtualMachine`
命名空间后与 `Old8Lang.Bytecode.VM.VirtualMachine` 类型名冲突。

仍留在 Runtime 的执行状态：解释期的 `LangInterpreter`/`VariateManager`/`ScopeLayer`，
IL 期的 `Compiler/`（`Compiler.Compile`、`LocalManager`、`RuntimeGlobals`、异步状态机），
VM 期的 `Bytecode/`（`BytecodeCompiler`、`VirtualMachine`、指令/元数据）。

## 阶段 3 验收证据（2026-11-14）

- 三个后端项目分别构建 0 错误，互不引用，只引用 Runtime。
- `BackendAssemblyBoundaryTests` 校验程序集引用、契约注册与 Runtime 源码边界。
- 组合根 `Old8HostBuilder` 显式注册三个后端；后端程序集不再使用 `ModuleInitializer`。
- `BytecodePersistenceTests` 验证字节码文件写入、读取与执行往返（VM 1083 通过）。
- 回归：Architecture 55、Interpreter 1959/1960（1 跳过，排除外部 HTTP 用例）、VM 1083；
  Compiler.Expressions 41 项既有失败与 HEAD 一致。

引擎余量（`LangInterpreter`/`VariateManager`、`Compiler/`、`Bytecode/`）仍在 Runtime，
是下一子阶段的拆分对象。
