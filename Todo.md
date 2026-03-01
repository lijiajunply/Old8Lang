# VM 模式性能优化 TODO（基于 2026-03-02 Quick 报告）

## 进度更新（2026-03-02）

- [x] P0-1：修复 `ChannelSend` 在 VM 线程中的异步阻塞异常（已完成，基准不再因该异常直接 NA）。
- [x] P0-2：Quick 报告增加失败场景显式标记和 `FailureReason` 提取（已完成）。
- [x] P1-1（部分）：命名参数绑定热路径优化（参数名索引缓存 + 参数数组填充优化）已完成并通过回归测试。
- [x] P1-1（部分）：参数类型校验热路径优化（基础类型快速判定）已完成并通过回归测试。
- [x] P1-1（剩余）：补充“仅位置参数且无需默认值补全”调度级 fast path（已完成）。
- [x] P1-1（补充）：`CallAsync` 命名参数路径去重，避免重复 `NormalizeArguments/ValidateParameterTypes`（已完成）。
- [x] P1-2（部分）：全局函数调用异常路径避免重复包装（`Old8Exception/VmException` 直通，未知异常再包装）。
- [x] P1-2（部分）：`VmException` 消息延迟构建，降低高抛异常热路径的即时字符串开销。
- [x] P1-2（部分）：按异常指令 IP 缓存候选处理器，避免高抛异常场景重复全表扫描 `ExceptionTable`。

## 结论摘要

- 当前最紧急问题不是“慢”，而是 **`VMXQ_Concurrency_Channel_MPMC_4Workers` 基准失效（NA）**，导致并发通道路径无法评估。
- 真实热点主要集中在：
  - 高参数调用热路径（`VMXQ_Edge_HighArgCount_CallHotPath`: 286.979~368.526 ms）
  - 高异常率路径（`VMXQ_Edge_HighThrowRate_TryCatch`: 36.792~54.469 ms）
  - 高频闭包捕获（`VMXQ_Edge_LargeClosureCapture_HighFreq`: 34.186~39.347 ms）
  - 互斥+原子计数并发（`VMXQ_Concurrency_MutexAtomicCounter_4Workers`: 241.784~252.553 ms）
- 分配量偏高场景：
  - `HighArgCount`: 544.133 bytes/op
  - `LargeClosureCapture`: 520.106 bytes/op
  - `HighThrowRate`: 250.094 bytes/op

## P0（必须先做）

- [x] 修复 `ChannelSend` 在 VM 线程中抛出 `The asynchronous operation has not completed` 的问题。
  - 证据：`BenchmarkDotNet.Artifacts/Old8Lang.Benchmarks.Benchmarks.VM.Suites.VMQuickPerformanceBenchmarks-20260302-001859.log`
  - 当前栈：`VirtualMachine.Helpers.FunctionCall.cs:36` 触发 `ChannelSend` 全局函数异常
  - 代码点：
    - `Old8Lang/Concurrency/ResourceManager.cs`（`SendChannel`）
    - `Old8Lang/GlobalFunctions/Implementations/Concurrency/ChannelFunctions.cs`（`ChannelSendFunction`）
  - 建议修复：避免直接对未完成 `ValueTask` 调 `GetAwaiter().GetResult()`；改为同步可等待路径（例如 `AsTask().GetAwaiter().GetResult()` 或 `WaitToWriteAsync + TryWrite`）。
  - 验收：`VMXQ_Concurrency_Channel_MPMC_4Workers` 不再为 `NA`，两套 Job 都产生有效 `Mean/StdDev/Allocated`。

- [x] 给 Quick 报告增加“失败场景显式标记”与“失败原因提取”。
  - 代码点：`Old8Lang.Benchmarks/Benchmarks/VM/Reports/VMPerformanceReport.cs`
  - 问题：当前只显示 `N/A`，没有把失败原因（异常摘要）带入报告，不利于 CI 快速定位。
  - 验收：报告中对 `NA` 场景新增 `FailureReason`（至少包含异常类型+关键消息）。

## P1（高收益优化）

- [ ] 优化高参数函数调用路径（减少每次调用的参数整理/查找成本）。
  - 代码点：
    - `Old8Lang/Bytecode/VM/Helpers/VirtualMachine.Helpers.CallDispatch.cs`
    - `Old8Lang/Bytecode/VM/Helpers/VirtualMachine.Helpers.FunctionCall.cs`（`ArrangeArgumentsWithNamed`）
  - 现状：命名参数通过 `function.Parameters.IndexOf(paramName)` 线性查找；高频调用中会放大开销。
  - 建议：
    - 在 `FunctionMetadata` 预构建 `参数名 -> 索引` 映射缓存。
    - 为“仅位置参数且无默认参数补全需求”走 fast path，绕过命名参数拼装逻辑。
  - 验收：`VMXQ_Edge_HighArgCount_CallHotPath` 平均耗时下降 >= 10%，alloc/op 下降 >= 15%。

- [ ] 优化高异常率 Try/Catch 路径，减少异常对象构造和包装层级。
  - 代码点：
    - `Old8Lang/Bytecode/VM/Instructions/VirtualMachine.Instructions.Exception.cs`
    - `Old8Lang/Bytecode/VM/Helpers/VirtualMachine.Helpers.FunctionCall.cs`（当前会把内部异常统一包装成 `InvalidOperationError`）
  - 问题：高抛异常场景里，频繁包装异常会带来显著分配和栈开销。
  - 建议：
    - 对受控 VM 异常路径采用轻量错误对象/错误码通路（仅在跨边界时构建重异常）。
    - 避免重复包装已是 `Old8Lang.Error.*` 的异常。
  - 验收：`VMXQ_Edge_HighThrowRate_TryCatch` 平均耗时下降 >= 15%，alloc/op 下降 >= 20%。

- [ ] 优化闭包调用路径，降低捕获环境访问和对象分配。
  - 代码点：
    - `Old8Lang/Bytecode/VM/VirtualMachine.Core.cs`（`CallClosureFunction`）
    - `Old8Lang/Bytecode/Closures/ClosureValue.cs`
  - 建议：
    - 闭包捕获变量按索引化存储（数组或紧凑结构）替代 `Dictionary<string, object?>` 热路径访问。
    - 对高频闭包函数增加“无变更捕获环境复用”策略。
  - 验收：`VMXQ_Edge_LargeClosureCapture_HighFreq` alloc/op 下降 >= 20%，耗时下降 >= 10%。

- [ ] 并发互斥计数路径减少资源管理层字典查找频率。
  - 代码点：
    - `Old8Lang/Concurrency/ResourceManager.cs`
    - `Old8Lang/Bytecode/VM/Instructions/VirtualMachine.Instructions.Concurrency.cs`
  - 问题：每次 `MutexLock/Unlock`、`AtomicIntIncrement` 都经过 `ConcurrentDictionary + wrapper.UpdateLastAccessTime()`，在高频循环下开销明显。
  - 建议：
    - 在 VM 执行期缓存热点资源句柄（例如 frame 级别缓存）减少重复查表。
    - 对高频原子操作提供轻量 fast path（保证语义一致）。
  - 验收：`VMXQ_Concurrency_MutexAtomicCounter_4Workers` 耗时下降 >= 10%，alloc/op 下降 >= 15%。

## P2（基准与报告质量）

- [ ] 报告去重策略：按 `Scenario + Job` 保留明细，并输出聚合视图。
  - 代码点：`Old8Lang.Benchmarks/Benchmarks/VM/Reports/VMPerformanceReport.cs`
  - 问题：当前同一场景在不同 Job 下重复出现，阅读和趋势判断成本高。
  - 建议：
    - Markdown 展示两层：`Per-Job 明细` + `Scenario 聚合（median 或加权均值）`。
    - JSON 增加 `Job` 维度，避免后续分析丢信息。
  - 验收：报告中同一场景不再“看起来重复”，而是结构化展示差异。

- [ ] 建立 Quick 基线文件并启用回归门禁。
  - 代码点：`Reports/Baselines/` + `VMPerformanceReport.cs` 基线加载逻辑
  - 问题：当前 `BaselineJson: N/A`，`Status` 全是 `N/A`，无法自动回归判定。
  - 验收：下一次 Quick 报告出现 `PASS/WARN/FAIL`，并在 CI 中可阻断 `FAIL`。

- [ ] 为关键场景添加性能单测阈值（非 BDN，快速 smoke）。
  - 建议测试：
    - Channel MPMC 基本可运行（不抛异常）
    - HighArgCount/HighThrowRate/LargeClosure 三场景的最大耗时与分配上限
  - 目标：在常规 `dotnet test` 阶段先挡住明显退化，再由 BenchmarkDotNet 做精确评估。

## 建议执行顺序

1. 先修 P0（让通道并发基准可测 + 报告可诊断）
2. 再做 P1（高收益热路径优化）
3. 最后做 P2（报告/门禁体系完善）

## 本次分析输入

- `Reports/VM_Quick_Performance_Report_20260302_002147.md`
- `Reports/VM_Quick_Performance_Report_20260302_002147.json`
- `BenchmarkDotNet.Artifacts/results/Old8Lang.Benchmarks.Benchmarks.VM.Suites.VMQuickPerformanceBenchmarks-report.csv`
- `BenchmarkDotNet.Artifacts/Old8Lang.Benchmarks.Benchmarks.VM.Suites.VMQuickPerformanceBenchmarks-20260302-001859.log`
