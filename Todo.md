# VM 模式性能优化 TODO（基于 2026-03-02 Quick 报告）

## 进度更新（2026-10-10，P3：VM 性能与基准门禁）

详见 `Reports/2026-10-10-22-33-P3-VM性能与基准门禁.md`。

- [x] 重跑 Quick 基准（macOS arm64）：`Reports/VM_Quick_Performance_Report_20261010_221552.{md,json}`（优化前）、
  `Reports/VM_Quick_Performance_Report_20261010_223112.{md,json}`（优化后）。
- [x] 20260425 报告里 4 个待收敛场景在本机**全部 PASS**，判定为跨机器伪回归：
  Channel_MPMC -30.96%、MutexAtomic +1.45%、LargeFile_10k -60.27%、LargeFile_50k -58.79%。
- [x] P1 高参数调用：同机中位数 37.877 → 10.867 ms（**-71.31%**，目标 ≥10% 达成）；
  alloc/op 相对 20260302 基线 344.13 → 240.15 B（**-30.2%**，目标 ≥15% 达成）。
- [x] P1 高异常率 try/catch：同机中位数 9.932 → 5.501 ms（**-44.39%**，目标 ≥15% 达成）；
  alloc/op 252.09 → 126.11 B（**-50.0%**，目标 ≥20% 达成）。
- [x] P1 闭包调用：同机中位数 22.979 → 15.842 ms（**-31.06%**，目标 ≥10% 达成）；
  alloc/op 400.14 → 296.09 B（**-26.0%**，目标 ≥20% 达成）。
- [x] 优化手段：解释循环内联极热指令 + 求值栈引用提到循环外（`_stack` 是 ThreadLocal 属性）；
  `CheckTypeMatch` 基础类型零分配快速路径；`Add/Sub/Mul`、`Equals/Less/Greater` 整数快路径前置。
- [x] P2 Quick 基线门禁：`--vm-report-quick` 出现 `FAIL` 时返回非零退出码（与 Nightly 一致）；
  CI 增加显式门禁步骤 + 按 `runner.os/arch` 缓存的基线 + `OLD8LANG_VM_REPORT_BASELINE` 固定基线；
  基线解析改为按文件名时间戳取最新（原先按 mtime，检出后 mtime 相同会导致基线漂移）。
- [x] P2 关键场景性能单测阈值：`VMPerformanceSmokeTests` 扩展为 4 个粗阈值用例（耗时 + 分配 + 结果正确性
  + Channel MPMC 不丢消息），新增 `Category=PerformanceSmoke` 并纳入默认测试档。
- [x] 回归验证：VM 用例 1033/1033、性能 smoke 4/4、报告/门禁单测 11/11 通过；默认档全量与
  `TestResults/fail-inventory.trx` 逐用例 diff，0 个基线失败消失、仅 3 个既有已知问题。

## 进度更新（2026-03-02）

- [x] P0-1：修复 `ChannelSend` 在 VM 线程中的异步阻塞异常（已完成，基准不再因该异常直接 NA）。
- [x] P0-2：Quick 报告增加失败场景显式标记和 `FailureReason` 提取（已完成）。
- [x] P0-3：修复 `TryReceiveChannel(timeout)` 超时后遗留挂起读取任务导致的通道消息丢失（已完成）。
- [x] P1-1（部分）：命名参数绑定热路径优化（参数名索引缓存 + 参数数组填充优化）已完成并通过回归测试。
- [x] P1-1（部分）：参数类型校验热路径优化（基础类型快速判定）已完成并通过回归测试。
- [x] P1-1（剩余）：补充“仅位置参数且无需默认值补全”调度级 fast path（已完成）。
- [x] P1-1（补充）：`CallAsync` 命名参数路径去重，避免重复 `NormalizeArguments/ValidateParameterTypes`（已完成）。
- [x] P1-2（部分）：全局函数调用异常路径避免重复包装（`Old8Exception/VmException` 直通，未知异常再包装）。
- [x] P1-2（部分）：`VmException` 消息延迟构建，降低高抛异常热路径的即时字符串开销。
- [x] P1-2（部分）：按异常指令 IP 缓存候选处理器，避免高抛异常场景重复全表扫描 `ExceptionTable`。
- [x] P1-4（完成）：并发资源热路径优化（`Mutex` 轻量实现 + 资源访问时间更新节流）。
- [x] P1-3（补充）：`CallFrame` 的 `DeferStack` 改为惰性分配，减少无 `defer` 函数调用下的固定对象分配。
- [x] 基准复测：已生成新 Quick 报告 `Reports/VM_Quick_Performance_Report_20260302_012717.{md,json}`，用于后续闭包/调用路径优化对比。
- [x] P1-3（部分）：函数调用 `Locals` 数组改为 `ArrayPool<object?>` 复用（`CallFunction/CallClosureFunction` + `ExecuteFrame` 归还）。
- [x] 基准复测：已生成新 Quick 报告 `Reports/VM_Quick_Performance_Report_20260302_013755.{md,json}`。
- [x] P1-3（部分）：`MakeClosure` 改为链式闭包环境（`ClosureEnvironment.Parent`），移除创建子闭包时对父环境的 `SnapshotToDictionary()` 全量拷贝；无捕获场景复用 `ClosureEnvironment.Empty`。
- [x] 回归验证：`VMLambdaExpressionTests`（15/15）与 `VMMemoryUsageTests`（26/26）通过。
- [x] 基准复测：已生成新 Quick 报告 `Reports/VM_Quick_Performance_Report_20260302_021345.{md,json}`。
- [x] P2-1：报告去重改造完成（Markdown 增加 `Per-Job Details` + `Scenario Aggregate (Median)` 双视图，Tiered JSON 保留 `Job` 维度并新增 `AggregatedScenarios`）。
- [x] 回归验证：`dotnet test Old8Lang.Benchmarks/Old8Lang.Benchmarks.csproj --filter "FullyQualifiedName~VMPerformanceReportTests"` 通过（7/7）。
- [x] P1-3（部分）：闭包环境读路径优化（`ClosureEnvironment` 对较大捕获集使用 `FrozenDictionary`，并为本地查找添加内联 fast path），减少高频闭包变量读取开销。
- [x] 回归验证：`VMLambdaExpressionTests`（15/15）与 `VMMemoryUsageTests`（26/26）通过（串行执行，避免并发构建文件锁冲突）。
- [x] P1-1（补充）：位置参数调用 fast path 预计算缓存（`FunctionMetadata.TryGetPositionalFastCallTypeKinds`）+ 调度路径去重，移除每次调用重复的可用性判定循环。
- [x] 回归验证：`Old8Lang.Tests.VirtualMachine.Functions.VM`（25/25）与 `VMLambdaExpressionTests`（15/15）通过。
- [x] 基准复测：已生成新 Quick 报告 `Reports/VM_Quick_Performance_Report_20260302_024032.{md,json}`。
- [x] 基准观察：`20260302_024032` 中 `VMXQ_Edge_HighThrowRate_TryCatch` 聚合状态为 `FAIL`（中位数约 `+7.52%`）。
  已于 2026-10-10 定案：该 `FAIL` 是跨机器/噪声造成的伪回归——本机重跑后该场景 `PASS`，
  且优化后中位数再降 **-44.39%**（见 `Reports/2026-10-10-22-33-P3-VM性能与基准门禁.md`）。

## 结论摘要

- `VMXQ_Concurrency_Channel_MPMC_4Workers` 的 **NA/偶发错误已修复**（`99999/100000` 丢消息问题），2026-03-02 01:08 的 quick 报告两套 Job 均为有效 `PASS`。
- 真实热点主要集中在：
  - 互斥+原子计数并发（`VMXQ_Concurrency_MutexAtomicCounter_4Workers`: 128.296~146.315 ms，已从高风险区间显著下降）
  - 50k 大文件编译执行（`VMXQ_LargeFile_CompileAndExecute_50k_Generated`: 41.976~43.733 ms）
  - 高频闭包捕获（`VMXQ_Edge_LargeClosureCapture_HighFreq`: 31.343~31.413 ms，`PASS` 但 alloc 偏高）
- 分配量偏高场景：
  - `LargeClosureCapture`: 19,535.9 KB/op（`20260302_013755`，较 `20260302_012717` 的 `25,395.3 KB/op` 下降约 **23.1%**）
  - `Channel_MPMC`: 28,896.3~29,788.4 KB/op（`20260302_013755`）
  - `MutexAtomicCounter`: 60,719.5~60,722.9 KB/op（`20260302_013755`）

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

- [x] 修复 `TryReceiveChannel(timeout)` 在超时场景下可能“吞消息”的并发正确性问题。
  - 代码点：`Old8Lang/Concurrency/ResourceManager.cs`（`TryReceiveChannel`）
  - 根因：旧实现对 `ReadAsync().AsTask().Wait(timeout)` 超时后不取消挂起读取，后续读取可能被遗留任务抢走，导致消费方统计缺失。
  - 修复：改为 `TryRead` fast path + `WaitToReadAsync(cts.Token)` + `TryRead`，超时通过 `OperationCanceledException` 明确返回失败。
  - 验收：
    - `dotnet test Old8Lang.Tests --filter "FullyQualifiedName~VMConcurrencyChannelTests"` 通过（14/14）。
    - `dotnet test Old8Lang.Tests --filter "FullyQualifiedName~VMConcurrencyPerformanceTests"` 通过（3/3）。
    - `VMXQ_Concurrency_Channel_MPMC_4Workers` 在 `20260302_010825` 报告中两套 Job 均为 `PASS`（78.191 ms / 73.765 ms）。

## P1（高收益优化）

- [x] 优化高参数函数调用路径（减少每次调用的参数整理/查找成本）。
  - 代码点：
    - `Old8Lang/Bytecode/VM/Helpers/VirtualMachine.Helpers.CallDispatch.cs`
    - `Old8Lang/Bytecode/VM/Helpers/VirtualMachine.Helpers.FunctionCall.cs`（`ArrangeArgumentsWithNamed`）
  - 现状：命名参数通过 `function.Parameters.IndexOf(paramName)` 线性查找；高频调用中会放大开销。
  - 建议：
    - 在 `FunctionMetadata` 预构建 `参数名 -> 索引` 映射缓存。
    - 为“仅位置参数且无默认参数补全需求”走 fast path，绕过命名参数拼装逻辑。
  - 当前进展（`20260302_024032`，基线 `20260302_023424`）：
    - Job-EATLBP：`40.689 -> 40.049 ms`（约 **-1.57%**，`PASS`）
    - Job-LGHQEI：`45.983 -> 41.023 ms`（约 **-10.79%**，`PASS`）
    - 聚合中位数：约 **-6.18%**（`PASS`，但尚未达到 TODO 目标 `>=10%`）
  - 最终进展（`20261010_223112`，同机基线 `20261010_221552`）：
    - 聚合中位数：`37.877 -> 10.867 ms`（**-71.31%**，达标）
    - alloc/op：`344.13 -> 240.15 B/op`（相对 `20260302_024032` 基线 **-30.2%**，达标）
    - 主要来源：解释循环内联极热指令 + 求值栈引用提到循环外 + `Add` 整数快路径 + `CheckTypeMatch` 零分配。
  - 验收：`VMXQ_Edge_HighArgCount_CallHotPath` 平均耗时下降 >= 10%，alloc/op 下降 >= 15%。**（已达成）**

- [x] 优化高异常率 Try/Catch 路径，减少异常对象构造和包装层级。
  - 代码点：
    - `Old8Lang/Bytecode/VM/Instructions/VirtualMachine.Instructions.Exception.cs`
    - `Old8Lang/Bytecode/VM/Helpers/VirtualMachine.Helpers.FunctionCall.cs`（当前会把内部异常统一包装成 `InvalidOperationError`）
  - 问题：高抛异常场景里，频繁包装异常会带来显著分配和栈开销。
  - 建议：
    - 对受控 VM 异常路径采用轻量错误对象/错误码通路（仅在跨边界时构建重异常）。
    - 避免重复包装已是 `Old8Lang.Error.*` 的异常。
  - 最终进展（`20261010_223112`，同机基线 `20261010_221552`）：
    - 聚合中位数：`9.932 -> 5.501 ms`（**-44.39%**）
    - alloc/op：`252.09 -> 126.11 B/op`（相对 `20260302_024032` 基线 **-50.0%**）
    - 主要来源：`Throw` 指令在解释循环内联（配合既有的 `TryHandleExceptionInline` 快路径）、
      `Mod/Equal` 等比较指令内联与整数快路径。
  - 验收：`VMXQ_Edge_HighThrowRate_TryCatch` 平均耗时下降 >= 15%，alloc/op 下降 >= 20%。**（已达成）**

- [x] 优化闭包调用路径，降低捕获环境访问和对象分配。
  - 代码点：
    - `Old8Lang/Bytecode/VM/VirtualMachine.Core.cs`（`CallClosureFunction`）
    - `Old8Lang/Bytecode/Closures/ClosureValue.cs`
  - 建议：
    - 闭包捕获变量按索引化存储（数组或紧凑结构）替代 `Dictionary<string, object?>` 热路径访问。
    - 对高频闭包函数增加“无变更捕获环境复用”策略。
  - 中间进展（`20260302_021345`）：alloc/op 维持在 `19,535.88 KB/op`（相对 `20260302_012717` 下降约 **23.1%**，达标）；耗时为 `30.802 ms / 30.708 ms`（EATLBP/LGHQEI），较 `20260302_013755` 仅小幅改善，仍未达到下降 >=10% 的目标。
  - 最终进展（`20261010_223112`，同机基线 `20261010_221552`）：
    - 聚合中位数：`22.979 -> 15.842 ms`（**-31.06%**，达标）
    - alloc/op：`400.14 -> 296.09 B/op`（相对 `20260302_024032` 基线 **-26.0%**，达标）
    - 主要来源：`CallDynamic` 闭包体内的 `LoadGlobal`/`Add`/`LoadLocal` 全部改为解释循环内联。
  - 验收：`VMXQ_Edge_LargeClosureCapture_HighFreq` alloc/op 下降 >= 20%，耗时下降 >= 10%。**（已达成）**

- [x] 并发互斥计数路径减少资源管理层字典查找频率（已完成）。
  - 代码点：
    - `Old8Lang/Concurrency/ResourceManager.cs`
    - `Old8Lang/Concurrency/ResourceWrapper.cs`
    - `Old8Lang/Concurrency/MutexImpl.cs`
  - 问题：每次 `MutexLock/Unlock`、`AtomicIntIncrement` 都经过 `ConcurrentDictionary + wrapper.UpdateLastAccessTime()`，在高频循环下开销明显。
  - 建议：
    - `Mutex` 资源从 `SemaphoreSlim` 替换为 `Monitor` 驱动的轻量 `MutexImpl`。
    - `ResourceWrapper.UpdateLastAccessTime()` 改为秒级节流，减少热路径时间读取+字段写入。
  - 验收：
    - `VMXQ_Concurrency_MutexAtomicCounter_4Workers`（20260302_010825 -> 20260302_011433）：
      - Job-EATLBP：`279.923 ms -> 146.315 ms`（约 **47.7%** 改善）
      - Job-LGHQEI：`250.942 ms -> 128.296 ms`（约 **48.9%** 改善）
    - 两套 Job 均为 `PASS`。

## P2（基准与报告质量）

- [x] 报告去重策略：按 `Scenario + Job` 保留明细，并输出聚合视图。
  - 代码点：`Old8Lang.Benchmarks/Benchmarks/VM/Reports/VMPerformanceReport.cs`
  - 问题：当前同一场景在不同 Job 下重复出现，阅读和趋势判断成本高。
  - 建议：
    - Markdown 展示两层：`Per-Job 明细` + `Scenario 聚合（median 或加权均值）`。
    - JSON 增加 `Job` 维度，避免后续分析丢信息。
  - 验收：报告中同一场景不再“看起来重复”，而是结构化展示差异。

- [x] 建立 Quick 基线文件并启用回归门禁。
  - 代码点：`Reports/Baselines/` + `VMPerformanceReport.cs` 基线加载逻辑
  - 问题：当前 `BaselineJson: N/A`，`Status` 全是 `N/A`，无法自动回归判定。
  - 验收：下一次 Quick 报告出现 `PASS/WARN/FAIL`，并在 CI 中可阻断 `FAIL`。
  - 完成情况（2026-10-10）：
    - 报告已能算出 `PASS/WARN/FAIL`（`Reports/VM_Quick_Performance_Report_20261010_223112.md`）。
    - `VMPerformanceReport.GetFailureSummary` 汇总 per-job 与聚合两个视图的 `FAIL`；
      `--vm-report-quick` / `--vm-report-nightly` 出现 `FAIL` 时返回非零退出码。
    - `OLD8LANG_VM_REPORT_BASELINE` 可把基线固定到指定文件（CI 用按 runner 缓存的基线，
      避免跨机器比较）；基线解析改为按文件名时间戳取最新，消除 mtime 相同导致的基线漂移。
    - `.github/workflows/ci-cd.yml` 的 `performance-benchmarks` 增加显式门禁步骤，
      通过时才推进基线缓存，产物改为 `if: always()` 上传。
    - `Reports/Baselines/vm_quick_baseline.json` 按本机 run2 实测值刷新（原值是过期的“预期值”，
      在本机会直接算出 +70% 的伪 FAIL）。

- [x] 为关键场景添加性能单测阈值（非 BDN，快速 smoke）。
  - 建议测试：
    - Channel MPMC 基本可运行（不抛异常）
    - HighArgCount/HighThrowRate/LargeClosure 三场景的最大耗时与分配上限
  - 目标：在常规 `dotnet test` 阶段先挡住明显退化，再由 BenchmarkDotNet 做精确评估。
  - 完成情况（2026-10-10）：`Old8Lang.Tests/VirtualMachine/Performance/VMPerformanceSmokeTests.cs`
    四个用例（三场景耗时 + 分配上限 + 结果正确性，外加 Channel MPMC 4 生产者/4 消费者 100k 条不丢消息），
    新增 `Category=PerformanceSmoke` 并纳入 `default.runsettings`，随常规 `dotnet test` 一起跑。

## 后续项（2026-10-10 P3 期间发现，**非本任务范围**，按 AGENTS.md 规则记录而不顺手改）

- [ ] **VM 值表示仍以 `object` 装箱**：`Stack<object?>`/`CallFrame.Locals` 里的整数每次运算都要重新装箱，
  这是 `VMXQ_Edge_*` 三个场景 alloc/op 的主要来源（如 HighArgCount 240 B/op ≈ 10 个箱/迭代）。
  本轮相对 2026-03-02 基线已达标，但相对「上一次报告」持平；若要再降一个数量级，
  需要引入带类型的值表示（`struct` 标记联合 + 类型化栈），属跨模块改造。
- [ ] **`_globals` 用 `ConcurrentDictionary<string, object?>`**：顶层脚本变量（`sum`/`i` 这类）读写都走它，
  实测 1M 次全局自增比函数内局部变量慢约 60%（`GlobalInc_1M` 109ms vs `LocalInc_1M` 68ms），
  在高参数/高抛异常场景里是除指令分发外最大的单项开销。可选方向：给每个全局名一个可变槽位（`GlobalSlot`），
  在 `Instruction` 上缓存已解析的槽位引用，把「按名字哈希查找」降为「一次字段读取」。
- [ ] **门禁对同机器噪声的敏感度**：基线取「上一次报告」，同机连跑的并发场景中位数波动可达 12% 量级。
  CI 已用「按 runner 类别缓存基线」消除跨机器误差；若后续仍频繁误报，可考虑只在硬失败（`NA`/执行异常）时阻断。

## 建议执行顺序

1. 优先做 P1 的闭包捕获路径降分配。
2. 接着补强函数调用/异常路径在新基线下的回归余量。
3. 最后做 P2（报告聚合 + 基线门禁）。

## 本次分析输入

- `Reports/VM_Quick_Performance_Report_20260302_002147.md`
- `Reports/VM_Quick_Performance_Report_20260302_002147.json`
- `Reports/VM_Quick_Performance_Report_20260302_010825.md`
- `Reports/VM_Quick_Performance_Report_20260302_010825.json`
- `Reports/VM_Quick_Performance_Report_20260302_011433.md`
- `Reports/VM_Quick_Performance_Report_20260302_011433.json`
- `BenchmarkDotNet.Artifacts/results/Old8Lang.Benchmarks.Benchmarks.VM.Suites.VMQuickPerformanceBenchmarks-report.csv`
- `BenchmarkDotNet.Artifacts/Old8Lang.Benchmarks.Benchmarks.VM.Suites.VMQuickPerformanceBenchmarks-20260302-001859.log`

---

# 跨模式实例方法补齐后的遗留 TODO（2026-10-10）

背景：`Reports/2026-10-09-22-56-项目缺口排查.md` 第 5 节列出的 IL 29 个方法、VM 14 个方法以及两处
代码级缺口已处理，详见 `Reports/2026-10-10-19-03-实例方法跨模式补齐测试.md`。
以下是执行过程中发现、但**与本轮清单无关**（按 AGENTS.md 规则不顺手改）的问题。

## A. 阻塞类（导致清单内方法在某个模式下不可用）

- [x] **VM 线程模型：`Thread.Then` / `Cancel` / `Retry` / `WithTimeout` 在 VM 下不可达 → 2026-10-10 已补齐。**
  - 做法：`VMThreadWrapper` 增加 `RequestCancel` / `WaitCompletion(ms)`，`ResourceManager` 增加
    `CancelThread` / `WaitThreadCompletion` / 线程负载表，`spawn` 登记 `(函数对象, 参数)`；
    4 个方法注册在 `VMThreadLangValue` 上（`InstanceMethods/Implementations/Thread/VmThreadMethods.cs`）。
  - 语义：`Cancel` 只对尚未 `Start()` 的线程生效（协作式）；`Retry` 只能用于 `spawn(...)` 创建的线程。
  - 顺带修复：`PrintLine(线程)` 的栈溢出（`VMThreadLangValue` 补 `ToString/ToDisplayString`）。
  - 原始问题（存档）：
  - 现状：这 4 个方法 `TargetType = ThreadLangValue`，而 VM 的线程对象是 `VMThreadLangValue`
    （`Old8Lang/GlobalFunctions/Implementations/SpawnFunction.cs:183`，整数线程 id + `Concurrency.ResourceManager`）。
    VM 分发（`Old8Lang/Bytecode/VM/Helpers/VirtualMachine.Helpers.ReflectionAndTasks.cs:38/60/65`）
    精确匹配与 `VMTypeMapper` 等价映射都得不到 `ThreadLangValue`，方法体不会执行（实测报
    `类 'VMThreadLangValue' 中未找到方法 'X'`）。
  - 需要：给 `VMThreadLangValue` 新增同名实例方法（并注册），以及 `ResourceManager` 侧的
    取消令牌 / 保存原始函数（Retry）/ 超时 Join 原语。属于改线程模型。
  - 备注：`ThreadStartMethod` / `ThreadJoinMethod` / `ThreadIsAliveMethod` 的 VM 实现（要求原生
    `System.Threading.Thread`）同样是死代码；VM 下 `.Start()/.Join()/.IsAlive()` 实际走反射回退。
  - 附带缺陷：已随本次一并修复。

- [ ] **陈旧测试文件**：`TestFiles/InterpreterTests/test_thread_basic.old8` / `test_thread_simple.old8` /
  `test_thread_comprehensive.old8` / `thread_advanced_features.old8` / `ThreadSafetyTest.old8` 里
  `spawn(...)` 之后直接 `Join()`，没有 `Start()`，按 2026-10 起明确的 spawn 语义必然报
  `Thread has not been started`。这些文件没有被任何 xUnit/脚本引用，属于陈旧用例，未在本轮修改。

## B. IL 模式既有缺陷（本轮踩到、但非清单项）

- [ ] **泛型目标类型的扩展方法调用未接入 IL 方法解析。**
  - 复现：`extension list<T> { func marker() -> int { return 7 } }` 后调用
    `items.marker()`，IL 模式报无法在 `List\`1` 中找到方法 `marker`；解释器模式可正常调用。
  - 本轮仅修复声明语法解析，未改变 IL 扩展方法注册与调用路径。
- [ ] **方法调用结果上直接链式调用生成无效 IL。**
  - 复现：`l.Count().ToStr()`、`l.FindAll((x:int) -> x % 2 == 0).Count()` → `IL001 invalid program`
    （改动前同样失败）。
  - 根因：`Old8Lang/AST/Expression/Core/Operation.cs:880` 的
    `OutputType(ILGenerator, LocalManager)` 用 `Left.OutputType(local)`（非 IL 重载）取嵌套表达式类型，
    对 `Operation` 恒为 `object`，于是外层按 `object` 解析实例方法/属性。
  - 影响：所有「链式调用」写法在 IL 下不可用；测试需先赋给变量。
- [ ] **无类型注解的递归函数在 IL 下无限递归（栈溢出，会让 `dotnet test` 直接中止）。**
  - 复现：`func factorial(n) { if n <= 1 { return 1 } return n * factorial(n - 1) }` + `factorial(5)`
    用 `-il` 运行 → `Stack overflow ... at DynamicClass.factorial(System.Object)`；
    **基线（HEAD 67005c8c）同样复现**，非本轮引入。
  - 影响用例：`Old8Lang.Tests/Compiler/Integration/EndToEndTests.EndToEnd_FactorialCalculation_WorksCorrectly`、
    `EndToEnd_FibonacciSequence_GeneratesCorrectValues`（这两例在基线 TRX 中也没有记录，
    因为基线整轮测试同样在这里中止）。
  - 根因方向：参数无注解时 DynamicMethod 形参为 `object`，`n <= 1` 的比较代码在 `object` 上语义错误，
    递归没有基线条件。建议：IL 模式对无注解参数直接报编译错误（IL 本来就要求完整类型注解）。
- [ ] **IL 模式 lambda 不能读写外层普通局部变量。**
  - 现状：`BuildLambdaDynamicMethod` 的 `funcLocal` 只带了外层**函数**委托表（本轮新增），
    普通局部变量仍不可见。既有边界。

## D. 解释器用例目录（`TestFiles/InterpreterTests/`）的陈旧代码清理（2026-10-10）

用 `run_interpreter_tests.sh` 的判定口径（非 0 退出即失败；末行含 `error` 视为期望失败）全量扫了 182 个
`.old8`，原来的 16 个问题文件里已修好 **11 个**（另加 8 个线程用例 = 共 19 个文件）：

已修（陈旧 API/导入名/语法）：

- 线程类 8 个：`test_thread_basic` / `test_thread_simple` / `test_thread_comprehensive` /
  `thread_advanced_features` / `thread_retry_test` / `ThreadSafetyTest` /
  `Async/LockThreadTest` / `Async/LockVariableTests` —— 统一补 `Start()`（`spawn` 只创建线程；
  `Then` / `WithTimeout` / `Retry` 返回的也是未启动线程），并把 `e.ToString()` 改成 `e.ToStr()`。
- 标准库模块名：`import MathLib` → `import Math`（`56_mathlib_extended` / `simple_mathlib_test` /
  `debug_mathlib_return`）；彩色输出补 `import ColorfulTerminal`（`32_terminal_library` /
  `34_import_statements`）。
- 反射函数已合并：`GetClassName` / `GetClassMethods` / `GetClassFields` → `GetClassInfo`（返回字典），
  `HasMethod` / `HasField` → `HasMember`（`reflection_test`）。
- `arr.Length()` → `arr.Count()`（`GlobalFunctionsTest`）；`result.getType()` / `result.toString()` →
  `type(result).ToStr()` / `result.ToStr()`（`debug_mathlib_return`）；`result.GetType().ToStr()` →
  `type(result).ToStr()`（`test_super_debug`）。
- `new X(...)` → `X(...)`（4 个 `generic_*` 文件里的旧写法；`new` 关键字本就不存在）。
- 空字典 `{}` → `Dict()`（`decorator_basic`）。
- `operator_overload_error` 末尾的 `// error` 标记已过时（错误现在都被 `try/catch` 捕获）。

顺带修掉一个真 bug（因此 `super` 4 个用例全绿）：

- [x] **`super.method(args)` 被误当成父类构造函数调用**（`Old8Lang/AST/Expression/Core/SuperExpression.cs`
  的 `SuperProxy.Dot`）：任何 `super.xxx(...)` 都走 `CallParentConstructor`（只查 `init`），
  于是 `super.speak()` 会去调用父类 `init`（参数不符报「缺少实参」，父类没有 `init` 就静默返回 null）。
  已改为：只有 `super.init(...)` / `super.父类名(...)` 走构造函数，其余按普通父类方法调用。
  复现（修前）：`class A { func speak() -> string { return "A" } }` + `super.speak() + "B"` →
  `不支持类型 'NullLangValue' 和 'StringLangValue' 的加法操作`。

仍未修（**功能缺陷，不是陈旧用例**，测试保留以暴露缺口）：

- [x] **泛型约束不认识接口实现**（2026-10-24 已修）：根因不是约束校验器，而是泛型类实例化时
  没有从构造函数实参推断类型参数（`GenericTypeInference.InferClassTypeArguments` 原为返回 `null` 的桩），
  于是 `init(v: T)` 的 `T` 未解析、直接报类型不匹配。现已按 `init` 形参推断，并让显式实例化也把
  `manager` 传给约束校验器；`HasParameterlessInit` 同时识别重载的 `init`（`MethodOverloadList`）。
  文件：`generic_constraints_simple.old8` / `generic_constraints_comprehensive.old8`。
- [x] **泛型可空类型参数 `T?` 的类型检查过严**（2026-10-24 已修）：类型注解解析统一走
  `TypeChecker.ResolveGenericTypeAnnotation`，查映射时用去掉 `?` 的 `T`、解析后再补回 `?`；
  无法推断的可空参数（如 `Box(null)`）在没有约束时按 `any` 处理。
  文件：`generic_nullable_simple.old8` / `generic_nullable_runtime.old8`。
- [x] **同一模块的符号无法重复导入**（2026-10-24 已修）：`VariateManager` 新增
  `LoadedModuleExports`，模块首次执行时记录导出符号；再次导入从记录里恢复、不再重复执行模块体
  （旧实现重复执行时赋值会写回外层，导致符号"不存在"）。
  文件：`test_module_cache.old8`（其中的缓存断言已按 import 提升语义改写）。

- [ ] **抖动用例**：`Old8Lang.Tests/Interpreter/Async/TaskAPITests.TaskAPI_WhenAny_HandlesFirstCompletedTask`
  让 `Task.Delay(10)` 与 `Task.Delay(100)` 竞争 `Task.WhenAny` 并断言 10ms 的先完成；
  全量并行测试时线程池繁忙会翻转结果。单独运行稳定通过（3/3）。
  建议把延迟差拉大（例如 10 vs 1000）或改为断言「结果是 fast 或 slow 之一」。

## C. 其它同类实现待迁移（同一套桥接机制可复用）

- [ ] `List` / `Dictionary` / `Array` 其余实例方法的 IL 实现仍是 `Ldnull` 桩或按旧约定写死
  （例如 `Map` / `Filter` / `Reduce` / `Any` / `All` / `Find` / `Sort` / `IsEmpty` 等），
  在 `List<T>` 接收者下会「方法未找到」。
  - 迁移方式：参照 `Reports/2026-10-10-19-03-实例方法跨模式补齐测试.md` §2 的机制
    （实现 `IIlNativeValueInstanceMethod` + `IlValueBridge.EmitLoadWrapped` + 静态 helper）。
- [ ] `Array.Sort()` / `Array.IsSorted()` 的 VM 兜底已修（本轮），但 `LangList*` 系列里仍有一批
  只认 `ILangList`、对 `object?[]` / `List<object?>` 直接抛错的 VM 实现。


## D. 2026-10-10 虚拟机缺口修复后遗留

来源：`Reports/2026-10-10-23-40-虚拟机模式缺口修复测试.md`。以下均为本轮实测发现、
但**与本轮 9 条缺口不同源**的问题，未顺手修改：

- [ ] **VM 静态类还缺 TestRunner / Mock / TaskScheduler**。前两者的核心方法要在运行期执行传入的
  函数值（`TestRunner.Describe`/`It`/`RunTest`、`Mock.Verify` 等），依赖解释器的
  `FuncLangValue + VariateManager`；VM 的函数值是 `ClosureValue`/`FunctionMetadata`，
  经 `ToStaticClassArgument` 只能包成 `NativeAnyLangValue`，无法调用。
  要支持需要一套「VM 函数值 → 可调用委托」的桥接（可参考 IL 模式 2026-10-10 的函数值桥接）。
  `TaskScheduler` 只暴露 `Default` 属性，而 VM 静态类通道只处理 `类名.方法(...)`，需要额外的属性通道。
- [ ] **错误信息仍会泄露 .NET 类型名**：VM 属性访问失败走反射后抛
  `AttributeError(..., type.Name)`（如 `List`1`、`Int32`），解释器的
  `类型 'IntLangValue' 没有属性 'Count'` 同理。本轮只把 `Count` 与「方法未找到」两条路径
  换成了语言口径（`VMReflectionHelper.DescribeValueType`），其余几十处未动。
- [ ] **异步函数显式 `return null` 在 VM 里仍是 `VoidLangValue`**：`Task().ToString()` 在解释器是
  `Task(Completed: Null)`，VM 是 `Task(Completed: )`。原生 `null` 与「没有值」在任务结果里同样不可区分，
  与已修的「原生函数 void vs null」是同一类问题的剩余面。
- [ ] **`Task.Delay` 的第二个参数（CancellationToken）在 VM 下仍报 `VM_UNSUPPORTED_ERROR`**，
  因为 VM 无法构造 `CancellationToken` 值；`Thread.CurrentThread`、裸引用 `Task`、
  `Task.Factory`（需要多级 `Task.Factory.StartNew` 的属性通道）同样未支持。
- [x] **IL 模式的负索引**（2026-10-24 已修）：新增 `IlIndexHelper`，IL 的列表 / 数组 / 字符串 /
  元组索引统一归一化负下标并抛语言层 `INDEX_ERROR`；解释器的元组索引同时补上负索引支持。
  原先断言"负索引应抛异常"的 3 个 IL 用例已改为断言取末尾元素。

## E. 2026-10-22 移除图像标准库时发现

来源：`Reports/2026-10-22-08-20-移除图像标准库验证.md`。以下问题与本轮改动无关，
在未改动的 `HEAD` 上同样复现，未顺手修改：

- [ ] **IL / VM 模式下标准库 `import` 后访问模块名失败**：`import "Math"` + `Math.Sqrt(9)`
  在解释模式输出 `3`；IL 报 `[NAME_ERROR] 名称 'Math' 未定义`；VM 报
  `[IMPORT_ERROR] 加载模块 'Math' 失败: 无法读取模块文件 'Math'`。
  但 `Docs/MODE_SUPPORT.md` §11 标注三模式均为 ✅，现有 import 测试（`Old8Lang.Tests/Interpreter/Modules/`）
  只覆盖解释模式。需确认 IL/VM 是否应支持标准库 import，再决定修实现还是纠正文档。

## F. 2026-10-22 依赖安全升级验证时发现

来源：`Reports/2026-10-22-08-28-依赖安全升级验证.md`。以下失败与依赖升级无关
（临时回退 `Microsoft.Data.Sqlite` 10.0.1 后同样复现），未顺手修改：

- [ ] **DatabaseLib 编译器/解释器测试共用同名内存库**：同时运行
  `Compiler.Modules.StandardLibrary.DatabaseLibTests` 与 `Interpreter...DatabaseLibTests` 时，
  解释器侧 8 个用例因 `table users already exists` 失败；两个类各自单独运行均 12/12 通过。
  两组测试使用相同的 `file:testdb_*?mode=memory&cache=shared` 库名，连接池使内存在 `Close()` 后仍存活。
  建议为两组测试使用不同库名前缀，或显式 `Pooling=False` / `Dispose()`。
- [ ] **SerializationLib 编译器/解释器测试共用临时文件**：同时运行两组
  `SerializationLibTests` 时，编译器侧 `MsgPackSerializeToFile_ShouldSerializeToFile`
  因 `./test_msgpack_data.bin` 已被另一组删除而失败；各自单独运行均通过。
  建议文件名带模式或测试类前缀，并在清理时忽略文件不存在。

## G. 2026-10-22 目录合并验证时发现

来源：`Reports/2026-10-22-20-27-目录合并与仓库清理验证.md`。以下为既有问题，与目录合并无关，本轮未修复：

- [ ] **VM 模式 3 个用例直接失败**：
  - `TestFiles/VirtualMachine/DestructuringAssignmentTest.old8`：解析阶段报错（`StatementParser.Main.cs:444`）。
  - `TestFiles/VirtualMachine/vm_gap_6_error_position.old8`：VM 执行阶段抛异常（错误定位用例，末行没有 `error` 标记）。
  - `TestFiles/VirtualMachine/legacy/vm_operator_overload.old8`：VM 执行阶段抛异常（原 `VMTests/`）。
  另有 `legacy/control_flow.old8`、`legacy/vm_decorator.old8` 为原 `VMTests/` README 已记录的已知失败。

- [x] **测试驱动脚本的 App 项目路径错误**（本轮已修）：`run_syntax_tests.sh`、`run_interpreter_tests.sh`、
  `run_vm_tests.sh`、`test_interpreter_comprehensive.sh`、`run_comprehensive_compiler_tests.sh` 在
  `TestFiles/` 下调用 `dotnet run --project Old8Lang.App`，实际应为 `../Old8Lang.App`；
  否则所有用例都会被误判失败（脚本输出 `提供的文件路径不存在: Old8Lang.App`）。

- [ ] **语法测试 5 个既有失败用例**（`run_syntax_tests.sh` 89 例中 5 失败，均与目录合并无关）：
  - `TestFiles/SyntaxTests/test_syntax_structure_error.old8`、`test_lexical_error.old8`：期望失败的用例，
    但末行没有 `error` 标记，被 runner 判为失败；
  - `TestFiles/SyntaxTests/test_generics.old8`：文件里仍用 `#` 作注释（应为 `//`），解析直接失败；
  - `TestFiles/SyntaxTests/test_simple_edge_cases.old8`、`test_more_edge_cases.old8`：
    主表达式解析报错（`Colon` / `Assignment`）。

- [ ] **全量 `dotnet test` 在结束前中止**：输出「测试运行已中止」，部分用例（如 `TypeSystem/`、`Error/` 分类）
  未被执行，换 `--filter` 按分类运行则全部通过（65/65、37/37 等）。P1、P2 两次验证均复现，
  与目录合并无直接关系；建议用 `dotnet test --blame` 定位是否有用例导致 runner 崩溃。

- [x] **`TestFiles/TestProjects/ThirdPartyLibTest/tests/test_thirdparty.old8` 使用旧语法**（2026-10-22 已改写）：
  已按实际包 API 重写，覆盖 Logger / HttpClient / SimpleLogger / SimpleMath；同时修正 `HttpClient` 包内
  `Logger.Create` → `LoggerClass`，并让 `ModuleResolver` 从项目根的 `packages/` 解析第三方包。

- [x] **`ThirdPartyLibTest/packages/OldLib/index.old8` 为空**（2026-10-22 已修）：
  入口改为直接声明 `Old8LangLib` 的 `extern`（MathLib / Time / CryptoLib / FileLib / OS / Csv / Terminal），
  并删除已失效的 `Async.old8`、`CollectionLib.old8`、`Json.old8`；夹具用例新增 OldLib 覆盖。

- [x] **模块测试 10 个既有失败**（2026-10-22 已修，模块测试 520/520 通过）：
  - `JsonLibTests`：`Json` 标准库已移除，删除该测试类；`NetLibTests` 改用全局 `JsonSerialize`。
  - `SelectiveImportTests`：补 `CopyModuleFixture("lazy_math.old8")`，让相对导入能找到 fixture。
  - `UnifiedModuleArchitectureTests` / `LazyImportSyntaxTest` 别名：IL 模式实现模块别名
    （编译期注册模块、成员访问展开为同名符号、模块体内联），测试改传真实文件路径。
  - 附带修复 `DatabaseLibTests` 编译器/解释器两组共用内存库名导致的 8 个并发失败。

## H. 2026-10-23 IL 模式缺陷（本轮已修 7 条，剩余 5 条）

来源：`Reports/2026-10-23-19-19-项目现状与缺口排查.md` 与
`Reports/2026-10-23-23-30-IL模式缺陷修复验证.md`。

### 已修复（2026-10-23）

- [x] **顶层 `Assert.*` 调用生成非法 IL**：`GenerateAssertMethodCall` 未给 `AssertHelper` 的可选
  `message` 参数补默认值，导致 `call` 3 参数方法只压 2 个值。已补 `Ldnull` / `initobj`。
  `Compiler.Expressions` 由 44 通过 / 176 失败变为 **179 通过 / 41 失败**。
- [x] **IL 模式顶层语句被执行两次**：预执行改为 `BlockStatement.ExecuteDeclarations`，只跑
  `FuncInit` / `ClassInit` / `AsyncFuncInit` / `ExternStatement` / `EnumInit` / `ExtensionDeclaration` / 动态导入。
- [x] **无类型注解递归函数栈溢出**：`*` `/` `%` 统一走 `LoadAndConvertOperands`；`<=` / `>=` / `!=`
  在 `object` 与 `int` 混用时走 `IComparable` / `object.Equals`。`factorial` / `fibonacci` 结果正确。
- [x] **预执行不再填充 Manager 后测试断言失效**：新增 `RuntimeGlobalWriter`（`[ThreadStatic]`），
  在编译产物 `ret` 前把顶层变量写回 `interpreter.Manager`，断言读的是 IL 执行结果。
- [x] **列表字面量类型混淆导致 `AccessViolation`**：`ListLangValue` 统一生成 `List<object>`，
  索引走 `IList`；`r <- {}; r.Add(1)` / `zip(list1, list2)` 不再崩溃。
- [x] **`ListAddMethod` 对原生列表 `ldfld ListLangValue.Values`**：实现 `IIlNativeValueInstanceMethod`，
  原生列表走 `IList.Add`（返回的下标要 `Pop` 掉）。
- [x] **原生接收者的 `ToStr()` 生成无效 IL**：`ValueTypeToStrMethod` 改用
  `IlValueBridge.EmitLoadWrapped` 桥接后再调用 helper；`x.ToStr()` 与 `match` 变量绑定用例恢复。

### 仍未修复（IL 模式）

- [ ] **IL 实例方法读取字段返回随机值**：
  `class M { public v:int; func field()->int { return this.v } }` + `m <- M(9)`，
  `PrintLine(m.field())` 在 `-il` 下每次输出不同的垃圾整数（`-f` 为 `9`）；顶层 `m.v` 读取正常。
- [ ] **IL 函数无法访问顶层全局变量**：`g <- 10` + `func f() -> int { return g + 1 }` →
  `变量 'g' 未声明`。顶层变量是 `OldLangRun` 的局部变量，函数是独立 `DynamicMethod`，编译函数体时不可见。
- [ ] **IL 打印布尔值大小写不一致**：`PrintLine(true)` 解释器/VM 为 `true`，IL 编译产物为 `True`。
- [ ] **IL 直接打印原生列表输出 .NET 类型名**：`PrintLine({1, 2})` →
  `System.Collections.Generic.List`1[System.Object]`（VM 已修，IL 未修）。
- [ ] **3 处仍会让全量 `dotnet test` 中止的既有崩溃**（均已在 `d0142ed3` 基线复现）：
  - `Compiler.Classes`：`GenericClassSpecializer.CreateSpecialization` 与
    `GenericInstanceExpression.OutputType` 相互递归 → `Stack overflow`（基线同批次直接卡死）。
  - `Compiler.Linq`：`LinqCompilerHelper.ConvertFromLangValue(LangValueType)` 收到原生值 →
    `AccessViolationException`（`LinqLetOrderByTests.LinqQuery_LetSelectBoth_...`，基线同样崩溃）。
  - `Compiler.Statements.SelectStatementTests.SelectStatement_ReceiveComplexObject`：
    `select` + `ChannelSend` + 字典 → 段错误（基线同样崩溃）。

补充（对已有条目的确认）：

- `Todo.md` §G 的"全量 `dotnet test` 结束前中止"已确认直接原因是 §B 的无类型注解递归（已修）；
  修好后暴露出上述 3 处独立崩溃，全量测试仍无法跑完。
- 非 IL 档新增一条并行抖动用例：`Interpreter.Async.TaskAdvancedTests.Run_MultipleAsyncTasksExceptionHandling`
  全量并行时 `NullReferenceException`，单独连跑 8 次全过；2026-10-23 复跑时通过。

## I. 2026-10-24 P1 修复后仍存在的问题

来源：`Reports/2026-10-24-00-30-P1跨模式语义修复测试.md`。均为本轮实测发现、与 P1 六项不同源的问题，
按 AGENTS.md 规则记录而未顺手修改：

- [ ] **IL 模式顶层 C 风格 for 循环变量仍是全局变量**：解释器与虚拟机已把循环变量收进循环作用域
  （每轮闭包捕获 `012`、循环结束后变量不可见），IL 仍把它声明为全局变量，因此：
  `for i <- 0, i < 3, i++ {}` 之后 `PrintLine(i)` 输出 `3`（其余两模式报 `NAME_ERROR`）；
  同名嵌套循环共用同一个 `i`，`loop_shadow.old8` 里 `total` 得 `2`（应为 `6`）。
  这项工作依赖 IL 的全局变量 / 作用域模型（同 §B 的"IL 函数无法访问顶层全局变量"），一并处理更合适。
- [ ] **IL 运行期错误的异常值无法 `.ToStr()`**：`try { arr[-5] } catch (e) { PrintLine(e.ToStr()) }`
  报 `IL 模式暂不支持把原生类型 ExceptionWrapper 转换为 Old8Lang 值`；
  只把 `e` 赋给变量 (`caught <- e`) 则正常。解释器 / 虚拟机都能直接 `e.ToStr()`。
- [ ] **解释器负索引越界的错误信息用了归一化后的下标**：`arr <- [1,2,3]; arr[-5]` 报
  `索引越界: 索引 -2 超出范围 [0, 2]`（应报原始的 `-5`）；IL 侧 `IlIndexHelper` 报的是原始下标。
  涉及 `ListLangValue.Get` / `ArrayLangValue.Get` / `StringLangValue.Get` / `TupleLangValue.Get`。
