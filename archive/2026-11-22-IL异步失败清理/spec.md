# IL 异步失败清理 spec

## 目标

处理 `Compiler.Async` 组当前 21 项失败（2026-11-22 实测：132 项，108 通过、21 失败、3 跳过），按根因分三阶段推进：

1. 修正过期/误用的用例（纯测试侧）。
2. 让异步状态机中的非局部标识符走正常解析链（模块静态类、函数值）。
3. 支持异步函数读写顶层变量，并保证 `RuntimeGlobals` 的上下文能跨 `await`  continuation 传递。

## 范围

- 只处理 `Old8Lang.Tests/Compiler/Async/` 下已复现的失败。
- 需要改实现的路径：`VisitLangId` 的异步分支、`LocalManager` 的异步存取、`AsyncStateMachineGenerator`、`RuntimeGlobals`。
- 暂不处理：
  - `catch` / `finally` 中的 `await`（需要 Roslyn 式搬移处理器体）。
  - `Task.WaitAll` / `Task.WaitAny` / `Task.Completed`（三种模式都没有这些 API，需先定产品语义）。
  - `Task.Run` / `Task.StartNew` 的真实执行语义（当前是返回已完成 `null` Task 的桩）。

## 验收标准

- 每个阶段结束后单独运行 `Compiler.Async` 整组，失败数只减不增。
- 阶段 1 后：`TaskContinueWith`、`TaskCancellation`、`MultipleAsyncCalls`、`AsyncFunctionWithArray`、`TaskDelay` 通过。
- 阶段 2 后：`TaskRun` 的编译期 `Variable 'heavyComputation' not found` 消失（是否通过取决于 Task.Run 桩语义）。
- 阶段 3 后：A 类 10 项中不再出现 `not found in async state machine fields`，并验证异步 `await` 后写回顶层变量对主线程可见。
- 最终运行 `Compiler.Async` 和相关 `Compiler.*` 回归，不引入新的测试主机崩溃。
