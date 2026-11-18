# Async：顶层 await 用例迁移与一次崩溃实验回退 —— 测试报告

日期：2026-11-18
关联待办：`changes/Todo.md`「Async 26 项：await 位置/状态机变量」。

## 1. 现象

`AwaitTests` 里多处把 async 函数当同步函数调用：

```old8
finalResults <- testAwaitInLoop()   // 得到 Task / AsyncStateMachineBox
Assert.Equal({2,4,6,8,10}, finalResults)
```

三种模式里 `x <- asyncFn()` 都返回 Task 对象（解释器探针：`Task(Status: Running)`），
所以断言拿到的是 `AsyncTaskMethodBuilder+AsyncStateMachineBox`。顶层 `await` 在
三种模式都可用（`x <- await f()` 返回 7）。

## 2. 用例迁移

`AwaitTests` 的 6 处调用补上 `await`：

- `testSequentialAwaits()`、`testConditionalAwait(true/false)`、`testAwaitInLoop()`、
  `testComplexExpressions()`、`testMethodChaining()`。

`AwaitTests` 17 项：失败 5 → **0**（2 跳过）；`Compiler.Async` 26 → **21**。

## 3. 崩溃实验（已回退）

为验证「async 函数访问顶层全局变量」，尝试：

1. async 函数体的 `LocalManager` 继承 `Globals`；
2. `VisitLangId` 在 async 模式下，非提升变量回退到全局槽位/普通解析。

结果：`AsyncStateMachineResume / finally / using` 等用例在**批量运行**时让测试进程
栈溢出（`Test Run Aborted`，Sequence 指向 `UsingBlockWithAwait_ResumesInsideRegion`），
单独运行却通过。定位成本高、风险大，**已完整回退**这两处改动，保留 `HasVariable`
接口的删除；当前 async 行为与 r31 一致（顶层全局变量在 async 内仍是编译期错误）。

## 4. 当前基线

- `Compiler.Async` 21 失败：7 项 await 在 catch/finally/defer/表达式内（已有明确
  CompilerException）、Task API 未实现（Completed/WaitAny/WaitAll/ContinueWith 等）、
  其余为异步执行结果未等待/API 缺失。
- 全组回归见 `changes/Todo.md`。
