# IL 异步状态机在 try 区域里的 await 生成无效 IL —— 测试报告

日期：2026-11-15
关联待办：`changes/Todo.md`「优先顺序 1. 排查会使测试进程崩溃的 IL 问题」。

## 1. 现象

`--filter Old8Lang.Tests.Compiler.Async` 整组 111 项里 **65 项失败**，其中 32 处异常是
`System.InvalidProgramException: Common Language Runtime detected an invalid program.`，
抛出点是 `AsyncStateMachine_<函数名>.MoveNext()`。CLI 最小复现：

```bash
dotnet Old8Lang.App/bin/Debug/net10.0/Old8Lang.App.dll -il v2.old8
# [RUNTIME_ERROR] Common Language Runtime detected an invalid program.
```

## 2. 最小复现

```old8
async func f() {
    try { await Task.Delay(5) } catch (e) { PrintLine(e) }
}
f()
```

二分结论：

| 变体 | 结果 |
|---|---|
| `async func f() { try { PrintLine("x") } catch (e) {} }`（try 里没有 await） | 正常 |
| `async func f() { if true { await Task.Delay(5) } }`（await 在 if 里） | 正常 |
| `async func f() { while ... { await ... } }`（await 在 while 里） | 正常 |
| `async func f() { try { await ... } catch ... }` | **无效 IL** |
| `async func f() { try { await ... } finally ... }` | **无效 IL** |

即：只要 await 位于 try 区域内部就必现，与 try 里是 catch 还是 finally 无关。

## 3. 根因

`AsyncStateMachineGenerator.GenerateMoveNextMethod` 在方法开头统一发射一次状态派发：

```csharp
il.Emit(OpCodes.Ldarg_0);
il.Emit(OpCodes.Ldfld, StateField!);
il.Emit(OpCodes.Switch, labels);   // labels[i] 是第 i 个 await 的恢复标签
il.Emit(OpCodes.Br_S, startLabel);
```

每个 await 的恢复标签由 `EmitAwaitResume` 在 **await 站点**标记。await 位于 try 里时，
恢复标签就落在 try 区域内部，于是 `Switch` 的分支从 try 外部跳进 try 区域中间。

ECMA-335 允许从外部跳到 try 的**首指令**，但禁止跳到区域中间。CLR 的 JIT 直接把整个方法
判为无效 IL。用 `DynamicMethod` 做了三组对照实验：

| 结构 | 结果 |
|---|---|
| 从 try 外部 `br` 到 try 区域中间 | `InvalidProgramException` |
| 从 try 外部 `br` 到 try 区域首指令 | 正常 |
| 从 try 内部 `br` 到同一 try 区域中间（Roslyn 采用的形式） | 正常 |

Roslyn 的异步状态机不在 MoveNext 开头用一个 `switch` 跨区域跳转：它在**方法体入口**和
**每个含 await 的受保护区域入口**各放一条派发链，外层只跳到内层区域的首指令，
内层再跳到自己区域内的恢复标签。这样任何分支都不跨区域中间。

## 4. 修复

1. `AsyncStateMachineGenerator` 在构造时按「所在受保护区域」给 await 分组：
   方法体是根区域，每个 `TryStatement` 的 try 块与 `UsingStatement` 的 using 体是子区域；
   同时登记「区域 → 直属 await 状态号」与「区域 → 子区域」两组关系。
2. 方法体入口发射根区域派发链；`TryStatement` 在 `BeginExceptionBlock` 之后、
   try 块第一条语句之前，`UsingStatement` 在 `BeginExceptionBlock` 之后调用
   `EnterProtectedRegion`，标记区域首指令并发射本区域派发链。
3. 派发链按 `ldarg.0; ldfld <>1__state; ldc.i4 idx; beq target` 逐条比较：
   直属 await 直接跳恢复标签，子区域的 await 跳子区域入口标签。
4. 用户 `finally` 体与 using 的 Dispose 前加 `if (state < 0)` 守卫
   （`EmitSuspendedFinallyGuard`）：await 挂起时 `leave` 会沿路触发 finally，
   但挂起不是函数退出，用户 finally / Dispose 只能执行一次。
5. 无法恢复的 await 位置在编译期报错，而不是生成错误控制流或无效 IL：
   - `catch` / `finally`（含 catch 过滤器）：只能由异常机制进入，恢复标签无法用分支到达；
   - `defer` / `yield`：IL 后端根本不生成这些语句的指令；
   - 表达式内部的 await（挂起时求值栈非空）：`leave` 要求求值栈为空。
     分析端按求值顺序跟踪「是否有待用值」，例如 `1 + await f()`、`PrintLine(await f())`
     会被拒绝，而 `x <- await f()`、`await f() + 1`、三元分支中的 await 仍允许。
6. `GetStateIndex` 增加防护：分析结束后若生成端再遇到未登记的 await 节点，
   直接抛编译错误，避免「状态没有派发链 → 从函数体开头重跑」的静默挂死。

涉及文件：

- `Old8Lang.Runtime/AsyncGeneration/IAsyncStateMachineGenerator.cs`
- `Old8Lang.Compiler/AsyncGeneration/AsyncStateMachineGenerator.cs`
- `Old8Lang.Compiler/Visitors/CompilerVisitor.ExpressionsAndValues.cs`
- `Old8Lang.Compiler/Visitors/CompilerVisitor.Statements2.cs`
- `Old8Lang.Compiler/Visitors/CompilerVisitor.Statements.cs`

## 5. 验证

### 5.1 新增回归测试（共 15 项）

`Old8Lang.Tests/Compiler/Async/AsyncAwaitTryBlockTests.cs`（3 项）：
try/catch、try/finally、嵌套 try 里的 await 都能编译执行。
用 `Task.FromResult` 走「已完成」快速路径，整个异步函数体在调用时同步执行；
无效 IL 由 JIT 在方法首次执行时校验，所以这 3 项在修复前稳定失败。

`Old8Lang.Tests/Compiler/Async/AsyncStateMachineResumeTests.cs`（6 项）：

- `TwoSequentialAwaits_DoNotReevaluateEarlierAwait`：两个真实挂起的 await，
  静态计数器必须正好是 2（重复求值会多计）；
- `AwaitInsideTryLoop_ResumesEachIteration`：循环内 try+await，三轮累加得到 3；
- `AwaitInsideNestedTry_ResumesAtInnerAwait`：嵌套 try + finally，累加得到 111；
- `SiblingTriesEachWithAwait_ResumeInCorrectRegion`：两个并列 try 各含 await，得到 11；
- `OuterAndInnerTryBothWithAwait_ResumeCorrectly`：外层与内层 try 都有直属 await，得到 1111；
- `UsingBlockWithAwait_ResumesInsideRegion`：using 体里的 await，得到 11。

`Old8Lang.Tests/Compiler/Async/AsyncFinallyTests.cs`（2 项）：

- `FinallyRunsOnceAfterRealSuspension`：真实挂起后 finally 只执行 1 次；
- `FinallyRunsOnceWhenExceptionPropagates`：异常传播时 finally 1 次 + catch 1 次 = 2。

`Old8Lang.Tests/Compiler/Async/AsyncUnsupportedAwaitTests.cs`（4 项）：
catch / finally / defer / 表达式内部的 await 都给出明确编译错误，
并断言异常类型不是 `InvalidProgramException`。

验证有效性：

- 把 AsyncStateMachineGenerator 等改动整体回退到旧实现后，恢复与 finally 相关的 5 项
  （Resume 3 项、Finally 2 项）全部失败；
- 再回退到原始的 `switch` 派发后，`AsyncAwaitTryBlockTests` 3 项全部失败；
- 恢复后 15 项全部通过。

### 5.2 CLI 复现对比

| 用例 | 修复前 | 修复后 |
|---|---|---|
| `try { await } catch` | 无效 IL | 正常 |
| `try { await } finally` | 无效 IL | 正常，finally 只在完成时执行一次 |
| 两个连续 await（无 try） | 正常 | 正常 |
| 两个连续 await（try 内） | 无效 IL | 正常，计数不再多算 |
| 循环内 try+await（3 轮） | 无效 IL | 累加 3 |
| 嵌套 try，内外都有 await | 无效 IL | 正常，finally 一次，计数 1111 |
| 并列 try 各含 await | 无效 IL | 正常，计数 11 |
| `using { await }` | 无效 IL / 提前 Dispose | 正常，Dispose 一次 |
| `await f() + 1` | 无效 IL | 正常（8） |
| `1 + await f()` / `PrintLine(await f())` | 无效 IL 或错误控制流 | 明确编译错误 |
| `catch`/`finally`/`defer` 中的 await | 无效 IL 或错误控制流 | 明确编译错误 |

### 5.3 基线对比

`dotnet test ... --filter FullyQualifiedName~Old8Lang.Tests.Compiler.Async`：

| 指标 | 修复前 | 修复后 |
|---|---|---|
| 失败 / 通过 / 跳过 | 65 / 43 / 3 | 54 / 75 / 3（含新增 21 项） |
| `InvalidProgramException` 次数 | 32 | **0** |

按原有 111 项折算：修复前 65 失败 / 43 通过，修复后 54 失败 / 54 通过，即 **11 项转通过**。
`Compiler.Exceptions` 组由 9 失败降为 8 失败：`ErrorPropagationThroughAsync_...` 转通过。
其余 17 个 `Compiler.*` 子组的通过/失败/跳过数与修复前完全一致
（`Compiler.Threading` 有一项偶发线程抖动，单独复跑 3/3 通过）。
`Parser`（889 项）、`VirtualMachine`（1083 项）、`LanguageServer`（474 通过）、
`Architecture`、`Diagnostics`、`EdgeCases`、`Error`、`InstanceMethods`、`Libraries`、`TypeSystem`
等命名空间均无 `InvalidProgramException`、无测试主机中止。

## 6. 遗留（未在本轮修改）

1. 剩余 54 项 `Compiler.Async` 失败的构成（按首条错误分类）：
   24 项经 `Assert.Null` 暴露的 `RUNTIME_ERROR`（内部含 NRE、全局变量提升、API 缺口等）、
   10 项 `CompilerException`、9 项 `Variable '...' not found in async state machine fields`
   （`Spawn`/全局变量提升与异步状态机的组合）、4 项用例自身旧语法（`spawn backgroundTask()` 等）、
   3 项未实现的 `Task.Completed/WaitAll/WaitAny`、2 项 catch/finally 中的 await、2 项其它。
2. catch / finally 中的 await 若要支持，需要像 Roslyn 一样把处理器体搬出异常块，
   再用 `<>s__` 字段记录「待执行哪个处理器体」。
3. 表达式内部的 await 若要支持，需要把挂起点的中间值提升为状态机字段
   （Roslyn 的临时变量提升）；当前直接拒绝并提示先赋值给变量。
4. `Task.Run` / `Task.StartNew` 仍是「已完成任务」的简化实现，不会真正调度委托。
5. `EmitReturnInternal` 先 `SetResult` 再 `leave`，因此 `return` 位于 try 中时用户 finally
   在任务完成之后执行；属既有行为，本轮未改。
