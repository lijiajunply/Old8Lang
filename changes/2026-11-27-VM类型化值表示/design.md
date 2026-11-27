# 架构设计：VM 类型化值表示（`VmValue` / `VmStack`）

## 1. 程序集归属与边界

遵循 `Docs/DEVELOPMENT_WORKFLOW.md §4.1` 约束：

| 组件 | 程序集 | 说明 |
|------|--------|------|
| `VmValue` / `VmValueKind` | `Old8Lang.Runtime/Bytecode/Core/VmValue.cs` | 字节码执行模型共享值类型，被 `CallFrame`、`GlobalSlot`、`UpValueCell`、`ClosureEnvironment`、`ConstantPool` 使用 |
| `CallFrame` | `Old8Lang.Runtime/Bytecode/Core/CallFrame.cs` | `Locals` 改为 `VmValue[]` |
| `GlobalSlot` | `Old8Lang.Runtime/Bytecode/Core/GlobalSlot.cs` | 槽位存储 `VmValue`，含单线程零锁与多线程无撕裂切换 |
| `UpValueCell` / `ClosureEnvironment` | `Old8Lang.Runtime/Bytecode/Closures/` | 共享单元存储 `VmValue`，环境提供 `VmValue` 读写与 `IsEmpty` |
| `ConstantPool` / `Instruction` | `Old8Lang.Runtime/Bytecode/` | 平行类型化常量表 `GetConstantValue` 与 `Instruction.IntOperand` |
| `VmStack` | `Old8Lang.VirtualMachine/VM/VmStack.cs` | VM 私有类型化求值栈，替代 `Stack<object?>` |
| `VirtualMachine.*` | `Old8Lang.VirtualMachine/VM/` | 内联热路径指令、算术/比较/逻辑/调用分派全部直接操作 `VmValue` |

`Old8Lang.Runtime` 仍不引用任何后端程序集，不破坏架构测试。

## 2. `VmValue` 内存布局与设计取舍

### 2.1 选定方案：24 字节显式标签 `readonly struct`

```csharp
public enum VmValueKind : byte
{
    Null = 0,
    Int = 1,
    Long = 2,
    Double = 3,
    Bool = 4,
    Char = 5,
    Object = 6,
    UpValue = 7
}

public readonly struct VmValue
{
    public readonly VmValueKind Kind;
    internal readonly long RawBits;
    internal readonly object? ObjectRef;
}
```

### 2.2 备选方案对比

| 方案 | 大小 | 优点 | 缺点 | 结论 |
|------|------|------|------|------|
| **方案 A（选定）：显式 `VmValueKind` + `long RawBits` + `object? ObjectRef`** | 24 B | `default(VmValue)` 即 `Null`；`ObjectRef` 对所有基础类型为 `null`，GC 扫描与卡表最友好；`Kind` 判定为单字节比较/跳转表；`BitConverter.DoubleToInt64Bits` 为 JIT 零开销指令 | 比 16 B 多 8 字节 | **采用** |
| 方案 B：哨兵对象编码标签（`_obj == IntSentinel`） | 16 B | 体积更小 | 每次创建基础类型都要加载静态字段并写入非空 GC 引用；`switch` 无法编译为跳转表；基础类型数组槽位全部非空增加 GC 卡表扫描 | 放弃 |
| 方案 C：NaN-boxing（单 `ulong` + 外部句柄表） | 8 B | C/C++ 最优 | .NET 托管引用无法直接塞进 `ulong`，句柄表引入额外间接与 GC 固定成本 | 放弃 |

### 2.3 关键不变式

1. `Kind == Null` 时 `RawBits == 0 && ObjectRef == null`（即 `default(VmValue)`）。
2. `Kind == Object` 时 `ObjectRef != null`（`FromObject(null)` 返回 `VmValue.Null`）。
3. `Kind == UpValue` 仅允许存在于 `CallFrame.Locals` 中，`ObjectRef` 恒为非空 `UpValueCell`，绝不直接暴露到求值栈；`LoadLocal` 仅做 `Kind == VmValueKind.UpValue` 一字节判定即可解引用。
4. `ToObject()` 对 `Bool` 继续复用共享静态装箱实例（`BoxedTrue`/`BoxedFalse`），避免跨边界布尔装箱。
5. `HasReference => Kind >= VmValueKind.Object`：弹栈与归还池时，基础类型无需清写引用槽（`Pop` 仅在 `HasReference` 时清空栈槽避免 GC 根滞留）。

## 3. `VmStack` 设计

替代 `Stack<object?>`，内部使用 `VmValue[] _items` + `int _count`（默认初始容量 64，倍增扩容）：

- **热路径方法（`AggressiveInlining`）**：`Push(VmValue)`、`PushInt`、`PushLong`、`PushDouble`、`PushBool`、`PushChar`、`PushNull`、`PushRef(object)`、`Pop() -> VmValue`、`PopDiscard()`、`Peek() -> VmValue`、`Dup()`、`Swap()`。
- **边界适配方法**：`PushObject(object?)`、`PopObject() -> object?`、`PeekObject() -> object?`、`ToObjectArray()`、`CopyTo(Span<VmValue>)`。
- **异常语义兼容**：空栈 `Pop`/`Peek` 抛出 `new InvalidOperationException("Stack empty.")`，与 `VirtualMachine.Core.cs` 既有的求值栈下溢诊断完全兼容。

## 4. `GlobalSlot` 并发无撕裂设计

`VmValue` 为 24 字节结构体，64 位 CLR 下跨线程并发读写不具备结构体级原子性，可能产生字段撕裂（例如 `Kind == Object` 配旧 `ObjectRef == null`）。

解决方案：为每个 VM 全局环境配一个轻量的 `GlobalTableState`（单实例共享给主 VM 与所有 worker VM）：

```csharp
internal sealed class GlobalTableState
{
    internal volatile bool IsConcurrent;
}
```

- **单线程热路径（`!_state.IsConcurrent`）**：`GlobalSlot.GetValue()` / `SetValue(VmValue)` 直接读写 `_value` 字段，零锁、零内存屏障、零装箱。
- **并发切换点**：`CreateWorkerVirtualMachine()` 在创建 worker 之前调用 `_globalsState.IsConcurrent = true`（以及 `Thread.Start()` / `Task.Run` 带来的 release/acquire 屏障）。
- **并发路径（`_state.IsConcurrent`）**：每个 `GlobalSlot` 使用 `System.Threading.Lock` 保护 `_value` 的读写（`Lock` 延迟按需或在构造时无额外锁开销；.NET 10 `System.Threading.Lock` 轻量且无 `Monitor` 同步块膨胀），保证 100% 无撕裂且零装箱。

## 5. `CallFrame`、`UpValueCell`、`ClosureEnvironment` 与调用分派

1. **`CallFrame`**：
   - `Locals` 类型改为 `VmValue[]`，通过 `ArrayPool<VmValue>.Shared` 租借与归还；归还时 `Array.Clear(frame.Locals, 0, frame.LocalCount)` 清零全部 `VmValue`（将 `Kind` 置 `Null` 且释放所有 `ObjectRef`）。
   - `LoadLocal` / `StoreLocal` / `RefreshLocalBinding` 直接操作 `VmValue`，仅当 `Kind == VmValueKind.UpValue` 时访问 `UpValueCell`。
2. **`UpValueCell`**：
   - 内部字段改为 `VmValue _value`；提供 `VmValue Value { get; set; }` 与 `object? ObjectValue` 兼容访问，以及 `UpValueCell.Box(ref VmValue slot)`。
   - 保留 `UpValueCell(object? value)` 与 `UpValueCell(VmValue value)` 构造重载，兼容既有测试与外部调用。
3. **`ClosureEnvironment`**：
   - 新增 `bool IsEmpty => _capturedNames.Length == 0 && Parent == null`。
   - 新增 `VmValue GetLocalVmValue(int index)`、`bool TryGetVmValue(string variableName, out VmValue value)`、`bool TrySetVmValue(string variableName, VmValue value)`，零装箱读写 `UpValueCell.Value`。
   - `LoadGlobal` / `StoreGlobal` 当 `frame.ClosureEnvironment is null or { IsEmpty: true }` 时直接走 `instruction.CachedGlobalSlot`，消除顶层 lambda 访问全局变量的字典查找与装箱！
4. **函数与闭包调用热路径**：
   - `TryInvokeResolvedFunctionPositionalFastFromStack`：直接从 `VmStack` 弹出 `VmValue` 写入 `VmValue[] fastFrame.Locals`，用 `FastTypeMatches(fastTypeKind, in VmValue)` 做零装箱类型校验。
   - `ExecuteCallDynamicInstruction`：当目标为 `ClosureValue` 或 `FunctionMetadata` 且满足 `CanSkipNormalizeForPositionalCall` 且非生成器时，同样直接从 `VmStack` 弹参数进 `fastFrame.Locals`（避免每次调用分配 `new object?[argCount]` 并避免装箱）。
   - `ExecuteFrame` 补无返回值占位时直接 `_stack.PushRef(VoidLangValue.Instance)`（复用单例，不再每次 `new VoidLangValue()`！）。

## 6. 算术、比较、逻辑、类型校验热路径

- `Add(VmValue a, VmValue b) -> VmValue`（及 `Sub`/`Mul`/`Div`/`Mod`/`Pow`/`Neg`）：
  - 首位 fast path：`if (a.Kind == VmValueKind.Int && b.Kind == VmValueKind.Int)` 直接执行 32/64 位整型运算返回 `VmValue.FromInt(...)` 或溢出升级 `VmValue.FromLong(...)`。
  - 次位 fast path：`Double`/`Long` 组合直接返回 `VmValue.FromDouble(...)` / `VmValue.FromLong(...)`。
  - 其余分支（`Object` 运算符重载、字符串拼接、数组/列表拼接、`LangValueType`）落入冷路径 `AddSlow(VmValue a, VmValue b)`，保证热路径方法体短小易于 JIT 内联！
- `Equals` / `Less` / `LessEqual` / `Greater` / `GreaterEqual` 同样拆为 `VmValue` 热路径 + `Slow` 冷路径，比较结果直接 `_stack.PushBool(...)`（零装箱）。
- `ToBool(VmValue)` 直接按 `Kind` 判断 `Bool`/`Int`/`Double`/`Null`/`Object`，消除 `JumpIfFalse`/`JumpIfTrue` 的类型测试。
- `CheckTypeMatch(string, VmValue)` 与 `FastTypeMatches(FastParameterTypeKind, VmValue)` 直接按 `Kind` 判定基础类型，零装箱完成函数参数与返回值校验。
- `ConstantPool` 平行维护 `List<VmValue> _vmConstants`，`GetConstantValue(int index)` 直接索引返回 `VmValue`；`Instruction` 维护 `internal int IntOperand` 消除 `(int)instruction.Operand!` 拆箱。
