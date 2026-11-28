# IL 异步失败清理 plan

## 阶段 1：用例修正

1. `TaskContinueWith` lambda 语法。
2. `TaskCancellation` 取消令牌源 API。
3. `TaskDelay` 补 `import Time` 并改用 `GetUnixTimeMilliseconds`。
4. `MultipleAsyncCalls` 顶层补 `await`。
5. `AsyncFunctionWithArray` 数组改列表。

验证：逐项运行对应测试，再跑 `Compiler.Async` 整组。

## 阶段 2：异步标识符解析回退

1. 给 `IAsyncStateMachineGenerator` / `LocalManager` 增加「字段不存在时返回 false」的读取入口。
2. `VisitLangId` 异步分支改为可回退。
3. 验证 `TaskRun` 不再报 `heavyComputation not found`；`TaskDelay` 不再报 `Time not found`。

## 阶段 3：异步顶层变量

1. 读路径接入 `Globals.IsSlot` + `RuntimeGlobals.EmitLoad`。
2. 写路径接入 `Globals.IsSlot`，避免把全局变量提升成各状态机私有字段。
3. `RuntimeGlobals` 改 `AsyncLocal` 并回归 `Spawn` 相关用例。
4. 跑 A 类 10 项 + `Compiler.Async` 整组 + `SpawnTests`。

## 回滚点

- 每个阶段结束提交一次；阶段 3 若整组出现崩溃，单独回退 `RuntimeGlobals` 改动，保留前两阶段。
