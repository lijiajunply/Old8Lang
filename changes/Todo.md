# Old8Lang 统一待办清单

核对日期：2026-11-11。此文档合并原根目录、解释器测试、IL 测试和语言服务器待办；现存的历史测试报告已归档到 `archive/Reports/`；更早的报告已不在仓库中，下文只保留其文件名以便追溯（需要时从 git 历史取回）。

**状态约定**：下列历史缺陷除非注明本次验证，否则均为「待复核」，不是本次已复现；测试文件存在也不能证明对应场景覆盖完整。复现、修复后请更新本清单并按测试指南生成报告。

## 优先顺序

1. 排查会使测试进程崩溃的 IL 问题，再核实其他 IL 结果错误和跨模式差异。
2. 修复测试隔离与不稳定问题，补齐关键场景覆盖。
3. 完善 LSP 作用域、诊断及调试集成；按收益推进 VM 性能和长期能力。

## IL 模式与跨模式缺陷

以下条目来自历史复现，尚未逐项重跑。报告见 IL 缺陷验证（`2026-10-23-23-30-IL模式缺陷修复验证.md`）、跨模式语义修复（`2026-10-24-00-30-P1跨模式语义修复测试.md`） 和 VM 残留修复（`2026-10-24-09-45-P2虚拟机模式残留修复测试.md`）。

- [x] **IL 比较运算装箱顺序错误导致测试进程崩溃**：`object` 与数值混合比较时先把两个操作数都压栈、
  再统一 `box`，`box` 只作用于栈顶，因此「数值在左、`object` 在右」（如
  `i >= this.data.Count()`，`data` 是未标注的空集合字段）会把右侧 `object` 装箱、左侧整数保持未装箱，
  随后 `callvirt IComparable.CompareTo` 把整数当 `this` 引用使用，生成无效 IL，运行期直接段错误、
  测试主机消失。六个比较运算符的该分支现改为「发射一个操作数就按位置装箱」。
  `Compiler.Classes` 与 `Compiler.Expressions` 两组由「测试运行中止」恢复为可完整运行
  （Classes 110 项、Expressions 232 项），其余 17 组通过/失败数与基线完全一致。
  见 change（`2026-11-14-IL比较装箱崩溃/test.md`）。修复后暴露的 `int?` 返回 `null` 触发 NRE
  仍是独立缺口（见下文“可空返回类型”一条）。

- [ ] **IL 函数 `return` 的类型转换既有缺口（待复现）**：2026-11-14 迁移 `ReturnStatement`
  辅助代码时发现，`CompilerVisitor.VisitReturnStatement` 的 try/finally 分支只做装箱/拆箱、
  没有数值转换；`string` 返回的兼容判断比实际 IL 转换更宽松；`IsNumericType` 不含
  `uint`/`ulong`/`ushort`/`sbyte`/`decimal`/`char`。迁移前后行为一致，未在本轮修复。

- [ ] **泛型目标类型的扩展方法 IL 支持**：`extension list<T>` 现会保留目标泛型参数及
  `where` 约束；IL 桥接在接收者、参数和嵌套集合之间保留引用关系并同步原地修改，`return this`
  可继续链式调用。覆盖嵌套集合、返回接收者、参数别名和 `struct` 约束；见
  测试报告（`2026-11-11-14-50-IL泛型扩展完整语义测试.md`）。空集合类型推断、多类型参数的交叉约束、字典与数组的别名及修改行为仍需验证。
- [x] **IL 扩展方法与顶层回写的自引用数组栈溢出**：桥接及类型推断避免展开循环集合，顶层回写复用已转换对象；自引用数组、列表、字典及别名的定向回归通过。见 测试报告（`2026-11-11-16-27-泛型扩展与循环引用回归.md`）。

- [ ] **方法调用结果上直接链式调用生成无效 IL。**
  - 2026-11-11 复核：`Count().ToStr()` 已可用；`FindAll(...).Count()` 的注册方法返回类型推断已修复，验证了三层链式调用和接收者只求值一次。
  - 2026-11-11：`MethodChainingRegressionTests` 与 `NestedAccessTests` 共 23 项通过，覆盖多层数组、字典、列表的读取、修改与链式接收者只求值一次。类成员、函数返回值及其他链式调用仍需复核；见 原测试报告（`2026-11-11-12-30-IL链式方法调用测试.md`） 与 嵌套集合复测（`2026-11-11-22-10-IL链式与嵌套集合.md`）。

- [ ] **IL 模式 lambda 的闭包函数值支持。**
  - 2026-11-11：直接赋值的 lambda 现可读取及写回创建点可见的普通局部变量；闭包环境在运行时绑定，每个 lambda 实例独立。`LambdaTests` 24 项通过，见 测试报告（`2026-11-11-19-00-IL-lambda-闭包局部变量.md`）。
  - 具名函数返回 `function` 的简单闭包链路现已可用，多个返回闭包实例彼此隔离；见 测试报告（`2026-11-11-20-27-IL-返回闭包函数值.md`）。2026-11-11 进一步确认：字典索引后的嵌套集合 `Length` 可在 `Filter` lambda 内使用；lambda 内继续创建并传给 `Reduce` 的嵌套 lambda 已避免捕获未初始化局部变量，嵌套数组以动态类型调用 `Reduce` 也可桥接执行。见 隔离报告（`2026-11-11-23-40-IL嵌套Lambda隔离.md`） 与 嵌套集合回归（`2026-11-12-00-10-IL嵌套集合Reduce.md`）。2026-11-13：集合索引得到的 `object` 函数值现可在运行期作为 .NET 委托调用；循环内保存的 lambda 分别捕获各轮局部快照，带参数调用得到 `10/11/12`；捕获 `this` 的 lambda 可读写其类实例私有字段。嵌套返回闭包、跨具名函数的顶层捕获和函数类型参数仍待处理，见 集合函数值测试（`2026-11-13-10-10-IL集合函数值调用.md`） 与 this 捕获测试（`2026-11-13-10-20-IL闭包捕获this.md`）。

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

- [ ] **剩余集合实例方法的专用 IL 迁移与死代码清理**：`IlNativeCollectionBridge` 已让全部 List / Dictionary /
  Array 实例方法在 IL 模式下可用，但热点方法（`Chunk` / `Flatten` / `Slice` / 排序等）目前每次调用都要包装、
  解析并走解释器实现；后续应按 `IIlNativeValueInstanceMethod` 机制恢复专用 IL，并删除 63 个方法中已失效的
  `GenerateIlInternal` 与 helper。嵌套字典元素在桥接边界也尚未做深度填充；桥接结果中的元组
  （`Zip()` 无 zipper、`WithIndex()`、`Dict.ToList()` 的元素）在 IL 侧还原为 `object?[]`，尚不支持直接按元组解构；
  IL 模式的原生字典无法表示解释器允许的重复键，`Add` 同键后 `Count` 语义与解释器不同。

- [ ] `Array.Sort()` / `Array.IsSorted()` 的 VM 兜底已修（本轮），但 `LangList*` 系列里仍有一批
  只认 `ILangList`、对 `object?[]` / `List<object?>` 直接抛错的 VM 实现。

- [x] **IL 排除端点后的反向范围生成错误**：2026-11-11 先让 IL 与解释器的旧算法对齐（`[1>~<1]` 生成 `2, 1, 0`）；2026-11-15 复核发现旧算法本身与语法文档冲突（`>`/`<` 只应排除端点、不应翻转方向），已改为区间收窄、等端点排他为空，见下方条目。历史报告：`2026-11-11-22-50-IL扩展范围.md`。

- [ ] **异步函数的装饰器仍不可用**：不是虚拟机独有——装饰器必须返回「异步函数值」，
  而 `async (x) -> ...` 异步 Lambda 目前在解释器下也会抛
  `NullReferenceException`（`Instance.Run`）。三种模式都还没有异步函数值表示，
  需要先补「异步 Lambda / 异步函数值」这一层能力，再让 VM 的 `CallAsync` 支持装饰器绑定。

- [ ] **元组模式解构 `(a, b) <- ...` 跨模式差异**：三种模式的基本元组赋值和交换已通过；IL 目前要求右侧静态类型为 CLR 元组，解释器还支持其他 `ILangList`，VM 集合右值及嵌套模式仍需复核。见 测试报告（`2026-11-11-13-42-三模式元组解构测试.md`）。

- [x] **解释器 `Thread.CurrentThread` 属性访问**：裸属性和无参调用现均返回执行中的线程；同时修正了无参调用原先包装新建线程的问题。见 测试报告（`2026-11-11-12-42-当前线程属性测试.md`）。
- [x] **IL `Thread.CurrentThread` 属性与调用**：两种形式均返回实际执行线程，并支持继续访问其属性。见 测试报告（`2026-11-11-13-33-IL当前线程测试.md`）。
- [x] **IL 其他 Thread API**：`Thread.Sleep` 与 `Time.GetUnixTimeMilliseconds()` 的组合现已通过；标准库 CLR `long` 返回值会归一化为语言 `double`，避免时间差计算按 `int` 拆箱。`Lock(value)` 现可在 IL 模式下创建共享锁变量，支持 `.Value`、`.Get()`、`.Set(value)` 与原子 `.Add(value)`；`Spawn` 会把运行期全局槽位和回写目标安装到子线程，线程结束后刷新共享变量。`ThreadTests` 15 项全部通过；见 long 数值报告（`2026-11-13-11-10-IL标准库long数值归一化.md`）、锁变量报告（`2026-11-13-11-20-IL锁变量与Spawn全局上下文.md`） 与 回写报告（`2026-11-13-11-30-IL线程全局集合回写.md`）。

- [x] **排查直接返回自身的 LangValue 类型**：源码检索发现线程、VM 线程及三种 VM 生成器值的 `GetValue()` 直接返回自身，均已重写 `ToString()`；三种生成器的字符串转换回归通过。见 测试报告（`2026-11-11-13-35-自引用值排查测试.md`）。

- [ ] **IL / VM 标准库 `import` 差异**：VM 的 `Math` 模块名、模块别名、选择性导入和符号别名调用已通过，包括字节码序列化。IL 的 `Math` 与 `Regex` 模块及其别名导入现在支持在顶层和函数体中调用方法；其他标准库、命名参数、模块属性和类导出仍需验证。见 VM 测试报告（`2026-11-11-16-37-VM标准库字节码回归.md`） 与 IL 回归报告（`2026-11-11-16-39-IL标准库导入初测.md`）。

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

- [ ] **IL LINQ 其他查询路径待复核**：基础 `let`/`where`/`select` 及末尾 `orderby` 的历史崩溃用例已通过；排序后继续查询、分组、连接及查询延续仍使用旧路径，需逐一核实。见 排序测试报告（`2026-11-11-10-37-IL-LINQ排序测试.md`）。
  2026-11-15：`join ... where` / `orderby ... where` 的无效 IL 已修（见上一条）。剩余是**语义**差异：
  join 结果只保留外层元素，后续子句引用内层范围变量时取到的是 join 循环结束后的最后一个值
  （`join ... where y > 2 select x` 在 IL 为 `[2, 3, 4]`、解释器为 `[3, 4]`）；
  `join ... into g` 的分组结果也与解释器不一致（`[[3],[3],[3]]` vs `[[1, 1], [2, 2, 2], [3]]`）。
  需要让 join 生成「配对行」并让后续子句按当前配对求值。见 change
  （`2026-11-15-IL-LINQ-where无效IL/test.md` §5）。

- [x] **IL `select` 接收复杂对象导致测试主机崩溃**：修正通道接收结果的值类型属性调用，成功接收、复杂对象接收和空通道默认分支定向测试通过。见 测试报告（`2026-11-11-08-49-IL-select回归测试.md`）。

- [ ] **解释器并发任务克隆作用域时可能抖动**：`TaskAdvancedTests.Run_MultipleAsyncTasksExceptionHandling` 在默认档一次失败，`VariateManager.CloneInternal` 枚举字典时报告 `Collection was modified`；单独复跑通过。历史记录亦有并行抖动，需复核同步与测试分档。见 `Reports/2026-11-14-12-23-字典字面量迁移测试.md`。

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

- [ ] **泛型扩展方法与约束**：核实泛型目标类型参数绑定和 `where`/`new()`/`class`/`struct` 约束在注册、方法体及运行时检查中的支持，参考 `Old8Lang/LangParser/Parsers/ExtensionParser.cs` 与相关测试。
- [ ] **VM 原生函数命名参数**：评估如何获取原生函数参数信息（`VirtualMachine.Instructions.ControlFlow.cs` 中的 TODO）。

- [ ] **其余实例方法的 IL 操作数包装审计**：`Dictionary` / `List` / `Array` / `Char` 等
  命名空间下仍有约 100 个 `GenerateIlInternal` 使用裸 `CreateCompiler`，其中形参是语言值、
  实参是原生值的同样会生成无效 IL（字符串命名空间已于 2026-11-15 修完）。
  需要按目录逐个核对 helper 签名，或在常见方法矩阵上先扫描崩溃点。

## 测试与工程问题

- [ ] **`ExceptionCreationTests.RuntimeError_Message_ContainsErrorCodeAndPosition` 的列号断言过时**：2026-11-13 Core 诊断与预处理器迁移回归中，`SourcePosition(1, 10)` 的面向用户展示列为 1 起始，而测试仍要求异常文本包含 `10`，导致 53 项中唯一失败。与程序集迁移无关；应统一测试期望与 `SourcePosition.ToString()` 的列号约定。见 Core 诊断与预处理报告（`2026-11-13-16-35-Core诊断与预处理拆分测试.md`）。

- [ ] **IL 数组回归失败**：2026-11-13 执行入口迁移定向测试中，`ArrayConcatenation_CompilesAndExecutesCorrectly` 因 `Len` 推断为 `Int32` 失败，`NestedArrays_CompilesAndExecutesCorrectly` 生成无效 IL。旧块入口与新入口均调用同一 `CompilerVisitor`；需独立基线复现。见 执行入口测试报告（`2026-11-13-22-17-执行入口迁移测试.md`）。

- [ ] **`Compiler/Collections` 与表达式组既有 IL 失败（2026-11-14 基线核对）**：以 `HEAD` 工作树对比，
  `Compiler/Collections` 9 项、`Compiler.Expressions|Linq|Modules` 41 项、`Functions.HigherOrderTests` 8 项
  在集合桥接改动前后失败集合完全一致。~~`Compiler.Classes` 组会使测试主机因生成无效 IL 崩溃~~
  ——该崩溃已于 2026-11-14 定位为比较运算装箱顺序错误并修复（见上文“IL 比较运算装箱顺序错误”），
  现在 `Compiler.Classes`（110 项）与 `Compiler.Expressions`（232 项）均可完整运行。
  `dict()` 空字典的 `Dictionary_EmptyDictionary_CompilesAndExecutes` 已在
  2026-11-14 修复（索引读写没有按 `DictionaryLangValue` 语言值处理，写路径残留栈值、
  读路径用原生键调用语言方法）；`RangeEdgeCases`、`MatchExpression_*`、`SyncIterator_*`、
  `NestedArrays` 已在 2026-11-14 修复（`Len` 的 object 分支先压栈再 `ldc.i4.0` 返回，
  栈上残留参数值）；`ArrayConcatenation` 则是「数组/列表 `+` 只有虚拟机支持」的跨模式差异，
  见下面单独条目。`MatchExpression_*` 与 `MatchTests` 的「结果类型 / 装箱顺序」问题也已在
  2026-11-14 修复（`MatchTests` 17/17 通过）；剩下的 `MatchExpressionEnhancedTests`
  属模式种类未实现的功能缺口，见下面单独条目。见 change（`2026-11-14-IL语言字典索引/test.md`）、
  change（`2026-11-14-IL-Len动态长度/test.md`）与 change（`2026-11-14-IL-match结果/test.md`）。

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

- [ ] **集合注解的元素类型不匹配仍未在编译期报错**：
  `items: list<int> <- {1, "hello", 3}` 在 `-f` 与 `-il` 下都能通过（返回第一个元素），
  只有 `-vm` 报 `list<int>` 类型不匹配；`GenericCollectionTypesCompilerTests` 有 6 项
  `*TypeMismatch_ThrowsCompilerException` 依赖这个编译期校验。
  需要先定「集合注解是否严格校验元素类型」，再统一三模式与用例。
  见 change（`2026-11-15-集合类型注解校验/test.md` §5）。

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
  见 change（`2026-11-15-IL闭包捕获形参/test.md`）。遗留：嵌套闭包返回函数值仍为 null。

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

- [ ] **`Task.Completed` / `Task.WaitAll` / `Task.WaitAny` 三种模式都没有实现**：
  `TaskClassLangValue` 的静态方法表里没有这三个方法，解释器会报 `ATTRIBUTE_ERROR`；
  IL 现在给出编译期 `INVALID_OPERATION_ERROR`（此前是无效 IL 或「偶然通过」）。
  `TaskAPITests.TaskCompleted_/TaskWaitAll_/TaskWaitAny_CompilesAndExecutesCorrectly`
  依赖这些不存在的 API，需要先定是否补 `Task` 静态 API，再实现或改用例。
  见 change（`2026-11-15-IL-Task静态方法实参/test.md` §5）。

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

- [ ] **IL 异步剩余失败**：修完上述无效 IL 与 finally 语义后，`Compiler.Async` 仍有 54 项失败
  （按首条错误分类）：24 项经 `Assert.Null` 暴露的 `RUNTIME_ERROR`（内部含 NRE、全局变量提升、
  API 缺口等）、10 项 `CompilerException`、9 项 `Variable '...' not found in async state machine
  fields`（`Spawn`/全局变量提升与异步状态机的组合）、4 项用例自身旧语法、3 项未实现的
  `Task` 静态 API、2 项暂不支持的 await 位置、2 项其它。属异步结果错误，不是无效 IL。

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

- [ ] **解释器无法在类内访问 `private static` 字段**：

  ```old8
  class M {
      private static counter <- 0
      public static func inc() -> int {
          M.counter <- M.counter + 1
          return M.counter
      }
  }
  PrintLine(M.inc())
  ```

  `-f` 报 `NAME_ERROR: 名称 'counter' 未定义`，`-il` / `-vm` 输出 `1`；
  改成 `public static` 后三种模式一致。属解释器侧静态成员可见性缺口。
  见 change（`2026-11-14-IL静态字段初值/test.md` §5.1）。

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

- [ ] **`TernaryTests` 两条失败是用例自身问题（已核实，不需改实现）**：
  `TernaryWithFunctionCalls` 把返回类型写成 `func f(...):int`（正确语法是 `-> int`），
  三种模式都报语法错误；`ComparisonConditionTernary` 的期望值按 `condition` 参数算
  （`expectedValue = condition ? 1 : 0`），但被断言的是 `value > 0`，`value=0, condition=true`
  时用例自己就要求 `1` 而实际只能得 `-1`。把 `:int` 改回 `-> int` 后，
  `c ? f(1,2) : f(3,4)` 在 `-f` 与 `-il` 下都正常。

：`match (1,2,3) { case (x,y,z) -> ... }` 在 `-vm` 下报
  `Match 表达式没有匹配的分支`，解释器与 IL 正常（2026-11-15 实测）。
  见 change（`2026-11-15-IL-match高级模式/test.md` §5）。

- [x] **数组 / 列表 `+` 拼接只有虚拟机支持**：`arr1 <- [1,2,3]; arr2 <- [4,5,6]; arr1 + arr2`
  在 `-f` 报 `INVALID_OPERATION_ERROR`，`-il` 被推断成数值加法，`-vm` 正常。
  2026-11-15 定为语言特性（同类型按顺序拼接，数组 + 列表仍一致报错）：解释器新增
  `ArrayLangValue.Plus` / `ListLangValue.Plus`，IL 走 `LangCollectionOperations`，
  VM 补原生 `List<object?>` 拼接。`Compiler.Collections` 6 → 5 失败
  （`ArrayConcatenation` 转通过），新增 7 项跨模式回归。
  见 change（`2026-11-15-集合拼接统一/test.md`）。

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

- [ ] **`Compiler.Basic.ErrorHandlingTests` 的 IL 异常预期已失配**：2026-11-13 在 Core 基础异常契约迁移的定向回归中，因筛选条件同时匹配该类，70 项中 10 项失败、2 项跳过。失败包括期望 `Old8Exception` 但实际抛出 `CompilerException`、期望运行期异常但未抛出，以及生成无效 IL。此次改动仅调整 `CompilerException` 与 `SourceContextWindow` 的程序集归属，未改异常继承或 IL 生成逻辑；需在独立基线中复现并按当前 IL 语义更新实现或断言。见 Core 基础异常契约报告（`2026-11-13-16-20-Core基础异常契约测试.md`）。

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

- [ ] **SerializationLib 编译器/解释器测试共用临时文件**：同时运行两组
  `SerializationLibTests` 时，编译器侧 `MsgPackSerializeToFile_ShouldSerializeToFile`
  因 `./test_msgpack_data.bin` 已被另一组删除而失败；各自单独运行均通过。
  建议文件名带模式或测试类前缀，并在清理时忽略文件不存在。

- [ ] **VM 模式 3 个用例直接失败**：
  - `TestFiles/VirtualMachine/DestructuringAssignmentTest.old8`：解析阶段报错（`StatementParser.Main.cs:444`）。
  - `TestFiles/VirtualMachine/vm_gap_6_error_position.old8`：VM 执行阶段抛异常（错误定位用例，末行没有 `error` 标记）。
  - `TestFiles/VirtualMachine/legacy/vm_operator_overload.old8`：VM 执行阶段抛异常（原 `VMTests/`）。
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
- [ ] **IL SpawnTests 使用过时调用形式及全局状态**：默认档中多项以 `spawn backgroundTask()` 报 `spawn` 未声明；当前 API 使用 `spawn(backgroundTask)` 并需 `Start()`。这些测试在编译前关闭全局 `IlVerificationEnabled`，若编译抛错则不会恢复，也未标记并发用例；需按当前 API 与测试分档重写并确保全局状态恢复。
- [ ] **IL 泛型类测试残留失败**：泛型特化类型现会生成静态字段、静态方法与静态初始化器，`Counter<T>.field/method` 可按每个特化独立读写；嵌套泛型类对象也可作为集合元素往返桥接。类级 `where` 约束与同一特化类型参数的字段比较均已通过，现有 `GenericClassTests` 11 项中 9 项通过。剩余两项仍使用旧式冒号继承与未支持的 `func(...) ->` 函数类型参数；将其迁移为 `extends`、`function` 与括号 lambda 后，另暴露两个 IL 缺口：特化泛型继承类中调用自身函数值字段会在未完成 `TypeBuilder` 上查询方法，泛型类 `Reduce` 的函数参数在运行时为 `null`。见 迁移排查报告（`2026-11-13-12-40-IL泛型类测试迁移排查.md`）、自引用参数报告（`2026-11-13-12-30-IL泛型类自引用参数.md`）、约束报告（`2026-11-13-12-20-泛型类where约束.md`）、静态成员报告（`2026-11-13-12-00-IL泛型类静态成员.md`）、桥接报告（`2026-11-13-12-10-IL泛型类对象桥接.md`） 与 既有报告（`2026-11-11-10-54-IL泛型递归特化测试.md`）。
- [ ] **IL 测试覆盖复核**：原清单列出的 54 个具体文件中已有 53 个同名文件；未找到 `StandardLibraryImportTests.cs`。按实际场景核实标准库导入、并发和端到端覆盖，不以文件名作为充分证据。
  2026-11-15 补充：并发全局函数里仍有约 40 处手写 `parameters[i].Accept(...)` + `Call`，
  若形参是 `int` 且实参可能是 `object`/`LangValueType`，同样需要拆箱；本轮只覆盖了
  `*Dispose` 及同文件的 int 参数调用点，其余未逐一核对。

## VM 性能

- [ ] **VM 值表示仍以 `object` 装箱**：`Stack<object?>` / `CallFrame.Locals` / `GlobalSlot` / `UpValueCell` 里的整数
  每次运算都要重新装箱。2026-10-24 P4 实测：`sum <- sum + 1` 型循环 1M 次仍分配 **45.78 B/次**
  （两次整数装箱；比较结果的布尔装箱已在 P4 用 `VmBoxed` 消除），`Bytecode/` 下共 417 处 `object?`。
  要再降一个数量级需要类型化值表示（`struct` 标记联合 + 类型化栈 / 类型化局部变量 / 类型化闭包单元），
  属跨模块改造；P4 报告 §4 给出分阶段方案（先栈与局部变量、保留 `object?` 边界、再逐步收紧）。

## 解释器测试覆盖待复核

以下是原清单提出的场景；本次核实 14 个所列专用文件名均不存在，但可能有其他文件覆盖部分场景，补测前先查现有测试。

### 1. 线程同步与并发原语测试 (Threading/)

**优先级**: 中高

`ThreadTests.cs` 覆盖了线程本身，但语法文档 §8.4 列出的并发原语在解释器侧没有测试。

- [ ] `ThreadSynchronizationTests.cs` - 线程同步测试
  - 多线程访问共享资源
  - 锁与临界区行为
  - 同步顺序与竞态场景

- [ ] `ConcurrentPrimitiveTests.cs` - 并发原语测试
  - Mutex（互斥锁）加锁/释放
  - Semaphore（信号量）计数与等待
  - AtomicInt（原子整数）并发自增
  - Channel（通道）收发
  - ReadWriteLock（读写锁）、CountDownLatch（倒计时锁）、CyclicBarrier（循环栅栏）

**AST 节点支持**: `LockedVariableLangValue`, `ThreadStaticMethodWrapper`

### 2. .NET 托管方法绑定测试 (Integration/)

**优先级**: 中高

绑定 C# 方法原本写作 `native func ... from ...`，解析器现已统一为 `extern`，`native` 关键字不再存在
（见 `LangParser/Parsers/StatementParser.ImportAndNative.cs`，其中 `Expect(LangTokenType.Extern)`）。
`Modules/Extern/` 下已覆盖 C/C++ P/Invoke（`NativeDllExternTests.cs`）、Python（`PythonExternTests.cs`）、
JavaScript（`JavaScriptExternTests.cs`），但语法文档 §5.15 的 .NET 托管程序集形式（`"C#:"` / `"cs:"` /
`"csharp:"` / `"dotnetdll:"` 前缀）在解释器侧仍无测试。

- [ ] `ManagedDllExternTests.cs` - .NET 托管 DLL 导入测试
  - 方法块导入：`extern "C#:System" Math { func Pow(x:double, y:double) -> double, ... }`
  - 单个方法导入：`extern "C#:System" Math func Pow(x:double, y:double) -> double`
  - 带别名的导入：`func Pow(x:double, y:double) -> double as Power`
  - 导入自定义 DLL：`extern "dotnetdll:MyLibrary.dll" MyMathClass { ... }`（需准备测试程序集）
  - 命名参数调用：`Pow(y: 3.0, x: 2.0)`
  - 程序集不存在时的异常处理（实测报 `IMPORT_ERROR`）
  - `cs:` / `csharp:` 前缀与 `C#:` 的等价性

> 已实测（2026-10-09，解释模式）：上述 `{ func ... }` 块、单个 `func`、`as` 别名、命名参数四种写法均可用；
> 而 `extern "C#:System" Math *` 和 `extern "C#:System" Math as SysMath` 会报
> `语法错误：缺少 'func' 关键字`——这两种写法属于 `NativeStatement` 解析路径，带 `C#:` 等托管前缀时走的是
> `ParseExternStatement`，因此写用例时不要照搬 §5.14.2/§5.14.4 的 `*`、`as` 示例（语法文档 §5.14 的复核说明
> 也已记录同类示例问题）。

**AST 节点支持**: `NativeStatement`, `NativeAnyLangValue`, `NativeStaticAny`

**示例测试场景**:
```old8
extern "C#:System" Math {
    func Pow(x:double, y:double) -> double,
    func Sqrt(x:double) -> double
}
result <- Pow(2.0, 10.0)
```

### 3. 模块命名空间与重载测试 (Modules/)

**优先级**: 中

`Modules/AdvancedImport/` 已覆盖条件导入、动态导入、惰性导入，但命名空间隔离与模块重载仍无测试。

- [ ] `ModuleNamespaceTests.cs` - 模块命名空间测试
  - 模块命名空间隔离
  - 跨模块符号访问
  - 模块符号冲突处理

- [ ] `ModuleReloadTests.cs` - 模块重载测试
  - 模块热重载
  - 模块缓存管理
  - 模块依赖更新

**AST 节点支持**: `UnifiedModule`, `ImportInfo`, `LazySymbolProxy`

### 4. LINQ 查询语法 group by / into 延续测试 (Linq/)

**优先级**: 中低

`ListAdvancedMethodsTests.cs` 覆盖的是 `List.GroupBy` 方法形式，`LinqJoinTests.cs` 中带 `into g` 的用例只有一条；
查询语法 `group x by ...` 在解释器侧尚无执行用例（解析器侧有 `Parser/Linq/LinqAdvancedParsingTests.cs`）。

- [ ] `LinqGroupByTests.cs` - 查询语法 group by 测试
  - `group x by <key>` 基础用法
  - 分组后聚合（Count / Sum / Max）
  - 多键分组
  - 分组后过滤

- [ ] `LinqQueryContinuationTests.cs` - 查询延续测试
  - `into` 关键字用法
  - 查询延续链式调用

**AST 节点支持**: `GroupByClause`, `QueryContinuation`

### 5. Super 表达式测试 (Classes/)

**优先级**: 中低

`super` 目前只在 `MemberAccessTests.cs`、`MixinTests.cs` 中顺带出现，没有专门用例。

- [ ] `SuperExpressionTests.cs` - super 关键字测试
  - super 调用父类方法
  - super 访问父类字段
  - super 在构造函数中的使用
  - super 链式调用

**AST 节点支持**: `SuperExpression`, `SuperProxy`

### 6. 错误处理增强测试 (EdgeCases/)

**优先级**: 中

`EdgeCases/TypeErrorsTests.cs` 已覆盖常见的类型不匹配运算，以下场景仍缺测试。

- [ ] `ParserErrorTests.cs` - 解析器错误测试
  - 语法错误恢复
  - 不完整语句处理
  - 错误提示信息准确性

- [ ] `RuntimeTypeErrorTests.cs` - 运行时类型错误测试
  - 空引用错误
  - 越界访问错误

- [ ] `CircularReferenceTests.cs` - 循环引用测试
  - 数据结构循环引用
  - 函数递归深度
  - 类实例循环引用

> 注：`Modules/ErrorHandling/CircularDependencyTests.cs` 测的是模块循环依赖，与此处的数据结构循环引用不是同一类场景。

### 7. 测试工具类测试 (Testing/)

**优先级**: 中低

AST 中有 `MockLibClassLangValue`, `TestRunnerClassLangValue`, `AssertClassLangValue` 等节点，解释器侧无测试。

- [ ] `TestUtilitiesTests.cs` - 测试工具类测试
  - Assert 类用法
  - TestRunner 类用法
  - MockLib 类用法
  - 测试辅助功能

**AST 节点支持**: `AssertClassLangValue`, `TestRunnerClassLangValue`, `MockLibClassLangValue`

### 8. 类型模板测试 (Types/)

**优先级**: 中

AST 中有 `TypeTemplate` 节点，用于类型参数和泛型。

- [ ] `TypeTemplateTests.cs` - 类型模板测试
  - 泛型类型参数
  - 类型约束
  - 类型参数推断
  - 嵌套泛型类型

**AST 节点支持**: `TypeTemplate`, `GenericParameter`, `GenericInstanceExpression`

### 9. 常量优化测试 (Expressions/)

**优先级**: 低

AST 中有 `ConstantLangValue` 节点，用于常量折叠。

- [ ] `ConstantFoldingTests.cs` - 常量折叠测试
  - 编译时常量计算
  - 常量表达式优化
  - 常量传播

**AST 节点支持**: `ConstantLangValue`

### 10. 枚举增强测试 (Statements/)

**优先级**: 低

`EnumTests.cs` 已覆盖声明、取值、比较、switch，但以下场景仍缺：

- [ ] 在现有 `EnumTests.cs` 中补充：
  - 枚举标志位组合
  - 枚举方法和属性

**AST 节点支持**: `EnumInit`, `EnumTemplate`

## Language Server 与 VS Code 扩展

### P0：可靠性与已有能力完善

- [ ] 为所有已注册 Handler 补齐稳定的单元测试和协议级测试（新增的 CodeLens / SelectionRange /
      InlayHint / WorkspaceIndex 已有单元测试，其余 Handler 仍需体系化回归）。
- [ ] 为符号表引入作用域链，正确处理局部变量、变量遮蔽和同名符号。
- [ ] 完善成员访问的类型推断，覆盖继承成员和静态成员补全。
- [ ] 改善诊断：类型不匹配、未使用变量、成员不存在，以及跨文件语义分析。
- [ ] 评估并逐步替换 token-based location workaround；Parser 修复后保留回归测试再移除。

### P1：尚未实现的 LSP 能力

- [ ] 实现 Inline Values，并与可用的调试运行时状态建立协议映射（依赖 DAP 或变量快照）。
- [ ] 实现 Call Hierarchy / Type Hierarchy（OmniSharp 0.19.9 提供协议 API）。

实现新 Handler 时需要同时完成：协议注册、能力声明（如需要）、单元测试、集成测试和文档更新。

### P2：性能与工程质量

- [ ] 为 token 按行建立索引，降低位置查找的线性扫描成本。
- [ ] 为实时诊断增加防抖和后台执行，避免大文件编辑卡顿。
- [ ] 增加 Language Client ↔ Language Server 的端到端测试。
- [ ] 增加大文件和实时诊断延迟的性能基线。

### 尚未完成

- [ ] 实现 Debug Adapter；当前扩展没有 `debugAdapter.ts`、调试贡献点或 DAP 配置。
      运行时目前只有 VM 的交互式终端调试器（`-vm --debug-session`）和 LSP 自定义调试请求，
      需要先确定 DAP 与运行时的对接方式（建议新增一个 `--dap` 模式），再实现适配器。
- [ ] 语法高亮的字符串插值、文档注释和嵌套结构可作为独立的 grammar 改进项。

- [ ] **私有成员补全**：按访问位置决定是否展示私有成员（`CompletionHandler.cs` 中的 TODO）。
- [ ] **补全测试**：核查泛型、表达式、Extern、预处理指令、联合/交叉类型及成员链、类型推断和文档注释的覆盖；评估并发、性能和模糊测试。参考 `Old8Lang.Tests/LanguageServer/Docs/` 中的测试计划。

## 长期研究与调试方向

- [ ] 评估类型推断的调用处推断、泛型及联合类型、多分支控制流和类型缩窄；先核实当前实现。[高级主题](../Docs/ADVANCED_TOPICS.md#限制和已知问题)
- [ ] 评估图形调试界面、增强条件表达式、IL 和远程调试、内存监控。[开发者工具](../Docs/DEVELOPER_TOOLS.md)

## 本次核对中移出的过期条目

- 线程样例缺少 `Start()`：所列五个文件现已包含启动调用。
- DatabaseLib 跨模式测试同名内存库：解释器使用 `itestdb_`、IL 使用 `testdb_` 前缀。本次相关筛选测试通过。
- 无类型注解递归栈溢出：原文后续明确记为已修，相关测试仍应在后续全量验证中覆盖。
- 原 IL 清单中的已有同名测试文件不再逐一列为“缺失”；其余覆盖仍需按场景审查。
- 原 VM 性能中的全局槽位、布尔装箱和基准噪声门禁已有完成记录；性能报告与原始复现的现存部分在 `archive/Reports/`。

本次测试结果见 待办核对测试报告（`2026-11-11-08-30-待办核对测试.md`）。

## 2026-11-15 复核更新

`Docs/MODE_SUPPORT.md` 已按 2026-11-15（IL 缺陷修复轮次）的逐条实测刷新：
矩阵里修正了 `in` 运算符（2026-11-15 起虚拟机 ✅）、数组/列表 `+` 拼接（2026-11-15 起三模式 ✅）、集合 `.Length` 属性
（解释器 ❌）、match 高级模式（IL ⚠️）、`yield`（IL ❌）、Mixin（IL ❌ / 虚拟机 ⚠️）、
带默认实现的接口（IL ⚠️）、反射 API（IL ✅）等单元格；IL 已知限制里过时的
「lambda 不能读写外层局部变量 / 不能在调用结果上链式调用」与旧失败数已更新；
并新增「三种模式共同的跨模式差异」小节，把 10 条尚未统一的语义集中列出。
