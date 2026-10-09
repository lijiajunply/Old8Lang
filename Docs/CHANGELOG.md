# 更新记录

> **关于本文档中的代码片段**：CHANGELOG 是按日期记录当时改动的日志，
> 其中较早条目（各 rc 版本）里的示例反映的是**当时**的语法，部分写法（如 `native` 语句、
> `{ ... }` 形式的省略占位符）在后续版本中已不再适用，不能直接复制运行。
> 当前可用的语法以 [Old8Lang_Grammar.md](./Old8Lang_Grammar.md) 与
> [MODE_SUPPORT.md](./MODE_SUPPORT.md) 为准。

## 文档示例代码全面核对与修正 (2026-10-09)

延续当日的文档整理，把 `Docs/` 下全部 **430 个 `.old8` 代码块**逐个落成文件实跑，
修掉跑不通的示例。

### 度量方式的一个坑

先用 `-s`（语法检查）扫，得到的失败数偏高。原因是 **`-s` 与 `-f` 对预编译指令的处理不一致**：

```
$ Old8Lang.App -s pp.old8      # 文件内容为 #define / #if
[SYNTAX_ERROR] 语法错误：无法识别的字符 '#'。

$ Old8Lang.App -f pp.old8
OTHER on                       # 正常执行
```

因此本轮改为「`-s` 失败后用 `-f` 复核」。最终：**398 块 `-s` 通过，另有 6 块仅 `-f` 通过，
26 块确认失败**；那 26 块经逐条核对全部是**合法项**——语法参考里的字面量/成员访问清单、
带 `params`/`TypeParam1` 占位符的骨架、以及文档中刻意标注「❌ 错误」的反例，
另有 3 块是下文所述的「已实现但未落地的语法草案」。**实际需要修的都修完了。**

### 修正的示例

| 文档 | 处数 | 主要问题 |
|-----|------|---------|
| `LANGUAGE_FEATURES.md` | 12 → 全部重写 | 整篇用 C#/Java 风格伪代码写成：`function` / `let` / `new` / `constructor` / `operator +` / `typeof` / `=` 赋值 / `start..end` |
| `ENVIRONMENT_GUIDE.md` | 3 | `var x = ...` |
| `FAQ.md` | 4 | `catch e` 缺括号、`native` 语句、`match` 缺 `case`、生成器 |
| `API_REFERENCE.md` | 8 | `for x <- list` 与 `for i in 0, i < n, i <- i+1` |
| `ARCHITECTURE.md` | 1 | `for item <- list`、`fn: func(T) -> R`、`(x) => ...` |
| `ADVANCED_TOPICS.md` | 2 | `native extern`、`switch` 用冒号分支 |
| `Old8Lang_Grammar.md` | 4 | `new Box(...)`、`extern ... -> Alias` |
| `REFLECTION_API.md` / `DEVELOPER_TOOLS.md` | 各 1 | `let`、`main() -> {` 与 `new Person()` |

期间确认并统一采用的正确写法：赋值 `<-`、函数 `func`、实例化**不用** `new`、
lambda 箭头是 `->`（**不是 `=>`**）、默认值是 `参数名: 默认值`、
函数参数的类型标注用 `Func`（但 lambda 实参不能标 `Func`，留空即可）、
取类型用 `Type(x)`（**没有** `typeof`）、`switch` 用 `case N { }` 块、
`match` 用 `case p -> e` 且通配是 `_`、范围是两端闭区间的 `[a~b]`。

### 核对中发现的既有问题（均已记入对应文档，前两项另见 [ROADMAP.md](./ROADMAP.md) 当前重点第 3 节）

- **泛型扩展方法与泛型约束未实现**：`extension list<T> { ... }` 在解析阶段即失败
  （`ExtensionParser.ParseExtensionDeclaration` 只接受非泛型目标类型名）。
  语法文档 §5.7 的整节泛型扩展示例属语法草案，已加复核说明。
- **`match` 不支持 `case 2 | 3` 这样的联合模式**（报「缺少箭头 `->`」）。
- **用户函数不能命名为 `range`**：`[a~b]` 会编译成对 `Range(...)` 的调用，
  被同名用户函数遮蔽后报「Range 函数需要至少 3 个参数」。
- **`=>` 写法的 lambda 会静默失败**：能通过解析，但参数不绑定，调用时报 `名称 'x' 未定义`。
- **Python 互操作在本机会挂起**：`extern "pymodule:math" { ... }` 后调用，30 秒无返回。
  环境为 pythonnet 3.0.5 + Python 3.13.9，而 pythonnet 3.0.x 官方支持到 Python 3.12。
  `Old8Lang/ExternProviders/PythonProvider.cs` 存在且 csproj 已引用 pythonnet，属实现/环境问题，
  **本次未修**，仅记录；相关文档示例已保证语法正确。
- `CHANGELOG.md` 顶部补充说明：本文档较早条目（各 rc 版本）中的代码片段反映当时的语法，
  其中 `native` 语句、`{ ... }` 省略占位符等在后续版本已不适用，不能直接复制运行。

## 修正 CLI 帮助文本与调试/性能分析文档 (2026-10-09)

延续当日的文档整理：核查 `CLI_GUIDE.md` 的 `profile` 一节时，发现整组「调试和性能分析命令」
（第 478–779 行，301 行）描述的接口与实现**没有一处对得上**；顺带发现 `-h` 的帮助文本本身也已失真。

### 1. `-h` 帮助文本（`Old8Lang.App/BasicInfo.cs`）

帮助里列着四个**从未注册**的命令，实测全部报 `未知命令`：

```
$ Old8Lang.App add      → 错误: 未知命令 'add'
$ Old8Lang.App info     → 错误: 未知命令 'info'
$ Old8Lang.App import   → 错误: 未知命令 'import'
$ Old8Lang.App -change  → 错误: 未知命令 '-change'
```

同时又漏掉了已注册的 `-vm`、`-compile`、`-execute`、`restore`、`pack`、`unpack`、`sign`、
`verify`、`cert`、`publish`、`env`、`debug-start`、`debug-bp`、`debug`、`profile`，
开头还只写了「解释模式 / IL 模式」两种模式。

已按 `Program.RegisterCommands` 的实际注册表重写。**验证方式**：把新帮助里列出的 26 个命令逐个
实际调用一遍，确认无一报 `未知命令`；四个幻影命令已不再出现。

### 2. `CLI_GUIDE.md` 的调试与性能分析章节（301 行 → 111 行）

| 文档原写法 | 实际 |
|-----------|------|
| `debug-start [选项] <文件>`，选项含 `-m` / `-p` / `-b, --break-on-start` | `debug-start <文件路径>`，**无任何选项** |
| `debug-breakpoint add/remove/list/clear` | `debug-bp add/func/list/remove/clear` |
| `debug-control continue/step-in/step-over/…` | `debug continue/step/stepinto/stepover/stepout/pause/stop` |
| `(old8lang-debugger)` 交互式提示符，含 `break` / `locals` / `watch` / `backtrace` / `quit` | 不存在；调试控制是独立调用的 `debug <命令>` |
| `profile [选项] <文件>`，选项含 `-m` / `-o` / `-f html` / `--samples` / `--include-memory` / `--include-gc` | `profile start/stop/status/clear`，**无任何选项** |
| 「VM 模式提供最完整的调试支持」 | 调试器与性能分析器都在**解释器侧**，虚拟机侧无可用实现 |
| 用 `debugger` 语句设置条件断点 | 语言里**没有 `debugger` 语句**，写它会报语法错误 |

### 3. 核查中确认的两处「接口存在但不可用」

- **`debug-bp` / `debug`**：要求进程内已初始化调试器，而唯一初始化入口 `debug-start` 会把文件
  **同步执行到结束**才返回，因此断点与单步来不及生效；独立执行时报 `错误: 调试器未初始化`。
  断点与单步的**引擎能力是真实且有测试覆盖的**（`Old8Lang.Tests/Debugger/` 下 22 个用例通过，
  执行路径会经 `DebuggableInterpreter` 回调），缺的是把它们串起来的 CLI 工作流。
- **`profile`**：没有任何执行路径调用 `ProfilerManager.RecordFunctionStart` / `RecordFunctionEnd`，
  因此 `profile stop` 的函数调用总数恒为 0，永远输出「性能分数 100.0/100 (A) · 未发现明显性能瓶颈」；
  且 `ProfilerService` 是进程内静态单例，跨进程的 `start` 与 `stop` 不共享会话。

两处均**未修改实现**（超出文档范围），但已在 `CLI_GUIDE.md` 中以醒目提示写明，
并指向真正可用的 `--perf` 系列参数。

### 4. 顺带发现（未处理）

`Program.Main` 里有一段 `#if DEBUG` 的无参启动代码，硬编码了一个 Windows 绝对路径
（`C:\Projects\RiderProjects\Old8Lang\test_langlist_conversions.old8`）并直接以 `-vm` 执行。
它使 **Debug 构建下无法进入交互式命令行模式**（无参启动会命中该分支）。
## 文档：性能文档三合一，ARCHITECTURE §14 改为指针 (2026-10-09)

`PERFORMANCE_GUIDE.md`（768 行）与 `PERFORMANCE_OPTIMIZATION.md`（591 行）**标题都叫
「Old8Lang 性能优化指南」**，读者无法判断该看哪份；`PERFORMANCE_BEST_PRACTICES.md`（115 行）
是第三份且全仓库无人引用；`ARCHITECTURE.md` 的 §14「性能优化架构」（344 行）则与
`PERFORMANCE_OPTIMIZATION.md` 几乎逐字重复——同样的 `126ms/32ms/30ms/4.87MB` 数字、
同样的零拷贝/内存池/字符串缓存/`ParserPerformanceMetrics` 说明。

### 变更

- 三份合并为 `Docs/PERFORMANCE_GUIDE.md`，分「第一部分 · 写代码」与「第二部分 · 运行时内部优化」。
  合并后 1056 行（原三份合计 1474 行），删去的部分是重复的执行模式选择、性能监控 CLI 命令
  与内存管理建议（三份各写了一遍）。
- `ARCHITECTURE.md` §14 由 344 行压缩为 13 行：保留一层「解析器 / 解释器 / 监控」的技术与实现位置对照表，
  细节指向 `PERFORMANCE_GUIDE.md`。
- 顺带修掉合并范围内跑不通的示例（该文件代码块通过率 **10/34 → 33/33**）：
  - `let x = 0` → `x <- 0`、`function f()` → `func f()`；
  - `for (let i = 0; i < n; i = i + 1)` → `for i <- 0, i < n, i++`；
  - `for i in 0..1000` → `for i in [0~1000]`（含以变量为边界的 5 处，共 21 处）；
  - `Add(list, x)` → `list.Add(x)`、`Contains(list, x)` → `list.Contains(x)`、
    `Join(parts, sep)` → `parts.Join(sep)`、`Count(data)` → `data.Count()`——
    这四个都是**列表实例方法**，文档此前当成全局函数调用；
  - `--profile` 不存在（`-f` 只认 `--perf`/`--perf-detailed`/`--perf-output`），
    交互式会话里另有 `profile start` 命令；
  - 删除了用 `GetCurrentTimeMs()` 计时的示例——**该函数不存在**，改为展示 `--perf` 的实测输出。

### 关于「优化前后」的数字

旧文档给过一组优化后快照（500/3000/5000 行 → 126/32/30 ms）。2026-10-09 用 `-s` 冷启动口径
复测约为 **41/53/64 ms**，与那组数字差异很大，无法互相印证（测量口径、样本、是否预热均不明）。
合并后的文档不再照抄历史快照，改为给出「以本机 `Old8Lang.Benchmarks` 现场测量为准」并说明差异。

### 引用更新

`DEVELOPMENT_WORKFLOW.md`、`README.md` 中的路径已指向合并后的 `PERFORMANCE_GUIDE.md`。
## 文档：模式支持矩阵合并为一份 (2026-10-09)

`MODE_COMPLETION_STATUS.md`（486 行）与 `Mode_Support_Summary.md`（295 行）写的是同一件事——
三种执行模式的特性可用性。两份是 2026-10-08 同一次复核的产物，互相链接、共享同一批实测数据，
但已经出现互相矛盾的单元格：`Mode_Support_Summary.md` 的虚拟机列仍把泛型类、函数装饰器标为 ❌，
而 `MODE_COMPLETION_STATUS.md` 标 ✅。`MODE_COMPLETION_STATUS.md` 自身也不自洽——功能表把闭包写回
标为 ⚠️「不可用」，同一份文件的已知限制却写着「自 2026-10-09 起支持」。

### 变更

- 合并为 `Docs/MODE_SUPPORT.md`，明确为模式可用性的唯一权威说明。
- 合并前逐格 diff 两份表，对 7 处冲突单元格重新编写用例，在 `-f` / `-il` / `-vm` 下实测裁决：

  | 功能 | 旧标记 | 实测结论 |
  |-----|-------|---------|
  | 泛型类（虚拟机） | 两份冲突（❌ / ✅） | ✅ `Box<int>()` + `set`/`get` 得 `42` |
  | 函数装饰器（虚拟机） | 两份冲突（❌ / ✅） | ✅ `@twice` 包装的 `inc(10)` 得 `12` |
  | Match 表达式（虚拟机） | ⚠️「无法判定」 | ✅ 三种模式均输出 `two`（旧文档示例漏了 `case` 关键字） |
  | 异步生成器（虚拟机） | 两份冲突（❌ / ✅） | ❌ 报 `STATE_ERROR` 求值栈为空（`IteratorMoveNext`） |
  | `async for-in`（虚拟机） | 两份冲突（❌ / ✅） | ❌ 与异步生成器是同一处缺陷 |
  | 闭包写回外层局部变量（虚拟机） | 同文件自相矛盾（⚠️ / ✅） | ✅ 闭包内两次写回后外层读出 `2` |
  | 闭包写回外层局部变量（解释器 / IL） | 两份均未列 | ❌ lambda 写回得 `0`；**具名嵌套函数**三种模式都得 `2` |

- 补齐旧文档 5 处标为「本次未单独验证」的单元格：`#undef`、`#if`/`#elif`/`#else`/`#endif`、
  条件表达式（`&&` `||` `!`）、命令行 `-D` 在虚拟机下均实测通过。
- 顺带修正 `-D` 的用法：**必须写在文件名之后**（`-f file.old8 -D SYMBOL`），
  `FromFileCommand` 把 `args[0]` 当文件名，`-D` 放在前面会被当成源代码。

### 顺带发现的既有问题（未修，已记入 `MODE_SUPPORT.md` 的已知限制）

- 虚拟机模式下异步生成器与 `async for-in` 不可用（`STATE_ERROR` 求值栈为空，指令 `IteratorMoveNext`）。
- 解释器与 IL 模式下 **lambda** 写回外层变量不传回（虚拟机传回），而具名嵌套函数三种模式都传回。
- `new` 关键字三种模式都不存在，但 `LANGUAGE_FEATURES.md` 与 `Old8Lang_Grammar.md` 的示例在用它
  （解析报 `无法识别的主表达式类型 'New'`）。

### 核查中发现的死代码

合并时逐项验证「其他特性」表里的四项虚拟机专属能力，发现三项与文档不符：

| 项 | 文档原标记 | 实况 |
|----|-----------|------|
| `VMDebugger`（调试器） | 虚拟机 ✅ | **全仓库无调用点**。真正的调试器是解释器侧的 `Old8Lang.Debugger.Debugger` + `DebuggableInterpreter`，入口 `debug-start` |
| `VMProfiler`（性能分析器） | 虚拟机 ✅ | **全仓库无调用点**。真正的分析器是解释器侧的 `ProfilerManager`，入口 `profile start` |
| `Disassembler`（字节码反汇编器） | 虚拟机 ✅ | **全仓库无调用点** |
| `BytecodeFile`（字节码序列化） | 虚拟机 ✅ | 属实：`-compile` 产出、`-execute` 载入，有 CLI 与测试覆盖 |

原表的 ✅ 是照抄类名而非验证可达性，已按实况修正。三个死代码类**未删除**（改动源码超出文档范围），
但已在 `MODE_SUPPORT.md` 的已知限制中记录。

### 顺带修正的命令名与参数

- `compile-bytecode <file> -o <out>` / `execute-bytecode <out>` → 实际是
  `-compile <输入.old8> <输出.o8c>` / `-execute <文件.o8c>`（见 `-compile -h`）。
  出现在 `ARCHITECTURE.md`、`CLI_GUIDE.md`、`PERFORMANCE_GUIDE.md` 共 4 处。
- `--vm <file>` 不存在，只有 `-vm`。
- `-f ... --profile` 与 `-f ... -p` 都不存在（`-f` 只认 `--perf`/`--perf-detailed`/`--perf-output`；
  `-p` 只在 `cert`/`publish`/`sign` 里是 `--password` 的简写）。出现在 `PERFORMANCE_GUIDE.md` 与 `FAQ.md`。
- `ARCHITECTURE.md` 与 `CLI_GUIDE.md` 中「VM 模式功能已完整实现」的表述与实测不符，改为指向已知限制。

### 引用更新

`ROADMAP.md`、`README.md`、`API_Mode_Comparison.md` 以及两处源码注释
（`BytecodeVisitor.Expressions.Basic.cs`、`BytecodeVisitor.Values.cs`）中的路径已改为 `MODE_SUPPORT.md`；
`CHANGELOG.md` 中指向「虚拟机已知限制」的指引也已改指新文件。
本文件 2026-10-08 的条目里叙述当时对旧文件做了什么的句子保留原样，那是当时的事实记录。
## 虚拟机闭包按引用捕获、字典遍历对齐、区间常量池下标修复 (2026-10-09)

起因是复核「文档标为支持、却没测过」的空白：虚拟机侧没有闭包写捕获用例、没有字典
`for ... in` 用例、extern 只有 3 个「能调通」用例。补齐这些测试时暴露出下面几处缺陷。

### 修复

- **闭包改为按引用捕获（共享单元）**：`MakeClosure` 原先把外层局部变量的当前值快照进闭包
  环境，导致外层在闭包创建之后再赋值时闭包读不到新值，闭包内的赋值也无法传回外层。
  现在外层帧的槽位会就地装箱成 `UpValueCell`，闭包与外层共用同一个盒子，双向可见。
  由此**闭包写回外层局部变量**（此前报 `VM_UNSUPPORTED_ERROR`）成为可用能力。
  未涉及闭包的函数不产生盒子，`LoadLocal`/`StoreLocal` 只多一次类型判断。
  相关：`Bytecode/Closures/UpValueCell.cs`、`ClosureEnvironment`、`MakeClosure` 处理器、
  `BytecodeCompiler.CompileFunction`（捕获变量不再复制成函数局部变量，改由环境按名解析）。
- **嵌套具名函数支持捕获外层变量**：`VisitFuncInit` 此前对所有具名函数一律
  `MakeFunction` + `StoreGlobal` 且不做捕获分析，于是函数体里的外层局部变量被当作全局名查找，
  运行时报 `名称 'x' 未定义`、位置还是无意义的 `0:0`。现在与 lambda 走同一套捕获分析与
  `MakeClosure`，并新增 `FunctionMetadata.NeedsClosureEnvironment`（字节码格式升至 **1.2**）
  让调用点回退到绑定查找以取到闭包环境。
- **循环变量按迭代重新绑定**：共享单元会让同一循环里创建的闭包全都读到循环变量的最终值
  （`for i in [0~2] { fs.Add(() -> i) }` 得到 `2,2,2`）。新增 `RefreshLocalBinding` 指令，
  在每轮迭代写入新元素前（C 风格循环则在增量之前）换一个新盒子，使每个闭包捕获当轮的值，
  与解释器一致（`0,1,2`）。
- **区间表达式取错常量池槽**：`VisitRangeLangValue` 把 `LoadConst` 的操作数当字面值用
  （`Emit(LoadConst, 0/1)`），而它其实是常量池下标 —— 等于假定槽 0 放着 `0`、槽 1 放着 `1`。
  只要此前有别的常量占用这两个槽就会取错，典型触发是 `extern` 块先放入库名与函数名：
  `extern "..." { func abs(...) }` 之后的 `for i in [1~3]` 会把布尔标志读成字符串 `"abs"`，
  报 `String 'abs' was not recognized as a valid Boolean`。现改为先入池再按下标加载，
  布尔标志改用 `LoadTrue`/`LoadFalse`。
  连带修正：`[5~5]` 此前因 `includeStart` 被误读为 false 而变成空区间，现为 `[5]`，
  与解释器/IL 一致。
- **字典遍历与解释器对齐**：`NewDict` 用栈顶弹出顺序写入 `Dictionary`，使插入顺序变成逆序；
  `GetIterator` 又把字典特判成 `dict.Keys`，单标识符只拿到键。现在 `NewDict` 保序、
  迭代元素统一为 `(键, 值)` 元组，三种模式的单变量绑定语义与遍历顺序完全一致。
  `__for_in_unpack` 增加「元组长度恰好等于解构元数时按位取」的判定，避免字典的值本身是
  元组时被展平（顺带修正 `for a, b in [(1, (2, 3))]`：此前虚拟机得到 `b=2`，解释器是 `b=(2, 3)`）。

### 测试

- `VirtualMachine/Functions/VMClosureTests.cs`（26 例）：闭包读/写捕获、跨层捕获、闭包工厂、
  循环捕获、嵌套具名函数捕获，以及与解释器的一致性对照。
- `VirtualMachine/Extern/VMExternConventionTests.cs`（16 例）：三种调用约定（块级与单函数级）、
  批量与单函数导入、别名、多块并存、int/long/double 转换、重复调用的委托缓存、
  缺库报 `IO_ERROR`、缺符号报 `METHOD_NOT_FOUND_ERROR`。全部跨平台，不用「非 Windows 就提前
  return」那种会让用例在 macOS/Linux 上静默通过的写法。
- `VirtualMachine/Collections/VMDictionaryTests.cs`：补 11 例字典遍历（含两模式一致性 Theory）。
- `VirtualMachine/Expressions/VMRangeTests.cs`：`Range_InListComprehension_ExecutesCorrectly`
  函数体原是普通 for 循环、与用例名不符，改为真正的列表推导式。
- `VirtualMachine/ErrorHandling/VMUnsupportedFeatureTests.cs`：把断言「闭包不能写回外层局部变量」
  的用例改写为断言写回生效，并补嵌套具名函数的读/写捕获。

### 已知限制（本次未处理）

- **脚本顶层的 C 风格 for 循环变量**：`for i <- 0, i < 3, i++` 的变量在顶层被声明为全局变量，
  循环体内创建的闭包读到的是最终值（`333`），解释器为 `012`。此分歧在本次改动之前就存在。
  函数内的同种循环已对齐。
- **解释器侧 lambda 写回不一致**：解释器对 lambda 的写回不传回外层
  （`func make() { c <- 0; inc <- () -> { c <- c + 1 }; inc(); print(c) }` 输出 `0`），
  而同等的具名嵌套函数却传回 `1`。虚拟机两种写法都传回。解释器侧待统一。
- **虚拟机运行期错误位置恒为 0:0**：`Instruction.LineNumber/ColumnNumber` 在整个编译器里
  从未被写入（带调试信息的 `Emit` 重载与 `Instruction.WithDebugInfo` 全仓库零调用），
  因此运行期错误的位置信息与源代码上下文都不可用。这是牵涉面较广的独立改动，本次未实施。

## 编译模式更名为 IL 模式 (2026-10-08)

「编译模式」这个名称名不副实：它做的是把 Old8Lang 编译成 IL（中间语言）再交给 .NET 运行时执行，
而虚拟机模式同样在做编译（编译成字节码）。两个模式都叫“编译”容易混淆，现统一更名为「IL 模式」。

### 变更

- **命令行**：新增 `-il`，`-c` 保留为等价别名，既有脚本无需修改。
  例：`Old8Lang.App -il script.old8`。
- **项目配置**：`o8package.json` 中 `runtime` 的取值由 `compiler` 改为 `il`；
  旧值 `compiler` 继续识别，既有项目配置无需修改。
- **术语统一**：文档、命令行帮助、代码注释与异常信息中的「编译模式 / 编译器模式」全部改为
  「IL 模式」，例如 `List.Single 方法暂不支持 IL 模式`。断言这些文案的单元测试已同步更新。

### 未变更

- C# 命名空间与类型名保持原样（`Old8Lang.Compiler`、`CompilerVisitor`、`CompilerCommand`）。
- 测试目录 `TestFiles/CompilerTests/`、`Old8Lang.Tests/Compiler/` 未改名。

## 虚拟机模式静态类 API 支持：Task / Thread / Assert (2026-10-08)

虚拟机模式下 `Task.Delay(...)`、`Assert.Equal(...)`、`Thread.Sleep(...)` 此前直接报
`VM_UNSUPPORTED_ERROR`。原因是解释器把这三个静态类注册成运行期全局对象、由各自的
`Dot(Instance, manager)` 分发，而虚拟机没有这套对象。本次建立了一套通用的静态类分发机制。

### 实现

- **编译期改写**（`BytecodeVisitor.StaticClasses.cs`）：`类名.方法(参数)` 被改写成带限定名的
  原生调用（如 `CallNative { argCount, "Task.Delay" }`）。拦截点在 `VisitOperation` 的 Dot 分支，
  且必须在访问左操作数之前——`Task.Delay(100)` 的语法树是
  `Operation{ Dot, Left=LangId("Task"), Right=Instance("Delay",[100]) }`，按普通成员访问走
  只会在 `VisitLangId` 里报“名称 'Task' 未定义”。
- **运行期分发**（`VirtualMachine.Helpers.StaticClasses.cs`）：在 `CallNativeFunction` 查全局函数
  注册表**之前**按限定名分发，这样 `Old8Exception` 能原样透传，不被注册表分支的兜底包装改写。
- **唯一真源**（`Bytecode/VmStaticClassRegistry.cs`）：编译期与运行期共用同一张
  “类 → 方法”表，编译期据此决定能否改写、不支持时如何报错，运行期据此找实现。
- **复用解释器实现**：`TaskClassLangValue` / `ThreadClassLangValue` / `AssertClassLangValue`
  各自新增 `internal` 的 `VmReusableMethods` 表，登记只依赖 `LangValueType` 的既有实现
  （`Task.Delay`/`WhenAll`/`WhenAny`/`FromResult`/`FromException`、`Thread.Sleep`、
  `Assert` 的 16 个方法）。语义不同或需要访问虚拟机的由虚拟机自己实现。

### 支持矩阵

- **Task**：`Delay`、`WhenAll`、`WhenAny`、`FromResult`、`FromException`、`Run`、`StartNew`
- **Thread**：`Sleep`
- **Assert**：24 个方法（短名与 `AssertXxx` 长名都接受）
- **不支持**（报 `VM_UNSUPPORTED_ERROR`，并列出该类支持的方法）：裸引用 `Task`、`Task.Factory`、
  `Thread.CurrentThread` / `Delay` / `WhenAll` / `WhenAny`、静态类方法使用命名参数、
  `Task.Delay` 的第二个参数；`TestRunner` / `Mock` / `TaskScheduler` / `TaskCompletionSource` /
  `CancellationTokenSource` 仍然完全不可用。

### 顺带修复的问题

- **虚拟机容器指令补上 `ILangList` 分支**（`GetIndex`/`SetIndex`/`GetIterator`/`GetCount`）：
  `ILangList` 既不是 `IList` 也不是 `IEnumerable`，所以 `await Task.WhenAll(...)` 返回的
  `ListLangValue` 落到求值栈上无法索引、无法 `for-in`。此前没有任何虚拟机路径产出这类值，
  缺口一直没被触发。
- **`t.Wait()` / `t.Await()`**：`TaskAwaitMethod` 的虚拟机实现只认 `Task<object>`，
  而栈上放的是 `TaskLangValue`，因此报“实例必须是 Task<object> 类型”。现在两种都支持，
  与已可用的 `t.Result` / `t.IsCompleted` / `t.Status` 对齐。
- **断言的相等语义**：解释器的 `AreEqual` 在遇到不认识的类型时会退回比较 `ToDisplayString()`，
  而虚拟机类实例的 `ToString()` 只含类名，导致 `Assert.Equal` 对同一类的两个不同实例**假通过**
  （`Assert.NotEqual` 假失败）。虚拟机改用自身语义：没有等价 `LangValueType` 表示的对象
  （类实例、函数值）走虚拟机的 `Equals`（认 `_eq` 重载，否则引用相等），其余值换算到解释器
  口径后沿用 `AreEqual`，从而 `Assert.Equal(42, await task)`、列表逐元素比较都能正确判等。
- **断言失败改为抛 `AssertionError`**：解释器用普通 `Exception` 表示断言失败，语言层的
  `try`/`catch` 都捕获不到它（这是解释器侧的既有行为）；虚拟机统一成 `AssertionError`
  （`Old8Exception` 子类），因此**虚拟机下 `try { Assert.Equal(1, 2) } catch { ... }` 能捕获到，
  而解释器下不能**——这是有意的跨模式差异，错误码也从通用的 `RUNTIME_ERROR` 变成 `ASSERTION_ERROR`。
- **`Assert.Throws` 改按异常类型识别自身失败**：解释器按消息前缀 `断言失败:` 判断，
  会让 `Assert.Throws(() -> Assert.True(false))` **假通过**。虚拟机改为 `ex is AssertionError`。
- **`Assert.Throws` 的求值栈回滚**：被断言的调用在表达式求值中途抛异常时，
  `ExecuteFrame` 的 finally 只归还 locals 与帧、不回滚求值栈，会把中间值留在栈上污染调用方。
  现在按 `ExecuteFunction` 的既有范式做快照与回滚。
- **`Assert.InstanceOf` 的类型名口径**：解释器是 `Int`/`List`/`Dictionary`，虚拟机的
  `GetValueTypeName` 是小写 `int`/`list`/`dict`。虚拟机改用解释器口径并做大小写不敏感比较，
  按解释器写好的断言在虚拟机下同样成立。

### 测试

- 新增 `VMTaskStaticApiTests`（14 条）、`VMStaticClassAssertTests`（20 条）、
  `VMThreadStaticApiTests`（3 条）、`Performance/VMStaticClassTimingTests`（2 条，Performance 档）。
- 改写 `ErrorHandling/VMUnsupportedFeatureTests` 中原本断言 `Task.Delay`/`Assert.Equal`/`Thread.Sleep`
  报不支持的三条用例，改为断言不支持的形式（裸引用、`Task.Factory`、`Thread.CurrentThread`、
  命名参数、`TaskScheduler`）。
- 虚拟机测试 933 → 972 通过 / 0 失败；解释器 1943、Parser 881、Unit 147 均无变化。

### 未修复（本次发现的既有缺口，与本改动无关）

- `func f() -> int { return await g() }` 报 `类型不匹配: 期望 int，但得到 IntLangValue`
  ——纯 `async`/`await` 加返回类型注解就会触发，`g` 是普通异步函数也一样。
  `CheckTypeMatch` 只按原始 CLR 类型匹配，不换算 `LangValueType`。已记入
  `MODE_SUPPORT.md` 的虚拟机已知限制。

## 虚拟机模式函数装饰器支持 (2026-10-08)

装饰器此前在虚拟机模式下**静默失效**：`@twice` 包装的 `inc(10)` 返回 `11` 而不是 `12`。
原因有两处，各自独立地让装饰器失去作用；两处都在本次修复。

### 实现修复

- **包装闭包捕获不到被装饰的函数**：`func deco(f) { return (x) -> f(x) * 2 }` 里的
  `f(x)` 是调用表达式，而 `ClosureCaptureAnalyzer` 只认字面量标识符与二元运算，
  调用表达式（`Instance` / `FunctionCallExpression`）整棵子树都没遍历到，
  形参 `f` 因此没进捕获列表。闭包生成时 `f` 不是局部变量，`f(x)` 被编译成按名静态调用，
  运行期报 `方法 'f' 未找到`。现在捕获分析会遍历调用表达式的被调名与实参。
  同一处还补了两条规则：
  - 点号右侧（`obj.field` / `obj.method()`）是成员名而不是变量引用，不再当作捕获项。
    此前 `(x) -> s.ToUpper() + x` 会为一个并不存在的变量 `ToUpper` 建捕获项，
    闭包创建时直接报“名称 'ToUpper' 未定义”。
  - 外层闭包只捕获**变量**（外层局部变量、全局变量、外层捕获变量）。
    函数名、类名、成员名这类名字各自由 `Call` / `NewObject` / `CallMethod` 解析，
    此前被一并收进捕获列表后会在闭包开头生成一句 `LoadGlobal 名字`，
    把一个本来能跑的闭包变成运行期报错（如 `(x) -> Print(x)`）
  - 嵌套闭包自己声明的名字（形参、内部赋值建立的局部变量）不再泄漏成外层的捕获项。
    此前内层闭包里的 `result <- f(x)` 会让外层凭空多出一个局部变量 `result`，
    内层随后在“闭包写外层局部变量”的检查里命中它而误报 `VM_UNSUPPORTED_ERROR`。
- **按函数名调用绕过运行期绑定**：装饰器把包装后的函数写回同名全局变量，
  但 `TryResolveCallableFunction` 先走「函数索引命中即直达函数体」的捷径，
  直达的是未包装的函数体。现在带装饰器的函数（`FunctionMetadata.IsDecorated`）
  会跳过这条捷径，改从全局绑定解析；装饰器返回非函数值时明确报 `TypeError`
  （此前会悄悄退回原函数，用户只看到装饰器“不生效”）。
- **异步函数的装饰器**：`@decorator async func` 在虚拟机模式下显式报
  `VM_UNSUPPORTED_ERROR`。异步函数不走 `MakeFunction`/`StoreGlobal` 绑定路径，
  装饰器无法生效，此前是被静默丢弃的。
- **字节码文件格式次版本号 1.0 → 1.1**：`FunctionMetadata` 新增 `IsDecorated` 字段。
  读取时要求次版本号不低于当前值——函数元数据按字段顺序定长写入，
  旧文件继续按新布局解析会读出垃圾数据而不是报错。旧 `.o8bc` 文件需要重新编译。

### 测试

- 新增 `Old8Lang.Tests/VirtualMachine/Functions/VMDecoratorTests.cs`（23 条）：
  无参/带参（含命名参数与表达式参数）/多参/无参函数/多层堆叠应用顺序/
  返回值类型改变/递归函数/缓存装饰器（字典 + 全局计数）/闭包访外部变量/
  从普通函数与闭包内调用被装饰函数/以函数值传递/嵌套函数上的装饰器/
  序列化重载后装饰器仍生效/装饰器返回非函数与装饰器未定义时的报错/异步装饰器报不支持。
- 虚拟机全量：**933 通过 / 0 失败**（含上述 23 条新用例）。
- 非 IL 模式全量（解释器/解析器/标准库/语言服务器/虚拟机）：**4661 通过 / 0 失败 / 3 跳过**。
- IL 模式仍是既有的 IL 校验失败，与本次改动无关。

## LSP 位置基准统一 (2026-10-08)

`SourcePosition` / `LangToken` 的约定是**行号 1 起始、列号 0 起始**（列号即相对行首的偏移量），
这与 LSP 的 `Position.Line`（0 起始）不完全相同，但与 `Position.Character`（0 起始）完全一致。
语言服务器内部此前对列号的假设分成两派：符号表构建、定义/引用/重命名/高亮按 0 起始使用（正确），
而折叠、语义高亮、诊断、作用域分析、签名帮助又多做了一次 `- 1`（把它当成 1 起始），
导致这些出口整体左偏一列；另有符号表回退路径与文档链接漏掉了行号转换。

本次统一为「**列号原样使用、行号减 1**」，修的是各消费端，没有改动词法器基准——
翻转词法器会连带打断 36 个按位置匹配的既有用例（hover/定义/重命名/引用），因此不动。

### 实现修复

- **列号多减 1**（整体左偏一列，列号为 0 的记号会算成 -1）：
  `FoldingRangeHandler`（折叠区起止列）、`SemanticTokensHandler`（语义标记起点）、
  `TextDocumentSyncHandler`（诊断范围起点）、`ScopeAnalyzer`（16 处符号位置）、
  `SignatureHelpHandler`（光标定位最后落在哪个记号）、`SemanticAnalyzer`（重复定义诊断多加的 1）。
- **行号漏减 1**：`SymbolTableBuilder` 的 AST 回退路径（24 处，函数/异步函数/类/方法/属性/变量/
  native/extern/import，另外函数参数因为查不到对应记号而必然走这条路径）、
  `DocumentLinkHandler` 的 import 范围、`MemberChainAnalyzer` 里按 0 起始的光标列。
- **语义高亮此前完全不产出标记**：`TokenTypes` 图例数组被声明成 `string[]`，
  `Array.IndexOf(TokenTypes, SemanticTokenType.X)` 因此绑定到非泛型的 `Array.IndexOf(Array, object)`，
  用 `Equals` 比较 `string` 与 `SemanticTokenType` 恒为 false，索引全部是 -1，所有记号都被丢弃。
  改为 `SemanticTokenType[]` 后恢复正常（修复列号偏移后，标记覆盖的源码片段与记号逐一对应）。
- `SourcePosition` 的文档注释改写为明确约定（原先只写了「基准不一致，使用方自行注意」）。

### 测试

- 新增 `Old8Lang.Tests/LanguageServer/PositionBasisTests.cs`：把词法器约定、折叠起止列、
  语义标记覆盖的源码片段、符号表位置、作用域符号位置、实际发布的诊断范围逐条钉死。
  其中 5 条在修复前失败、修复后通过。
- 已有的折叠与语义高亮用例此前只断言数量（`Count >= 3`、`Assert.NotNull`），
  这正是偏移长期没被发现的原因；新用例改为精确断言。
- 语言服务器全量：**445 通过 / 0 失败 / 2 跳过**（此前 438，新增 7）。
- 非 IL 模式全量：4637 通过 / 1 失败 / 3 跳过，失败的是既有并发用例
  `VMConcurrencyCyclicBarrierTests.CyclicBarrier_PhaseCoordination_ExecutesCorrectly`（单独跑同样失败）。
- IL 模式仍是既有的 IL 校验失败（`Common Language Runtime detected an invalid program`），与本次改动无关。

## 文档与实现对齐修复 (2026-10-08)

本次按三种执行模式的**实际运行结果**复核并修正了文档中的支持矩阵，同时修复复核过程中
暴露的实现问题。注意：本节之前的最后一条记录停留在 2024-02-14，中间版本的变更没有记录在案。

### 实现修复

- **解析器**：省略 `func` 关键字时 `-> 返回类型` 无法解析，
  导致 `public greet() -> string { ... }` 报 `无法识别的主表达式类型 'Arrow'`。
  现在四种函数声明写法可以自由组合（受影响的有文档 §5.6.1、§8.5.6 及运算符重载示例）。
- **错误位置报告**：
  - 渲染器用「错误行号 − 上下文长度/2」反推窗口首行行号，窗口在文件首尾被裁剪时行号标注与
    脱字符会整体错位；现改为随上下文一并传递起始行号。
  - 上下文切分使用了 `RemoveEmptyEntries`，源码中的空行被丢弃，标注随之错位；现保留空行。
  - `SourcePosition.Column` 实际是 0 起始偏移，展示时未转换，导致脱字符偏移一列、显示列号小 1；
    现展示统一为 1 起始。
  - EOF 标记的行号被写成了标记下标，在文件末尾报错时会显示无意义的行号；现取自最后一个真实标记。
- **虚拟机列表推导式**：`VisitListComprehension` 原为空实现（直接 `return null`），
  表达式不产出字节码导致栈失衡，最终在无关指令处报 `Stack empty`。现已实现。
- **虚拟机 `for key, value in dict`**：字节码访问者只绑定首个标识符，第二个变量报「名称 'v' 未定义」。
  现支持多标识符解构（字典按键取值，元组/列表按元素解构）。
- **虚拟机静默空实现改为明确报错**：`BytecodeVisitor.Values.cs` 中原有 30 处 `=> null` 桩
  （Task/Thread/Assert/Mock/Type/CancellationToken 等）。这些桩不产出字节码，报错信息完全不指向
  真实原因。现改为 `VM_UNSUPPORTED_ERROR` 并给出位置与特性名。
- **虚拟机闭包写回外层局部变量**：按值快照捕获无法支持写回，此前报「名称 'c' 未定义」，
  现报 `VM_UNSUPPORTED_ERROR` 并说明原因；只读捕获与写回全局变量不受影响。
- **虚拟机求值栈下溢的诊断**：`Stack empty` 现在会指出失败的指令与所在函数，
  并提示常见成因（把没有返回值的调用当作值使用）。缺陷本身尚未修复。
- **元组解构赋值**：`(a, b) <- (1, 2)` 在虚拟机下因 `LoadConst` 误用常量池下标而报索引越界，已修复。
- **解释器单变量遍历字典**：绑定前未对迭代元组求值，`for k in dict` 得到空元组 `()`，已修复。

### 文档修正

- `Old8Lang_Grammar.md`：120 处 `**模式支持**` 标记按实测结果复核，逐条附「复核说明」。
- `MODE_COMPLETION_STATUS.md`：修正支持矩阵中「标低了」与「标高了」两类错误，
  新增复核说明、修正条目表、实测测试数据，并重写虚拟机模式的已知限制。
- `Mode_Support_Summary.md`：虚拟机模式一列大量滞后（已可用特性仍标 ❌），按实测大幅修正。
- `API_Mode_Comparison.md`：补充标准库导入调用在编译/虚拟机模式不可用的实测结论。

据此修正的主要条目：运算符重载（虚拟机 ❌→✅）、函数装饰器（编译/虚拟机 ⚠️→❌）、
Task API 与 Thread API（虚拟机 ✅→❌）、原生库导入（编译/虚拟机 ✅→❌）、
以及元组/范围/可空类型/泛型集合/联合与交叉类型/切片/类型转换/字符串模板/defer/select/
Lambda/异步（虚拟机 ❌→✅）。

### 测试

- 新增 5 组回归用例：函数声明写法解析、错误上下文渲染、虚拟机列表推导式、
  `for-in` 多标识符解构、虚拟机不支持特性的报错。
- 非 IL 模式全量：**4562 通过 / 0 失败 / 3 跳过**。
- IL 模式在本机存在既有的 IL 校验失败（`Common Language Runtime detected an invalid program`），
  737 个既有失败，与本次改动无关。

## 虚拟机求值栈一致性与 spawn 文档修正 (2026-10-08)

### 实现修复

- **虚拟机：统一"一次调用在求值栈上恰好留下一个值"的调用约定**。
  此前无返回值的用户函数（`ReturnVoid` 或函数体执行到末尾）与原生函数都不向求值栈压入任何值，
  而调用方按值位置消费，于是 `r <- f()`（f 无返回值）、`print(f())`、`r <- Sleep(1)` 等写法
  会读到空栈，报出与真实原因无关的 `Stack empty`。
  现改为：无返回值的调用补一个 `VoidLangValue` 占位（与解释器语义一致，
  `r == null` 为 false、`print(f())` 不输出内容）；对应地，语句位置等**丢弃返回值**的调用补 `Pop`
  （`FuncRunStatement`、类实例化的构造函数调用）。
- **解释器：修复函数返回值的残留**。函数走末尾未执行 `return` 时，会返回上一次调用留在
  变量管理器上的返回值（例如先 `f(true)` 得到 42，再 `f(false)` 仍得到 42）。现在返回 void。
- 求值栈下溢的报错信息会指出失败的指令与所在函数（此前只报 `Stack empty`）。
- 虚拟机反射调用实例方法时的 `TargetInvocationException` 不再吞掉真实原因，
  内部异常会带着原始调用栈重新抛出（此前只显示 `Exception has been thrown by the target of an invocation.`）。

### 残留差异

- **原生函数无法区分"无返回值"与"null"**：用户函数无返回值时虚拟机补 `VoidLangValue` 占位，
  与解释器一致；但原生/内置函数（`Sleep`、`PrintLine` 等）统一返回 C# `null`，
  虚拟机无法区分二者，因此 `print(Sleep(1))` 在虚拟机输出 `null`（解释器不输出）、
  `r <- Sleep(1)` 后 `r == null` 为 true（解释器为 false）。
  彻底对齐需要各原生函数分别返回 `VoidLangValue` / `NullLangValue`，本次未做。

### 文档修正

- 语法文档 §5.10 多线程编程：明确 `spawn` **只创建线程、必须再调用 `Start()`** 才会执行，
  之后用 `Join()` 等待并取回线程函数返回值；原示例 `t <- spawn(worker(1))` + `t.Join()`
  在三种模式下都无法运行（参数形式错误且缺少 `Start()`）。新增返回值与闭包两种完整示例。
- 支持矩阵中 `spawn` 一行由 ⚠️ 更正为 ✅（三种模式行为一致，实测通过）。
- 更正"虚拟机不支持 `Thread.Sleep`"的说明：无 `Thread.` 前缀的全局函数 `Sleep(...)` 三种模式均可用。

## Old8Lang 1.0.0 rc9 - 文档更新 (2024-02-14)

### 文档系统全面更新

本次更新对 Old8Lang 文档进行了全面改进，涵盖三种执行模式、最新架构和完整 API 参考。

#### 三种执行模式文档化

**新增内容**:
- 详细的三种执行模式对比（解释模式、IL 模式、VM 模式）
- 每种模式的特点、性能、适用场景和命令参数
- 模式选择指南和最佳实践
- 可运行的代码示例（每种模式至少1个）

**更新文件**:
- `CLAUDE.md` - 添加执行模式对比表和处理流程图
- `Docs/ARCHITECTURE.md` - 新增"执行模式"章节（1.3）
- `Docs/CLI_GUIDE.md` - 新增"执行模式对比"章节
- `Docs/PERFORMANCE_GUIDE.md` - 新增"执行模式性能"章节
- `Docs/LANGUAGE_FEATURES.md` - 新增"模式特定功能"章节

**模式对比表**:
| 特性 | 解释模式 | IL 模式 | VM 模式 |
|------|---------|---------|---------|
| 启动速度 | 快 | 慢（需编译） | 中等 |
| 运行性能 | 中等 | 高 | 中等偏高 |
| 类型系统 | 动态类型 | 静态类型 | 动态类型 |
| 泛型支持 | ✅ | ❌ | ✅ |
| 运算符重载 | ✅ | ❌ | ✅ |
| Python 互操作 | ✅ | ❌ | ✅ |
| 调试支持 | 基础 | 基础 | 高级 |
| 跨平台分发 | 需源代码 | 需源代码 | ✅ 字节码 |

#### 架构文档更新

**新增章节**:
- **Visitor 模式详解**（第4章）- 详细说明 IVisitor 接口和四个主要实现
  - InterpreterVisitor - 解释执行（返回 object）
  - CompilerVisitor - IL 代码生成（返回 void）
  - BytecodeVisitor - 字节码生成（返回 List<Instruction>）
  - TypeInferenceVisitor - 类型推断（返回 TypeInfo）
- **AST 节点组织**（第5章）- Expression 和 Statement 节点的分类和组织
- **Parser 结构**（第6章）- Facade 模式和递归下降解析机制
- **Type System**（第7章）- TypeChecker、TypeInferenceEngine、GenericTypeInference

**更新内容**:
- `CLAUDE.md` - 详细的 Visitor 模式说明和目录结构
- `Docs/ARCHITECTURE.md` - 新增4个架构章节，重新编号后续章节
- `Docs/ADVANCED_TOPICS.md` - 新增"Extending Old8Lang"章节

**关键文件位置**:
- Visitor 实现: `Old8Lang/AST/Visitor/`
- Bytecode VM: `Old8Lang/Bytecode/VirtualMachine.Core.cs`
- 类型系统: `Old8Lang/TypeSystem/`

#### API 参考文档完善

**新增标准库文档**:
- **核心标准库 (Old8LangLib)** - 12个模块
  - Math, File, Crypto, Image, Regex, Terminal, ColorfulTerminal, Time, OS, CSV, Template, Vector
- **网络库 (Old8Lang.NetLib)** - 5个模块
  - HTTP, WebSocket, MQTT, Socket, WebAPI
- **数据库库 (Old8Lang.DatabaseLib)** - 5个模块
  - MySQL, PostgreSQL, SQLite, InMemory, ORM
- **序列化库 (Old8Lang.SerializationLib)** - 3个模块
  - MessagePack, Protobuf, Factory
- **机器学习库 (Old8Lang.MachineLearningLib)** - 5个模块
  - Classification, Regression, Clustering, DataLoader, Predictor

**文档特点**:
- 每个模块包含完整的函数签名和描述
- 模式支持标注（✅ 解释模式 | ✅/❌ IL 模式 | ✅ VM 模式）
- 至少1个可运行的代码示例
- 导入方式和使用场景说明

**更新文件**:
- `Docs/API_REFERENCE.md` - 新增30个模块的完整文档

#### CLI 命令文档更新

**新增章节**:
- **调试和性能分析命令** - 完整的调试工具文档
  - `debug-start` - 启动调试会话
  - `debug-breakpoint` - 断点管理
  - `debug-control` - 调试控制
  - `profile` - 性能分析
  - 交互式调试命令
  - 调试工作流示例
  - 调试最佳实践

**更新内容**:
- `Docs/CLI_GUIDE.md` - 新增调试章节
- `CLAUDE.md` - 更新包管理命令示例

**包管理命令**:
- init, install, remove, restore, list
- pack, unpack, sign, verify, cert
- publish（一键发布）

#### 代码示例验证

**新增示例文件**:
- `examples/interpretation_mode_example.old8` - 解释模式示例
- `examples/compilation_mode_example.old8` - IL 模式示例
- `examples/vm_mode_example.old8` - VM 模式示例

**验证结果**:
- ✅ 所有示例使用正确的 Old8Lang 语法
- ✅ 赋值使用 `<-` 运算符
- ✅ 函数声明使用 `func` 关键字
- ✅ 类型注解使用 `int` 而非 `number`
- ✅ 所有示例可成功运行

#### 文档质量改进

**统一规范**:
- ✅ 代码块语言标注统一为 `old8lang`（原有 `old8` 已全部更新）
- ✅ 内部链接格式统一
- ✅ 表格格式统一（对齐方式、列宽）
- ✅ 术语使用一致（中英文对照）

**文档覆盖率**:
- 主要文档: 7个文件全面更新
- 新增章节: 15+
- 代码示例: 100+
- 函数文档: 200+

#### 变更文件列表

**核心文档**:
- `CLAUDE.md` - 执行模式、Visitor 模式、目录结构、关键文件
- `Docs/ARCHITECTURE.md` - 4个新章节（Visitor、AST、Parser、TypeSystem）
- `Docs/CLI_GUIDE.md` - 执行模式对比、调试命令
- `Docs/API_REFERENCE.md` - 5个标准库完整文档
- `Docs/LANGUAGE_FEATURES.md` - 模式特定功能
- `Docs/PERFORMANCE_GUIDE.md` - 执行模式性能
- `Docs/ADVANCED_TOPICS.md` - 扩展 Old8Lang

**示例文件**:
- `examples/interpretation_mode_example.old8`
- `examples/compilation_mode_example.old8`
- `examples/vm_mode_example.old8`

**备份文件**:
- `Docs/.backup/` - 所有更新前的文档备份

#### 功能需求满足情况

本次更新满足以下功能需求：

- ✅ **FR-001**: 三种执行模式的完整文档
- ✅ **FR-002**: 模式对比表和选择指南
- ✅ **FR-003**: 每种模式的可运行示例
- ✅ **FR-004**: Visitor 模式详细说明
- ✅ **FR-005**: AST 和 Parser 架构文档
- ✅ **FR-006**: 类型系统文档
- ✅ **FR-007**: 5个标准库完整 API 文档
- ✅ **FR-008**: 模式支持标注
- ✅ **FR-009**: 包管理命令文档
- ✅ **FR-010**: 调试和性能分析命令文档

#### 用户价值

**开发者**:
- 清楚理解三种执行模式的区别和使用场景
- 快速查找 API 方法和标准库函数
- 掌握调试和性能优化技巧

**贡献者**:
- 理解 Old8Lang 的架构设计
- 了解如何扩展 Visitor 模式
- 掌握添加新语言特性的流程

**用户**:
- 根据场景选择合适的执行模式
- 使用完整的标准库功能
- 高效管理和发布包

---

## Old8Lang 1.0.0 rc8

### 语言特性增强

#### extern 语句新增 `dotnetdll:` 前缀
- **功能**: 新增 `dotnetdll:` 前缀用于明确导入 .NET 托管 DLL，与 C/C++ 非托管 DLL（P/Invoke）区分
- **背景**: 之前使用 `.dll` 后缀时无法区分是 C# 托管 DLL 还是 C/C++ 非托管 DLL
- **使用示例**:
  ```old8
  // 导入 .NET 托管 DLL（使用 dotnetdll: 前缀）
  extern "dotnetdll:MyLibrary.dll" MyClass {
      func MyMethod(x:int) -> int
  }

  // 导入 .NET 标准库（也可以使用原有的 C#: 前缀）
  extern "C#:System" Math {
      func Pow(x:double, y:double) -> double
  }

  // 导入 C/C++ 非托管 DLL（P/Invoke，无前缀）
  extern "kernel32.dll" stdcall {
      func GetCurrentProcessId() -> int
  }
  ```
- **支持的前缀**:
  - `dotnetdll:` - .NET 托管 DLL 文件（新增）
  - `C#:` / `cs:` / `csharp:` - .NET 程序集（原有）
  - 无前缀 `.dll` - C/C++ 非托管 DLL（P/Invoke）
- **兼容性**: 完全向后兼容，原有语法不受影响

#### 全局函数命名参数支持
- **功能**: 所有全局函数现在支持命名参数调用，提升代码可读性和易用性
- **影响范围**: 60+ 个全局函数，包括：
  - IO 函数：`PrintLine`, `Print`, `ReadLine`, `Error`, `Clear`
  - 并发函数：`MutexLock`, `ChannelSend`, `SemaphoreCreate`, `AtomicIntSet` 等
  - 反射函数：`GetClassName`, `InvokeMethod`, `GetField`, `SetField` 等
  - 工具函数：`Len`, `Type`, `Assert`, `Sleep` 等
- **使用示例**:
  ```old8
  // 使用命名参数调用全局函数
  Sleep(milliseconds: 1000)
  MutexLock(mutexId: mutex)
  ChannelSend(channelId: ch, value: 100)
  InvokeMethod(obj: person, methodName: "greet", args: {})
  SemaphoreCreate(initialCount: 1, maxCount: 3)
  AtomicIntSet(atomicId: atomic, newValue: 20)
  ```
- **优势**:
  - 提高代码可读性：参数含义一目了然
  - 减少参数顺序错误：不需要记忆参数顺序
  - 支持混合调用：可以混合使用位置参数和命名参数
- **实现细节**:
  - 为所有全局函数类添加了 `ParameterNames` 属性
  - 参数名称遵循统一的命名规范（如 `mutexId`, `channelId`, `obj`, `methodName` 等）
  - 命名参数重新排序逻辑已在 `Instance.cs` 中实现
- **兼容性**: 完全向后兼容，现有代码无需修改

#### 函数参数信息查询
- **功能**: 新增 `GetFunctionInfo(functionName:string)` 全局函数，用于查询全局函数的参数信息
- **返回信息**:
  - `name`: 函数主名称
  - `names`: 所有别名列表
  - `parameters`: 参数列表（包含参数名称和类型）
  - `minParameterCount`: 最小参数数量
  - `maxParameterCount`: 最大参数数量（-1 表示不限制）
  - `returnType`: 返回类型
  - `isGlobalFunction`: 是否是全局函数
- **使用示例**:
  ```old8
  info <- GetFunctionInfo("ChannelSend")
  PrintLine(info.ToStr())
  // 输出: {"name": "ChannelSend", "names": ["ChannelSend"],
  //        "parameters": [{"name": "channelId", "type": "object"},
  //                       {"name": "value", "type": "object"}],
  //        "minParameterCount": 2, "maxParameterCount": 2,
  //        "returnType": "object", "isGlobalFunction": true}
  ```
- **应用场景**:
  - 动态函数调用：在运行时获取函数签名
  - 代码生成：根据函数信息生成调用代码
  - 文档生成：自动生成函数文档
  - 调试和开发工具：提供函数信息查询功能
- **实现细节**:
  - 新增 `GetFunctionInfoFunction` 类实现函数信息查询
  - 支持解释器模式、IL 模式和虚拟机模式
  - 在 `ReflectionHelper.cs` 中添加了 `GetFunctionInfo` 辅助方法
- **兼容性**: 新增功能，不影响现有代码

#### 原生函数命名参数支持
- **功能**: 原生 .NET 方法、P/Invoke 函数和 Python 函数现在支持命名参数调用
- **影响范围**:
  - 原生 .NET 方法：通过 `extern "<xxx.dll>"` 导入的 .NET 类库方法
  - P/Invoke 函数：通过 `extern "<xxx.dll>"` 导入的 C/C++ DLL 函数
  - Python 函数：通过 `extern PythonScript` 或 `extern PythonModule` 导入的 Python 函数
- **使用示例**:
  ```old8
  // 原生 .NET 方法命名参数
  extern "C#:System" Math {
      func Pow(x:double, y:double) -> double
  }

  // 使用命名参数（乱序）
  result <- Pow(y: 3.0, x: 2.0)  // 2^3 = 8.0

  // 混合位置参数和命名参数
  result <- Pow(2.0, y: 3.0)

  // Python 函数命名参数
  extern "script.py" {
      func greet(name:string, age:int) -> string
  }

  // 使用 Python kwargs 机制
  result <- greet(name: "Alice", age: 25)
  result <- greet(age: 25, name: "Alice")  // 乱序调用
  ```
- **技术实现**:
  - **原生 .NET 方法**：在 `FuncLangValue.Execution.cs` 中添加了 `ReorderNativeMethodArguments` 方法
  - **P/Invoke 函数**：在 `NativeDelegateFuncLangValue.cs` 中添加了命名参数重载方法
  - **Python 函数**：在 `PythonFunctionLangValue.cs` 中添加了 `ExecutePythonFunctionWithNamedArgs` 方法
  - 使用 .NET 反射 API (`MethodInfo.GetParameters()`) 获取参数名称
  - Python 函数使用 Python.NET 的 kwargs 机制传递命名参数
  - 支持参数重新排序、默认值处理和错误验证
- **优势**:
  - 提高原生函数调用的可读性
  - 减少参数顺序错误
  - 与 Old8Lang 函数保持一致的调用体验
  - Python 函数完全支持 Python 的关键字参数特性
- **兼容性**: 完全向后兼容，现有代码无需修改

#### C# 程序集导入语法
- **功能**: 新增 `extern "C#:AssemblyName" ClassName { ... }` 语法，支持从 .NET 程序集导入静态方法
- **语法格式**:
  ```old8
  // 基本语法
  extern "C#:AssemblyName" ClassName {
      func MethodName(param1:type1, param2:type2) -> returnType
  }

  // 支持的前缀
  extern "C#:System" Math { ... }      // C#: 前缀
  extern "cs:System" Math { ... }      // cs: 前缀
  extern "csharp:System" Math { ... }  // csharp: 前缀
  ```
- **使用示例**:
  ```old8
  // 导入 System.Math 类的静态方法
  extern "C#:System" Math {
      func Pow(x:double, y:double) -> double,
      func Sqrt(x:double) -> double,
      func Abs(value:double) -> double,
      func Max(val1:double, val2:double) -> double,
      func Min(val1:double, val2:double) -> double,
      func Round(value:double) -> double,
      func Floor(d:double) -> double,
      func Ceiling(a:double) -> double
  }

  // 使用导入的方法
  result <- Pow(2.0, 10.0)           // 1024
  sqrtResult <- Sqrt(144.0)          // 12
  complexResult <- Sqrt(Pow(3.0, 2.0) + Pow(4.0, 2.0))  // 5
  ```
- **支持的程序集查找方式**:
  - 从已加载的程序集中查找
  - 从 GAC（全局程序集缓存）加载
  - 从文件路径加载
- **技术实现**:
  - 新增 `CSharpDllProvider` 类实现 C# 程序集加载
  - 使用 .NET 反射 API 动态查找和调用方法
  - 支持解释器模式和 IL 模式
  - 自动类型转换（Old8Lang 类型 ↔ .NET 类型）
- **兼容性**: 新增功能，不影响现有代码
- **功能**: 新增 Python 风格的运算符重载功能，允许用户自定义类的运算符行为
- **支持的运算符**:
  - 算术运算符：`+` (`_add`)、`-` (`_sub`)、`*` (`_mul`)、`/` (`_div`)、`%` (`_mod`)、`^` (`_pow`)
  - 比较运算符：`==` (`_eq`)、`<` (`_lt`)、`>` (`_gt`)、`<=` (`_le`)、`>=` (`_ge`)
  - 索引运算符：`obj[key]` (`_getitem`)、`obj[key] <- value` (`_setitem`)
  - 自动实现：`!=` 运算符自动通过 `_eq` 方法的取反实现
- **使用示例**:
  ```old8
  class Vector {
      public x
      public y

      init(x, y) -> {
          this.x <- x
          this.y <- y
      }

      // 向量加法
      _add(other) -> {
          return Vector(this.x + other.x, this.y + other.y)
      }

      // 向量数乘
      _mul(scalar) -> {
          return Vector(this.x * scalar, this.y * scalar)
      }

      // 相等比较
      _eq(other) -> {
          return this.x == other.x && this.y == other.y
      }
  }

  // 索引运算符重载示例
  class SparseArray {
      private data

      init() -> {
          this.data <- {"dummy": 0}
      }

      // 索引获取
      _getitem(index) -> {
          return this.data[index.ToStr()]
      }

      // 索引设置
      _setitem(index, value) -> {
          this.data[index.ToStr()] <- value
      }
  }

  v1 <- Vector(1, 2)
  v2 <- Vector(3, 4)
  v3 <- v1 + v2        // 调用 v1._add(v2)
  v4 <- v1 * 2         // 调用 v1._mul(2)
  if v1 == v2 { ... }  // 调用 v1._eq(v2)
  if v1 != v2 { ... }  // 自动实现为 !v1._eq(v2)

  arr <- SparseArray()
  arr[0] <- 10         // 调用 arr._setitem(0, 10)
  val <- arr[0]        // 调用 arr._getitem(0)
  ```
- **实现细节**:
  - 在 `AnyLangValue` 类中重写了所有运算符虚方法（`Plus`、`Minus`、`Times` 等）
  - 在 `LangListItem.Run` 中添加了对 `_getitem` 方法的支持
  - 在 `SetStatement` 中添加了对 `_setitem` 方法的支持
  - 运算符方法查找通过 `MethodTable.LookupMethod()` 实现
  - 如果类未定义对应的运算符方法，会抛出清晰的错误提示
  - 比较运算符方法必须返回 `bool` 类型，否则抛出 `TypeError`
- **特性**:
  - 支持链式运算：`a + b + c` 会依次调用 `a._add(b)` 和 `result._add(c)`
  - 支持复杂表达式：`(a + b) * c` 会正确处理运算优先级
  - 支持索引语法：`obj[key]` 和 `obj[key] <- value`
  - 错误处理：未定义运算符时提供清晰的错误信息
- **测试覆盖**:
  - 基本算术运算符重载（Vector 类）
  - 比较运算符重载（Point 类）
  - 链式运算和复杂表达式（Complex 类）
  - 索引运算符重载（SparseArray 类）
  - 错误处理（未定义运算符）
- **当前限制**:
  - 仅支持解释器模式（`-f`），IL 模式（`-c`）将在后续版本中支持
  - 暂不支持一元运算符重载（如 `-x`、`!x`）
  - 索引运算符的 `_setitem` 方法中修改引用类型字段的内部状态可能需要特殊处理

#### 函数装饰器支持
- **功能**: 新增 Python 风格的函数装饰器语法，支持在运行时动态包装和增强函数行为
- **语法**:
  - 无参数装饰器：`@decorator`
  - 带参数装饰器：`@decorator(arg1, arg2)` 或 `@decorator(name: value)`
  - 多个装饰器：可以堆叠多个装饰器，从下到上依次应用
- **支持的功能**:
  - 装饰普通函数和异步函数
  - 支持命名参数语法（如 `@cache(timeout: 60)`）
  - 装饰器可以接收目标函数作为参数并返回包装后的函数
  - 多个装饰器按从下到上的顺序应用（最接近函数的装饰器最先应用）
- **使用示例**:
  ```old8
  // 无参数装饰器
  @log
  func myFunc(x:int) -> int {
      return x * 2
  }

  // 带参数的装饰器
  @cache(timeout: 60)
  func expensiveOp(n:int) -> int {
      return n * n
  }

  // 多个装饰器
  @log
  @cache(timeout: 30)
  func complexFunc(a:int, b:int) -> int {
      return a + b
  }
  ```
- **实现细节**:
  - 词法分析器：添加 `@` 符号的 token 类型（`At`）
  - 解析器：新增 `ParseDecorator` 和 `ParseDecorators` 方法，支持命名参数解析
  - AST：为 `FuncLangValue` 和 `AsyncFuncLangValue` 添加 `Decorators` 属性
  - 运行时：在函数初始化时自动应用装饰器，支持无参数和带参数两种模式
- **测试覆盖**:
  - 语法解析测试：验证装饰器语法正确解析
  - 解释器测试：验证装饰器在运行时正确应用
  - 支持简单装饰器、带参数装饰器和多个装饰器的组合
- **已知限制**:
  - IL 模式下的装饰器支持需要进一步完善
  - Lambda 闭包捕获装饰器参数的场景需要特殊处理

#### 反射系统支持
  添加完整的反射功能，支持运行时类型检查和动态操作
  - 添加 12 个反射全局函数：
    - 类型信息查询：`GetClassName`, `GetClassMethods`, `GetClassFields`, `GetMethodInfo`, `GetFieldInfo`
    - 动态方法调用：`InvokeMethod`
    - 动态字段访问：`GetField`, `SetField`
    - 动态实例创建：`CreateInstance`
    - 类型检查：`IsInstanceOf`, `HasMethod`, `HasField`
  - 反射可以访问所有成员（包括 private）
  - 支持解释器模式和 IL 模式
  - 类定义时自动注册到全局类型注册表

#### 内部改进

- 在 `AnyLangValue` 中添加反射专用方法（`ReflectionGetField`, `ReflectionSetField`, `ReflectionInvokeMethod`）
- 在 `TypeTemplate` 中添加全局类型注册表
- 创建 `ReflectionHelper` 运行时辅助类用于 IL 模式支持
- 将 `AnyLangValue.ExecuteMethod` 访问级别从 `private` 改为 `protected`

## Old8Lang 1.0.0 rc7

### 语法统一

#### 统一 extern 关键字
- **变更**: 将 `native` 和 `native extern` 关键字统一为 `extern`
- **影响**:
  - C# DLL 导入：`native "DllName" Class *` → `extern "DllName" Class *`
  - C/C++ P/Invoke：`native extern "dll" func foo() -> int` → `extern "dll" func foo() -> int`
  - Python 函数导入：`native extern "script.py" func bar() -> void` → `extern "script.py" func bar() -> void`
  - JavaScript 函数导入：`native extern "script.js" func baz() -> void` → `extern "script.js" func baz() -> void`
- **优点**:
  - 语法更简洁统一
  - 降低学习成本
  - 所有外部导入使用同一个关键字
- **向后兼容**: 旧代码需要将 `native` 替换为 `extern`

### 语言特性增强

#### 一元 NOT 运算符完整支持
- **功能**: 完善了一元 NOT 运算符（`!`）的前缀使用支持
- **修复内容**:
  - 修正了解析器中无法识别 `!val` 语法的问题
  - 在 `ExpressionParser.ParsePower()` 方法中添加了一元运算符处理逻辑
  - 支持多重一元运算符嵌套（如 `!!a`，双重取反）
  - 支持一元 NOT 与表达式组合（如 `!(10 > 5)`）
- **技术细节**:
  - 将一元运算符（`!` 和 `-`）处理移至 `ParsePower()` 方法开始处
  - 使用递归调用实现多重一元运算符支持
  - 创建 `Operation` 节点时，左操作数设为 `null` 表示一元运算
- **测试覆盖**:
  - 基本一元 NOT: `b <- !a`
  - 双重取反: `b <- !!a`
  - 负数前缀: `d <- -c`
  - NOT 与比较表达式组合: `e <- !(10 > 5)`
  - IL 模式和解释器模式均通过测试

### 编译器架构优化

#### Operation.cs 代码重构
- **重构背景**: Operation.cs 文件过于庞大（2335 行），包含大量重复代码，影响可维护性
- **重构内容**:
  - 提取数值运算助手类 `NumericBinaryOpHelper`（支持 +、-、*、/、%、^ 运算符）
  - 提取比较运算助手类 `ComparisonOpHelper`（支持 >、<、==、!=、<=、>= 运算符）
  - 提取 in 操作符助手类 `InOperatorHelper`（支持 List、Array、Dictionary、String 等集合类型）
- **重构效果**:
  - Operation.cs 从 2335 行减少到 1850 行，**减少 485 行代码（21%）**
  - 消除约 500 行重复代码
  - 创建 3 个专用助手类，23 个助手方法
  - 代码可维护性和可读性大幅提升
  - 所有测试通过，无功能影响
- **技术细节**:
  - 统一处理类型转换（int/double 混合运算、object vs int 特殊情况）
  - 优化栈操作（SwapStackOrder 方法处理 IL 栈顺序）
  - 支持 IL 模式和解释器模式

## Old8Lang 1.0.0 rc6

### LSP (语言服务器) 功能增强

#### 新增高优先级 LSP 功能
- **文档符号大纲 (Document Symbol)**: 提供代码结构视图，显示文件中的函数、类、变量等符号的层级关系，方便快速导航
- **签名帮助 (Signature Help)**: 在函数调用时提供参数提示和文档说明
  - 支持自定义函数的参数类型和文档提示
  - 内置函数参数提示（PrintLine、Input、ToInt、Range 等）
  - 实时高亮当前参数位置
- **代码格式化 (Document Formatting)**: 自动格式化 Old8Lang 代码
  - 支持整个文档格式化
  - 支持选定范围格式化
  - 自动调整缩进和代码对齐
  - 支持 Tab 和空格配置
- **代码操作 (Code Action)**: 提供智能代码建议和快速修复
  - 快速修复：针对"未定义的符号"错误，提供自动生成变量或函数定义的建议
  - 重构功能：选中代码后可提取为独立函数
  - 支持多种代码操作类型（QuickFix、Refactor、Extract 等）
- **文档链接 (Document Link)**: 为 import 语句提供可点击的文件链接
  - 支持相对路径导入（`./`、`../`）
  - 自动解析 `.old8` 文件扩展名
  - 支持目录模块（查找 `__init__.old8` 或 `index.old8`）
  - 鼠标悬停在 import 语句时显示可点击链接
  - 点击链接直接跳转到导入的模块文件
  - 提升代码导航体验和开发效率

#### 已有 LSP 功能
- **语义诊断 (Diagnostics)**: 实时语法错误和语义错误检查（已在之前版本实现）
- **自动补全 (Completion)**: 关键字、符号、代码片段、成员访问补全
- **跳转定义 (Go to Definition)**: 跳转到符号定义位置
- **查找引用 (Find References)**: 查找符号的所有引用
- **重命名 (Rename)**: 符号重命名
- **悬停提示 (Hover)**: 显示符号信息

#### 架构改进
- **符号信息模型扩展**: 为未来的调用层次功能预留数据结构
  - 扩展 `SymbolInfo` 类，添加 `Calls` 和 `CalledBy` 列表
  - 创建 `CallSite` 类存储调用点信息（符号名称和位置）
  - 为将来实现 Call Hierarchy 功能打下基础

#### 已知限制
由于当前使用的 OmniSharp.Extensions.LanguageServer 0.19.9 基于较旧的 LSP 规范（< 3.16），以下高级功能暂时无法实现：
- 内联提示 (InlayHint) - 类型推断和参数名称提示
- 调用层次 (CallHierarchy) - 函数调用关系视图
- 类型层次 (TypeHierarchy) - 类型继承关系视图

这些功能需要等待 OmniSharp 库升级到支持 LSP 3.16+ 的版本后才能实现。

## Old8Lang 1.0.0 rc5

### 语言特性增强

#### 1. Extern 原生函数导入（P/Invoke FFI 支持）
- 添加 `extern` 关键字，支持通过 P/Invoke 调用 C/C++ 原生库函数
  - 使用语法：`native extern "dll_name" func FunctionName(params) -> returnType`
  - 支持三种调用约定：`cdecl`（默认）、`stdcall`（Windows API）、`winapi`
  - 支持函数别名：`native extern "kernel32.dll" func GetCurrentProcessId() -> int as GetProcId`
  - 支持批量导入：使用块语法 `{ func1, func2, ... }` 一次导入多个函数
  - 支持为单个函数指定不同的调用约定
- 完整的类型映射支持
  - 支持基本类型：int、long、double、float、bool、string、void、char、byte、short
  - 支持无符号类型：uint、ulong、ushort
  - 自动处理 Old8Lang 类型到 C# 类型的转换
- IL 模式和解释器模式完全支持
  - IL 模式：动态生成 P/Invoke 方法定义和委托类型
  - 解释器模式：运行时创建委托并绑定原生函数指针
- 语法示例：
  ```old8
  // 单个函数导入
  native extern "msvcrt.dll" func abs(x:int) -> int

  // 指定调用约定
  native extern "kernel32.dll" stdcall func GetCurrentThreadId() -> int

  // 批量导入
  native extern "user32.dll" {
      func MessageBoxA(hWnd:int, text:string, caption:string, type:int) -> int,
      func MessageBoxW(hWnd:int, text:string, caption:string, type:int) -> int
  }
  ```

## Old8Lang 1.0.0 rc4

### 语言特性增强

#### 1. 可变参数（Params）支持
- 添加 `params` 关键字，支持函数接受任意数量的参数
  - 使用 `params` 声明可变参数：`func sum(params args:array<int>) -> int`
  - 调用时可传入任意数量的参数：`sum(1, 2, 3, 4, 5)`
  - 支持结合普通参数使用：`func format(prefix:string, params items:array<string>)`
  - params 参数在函数内部作为数组访问
- IL 模式和解释器模式完全支持
  - IL 模式使用 IL 指令（`Newarr`、`Stelem`）在运行时创建数组
  - 解释器模式直接处理参数列表转换为数组
- 语法规则保证代码安全性
  - params 参数必须是参数列表的最后一个参数
  - 一个函数只能有一个 params 参数
  - params 参数必须声明为数组类型（`array<T>`）

#### 2. 泛型集合类型支持
- 添加泛型集合类型注解：`list<T>`、`array<T>`、`dict<K,V>`
  - 支持单类型参数集合：`list<int>`、`array<string>`
  - 支持双类型参数字典：`dict<string, int>`
  - 支持嵌套泛型类型：`list<list<int>>`、`dict<string, list<int>>`
- 在 IL 模式下提供编译时类型检查
  - 检测集合元素类型不匹配错误
  - 提供详细的错误信息（包含变量名、期望类型、实际类型、元素位置）
- 解释器模式保持完全向后兼容
  - 不带类型注解的集合继续支持混合类型（如 `{1, "hello", true}`）
  - 泛型类型注解为可选特性，不影响现有代码

#### 3. 结构化文档注释支持
- 添加结构化文档注释解析功能，使用 `///` 三斜杠语法
  - 自动解析文档注释，提取函数/类的说明、参数描述、返回值说明等信息
  - 支持多种主流文档注释风格：
    - **Google Style**: `Args:`、`Returns:` 格式
    - **Sphinx/reStructuredText**: `:param`、`:return:` 格式
    - **JavaDoc**: `@param`、`@return`、`@throws` 格式
    - **中文风格**: `参数:`、`返回:`、`异常:` 格式
  - 自动检测文档注释风格，无需手动指定
- 结构化存储文档信息，包含：
  - 函数/类的摘要说明
  - 参数名称、类型、描述
  - 返回值类型和描述
  - 异常类型和描述
  - 示例代码
- 支持普通函数和异步函数的文档注释
- 为 IDE 集成和自动文档生成提供基础支持

#### 4. 并发原语原生化
- 将并发原语从 AsyncLib 标准库迁移为语言核心的全局函数
  - 无需 `import Async` 即可直接使用所有并发功能
  - 总计 57 个全局函数，覆盖 8 大并发原语类别：
    - **Mutex（互斥锁）**: 5 个函数 - `MutexCreate()`, `MutexLock()`, `MutexUnlock()`, `MutexTryLock()`, `MutexDispose()`
    - **Semaphore（信号量）**: 5 个函数 - `SemaphoreCreate()`, `SemaphoreAcquire()`, `SemaphoreRelease()`, `SemaphoreTryAcquire()`, `SemaphoreDispose()`
    - **AtomicInt（原子整数）**: 8 个函数 - `AtomicIntCreate()`, `AtomicIntGet()`, `AtomicIntSet()`, `AtomicIntIncrement()`, `AtomicIntDecrement()`, `AtomicIntAdd()`, `AtomicIntCompareAndSet()`, `AtomicIntDispose()`
    - **Channel（通道）**: 8 个函数 - `ChannelCreate()`, `ChannelCreateBounded()`, `ChannelSend()`, `ChannelReceive()`, `ChannelTrySend()`, `ChannelTryReceive()`, `ChannelClose()`, `ChannelDispose()`
    - **ReadWriteLock（读写锁）**: 8 个函数 - `ReadWriteLockCreate()`, `ReadLockAcquire()`, `ReadLockRelease()`, `WriteLockAcquire()`, `WriteLockRelease()`, `ReadLockTryAcquire()`, `WriteLockTryAcquire()`, `ReadWriteLockDispose()`
    - **CountDownLatch（倒计时锁）**: 6 个函数 - `CountDownLatchCreate()`, `CountDownLatchCountDown()`, `CountDownLatchWait()`, `CountDownLatchWaitTimeout()`, `CountDownLatchGetCount()`, `CountDownLatchDispose()`
    - **CyclicBarrier（循环栅栏）**: 6 个函数 - `CyclicBarrierCreate()`, `CyclicBarrierAwait()`, `CyclicBarrierAwaitTimeout()`, `CyclicBarrierGetParticipantCount()`, `CyclicBarrierGetWaitingCount()`, `CyclicBarrierDispose()`
    - **CancellationTokenSource（取消令牌源）**: 4 个函数 - `CreateCancellationTokenSource()`, `Cancel()`, `CancelAfter()`, `DisposeCancellationTokenSource()`
  - 并发工具函数: 3 个 - `Sleep()`, `GetCurrentThreadId()`, `GetProcessorCount()`
- 使用 ResourceManager 集中管理所有并发资源
  - 自动资源清理和生命周期管理
  - 线程安全的资源访问
  - 支持资源跟踪和错误处理

#### 5. using 语句（资源管理）
- 添加 `using` 语句，实现自动资源管理
  - 语法形式 1: `using varName <- resource { ... }` - 创建资源并自动管理
  - 语法形式 2: `using resource { ... }` - 管理已有资源
- 使用 try-finally 模式确保资源释放
  - 即使发生异常也能正确调用 Dispose
  - 块结束时自动释放资源
- 支持所有返回资源 ID 的并发原语
- IL 模式和解释器模式均完全支持

#### 6. select 语句（Channel 多路选择）
- 添加 `select` 语句，实现 Go 风格的 Channel 多路选择
  - 语法格式: `select { case ch <- value -> { ... } default -> { ... } }`
  - 支持发送操作: `case ch <- value ->`
  - 支持接收操作: `case value <- ch ->`（存在语法歧义限制）
  - 支持 default 分支: `default ->`
- 使用轮询策略检查多个 Channel
  - 执行第一个可用的 case
  - 无可用 case 且有 default 时立即执行 default
  - 无可用 case 且无 default 时阻塞等待
- **限制**: 仅解释器模式支持，IL 模式会抛出 NotImplementedException

### 系统优化

#### 2. 解释器模式增强
- 为数组类型添加 `Length()` 方法，与 `Count()` 方法等效
- 提升解释器模式与 IL 模式的 API 一致性

#### 3. 并发原语内部实现重构
- 创建专用的 `Old8Lang/Concurrency/` 目录
  - 提取并独立管理并发原语内部实现（AtomicInt, CountDownLatch, CyclicBarrier, ResourceWrapper）
  - ResourceManager 统一管理资源生命周期和清理
- 创建 `GlobalFunctions/Implementations/Concurrency/` 目录
  - 9 个专门的函数实现文件，每个负责一组并发原语
  - 清晰的职责分离和代码组织

### 测试与质量

- 新增可变参数（params）功能完整测试套件（10 个测试）
  - 解释器模式测试：5 个测试覆盖无参数、多参数、结合普通参数、数组访问等场景
  - IL 模式测试：5 个测试验证 IL 代码生成和运行时行为
  - 所有测试在 IL 模式和解释器模式下均通过
- 新增泛型集合功能完整测试套件（8 个测试文件）
  - 基本类型测试（list、array、dict）
  - 嵌套泛型类型测试
  - 类型错误检测测试（编译时捕获类型不匹配）
  - 向后兼容性测试（混合类型集合）
- 新增结构化文档注释功能测试套件（8 个测试）
  - Google Style、Sphinx Style、JavaDoc Style、中文风格测试
  - 默认风格和无文档注释函数测试
  - 类文档注释和异步函数文档注释测试
- 新增并发原语测试套件
  - 迁移并更新 16 个测试文件，移除 `import Async` 依赖
  - 验证所有 57 个并发全局函数的功能
  - 测试覆盖所有 8 大并发原语类别（Mutex、Semaphore、AtomicInt、Channel、ReadWriteLock、CountDownLatch、CyclicBarrier、CancellationTokenSource）
- 新增 using 和 select 语句测试
  - 验证 using 语句的自动资源管理
  - 验证 select 语句的 Channel 多路选择
- 所有测试在 IL 模式和解释器模式下均通过

## Old8Lang 1.0.0 rc3

### 语言特性增强

#### 1. 枚举（Enum）支持
- 添加枚举声明语法，支持定义命名的整数常量
- 支持自动值递增（从 0 开始）
- 支持显式指定枚举成员的整数值
- 支持混合使用自动值和显式值
- 枚举成员可用于比较、算术运算和条件语句

#### 2. 类型系统重构与增强
- 添加 Mixin、抽象类、接口支持，增强代码复用能力
- 添加泛型支持（泛型函数、泛型类），提高代码通用性
  - 支持泛型类型推断，自动从函数调用参数推断泛型类型
  - 支持泛型约束的 `&` 符号语法（例如 `class Box<T: IComparable & ICloneable>`）
  - 支持 `where` 子句语法用于函数级别的约束（例如 `func sort<T>(items: List<T>) -> List<T> where T: IComparable`）
  - 支持混合约束语法（在泛型参数声明中使用约束 + where 子句组合）
  - 支持可空泛型类型参数（例如 `class Optional<T?>` 或 `func identity<T?>(value: T?) -> T?`）
- 添加联合类型和交叉类型注解支持
  - **联合类型** (`A | B`): 值可以是多个类型之一，用于编译时类型检查
    - 支持变量声明：`value: int | string <- 123`
    - 支持函数参数：`func process(x: int | string) -> void`
    - 支持函数返回值：`func getValue() -> int | string`
    - 支持类字段：`public data: int | string | bool`
    - 支持泛型参数：`List<int | string>`
    - 支持可空联合类型：`value: int? | string? <- null`
  - **交叉类型** (`A & B`): 类型必须同时满足所有约束，主要用于接口组合
    - 支持泛型约束：`where T: IComparable & ICloneable`
    - 支持函数参数：`func process(x: Interface1 & Interface2)`
    - 支持变量声明：`value: A & B`
  - 类型兼容性：实现完整的联合/交叉类型兼容性检查规则
- 添加多态支持，增强面向对象编程能力
- 加入 `this` 和 `super` 关键字，支持父类方法调用
- 重构类型系统，引入类型模板机制
- 添加可空类型注解（`int?`、`string?` 等）
- 增强类型推断能力，减少显式类型声明
- 添加访问修饰符支持（public、private）

#### 3. 表达式与模式匹配
- 加入三元表达式（`condition ? true_value : false_value` 方式）
- 加入 `match` 表达式，支持模式匹配
  - **值匹配**: 匹配特定值 `case 1 -> "one"`
  - **变量绑定**: 捕获并绑定值 `case x -> x + 1`
  - **通配符匹配**: 使用 `_` 或 `default` 匹配任意值
  - **元组解构**: 解构元组并匹配元素 `case (x, 0) -> "On X-axis"`，支持嵌套模式和通配符
  - **类型匹配**: 根据值类型进行匹配 `case x:int -> "整数"`，支持 int、double、string、bool 等类型
  - **守卫条件**: 为类型匹配添加条件约束 `case x:int if x > 0 -> "正整数"`
  - **范围匹配**: 匹配数值范围 `case [0~12] -> "儿童"`，支持包含/排除边界 `[0~<10]`、`[0>~10]`、`[0>~<10]`
  - **作用域隔离**: match 表达式中绑定的变量不会泄漏到外部作用域，确保变量安全
- 加入 `is` 和 `in` 表达式，简化类型检查和集合成员检查
- 增强条件表达式的灵活性

#### 4. 语法优化
- 列表声明语法改为 `{...}`，与字典 `{"key": value}` 形成统一风格
- 添加 `dict()` 和 `tuple()` 函数，提供显式集合创建方式
- 优化 AST 树结构，提升解析性能和可维护性

### 异步与并发

#### 5. 生成器与异步流
- 添加生成器函数支持（`yield` 语句）
- 添加异步流（async for-in）
- 添加异步生成器，支持异步数据流处理

#### 6. 多线程与并发控制
- 添加多线程支持（`spawn` 函数）
- 添加异步操作支持（`async`/`await`）
- 添加锁操作（`lock` 语句）
- 添加原子操作、读写锁、Semaphore、Mutex
- 添加通道（Channel）机制，支持线程间通信

### 标准库扩展

#### 7. 新增标准库
- **机器学习库**: 支持基础机器学习算法
- **序列化库**: 支持 JSON、XML 等格式序列化
- **数据库库**: 添加数据库操作和 ORM 支持
- **图像处理库**: 基础图像处理功能
- **模板引擎库**: 支持模板渲染
- **网络操作库**: HTTP 操作、MQTT 操作、WebApi 服务

#### 8. 包管理与项目系统
- 添加第三方库支持（使用全局本地库: `~/.old8lang/packages/`）
- 引入 Old8Lang.PackageManager.Core 包管理器
- 添加项目导入功能，使用 `o8package.json` 文件进行项目管理
- 支持包依赖管理和版本控制

### 系统优化

#### 9. 模块系统优化
- 加入懒加载机制，按需加载模块
- 加入库缓存功能，提升模块加载性能
- 加入动态导入支持，增强模块加载灵活性
- 优化全局函数、全局静态类和标准库的录入流程

#### 10. 开发工具改进
- 优化 CLI 命令行工具，提升用户体验
- 增强错误提示信息，提供更准确的调试信息

### 测试与质量

- 大幅增加单元测试覆盖率
- 新增枚举功能完整测试套件
- 新增模式匹配增强功能完整测试套件（27 个单元测试）
  - 元组解构匹配测试（包括作用域隔离验证）
  - 类型匹配测试（int、string、double、bool）
  - 守卫条件测试（简单条件、复杂条件、外部变量引用）
  - 范围匹配测试（包含/排除边界、浮点数范围）
  - 混合模式测试
- **注意**: IL 模式仍在测试和完善中，建议优先使用解释模式

## Old8Lang 1.0.0 rc2

1. 加入了三元表达式
2. 加入了 break 和 continue
3. 加入了 try catch 语句
4. 彻底移除 Csly 引用，完全使用自己的解析器
5. 加入了继承和类元素声明标识符( static , public 等)
6. 优化 AST 树
7. 解决多个 Bug
8. 元组可以存多个值，但是访问其实有点困难，因为最后会被解析成多个嵌套的元组
9. 列表声明改为 `list[...]`，后面可能会改回来，因为这个写法会造成一些问题

## Old8Lang 1.0.0 rc1

1. 对类声明进行了修改
2. 修复了若干问题
3. 对类型进行了改进

## Old8Lang 0.8.0 版本

1. 修复以往Bug
2. 加入Json操作和基本方法
3. 使用反射来支持自定义方法
4. 加入类型转换
5. 将缩进解析转变为大括号块

完成时间：2024年10月4日

这个项目从22年立项以来，已经快2年了。

这两年的时间我逐步完善了Old8Lang，修了很多的Bug，添加了很多的功能。
但是一直停留在解释器和csly这里。
所以在未来的一段时间里，我可能会先完成自己的前端（即代码文本解析）。
然后就是对于递归的优化。

## Old8Lang 0.2.0 0.3.0版本

我们现在可以使用字典，列表，数组，元组（现在只支持而二元数组）。0.3.0版本则是对项目进行优化

```
a <- {1 2 3 4}//列表
b <- [1 2 3 4]//数组
c <- {(1:"1232") (2:"12345")}//字典
d <- (1 "asdf")
```

## Old8Lang 0.1.0 版本

在0.1.0版本中，可以使用原生函数和引用语句：

```
import os
import console
import net
import math

[import "console.dll" console Write print]
[import "console.dll" console WriteLine printline]
```

引用语句会引用相关内容，使其类和方法加载到该文件上：

import `<context>`

原生函数需要使用到C#的dll，该语法需要3~4个参数：

[import `<dllname> <classname> <methodname> <nativemethodname>`]

## 2022.12.30 12h

现在已经基本上写完了，但是只是一小部分，因为个人能力有限，现在先写成这个样子

已实现的：赋值语句，指向语句，if语句，for语句，while语句，func语句（还没有实现传参和返回功能），类实现（目前类里面方法功能还不太行）

未实现的：方法传参返回，继承，泛型，原生函数（也就是说只能通过变量储存器去观看变量）

未来还要写虚拟机但我已经忙好几天了，好累，等明年再说吧，现在连测试都还没开始，但应该可以使用。

## 2022.11.22 晚

下个学期再写吧，这个学期先写一下Old8Down（类markdown,想用这个专门写文章）

链接：

[Old8Down 西建大专用标记语言](https://gitee.com/luckyfishisdashen/Old8Down)

这个标记语言我目前还没想好具体的语法，可能要寒假的时候才能写完。

现在的想法就是可以专门用来写文章，语法可能要改一下，毕竟我想让markdown不那么难用，或者说想让markdown小白一点

## 2022.11.22 建库

我一直想写一门编程语言，然后最近看到了一个C#写编译器的教程：https://www.bilibili.com/video/BV15v41147Zg （国内）/ https://www.youtube.com/watch?v=wgHIkdUQbp0&list=PLRAdsfhKI4OWNOSfS7EUu5GRAVmze1t2y (国外)

然后我就想自己也写一个。
