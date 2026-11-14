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
