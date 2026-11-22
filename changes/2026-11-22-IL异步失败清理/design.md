# IL 异步失败清理 design

## 现状链路

1. `CompilerVisitor.Expressions.VisitLangId`：只要 `local.AsyncStateMachineGenerator != null`，直接 `local.LoadVariable` 并返回，不再尝试参数、函数值、静态类或全局槽位。
2. `LocalManager.LoadVariable/StoreVariable`：只要处于异步状态机，就把全部读写路由到状态机字段；字段不存在则抛异常。
3. `RuntimeGlobals` 用 `[ThreadStatic]` 保存全局槽位，`await` 后续体可能在线程池线程执行，看不到主线程槽位。
4. C 风格 `for` 的循环变量由 `ForStatementRuntimeExtensions.BeginLoopVariableShadow` 直接 `DeclareLocal`，没有走 `LocalManager.DefineVariable`，异步状态下不会提升为字段。

## 方案

### 阶段 1：用例修正（无实现改动）

- `TaskContinueWith`：旧 lambda 语法 `func(t) -> int {}` 改为 `(t:int) -> int {}`。
- `TaskCancellation`：`CreateCancellationTokenSource()` 返回 `int` 资源 id，改用 `CancellationTokenSource()`。
- `TaskDelay`：补 `import Time`，并把不存在的 `Time.Now()` 改为 `Time.GetUnixTimeMilliseconds()`。
- `MultipleAsyncCalls`：顶层补 `await`。
- `AsyncFunctionWithArray`：`results <- []`（数组，无 `Add`）改为 `{}`（列表），返回注解与期望值同步改为列表。

### 阶段 2：异步状态机的标识符解析回退

- `IAsyncStateMachineGenerator` 增加 `bool TryLoadVariable(ILGenerator, string)`（或让 `LoadVariable` 返回 bool）。
- `LocalManager` 在异步状态下先尝试状态机字段，失败后再走正常解析链；`VisitLangId` 的顺序保持：
  静态类 → 状态机字段 → 捕获 → 局部变量/参数 → 字段 → 具名函数值 → 全局槽位。
- `Time` 这类 `import` 后注册到 `GlobalStaticClasses` 的模块已在第一步之前处理，无需额外改动；`heavyComputation` 由 `TryGetFunctionMethod` 分支处理。

### 阶段 3：异步函数读写顶层变量

- 读：状态机无字段且 `local.Globals.IsSlot(name)` 时，发射 `RuntimeGlobals.EmitLoad`。
- 写：`DefineVariable/StoreVariable` 在异步状态下先判断 `Globals.IsSlot(name)`；是全局槽位就写 `RuntimeGlobals`，否则仍提升为状态机字段。
- 上下文传递：把 `RuntimeGlobals._slots` 从 `[ThreadStatic]` 改为 `AsyncLocal<Dictionary<string, Slot>?>`（保留 `CaptureContext` / `InstallContext` 给 `Spawn` 显式安装）。`AsyncLocal` 会随 `await` 的 `ExecutionContext` 流到 continuation；字典本身是共享引用，续体中的写回对主线程可见。
- 风险控制：上一轮回退实验曾出现批量运行栈溢出。本次每次只改一处，先跑单测，再跑整组 `Compiler.Async` + `AsyncStateMachineResume/finally/using` 相关用例，出现异常立即定位而非继续叠加。

## 备选方案

- 状态机携带槽位字典字段：改动面比 `AsyncLocal` 大，且需要每个 await 前后手动安装。
- 保持 `[ThreadStatic]` + 在每次续体入口安装：需要生成额外 IL，容易漏掉 finally/using 路径。

选 `AsyncLocal`：改动集中在一个类，语义与 `ExecutionContext` 一致。
