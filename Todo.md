# VM 模式性能优化 TODO（基于 2026-03-02 Quick 报告）

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
- [ ] 基准观察：`20260302_024032` 中 `VMXQ_Edge_HighThrowRate_TryCatch` 聚合状态为 `FAIL`（中位数约 `+7.52%`），需在后续排查是否为噪声还是异常路径回归。

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

- [ ] 优化高参数函数调用路径（减少每次调用的参数整理/查找成本）。
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
  - 当前进展（`20260302_021345`）：alloc/op 维持在 `19,535.88 KB/op`（相对 `20260302_012717` 下降约 **23.1%**，达标）；耗时为 `30.802 ms / 30.708 ms`（EATLBP/LGHQEI），较 `20260302_013755` 仅小幅改善，仍未达到下降 >=10% 的目标。
  - 验收：`VMXQ_Edge_LargeClosureCapture_HighFreq` alloc/op 下降 >= 20%，耗时下降 >= 10%。

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

- [ ] **VM 线程模型：`Thread.Then` / `Cancel` / `Retry` / `WithTimeout` 在 VM 下不可达。**
  - 现状：这 4 个方法 `TargetType = ThreadLangValue`，而 VM 的线程对象是 `VMThreadLangValue`
    （`Old8Lang/GlobalFunctions/Implementations/SpawnFunction.cs:183`，整数线程 id + `Concurrency.ResourceManager`）。
    VM 分发（`Old8Lang/Bytecode/VM/Helpers/VirtualMachine.Helpers.ReflectionAndTasks.cs:38/60/65`）
    精确匹配与 `VMTypeMapper` 等价映射都得不到 `ThreadLangValue`，方法体不会执行（实测报
    `类 'VMThreadLangValue' 中未找到方法 'X'`）。
  - 需要：给 `VMThreadLangValue` 新增同名实例方法（并注册），以及 `ResourceManager` 侧的
    取消令牌 / 保存原始函数（Retry）/ 超时 Join 原语。属于改线程模型。
  - 备注：`ThreadStartMethod` / `ThreadJoinMethod` / `ThreadIsAliveMethod` 的 VM 实现（要求原生
    `System.Threading.Thread`）同样是死代码；VM 下 `.Start()/.Join()/.IsAlive()` 实际走反射回退。
  - 附带缺陷：`PrintLine(VMThreadLangValue)` 会栈溢出（`VMThreadLangValue.GetValue()` 返回自身，
    `LangValueType.ToString()` 递归）。

## B. IL 模式既有缺陷（本轮踩到、但非清单项）

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

## C. 其它同类实现待迁移（同一套桥接机制可复用）

- [ ] `List` / `Dictionary` / `Array` 其余实例方法的 IL 实现仍是 `Ldnull` 桩或按旧约定写死
  （例如 `Map` / `Filter` / `Reduce` / `Any` / `All` / `Find` / `Sort` / `IsEmpty` 等），
  在 `List<T>` 接收者下会「方法未找到」。
  - 迁移方式：参照 `Reports/2026-10-10-19-03-实例方法跨模式补齐测试.md` §2 的机制
    （实现 `IIlNativeValueInstanceMethod` + `IlValueBridge.EmitLoadWrapped` + 静态 helper）。
- [ ] `Array.Sort()` / `Array.IsSorted()` 的 VM 兜底已修（本轮），但 `LangList*` 系列里仍有一批
  只认 `ILangList`、对 `object?[]` / `List<object?>` 直接抛错的 VM 实现。

