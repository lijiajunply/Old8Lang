# 当前优先处理清零 — 阶段划分（plan）

## 阶段 1：`Task.Completed` / `Task.WaitAll` / `Task.WaitAny`
1. 在 `TaskClassLangValue` 实现 `Completed` / `WaitAll` / `WaitAny`，接入 `VmReusableMethods`。
2. 在 `VmStaticClassRegistry` 与 `VirtualMachine` 接入 VM 支持。
3. 在 `DotOperatorILHelper`、`TypeInferenceVisitor` 及 Runtime helper 实现 IL 支持。
4. 补写三模式单元测试，验证 `TaskAPITests` 全绿。

## 阶段 2：IL `async for-in` 与异步生成器
1. 全面摸底 `AsyncGeneratorTests` 与 IL 异步生成器、`AsyncForInStatement` 编译路径。
2. 修复 `AsyncGeneratorWithError` 暴露的 `ExceptionWrapper` 转换问题及 `async for-in` / 异步生成器核心缺陷。
3. 验证 `AsyncGeneratorTests` 相关用例与新增不抛异常/抛异常回归。

## 阶段 3：IL `catch` / `finally` 中的 `await`
1. 在 `AsyncStateMachineGenerator` 放开 `catch`/`finally` 块内 `await` 分析（保留 catch filter 限制），记录哪些 `TryStatement` 含异步 `catch` 或异步 `finally`。
2. 在 `CompilerVisitor.VisitTryStatement` 实现异步 `catch` 与异步 `finally` 的搬移降级，并在 `AsyncStateMachineGenerator` 支持可组合的 return/break/continue 拦截（嵌套 `try-finally`）。
3. 验证 `AsyncFunctionWithFinally`、`AsyncGeneratorWithResourceCleanup` 及补充专项测试（catch await、finally await、try return + finally await、try throw + finally await、嵌套 finally await）。

## 阶段 4：EdgeCases 跨模式差异收口
1. 核查并修复 VM 私有字段读写访问控制。
2. 编写三模式私有成员访问测试。
3. 更新 `Docs/MODE_SUPPORT.md`、`Docs/API_REFERENCE.md`、`Docs/CHANGELOG.md`。

## 阶段 5：验收、Review 与文档收尾
1. 运行 `Compiler.Async`、VM/Interpreter 相关测试、`Old8Lang.Tests.Architecture` 架构边界测试。
2. 调用只读子代理 review 全部核心改动。
3. 编写 `changes/2026-11-23-当前优先处理清零/test.md`，勾选更新 `changes/Todo.md`。
