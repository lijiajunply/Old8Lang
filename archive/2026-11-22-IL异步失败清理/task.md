# IL 异步失败清理 task

## 基线（2026-11-22）

- `Compiler.Async`：132 项，108 通过、21 失败、3 跳过。

## 阶段 1：用例修正

- [x] `TaskContinueWith`：lambda 改为 `(t:int) -> { ... }`
- [x] `TaskCancellation`：改用 `CancellationTokenSource()`
- [x] `TaskDelay`：补 `import Time`，改用 `Time.GetUnixTimeMilliseconds()`
- [x] `MultipleAsyncCalls`：顶层补 `await`
- [x] `AsyncFunctionWithArray`：`results <- {}` 并同步注解/期望
- [x] 复跑 `Compiler.Async`，记录失败数（21 → 16）

## 阶段 2：异步标识符解析回退

- [x] `IAsyncStateMachineGenerator` 增加可回退的字段读取（`TryLoadVariable`）
- [x] `VisitLangId` 异步分支回退到正常解析链
- [x] 复跑 `TaskRun` / `TaskDelay`（均通过）

## 阶段 3：异步顶层变量

- [x] 读路径接入 `Globals.IsSlot` + `RuntimeGlobals.EmitLoad`
- [x] 写路径区分全局槽位与状态机字段（`VisitAsyncFuncInit` 把 `Globals` 传给函数 LocalManager）
- [x] C 风格 for 循环变量在异步状态机中提升为字段
- [x] `RuntimeGlobals` 改 `AsyncLocal` 并回归 `Spawn`
- [x] 顺带修复：异步 `try/catch/finally + await` 的 JIT 崩溃（区域前置落点 + `nop`）
- [x] 顺带修复：`return` 在 try 中先存返回值、finally 执行完再 `SetResult`
- [x] 复跑 A 类用例
- [x] 复跑 `Compiler.Async` 整组与 `Compiler` 全量回归

## 结果

- `Compiler.Async`：108 通过/21 失败 → **123 通过/6 失败/3 跳过**
- 剩余 6 项：Task API 3 项、catch/finally 中 await 2 项、IL `async for-in` 枚举 1 项

