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

## 阶段 3.5：编译器专用 IL 代码迁入 IL 后端（2026-11-14）

`ComparisonOpHelper`、`LogicalOpILHelper`、`InOperatorHelper`、
`NullishCoalescingILHelper`、`NumericBinaryOpHelper`、`TypeCheckILHelper` 与 LINQ 的
IL 生成（`LinqExpressionRuntimeExtensions`）已从 Runtime 迁入 `Old8Lang.Compiler`。
Runtime 只保留同时被类型推断使用的 `CanGenerateStreamingQuery` 判定，以及
`DotOperatorILHelper`（其中 `InferInstanceMethodReturnType` 仍被类型推断调用）。

## 阶段 3.6：DotOperator IL 辅助迁入 IL 后端（2026-11-14）

`DotOperatorILHelper` 已从 Runtime 迁入 `Old8Lang.Compiler/OperationHelpers/`，Runtime 只保留
`InstanceMethodTypeResolver`（实例方法返回类型推断、注册表解析与原生类型映射）。
ILD 发射与 Runtime 类型推断共享同一套解析规则，避免两条路径分叉。

## 阶段 3.7：FuncLangValue lambda IL 辅助迁入 IL 后端（2026-11-14）

`FuncLangValue.Compiler` partial 中的 IL 代码已改为 `Old8Lang.Compiler` 的扩展方法
（`FuncLangValueIlCompilerExtensions`）；Runtime 的 `FuncLangValue.Shared.cs` 只保留
`GetItemType` 与 `ToString`。其余运行时代码 partial（`FuncLangValue.Execution`、
`Instance.InstanceMethods`、`AnyLangValue.Reflection`、`SuperProxy`）仍待按后端拆分。

## 阶段 3.8：LangInterpreter 迁入解释后端（2026-11-14）

`LangInterpreter` 位于 `Old8Lang.Interpreter`；Runtime 只定义 `ILangInterpreter` 契约
（Manager、TypeAnnotationManager、PerformanceMonitor、ModuleCache、OutputProvider、Build）。
Runtime 的模块加载、编译器、反射与调试代码依赖契约，并通过
`RuntimeVisitors.CreateInterpreterHost(StandardLibraryCatalog)` 创建解释器宿主。
`VariateManager`、`ScopeLayer` 等解释期状态仍留在 Runtime，是后续接口化的对象。

## 阶段 3.9：解释器调试器迁入解释后端（2026-11-14）

`Old8Lang.Debugger` 命名空间下的 `Debugger`、`BreakpointManager`、`VariableWatcher`、
`DebuggableInterpreter` 已从 Runtime 迁入 `Old8Lang.Interpreter`。Runtime 不再保存解释器
调试状态；App、LanguageServer 与测试通过解释后端项目引用继续使用。

## 阶段 3.10：VM 引擎与字节码编译器迁入 VM 后端（2026-11-14）

```text
Old8Lang.VirtualMachine
  ├─ Visitors/BytecodeVisitor
  ├─ VM/（VirtualMachine 全部 partial、GlobalSlot/VMTypeMapper 经 friend 复用 Runtime）
  ├─ Generators/（字节码生成器值）
  └─ Bytecode/（BytecodeCompiler、ModuleSystem、Interop、Debugger、Profiler、VmStaticClassRegistry）
                    ↓ 只引用
              Old8Lang.Runtime
  ├─ Bytecode/Core（Instruction、OpCode、CallFrame、VMContext、IVirtualMachine、IBytecodeCompiler）
  ├─ Bytecode/Metadata、Bytecode/Closures、BytecodeFile（字节码格式与序列化）
  └─ 运行时值、实例/全局函数、模块系统、类型系统
```

Runtime 不再包含 VM 执行引擎或字节码编译器源码；`VMContext.CurrentVM` 与
`VMReflectionHelper` 只依赖 `IVirtualMachine`。`ModuleDependency`/`ExportedSymbol` 作为
`BytecodeFile` 的格式类型留在 Runtime，`ModuleLoader`/`ModuleRegistry` 位于 VM 后端。

## 阶段 3.11：编译器辅助类迁入 IL 后端（2026-11-14）

`CompiledDelegateRegistry`、`CodeGeneration/StaticClassCompiler`、`Helpers/CollectionHelper`、
`Helpers/ExceptionHelper`、`Helpers/TypeConversion`、`Helpers/TaskHelper` 已从 Runtime 迁入
`Old8Lang.Compiler`。Runtime 保留 `LocalManager` 及 IL 生成缓存、`RuntimeGlobals`/常量注册表、
`AssertHelper`/`ExceptionWrapper`、泛型特化与类型推断辅助等共享编译契约。

## 阶段 3.12：IL 编译器驱动迁入 IL 后端（2026-11-14）

`Compiler.Compile`、`GlobalVariableAnalysis`、`ILVerifier` 已从 Runtime 迁入
`Old8Lang.Compiler`。Runtime 新增 `IlCompilerOptions`（调试/IL 验证/异步状态机/类型推断开关与
文件头指令应用），`Compiler` 的同名属性委托到该选项。Runtime 的 `Compiler/` 只剩
`LocalManager`、IL 生成缓存、`GlobalVariableScope`、`RuntimeGlobals`/常量注册表、
`AssertHelper`/`ExceptionWrapper`、泛型特化与 `IlDisassembler` 等共享契约。

## 阶段 3.13：异步函数体 IL 迁入 IL 后端（2026-11-14）

`AsyncFuncLangValue.GenerateMethodBody` 改为 `Old8Lang.Compiler` 的扩展方法，
`IlDisassembler` 随迁入 `Old8Lang.Compiler/Verification/`。Runtime 的 `Compiler/` 只剩
`LocalManager`、`GlobalVariableScope`、IL 生成缓存、`RuntimeGlobals`/常量注册表、
`AssertHelper`/`ExceptionWrapper`、泛型特化与 `IlCompilerOptions` 等共享编译契约。
`AsyncStateMachineGenerator` 因 `LocalManager` 直接持有其类型仍暂留 Runtime。

## 阶段 3.14：异步状态机生成器迁入 IL 后端（2026-11-14）

`AsyncStateMachineGenerator` 迁入 `Old8Lang.Compiler/AsyncGeneration/`；Runtime 新增
`IAsyncStateMachineGenerator` 契约，`LocalManager` 的变量提升/读写路径只依赖接口。
Runtime 的 `AsyncGeneration/` 只剩解释器生成器与共享接口。

## 阶段 3 完成：最终依赖图（2026-11-14）

```text
Old8Lang.App / LanguageServer / Benchmarks / Tests
                     ↓
                   Hosting（显式注册三个后端）
        ↙                ↓                 ↘
Old8Lang.Interpreter  Old8Lang.Compiler  Old8Lang.VirtualMachine
        ↘                ↓                 ↙
                 Old8Lang.Runtime
   ↙        ↓         ↓          ↘
Core  Concurrency  Modules.Abstractions  Observability / ProjectSystem / Syntax
```

三个后端程序集互不引用，只引用 Runtime；Runtime 不引用任何后端。Runtime 保留共享运行时值、
内置函数、模块/类型系统、字节码格式与最小契约（`RuntimeVisitors`、`ILangInterpreter`、
`IVirtualMachine`、`IBytecodeCompiler`、`IAsyncStateMachineGenerator`、`IlCompilerOptions`），
以及共享执行状态（`VariateManager`/`ScopeLayer`、`LocalManager`/类型推断/泛型特化）。
`BackendAssemblyBoundaryTests` 固化程序集引用、契约归属与 Runtime 源码边界。
