# IL 并发全局函数的 int 实参缺少拆箱 —— 测试报告

日期：2026-11-15
关联待办：`changes/Todo.md`「优先顺序 1. 排查会使测试进程崩溃的 IL 问题」。
数据来源：`TestFiles/InterpreterTests/concurrency_primitives_test.old8` 语料扫描。

## 1. 现象

```old8
locks <- {ReadWriteLockCreate()}
for lock in locks {
    ReadWriteLockDispose(lock)     // -il: 无效 IL
}
```

`ReadWriteLockDispose(value)` 的实参来自 for-in 循环变量（静态类型 `object`），
IL 模式生成 `call ResourceManager.DisposeReadWriteLock(int)` 时没有拆箱，
JIT 报 `Common Language Runtime detected an invalid program`。

## 2. 根因

并发全局函数把「加载实参 + call ResourceManager.Xxx(int)」直接发射到调用方 IL：

```csharp
parameters[0].Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
ilGenerator.Emit(OpCodes.Call, method);   // 形参是 int
```

实参静态类型是 `object`（或语言值 `LangValueType`）时，栈上的值与 `int` 形参不匹配。
`Sleep(object)` 等同样形态的函数也受影响。

## 3. 修复

新增 `ConcurrencyIlHelpers.EmitLoadIntArgument`（`ConcurrencyUtils.cs`）：

- 实参类型 `int` → 原样；
- `object` → `unbox.any int`；
- `LangValueType` → `IlValueBridge.Unwrap` 后 `unbox.any int`；
- 其他数值类型 → `conv.i4`。

七个 `*Dispose` 函数（AtomicInt / Channel / CountDownLatch / CyclicBarrier / Mutex /
ReadWriteLock / Semaphore）改为调用该助手；每个文件的第一处 `int` 参数调用点
（Acquire 等）也顺带接入，其余调用点未批量改动。

涉及文件：`Old8Lang.Runtime/GlobalFunctions/Implementations/Concurrency/` 下的
`ConcurrencyUtils.cs` 与 7 个 `*Functions.cs`。

## 4. 验证

### 4.1 新增回归测试

`Old8Lang.Tests/Compiler/Threading/ConcurrencyIdArgumentIlTests.cs`（3 项）：

- `DisposeReadWriteLock_WithObjectTypedId_CompilesAndExecutes`
- `DisposeMutex_WithObjectTypedId_CompilesAndExecutes`
- `DisposeCountDownLatchAndBarrier_WithObjectTypedIds_CompilesAndExecutes`

修复前均以 `InvalidProgramException` 失败，修复后通过。

### 4.2 语料

`concurrency_primitives_test.old8` 由 invalid program（exit 1）变为正常退出（exit 0）。

### 4.3 回归面

`Compiler.Threading` 组通过（新增 3 项）；其余组见 Todo 记录的整组对比。

## 5. 遗留

并发命名空间里仍有其他手写 `parameters[i].Accept(...)` + `Call` 的函数（约 40 处），
如果形参是 `int` 且实参可能为 `object`/`LangValueType`，同样需要拆箱；
本轮只覆盖了 `*Dispose` 及同文件的 `int` 参数调用点，其余未逐一核对。
