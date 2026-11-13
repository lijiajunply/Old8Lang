# Old8Lang 统一待办清单

核对日期：2026-11-11。此文档合并原根目录、解释器测试、IL 测试和语言服务器待办；历史测试报告保留在 `Reports/`。

**状态约定**：下列历史缺陷除非注明本次验证，否则均为「待复核」，不是本次已复现；测试文件存在也不能证明对应场景覆盖完整。复现、修复后请更新本清单并按测试指南生成报告。

## 优先顺序

1. 排查会使测试进程崩溃的 IL 问题，再核实其他 IL 结果错误和跨模式差异。
2. 修复测试隔离与不稳定问题，补齐关键场景覆盖。
3. 完善 LSP 作用域、诊断及调试集成；按收益推进 VM 性能和长期能力。

## IL 模式与跨模式缺陷

以下条目来自历史复现，尚未逐项重跑。报告见 [IL 缺陷验证](Reports/2026-10-23-23-30-IL模式缺陷修复验证.md)、[跨模式语义修复](Reports/2026-10-24-00-30-P1跨模式语义修复测试.md) 和 [VM 残留修复](Reports/2026-10-24-09-45-P2虚拟机模式残留修复测试.md)。

- [ ] **泛型目标类型的扩展方法 IL 支持**：`extension list<T>` 现会保留目标泛型参数及
  `where` 约束；IL 桥接在接收者、参数和嵌套集合之间保留引用关系并同步原地修改，`return this`
  可继续链式调用。覆盖嵌套集合、返回接收者、参数别名和 `struct` 约束；见
  [测试报告](Reports/2026-11-11-14-50-IL泛型扩展完整语义测试.md)。空集合类型推断、多类型参数的交叉约束、字典与数组的别名及修改行为仍需验证。
- [x] **IL 扩展方法与顶层回写的自引用数组栈溢出**：桥接及类型推断避免展开循环集合，顶层回写复用已转换对象；自引用数组、列表、字典及别名的定向回归通过。见 [测试报告](Reports/2026-11-11-16-27-泛型扩展与循环引用回归.md)。

- [ ] **方法调用结果上直接链式调用生成无效 IL。**
  - 2026-11-11 复核：`Count().ToStr()` 已可用；`FindAll(...).Count()` 的注册方法返回类型推断已修复，验证了三层链式调用和接收者只求值一次。
  - 2026-11-11：`MethodChainingRegressionTests` 与 `NestedAccessTests` 共 23 项通过，覆盖多层数组、字典、列表的读取、修改与链式接收者只求值一次。类成员、函数返回值及其他链式调用仍需复核；见 [原测试报告](Reports/2026-11-11-12-30-IL链式方法调用测试.md) 与 [嵌套集合复测](Reports/2026-11-11-22-10-IL链式与嵌套集合.md)。

- [ ] **IL 模式 lambda 的闭包函数值支持。**
  - 2026-11-11：直接赋值的 lambda 现可读取及写回创建点可见的普通局部变量；闭包环境在运行时绑定，每个 lambda 实例独立。`LambdaTests` 24 项通过，见 [测试报告](Reports/2026-11-11-19-00-IL-lambda-闭包局部变量.md)。
  - 具名函数返回 `function` 的简单闭包链路现已可用，多个返回闭包实例彼此隔离；见 [测试报告](Reports/2026-11-11-20-27-IL-返回闭包函数值.md)。2026-11-11 进一步确认：字典索引后的嵌套集合 `Length` 可在 `Filter` lambda 内使用；lambda 内继续创建并传给 `Reduce` 的嵌套 lambda 已避免捕获未初始化局部变量，嵌套数组以动态类型调用 `Reduce` 也可桥接执行。见 [隔离报告](Reports/2026-11-11-23-40-IL嵌套Lambda隔离.md) 与 [嵌套集合回归](Reports/2026-11-12-00-10-IL嵌套集合Reduce.md)。2026-11-13：集合索引得到的 `object` 函数值现可在运行期作为 .NET 委托调用；循环内保存的 lambda 分别捕获各轮局部快照，带参数调用得到 `10/11/12`；捕获 `this` 的 lambda 可读写其类实例私有字段。嵌套返回闭包、跨具名函数的顶层捕获和函数类型参数仍待处理，见 [集合函数值测试](Reports/2026-11-13-10-10-IL集合函数值调用.md) 与 [this 捕获测试](Reports/2026-11-13-10-20-IL闭包捕获this.md)。

- [ ] `List` / `Dictionary` / `Array` 其余实例方法的 IL 实现仍是 `Ldnull` 桩或按旧约定写死
  （例如 `Map` / `Filter` / `Reduce` / `Any` / `All` / `Find` / `Sort` / `IsEmpty` 等），
  在 `List<T>` 接收者下会「方法未找到」。
  - 2026-11-11：`Map`、`Filter`、`Find`、`Contains`、`Distinct`、`Sum`、`Take`、`Skip`、`Any/Some`、`All/Every`、`IsEmpty`、`Reduce`、`Sort`、`Min`、`Max` 与 `Average` 已在原生 `List<T>` 接收者上支持；`Map` / `Filter` / `Sort` / `Distinct` / `Take` / `Skip` 及数值聚合也覆盖数组表示。字典的 `Keys` / `Values` 已支持原生 `Dictionary<K,V>` 接收者并返回可继续调用集合方法的列表；显式 `dict` / `dictionary` 注解可将空花括号编译为字典。`Length` 属性现也可用于原生与经字典索引得到的集合。见 [Map/Filter 测试报告](Reports/2026-11-11-20-40-IL集合MapFilter.md)、[Find/Any/All 测试报告](Reports/2026-11-11-20-45-IL集合FindAnyAll.md)、[IsEmpty 测试报告](Reports/2026-11-11-20-50-IL集合IsEmpty.md)、[Reduce 测试报告](Reports/2026-11-11-21-00-IL集合Reduce.md)、[数组测试报告](Reports/2026-11-11-21-10-IL数组MapFilter.md)、[字典键值测试报告](Reports/2026-11-11-21-20-IL字典KeysValues.md)、[排序测试报告](Reports/2026-11-11-21-30-IL集合Sort.md)、[集合长度测试报告](Reports/2026-11-11-22-00-IL集合Length.md)、[空字典注解测试报告](Reports/2026-11-11-22-30-IL空字典注解.md)、[Contains 测试报告](Reports/2026-11-11-22-40-IL集合Contains.md)、[Distinct 测试报告](Reports/2026-11-11-23-00-IL集合Distinct.md)、[Sum 测试报告](Reports/2026-11-11-23-10-IL集合Sum.md)、[Take 测试报告](Reports/2026-11-11-23-20-IL集合Take.md)、[Skip 测试报告](Reports/2026-11-11-23-30-IL集合Skip.md) 与 [数值聚合测试报告](Reports/2026-11-12-00-30-IL集合数值聚合.md)。其余字典及集合方法仍待迁移。
  - 迁移方式：参照 `Reports/2026-10-10-19-03-实例方法跨模式补齐测试.md` §2 的机制
    （实现 `IIlNativeValueInstanceMethod` + `IlValueBridge.EmitLoadWrapped` + 静态 helper）。
  - 2026-11-12：读取型 `First`、`Last`、`ElementAt` / `At` 现支持原生列表与数组，并保留负索引语义；见 [读取型集合测试报告](Reports/2026-11-12-00-40-IL集合读取方法.md)。
  - 2026-11-12：`FirstOrDefault`、`LastOrDefault` 现支持原生列表与数组，无元素时可返回 `null` 或调用方提供的默认值；见 [默认读取测试报告](Reports/2026-11-12-00-50-IL集合默认读取.md)。
  - 2026-11-12：`IndexOf` 现支持原生列表与数组，保留首个匹配项索引及未找到时 `-1` 的语义；见 [索引查找测试报告](Reports/2026-11-12-01-00-IL集合IndexOf.md)。
  - 2026-11-12：`ToList` 与 `ToArray` 现支持原生列表、数组及转换结果的继续调用；见 [集合表示转换测试报告](Reports/2026-11-12-01-10-IL集合表示转换.md)。
  - 2026-11-12：`Union` 现支持原生列表、数组和混合接收者，保留原列表元素顺序及右侧新元素去重追加语义；见 [集合并集测试报告](Reports/2026-11-12-01-20-IL集合Union.md)。
  - 2026-11-12：`Intersect` 与 `Except` 现支持原生列表、数组和混合接收者，保持解释器的去重集合语义；见 [集合交差集测试报告](Reports/2026-11-12-01-30-IL集合交差集.md)。
  - 2026-11-12：`Count` 现支持原生列表与数组，并可继续链式转换为字符串；见 [集合 Count 测试报告](Reports/2026-11-12-01-40-IL集合Count.md)。
  - 2026-11-13：`Concat` 现支持原生列表、数组和混合接收者，按接收者在前、参数在后的顺序保留所有元素；见 [集合连接测试报告](Reports/2026-11-13-09-00-IL集合Concat.md)。
  - 2026-11-13：`ToStr` 现支持原生列表与数组，输出与解释器的集合显示格式一致；见 [集合字符串化测试报告](Reports/2026-11-13-09-10-IL集合ToStr.md)。
  - 2026-11-13：`IsSorted` 现支持原生列表与数组，保留升序（含重复值）判断语义；见 [集合有序性测试报告](Reports/2026-11-13-09-20-IL集合IsSorted.md)。
  - 2026-11-13：`ForEach` 现支持原生列表与数组，回调可写回创建点可见的局部变量；见 [集合遍历测试报告](Reports/2026-11-13-09-30-IL集合ForEach.md)。

- [ ] `Array.Sort()` / `Array.IsSorted()` 的 VM 兜底已修（本轮），但 `LangList*` 系列里仍有一批
  只认 `ILangList`、对 `object?[]` / `List<object?>` 直接抛错的 VM 实现。

- [x] **IL 排除端点后的反向范围生成错误**：IL 现在按调整后的端点确定范围方向，`[1>~<1]` 与解释器一致生成 `2, 1, 0`；`ExtendedRangeTests` 21 项通过。见 [测试报告](Reports/2026-11-11-22-50-IL扩展范围.md)。

- [ ] **异步函数的装饰器仍不可用**：不是虚拟机独有——装饰器必须返回「异步函数值」，
  而 `async (x) -> ...` 异步 Lambda 目前在解释器下也会抛
  `NullReferenceException`（`Instance.Run`）。三种模式都还没有异步函数值表示，
  需要先补「异步 Lambda / 异步函数值」这一层能力，再让 VM 的 `CallAsync` 支持装饰器绑定。

- [ ] **元组模式解构 `(a, b) <- ...` 跨模式差异**：三种模式的基本元组赋值和交换已通过；IL 目前要求右侧静态类型为 CLR 元组，解释器还支持其他 `ILangList`，VM 集合右值及嵌套模式仍需复核。见 [测试报告](Reports/2026-11-11-13-42-三模式元组解构测试.md)。

- [x] **解释器 `Thread.CurrentThread` 属性访问**：裸属性和无参调用现均返回执行中的线程；同时修正了无参调用原先包装新建线程的问题。见 [测试报告](Reports/2026-11-11-12-42-当前线程属性测试.md)。
- [x] **IL `Thread.CurrentThread` 属性与调用**：两种形式均返回实际执行线程，并支持继续访问其属性。见 [测试报告](Reports/2026-11-11-13-33-IL当前线程测试.md)。
- [ ] **IL 其他 Thread API**：仍需逐项复核。

- [x] **排查直接返回自身的 LangValue 类型**：源码检索发现线程、VM 线程及三种 VM 生成器值的 `GetValue()` 直接返回自身，均已重写 `ToString()`；三种生成器的字符串转换回归通过。见 [测试报告](Reports/2026-11-11-13-35-自引用值排查测试.md)。

- [ ] **IL / VM 标准库 `import` 差异**：VM 的 `Math` 模块名、模块别名、选择性导入和符号别名调用已通过，包括字节码序列化。IL 的 `Math` 与 `Regex` 模块及其别名导入现在支持在顶层和函数体中调用方法；其他标准库、命名参数、模块属性和类导出仍需验证。见 [VM 测试报告](Reports/2026-11-11-16-37-VM标准库字节码回归.md) 与 [IL 回归报告](Reports/2026-11-11-16-39-IL标准库导入初测.md)。

- [x] **IL 实例方法读取字段返回随机值**：
  `class M { public v:int; func field()->int { return this.v } }` + `m <- M(9)`，
  `PrintLine(m.field())` 在 `-il` 下每次输出不同的垃圾整数（`-f` 为 `9`）；顶层 `m.v` 读取正常。
  已修复有注解字段类型及方法返回值装箱/拆箱，覆盖多个实例及 `object` 返回值；见 [测试报告](Reports/2026-11-11-09-05-IL实例字段回归测试.md)。

- [x] **IL 函数无法访问顶层全局变量**：`g <- 10` + `func f() -> int { return g + 1 }` →
  `变量 'g' 未声明`。顶层变量是 `OldLangRun` 的局部变量，函数是独立 `DynamicMethod`，编译函数体时不可见。
  2026-11-11：被函数体引用的顶层变量现提升为运行期全局槽位，具名函数、lambda、类方法和字段初始化器都可读写，
  支持函数声明早于变量赋值的前向引用；同名参数、局部变量、闭包单元和字段仍然遮蔽全局变量。未被函数引用的
  顶层变量继续使用局部变量。异步函数尚未接入该模型，跨线程调用时槽位环境不可见。见
  [测试报告](Reports/2026-11-11-22-40-IL具名函数读取顶层变量.md)。

- [x] **IL 实例方法读取裸字段仍是 `NullReferenceException`**：裸字段现在在静态类型推断中返回其字段类型，
  避免返回路径对已加载的值类型重复拆箱；读取型回归通过。见
  [测试报告](Reports/2026-11-13-08-50-IL裸字段访问.md)。
- [x] **IL 集合 `Length` 参与比较会生成无效 IL/运行期 NRE**：集合 `Length` 在发射路径和
  静态类型推断路径中均统一为 `int`，顶层与函数体中的比较回归均通过。见
  [测试报告](Reports/2026-11-13-09-40-IL集合Length比较.md)。

- [x] **IL 打印布尔值大小写不一致**：`PrintLine(true)` 现在输出 `true`；见 [测试报告](Reports/2026-11-11-11-11-IL打印格式测试.md)。

- [x] **IL 直接打印原生列表输出 .NET 类型名**：`PrintLine({1, 2})` 现在输出 `[1, 2]`；`Print` / `PrintLine` 也会处理所有参数。

- [x] **泛型类递归特化导致栈溢出**：原崩溃用例及自引用特化回归已通过；完整 `Compiler.Classes` 组正常结束，但仍有普通失败。见 [测试报告](Reports/2026-11-11-10-54-IL泛型递归特化测试.md)。
- [ ] **IL LINQ 其他查询路径待复核**：基础 `let`/`where`/`select` 及末尾 `orderby` 的历史崩溃用例已通过；排序后继续查询、分组、连接及查询延续仍使用旧路径，需逐一核实。见 [排序测试报告](Reports/2026-11-11-10-37-IL-LINQ排序测试.md)。

- [x] **IL `select` 接收复杂对象导致测试主机崩溃**：修正通道接收结果的值类型属性调用，成功接收、复杂对象接收和空通道默认分支定向测试通过。见 [测试报告](Reports/2026-11-11-08-49-IL-select回归测试.md)。

历史记录还提及 `TaskAdvancedTests.Run_MultipleAsyncTasksExceptionHandling` 曾发生并行抖动，单独运行及后续复跑通过；必要时重新核查。

- [ ] **IL 模式顶层 C 风格 for 循环变量仍是全局变量**：解释器与虚拟机已把循环变量收进循环作用域
  （每轮闭包捕获 `012`、循环结束后变量不可见），IL 仍把它声明为全局变量，因此：
  `for i <- 0, i < 3, i++ {}` 之后 `PrintLine(i)` 输出 `3`（其余两模式报 `NAME_ERROR`）；
  同名嵌套循环共用同一个 `i`，`loop_shadow.old8` 里 `total` 得 `2`（应为 `6`）。
  这项工作依赖 IL 的全局变量 / 作用域模型（同本节的“IL 函数无法访问顶层全局变量”），一并处理更合适。
  2026-11-11：全局槽位模型已就位；若循环变量与「被函数引用的顶层变量」同名，IL 已在循环内建立
  局部遮蔽并恢复槽位，函数读到的仍是循环前的顶层值。普通循环变量「循环结束后仍可见」的问题不变。
  2026-11-13：循环初始化现在始终建立独立局部绑定，并在退出时恢复外层绑定；同名嵌套循环得到正确的
  `total = 6`，未声明的循环变量在结束后会报未声明错误。保存的 lambda 也可从集合索引调用，并分别捕获
  每轮值；见 [测试报告](Reports/2026-11-13-09-50-IL循环变量作用域.md) 与
  [集合函数值回归](Reports/2026-11-13-10-10-IL集合函数值调用.md)。

- [x] **IL 运行期错误的异常值无法 `.ToStr()`**：现已支持直接转换捕获的索引错误和抛出消息，见 [测试报告](Reports/2026-11-11-12-35-IL异常字符串测试.md)。

- [x] **解释器负索引越界的错误信息用了归一化后的下标**：`arr <- [1,2,3]; arr[-5]` 原先报
  `索引越界: 索引 -2 超出范围 [0, 2]`（应报原始的 `-5`）；IL 侧 `IlIndexHelper` 报的是原始下标。
  涉及 `ListLangValue.Get` / `ArrayLangValue.Get` / `StringLangValue.Get` / `TupleLangValue.Get`。
  已修正读取及列表、元组赋值的越界信息，并增加回归测试。

- [ ] **泛型扩展方法与约束**：核实泛型目标类型参数绑定和 `where`/`new()`/`class`/`struct` 约束在注册、方法体及运行时检查中的支持，参考 `Old8Lang/LangParser/Parsers/ExtensionParser.cs` 与相关测试。
- [ ] **VM 原生函数命名参数**：评估如何获取原生函数参数信息（`VirtualMachine.Instructions.ControlFlow.cs` 中的 TODO）。

## 测试与工程问题

- [x] **IL 字符串 `Length()` 可能导致进程崩溃**：原生字符串现在使用 CLR 字符串长度，返回原生整数；原崩溃用例及别名、链式调用回归通过，见 [测试报告](Reports/2026-11-11-12-37-IL字符串长度测试.md)。
- [x] **IL 嵌套异常测试的包含断言**：调整为 `Assert.Contains(text, substring)` 的实际参数顺序，异常与字符串相关回归 13 项通过；见 [测试报告](Reports/2026-11-11-13-51-IL异常处理回归测试.md)。

- [x] **IL 类测试中的继承语法失败**：`InheritanceTests` 已由已废弃的 `class Derived : Base` 迁移为
  `class Derived extends Base`；同时 `super.method()` / `super.init()` 改为直接调用父类实现，避免虚分派回到
  子类重写方法而栈溢出。现已支持受保护字段、静态字段/方法继承，以及子类中按签名调用父类初始化器和方法；
  `InheritanceTests` 全组 10 项通过，见 [前序测试报告](Reports/2026-11-13-10-00-IL继承语法与super调用.md) 与
  [成员访问测试报告](Reports/2026-11-13-10-30-IL继承成员访问.md)。
- [x] **`SelectStatementTests` 整组测试完成性**：带 10 秒挂起超时复跑 21 项全部通过；先前运行卡住的原因未复现。见 [测试报告](Reports/2026-11-11-13-55-IL-select整组复测.md)。
- [ ] **IL 类实例化测试残留问题**：2026-11-11 复测 14 项中 9 项通过、5 项失败，涉及集合元素与嵌套对象的链式调用、静态成员及集合方法；方法重载测试取值顺序修正后已通过。字段初始值与初始化器参数为不同数值类型时现会显式转换，构造器链回归已通过；见 [构造器数值转换报告](Reports/2026-11-13-10-40-IL构造器数值转换.md) 与 [既有测试报告](Reports/2026-11-11-16-24-IL类实例化扩展回归.md)。

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

- [ ] **全量测试完成性**：2026-11-11 默认档在 30 秒无进展后中止，已完成 4252 通过、409 失败、28 跳过；当时活动测试包含 Python 外部模块用例，该筛选隔离运行 2 项通过，挂起根因未确定。失败集中于 IL 异步、函数、类、表达式和集合等，需逐组复现并核对既有基线。见 [默认档报告](Reports/2026-11-11-13-57-默认档超时测试.md)及 [隔离报告](Reports/2026-11-11-14-00-Python外部模块隔离测试.md)。
- [ ] **IL SpawnTests 使用过时调用形式及全局状态**：默认档中多项以 `spawn backgroundTask()` 报 `spawn` 未声明；当前 API 使用 `spawn(backgroundTask)` 并需 `Start()`。这些测试在编译前关闭全局 `IlVerificationEnabled`，若编译抛错则不会恢复，也未标记并发用例；需按当前 API 与测试分档重写并确保全局状态恢复。
- [ ] **IL 泛型类测试残留失败**：2026-11-11 `GenericClassTests` 11 项中 6 项失败，涉及泛型约束与继承解析、函数类型参数、嵌套集合元素及静态方法；详见 [测试报告](Reports/2026-11-11-10-54-IL泛型递归特化测试.md)。
- [ ] **IL 测试覆盖复核**：原清单列出的 54 个具体文件中已有 53 个同名文件；未找到 `StandardLibraryImportTests.cs`。按实际场景核实标准库导入、并发和端到端覆盖，不以文件名作为充分证据。

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

- [ ] 评估类型推断的调用处推断、泛型及联合类型、多分支控制流和类型缩窄；先核实当前实现。[高级主题](Docs/ADVANCED_TOPICS.md#限制和已知问题)
- [ ] 评估图形调试界面、增强条件表达式、IL 和远程调试、内存监控。[开发者工具](Docs/DEVELOPER_TOOLS.md)

## 本次核对中移出的过期条目

- 线程样例缺少 `Start()`：所列五个文件现已包含启动调用。
- DatabaseLib 跨模式测试同名内存库：解释器使用 `itestdb_`、IL 使用 `testdb_` 前缀。本次相关筛选测试通过。
- 无类型注解递归栈溢出：原文后续明确记为已修，相关测试仍应在后续全量验证中覆盖。
- 原 IL 清单中的已有同名测试文件不再逐一列为“缺失”；其余覆盖仍需按场景审查。
- 原 VM 性能中的全局槽位、布尔装箱和基准噪声门禁已有完成记录；性能报告与原始复现保留在 `Reports/`。

本次测试结果见 [待办核对测试报告](Reports/2026-11-11-08-30-待办核对测试.md)。
