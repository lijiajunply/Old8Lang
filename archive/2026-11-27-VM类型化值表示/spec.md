# 需求规格：VM 类型化值表示（消除 `object` 装箱）

## 1. 背景与问题

来源：`changes/Todo.md`「VM 性能」条目及历史 P4 报告（`2026-10-24-11-25-P4-VM性能后续.md` §4）。

当前虚拟机（`Old8Lang.VirtualMachine` / `Old8Lang.Runtime/Bytecode`）的值表示全部依赖 `object?`：

- 求值栈为 `Stack<object?>`
- 局部变量槽为 `CallFrame.Locals : object?[]`
- 全局变量槽为 `GlobalSlot.Value : object?`
- 闭包共享单元为 `UpValueCell.Value : object?`
- 常量池 `ConstantPool.GetConstant(int)` 返回装箱的 `object`
- 指令整数操作数每次执行需对 `object? Operand` 执行 `(int)` 拆箱

导致在热循环中（例如 `sum <- sum + 1`、`fold8` 多参调用、高频闭包调用、高频 `try/catch`），`int`/`long`/`double`/`bool`/`char` 等基础类型在每次算术运算、变量读写、函数传参和返回时都反复在托管堆上装箱分配：

- P4 实测 `sum <- sum + 1`（1M 次迭代）仍分配 **45.78 B/次**（45.78 MB）。
- `HighArgCount_CallHotPath`（30k 次 8 参调用）约分配 **7.2 MB**。
- `LargeClosureCapture_HighFreq`（50k 次 8 捕获闭包调用）约分配 **14.8 MB**。
- `HighThrowRate_TryCatch`（40k 次迭代 / 10k 次抛异常）约分配 **5.0 MB**。

## 2. 目标与范围

按 P4 §4.3 的三阶段路线一次落地完整闭环：

1. **阶段 1（类型化值表示 + 类型化栈 + 类型化局部变量 + 常量池/操作数热路径）**：
   - 在 `Old8Lang.Runtime/Bytecode/Core/` 引入 `readonly struct VmValue`（`VmValueKind` 标记 + 64 位内联标量载荷 + `object?` 引用载荷），原生承载 `Null` / `Int` / `Long` / `Double` / `Bool` / `Char` / `Object` / `UpValue`。
   - 在 `Old8Lang.VirtualMachine/VM/` 引入 `VmStack` 替代 `Stack<object?>`。
   - `CallFrame.Locals` 升级为 `VmValue[]`（继续走 `ArrayPool<VmValue>.Shared` 池化复用）。
   - `ConstantPool` 并行维护类型化常量表 `GetConstantValue(int) -> VmValue`；`Instruction` 同步维护 `IntOperand` 免拆箱读取。
   - 算术运算（`Add`/`Sub`/`Mul`/`Div`/`Mod`/`Pow`/`Neg`）、比较运算（`Equal`/`NotEqual`/`Less`/`LessEqual`/`Greater`/`GreaterEqual`）、逻辑运算与条件跳转（`ToBool`/`And`/`Or`/`Not`/`JumpIfFalse`/`JumpIfTrue`）、类型校验（`CheckTypeMatch`/`FastTypeMatches`）、位置参数快速调用路径全面类型化。
2. **阶段 2（全局槽位 + 闭包共享单元 + 闭包环境类型化）**：
   - `GlobalSlot` 底层存储改为 `VmValue`，单线程路径零锁零装箱，创建 worker VM 并发执行时自动切换为无撕裂同步读写。
   - `UpValueCell` 底层存储改为 `VmValue`，新增 `UpValueCell.Box(ref VmValue slot)`，闭包读写捕获的基础类型变量零装箱。
   - `ClosureEnvironment` 补齐 `VmValue` 原生读写方法与 `IsEmpty` 快速判定；无捕获变量的闭包（顶层 lambda）可直接复用 `Instruction.CachedGlobalSlot`。
3. **阶段 3（边界收敛与热点优化 + 测试/门禁更新）**：
   - `CallDynamic` 对纯位置调用的闭包/函数增加免 `object?[]` 分配的快速路径。
   - 保持外部/冷路径边界（原生函数、容器、类字段、生成器状态、调试器/DAP、公共 API `GetGlobalVariable` / `ExecuteFunction` 等）通过 `VmValue.ToObject()` / `VmValue.FromObject()` 无损互通。
   - 补充类型化值表示、UpValue/GlobalSlot 并发与零分配回归测试，按实测显著改善更新 `VMPerformanceSmokeTests` 分配阈值。

## 3. 明确不做

- 不改变 `.o8c` 字节码二进制序列化格式（保持向后兼容）。
- 不修改解释器后端（`Old8Lang.Interpreter`）与 IL 编译器后端（`Old8Lang.Compiler`）。
- 不改变 Old8Lang 语言语义（整数溢出升级 `long`、`int == double` 数值相等、`bool` 严格逻辑操作数校验、异常类型与错误消息、闭包共享写回、循环变量 `RefreshLocalBinding` 等全部保持一致）。
- 不强行将外部容器（`List<object?>` / `object?[]` / `Dictionary<object, object?>`）与标准库互操作签名为 `VmValue`（维持 `object?` 边界，避免侵入全仓库运行时值契约）。

## 4. 验收标准

1. **正确性**：
   - `dotnet build Old8Lang.sln` 0 错误。
   - `Old8Lang.Tests.VirtualMachine` 全部用例通过（0 失败）。
   - `Old8Lang.Tests.Architecture` 程序集边界测试与既有基线一致（不引入任何新增失败，Runtime 不反向引用任何后端）。
   - `Old8Lang.Benchmarks` 单元测试全部通过。
   - `TestFiles/run_vm_tests.sh` 既有可通过用例全通过。
2. **性能与分配**：
   - `sum <- sum + 1`（1M 次迭代，局部变量循环与全局变量循环）每轮堆分配降至 **0 B/次**（总分配从 ~45.78 MB 降至接近 0 KB）。
   - `HighArgCount_CallHotPath`、`HighThrowRate_TryCatch`、`LargeClosureCapture_HighFreq` 堆分配量显著下降（目标降幅 > 90%），执行时间同向改善或持平无退化。
3. **文档留痕**：
   - 完成 `changes/2026-11-27-VM类型化值表示/test.md` 测试与对比报告。
   - 更新 `changes/Todo.md`「VM 性能」条目状态。
