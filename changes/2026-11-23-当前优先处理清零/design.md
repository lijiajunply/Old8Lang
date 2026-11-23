# 当前优先处理清零 — 方案与取舍（design）

## 1. `Task.Completed` / `Task.WaitAll` / `Task.WaitAny`

### 备选方案
- 方案 A：仅修改/删除 `TaskAPITests` 的 3 个用例。
- 方案 B：在解释器、IL、VM 三种模式统一实现 `Task.Completed()`、`Task.WaitAll(tasks)`、`Task.WaitAny(tasks)`。

### 选定方案：方案 B
原因：三个方法与现有 `Task.WhenAll`/`Task.WhenAny`/`Task.FromResult` 高度对称，语义清晰明确，三模式实现成本低且能补齐异步并发标准能力。

### 受影响程序集与接口
- `Old8Lang.Runtime`：`TaskClassLangValue` 新增 `Completed`、`WaitAll`、`WaitAny` 及 `VmReusableMethods` 映射；`TypeInferenceVisitor` 补齐三者返回类型推断（`Task` / `void` / `int`）；必要时提供 `TaskIlHelper` 静态运行时辅助（供 IL 复用）。
- `Old8Lang.Compiler`：`DotOperatorILHelper.GenerateTaskStaticMethodCall` 新增三分支。
- `Old8Lang.VirtualMachine`：`VmStaticClassRegistry` 的 `Task` 表新增三项。

## 2. IL `async for-in` 与异步生成器

待阶段 2 深入检查 `AsyncGeneratorTests` 与 `AsyncFuncLangValueIlCompilerExtensions` / `AsyncForInStatementRuntimeExtensions.Compiler.cs` 后补充具体细节。原则：
- 修正 `GenerateAsyncEnumerableIl` 及运行时对象适配，避免直接对任意集合/包装类型错误调用接口方法或 `InvalidCastException`；
- 核查异步生成器 `yield` 在 IL 下的生成方式与异常传播。

## 3. IL `catch` / `finally` 中的 `await`

### 核心思路（Roslyn 风格处理器体搬移）
CLR 禁止从受保护块（`try`/`catch`/`finally`）外部跳转到块内恢复标签，且 `finally` 内不允许用 `leave` 挂起退出。
- **分析阶段**：允许 `tryStmt.CatchBlocks` 和 `tryStmt.FinallyBlock` 中的 `await`（仍拒绝 catch 过滤器 `when` 内的 `await`），将其归入外层/当前可到达的 `AwaitRegion`，标记该 `TryStatement` 需要异步 catch / 异步 finally 降级。
- **异步 `catch` 降级**：
  - 在 CLR `catch (Exception ex)` 块内仅做轻量捕获：将异常存入状态机字段，并按 catch 子句顺序完成类型/过滤器匹配，记录命中的 catch 索引（未命中则 `rethrow`），`leave` 跳出 CLR catch 块。
  - 在 `EndExceptionBlock` 之后（外层普通代码块，位于状态机可派发区域内），按命中的 catch 索引分派执行对应的 catch 块体（可自由包含 `await`、`return`、`throw`）。
- **异步 `finally` 降级**：
  - 在状态机上分配「待决控制流」字段（保存异常对象与目标分支编码：0=正常继续、1=重抛异常、2=方法 return 等）。
  - 用 `try { ... } catch (Exception ex) { pendingException = ex; pendingReason = 1; leave afterTry; }` 包裹 `try`（及异步 `catch` 之后的逻辑），并在正常出口与 `return` 出口（改写当前作用域的 `ReturnLabel` / 拦截 `EmitReturn`）设置对应原因后跳到异步 `finally` 体。
  - 异步 `finally` 体位于异常块外部，可包含任意 `await`；执行完毕后根据待决原因：重抛保存的异常（`ExceptionDispatchInfo.Capture(ex).Throw()`）、执行外层 `return`（或跳外层异步 `finally`）、或继续执行 `try` 之后的语句。

## 4. EdgeCases 跨模式差异与 VM 私有成员控制

- 在 `Old8Lang.VirtualMachine` 检查类元数据（`ClassMetadata`）的字段访问修饰符存储与 `OpCode.GetField` / `OpCode.SetField` 执行逻辑：
  - 若当前执行帧不处于该类（或其子类，针对 `protected`）的方法体内，外部读写 `private` 字段应抛出访问权限错误（与解释器一致）。
- 在 `Docs/MODE_SUPPORT.md` 的「三种模式共同的跨模式差异」补充私有成员访问、未声明变量、未知方法及 `.Match` 的三模式口径。
