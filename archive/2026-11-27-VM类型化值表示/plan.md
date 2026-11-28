# 实施计划：VM 类型化值表示

## 阶段划分与依赖

1. **阶段 0：基线采集与文档建档**
   - 建立 `changes/2026-11-27-VM类型化值表示/` 下 `spec.md`、`design.md`、`plan.md`、`task.md`。
   - 采集改造前 Release 性能基线（1M 局部/全局循环、`HighArgCount`、`HighThrowRate`、`LargeClosureCapture`、`--vm-diagnose`）。
   - 确认基线测试通过情况（VM 1141 项全通过，Architecture 72/73 与 Todo 一致）。

2. **阶段 1：Runtime 核心类型化存储层**
   - 新增 `Old8Lang.Runtime/Bytecode/Core/VmValue.cs`（`VmValueKind`、`VmValue`）。
   - 更新 `ConstantPool.cs`（并行 `_vmConstants` 与 `GetConstantValue`）。
   - 更新 `Instruction.cs`（`IntOperand` 自动同步缓存）。
   - 更新 `CallFrame.cs`（`VmValue[] Locals`）。
   - 更新 `GlobalSlot.cs`（`GlobalTableState` + `VmValue` 存储 + 并发无撕裂保护）。
   - 更新 `UpValueCell.cs` 与 `ClosureEnvironment.cs`（`VmValue` 存储、`Box(ref VmValue)`、`IsEmpty`、`TryGetVmValue`/`TrySetVmValue`）。

3. **阶段 2：VirtualMachine 栈与全指令迁移**
   - 新增 `Old8Lang.VirtualMachine/VM/VmStack.cs`。
   - 更新 `VirtualMachine.Core.cs`（`_threadStack` 切换为 `VmStack`、池化 `ArrayPool<VmValue>`、内联指令循环全面类型化、无返回值复用 `VoidLangValue.Instance`）。
   - 更新算术/比较/逻辑/类型校验/值转换辅助（`Helpers.Arithmetic`、`Helpers.Comparison`、`Helpers.TypeConversion`、`Helpers.Validation`、`Instructions.Main`）。
   - 更新调用分派（`Helpers.CallDispatch`：位置参数快速路径与 `CallDynamic` 免数组快速路径）。
   - 更新其余全部指令文件（`Stack`、`Arithmetic`、`Comparison`、`Logical`、`ControlFlow`、`Container`、`Iterator`、`Type`、`Concurrency`、`Thread`、`Async`、`Exception`、`Module`、`Defer`、`Helpers.StaticClasses`、`VirtualMachine.Async`、`VirtualMachine.Generators`、`VirtualMachine.Exceptions`、`VirtualMachine.Tooling`）。
   - 移除不再需要的 `VmBoxed.cs`（若已完全被 `VmValue` 替代）。

4. **阶段 3：回归验证、测试补齐与性能对比**
   - 跑全量 `Old8Lang.Tests.VirtualMachine`、`Old8Lang.Tests.Architecture`、`Old8Lang.Benchmarks` 单元测试与 `TestFiles/run_vm_tests.sh`。
   - 新增 `VMValueRepresentationTests.cs`：覆盖 `VmValue` 各类型边界（`int` 溢出 `long`、`double`/`int` 混合运算与相等、`char`/`bool`/`null`、闭包共享单元修改、`RefreshLocalBinding`、并发 worker 读写 `GlobalSlot` 无撕裂、1M 循环零分配断言）。
   - 采集改造后 Release 性能数据并对比改造前基线。
   - 按实测结果收紧 `VMPerformanceSmokeTests` 分配阈值。
   - 写入 `test.md` 并更新 `changes/Todo.md`。

## 回滚策略

- 所有改动严格限制在 `Old8Lang.Runtime/Bytecode/`、`Old8Lang.VirtualMachine/` 与对应测试文件内，不影响语法解析、解释器、IL 编译器或 `.o8c` 二进制格式，必要时可通过 git 按阶段独立回退。
