# IL 异步失败清理测试记录

## 基线

- 2026-11-22：`dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj --no-restore --filter 'FullyQualifiedName~Compiler.Async'`
- 结果：132 项，108 通过、21 失败、3 跳过。

## 阶段 1：用例修正

改动：

- `TaskAPITests.TaskDelay`：补 `import Time`，`Time.Now()` → `Time.GetUnixTimeMilliseconds()`。
- `TaskAPITests.TaskContinueWith`：`func(t) -> int {}` → `(t:int) -> {}`；回调参数是任务结果，改为 `t + 10`。
- `TaskAPITests.TaskCancellation`：`CreateCancellationTokenSource()` → `CancellationTokenSource()`。
- `AsyncFunctionTests.MultipleAsyncCalls`：顶层补 `await`。
- `AsyncFunctionTests.AsyncFunctionWithArray`：`results <- []` → `{}`，返回注解与期望值改为列表。

定向运行：5 项全部通过。

整组复跑：

```text
Total tests: 132
     Passed: 113
     Failed: 16
    Skipped: 3
```

21 → 16，无新增失败。

## 阶段 2：异步标识符解析回退

改动：

- `IAsyncStateMachineGenerator` 增加 `TryLoadVariable`。
- `VisitLangId` 的异步分支先查状态机字段，不是提升变量时继续走静态类 / 函数值 / 全局槽位解析链。
- `Task.Run` / `Task.StartNew` 改为通过 `IlValueBridge.RunAsync` 在线程池调用函数值并等待结果。

结果：`TaskRun`、`TaskDelay` 通过；整组 21 → 15。

## 阶段 3：异步顶层变量

改动：

- `VisitLangId` 对 `Globals.IsSlot` 的标识符发射 `RuntimeGlobals.EmitLoad`。
- `VisitAsyncFuncInit` 把顶层 `Globals` 传给异步函数的 `LocalManager`。
- `ForStatementRuntimeExtensions.BeginLoopVariableShadow` 在异步状态机中把循环变量提升为字段。
- `RuntimeGlobals` 从 `[ThreadStatic]` 改为 `AsyncLocal`，使 await 续体与主线程共享同一份槽位字典。
- 修复异步 `try/catch/finally + await` 的 JIT 崩溃：父区域派发先跳到 try 外的 `nop` 落点，
  再顺序进入 try 首指令（对齐 Roslyn 的两级派发）。
- 修复 `return` 在 try 中的完成顺序：先把返回值存入局部变量并 `leave` 到完成标签，
  finally 执行完后再 `SetResult`。

验证：

- 最小复现：异步函数 `await` 后写顶层变量，主线程可读到 `1`。
- `AwaitWithFinally` 语义复测：finally 在任务完成前执行，返回值与两个标志位均正确。
- `Compiler.Async`：**123 通过 / 6 失败 / 3 跳过**（132 项）。
- `Compiler` 全量：**1950 通过 / 41 失败 / 26 跳过**（2017 项）。其中 40 项是
  既有「缺少类型注解」用例问题（装饰器、命名实参、集成用例），另 1 项
  `ThreadTests.Run_ThreadExecutionOrder` 是已记录的时序抖动（单独复跑 3 次失败 1 次），
  均与本次改动无关。

## 剩余 6 项

| 用例 | 原因 |
|---|---|
| `TaskWaitAll` / `TaskWaitAny` / `TaskCompleted` | 三种模式都没有对应 Task 静态 API |
| `AsyncFunctionWithFinally` / `AsyncGeneratorWithResourceCleanup` | 暂不支持 catch/finally 内的 await |
| `AsyncGeneratorWithError` | IL `async for-in` 枚举协议本身不可用（不抛异常也报 `Entry point was not found.`） |

