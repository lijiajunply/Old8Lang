# 当前优先处理清零 — 执行清单（task）

## 基线（2026-11-23）
- `Compiler.Async`：132 项，123 通过、6 失败、3 跳过。
- `changes/Todo.md`「当前优先处理」剩余 2 项未勾选。

## 阶段 1：`Task.Completed` / `Task.WaitAll` / `Task.WaitAny`
- [x] Runtime：`TaskClassLangValue` 新增 `Completed` / `WaitAll` / `WaitAny` 及 `VmReusableMethods`
- [x] VM：`VmStaticClassRegistry` 新增三项并验证执行
- [x] IL：`DotOperatorILHelper`、`TypeInferenceVisitor` 及运行期 helper 实现三项
- [x] 测试：`TaskAPITests` 全绿（含补齐顶层 `await`）+ 三模式测试（58/58 通过）

## 阶段 2：IL `async for-in` 与异步生成器
- [x] 摸底 `AsyncGeneratorTests` 及 IL 异步生成器 / `async for-in` 路径
- [x] 新增 `IlAsyncGeneratorStream` / `IlAsyncYieldEmitter` / `IlAsyncLoopEnumerator`，实现 IL 异步生成器（返回 `IAsyncEnumerable<object>`）与非阻塞 `async for-in`
- [x] `AsyncGeneratorTests` 全部补齐顶层 `await` 验证通过（11/11）

## 阶段 3：IL `catch` / `finally` 中的 `await`
- [x] 放开 `AsyncStateMachineGenerator` 中 `catch`/`finally` 体内的 `await` 分析（保留 `catch` 过滤器与 `defer` / 栈非空表达式限制）
- [x] 实现异步 `catch` / 异步 `finally` 搬移降级、返回值状态机字段提升与待决控制流（normal / rethrow / return / break / continue）
- [x] 验证 `AsyncFunctionWithFinally`、`AsyncGeneratorWithResourceCleanup` 及 6 项新增专项回归全部通过，`Compiler.Async` 清零（133 通过 / 0 失败 / 3 跳过）

## 阶段 4：EdgeCases 跨模式差异收口
- [x] 修复 VM 实例/静态字段 `AccessModifier` 元数据传播与 `OpCode.GetField`/`SetField` 运行期访问检查
- [x] 补充 VM 私有实例/静态字段读写测试（`VMUnexpectedInputsTests`），VM EdgeCases/Classes/Reflection 177/177 通过
- [x] 更新 `Docs/MODE_SUPPORT.md`、`Docs/API_REFERENCE.md`、`Docs/CHANGELOG.md`

## 阶段 5：验收与收尾
- [x] 回归 `Compiler.Async`、`Compiler.EdgeCases`、`Compiler.Exceptions`、`VirtualMachine.Async`、`Interpreter.Async`、`Architecture`（538 通过，仅余既有记录的 `ScalarLiteralExpressionTests` 1 项）
- [x] 子代理只读 review，修复私有字段权限借用与生成器提前执行/清理竞态；值类型异步枚举适配经测试通过
- [x] 产出 `test.md` 并更新 `changes/Todo.md`
- [ ] 完整 Architecture 测试通过：既有 ScalarLiteralExpressionTests 用例仍失败，已记录在全局 Todo。
