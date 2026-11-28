# 测试与性能验证报告：VM 类型化值表示（`VmValue` / `VmStack`）

测试日期：2026-11-27  
运行环境：.NET 10.0.0（macOS）

## 1. 改动摘要

按 P4 §4.3 方案三阶段全部落地，消除虚拟机执行热路径的基础类型 `object` 装箱：

- **阶段 1（类型化值表示、类型化栈、局部变量、常量池与操作数缓存）**：
  - 在 `Old8Lang.Runtime/Bytecode/Core/VmValue.cs` 新增 24 字节标记联合 `readonly struct VmValue`（`VmValueKind`：`Null` / `Int` / `Long` / `Double` / `Bool` / `Char` / `Object` / `UpValue`），并在跨边界 `ToObject()` 复用共享布尔与小整数（`-128..4095`）装箱表。
  - 在 `Old8Lang.VirtualMachine/VM/VmStack.cs` 新增 `VmStack` 替代 `Stack<object?>`（延迟按需扩容、弹出基础类型不触发引用清空写屏障）。
  - `CallFrame.Locals` 升级为池化的 `VmValue[]`（`ArrayPool<VmValue>.Shared`）。
  - `ConstantPool` 并行维护类型化常量表 `GetConstantValue(int)`；`Instruction` 自动同步缓存 `IntOperand`。
  - `VirtualMachine` 内联循环、算术、比较、逻辑、条件跳转、参数与返回值类型校验全部直接操作 `VmValue`；无返回值占位改用单例 `VoidLangValue.Instance`；移除原 `VmBoxed.cs`。
- **阶段 2（全局槽位、闭包共享单元与闭包环境类型化）**：
  - `GlobalSlot` 底层存储改为 `VmValue`，单线程零锁零屏障，在派生 worker 虚拟机（`spawn` / `ThreadCreate` / `NewTask` / `CallAsync` / `Task.Run`）前通过 `GlobalTableState.IsConcurrent` 切换为无锁 SeqLock 读 / CAS 写，保证跨线程无撕裂、零 SyncBlock 分配与只读零争用。
  - `UpValueCell` 改为 `VmValue` 存储并提供 `Box(ref VmValue)`；`ClosureEnvironment` 新增 `IsEmpty`、`GetLocalVmValue`、`TryGetVmValue`、`TrySetVmValue`；顶层无捕获 lambda 直接复用 `Instruction.CachedGlobalSlot`。
- **阶段 3（调用边界优化与门禁收紧）**：
  - `Call` 位置快速路径与 `CallDynamic` 闭包/函数纯位置调用均直接从 `VmStack` 弹出 `VmValue` 写入 `CallFrame.Locals`，免 `object?[]` 分配与装箱。
  - 新增 `VMValueRepresentationTests`（8 项：类型往返、溢出与混合比较、闭包写回与循环刷新、多线程并发 `GlobalSlot` 无撕裂压测、1M 局部/全局循环及 50k UpValue 闭包零分配验证）。
  - `VMPerformanceSmokeTests` 三个核心场景分配上限由 30MB / 20MB / 45MB 统一收紧至 **1MB**。

## 2. 性能与内存分配对比（Release）

### 2.1 热循环与 Smoke 关键场景（`dotnet test -c Release`）

| 场景 | 改造前（P4 / 原基线） | 改造后实测（Release） | 分配降幅 |
|---|---:|---:|---:|
| `1M sum <- sum + 1`（局部变量循环） | 45.78 MB（45.78 B/次） | **1,184 B（0.0012 B/次）** / 59.21 ms | **-99.997%** |
| `1M sum <- sum + 1`（顶层全局变量循环） | 45.78 MB（45.78 B/次） | **944 B（0.0009 B/次）** / 60.39 ms | **-99.998%** |
| `50k UpValue 闭包写回循环` | 每轮装箱 | **1,592 B（0.0318 B/次）** / 24.44 ms | **~0 B/次** |
| `HighArgCount_CallHotPath`（30k × 8 参） | ~7.2 MB（上限 30 MB） | **1,600 B** / 24.35 ms（上限 1 MB） | **-99.98%** |
| `HighThrowRate_TryCatch`（40k / 10k throw） | ~5.0 MB（上限 20 MB） | **1,272 B** / 4.58 ms（上限 1 MB） | **-99.97%** |
| `LargeClosureCapture_HighFreq`（50k × 8 捕获） | ~14.8 MB（上限 45 MB） | **1,872 B** / 34.64 ms（上限 1 MB） | **-99.99%** |

### 2.2 `--vm-diagnose` 同机前后对比（Release，每场景 6 轮中位数）

| 场景（VMExecute） | 改造前耗时 (ms) | 改造后耗时 (ms) | 耗时变化 | 改造前分配 (B) | 改造后分配 (B) | 分配变化 | GC (Gen0) |
|---|---:|---:|---:|---:|---:|---:|---:|
| `LargeFile_10k` | 9.266 | **8.557** | -7.7% | 1,706,680 | **1,067,568** | **-37.4%** | 0 → 0 |
| `LargeFile_50k_Generated` | 11.146 | **10.298** | -7.6% | 8,531,616 | **5,225,456** | **-38.8%** | 0 → 0 |
| `LargeClosureCapture_HighFreq` | 13.653 | **10.784** | **-21.0%** | 13,604,936 | **5,352** | **-99.96%** | **6 → 0** |
| `ChannelTryReceive_Timeout10` | 19.525 | 19.863 | +1.7% | 3,858,480 | 3,858,776 | +0.0% | 0 → 0 |
| `Await_10k` | 20.016 | 22.249 | +11.2% | 4,404,328 | **4,067,120** | **-7.7%** | 12 → 13 |

> 注：`ChannelTryReceive` 与 `SpawnJoin_10k` / `Await_10k` 以 OS 线程/线程池调度和原生 `CallNative` 对象数组边界为主，经小整数装箱缓存与无锁 SeqLock 调优后分配与基线持平或略优。

## 3. 回归验证记录

| 验证项 | 命令 | 结果 |
|---|---|---|
| 解决方案构建 | `dotnet build Old8Lang.sln --no-restore` | **0 错误** |
| VM 全量单元测试（含新增 8 项与收紧后 Smoke） | `dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj --no-restore --no-build --filter "FullyQualifiedName~Old8Lang.Tests.VirtualMachine"` | **1,149 通过 / 0 失败 / 0 跳过** |
| 程序集边界与依赖架构测试 | `dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj --no-restore --no-build --filter "FullyQualifiedName~Old8Lang.Tests.Architecture.BackendAssemblyBoundaryTests\|FullyQualifiedName~Old8Lang.Tests.Architecture.ArchitectureProjectDependencyTests"` | **22 通过 / 0 失败 / 0 跳过** |
| Benchmarks 单元测试 | `dotnet test Old8Lang.Benchmarks/Old8Lang.Benchmarks.csproj --no-restore --no-build` | **31 通过 / 0 失败 / 0 跳过** |
| VM 性能档（`performance.runsettings`） | `dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj --no-restore --no-build --settings Old8Lang.Tests/performance.runsettings --filter "FullyQualifiedName~Old8Lang.Tests.VirtualMachine.Performance"` | **76 通过 / 0 失败 / 1 跳过**（既有 `Performance_Select_ChannelSelect`） |
| Release 零分配与 Smoke 复测 | `dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~VMValueRepresentationTests\|FullyQualifiedName~VMPerformanceSmokeTests"` | **12 通过 / 0 失败** |
| `.old8` VM 脚本集 | `cd TestFiles && bash run_vm_tests.sh` | **78 通过 / 1 失败**（失败项为无关既有文件 `array_sort_vm_test.old8`，见第 4 节） |

## 4. 无关遗留记录

- `TestFiles/VirtualMachine/array_sort_vm_test.old8` 报错 `QuickSort 字符串失败: a,bb,ccc,dd`：该历史脚本仍按旧版字符串长度排序（`a,bb,dd,ccc`）断言，而 2026-11-18 字符串排序已统一改为字典序（`CompareOrdinal`）。按规则 6 不在本任务中顺手修改，已记录到 `changes/Todo.md`。

## 5. 结论

验收标准全部满足：`VmValue` / `VmStack` / 类型化 `CallFrame.Locals` / 类型化无撕裂 `GlobalSlot` / 类型化 `UpValueCell` 与 `ClosureEnvironment` 已完整落地，热点循环与高频调用/闭包/异常场景堆分配降幅达 **99.96%~99.998%**（1M 自增循环由 45.78 B/次降至 **~0 B/次**），全部 1,149 项 VM 单测与架构边界测试全绿。
