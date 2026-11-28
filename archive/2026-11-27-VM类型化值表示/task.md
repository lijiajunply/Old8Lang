# 执行清单：VM 类型化值表示

- [x] 阶段 0：建立 change 文档（`spec.md` / `design.md` / `plan.md` / `task.md`）
- [x] 阶段 0：采集改造前 Release 性能基线数据
- [x] 阶段 1：在 `Old8Lang.Runtime` 实现 `VmValue` / `VmValueKind`
- [x] 阶段 1：升级 `ConstantPool`（`GetConstantValue`）与 `Instruction`（`IntOperand`）
- [x] 阶段 1：升级 `CallFrame`、`GlobalSlot`、`UpValueCell`、`ClosureEnvironment`
- [x] 阶段 2：在 `Old8Lang.VirtualMachine` 实现 `VmStack`
- [x] 阶段 2：升级 `VirtualMachine.Core`、`Helpers.Arithmetic`、`Helpers.Comparison`、`Helpers.TypeConversion`、`Helpers.Validation`、`Helpers.CallDispatch`、`Instructions.Main`
- [x] 阶段 2：升级其余 VM 指令与生成器/异步/异常/工具链文件
- [x] 阶段 3：验证构建与全量 VM / Architecture / Benchmarks / `.old8` 回归测试
- [x] 阶段 3：新增 `VmValue` 语义、并发与零分配回归测试，更新 `VMPerformanceSmokeTests` 阈值
- [x] 阶段 3：采集改造后性能对比数据，填写 `test.md` 并更新 `changes/Todo.md`
