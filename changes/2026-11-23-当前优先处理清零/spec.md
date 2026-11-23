# 当前优先处理清零 — 需求与约束（spec）

日期：2026-11-23  
对应目标：完成 `changes/Todo.md` 中「当前优先处理」剩余全部未勾选条目。

## 范围

1. **`Task.Completed` / `Task.WaitAll` / `Task.WaitAny`（三模式统一补齐）**
   - 解释器、IL、虚拟机三种模式统一支持 `Task.Completed()`、`Task.WaitAll(tasks)`、`Task.WaitAny(tasks)`。
   - `TaskAPITests` 对应 3 项用例编译执行通过，且实际执行断言。
2. **IL `async for-in` 与异步生成器（1 项失败用例 + 基础枚举协议）**
   - 修复 `AsyncGeneratorTests.AsyncGeneratorWithError_CompilesAndExecutesCorrectly` 及其背后的 IL `async for-in` / 异步生成器缺陷。
   - 确保不抛异常与抛异常的 `async for x in gen()` 在 IL 下都能真实迭代产出元素并正确传播异常。
3. **IL 异步状态机支持 `catch` / `finally` 块中的 `await`（2 项失败用例）**
   - `AsyncFunctionTests.AsyncFunctionWithFinally_CompilesAndExecutesCorrectly`
   - `AsyncGeneratorTests.AsyncGeneratorWithResourceCleanup_CompilesAndExecutesCorrectly`
   - 支持在 `catch` 块和 `finally` 块内使用 `await`，保持正常返回、异常重抛、资源清理和循环控制语义正确。
4. **EdgeCases 跨模式差异收口**
   - 排查并修复 VM 对私有字段访问静默放行的问题，使解释器与 VM 均在运行期拒绝外部访问私有成员。
   - 在 `Docs/MODE_SUPPORT.md` 明确记录私有成员访问 / 未声明变量 / 未知方法在 IL（编译期）与解释器/VM（运行期）的阶段差异及 `.Match` 非字符串 API 的口径。

## 明确不做

- 不修改与当前优先处理无关的其他 Todo 条目（如 LINQ join 配对语义、泛型扩展方法、LSP 等）。
- 不处理 IL 表达式内部 `await`（挂起时求值栈非空）与 `defer` 内 `await`。
- 不引入破坏程序集边界的跨层引用（须通过 `Old8Lang.Tests.Architecture`）。

## 验收标准

1. `Compiler.Async` 0 失败（当前 123 通过 / 6 失败 / 3 跳过 → 129 通过 / 0 失败 / 3 跳过，及新增回归全通过）。
2. `Task.Completed` / `Task.WaitAll` / `Task.WaitAny` 在解释器、IL、VM 三种模式下均通过测试。
3. VM 外部访问私有字段不再静默放行，与解释器一致抛访问权限错误；三模式差异写入 `Docs/MODE_SUPPORT.md`。
4. `Old8Lang.Tests.Architecture` 架构边界测试通过，新增/受影响测试组无回归。
5. `changes/Todo.md`「当前优先处理」全部条目勾选完成，相关文档（`Docs/CHANGELOG.md`、`Docs/MODE_SUPPORT.md`、`Docs/API_REFERENCE.md`）按规则同步更新。
