# Old8Lang 统一待办清单

核对日期：2026-11-11。此文档合并原根目录、解释器测试、IL 测试和语言服务器待办；历史测试报告保留在 `Reports/`。

**状态约定**：下列历史缺陷除非注明本次验证，否则均为「待复核」，不是本次已复现；测试文件存在也不能证明对应场景覆盖完整。复现、修复后请更新本清单并按测试指南生成报告。

## 优先顺序

1. 排查会使测试进程崩溃的 IL 问题，再核实其他 IL 结果错误和跨模式差异。
2. 修复测试隔离与不稳定问题，补齐关键场景覆盖。
3. 完善 LSP 作用域、诊断及调试集成；按收益推进 VM 性能和长期能力。

## IL 模式与跨模式缺陷

以下条目来自历史复现，尚未逐项重跑。报告见 [IL 缺陷验证](Reports/2026-10-23-23-30-IL模式缺陷修复验证.md)、[跨模式语义修复](Reports/2026-10-24-00-30-P1跨模式语义修复测试.md) 和 [VM 残留修复](Reports/2026-10-24-09-45-P2虚拟机模式残留修复测试.md)。

- [ ] **泛型目标类型的扩展方法调用未接入 IL 方法解析。**
  - 复现：`extension list<T> { func marker() -> int { return 7 } }` 后调用
    `items.marker()`，IL 模式报无法在 `List\`1` 中找到方法 `marker`；解释器模式可正常调用。
  - 本轮仅修复声明语法解析，未改变 IL 扩展方法注册与调用路径。

- [ ] **方法调用结果上直接链式调用生成无效 IL。**
  - 2026-11-11 复核：`Count().ToStr()` 已可用；`FindAll(...).Count()` 的注册方法返回类型推断已修复，验证了三层链式调用和接收者只求值一次。
  - 集合元素、嵌套对象及其他链式调用仍需复核；见 [测试报告](Reports/2026-11-11-12-30-IL链式方法调用测试.md)。

- [ ] **IL 模式 lambda 不能读写外层普通局部变量。**
  - 现状：`BuildLambdaDynamicMethod` 的 `funcLocal` 只带了外层**函数**委托表（本轮新增），
    普通局部变量仍不可见。既有边界。

- [ ] `List` / `Dictionary` / `Array` 其余实例方法的 IL 实现仍是 `Ldnull` 桩或按旧约定写死
  （例如 `Map` / `Filter` / `Reduce` / `Any` / `All` / `Find` / `Sort` / `IsEmpty` 等），
  在 `List<T>` 接收者下会「方法未找到」。
  - 迁移方式：参照 `Reports/2026-10-10-19-03-实例方法跨模式补齐测试.md` §2 的机制
    （实现 `IIlNativeValueInstanceMethod` + `IlValueBridge.EmitLoadWrapped` + 静态 helper）。

- [ ] `Array.Sort()` / `Array.IsSorted()` 的 VM 兜底已修（本轮），但 `LangList*` 系列里仍有一批
  只认 `ILangList`、对 `object?[]` / `List<object?>` 直接抛错的 VM 实现。

- [ ] **异步函数的装饰器仍不可用**：不是虚拟机独有——装饰器必须返回「异步函数值」，
  而 `async (x) -> ...` 异步 Lambda 目前在解释器下也会抛
  `NullReferenceException`（`Instance.Run`）。三种模式都还没有异步函数值表示，
  需要先补「异步 Lambda / 异步函数值」这一层能力，再让 VM 的 `CallAsync` 支持装饰器绑定。

- [ ] **元组模式解构 `(a, b) <- ...` 在解释器下无效**：语法能解析，但 `SetStatement.Run`
  没有对应分支，变量不会被赋值（`-f` 报 `NAME_ERROR`）；IL 生成也不正确。虚拟机的字节码路径
  反而支持。属于跨模式语义分歧，需统一（建议以数组解构为准，或两者都支持）。

- [ ] **`Thread.CurrentThread` 的裸属性写法只在虚拟机可用**：解释器下
  `Thread.CurrentThread`（不带括号）报 `类型 'Thread' 没有属性 'CurrentThread'`，
  `Thread.CurrentThread()` 正常；属于解释器的静态类属性通道缺失，非本轮范围。

- [ ] **排查返回自身的 LangValue 类型**：`ThreadLangValue.GetValue()`
  返回 `this`，而基类 `ToString()` 走 `GetValue()?.ToString()`，原先任何
  `ThreadLangValue.ToString()` 都会无限递归（栈溢出）。2026-10-24 已补
  `ToString` / `TypeToString` / `Id`；其它 `GetValue()` 返回 `this` 的 `LangValueType`
  值得同类排查。

- [ ] **IL / VM 模式下标准库 `import` 后访问模块名失败**：`import "Math"` + `Math.Sqrt(9)`
  在解释模式输出 `3`；IL 报 `[NAME_ERROR] 名称 'Math' 未定义`；VM 报
  `[IMPORT_ERROR] 加载模块 'Math' 失败: 无法读取模块文件 'Math'`。
  但 `Docs/MODE_SUPPORT.md` §11 标注三模式均为 ✅，现有 import 测试（`Old8Lang.Tests/Interpreter/Modules/`）
  只覆盖解释模式。需确认 IL/VM 是否应支持标准库 import，再决定修实现还是纠正文档。

- [x] **IL 实例方法读取字段返回随机值**：
  `class M { public v:int; func field()->int { return this.v } }` + `m <- M(9)`，
  `PrintLine(m.field())` 在 `-il` 下每次输出不同的垃圾整数（`-f` 为 `9`）；顶层 `m.v` 读取正常。
  已修复有注解字段类型及方法返回值装箱/拆箱，覆盖多个实例及 `object` 返回值；见 [测试报告](Reports/2026-11-11-09-05-IL实例字段回归测试.md)。

- [ ] **IL 函数无法访问顶层全局变量**：`g <- 10` + `func f() -> int { return g + 1 }` →
  `变量 'g' 未声明`。顶层变量是 `OldLangRun` 的局部变量，函数是独立 `DynamicMethod`，编译函数体时不可见。

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

- [x] **IL 运行期错误的异常值无法 `.ToStr()`**：现已支持直接转换捕获的索引错误和抛出消息，见 [测试报告](Reports/2026-11-11-12-35-IL异常字符串测试.md)。

- [x] **解释器负索引越界的错误信息用了归一化后的下标**：`arr <- [1,2,3]; arr[-5]` 原先报
  `索引越界: 索引 -2 超出范围 [0, 2]`（应报原始的 `-5`）；IL 侧 `IlIndexHelper` 报的是原始下标。
  涉及 `ListLangValue.Get` / `ArrayLangValue.Get` / `StringLangValue.Get` / `TupleLangValue.Get`。
  已修正读取及列表、元组赋值的越界信息，并增加回归测试。

- [ ] **泛型扩展方法与约束**：核实泛型目标类型参数绑定和 `where`/`new()`/`class`/`struct` 约束在注册、方法体及运行时检查中的支持，参考 `Old8Lang/LangParser/Parsers/ExtensionParser.cs` 与相关测试。
- [ ] **VM 原生函数命名参数**：评估如何获取原生函数参数信息（`VirtualMachine.Instructions.ControlFlow.cs` 中的 TODO）。

## 测试与工程问题

- [ ] **IL 字符串 `Length()` 可能导致进程崩溃**：2026-11-11 运行 `TryCatchWithVariableAccess` 时，原生字符串传入 `StringLengthMethod.LengthHelper(StringLangValue)` 触发 `AccessViolationException`；需隔离修复并复测，见 [测试报告](Reports/2026-11-11-12-35-IL异常字符串测试.md)。

- [ ] **IL 类测试中的继承语法失败**：多个 `InheritanceTests` 使用 `class Derived : Base`，而当前语法使用 `extends`；错误提示中的格式字符串异常已修复，需逐项核实旧测试的语法与预期，见 [测试报告](Reports/2026-11-11-10-57-括号错误报告测试.md)。
- [ ] **`SelectStatementTests` 整组测试未完成**：2026-11-11 运行该组时长时间无结果并被手动中止，具体阻塞用例及原因尚未定位。
- [ ] **IL 类实例化测试残留问题**：2026-11-11 `ClassInstantiationTests` 13 项中 6 项失败，涉及集合元素与嵌套对象的链式调用、静态成员、方法重载和标准库方法；具体错误见 [测试报告](Reports/2026-11-11-09-05-IL实例字段回归测试.md)，需逐项复核。

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

- [ ] **全量测试完成性**：三处历史崩溃的定向用例已通过；IL LINQ 其他路径及完整默认档仍需复核。
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
