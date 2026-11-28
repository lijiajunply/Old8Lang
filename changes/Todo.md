# Old8Lang 统一待办清单

核对日期：2026-11-28（结合 2026-11-15~27 修复记录与当前工作区实测）。此文档合并原根目录、解释器测试、IL 测试和语言服务器待办；现存的历史测试报告已归档到 `archive/Reports/`；更早的报告已不在仓库中，下文只保留其文件名以便追溯（需要时从 git 历史取回）。

**状态约定**：`[x]` 表示对应范围已有修复或通过记录，`[ ]` 表示仍需处理或待复核。历史数字只表示当时基线；未注明本次复测的条目不代表当前仍能复现，测试文件存在也不能证明场景覆盖完整。工作区未提交改动的通过结果不等于已提交版本通过。

## 优先顺序

1. 排查会使测试进程崩溃的 IL 问题，再核实其他 IL 结果错误和跨模式差异。
2. 修复测试隔离与不稳定问题，补齐关键场景覆盖。
3. 完善 LSP 作用域、诊断及调试集成；按收益推进 VM 性能和长期能力。

## 当前核对摘要（2026-11-18）

本次只更新清单，没有修改实现。测试针对当前工作区（含原有未提交的
`CompilerVisitor.SetValueToIl.cs`、`CompilerVisitor.Statements2.cs`、`IlValueBridge.cs` 改动），
使用默认档；没有重跑全量测试、性能档或全部三模式语料。

| 本次筛选范围 | 通过 | 失败 | 跳过 |
|---|---:|---:|---:|
| `Compiler.Collections` | 183 | 0 | 3 |
| `Compiler.Types` | 106 | 0 | 0 |
| `Compiler.Classes` | 117 | 0 | 0 |
| `Compiler.Async` | 103 | 26 | 3 |
| `Compiler.Functions` | 147 | 19 | 12 |
| `Compiler.Basic.ErrorHandlingTests` | 28 | 0 | 2 |
| `Compiler.Expressions.TernaryTests` | 15 | 0 | 0 |
| `Error.ExceptionCreationTests` | 14 | 1 | 0 |

前五组同次运行：719 项，656 通过、45 失败、18 跳过。另一次定向运行包含
`Compiler.Collections.ArrayTests`（12 通过）和 `Compiler.Functions.HigherOrderTests`
（10 通过、1 失败），与上表有重叠，不能相加当作唯一测试总数。
命令均为 `dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj --no-restore --filter '<对应完整命名空间或类名，以 | 连接>'`；
前五组复用首次构建，另加 `--no-build`。本次未出现测试主机崩溃。

当前优先处理：

- [x] **装饰器转发里 wrapper 读不到顶层变量**：IL 顶层变量在 `RuntimeGlobals` 槽位，
  解释器 wrapper 看不到。`InvokeByName` 调用前把槽位快照写入管理器、调用后写回，
  装饰器缓存/闭包/计数器用例全部通过（装饰器两组 10 → 3 失败）。
  见 change（`2026-11-18-装饰器顶层变量/test.md`）。

- [x] **装饰器可以改变返回类型（IL 支持）**：2026-11-23 将非 void 装饰器转发委托统一改为返回
  `object`，并修正 `SetStatementRuntimeExtensions.TryEmitGlobalStore` 用类型注解冒充栈类型
  导致对已装箱对象再次 `box` 产出垃圾整数的问题；`DecoratorCompilerTests` /
  `DecoratorInterpreterTests` 剩余 3 项全部转通过。
- [x] **动态结果循环回写**：`CoerceDynamic` 转换后写回原槽位，`reduce3` 中间值与
  解释器一致，`HigherOrderTests.ReduceFunction` 通过。object 算术和整数溢出仍是独立缺口。
- [x] **EdgeCases 组边界用例**：20 项失败中大多数是旧语法（`for ... to`、`case ->`、
  空 lambda、`()`、`2.0.Pow`）或与当前语义不符的期望（null.ToStr 返回 "null"、
  越界切片返回空、`[10~5]` 合法降序、`[]` 是数组没有 Add）。真实 IL 修复两处：
  语言值写入原生字段先解包（否则读回垃圾整数）、`&&`/`||` 的非 bool 操作数运行期
  报 TYPE_ERROR。`Compiler.EdgeCases` 20 → 0。

- [x] **解释器模块导入用例失败**：2026-11-23 查明 `ConditionalImportTests.Import_ConditionalWithComplexExpression_ShouldEvaluateExpression`
  并非环境/顺序抖动，而是字符串比较改为字典序后 `"1.5" < "2.0"` 由 `false` 变为 `true`，
  原用例断言停留在旧的长度比较分支；已修正期望，`ConditionalImportTests` 全通过。

- [x] **模式差异记录（EdgeCases 相关）**：VM 私有字段访问检查及跨模式阶段差异已补齐，`.Match` 的正则 API 口径已写入 `Docs/MODE_SUPPORT.md`。见 `2026-11-23-当前优先处理清零/test.md`。

- [x] **Sort 对字符串列表的跨模式差异**：根因是 `StringLangValue.Less/Greater` 按
  **长度**比较，长度相同的字符串无法排序（解释器 QuickSort/IL SortHelper 都因此保持原序）。
  现在改为 `CompareOrdinal` 字典序，VM 的小于/大于也补上字符串分支；三模式
  `{"z","a","x","b","y","c"}.Sort()` 均得到 `["a","b","c","x","y","z"]`。
  见 change（`2026-11-18-字符串序/test.md`）。

- [x] **高阶函数链用例使用关键字作为变量名**：`func <- functions[i]` 改名为 `fn`，
  `ChainFunction` 通过。
- [x] **AwaitTests 顶层调用缺少 await**：`finalResults <- testAwaitInLoop()` 拿到的是
  Task/AsyncStateMachineBox。补 await 后 `AwaitTests` 0 失败，Async 26 → 21。

- [x] **async 函数访问顶层全局变量**：已在 `2026-11-22-IL异步失败清理` 阶段 3 通过
  `RuntimeGlobals` 改用 `AsyncLocal` 解决（见下文「IL 异步剩余失败」条目）。

- [x] `Compiler.Async` 剩余 6 项已处理，涵盖 Task 静态 API、异步 catch/finally 和异步生成器；验收详见 `2026-11-23-当前优先处理清零/test.md`。

## IL 模式与跨模式缺陷

以下条目来自历史复现，尚未逐项重跑。报告见 IL 缺陷验证（`2026-10-23-23-30-IL模式缺陷修复验证.md`）、跨模式语义修复（`2026-10-24-00-30-P1跨模式语义修复测试.md`） 和 VM 残留修复（`2026-10-24-09-45-P2虚拟机模式残留修复测试.md`）。

- [x] **VM 中字典转导入的类后调用实例方法失败**：2026-11-27 修复，`a <- a as Test` 后执行 `a.Add()` 得到 `4`。见 [验证记录](2026-11-27-VM字典转类/test.md)。

- [x] **VM 的 `JsonSerialize` 输出为空**：2026-11-27 修复，`Old8Lang.App/Ex/init.old8` 中的 `Json(test)` 输出包含 `a` 和 `b` 的 JSON 对象。

- [x] **IL 比较运算装箱顺序错误导致测试进程崩溃**：`object` 与数值混合比较时先把两个操作数都压栈、
  再统一 `box`，`box` 只作用于栈顶，因此「数值在左、`object` 在右」（如
  `i >= this.data.Count()`，`data` 是未标注的空集合字段）会把右侧 `object` 装箱、左侧整数保持未装箱，
  随后 `callvirt IComparable.CompareTo` 把整数当 `this` 引用使用，生成无效 IL，运行期直接段错误、
  测试主机消失。六个比较运算符的该分支现改为「发射一个操作数就按位置装箱」。
  `Compiler.Classes` 与 `Compiler.Expressions` 两组由「测试运行中止」恢复为可完整运行
  （Classes 110 项、Expressions 232 项），其余 17 组通过/失败数与基线完全一致。
  见 change（`2026-11-14-IL比较装箱崩溃/test.md`）。修复后暴露的 `int?` 返回 `null` 触发 NRE
  仍是独立缺口（见下文“可空返回类型”一条）。

- [x] **IL 函数 `return` 的类型转换既有缺口**：2026-11-23 已在 `ReturnLabel` 分支补数值转换并修正字符串与动态基本类型返回值转换；2026-11-28 补齐全部 CLR 数值类型（`uint`/`ulong`/`ushort`/`sbyte`/`decimal`）、`char` 及其默认返回路径、可空与泛型可空返回值校验转换，`ReturnConversionRegressionTests` 全通过。见 `2026-11-23-IL与跨模式缺陷/test.md`。

- [x] **泛型目标类型的扩展方法 IL 支持**：`extension list<T>` / `dict<K, V>` / `array<T>` 保留目标泛型参数及 `where` 约束，IL 桥接保持接收者、参数与嵌套集合的引用别名及原地修改并支持 `return this` 链式调用。2026-11-28 已补齐空集合类型推断、多类型参数交叉约束（含 VM 字节码往返）以及字典与数组的别名/修改回归，`GenericListExtensionRegressionTests` 全通过。见 `2026-11-23-IL与跨模式缺陷/test.md`。
- [x] **IL 扩展方法与顶层回写的自引用数组栈溢出**：桥接及类型推断避免展开循环集合，顶层回写复用已转换对象；自引用数组、列表、字典及别名的定向回归通过。见 测试报告（`2026-11-11-16-27-泛型扩展与循环引用回归.md`）。

- [x] **方法调用结果上直接链式调用生成无效 IL。**
  - 2026-11-11：`Count().ToStr()`、`FindAll(...).Count()` 与多层数组/字典/列表链式调用已可用，接收者只求值一次。
  - 2026-11-28：补齐类成员流式链式、嵌套属性、函数返回值与柯里化链式回归，`MethodChainingRegressionTests` 全通过。见 `2026-11-23-IL与跨模式缺陷/test.md`。

- [x] **IL 模式 lambda 的闭包函数值支持。**
  - 2026-11-11~15：支持局部变量读写、具名函数返回闭包、嵌套闭包隔离、循环每轮快照、捕获 `this`、形参捕获与 `object` 函数值调用。
  - 2026-11-28：补齐跨具名函数的顶层变量捕获与函数类型参数（`(int) -> int` / `function`）传递及再捕获回归，`ClosureTests` 全通过。见 `2026-11-23-IL与跨模式缺陷/test.md`。

- [x] `List` / `Dictionary` / `Array` 其余实例方法的 IL 实现仍是 `Ldnull` 桩或按旧约定写死
  （例如 `Map` / `Filter` / `Reduce` / `Any` / `All` / `Find` / `Sort` / `IsEmpty` 等），
  在 `List<T>` 接收者下会「方法未找到」。
  - 2026-11-11：`Map`、`Filter`、`Find`、`Contains`、`Distinct`、`Sum`、`Take`、`Skip`、`Any/Some`、`All/Every`、`IsEmpty`、`Reduce`、`Sort`、`Min`、`Max` 与 `Average` 已在原生 `List<T>` 接收者上支持；`Map` / `Filter` / `Sort` / `Distinct` / `Take` / `Skip` 及数值聚合也覆盖数组表示。字典的 `Keys` / `Values` 已支持原生 `Dictionary<K,V>` 接收者并返回可继续调用集合方法的列表；显式 `dict` / `dictionary` 注解可将空花括号编译为字典。`Length` 属性现也可用于原生与经字典索引得到的集合。见 Map/Filter 测试报告（`2026-11-11-20-40-IL集合MapFilter.md`）、Find/Any/All 测试报告（`2026-11-11-20-45-IL集合FindAnyAll.md`）、IsEmpty 测试报告（`2026-11-11-20-50-IL集合IsEmpty.md`）、Reduce 测试报告（`2026-11-11-21-00-IL集合Reduce.md`）、数组测试报告（`2026-11-11-21-10-IL数组MapFilter.md`）、字典键值测试报告（`2026-11-11-21-20-IL字典KeysValues.md`）、排序测试报告（`2026-11-11-21-30-IL集合Sort.md`）、集合长度测试报告（`2026-11-11-22-00-IL集合Length.md`）、空字典注解测试报告（`2026-11-11-22-30-IL空字典注解.md`）、Contains 测试报告（`2026-11-11-22-40-IL集合Contains.md`）、Distinct 测试报告（`2026-11-11-23-00-IL集合Distinct.md`）、Sum 测试报告（`2026-11-11-23-10-IL集合Sum.md`）、Take 测试报告（`2026-11-11-23-20-IL集合Take.md`）、Skip 测试报告（`2026-11-11-23-30-IL集合Skip.md`） 与 数值聚合测试报告（`2026-11-12-00-30-IL集合数值聚合.md`）。其余字典及集合方法仍待迁移。
  - 迁移方式：参照 `Reports/2026-10-10-19-03-实例方法跨模式补齐测试.md` §2 的机制
    （实现 `IIlNativeValueInstanceMethod` + `IlValueBridge.EmitLoadWrapped` + 静态 helper）。
  - 2026-11-12：读取型 `First`、`Last`、`ElementAt` / `At` 现支持原生列表与数组，并保留负索引语义；见 读取型集合测试报告（`2026-11-12-00-40-IL集合读取方法.md`）。
  - 2026-11-12：`FirstOrDefault`、`LastOrDefault` 现支持原生列表与数组，无元素时可返回 `null` 或调用方提供的默认值；见 默认读取测试报告（`2026-11-12-00-50-IL集合默认读取.md`）。
  - 2026-11-12：`IndexOf` 现支持原生列表与数组，保留首个匹配项索引及未找到时 `-1` 的语义；见 索引查找测试报告（`2026-11-12-01-00-IL集合IndexOf.md`）。
  - 2026-11-12：`ToList` 与 `ToArray` 现支持原生列表、数组及转换结果的继续调用；见 集合表示转换测试报告（`2026-11-12-01-10-IL集合表示转换.md`）。
  - 2026-11-12：`Union` 现支持原生列表、数组和混合接收者，保留原列表元素顺序及右侧新元素去重追加语义；见 集合并集测试报告（`2026-11-12-01-20-IL集合Union.md`）。
  - 2026-11-12：`Intersect` 与 `Except` 现支持原生列表、数组和混合接收者，保持解释器的去重集合语义；见 集合交差集测试报告（`2026-11-12-01-30-IL集合交差集.md`）。
  - 2026-11-12：`Count` 现支持原生列表与数组，并可继续链式转换为字符串；见 集合 Count 测试报告（`2026-11-12-01-40-IL集合Count.md`）。
  - 2026-11-13：`Concat` 现支持原生列表、数组和混合接收者，按接收者在前、参数在后的顺序保留所有元素；见 集合连接测试报告（`2026-11-13-09-00-IL集合Concat.md`）。
  - 2026-11-13：`ToStr` 现支持原生列表与数组，输出与解释器的集合显示格式一致；见 集合字符串化测试报告（`2026-11-13-09-10-IL集合ToStr.md`）。
  - 2026-11-13：`IsSorted` 现支持原生列表与数组，保留升序（含重复值）判断语义；见 集合有序性测试报告（`2026-11-13-09-20-IL集合IsSorted.md`）。
  - 2026-11-13：`ForEach` 现支持原生列表与数组，回调可写回创建点可见的局部变量；见 集合遍历测试报告（`2026-11-13-09-30-IL集合ForEach.md`）。
  - 2026-11-14：其余 63 个未实现 `IIlNativeValueInstanceMethod` 的 List / Dictionary / Array 方法改为
    统一运行期桥接（`IlNativeCollectionBridge`）：编译期解析到语言方法后发射「包装接收者与实参 → 解释器实现 →
    还原结果」，`GroupBy` 与带 zipper 的 `Zip` 的 `Ldnull` 桩不再触发。重载由编译期解析出的具体方法类型决定，
    类型推断与 IL 栈类型共用同一返回类型映射。变异方法（列表增删改清与 `Pop`、字典 `Add`/`Remove`/`Clear`/`Update`、
    数组 `Set`）会同步回原生集合，别名可见修改。26 项定向测试通过；`Compiler/Collections` 复跑无新增失败，
    并顺带修复 6 项既有失败（`ListIsEmpty`、`List_AddAndRemove`、`ListJoin`、`ListDefaultElementReaders`、
    `CollectionErrorHandling`、`Dictionary_Remove`）。见 change（`2026-11-14-IL原生集合实例方法桥接/test.md`）。
  - 遗留：桥接保证语义一致，但未做专用 IL 优化；63 个方法旧的失效 `GenerateIlInternal`/helper 仍待清理。

- [x] **剩余集合实例方法的专用 IL 迁移与死代码清理**：2026-11-28 已将热点方法（`Chunk` / `Flatten` / `FlattenDeep` / `Slice` 及全部列表与数组排序算法）迁回专用 IL 并删除 63 个方法中失效的 `GenerateIlInternal` 与 helper；补齐嵌套字典在桥接边界的深度填充、桥接元组结果（`Zip`/`WithIndex`/`Dict.ToList`）的直接解构，并统一三模式字典 `Add` 同键覆盖语义。`NativeCollectionMethodBridgeTests` 全通过，见 `2026-11-23-IL与跨模式缺陷/test.md`。

- [x] `Array.Sort()` / `Array.IsSorted()` 及 `LangList*` 系列 VM 原生数组/列表支持：2026-11-23 补齐集合运算与聚合；2026-11-28 完成全部 `LangList*`（`First`/`Last`/`ElementAt`/`Contains`/`IndexOf`/`Count`/`Map`/`Filter`/`Find`/`Any`/`All`/`Reduce`/`Join`/`Zip`/`Zip3`/`SelectMany`/`SortBy`/`Min`/`Max`/`Sum`/`Average`/`ToList`/`ToArray`/`IsSorted`）原生数组/列表及空集合与越界报错审计，`VMNativeSequenceRegressionTests` 全通过。见 `2026-11-23-IL与跨模式缺陷/test.md`。

- [x] **IL 排除端点后的反向范围生成错误**：2026-11-11 先让 IL 与解释器的旧算法对齐（`[1>~<1]` 生成 `2, 1, 0`）；2026-11-15 复核发现旧算法本身与语法文档冲突（`>`/`<` 只应排除端点、不应翻转方向），已改为区间收窄、等端点排他为空，见下方条目。历史报告：`2026-11-11-22-50-IL扩展范围.md`。

- [x] **异步函数的装饰器仍不可用**：2026-11-28 三模式已补齐异步 Lambda / 异步函数值表示及简单、参数化、堆叠异步函数装饰器（含 VM 字节码往返与 `TypeChecker.IsPolymorphicCompatible` 对 `async_func -> function` 的兼容），`AsyncLambdaAndDecoratorTests` 与 `VMDecoratorTests` 全通过。见 `2026-11-23-IL与跨模式缺陷/test.md`。

- [x] **元组模式解构 `(a, b) <- ...` 跨模式差异**：三种模式支持元组、数组、列表及嵌套模式；IL 支持动态右值、已有局部变量、参数、全局槽位和异步挂起后的变量。VM 的嵌套元组解构及字节码保存加载已验证；覆盖无效嵌套值。见 `2026-11-23-IL与跨模式缺陷/test.md`。

- [x] **解释器 `Thread.CurrentThread` 属性访问**：裸属性和无参调用现均返回执行中的线程；同时修正了无参调用原先包装新建线程的问题。见 测试报告（`2026-11-11-12-42-当前线程属性测试.md`）。
- [x] **IL `Thread.CurrentThread` 属性与调用**：两种形式均返回实际执行线程，并支持继续访问其属性。见 测试报告（`2026-11-11-13-33-IL当前线程测试.md`）。
- [x] **IL 其他 Thread API**：`Thread.Sleep` 与 `Time.GetUnixTimeMilliseconds()` 的组合现已通过；标准库 CLR `long` 返回值会归一化为语言 `double`，避免时间差计算按 `int` 拆箱。`Lock(value)` 现可在 IL 模式下创建共享锁变量，支持 `.Value`、`.Get()`、`.Set(value)` 与原子 `.Add(value)`；`Spawn` 会把运行期全局槽位和回写目标安装到子线程，线程结束后刷新共享变量。`ThreadTests` 15 项全部通过；见 long 数值报告（`2026-11-13-11-10-IL标准库long数值归一化.md`）、锁变量报告（`2026-11-13-11-20-IL锁变量与Spawn全局上下文.md`） 与 回写报告（`2026-11-13-11-30-IL线程全局集合回写.md`）。

- [x] **排查直接返回自身的 LangValue 类型**：源码检索发现线程、VM 线程及三种 VM 生成器值的 `GetValue()` 直接返回自身，均已重写 `ToString()`；三种生成器的字符串转换回归通过。见 测试报告（`2026-11-11-13-35-自引用值排查测试.md`）。

- [x] **IL / VM 标准库 `import` 差异**：2026-11-28 三模式已补齐 `Math`/`Regex`/`Crypto`/`Csv`/`Time`/`Vector`/`Net` 模块导入、模块别名、函数体内调用、命名参数、选择性导入、原生类与静态成员导出（含 VM 字节码序列化往返），`StandardLibraryCrossModeImportTests` 9 项全通过。见 `2026-11-23-IL与跨模式缺陷/test.md`。

- [x] **IL 实例方法读取字段返回随机值**：
  `class M { public v:int; func field()->int { return this.v } }` + `m <- M(9)`，
  `PrintLine(m.field())` 在 `-il` 下每次输出不同的垃圾整数（`-f` 为 `9`）；顶层 `m.v` 读取正常。
  已修复有注解字段类型及方法返回值装箱/拆箱，覆盖多个实例及 `object` 返回值；见 测试报告（`2026-11-11-09-05-IL实例字段回归测试.md`）。

- [x] **IL 函数无法访问顶层全局变量**：`g <- 10` + `func f() -> int { return g + 1 }` →
  `变量 'g' 未声明`。顶层变量是 `OldLangRun` 的局部变量，函数是独立 `DynamicMethod`，编译函数体时不可见。
  2026-11-11：被函数体引用的顶层变量现提升为运行期全局槽位，具名函数、lambda、类方法和字段初始化器都可读写，
  支持函数声明早于变量赋值的前向引用；同名参数、局部变量、闭包单元和字段仍然遮蔽全局变量。未被函数引用的
  顶层变量继续使用局部变量。`Spawn` 创建的子线程现在会继承该执行的槽位环境，并在结束后回写共享变量；异步函数尚未接入该模型。见
  测试报告（`2026-11-11-22-40-IL具名函数读取顶层变量.md`）。

- [x] **IL 实例方法读取裸字段仍是 `NullReferenceException`**：裸字段现在在静态类型推断中返回其字段类型，
  避免返回路径对已加载的值类型重复拆箱；读取型回归通过。见
  测试报告（`2026-11-13-08-50-IL裸字段访问.md`）。
- [x] **IL 集合 `Length` 参与比较会生成无效 IL/运行期 NRE**：集合 `Length` 在发射路径和
  静态类型推断路径中均统一为 `int`，顶层与函数体中的比较回归均通过。见
  测试报告（`2026-11-13-09-40-IL集合Length比较.md`）。

- [x] **IL 打印布尔值大小写不一致**：`PrintLine(true)` 现在输出 `true`；见 测试报告（`2026-11-11-11-11-IL打印格式测试.md`）。

- [x] **IL 直接打印原生列表输出 .NET 类型名**：`PrintLine({1, 2})` 现在输出 `[1, 2]`；`Print` / `PrintLine` 也会处理所有参数。

- [x] **泛型类递归特化导致栈溢出**：原崩溃用例及自引用特化回归已通过；完整 `Compiler.Classes` 组正常结束，但仍有普通失败。见 测试报告（`2026-11-11-10-54-IL泛型递归特化测试.md`）。
- [x] **IL 字符串拼接直接用 CLR ToString 导致显示不一致**：`"x" + true` 输出 `xTrue`、
  `"x" + {1, 2}` 输出 `xSystem.Collections.Generic.List…`（CLR 类型名），解释器分别是
  `xtrue` 与 `x[1, 2]`。现在拼接前先经 `VMValueFormatter.ToDisplayString` 转成语言显示文本，
  bool/列表/字典/字符串元素引号都与解释器一致；`"x" + null` 仍为 IL 的 `null`（见 null 显示条目）。
  新增 4 项回归。见 change（`2026-11-15-IL字符串拼接显示/test.md`）。

- [x] **IL LINQ 其他查询路径待复核**：2026-11-23~24 完成 IL 连接行、分组、查询延续与作用域隔离；2026-11-28 统一 IL 流式排序与解释器 `LinqQueryExecutor` 的键比较语义（`CompareOrdinal` + 混合数值），修复解释器多段 `join`/`let`/`orderby`，并重构 VM `OrderBy`（`LinqSortByKeys` / `LinqGroupSortedEntries` 按键表达式稳定排序）与嵌套多段 `join` + `group by` + `into` 延续，三模式 165/165 LINQ 用例全通过。见 `2026-11-23-IL与跨模式缺陷/test.md`。

- [x] **IL `select` 接收复杂对象导致测试主机崩溃**：修正通道接收结果的值类型属性调用，成功接收、复杂对象接收和空通道默认分支定向测试通过。见 测试报告（`2026-11-11-08-49-IL-select回归测试.md`）。

- [x] **解释器并发任务克隆作用域时可能抖动**：2026-11-28 将 `AsyncFuncLangValue.RunAsync` 的作用域克隆移至 `Task.Run` 之前在调用方线程同步执行，并在 `VariateManager` 中对作用域栈读写与 `CloneInternal` 加锁同步，避免并发枚举字典报 `Collection was modified`；新增 `VariateManager_ConcurrentCloneAndScopeMutation_DoesNotThrow` 压力回归，`Interpreter.Async` 144 项 0 失败。见 `2026-11-23-IL与跨模式缺陷/test.md`。

- [x] **IL 模式顶层 C 风格 for 循环变量仍是全局变量**：解释器与虚拟机已把循环变量收进循环作用域
  （每轮闭包捕获 `012`、循环结束后变量不可见），IL 仍把它声明为全局变量，因此：
  `for i <- 0, i < 3, i++ {}` 之后 `PrintLine(i)` 输出 `3`（其余两模式报 `NAME_ERROR`）；
  同名嵌套循环共用同一个 `i`，`loop_shadow.old8` 里 `total` 得 `2`（应为 `6`）。
  这项工作依赖 IL 的全局变量 / 作用域模型（同本节的“IL 函数无法访问顶层全局变量”），一并处理更合适。
  2026-11-11：全局槽位模型已就位；若循环变量与「被函数引用的顶层变量」同名，IL 已在循环内建立
  局部遮蔽并恢复槽位，函数读到的仍是循环前的顶层值。普通循环变量「循环结束后仍可见」的问题不变。
  2026-11-13：循环初始化现在始终建立独立局部绑定，并在退出时恢复外层绑定；同名嵌套循环得到正确的
  `total = 6`，未声明的循环变量在结束后会报未声明错误。保存的 lambda 也可从集合索引调用，并分别捕获
  每轮值；2026-11-15 三种模式复测：`for i <- 0, i < 3, i++ {}` 之后 `PrintLine(i)` 三模式都报
  `NAME_ERROR` / `变量 'i' 未声明`，同名嵌套循环计数为 9（三次外层 × 三次内层），
  `LoopTests.ForLoopVariable_IsNotVisibleAfterLoop` 与 `ForLoopClosures_CaptureEachIterationValue`
  已覆盖，条目关闭。见 测试报告（`2026-11-13-09-50-IL循环变量作用域.md`） 与
  集合函数值回归（`2026-11-13-10-10-IL集合函数值调用.md`）。

- [x] **IL 运行期错误的异常值无法 `.ToStr()`**：现已支持直接转换捕获的索引错误和抛出消息，见 测试报告（`2026-11-11-12-35-IL异常字符串测试.md`）。

- [x] **解释器负索引越界的错误信息用了归一化后的下标**：`arr <- [1,2,3]; arr[-5]` 原先报
  `索引越界: 索引 -2 超出范围 [0, 2]`（应报原始的 `-5`）；IL 侧 `IlIndexHelper` 报的是原始下标。
  涉及 `ListLangValue.Get` / `ArrayLangValue.Get` / `StringLangValue.Get` / `TupleLangValue.Get`。
  已修正读取及列表、元组赋值的越界信息，并增加回归测试。

- [x] **泛型扩展方法与约束**：2026-11-28 已在三模式补齐泛型目标类型参数绑定与 `where`/`new()`/`class`/`struct`/交叉类型参数约束的注册与运行期校验，`GenericListExtensionRegressionTests` 全通过。见 `2026-11-23-IL与跨模式缺陷/test.md`。
- [x] **VM 原生函数命名参数**：`CallNative` 使用全局函数注册表中各重载的 `ParameterNames` 绑定，按参数类型选择重载，校验未知、重复和缺失参数；混合调用按实际压栈顺序取值。命名参数字节码也支持保存和加载。静态类方法命名参数仍受 `Docs/MODE_SUPPORT.md` 所述限制。见 `2026-11-23-IL与跨模式缺陷/test.md`。

- [x] **其余实例方法的 IL 操作数包装审计**：2026-11-28 已完成 `Dictionary` / `List` / `Array` / `Char` / `Generic` / `Tuple` / `TypeValue` / `ValueType` 命名空间的 `GenerateIlInternal` 审计与清理，剩余 `CreateCompiler` 仅用于原生值接收者，`NativeCollectionMethodBridgeTests.AuditedValueTypeCharAndDynamicIndexMethods_EmitValidIl` 通过。见 `2026-11-23-IL与跨模式缺陷/test.md`。

## 测试与工程问题

- [x] **`ExceptionCreationTests.RuntimeError_Message_ContainsErrorCodeAndPosition` 的列号断言过时**：2026-11-23 已按 `SourcePosition.ToString()` 1 起始列号约定更新为断言 `11` / `test.old8(1:11)`，`Error.ExceptionCreationTests` 15 项全通过。

- [ ] **`ScalarLiteralExpressionTests.AsyncStateMachine_FindsAwaitInsideSyntaxCollections` 失败**：2026-11-23 跑 Architecture 回归时发现，异步状态机分析器已拒绝「表达式内部的 await（挂起时求值栈非空）」，导致该用例抛 `CompilerException`。与本轮改动无关，待按当前异步限制更新用例。
  2026-11-24 查询作用域改动后重跑 Architecture：72 通过、1 失败，失败仍为此项；单项复跑同样失败。

- [ ] **默认档失败基线待复核（2026-11-27）**：验证 VM 本地导入时，默认档超过 30 秒无新输出后中止，中止前输出 43 条失败。涉及 `ScalarLiteralExpressionTests`、`ErrorPropagationTests`、`DecoratorInterpreterTests`、两组 `NamedArguments*Tests`、`EndToEndTests`、`InterpreterIntegrationTests`、`ConditionalImportTests`、`LoopTests`、`SelectStatementTests`、`UsingStatementTests`、`ThreadTests`、`IntersectionTypesInterpreterTests` 和 `EdgeCaseTests`。
  - 2026-11-28 复核：`ErrorPropagationTests.ErrorPropagatesThroughInheritance_CompilesAndExecutesCorrectly` 的 CLR `invalid program` 已修复（`TypeInferenceVisitor.VisitSuperExpression` 返回基类类型而非 `object`，避免 `return super.doWork()` 对未装箱 `int` 做动态转换；`Compiler.Exceptions` 与 `Compiler.Classes` 181/181 全通过）；`Compiler` 11 个主组定向运行为 1,639 通过、1 失败、23 跳过，唯一失败为 `Compiler.Modules.AdvancedImport.ConditionalImportTests.Import_ConditionalWithComplexExpression_ShouldEvaluateExpression`（与解释器同名用例相同，仍断言旧的字符串长度比较分支）；其余并行全跑时的失败为测试隔离问题（单独/按主组运行均通过）。

- [x] **IL 数组回归失败已关闭**：`Len` 残留栈值与集合 `+` 拼接已分别修复，见
  [动态长度报告](2026-11-14-IL-Len动态长度/test.md) 与
  [集合拼接报告](2026-11-15-集合拼接统一/test.md)。2026-11-18 当前工作区
  `Compiler.Collections.ArrayTests` 12 项通过，含原 `ArrayConcatenation` / `NestedArrays`。

- [ ] **旧 IL 失败基线的后续复核**：2026-11-14 的 Collections 9 项、
  Expressions/Linq/Modules 41 项、HigherOrder 8 项失败已被多轮修复取代，不能作为当前失败数。
  2026-11-18 当前工作区 Collections 为 183 通过、3 跳过，Classes 为 117 全通过；
  HigherOrder 仅剩 `ChainFunction` 的关键字变量名解析失败。Expressions/Linq/Modules
  本次未整组重跑，仍需按当前版本重建基线。

- [x] **IL 可空返回类型 `int?` 返回 `null` 触发 `NullReferenceException`**：2026-11-14 修掉比较装箱崩溃后
  暴露。可空类型注解在 IL 里被解析成基础值类型（`int?` → `int`），`return null` 的返回路径
  对空引用执行 `unbox.any int32`，运行期抛 NRE（`-f` / `-vm` 都返回 null）。
  现在「单个可空值类型」的**返回类型**统一用 `object` 承载（`IlReturnTypeHelper`），
  值走装箱、null 就是空引用；只影响返回类型，参数/字段/局部变量不变。
  `MemberAccessTests` 8→9 项全通过，`Compiler.Classes` 19→18 失败，其余 18 组不变。
  见 change（`2026-11-14-IL可空返回值/test.md`）。

- [x] **`null` 的显示大小写跨模式不一致**：`NullLangValue.ToString()` 原返回 `Null`，
  IL/虚拟机返回 `null`，而 `Converse(String)` 又返回 `"null"`。2026-11-15 统一为小写
  `null`（与字面量、JSON 风格、2/3 模式一致）；类型名仍由 `TypeToString()` 返回 `Null`。
  更新了 `UnionTypesInterpreterTests` 的期望，新增解释器/IL/VM 各 1 项显示回归。
  见 change（`2026-11-15-null显示统一/test.md`）。

- [x] **`null.ToStr()` 只在解释器可用**：2026-11-15 起三种模式都返回 `null`：
  IL 在 `DotOperatorILHelper` 的两条 ToStr 路径（静态发射与 `InvokeDynamicObjectMethod`）
  加空引用分支，VM 在 `CallMethod` 与 `InvokeTypeMethod` 的空引用检查里放行 ToStr。
  见 change（`2026-11-15-null显示统一/test.md`）。

- [x] **IL 类型推断在未完成的 `TypeBuilder` 上调用 `GetMethod`**：类方法体内 `this.method()`
  的接收者类型是尚未 `CreateType` 的 `TypeBuilder`，`VisitOperation` 兜底与
  `DotOperatorILHelper` 都调用 `leftType.GetMethod(...)`，编译期直接抛
  `NotSupportedException : The invoked member is not supported before the type is created`。
  现在 `TypeBuilder` 接收者与 `object` 接收者一样走运行期分派（推断返回 `object`、
  发射 `InvokeDynamicObjectMethod`，并允许匹配 CLR 私有方法）。
  `MemberAccessTests` 4→6 项通过，`ErrorPropagatesThroughClassMethods` 一并修复；
  `Compiler.Classes` 23→21 失败、`Compiler.Exceptions` 12→11 失败，其余 17 组不变。
  见 change（`2026-11-14-IL类内方法调用/test.md`）。遗留：`this.method()` 目前是反射分派，
  后续可在 `LocalManager` 预登记 `MethodBuilder` 恢复直接 `callvirt`。

- [x] **IL 静态字段声明期初值从不写入**：`DefineFields` 只把实例字段初值交给构造函数，
  静态字段既没有 `.cctor` 也没有其它写入口，`class M { public static PI <- 3.14159 }` 在
  `-il` 下读到 `0`（`-f`/`-vm` 为 `3.14159`）。现已补齐非泛型类的 `.cctor` 初始化
  （与 `GenericClassSpecializer` 的泛型特化路径一致），`MemberAccessTests.StaticMemberAccess_...`
  通过；`Compiler.Classes` 21→20 失败，其余 18 个子组不变。
  见 change（`2026-11-14-IL静态字段初值/test.md`）。

- [x] **`ArithmeticTests.UnaryMinus_Integer_*` 生成 `--1` 的用例缺陷**：Theory 用
  `result <- -{value}` 拼源码，负数参数会得到 `--1`/`--100`，被解析成自减，
  三种模式都输出 `0`/`99`（已在 `-f`/`-il`/`-vm` 实测），不是一元负号。
  已改为 `-({value})`，4 项通过；`Compiler.Expressions` 37→35 失败。

- [x] **IL 模式把 object 当布尔用**：`if obj.method()`、`obj && obj`、`!obj.method()`
  直接把装箱 bool 的引用交给 `brfalse` / `xor`，按引用判空得到恒真 / 垃圾结果
  （`if` 值 false 时仍走 true 分支，`!false` 返回 false）。现在只对静态类型为 `object`
  的操作数追加运行期 `IlValueBridge.ToBoolean`，接入条件（if/elif/while/for/三元）、
  逻辑运算与一元 `!`；非 object 类型保持原有真值语义。`Compiler.Classes` 16→15、
  `Compiler.Functions` 57→56 失败，其余组不变。见 change（`2026-11-14-IL布尔条件/test.md`）。

- [x] **IL 模式 try/catch 赋值把局部变量槽位换掉，丢失成功分支的值**：变量在 try 与 catch
  里都被赋值且两处类型不同时（`result <- ""; try { result <- "valid" } catch (e) { result <- e }`），
  catch 的赋值会在编译期把变量重绑定成 `ExceptionWrapper` 槽位，成功分支写入的值因此丢失
  （`-il` 读到空，`-f`/`-vm` 正常）。现在扫描「try + catch 都赋值」的变量并改用 `object`
  槽位承载；相等比较只要任一侧静态类型是 `object` 就走 `object.Equals`。
  `Compiler.Exceptions` 11→10 失败，其余 18 组不变。见 change（`2026-11-15-IL-try局部变量/test.md`）。

- [x] **IL 模式 for-in 里的 switch 与 try 内的 break/continue 生成无效 IL**：
  switch 值静态类型为 `object`（for-in 循环变量来自 `IEnumerator.Current`）时，
  `VisitSwitchStatement` 按实例方法调用 `object.Equals(object)`，只压了 case 值且未装箱；
  `break` / `continue` 在 try 块里仍发 `br`，而 CLR 要求跨受保护区域用 `leave`。
  现在普通匹配按「值类型 ceq / 字符串 string.Equals / 其它 object.Equals」三路发射，
  并新增 `LocalManager.IsInProtectedRegion` 让 try/catch 内的跳转发 `leave`。
  `Compiler.Statements` 3→0 失败（186/186 全通过），其余 18 组不变。
  见 change（`2026-11-15-IL-switch与try跳转/test.md`）。

- [x] **带元素类型的集合注解在 IL 模式被误判「实际值不是列表」**：
  `CollectionTypeValidator` 只认解释器的 `ListLangValue` / `ArrayLangValue` /
  `DictionaryLangValue`，而 IL 模式下 `{...}` / `[...]` / `{k: v}` 是
  `ListLiteralExpression` / `ArrayLiteralExpression` / `DictionaryLiteralExpression`，
  于是 `intList: list<int> <- {1, 2, 3}` 直接报「实际值不是列表（实际类型 any）」。
  现在三个校验方法同时接受两种表示。`Compiler.Types` 51→17 失败、`Compiler.Basic` 16→13 失败，
  其余组不变。见 change（`2026-11-15-集合类型注解校验/test.md`）。

- [x] **IL 模式联合类型返回值被收窄成第一个分支的类型**：`func f(flag:bool) -> int | string`
  的注解解析成 `object` 后，返回类型解析又回退到「按函数体第一个 return 推断」，
  变成 `int`，另一个分支 `return "text"` 报「返回类型不匹配」。现在
  `IlReturnTypeHelper.Resolve` 对含 `|` / `&` 的注解固定返回 `object`。
  `Compiler.Types` 17→15 失败。见 change（`2026-11-15-IL联合类型返回/test.md`）。

- [x] **集合注解元素校验仍有跨模式差异**：2026-11-28 已在解释器 `TypeChecker.ValidateVariableAssignment` 补齐 `list<T>`/`array<T>`/`dict<K,V>` 运行期递归元素校验，并在 IL `CollectionTypeValidator` 补齐嵌套集合字面量递归校验，三模式统一拒绝 `list<int> <- {1, "hello"}`、`list<list<int>> <- {{1, 2}, {3, "bad"}}` 与 `dict<string, int> <- {"a": 1, "b": "bad"}`（`CrossModeDefectsRegressionTests` 全通过）。见 `2026-11-23-IL与跨模式缺陷/test.md`。

- [x] **IL 模式反射 API 不可用**：`CreateInstance` / `InvokeMethod` 的 IL 发射用
  `ldsfld LocalManager.Interpreter`（该成员是实例属性，没有同名字段），编译期直接抛
  `ArgumentNullException (Parameter 'field')`；`ReflectionHelper` 的字段/方法/实例化
  又一律要求 `AnyLangValue`，对 IL 的原生对象报「对象不是类实例」；`SetField` 的值类型实参
  未装箱；`CreateInstance` 按类名扫全部动态程序集还会命中上一个用例的同名类。
  现在改用 `RuntimeGlobalWriter.CurrentManager`，为反射助手补原生对象分支，
  `SetField` 补 `box`，`FindNativeType` 倒序取最新程序集。`Compiler.Reflection` 14→0 失败
  （17/17 通过）。见 change（`2026-11-15-IL反射原生对象/test.md`）。

- [x] **IL 模式 lambda 无法捕获函数形参**：`func makeAdder(n:int) -> function { return (x:int) -> x + n }`
  报「变量 'n' 未声明」——`GetCapturedLocalValues` 只从 `LocalVar` 收集捕获，
  而形参在 `ArgumentIndices` 里、没有 LocalBuilder。现在捕获列表补上形参与外层 `CapturedLocals`，
  发射端按索引发 `ldarg` / 读外层闭包环境；解析器对 lambda 参数注解的白名单也补了
  `array`/`dictionary`/`object`/`function`。`Compiler.Functions` 56→54 失败，
  `BasicClosure`、`ClosureWithHigherOrderFunction` 通过。
  见 change（`2026-11-15-IL闭包捕获形参/test.md`）。该阶段发现的嵌套闭包返回 null 已由下条函数值调用修复，不再作为遗留。

- [x] **IL 模式调用 object 静态类型的函数值静默返回 null**：`m2 <- f2(); m2(5)`
  （`f2` 返回一个 lambda）在 `VisitInstance` 里既不是委托类型的局部变量、也不在
  `DelegateVar`，于是落到「按类名构造实例」兜底，找不到类就发 `ldnull` 返回 null。
  现在先在「按类名构造」之前识别已存在的局部变量并走新增的
  `IlValueBridge.InvokeFunctionValue`；同时修正嵌套闭包读取外层捕获值的拆箱
  （值类型必须 `unbox.any`，`castclass int` 曾产出垃圾整数）。
  `Compiler.Functions` 54→53 失败，`ClosureReturningClosure` 通过。
  见 change（`2026-11-15-IL函数值调用/test.md`）。

- [x] **IL 异步状态机在 try 区域里的 await 生成无效 IL**：`async func f() { try { await Task.Delay(5) } catch (e) {} }`
  在 IL 模式下抛 `InvalidProgramException`（`AsyncStateMachine_f.MoveNext()`）。根因是 `MoveNext` 开头用一次
  `switch` 派发状态，而 await 的恢复标签位于 try 区域内部；CLR 拒绝从 try 外部跳进区域中间，
  JIT 直接把方法判为无效 IL（已用 `DynamicMethod` 对照实验确认：从区域内跳到区域中间正常）。
  现在改为**按受保护区域派发**：方法体入口、每个含 await 的 try 块入口与 using 体入口各发射
  一条派发链，外层只跳到内层区域的**首指令**，内层再跳到自己区域内的恢复标签
  （与 Roslyn 的结构一致）。同时给用户 `finally` 体与 using 的 Dispose 加 `if (state < 0)` 守卫，
  避免挂起返回时提前执行。`Compiler.Async` 的 `InvalidProgramException` 由 32 处降为 0，
  原有 111 项中 65 失败→54 失败（11 项转通过），`Compiler.Exceptions` 9→8 失败；
  连续两个 await、循环内 try+await、嵌套/并列 try、using 体、finally 执行次数均已用
  CLI 与新增 15 项用例验证。
  见 change（`2026-11-15-IL异步try无效IL/test.md`）。无法恢复的 await 位置改为明确报错，见下一条。

- [x] **IL 模式 Task 静态方法实参发射错误导致无效 IL**：`Task.FromResult(42)` 统一实例化为
  `FromResult<object>(object)` 却没把 `int` 装箱；`Task.WhenAll` / `Task.WhenAny` 在分派前预压实参、
  分支里又求值一次，留下未消费的栈值；`Task.Run` / `Task.StartNew` 忽略实参；未知静态方法
  （`Task.Completed` 等）什么都不发射却声明返回 `Task<object>`。现在各分支自己求值与转换
  （装箱/拆箱/数值转换），`WhenAll` / `WhenAny` 只求值一次，未知方法改为抛
  `InvalidOperationError`。8 项 Task 相关用例转通过。
  见 change（`2026-11-15-IL-Task静态方法实参/test.md`）。

- [x] **`Task.Completed` / `Task.WaitAll` / `Task.WaitAny` 三种模式都没有实现**：已于 `2026-11-23-当前优先处理清零` 补齐三模式实现；2026-11-28 实测 `Compiler.Async`、`Interpreter.Async` 与 `VirtualMachine` 均 0 失败。

- [ ] **IL 模式暂不支持部分位置的 await**：以下位置会在编译期给出明确错误
  （`CompilerException`，不再生成无效 IL 或错误控制流）：
  - `catch` / `finally`（含 catch 过滤器）：只能由异常机制进入，恢复标签无法用分支到达
    （Roslyn 通过把处理器体搬出异常块、用 `<>s__` 记录待执行处理器来支持）；
  - `defer` / `yield`：IL 后端不生成这些语句的指令，其中的 await 无从恢复；
  - 表达式内部的 await（挂起时求值栈非空）：`1 + await f()`、`PrintLine(await f())`
    会被拒绝，`x <- await f()`、`await f() + 1`、三元分支中的 await 仍允许。
    支持它需要把挂起点的中间值提升为状态机字段（Roslyn 的临时变量提升）。
  受影响用例：`AsyncFunctionWithFinally_...`、`AsyncGeneratorWithResourceCleanup_...`
  （两者此前也失败，只是错误不同）。

- [x] **IL 异步剩余失败**：已于 `2026-11-23-当前优先处理清零` 全部清零（涵盖 `Task.WaitAll`/`Task.WaitAny`/`Task.Completed`、异步 `catch`/`finally` 中的 `await` 及 IL 异步生成器流协议）；2026-11-28 实测 `Compiler.Async` 0 失败。见 `2026-11-23-当前优先处理清零/test.md`。

- [x] **IL 模式 `double` 与 `int` 比较按位解释**：`a <- -500.0; a < 0` 在 `-il` 下得 `false`
  （`clt` 拿 double 与 int 直接比较，按内存布局读整数），导致「构造器里 `if amount < 0 { throw }`」
  静默不抛、catch 不执行。比较运算此前没有像算术那样做 double 提升。
  现在四个顺序比较与相等比较的普通数值分支都会先 `ConvertToDoubleIfNeeded`。
  `Compiler.Classes` 15→14 失败。见 change（`2026-11-15-IL混合数值比较/test.md`）。

- [x] **IL 未标注类型字段写入不装箱导致进程段错误**：`class C { public x }` + `c.x <- 100`
  之后任何读取（`"x=" + c.x`、`s <- c.x`、`c.x == 100`）都会 SIGSEGV——非 this 形式的
  实例字段写入只发 `stfld`、不做装箱，`object` 字段里存的是裸 int32，读取时被当成对象引用。
  现在字段与属性 setter 都按目标类型转换（值类型 → object 装箱、object → 值类型拆箱、
  数值宽度转换）。`TestFiles/CompilerTests/40_uninitialized_fields.old8` 段错误 → 正常退出；
  157 个 CompilerTests 语料扫描的段错误数 1 → 0；新增 5 项回归。
  见 change（`2026-11-15-IL未初始化字段崩溃/test.md`）。

- [x] **IL `in` 运算符生成无效 IL**：`"x" + ("a" in dict)` 直接无效 IL，`"x" + (1 in list)`
  则拼出 `x1`。三处根因：类型推断漏了 `In`（落到「返回左操作数类型」）、
  `List<T>.Contains(T)` / `Dictionary<K,V>.ContainsKey(TKey)` 收到装箱的 `object`、
  通用回退找不到 `Contains(object)` 时什么都不发射却声明返回 bool。现在 `In` 恒推断为 bool，
  按元素/键类型拆箱（数值宽度转换），回退改为运行期 `RuntimeContains` 按语言相等语义枚举。
  `12_in_expression.old8` 从无效 IL 变为跑完；新增 4 项回归。
  见 change（`2026-11-15-IL-in运算符无效IL/test.md`）。遗留：右侧是字符串字面量时
  `'h' in "abc"` 仍返回 false，见下一条。

- [x] **IL 类方法 object 形参与同步生成器 yield 生成无效 IL**：`func add(a, b) { return a + b }`
  的形参在 CLR 签名里是 `object`，调用点按原始类型压栈后直接 `callvirt`，未装箱的 int 被当
  object 传入；同步生成器的 `yield` 没有 IL 实现、不发射任何指令。现在实例方法调用按形参类型
  重新加载实参（装箱/拆箱/数值转换，引用类型之间不 castclass），同步生成器在编译期报
  「IL 模式暂不支持同步生成器（yield）」。`15_class_declaration.old8` 与 5 个 generator 用例
  不再产生无效 IL；新增 4 项回归。
  见 change（`2026-11-15-IL类方法实参与生成器无效IL/test.md`）。

- [x] **模板插值中的嵌套同种引号会让词法器死循环**：`PrintLine($"s={"ab"}")` 在 `-s`
  下 8 秒超时（`-f` / `-il` / `-vm` 同样挂死）。根因是词法把模板截成 `s={`，解析器扫到
  末尾的 `{` 时循环不推进。现在抛 `SyntaxError「字符串模板中的花括号未闭合」`，
  并加「本轮未推进」兜底；词法仍不支持插值内的同种引号（改用单引号或先赋值）。
  新增 1 项 Parser 回归。见 change（`2026-11-15-模板插值死循环/test.md`）。

- [x] **解析器在类体被文件结尾截断时死循环**：`class A {` 这类截断文件会让整个进程挂死
  （`-s` 也挂住，只能 kill）。`ParseClassBlock` 遇到 EOF 时执行 `CurrentIndex++` 后 `continue`，
  但 EOF 位置不会前进。现在直接抛 `SyntaxError`「类定义缺少右花括号」。新增 2 项回归。
  见 change（`2026-11-15-解析器类体EOF死循环/test.md`）。

- [x] **元组实例方法只有解释器可用**：`(1,2,3).Get(1)` / `Slice` / `ToList` / `Reverse` /
  `Contains` / `All` / `Any` / `Filter` / `Map` 在解释器正常，IL 报「方法 'Get' 未找到」，
  VM 报「实例必须是 Tuple 类型」。IL 侧扩展 `IlNativeCollectionBridge` 把原生
  `ValueTuple` 包装成 `TupleLangValue`；VM 侧先把右嵌套原生 `Tuple` 按 VM 编码展平再走
  VM 分派（lambda 参数走 `vm.CallFunctionObject`），并修了 `TupleSlice` 的 VM 分支与
  `LangListContains` 的原生实参转换。新增 IL/VM 各 1 项回归。
  见 change（`2026-11-15-元组实例方法跨模式/test.md`）。

- [x] **字符串实例方法的 IL 操作数未包装**：`"abc".StartsWith("a")` 等 19 个方法把原生
  string/int 直接传给 `StringLangValue`/`IntLangValue` 形参，生成无效 IL；模板结果上的
  `result.StartsWith(...)` 甚至触发 `AccessViolationException`。现在统一改用
  `IlValueBridge.EmitLoadWrapped`；三个谓词（`StartsWith`/`EndsWith`/`Contains`）改返回原生 bool，
  13 个字符串方法改返回原生 string（`Assert.Equal` 不再看到带引号显示）。
  新增 1 项覆盖 16 个方法的 IL 回归。见 change（`2026-11-15-字符串方法操作数包装/test.md`）。

- [x] **IL 字符串实例方法 `Repeat` 与语言值相等比较**：`"=".Repeat(5)` 生成无效 IL
  （`StringRepeatMethod` 把原生 string/int 直接传给 `StringLangValue`/`IntLangValue` 形参）；
  `"a".Repeat(1) == "a"` 为 false（语言值分支用了 `GetMethod("Equals",[LangValueType])`，
  实际绑定到 `object.Equals(object)` 的引用比较）；`!=` 也没有语言值分支。
  现在 `Repeat` 用 `IlValueBridge.EmitLoadWrapped` 包装，`==`/`!=` 调用 `LangValueType.Equal`。
  `class_modifiers_test.old8` 由 invalid program 变为正常退出；新增 1 项回归。
  见 change（`2026-11-15-IL字符串方法与值相等/test.md`）。

- [x] **IL 泛型可空参数（`T?`）的构造/调用实参未转换**：`Box<int>(5)`、`identity<int>(456)`
  生成无效 IL——`T?` 在特化里解析成 `object`，而调用点按原生 int 压栈后直接调用。
  现在构造 `init` 与泛型函数调用都按形参类型装箱/数值转换；`Box(123)`（不写类型参数）
  改为明确报「IL 模式暂不支持由构造函数实参推断泛型类」而不是静默返回 null。
  新增 4 项回归。见 change（`2026-11-15-IL泛型可空实参/test.md`）。

- [x] **IL 并发全局函数的 int 实参缺少拆箱**：`for lock in locks { ReadWriteLockDispose(lock) }`
  生成无效 IL——for-in 变量静态类型是 `object`，`DisposeXxx(int)` 调用点没有拆箱。
  新增 `ConcurrencyIlHelpers.EmitLoadIntArgument`（int/object/LangValueType/其他数值），
  七个 `*Dispose` 与同文件的 int 参数调用点接入。`concurrency_primitives_test.old8`
  由 invalid program 变为正常退出；新增 3 项回归。
  见 change（`2026-11-15-IL并发函数int实参/test.md`）。

- [x] **IL 非流式 LINQ 查询里的 `where` 生成无效 IL**：含 join 或非末尾 orderby 的查询走旧的手写
  `GenerateWhereClauseIL`，其中 `x > 2` 拿 `LangValueType` 引用直接发 `cgt`（`ComparisonOpHelper`
  只识别 `AnyLangValue` 子类），且条件值没按类型装箱就传给 `IsTruthy(LangValueType)`。
  现在六个比较分支识别 `LangValueType` 基类、操作数转换改用 `IlValueBridge.Wrap`、
  真值判定按条件类型装箱并改用 `LinqStreamingHelper.IsTruthy(object?)`。
  语料里最后一个 `invalid program`（`linq_join_compiler.old8`）消失，
  `join ... where`、`orderby ... where` 结果与解释器一致；新增 4 项回归。
  见 change（`2026-11-15-IL-LINQ-where无效IL/test.md`）。

- [x] **`'h' in "abc"` 的"跨模式差异"已核实为误记**：`"abc"` 不含 `'h'`，三种模式都返回
  `false`（2026-11-15 复测：`-f` / `-il` / `-vm` 一致）；此前记录为「解释器 true」是把
  `'h' in "hello"` 的输出看串了。`'h' in "hello"`、`'h' in str`、`"he" in "hello"`
  三种模式均为 `true`，无需修改实现。

- [x] **字符串拼接里的 bool 显示大小写跨模式不一致**：`"x" + (1 in list)` 曾输出 `xTrue`。
  IL 侧已在 2026-11-15 的字符串拼接修复中改用 `VMValueFormatter`；本轮补上 VM：
  `Add` 的字符串分支与 `System.String::Concat`（插值）都按语言显示规则格式化。
  三模式现在都输出 `xtrue` / `xnull` / `v=[1, 2]`；新增 VM 1 项回归。
  见 change（`2026-11-15-VM字符串显示/test.md`）。

- [x] **虚拟机混合数值比较偏差**：`b <- 3; b >= 3.0` 在 `-vm` 下返回 `false`,
  解释器与 IL 都是 `true`。根因是 VM 的 `Equals` 只处理同类型，`int == double` 落到
  `object.Equals` 恒为 false，而 `>=` / `<=` 由「大于/小于 或 相等」拼成。
  现在混合原生数值先转 `double` 比较。`VMMixedNumericComparisonTests` 2 项通过，
  VM 全组 1085 项通过。见 change（`2026-11-15-VM混合数值比较/test.md`）。

- [ ] **IL 算术对 `object` 操作数一律按 `int` 拆箱**：`func process(v) { return v + 1 }` +
  `process(3.5)` 抛 `InvalidCastException`（`NumericBinaryOpHelper.LoadAndConvertOperands`）。
  2026-11-15 试过「任一操作数静态类型为 object 时调用运行期数值运算助手并返回 object」，
  单看该用例通过，但让 `sum <- sum + i` 这类「int 局部变量接收 object 结果」的写法整批回归
  （Async、Collections、Statements、EdgeCases 共 −15 项），已整体回退。
  可行的方向是同时让「object 结果写入已定型局部变量」走拆箱/数值转换，而不是换槽位；
  这会与「值类型变量动态改数值宽度靠重绑定」的既有行为冲突，需要先定语义。
  见 `Docs/MODE_SUPPORT.md` 的跨模式差异小节第 1 条。
  2026-11-15 补充：类方法未注解参数（CLR 签名为 object）收到 `double` / `string` 实参后，
  方法体里的 `a + b` 也会走到同一处按 int 拆箱，`Unable to cast object of type
  'System.Double' to type 'System.Int32'`；类方法实参装箱已修，但这条数值/字符串语义缺口仍在。
  2026-11-15 第二轮尝试（已回退，实验代码归档在 `backups/trash-2026-11-15-IL动态算术/`）：
  新增 `IlDynamicArithmetic.Evaluate` + 在 `NumericBinaryOpHelper` 六种运算里对 object 操作数
  改走运行期分派，并把类型推断改为返回 `object`。`func addOne(v) { return v + 1 }` 与
  `addOne("ab")` 单独验证通过，但回归实测：Async 54→57、Collections 4→9、EdgeCases 22→24、
  Statements 0→3（`sum <- sum + item` 的 for-in 累加得到错误结果）。
  根因：`sum` 局部变量被重新推断为 object 后，把 object 结果 `stloc` 到 int 槽位，JIT 按位拷贝，
  下一轮读到垃圾值。可行方向仍是「object 结果写入已定型局部变量时做运行期拆箱/数值转换」，
  但需要同时处理「值类型变量动态改宽度」的既有行为；在定下这个语义前不要再单独改算术分支。

- [x] **IL 的 `throw` 丢弃被抛出的值，catch 只能拿到异常包装**：`throw "msg"` 在 IL 里被编译成
  「ToString 后 `new Exception(string)`」，catch 绑定的是 `ExceptionWrapper`，原始值无从取回。
  于是 `try { Account("Bob", -500.0) } catch (e) { caughtError <- e }` 之后
  `Assert.Equal("Initial balance cannot be negative", caughtError)` 失败（实际为空），
  而解释器里 `e` 就是被抛出的字符串。`ThrowComplexObject`（抛字典后 `e["field"]`）同源。
  需要新增「携带原始语言值的异常」并在 catch 处解包回原始值（同时确认 `e.ToStr()` 等用法）。
  已修复（2026-11-15）：新增 `LangThrownException` 携带原始语言值，catch 处用
  `ExceptionWrapper.Bind` 解包回原生值，catch 变量槽位改为 `object`；
  `Assert` 的 `string` 形参改为接受任意实参（按显示文本转换）。
  `Compiler.Exceptions` 10→9 失败。见 change（`2026-11-15-IL抛出值保留/test.md`）。

- [ ] **IL 模式值类型变量在 catch 里被赋异常仍会丢值**：`x <- 1` 之后 `catch (e) { x <- e }`，
  后续读取 `x` 得到 `null`（`-f` 为 `1`）。当前只把「引用类型初值」且「try + catch 都赋值」
  的变量提升为 `object`；值类型槽位保留按新类型重绑定，因为 `x <- 1; x <- 2.5` 依赖该行为。
  需要在「动态改数值宽度」和「catch 赋任意类型」之间定一条规则。
  见 change（`2026-11-15-IL-try局部变量/test.md` §5.1）。

- [x] **`(-1.0).Sqrt()` 跨模式行为不一致**：2026-11-15 统一为「负数开方报错」：
  IL 的实例 `Sqrt` 改调 `IlNumericHelper.Sqrt`（与 `MathLib.Sqrt` 同一校验），
  VM 在方法分派前拦截 `Sqrt` 并走相同助手；`(4.0).Sqrt()` 仍是 2。
  解释器仍没有 `.Sqrt()` 实例方法（文档只写 `Math.Sqrt(value)`），
  `EdgeCases` 失败集与改动前逐项一致（22 项，该用例在两种实现下都能通过），
  但 CLI 层面的 `(-1.0).Sqrt()` 已从 NaN 变为明确报错。
  见 change（`2026-11-15-实例Sqrt负数/test.md`）。
  以下为原记录：`-il` 返回 `NaN`（映射到 `Math.Sqrt`），
  解释器没有 `.Sqrt()` 方法、报 `ATTRIBUTE_ERROR`，文档只写 `Math.Sqrt(value)`。
  `EdgeCases/UnexpectedInputsTests.NegativeSquareRoot` 断言会抛异常；基线通过是因为
  `caughtError != ""` 走字符串引用比较（两个不同的 `""` 实例判不等）。
  需要先定「负数开方」的语义（NaN 还是抛错），再统一三模式与用例。
  见 change（`2026-11-15-IL-try局部变量/test.md` §5.2）。

- [ ] **解释器在「循环内 if 里 return + 索引访问字段」时死循环**：

  ```old8
  class RM {
      private resources <- {}
      func init() { this.resources.Add("a") }
      func has(name:string) -> bool {
          i <- 0
          while i < this.resources.Count() {
              if this.resources[i] == name { return true }
              i <- i + 1
          }
          return false
      }
  }
  ```

  `-f` 无限循环（每次迭代 `i` 都是 0，`i <- i + 1` 不生效），`-il` / `-vm` 正常。
  去掉循环内的 `if ... return true`，或单独执行 `this.resources[0] == name` 都正常。
  见 change（`2026-11-14-IL布尔条件/test.md` §5）。

- [x] **解释器无法在类内访问 `private static` 字段**：2026-11-28 修复 `TypeTemplate.CallStaticMethod` 遗漏 `tempManager.IsClass = true` 的问题；三模式均可在静态方法内读写本类 `private static` 字段，`CrossModeDefectsRegressionTests.PrivateStaticField_AccessibleInsideStaticMethod_WorksAcrossAllThreeModes` 通过。见 `2026-11-23-IL与跨模式缺陷/test.md`。

- [ ] **解释器三层及以上继承中连续 `super` 调用导致栈溢出**：2026-11-27 补测发现，`SuperProxy.GetDirectParentMetadata()` 始终读取 `currentInstance.Metadata.ParentClassName`（而非当前执行方法所属类的父类），当 `Level3` 调 `super.method()`/`super.init()` 进入 `Level2` 后，`Level2` 再调 `super` 仍解析到 `Level2` 自身，触发无限递归与 `StackOverflowException`。见 `2026-11-27-解释器测试覆盖复核/test.md`。

- [ ] **解释器集合/元组字面量按 AST 节点复用单一可变实例**：2026-11-27 补测发现，`InterpreterVisitor.SyntaxAdapters.cs` 对 `TupleLiteralExpression`/`DictionaryLiteralExpression`/`ListLiteralExpression`/`ArrayLiteralExpression` 缓存单一实例，且 `VisitTupleLangValue`/`VisitDictionaryLangValue` 原地 `Clear()` 覆写，导致循环或 LINQ 中 `group ... by (a, b)` 所有分组塌缩为 1 组、`select { "k": v }` 所有元素指向最后一次迭代的字典。见 `2026-11-27-解释器测试覆盖复核/test.md`。

- [ ] **解释器 `import "mod" as alias` 在调用者当前作用域执行模块体**：2026-11-27 补测发现，`ModuleSystemService.HandleLocalModuleImport` 对带别名/普通导入未隔离模块作用域，两个别名模块若定义同名顶层变量（如 `offset`），后导入的模块会覆盖前一个模块的变量，导致先导入模块的函数读到错误值。见 `2026-11-27-解释器测试覆盖复核/test.md`。

- [ ] **解释器 `MaxRecursionDepth = 1000` 高于托管线程栈容量**：2026-11-27 补测发现，每次 Old8Lang 递归调用占用多层 CLR 栈帧，无限递归在触达 `VariateManager.RecursionDepth > 1000` 守卫前即触发不可捕获的 `StackOverflowException`。见 `2026-11-27-解释器测试覆盖复核/test.md`。

- [ ] **解释器补测暴露的若干边角缺口（2026-11-27）**：调用非函数值（如 `num <- 42; num(1)`）未抛错；`LinqParser.ParseQueryContinuation` 仅支持单层 `into`（不支持链式 `into ... into ...`）；`GenericConstraintValidator.ValidateTypeParameterConstraint`（`where T: U`）未结合 `VariateManager` 解析用户类继承；`EnumLangValue + EnumLangValue` 报 `不支持整数与类型 'EnumLangValue' 的加法操作`。见 `2026-11-27-解释器测试覆盖复核/test.md`。

- [x] **虚拟机不支持集合字面量作为字段初值**：`BytecodeCompiler.EvaluateConstantExpression`
  现在递归求值列表/数组/字典字面量（`List<object?>` / `object[]` / `Dictionary<object,object>`），
  `class C { public items <- {1,2,3} }` 在 `-vm` 下可用；新增 1 项 VM 回归。
  注意：字段集合在解释器与 VM 中仍是「所有实例共享同一个对象」，IL 是每实例独立，
  该语义差异未变，已记在遗留里。见 change（`2026-11-15-VM字段集合初值/test.md`）。

- [x] **`MemberAccessWithComplexTypes_CompilesAndExecutesCorrectly` 的断言与语言语义冲突**：
  用例先取 `numbers <- collection.getNumbers()`，随后 `collection.addNumber(6)`，再断言
  `Assert.Equal(5, numbers.Count())`。集合是引用类型，`getNumbers()` 返回的正是字段本身，
  解释器与 IL 都得到 `6`（已用同一段代码在 `-f` / `-il` 实测）。已改为「修改前断言 5，
  修改后断言 `numbers` 与 `updatedNumbers` 都是 6」并注明别名语义；`MemberAccessTests`
  整组 7→8 通过。这条不是 IL 结果错误。

- [x] **范围字面量：端点相等且被排除时方向错误**：`[5>~<5]`、`[5>~5]`、`[5~<5]` 在解释器与 IL
  下分别得到 `[6, 5, 4]`、`[6, 5]`、`[5, 4]`（虚拟机为空）。根因是两份实现都先把边界 ±1、
  再按调整后的值判方向，端点相等时被误判成降序。现在方向由原始端点决定、排除端点只收窄区间，
  解释器直接复用 `RangeLangValue.CreateRangeArray`。三模式结果一致；
  `ExtendedRangeTests.EmptyRange_...` 里按旧行为写的断言已修正。
  新增 14 项解释器/虚拟机回归。见 change（`2026-11-15-范围端点语义/test.md`）。

- [x] **集合 `.Length` 属性的解释器缺口**：`array.Length` 在 IL 与虚拟机可用，解释器报
  `NAME_ERROR`。2026-11-15 决定保留并统一为语言属性：数组/列表/字符串/字典的 `.Length`
  等价 `len(x)`；解释器在 `Dot` 里补属性、VM 补字典 `Length`，API_REFERENCE 同步改为支持。
  字典键名 `"Length"` 仍可用 `d["Length"]`。解释器/IL/VM 各新增回归（共 10 项）。
  见 change（`2026-11-15-Length属性统一/test.md`）。

- [x] **虚拟机 `in` 运算符未实现**：`BytecodeVisitor.VisitOperation` 的二元运算符 switch
  没有 `LangTokenType.In` 分支，落到 `default: Emit(OpCode.Nop)`，表达式结果变成右操作数。
  2026-11-15 新增 `OpCode.In = 0x26`：解释器同一套语义（`ILangList.In` 处理字符/子串、
  列表按值、字典按键），并补原生 `IDictionary` / `IEnumerable` 回退。
  `VMInOperatorTests` 11 项通过（修复前 10 项失败），VirtualMachine 全组 1096 项通过。
  见 change（`2026-11-15-VM-in运算符/test.md`）。

- [x] **IL 模式 match 不支持范围 / 类型 / 元组解构模式与 guard**：
  `MatchExpressionEnhancedTests` 曾 25 项失败。现在新增 `IlMatchRegistry`：
  编译期登记 `MatchCase`、运行期直接调用解释器的 `IsMatch`（复用值 / 绑定 / 元组解构 /
  类型 / 范围 / guard 全部语义），`VisitMatchExpression` 只负责按 id 询问、写回绑定变量、
  计算分支结果；`IlValueBridge.Wrap` 也补了 `ITuple` 分支把 `ValueTuple` 包成
  `TupleLangValue`。`Compiler.Expressions` 35→11 失败，其余组不变。
  见 change（`2026-11-15-IL-match高级模式/test.md`）。

- [x] **IL 的 match 解构绑定污染外层作用域、guard 读不到外层变量**：
  `case (x, y)` 会覆盖同名外层局部变量并在分支结束后把它删掉；guard 在只含绑定变量的
  子作用域里求值，`if x > threshold` 解析不到 `threshold`。现在绑定前记录被遮蔽的槽位、
  分支结束后恢复，并把当前局部变量打包成 names/values 传给 `IlMatchRegistry`，
  运行期在求值 guard 前恢复成一个子作用域。`Compiler.Expressions` 11→9 失败。
  见 change（`2026-11-15-IL-match作用域/test.md`）。

- [x] **`TernaryTests` 旧用例问题已关闭**：返回类型语法及比较条件期望已修正，
  2026-11-18 当前工作区 `Compiler.Expressions.TernaryTests` 15 项全通过。

- [x] **VM match 三元组解构仍待复核**：2026-11-28 修复 VM `FlattenTuple` 仅识别 `TupleLangValue` 而不识别原生 `ITuple` 的缺陷；三元组与四元组解构匹配在三模式均已通过（含字节码序列化往返），`CrossModeDefectsRegressionTests.MatchTupleDestructuring_ThreeAndFourElements_WorksAcrossAllThreeModes` 通过。见 `2026-11-23-IL与跨模式缺陷/test.md`。

- [x] **数组 / 列表 `+` 拼接只有虚拟机支持**：`arr1 <- [1,2,3]; arr2 <- [4,5,6]; arr1 + arr2`
  在 `-f` 报 `INVALID_OPERATION_ERROR`，`-il` 被推断成数值加法，`-vm` 正常。
  2026-11-15 定为语言特性（同类型按顺序拼接，数组 + 列表仍一致报错）：解释器新增
  `ArrayLangValue.Plus` / `ListLangValue.Plus`，IL 走 `LangCollectionOperations`，
  VM 补原生 `List<object?>` 拼接。`Compiler.Collections` 6 → 5 失败
  （`ArrayConcatenation` 转通过），新增 7 项跨模式回归。
  见 change（`2026-11-15-集合拼接统一/test.md`）。

- [x] **IL 除零错误被包装成通用 RUNTIME_ERROR**：`10 / 0` 在 IL 抛
  `RUNTIME_ERROR 除数不能为零`，解释器/VM 是 `ZERO_DIVISION_ERROR`。现在执行委托的
  `DivideByZeroException` 分支改抛 `ZeroDivisionError`；集成用例同步改为
  「编译成功、执行阶段抛错」。`Compiler.Integration` 2 → 0 失败。
  见 change（`2026-11-15-IL除零错误/test.md`）。

- [x] **SpawnTests 使用已移除的 spawn 关键字语法**：`task <- spawn f()` 在三种模式下都报
  `NAME_ERROR 'spawn' 未定义`，整组 14 项失败是用例停留在旧 API。已迁移为
  `spawn(f, args...)` + `Start()` + `Join()`，匿名函数改 lambda，方法调用改
  `spawn(() -> obj.method())`；变量遮蔽、`Time.Now`、斐波那契期望值一并修正。
  `Compiler.Async` 42 → 33 失败。见 change（`2026-11-15-spawn用例迁移/test.md`）。

- [x] **IL 命名实参校验/局部函数支持**：命名实参此前被忽略或只按位置处理，
  `NamedArgumentsErrorTests` 不报错、局部 lambda 调用报 NAME_ERROR。
  现在带命名实参的调用统一走解释器语义，并在编译期校验参数名/重复/冲突/必需参数/params；
  局部 lambda 的形参名登记后按名重排。`Compiler.Functions` 26 → 17 失败。
  见 change（`2026-11-15-IL命名实参/test.md`）。

- [x] **纯位置调用缺参数的编译期校验**：`test()` 缺少必需参数现在在编译期抛
  `ArgumentError`（`NamedArguments_AllParametersMissing` 通过）。

- [x] **类方法命名实参 + 高阶函数命名 lambda**：2026-11-23 修复：
  `ClassInitRuntimeExtensions.DefineMethodInternal` 通过 `MethodBuilder.DefineParameter`
  把形参名写入 CLR 元数据，`DotOperatorILHelper` 与 `TypeInferenceVisitor` 补齐类方法
  命名实参重排与返回类型推断；`IlRuntimeFunctions.InvokeInstance` 命名实参改用
  `WrapArgument`，且 `IlDelegateFuncLangValue.Accept` 在解释器求值时返回自身避免被
  `VisitFuncLangValue` 克隆成空函数体。`Compiler.Functions.NamedArguments*` 全通过。


- [x] **IL 动态调用结果循环回写**：已与顶部条目一并验收，`HigherOrderTests.ReduceFunction` 通过。object 算术和 catch 赋值仍按各自独立条目跟踪。

- [x] **Exceptions 组旧语法 + catch 变量拿到 reflection 消息**：5 项用例用旧的
  裸 `throw`/`case ->`/匿名函数/`:` 继承/match 分支 throw 语法；lambda 内语言的 throw
  经反射调用被包成 `TargetInvocationException`，catch 变量拿到通用 reflection 消息。
  `ExceptionWrapper.Bind` 现在逐层解包；用例迁移到当前语法与真实语义。
  `Compiler.Exceptions` 8 → 0 失败。见 change（`2026-11-15-异常传播用例/test.md`）。

- [x] **IL 类型错误：无效 IL / 编译期拒绝 / 静默写入**：参数类型不匹配会生成
  `call int(int,int)` 收到 string 的无效 IL；返回不匹配在编译期拒绝；带注解赋值静默接受；
  非数值 `*` 把引用当整数乘出垃圾结果。现在统一由 `IlTypeErrors` 在运行期抛 TYPE_ERROR /
  INVALID_OPERATION（与解释器/VM 对齐），`Compiler.Basic` 11 → 0 失败。
  见 change（`2026-11-15-IL类型错误运行期化/test.md`）。

- [ ] **未定义变量/成员在 IL 是编译期错误**：解释器/VM 分别是运行期 NAME_ERROR /
  属性错误；IL 在编译期就拒绝（`变量 'x' 未声明`、`类型 Test 没有属性 y`）。
  用例已按 IL 语义迁移；若要与其它模式完全一致，需要在未知名字处发射运行期查找/错误
  （会牺牲编译期拼写检查，暂不处理）。

- [x] **IL mixin / 接口默认方法未合并**：`class C with M` 的 mixin 字段/方法、
  `implements I` 的接口默认实现都没并入类模板（解释器/VM 在方法表里合并）。
  现在 `MergeMixinMembers` 在生成 IL 前递归合并 mixin（含父 mixin）与接口默认方法，
  类自身成员优先；用户类自定义 `toString`/`equals` 也不再被内置别名截走。
  `Compiler.Classes` 13 → 0 失败。见 change（`2026-11-15-IL-Mixin合并/test.md`）。

- [x] **IL 泛型特化里“函数值字段当方法调用”**：2026-11-28 已在 IL（静态方法查找与动态分派 `InvokeDynamicObjectMethod`）、解释器（`AnyLangValue.CallMethod`）与 VM（`CallMethod` 字段回退并校验访问权限）三模式统一支持调用保存函数值的实例字段（如 `this.validator(x)`、`box.validator(x)`），`CrossModeDefectsRegressionTests.GenericClass_CallableFunctionField_WorksAcrossAllThreeModes` 通过。见 `2026-11-23-IL与跨模式缺陷/test.md`。

- [x] **IL spawn 带参调用不互通**：`Thread.Join` 返回 object、`Assert` 的 object 实参
  动态解包、`InvokeDynamicObjectMethod` 支持语言值接收者后，Collections/Conditional 通过；
  槽位类型与写入值不兼容时退化为 object（顶层槽位定型修复）后
  `SpawnWithParameters` 通过；`SpawnWithErrorHandling` 改为按错误消息内容断言
  （三模式包装不同：CustomError / LangThrownException / 原始值）。
  `Compiler.Async.SpawnTests` 13 项全通过。见 change
  （`2026-11-15-Join结果动态分派/test.md`、`2026-11-15-顶层槽位定型/test.md`）。

- [ ] **线程异常消息口径三模式不一致**：线程内 `throw` 后 `Join()`，解释器抛
  `线程执行异常: CustomError: [RUNTIME_ERROR] ...`，IL 抛
  `LangThrownException: ...`，VM 直接是原始异常值；`type(e)` 分别为 CustomError /
  Object / String。需要统一为“保留原始语言错误值”或统一包装，涉及
  `ThreadLangValue.Join` 与 IL 的 `LangThrownException` 转换。

- [x] **IL 集合注解元素类型不匹配不报错**：`items:list<int> <- {1, "hello"}` 在 IL 下被接受
  （VM 编译期报错）。`CollectionTypeValidator` 不认识 IL 的标量/嵌套字面量，
  元素类型退化成 `any`；`list<int>` 注解又被解析成 CLR `List<int>`，与字面量
  `List<object>` 不一致。现在补标量/嵌套类型描述、同基集合形状比较（`any` 放行），
  泛型集合注解统一映射到 object 容器；`comparable` 约束映射 `IComparable`。
  `Compiler.Types` 10 → 0 失败。见 change（`2026-11-15-集合注解类型检查/test.md`）。

- [x] **lambda 参数注解 `object` 解析失败**：`(f:object) -> ...` 被预读逻辑误判成命名元组，
  报「无法识别的主表达式类型 'Arrow'」；`Object` 还报「不支持的类型注解」。
  两处类型表补 `object`/`function` 并改为大小写不敏感匹配。
  装饰器 wrapper lambda 补 `:int` 注解后，装饰器两组测试 19 → 10 失败。
  见 change（`2026-11-15-lambda-object类型/test.md`）。

- [x] **IL 装饰器无法调用被装饰函数**：装饰器应用后拿到的是解释器 wrapper 闭包，
  自由变量 `f` 不在 IL 作用域，编译后运行报 `NAME_ERROR 'f' 未定义`。
  现在装饰器函数不直接编译函数体，改为生成转发委托：
  `IlRuntimeFunctions.InvokeByName(函数名, 参数)` 运行期调用解释器里已装饰的函数；
  返回类型/参数类型沿用原注解并按原规则注册。
  `Compiler.Functions` 当时 45 → 35 失败；该阶段仍有未标注 lambda 参数/旧语法用例。
  2026-11-18 当前工作区另有错误整数返回值、闭包变量不可见与异常断言差异，见顶部核对摘要。
  见 change（`2026-11-15-IL装饰器转发/test.md`）。

- [x] **IL 命名实参 / params / 高阶函数参数调用**：这些调用编译期匹配不到签名，
  落入 `VisitInstance` 兜底后静默返回 null（相关测试此前是假通过）。现在兜底重建
  `Instance` 交给解释器执行（`IlRuntimeFunctions.InvokeInstance`），命名实参重排、
  params 收集、未定义函数 NAME_ERROR 全部与解释器一致；函数参数调用补
  `ArgumentIndices` → `ldarg` 分支。
  `ValueTypeTo{Double,Char,Bool}Method` 改用 `EmitLoadWrapped` 并返回原生值，
  `(42).ToDouble()` 不再生成无效 IL。`Compiler.Functions` 53 → 45 失败。
  见 change（`2026-11-15-IL函数调用回退/test.md`）。

- [x] **IL 相互递归函数无法编译 / 未定义函数静默返回 null**：编译函数体时后定义的函数还没
  注册，`isEven` 调用 `isOdd` 被当类名处理，旧行为发 `ldnull` 静默返回 null。
  现在未知名字改为运行期 `IlRuntimeFunctions.InvokeGlobal`（按名字+参数个数查
  `ImportInfos`），相互递归可用、真正未定义的函数报 `NAME_ERROR`。
  `ErrorCompilationTests` 12 项按当前语义重写（合法代码断言成功、名字错误断言编译错误、
  除零/越界/未定义函数断言执行期错误、`var` 断言语法错误），`Compiler.Error` 12 → 0 失败。
  见 change（`2026-11-15-IL相互递归/test.md`）。

- [x] **枚举作为函数参数/返回类型在 IL/VM 下不可用**：函数体先于顶层枚举声明编译，
  `ResultType.Success` 解析为「变量未声明」；返回类型 `-> ResultType` 又因成员推断为 int
  报「返回类型不匹配」；VM 的 `as int` 对 `EnumLangValue` 抛 CastError。
  现在编译器在生成 IL 前预注册顶层枚举；返回类型兼容 `enum ← int`；
  VM CastType 先取出语言值的底层原生值。三模式均可运行，`Compiler.Types` 14 → 10 失败。
  见 change（`2026-11-15-IL-as转换/test.md` 第 5 节补充）。

- [x] **IL 的 `as` 转换对 object / string 直接拆箱**：`"1" as int` 在 IL 抛
  `InvalidCastException`，解释器会解析字符串得到 1。现在 `object` / `string` /
  `LangValueType` 左侧改走 `IlConversionHelper`（Wrap 后 `Converse` 再 Unwrap），
  与解释器一致，无效字符串报语言 `FORMAT_ERROR`。新增/修正 6 个表达式用例。
  见 change（`2026-11-15-IL-as转换/test.md`）。

- [ ] **IL 整数溢出语义与解释器/VM 不同**：`2147483647 + 2147483646` 在 IL 是 unchecked
  32 位回绕（-3），解释器/虚拟机提升为 double（4294967293）。让 IL 也提升需要表达式
  静态类型从 int 变为 double/object，与「int 局部变量接收」冲突，需先定语义。
  `ArithmeticTests.VeryLargeNumbers_...` 已标记 `Skip` 并注明原因。

- [x] **IL 两个 `object` 操作数的大小比较恒为 false**：数组/字典元素赋给变量后静态类型是
  `object`，`item < min` 两侧都是 object 时没有动态分支，落到数值/引用比较恒 false
  （for-in 求 min/max 得到首元素）。新增 `IlDynamicComparison`，四个顺序运算符在
  「两侧都是 object」时按语言值语义分派；`SyncIterator_ArrayElementIteration` 转通过。
  另两个同步生成器用例按 MODE_SUPPORT 改为断言明确的编译期错误。
  新增 2 项回归；`Compiler.Collections` 3 → 0 失败。
  见 change（`2026-11-15-IL动态比较/test.md`）。

- [x] **IL 枚举成员访问返回枚举类型**：`EmitStaticMemberAccess` 发射 `ldc.i4` 却返回 `Color`，
  调用方按枚举装箱：`PrintLine(Color.Red)` 显示 `Red`、`Color.Red.ToStr()` 为
  `NativeObject(Red)`、`match` 匹配不到 `case 0`。现在返回 `typeof(int)`，类型推断同步为 int；
  `MatchExpression_EnumValue` 用例改为文档语法（逗号分隔成员、数值 case）。
  `Compiler.Collections` 4 → 3 失败，新增 `EnumValueIlTests`。
  见 change（`2026-11-15-IL枚举成员值/test.md`）。
  遗留：`type(枚举值)` 在解释器返回 `Value`、IL/VM 返回 `Int32`。

- [x] **IL 包装字典的 ToStr 输出空元组**：`d.ToStr()` 在 IL 下得到 `{(), ()}`——`IlValueBridge.Wrap`
  构造的 `DictionaryLangValue` 只填 `Tuples`、`Value` 为空，而显示/索引读的是 `Value`。
  现在包装后立即解释器求值一次填充；`d.ToStr()`、字段字典 ToStr、`d["a"]` 均与解释器一致。
  新增 1 项 IL 回归。见 change（`2026-11-15-IL字典包装显示/test.md`）。

- [x] **字典缺失键的行为三模式不一致**：`d["missing"]` 此前解释器返回 null、VM 抛
  `KEY_ERROR`、IL 原生字典抛 CLR `KeyNotFoundException`（`dict()` 分支返回 null）。
  2026-11-15 定为「缺失键报错」（安全访问用 `GetOrElse` / `TryGet`）：解释器
  `DictionaryLangValue.Get` 与 `GetValue` 抛语言 `KeyError`，IL 原生字典索引改走
  `IlIndexHelper.GetDictionaryItem`（内部 `Contains` 检查后抛 `KeyError`），VM 的
  `GetValue` 也改为 `KeyError`。三模式统一输出 `KEY_ERROR`；新增 9 项跨模式回归。
  见 change（`2026-11-15-字典缺失键语义/test.md`）。

- [x] **IL 链式比较 `a < b < c` 的行为错误**：`a <- 5; b <- 10; c <- 15; a < b < c` 在 IL 下
  曾得到 `true`（把第二段的 bool 当 int32 用 Clt 比较），而解释器与虚拟机都会报
  「不支持 Less / bool 与 int 不能比较」。语义上链式比较等价于 `(a < b) < c`，第二段是
  bool 与 int 比较，属于错误用法；现在 IL 在编译期抛
  `InvalidOperationError「布尔值不支持大小比较」`（`<` / `>` / `<=` / `>=` 四处都拒绝），
  `ChainedComparisons_...` 改为断言该错误。见 change（`2026-11-15-IL链式比较/test.md`）。

- [x] **`Compiler.Basic.ErrorHandlingTests` 旧异常预期已关闭**：
  [类型错误运行期化](2026-11-15-IL类型错误运行期化/test.md) 后，
  2026-11-18 当前工作区该类 28 通过、2 跳过，未再复现早期 10 项失败。

- [x] **IL 字符串 `Length()` 可能导致进程崩溃**：原生字符串现在使用 CLR 字符串长度，返回原生整数；原崩溃用例及别名、链式调用回归通过，见 测试报告（`2026-11-11-12-37-IL字符串长度测试.md`）。
- [x] **IL 嵌套异常测试的包含断言**：调整为 `Assert.Contains(text, substring)` 的实际参数顺序，异常与字符串相关回归 13 项通过；见 测试报告（`2026-11-11-13-51-IL异常处理回归测试.md`）。

- [x] **IL 类测试中的继承语法失败**：`InheritanceTests` 已由已废弃的 `class Derived : Base` 迁移为
  `class Derived extends Base`；同时 `super.method()` / `super.init()` 改为直接调用父类实现，避免虚分派回到
  子类重写方法而栈溢出。现已支持受保护字段、静态字段/方法继承，以及子类中按签名调用父类初始化器和方法；
  `InheritanceTests` 全组 10 项通过，见 前序测试报告（`2026-11-13-10-00-IL继承语法与super调用.md`） 与
  成员访问测试报告（`2026-11-13-10-30-IL继承成员访问.md`）。
- [x] **`SelectStatementTests` 整组测试完成性**：带 10 秒挂起超时复跑 21 项全部通过；先前运行卡住的原因未复现。见 测试报告（`2026-11-11-13-55-IL-select整组复测.md`）。
- [x] **IL 类实例化测试残留问题**：空花括号在未定型局部变量与类字段中现保留为动态集合；首次字符串键写入会提升为空字典，而后续字典读取、`GetOrElse` 与成员后索引赋值均由运行期分派处理。字段初始值和初始化器参数的数值转换、`number.Sqrt()`、动态数值比较也已回归。`ClassInstantiationTests` 全组 15 项通过；见 构造器数值转换报告（`2026-11-13-10-40-IL构造器数值转换.md`）、数值 Sqrt 报告（`2026-11-13-10-50-IL数值Sqrt实例调用.md`）、动态数值比较报告（`2026-11-13-11-00-IL动态数值比较.md`） 与 空集合定型报告（`2026-11-13-11-40-IL类空集合与字典索引.md`）。

- [ ] **抖动用例**：`Old8Lang.Tests/Interpreter/Async/TaskAPITests.TaskAPI_WhenAny_HandlesFirstCompletedTask`
  让 `Task.Delay(10)` 与 `Task.Delay(100)` 竞争 `Task.WhenAny` 并断言 10ms 的先完成；
  全量并行测试时线程池繁忙会翻转结果。单独运行稳定通过（3/3）。
  建议把延迟差拉大（例如 10 vs 1000）或改为断言「结果是 fast 或 slow 之一」。

- [ ] **抖动用例**：`Old8Lang.Tests/Compiler/Threading/ThreadTests.Run_ThreadExecutionOrder_WorksCorrectly`
  三个线程各自 `Thread.Sleep(10)` 后向同一个 `ListLangValue` 追加元素，测试固定等待 100ms
  再断言数量为 3。单独复跑 3 次有 1 次得到 2（全量运行时也会偶发）；
  列表追加本身没有同步，等待时间也偏短。建议改用锁/原子计数，或把等待改为轮询直至 3 个元素。

- [ ] **SerializationLib 编译器/解释器测试共用临时文件**：同时运行两组
  `SerializationLibTests` 时，编译器侧 `MsgPackSerializeToFile_ShouldSerializeToFile`
  因 `./test_msgpack_data.bin` 已被另一组删除而失败；各自单独运行均通过。
  建议文件名带模式或测试类前缀，并在清理时忽略文件不存在。

- [ ] **VM 模式 3 个用例直接失败**：
  - `TestFiles/VirtualMachine/DestructuringAssignmentTest.old8`：解析阶段报错（`StatementParser.Main.cs:444`）。
  - `TestFiles/VirtualMachine/vm_gap_6_error_position.old8`：VM 执行阶段抛异常（错误定位用例，末行没有 `error` 标记）。
  - `TestFiles/VirtualMachine/legacy/vm_operator_overload.old8`：VM 执行阶段抛异常（原 `VMTests/`）。
  - `TestFiles/VirtualMachine/array_sort_vm_test.old8`（2026-11-27 回归顺带发现）：期望旧的字符串长度排序结果，与 2026-11-18 改为字典序（`CompareOrdinal`）后的 `a,bb,ccc,dd` 冲突。
  另有 `legacy/control_flow.old8`、`legacy/vm_decorator.old8` 为原 `VMTests/` README 已记录的已知失败。

- [ ] **语法测试 5 个既有失败用例**（`run_syntax_tests.sh` 89 例中 5 失败，均与目录合并无关）：
  - `TestFiles/SyntaxTests/test_syntax_structure_error.old8`、`test_lexical_error.old8`：期望失败的用例，
    但末行没有 `error` 标记，被 runner 判为失败；
  - `TestFiles/SyntaxTests/test_generics.old8`：文件里仍用 `#` 作注释（应为 `//`），解析直接失败；
  - `TestFiles/SyntaxTests/test_simple_edge_cases.old8`、`test_more_edge_cases.old8`：
    主表达式解析报错（`Colon` / `Assignment`）。

- [ ] **全量测试完成性**：2026-11-11 默认档在 30 秒无进展后中止，已完成 4252 通过、409 失败、28 跳过；当时活动测试包含 Python 外部模块用例，该筛选隔离运行 2 项通过，挂起根因未确定。失败集中于 IL 异步、函数、类、表达式和集合等，需逐组复现并核对既有基线。见 默认档报告（`2026-11-11-13-57-默认档超时测试.md`）及 隔离报告（`2026-11-11-14-00-Python外部模块隔离测试.md`）。
  - 2026-11-14 阶段 3 后端拆分回归：`NetLibTests` 的 8 个用例并发访问
    `jsonplaceholder.typicode.com`，连续全量运行时会因外部限流/连接抖动出现 1~7 项失败，
    隔离或重跑可恢复；排除该组后解释器 1959 通过、1 跳过。属测试隔离/外部依赖问题，
    未在本轮修改。
  - 2026-11-14 架构迁移阶段 2 基线复跑：测试主机在 IL 编译异常后中止，已报告 565 通过、28 失败、1 跳过。可见失败包括可选参数构造器 `InvalidProgramException`、枚举/异步返回、以及原生 `List<object>` 上 `IsEmpty`/`Find`/`Join`/`FirstOrDefault` 的分派；本轮未改这些路径，详见 阶段 2 基线报告（`2026-11-14-01-10-阶段2固定基线与依赖图.md`）。
- [ ] **IL SpawnTests 全局状态恢复与分档仍需整理**：旧 spawn 调用与带参互操作已修，
  2026-11-18 当前工作区 13 项全通过。源码仍在编译前后直接切换全局
  `IlVerificationEnabled`，缺少 `try/finally` 恢复；使用线程/Sleep 的用例仍未标
  `Category=Performance`，需按测试指南分档。不要把这些工程问题与已关闭的 API 迁移混为一项。

- [x] **IL 泛型类的函数值字段调用仍待实现**：已于 2026-11-28 完成三模式支持并补齐 `CrossModeDefectsRegressionTests.GenericClass_CallableFunctionField_WorksAcrossAllThreeModes` 回归。

- [ ] **IL 测试覆盖复核**：原清单列出的 54 个具体文件中已有 53 个同名文件；未找到 `StandardLibraryImportTests.cs`。按实际场景核实标准库导入、并发和端到端覆盖，不以文件名作为充分证据。
  2026-11-15 补充：并发全局函数里仍有约 40 处手写 `parameters[i].Accept(...)` + `Call`，
  若形参是 `int` 且实参可能是 `object`/`LangValueType`，同样需要拆箱；本轮只覆盖了
  `*Dispose` 及同文件的 int 参数调用点，其余未逐一核对。

## VM 性能

- [x] **VM 值表示仍以 `object` 装箱**：2026-11-27 已按 P4 §4.3 三阶段方案完成类型化值表示改造：
  在 `Old8Lang.Runtime` 引入 24 字节标记联合 `readonly struct VmValue`（含边界共享布尔与 `-128..4095`
  小整数缓存），升级 `ConstantPool.GetConstantValue`、`Instruction.IntOperand`、`CallFrame.Locals`（池化
  `VmValue[]`）、`GlobalSlot`（单线程零锁 + 并发无锁 SeqLock 防撕裂）、`UpValueCell` 与 `ClosureEnvironment`
  （含 `IsEmpty` 全局槽位直通）；在 `Old8Lang.VirtualMachine` 引入 `VmStack` 替代 `Stack<object?>`，
  算术/比较/逻辑/类型校验与 `Call`/`CallDynamic` 位置快速路径全面零装箱，移除 `VmBoxed`。
  Release 实测：`1M sum <- sum + 1` 循环由 **45.78 MB（45.78 B/次）** 降至 **944~1,184 B（~0 B/次）**，
  `HighArgCount` 由 7.2 MB 降至 **1.6 KB**，`HighThrowRate` 由 5.0 MB 降至 **1.3 KB**，
  `LargeClosureCapture_HighFreq` 由 14.8 MB（诊断 13.6 MB）降至 **1.9 KB（诊断 5.4 KB，耗时 -21%，GC 6 → 0）**，
  `LargeFile_10k/50k` 执行分配降 **37%~39%**。已新增 `VMValueRepresentationTests`（8 项）并收紧
  `VMPerformanceSmokeTests` 分配上限至 1 MB；VM 单测 1,149/1,149、架构边界 22/22、Benchmarks 31/31 全通过。
  见 change（`2026-11-27-VM类型化值表示/test.md`）。

## 解释器测试覆盖待复核

2026-11-27 已逐项核对实现与现有覆盖并补齐全部 10 类场景（15 个解释器测试类共 134 通过、0 失败，其中新增 114 项用例），实测记录与过程中发现的解释器实现缺口见 [测试报告](2026-11-27-解释器测试覆盖复核/test.md)。

### 1. 线程同步与并发原语测试 (Threading/)

**优先级**: 中高

`ThreadTests.cs` 覆盖了线程本身，2026-11-27 已补齐解释器侧线程同步与 7 类并发原语测试（连续 3 次运行 18/18 稳定通过）。

- [x] `ThreadSynchronizationTests.cs` - 线程同步测试（7 项）
  - 多线程访问共享资源（`Lock` / `LockedVariableLangValue`）
  - 锁与临界区行为（`Mutex`）
  - 同步顺序与协调场景（`CountDownLatch` / `Channel` / `CyclicBarrier`）

- [x] `ConcurrentPrimitiveTests.cs` - 并发原语测试（11 项）
  - Mutex（互斥锁）加锁/释放/超时竞争
  - Semaphore（信号量）计数与并发上限
  - AtomicInt（原子整数）全操作与并发自增
  - Channel（通道）无界/有界收发与超时
  - ReadWriteLock（读写锁）、CountDownLatch（倒计时锁）、CyclicBarrier（循环栅栏）及并发工具函数

**AST 节点支持**: `LockedVariableLangValue`, `ThreadStaticMethodWrapper`

### 2. .NET 托管方法绑定测试 (Modules/Extern/)

**优先级**: 中高

绑定 C# 方法原本写作 `native func ... from ...`，解析器现已统一为 `extern`，`native` 关键字不再存在
（见 `LangParser/Parsers/StatementParser.SpecialStatements.cs`）。
2026-11-27 已在 `Modules/Extern/` 下与 `NativeDllExternTests.cs` 等并列补齐托管 DLL 导入测试。

- [x] `ManagedDllExternTests.cs` - .NET 托管 DLL 导入测试（9 项）
  - 方法块导入：`extern "C#:System" Math { func Pow(x:double, y:double) -> double, ... }`
  - 单个方法导入：`extern "C#:System" Math func Pow(x:double, y:double) -> double`
  - 带别名的导入：`func Pow(x:double, y:double) -> double as Power`
  - 导入自定义 DLL：`extern "dotnetdll:<assemblyPath>" ManagedDllExternTestHelper { ... }`
  - 命名参数调用：`Pow(y: 3.0, x: 2.0)`
  - 程序集/类型/方法不存在时的异常处理（`ImportError` / `InvalidOperationError`）
  - `cs:` / `csharp:` 前缀与 `C#:` 的等价性

> 已实测（2026-10-09，解释模式；2026-11-27 纳入单测）：上述 `{ func ... }` 块、单个 `func`、`as` 别名、命名参数四种写法均可用；
> 而 `extern "C#:System" Math *` 和 `extern "C#:System" Math as SysMath` 会报
> `语法错误：缺少 'func' 关键字`——这两种写法属于 `NativeStatement` 解析路径，带 `C#:` 等托管前缀时走的是
> `ParseExternStatement`。

**AST 节点支持**: `NativeStatement`, `NativeAnyLangValue`, `NativeStaticAny`

### 3. 模块命名空间与重载测试 (Modules/)

**优先级**: 中

2026-11-27 已在 `Modules/Namespace/` 与 `Modules/Reload/` 补齐命名空间与缓存/热重载测试（核对中发现的别名导入共享作用域变量覆盖问题已另记上方缺口）。

- [x] `ModuleNamespaceTests.cs` - 模块命名空间测试（5 项）
  - 模块命名空间隔离
  - 跨模块符号访问（函数、常量、类）
  - 模块符号冲突处理（别名消歧、提升导入覆盖、模块闭包作用域）

- [x] `ModuleReloadTests.cs` - 模块重载测试（4 项）
  - 模块热重载（磁盘文件修改 + 清缓存后加载新实现）
  - 模块缓存管理（`CacheManager` / `ModuleLoader` 命中、绕过、单项/全量清理）
  - 模块依赖更新与单次运行重复导入不重复执行副作用

**AST 节点支持**: `UnifiedModule`, `ImportInfo`, `LazySymbolProxy`

### 4. LINQ 查询语法 group by / into 延续测试 (Linq/)

**优先级**: 中低

2026-11-27 已补齐查询语法 `group by` 与 `into` 延续执行测试（核对中发现的字面量集合/元组 AST 缓存复用及不支持链式多段 `into` 问题已另记上方缺口）。

- [x] `LinqGroupByTests.cs` - 查询语法 group by 测试（6 项）
  - `group x by <key>` 基础用法与元素投影
  - 分组后聚合（Count / Sum / Max）
  - 多键复合分组
  - 分组前后过滤与空数据源

- [x] `LinqQueryContinuationTests.cs` - 查询延续测试（5 项）
  - `select ... into` 与 `group ... by ... into` 用法
  - 延续中的 `let` / `where` / `orderby` / `group by`、二段查询链与作用域不泄漏

**AST 节点支持**: `GroupByClause`, `QueryContinuation`

### 5. Super 表达式测试 (Classes/)

**优先级**: 中低

2026-11-27 已补齐专用 `SuperExpressionTests.cs`（核对中发现的三层及以上连续 `super` 调用栈溢出已另记上方缺口）。

- [x] `SuperExpressionTests.cs` - super 关键字测试（7 项）
  - super 调用父类方法（含重写与重载）
  - super 访问父类字段
  - super 在构造函数中的使用（`super.init` / `super.ParentClass` / 无构造父类）
  - super 返回值链式调用、继承祖类方法及错误场景（无父类 / 成员不存在）

**AST 节点支持**: `SuperExpression`, `SuperProxy`

### 6. 错误处理增强测试 (EdgeCases/)

**优先级**: 中

2026-11-27 已补齐解析错误、运行时类型/越界错误及循环引用与递归深度测试（核对中发现的 `MaxRecursionDepth = 1000` 高于托管栈及调用非函数值不报错已另记上方缺口）。

- [x] `ParserErrorTests.cs` - 解析器错误测试（12 项）
  - 不完整语句与非法语法处理
  - 错误提示信息与行号位置准确性
  - 错误后同解释器实例继续解析合法代码

- [x] `RuntimeTypeErrorTests.cs` - 运行时类型错误测试（16 项）
  - 空引用错误（成员/方法/索引/算术）
  - 越界访问错误（数组/列表/字符串/元组正负越界及原始负下标消息）
  - 类型转换、未定义函数调用与实参类型/数量不匹配

- [x] `CircularReferenceTests.cs` - 循环引用测试（8 项）
  - 数据结构循环引用（自引用/互引用列表与字典）
  - 函数递归深度（合法递归、相互递归与触发 `MaxRecursionDepth` 守卫）
  - 类实例循环引用（自引用节点与双节点环遍历及断环）

> 注：`Modules/ErrorHandling/CircularDependencyTests.cs` 测的是模块循环依赖，与此处的数据结构循环引用不是同一类场景。

### 7. 测试工具类测试 (Testing/)

**优先级**: 中低

2026-11-27 已补齐 `Assert`、`TestRunner`、`Mock`（`MockLibClassLangValue`）全部内置测试工具类的解释模式测试。

- [x] `TestUtilitiesTests.cs` - 测试工具类测试（8 项）
  - Assert 类用法（相等/布尔/空值/比较/字符串/集合/异常/类型断言与自定义错误消息）
  - TestRunner 类用法（`Describe`/`It`/`Test`/`RunTest`/`RunAll`/`GetStatistics`/`Clear`）
  - Mock 类用法（`CreateMock`/`CreateMockWithReturns`/`CreateStub`/`CreateSpy`/`RecordCall`/`Call`/`Verify*`/`Reset`）

**AST 节点支持**: `AssertClassLangValue`, `TestRunnerClassLangValue`, `MockLibClassLangValue`

### 8. 类型模板测试 (Types/)

**优先级**: 中

2026-11-27 已补齐 `TypeTemplateTests.cs`（核对中发现的 `where T: U` 未解析用户类继承问题已另记上方缺口）。

- [x] `TypeTemplateTests.cs` - 类型模板测试（8 项）
  - 泛型类型参数、`TypeTemplate` 元数据与泛型继承
  - 类型约束（`new()` / `class` / `struct` / 接口与基类 / `where T: U` 及违约拒绝）
  - 类型参数推断
  - 嵌套泛型类型（`Box<Box<int>>`、`Pair<Box<int>, Box<string>>`、`Box<list<int>>`）

**AST 节点支持**: `TypeTemplate`, `GenericParameter`, `GenericInstanceExpression`

### 9. 常量优化测试 (Expressions/)

**优先级**: 低

经 2026-11-27 源码核实：`ConstantLangValue` 是模块通配符导出常量时使用的 `ImportInfo` 包装类（`SymbolExtractor.WrapConstantsAsImportInfo`），并非常量折叠 AST 节点；解释器当前无独立 AST 常量折叠优化遍。已按实际实现补齐测试。

- [x] `ConstantFoldingTests.cs` - 常量表达式与 `ConstantLangValue` 测试（4 项）
  - 常量算术/布尔/字符串表达式求值
  - `:const` 常量声明不可变性与跨表达式/函数/循环传播
  - `ConstantLangValue` 包装属性及 `import * from "..."` 模块常量导出

**AST 节点支持**: `ConstantLangValue`

### 10. 枚举增强测试 (Statements/)

**优先级**: 低

经 2026-11-27 源码核实：Old8Lang 无按位运算符，`EnumTemplate` 仅支持成员访问。已在 `EnumTests.cs` 中按当前实现补充 4 项增强用例（共 24 项通过）。

- [x] 在现有 `EnumTests.cs` 中补充（新增 4 项，共 24 项）：
  - 枚举 2 的幂标志位声明、常量表达式初始化与标志组合检测
  - 枚举 `as string` 成员名转换、`EnumLangValue` 运行时属性、`EnumTemplate.HasMember`、跨枚举比较与静态方法调用报错

**AST 节点支持**: `EnumInit`, `EnumTemplate`

## Language Server 与 VS Code 扩展

### P0：可靠性与已有能力完善

- [x] 为所有已注册 Handler 补齐稳定的单元测试和协议级测试（已补齐 `DebugProfilerHandlersTests`、`ScopedReferencesHandlerTests`、`TextDocumentSyncHandlerTests` 及基于 `Pipe` + JSON-RPC 2.0 覆盖全部 26 个注册 Handler 的 `LspProtocolEndToEndTests`，详见 `changes/2026-11-26-LSP-P0可靠性和能力完善/test.md`）。
- [x] 为符号表引入作用域链，正确处理局部变量、变量遮蔽和同名符号（新增 `SymbolScope` 词法作用域树，接入全部导航/补全/悬停/引用/重命名/高亮/语义令牌 Handler，并新增 `ScopeChainTests`）。
- [x] 完善成员访问的类型推断，覆盖继承成员和静态成员补全（新增 `TypeResolver`，升级 `MemberChainAnalyzer` / `CompletionHandler` / `SignatureHelpHandler` 支持多级继承、Mixin、接口、`this`/`super`、静态成员、多级属性/方法链与跨文件类，并新增 `MemberTypeInferenceTests`）。
- [x] 改善诊断：类型不匹配、未使用变量、成员不存在，以及跨文件语义分析（重构 `SemanticAnalyzer` 为 AST + `RootScope` + `WorkspaceIndex` 驱动并接入 `Program.cs`/`DocumentManager`，扩充 `SemanticAnalyzerTests`）。
- [x] 评估并逐步替换 token-based location workaround；Parser 修复后保留回归测试再移除（已修复 `LangToken`、`ClassParser`、`StatementParser`、`LangParser` 及相关 AST 节点位置，移除 `SymbolTableBuilder` 的 token 扫描变通与 `DocumentLinkHandler` 正则变通，保留 `PositionBasisTests` 回归）。

### P1：尚未实现的 LSP 能力

- [x] 实现 Inline Values，并与可用的调试运行时状态建立协议映射（新增 `InlineValuesHandler` 实现 LSP 3.17 `textDocument/inlineValue`，支持标准 DAP `InlineValueVariableLookup` / `InlineValueEvaluatableExpression` 映射及经 `old8lang/updateDebugSnapshot` 同步的暂停变量快照 `InlineValueText`，详见 `changes/2026-11-27-LSP与VSCode扩展完善/test.md`）。
- [x] 实现 Call Hierarchy / Type Hierarchy（基于 OmniSharp 0.19.9 实现 `CallHierarchyHandler` 与 `TypeHierarchyHandler`，在 `SymbolTableBuilder` 提取函数/方法/顶层调用点，支持单文件与跨文件继承/接口/Mixin 查询，已注册并补齐单测、协议测试与文档）。

实现新 Handler 时需要同时完成：协议注册、能力声明（如需要）、单元测试、集成测试和文档更新。

### P2：性能与工程质量

- [x] 为 token 按行建立索引，降低位置查找的线性扫描成本（新增 `TokenLineIndex` 并接入 `DocumentParseResult`、`SymbolFinder`、`MemberChainAnalyzer`、`SelectionRangeBuilder`、`SignatureHelpHandler`、`InlayHintHandler` 与 `DocumentHighlightHandler`，5000 行实测加速 382x）。
- [x] 为实时诊断增加防抖和后台执行，避免大文件编辑卡顿（新增 `DiagnosticsScheduler` 与 `DocumentManager` 暂存/按需解析机制，接入 `TextDocumentSyncHandler` 与 `Program.cs`，15 次连续编辑合并为 1 次后台发布且不阻塞补全/导航）。
- [x] 增加 Language Client ↔ Language Server 的端到端测试（新增 C# 子进程级 `LspClientServerProcessEndToEndTests` 与 `vscode-old8lang/test/run-tests.js` 基于 `vscode-jsonrpc` 的真实 stdio Client↔Server 与 DAP 端到端测试）。
- [x] 增加大文件和实时诊断延迟的性能基线（新增 `LspPerformanceBaselineTests`，覆盖 1000/2000/5000 行大文件解析、索引查找与防抖延迟的 `PerformanceSmoke` 与 `Performance` 双档基线）。

### 尚未完成

- [x] 实现 Debug Adapter（运行时新增 `VmDapServer` 与 `Old8Lang.App --dap` 命令，支持行/函数/条件断点、按行单步、调用栈、局部/全局变量与对象字段查看、表达式求值；扩展新增 `vscode-old8lang/src/debugAdapter.ts`、`debuggers`/`breakpoints`/`old8lang.debugFile` 贡献点及 DAP→LSP 变量快照跟踪器）。
- [x] 语法高亮的字符串插值、文档注释和嵌套结构改进（升级 `vscode-old8lang/syntaxes/old8lang.tmLanguage.json`，支持 `///` 多风格文档注释标签、插值 `{{`/`}}` 转义与嵌套花括号、文件头/预编译指令、装饰器与泛型/括号嵌套，并纳入 `npm test` 校验）。

- [x] **私有成员补全**：核验并完善外部、本类 `this`/同类实例、子类 `this`/`super`、静态类名与方法内裸名称的 `private`/`protected`/`public` 可见性过滤，修复空可访问成员列表回退到全局补全及悬空 `.` 语法错误丢失符号表的问题，新增 `CompletionHandler_PrivateMemberTests`。
- [x] **补全测试**：新增 `CompletionHandler_PrivateMemberTests`、`CompletionHandler_AdvancedCoverageTests`（泛型、表达式、Extern、文件头指令、联合/交叉类型、多级成员链、类型推断与文档注释）及 `CompletionHandler_StressAndFuzzTests`（多线程并发与确定性模糊测试），并启用 `CompletionHandler_ContextAwareTests` 私有成员断言。

## 长期研究与调试方向

- [ ] 评估类型推断的调用处推断、泛型及联合类型、多分支控制流和类型缩窄；先核实当前实现。[高级主题](../Docs/ADVANCED_TOPICS.md#限制和已知问题)
- [ ] 评估图形调试界面、增强条件表达式、IL 和远程调试、内存监控。[开发者工具](../Docs/DEVELOPER_TOOLS.md)

## 历史核对中移出的过期条目（2026-11-11）

- 线程样例缺少 `Start()`：所列五个文件现已包含启动调用。
- DatabaseLib 跨模式测试同名内存库：解释器使用 `itestdb_`、IL 使用 `testdb_` 前缀。本次相关筛选测试通过。
- 无类型注解递归栈溢出：原文后续明确记为已修，相关测试仍应在后续全量验证中覆盖。
- 原 IL 清单中的已有同名测试文件不再逐一列为“缺失”；其余覆盖仍需按场景审查。
- 原 VM 性能中的全局槽位、布尔装箱和基准噪声门禁已有完成记录；性能报告与原始复现的现存部分在 `archive/Reports/`。

2026-11-11 测试结果见 待办核对测试报告（`2026-11-11-08-30-待办核对测试.md`）。

## 2026-11-15 复核更新

该轮修复已覆盖 VM `in`、同类型集合 `+`、范围端点排除、集合 `.Length`、null 显示、
字典缺失键错误、IL match 高级模式与作用域、mixin/接口默认方法、反射及命名实参校验等。
逐项范围以对应 change 的 `test.md` 为准；本次没有把这些历史结果当作三模式全量复测。
`Docs/MODE_SUPPORT.md` 仍有历史说明与后续修复记录不一致（例如 IL 泛型/装饰器总括限制、
顶层 for 作用域，以及“共同差异”表中的已统一行为），后续需按现有报告逐项校正。

- [ ] **IL 异步生成器的线程占用**：当前逐项握手在 yield 等待消费者时占用线程池线程；大量并发或长期暂停的生成器可能造成线程池压力。后续应将 yield 纳入异步状态机实现真正的非阻塞挂起，并补并发压力测试。
