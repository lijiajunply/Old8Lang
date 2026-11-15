# IL 模式 Task 静态方法实参发射导致无效 IL —— 测试报告

日期：2026-11-15
关联待办：`changes/Todo.md`「优先顺序 1. 排查会使测试进程崩溃的 IL 问题」。

## 1. 现象

修掉「await 在 try 区域里」的无效 IL 之后，`Compiler.Async` 仍有 18 处
`System.InvalidProgramException`，集中在 `Task` 静态方法调用：

| 用例 | 抛出点 |
|---|---|
| `TaskAPITests.TaskFromResult_...` | `AsyncStateMachine_testFromResult.MoveNext()` |
| `TaskAdvancedTests.Run_TaskFromResult*` | `AsyncStateMachine_boolTask.MoveNext()` |
| `TaskAPITests.TaskCompleted_...` | `AsyncStateMachine_testCompleted.MoveNext()` |
| `TaskAPITests.TaskWhenAll_/TaskWhenAny_...` | `AsyncStateMachine_main.MoveNext()` |

## 2. 根因

`DotOperatorILHelper.GenerateTaskStaticMethodCall` 在分派前**统一求值并压栈全部实参**，
再由各分支决定怎么用：

```csharp
foreach (var instanceId in instance.Ids) { 求值; 压栈; }
switch (methodName) { ... }
```

各分支对实参的假设不一致，产生三类无效 IL：

1. **`Task.FromResult` 漏装箱**：统一实例化为 `FromResult<object>(object)`，但实参是以
   `int` 直接压栈的。反汇编可见 `ldc.i4.s 42` 之后直接
   `call Task::FromResult<Object>(object)`，栈上是 int、形参是 object，JIT 判为无效 IL。
2. **`Task.WhenAll` / `Task.WhenAny` 重复求值**：分派前已压入列表，分支里又
   `listExpr.Accept(...)` 压一次，`RuntimeWhenAll` 只消费一个，留下一个未消费的栈值。
   反汇编为 `ldfld tasks; ldfld tasks; call RuntimeWhenAll`。
3. **未知静态方法什么都不发射**：`Task.Completed()`（以及 `Task.WaitAll` / `Task.WaitAny`）
   不在分支表里，函数返回 `Task<object>` 却没有发射任何指令，调用方按「栈上有一个值」继续用，
   栈直接失衡。`Task.Run` / `Task.StartNew` 分支忽略实参、自己压 `null`，同样会留下
   未消费的栈值。

## 3. 修复

把「先统一压栈」改成「各分支自己按形参求值与转换」：

1. `GenerateTaskStaticMethodCall` 不再预压实参，直接按方法名分派到负责该方法的发射函数；
2. 新增 `EmitTaskArguments`（按顺序求值并返回静态类型）与
   `EmitTaskArgumentConversion`（按形参类型装箱 / 拆箱 / `conv.i4` / `conv.r8`）；
3. `FromResult` / `FromException` 显式把值类型实参装箱为 `object`；
4. `WhenAll` / `WhenAny` 只求值一次；静态类型为 `object` 时也走 `RuntimeWhenAll` /
   `RuntimeWhenAny`（运行期再转 `IEnumerable`），不再出现未消费的栈值；
5. `Run` / `StartNew` 保留「已完成任务」的简化实现，但实参仍会求值后 `pop`；
6. 未知 `Task` 静态方法改为抛 `InvalidOperationError`（`INVALID_OPERATION_ERROR`），
   不再「不发射指令却声明返回值」。

涉及文件：`Old8Lang.Compiler/OperationHelpers/DotOperatorILHelper.cs`。

## 4. 验证

### 4.1 新增回归测试

`Old8Lang.Tests/Compiler/Async/TaskStaticMethodIlTests.cs`（6 项）：

- `FromResult_ValueTypeArgument_IsBoxedForObjectParameter`
- `FromResult_ReferenceTypeArgument_CompilesAndExecutes`
- `WhenAll_ListArgument_IsEvaluatedOnce`
- `WhenAny_ListArgument_IsEvaluatedOnce`
- `Run_ArgumentIsStillConsumed`
- `UnknownStaticMethod_ReportsManagedErrorInsteadOfInvalidIl`

单独回退本修复验证：6 项中 5 项失败（`FromResult_ReferenceTypeArgument` 因引用类型本就不需要
装箱而通过），恢复后 6 项全部通过。

### 4.2 基线对比

`Compiler.Async`（仅完成上一项「await 在 try 里」修复时为 60 失败）：

| 用例 | 修复前 | 修复后 |
|---|---|---|
| `TaskFromResult_...` | 失败（无效 IL） | 通过 |
| `TaskWhenAll_...` | 失败（无效 IL） | 通过 |
| `TaskWhenAny_...` | 失败（无效 IL） | 通过 |
| `Run_TaskFromResult_...` | 失败（无效 IL） | 通过 |
| `Run_TaskFromResultDifferentTypes_...` | 失败（无效 IL） | 通过 |
| `Run_TaskWhenAll_...` | 失败（无效 IL） | 通过 |
| `Run_TaskWhenAllWithException_...` | 失败（无效 IL） | 通过 |
| `Run_TaskWhenAny_...` | 失败（无效 IL） | 通过 |
| `TaskWaitAll_...` | 「通过」 | 明确报 `Task 静态方法 'WaitAll' 未在 IL 模式实现` |
| `TaskWaitAny_...` | 「通过」 | 明确报 `Task 静态方法 'WaitAny' 未在 IL 模式实现` |
| `TaskCompleted_...` | 失败（无效 IL） | 明确报 `Task 静态方法 'Completed' 未在 IL 模式实现` |

`TaskWaitAll` / `TaskWaitAny` 原先的「通过」是**偶然通过**：分支没有发射调用，
栈上只剩预压的列表实参，恰好被语句层丢弃，而 `Assert` 写在异步函数体里、
任务未完成时根本不会执行。现在它们变成编译期明确报错，与解释器一致
（`TaskClassLangValue` 同样没有这三个方法）。

其余 `Compiler.*` 子组通过/失败数与修复前一致。

## 5. 遗留

1. `Task.Completed` / `Task.WaitAll` / `Task.WaitAny` 三种模式都没有实现，
   对应的三个测试属于「用例依赖不存在的 API」，需要先定是否补 API，再改用例或实现。
2. `Task.Run` / `Task.StartNew` 仍是「已完成任务」的简化实现，不会真正调度委托；
   把函数值作为实参传给它们还会撞上「IL 函数值调用」的独立缺口。
3. `Task.FromResult` 的结果类型固定为 `Task<object>`，`await` 之后是 `object`，
   静态类型为值类型的后续运算依赖运行期桥接。
