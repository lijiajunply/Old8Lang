# Old8Lang 三种模式支持矩阵

**最后更新**: 2026-11-28

本文档是 Old8Lang 三种执行模式下**各特性可用性的唯一权威说明**。
原先分散在 `MODE_COMPLETION_STATUS.md` 与 `Mode_Support_Summary.md` 两份文档里，
2026-10-09 合并为本文件——两份表出现过互相矛盾的单元格，合并前已逐格实机复核。

语法本身的定义见 [Old8Lang_Grammar.md](./Old8Lang_Grammar.md) 与 [Old8Lang.ebnf](./Old8Lang.ebnf)；
三种模式的**内部实现机制**差异见 [API_Mode_Comparison.md](./API_Mode_Comparison.md)。

## 模式说明

- **解释器模式**（`-f`）：直接执行 AST，启动最快，动态特性最完整。
- **IL 模式**（`-il`，`-c` 为等价别名）：编译为 .NET IL 后执行，要求完整的类型注解。
- **虚拟机模式**（`-vm`）：编译为自定义字节码后由 VM 执行，支持字节码序列化与调试工具。

## 架构迁移说明

2026-11 的架构迁移只调整 C# 程序集与宿主组合方式（语法/后端/宿主拆分、模块目录隔离），
不改变本矩阵中任何语言可见行为；三种模式的执行结果仍以本表为准。宿主模块目录的 C# API
边界由 `Old8Lang.Tests/Architecture/HostModuleIsolationTests.cs` 固化。

## 图例

- ✅ **完全支持**：已实现且实测通过
- ⚠️ **部分支持**：基本可用但有限制（限制写在备注里）
- ❌ **不支持**：未实现或实测报错

---

## 复核说明

> **做法**（2026-10-08 首次全量复核）：把 [语法文档](./Old8Lang_Grammar.md) 中每一节带 `**模式支持**`
> 标记的示例代码块原样落成 `.old8` 文件，分别在 `-f` / `-il` / `-vm` 下实际运行，按运行结果修正标记；
> 共 120 处标记，其中 80 处带有可运行代码块。对报告中点名的特性另外编写针对性用例复核。
>
> **做法**（2026-10-09 合并复核）：逐格 diff 两份旧文档，对 7 处冲突单元格重新编写用例实测裁决，
> 并把旧文档中 5 处标为「本次未单独验证」的单元格补齐实测。
>
> **复核的边界**（以下情况未取得可判定结论，相关标记保持原状）：
> - 示例本身不可运行（含占位符、依赖外部服务、依赖未实现功能），三种模式同样失败；
> - 依赖 IL 代码生成的项：本机环境下 IL 模式存在**既有的** IL 校验失败
>   （`Common Language Runtime detected an invalid program`），因此 IL 模式列中
>   「因环境而失败」的项已单独标注，未直接判为 ❌。

## 本次复核修正的条目

### 2026-11-28（续：IL 异步生成器非阻塞挂起、遗留项清理与矩阵校正）

- **IL 异步生成器非阻塞挂起**：将 `yield` 纳入 `AsyncStateMachineGenerator` 状态机挂起点与受保护区域派发链，`IlAsyncYieldEmitter` 改用非阻塞 `TaskCompletionSource`（`YieldAsync`），暂停期间占 0 个线程池线程，并支持在 `try`/`catch`/`finally`（含异步 `finally`）与 `using` 中安全挂起、恢复和提前取消清理。
- **IL 类内 `this.method()` 直接 `callvirt`**：普通类（`ClassInitRuntimeExtensions`）与泛型特化类（`GenericClassSpecializer`）改为两遍方法定义，预先在 `LocalManager.BuildingClassMethods` 登记全部 `MethodBuilder` 签名；类内 `this.method()` 恢复直接 `callvirt`/`call` 发射与精确返回类型推断（闭包 `DynamicMethod`、异步状态机及函数值字段仍安全回退动态分派）。
- **类字段集合初值三模式独立**：VM 的 `AllocateObjectInstance` 与导入类 `CreateObjectInstance` 对常量池中的列表/数组/字典（含嵌套）字段初值执行递归深拷贝，三种模式统一为「每实例独立」。
- **`type()` 与 `TypeOf()` 三模式语言类型名统一**：`EnumLangValue.TypeToString()` 返回 `"Int"`（枚举类型校验仍读取 `EnumTypeName`）；`TypeFunction` 与 `ReflectionHelper.TypeOf` 在三种模式统一输出语言类型名（`Int`/`Double`/`String`/`Bool`/`List`/`Array`/`Dictionary`/`Null` 等），不再暴露 `Value`/`Int32`/`EnumLangValue`。
- **支持矩阵全面校正**：同步修正历史过时单元格与限制说明（IL 闭包写回、泛型函数/类/约束、装饰器、接口默认方法、Mixin、异步生成器、`async for-in`、顶层 `for` 作用域、标准库 `import`；VM 异步函数装饰器、Match 元组解构、LINQ `join`/`into`、DAP 调试器）。报告见 `changes/2026-11-28-IL异步生成器与遗留项清理/test.md`。

### 2026-11-28（测试与工程问题全面清理与跨模式语义对齐）

- **IL 表达式内部 `await`**：新增 `AsyncExpressionAwaitLowerer` 将二元运算、调用参数、集合/字典/元组/模板字面量、短路逻辑及三元表达式中的 `await` 预降级提升为状态机临时字段，挂起点保持栈空，消除 `InvalidProgramException`（仅 `catch` 过滤器、`defer` 及非异步生成器 `yield` 保留编译期拒绝）。
- **IL `object` 操作数动态算术**：`NumericBinaryOpHelper`（`+ - * / % ^`）与 `IlDynamicArithmetic` 在任一操作数为 `object` 时按运行期实际数值/字符串类型分派，并在写入已定型局部变量/状态机字段时走 `CoerceDynamic` 写回；`ComparisonOpHelper` 同步补齐 `object` 与数值的动态比较。
- **IL 值类型变量在 `catch` 赋值保留成功分支值**：`CatchAssignmentScanner` 扩展至所有在 `catch` 内被赋值的外层局部变量，统一使用 `object` 槽位并在写入值类型时装箱。
- **三模式线程异常口径统一**：`ThreadLangValue` 在 `Join()` / `Result` 解包 `TargetInvocationException` 并直接重抛原始 `Old8Exception` / `LangThrownException`，不再额外包装「线程执行异常」。
- **既定 IL 边界明确化**：
  - 未定义变量/成员在 IL 下为**编译期报错**（保留静态拼写与成员检查，解释器/VM 为运行期报错）。
  - IL 的 `int` 固定映射 CLR 32 位 `unchecked Int32`（`2147483647 + 2147483646` 回绕为 `-3`；超过 32 位范围请使用 `double` 字面量），解释器与 VM 溢出时提升为 `double`。
- **并发与运行时稳定性**：并发全局函数全部 `int` 实参补齐 `ConcurrencyIlHelpers.EmitLoadIntArgument` 拆箱与 `ChannelTryReceive` 结果展开；`Old8Exception` 异步调用栈改为不可变持久化链表避免多线程/并发任务共享可变 `List`；`PythonProvider` 初始化后调用 `BeginAllowThreads()` 消除多线程测试宿主 GIL 死锁；`RuntimeGlobals` 改用 `ConcurrentDictionary`。
- **全量验证基线（2026-11-28）**：默认档 **7,256 通过 / 0 失败 / 26 跳过**（47s，无挂起）；Architecture **73/73**；性能档 `SpawnTests` **13/13**；`run_syntax_tests.sh` **89/89**；`run_vm_tests.sh` **79/79**。报告见 `changes/2026-11-28-测试与工程问题清理/test.md`。

### 2026-11-15（IL 缺陷修复轮次复核）

按 `changes/Todo.md` 的优先顺序逐项实测了 IL 模式，并把结论落回本文件：

- **不再崩溃**：`Compiler.Classes` 与 `Compiler.Expressions` 两组原先会让测试主机段错误退出
  （比较运算装箱顺序错误导致 `InvalidProgram`），现已完整跑完；`Compiler` 树合计
  252 失败 / 1678 通过 / 25 跳过。
- **整组转绿**：`Compiler.Statements`（186/186）、`Compiler.Reflection`（17/17）；
  `Compiler.Modules`、`Compiler.Linq`、`Compiler.Threading`、`Compiler.Unit`、
  `Compiler.FileHeader` 本就全通过。
- **矩阵修正**：`in` 运算符（2026-11-15 起虚拟机 ✅）、数组/列表 `+` 拼接（2026-11-15 起三模式 ✅）、
  集合 `.Length` 属性（解释器 ❌）、match 高级模式（IL ⚠️）、`yield`（IL ❌）、
  Mixin（IL ❌ / 虚拟机 ⚠️）、带默认实现的接口（IL ⚠️）、反射 API（IL ✅）。
- **新增**「三种模式共同的跨模式差异」小节，列出 10 条尚未统一的语义。

### 2026-10-24（P1 跨模式语义修复）

| 功能 | 旧标记 | 现标记 | 依据 |
|-----|-------|-------|------|
| lambda 写回外层局部变量（解释器） | ❌ | ✅ | 外层帧仍存活时闭包直接在外层管理器的帧上执行，写回与具名嵌套函数一致；外层帧已退出时退回捕获快照。虚拟机用共享单元，两模式结果一致（`122`） |
| 脚本顶层 C 风格 `for` 循环变量（虚拟机） | ⚠️ | ✅ | 循环变量改为循环作用域的局部变量，每轮换共享单元；闭包得 `012`，循环结束后变量不可见，均与解释器一致 |
| 负索引（IL / 解释器元组） | ⚠️ | ✅ | 新增 `IlIndexHelper` 统一归一化负下标；`.old8` 的 IL 套件中 `nested_slice_access_test` 转绿 |
| 同一模块符号重复导入（解释器） | ❌ | ✅ | 模块首次执行时记录导出，再次导入从记录恢复，不再重复执行模块体 |
| 泛型类省略类型参数（解释器） | ❌ | ✅ | 从 `init` 形参推断类型参数；接口约束按实际实现校验，`T?` 接受值或 `null` |

### 2026-10-10（虚拟机模式缺口逐条实测）

| 功能 | 旧标记 | 现标记 | 依据 |
|-----|-------|-------|------|
| 异步生成器（虚拟机） | ❌ | ✅ | 原判定把「体内含 `await`」当成整个特性不可用；实测不含 `await` 时本来就通过，含 `await` 的失败原因是恢复逻辑里的 `await` 会切线程（详见「已知限制」第 1 条），已修 |
| `async for-in`（虚拟机） | ❌ | ✅ | 与异步生成器同一处缺陷；同时补齐多标识符解构 `async for k, v in ...` |
| 元组 `.Count()`（虚拟机） | ⚠️ | ✅ | `(1,2,3).Count()` → `3`；顺带修好原生容器的负索引 |
| 集合直接打印（虚拟机） | ⚠️ | ✅ | 直接打印与 `.ToStr()` 一致：`[1, 2, 3]` / `{"a": 1}` / `(1, 2, 3)` |
| 原生函数「无返回值」与 `null`（虚拟机） | ⚠️ | ✅ | `r <- Sleep(1)` 后 `r == null` 为 `false`（与解释器一致） |
| `TypeLangValue` 实例方法（虚拟机） | ⚠️ | ✅ | 原为整族抛 `ArgumentException`（不是「固定返回 false」），现 9 个方法全部可用 |
| `TypeOf` / `GetType`（三模式） | 未记录 | ✅ | `TypeOf` 接受任意值，`GetType` 接受内建类型名（`int` / `list` / `dict` …） |
| 运行期错误位置（虚拟机） | ⚠️ | ✅ | 编译期把源码位置写进指令，错误报告带行列与上下文；除零改为 `ZERO_DIVISION_ERROR` |

### 2026-10-09（合并两份旧文档时实测裁决）

| 功能 | 旧标记 | 现标记 | 依据 |
|-----|-------|-------|------|
| 泛型类（虚拟机） | ❌ | ✅ | `class Box<T>` + `Box<int>()` + `set`/`get` 实测得 `42` |
| 函数装饰器（虚拟机） | ❌ | ✅ | `@twice` 包装的 `inc(10)` 实测得 `12`（写回正确） |
| Match 表达式（虚拟机） | ⚠️ | ✅ | 三种模式均输出 `two`；旧标「无法判定」是因文档示例漏了 `case` 关键字 |
| 异步生成器（虚拟机） | ✅ | ❌ | 报 `STATE_ERROR` 求值栈为空（指令 `IteratorMoveNext`） |
| `async for-in`（虚拟机） | ✅ | ❌ | 同上，两者是同一处缺陷 |
| 闭包写回外层局部变量（虚拟机） | ⚠️ | ✅ | 闭包内两次写回后外层读出 `2` |
| 闭包写回外层局部变量（解释器 / IL） | 未列 | ❌ → 解释器 ✅ | lambda 写回原不传回外层（得 `0`）；**解释器已于 2026-10-24 修复**，IL 仍为 ❌ |
| `#undef`、条件表达式、命令行 `-D`（虚拟机） | ⚠️ 未验证 | ✅ | 三者均实测通过 |

### 2026-10-08（首次全量复核）

| 功能 | 原标记 | 现标记 | 依据 |
|-----|-------|-------|------|
| 运算符重载（虚拟机） | ❌ | ✅ | `_add`/`_mul`/`_eq` 均正确生效 |
| 函数装饰器（IL 模式） | ⚠️ | ❌ | 编译期报「返回类型不匹配」（期望 `Int32`，实际 `Object`） |
| Task API（虚拟机） | ✅ | ✅ | 静态方法、`Task.Factory.StartNew`、`Task.Delay` 的 `CancellationToken`、`Task.Delay` 作为函数值均可用（2026-10-24） |
| Thread API（虚拟机） | ✅ | ✅ | `Thread.Sleep` 与 `Thread.CurrentThread`（属性 / 无参调用两种写法）可用（2026-10-24） |
| Assert 断言（虚拟机） | ✅ | ⚠️ | 24 个断言方法可用，但失败抛 `AssertionError`（解释器抛的普通异常语言层捕获不到） |
| 原生库导入 extern（IL / 虚拟机） | ✅ | ⚠️ | 原生 C 库自 2026-10-09 起可用；Python / JavaScript / C# DLL 未验证 |
| 数组 / 列表 / 字典直接打印（虚拟机） | ✅ | ⚠️ | 直接 `print` 输出 .NET 类型名，`.ToStr()` 正常 |
| 列表推导式（虚拟机） | ✅ | ✅（已修） | 原为 `VisitListComprehension => null` 的空实现，栈失衡后报 `Stack empty` |
| 无返回值调用参与表达式（虚拟机） | 未标 | ✅（已修） | 原报 `Stack empty`，现统一「一次调用在求值栈上留下一个值」 |
| 返回值残留（解释器） | 未标 | ✅（已修） | 函数走末尾无 `return` 时曾返回上一次调用的残留值 |

---

## 支持矩阵

### 1. 词法规则

| 特性 | 解释器 | IL | 虚拟机 | 备注 |
|------|--------|-----|--------|------|
| 文件头指令 | ✅ | ✅ | ✅ | 元数据与配置指令 |
| 单行注释 (`//`) | ✅ | ✅ | ✅ | |
| 文档注释 (`///`) | ✅ | ✅ | ✅ | |
| 标识符 / 关键字 | ✅ | ✅ | ✅ | |
| 字面量 | ✅ | ✅ | ✅ | 整数、浮点、字符串、字符、布尔、`null` |

### 2. 运算符

| 特性 | 解释器 | IL | 虚拟机 | 备注 |
|------|--------|-----|--------|------|
| 算术 (`+ - * / % ^`) | ✅ | ✅ | ✅ | 数组/列表的 `+` 按顺序拼接在三种模式都支持（2026-11-15 统一）；数组 + 列表的混合拼接仍一致报错 |
| 比较 (`== != < > <= >=`) | ✅ | ✅ | ✅ | |
| `in` 运算符 | ✅ | ✅ | ✅ | 2026-11-15 新增 `OpCode.In`：数组/列表按值、字典按键、字符串按字符/子串，语义与解释器一致 |
| 逻辑 (`and` `or` `xor` `not`) | ✅ | ✅ | ✅ | |
| 赋值 (`<-`) | ✅ | ✅ | ✅ | |
| 成员访问 (`.`) / 索引 (`[]`) | ✅ | ✅ | ✅ | |
| 三元 (`? :`) | ✅ | ✅ | ✅ | |
| **用户定义的运算符重载** | ✅ | ❌ | ✅ | Python 风格（`_add`、`_mul`、`_eq` 等）；一元运算符重载三种模式均不支持 |

### 3. 数据类型

| 特性 | 解释器 | IL | 虚拟机 | 备注 |
|------|--------|-----|--------|------|
| 基本类型 | ✅ | ✅ | ✅ | `int` `double` `string` `char` `bool` `null` |
| 数组 `[]` | ✅ | ✅ | ✅ | 创建/索引/长度（含负索引）正常；直接 `print` 与 `.ToStr()` 一致 |
| 列表 `{}` | ✅ | ✅ | ✅ | `Add`/`Remove`/`Count`/索引（含负索引）正常；直接 `print` 同上 |
| 字典 | ✅ | ✅ | ✅ | 遍历语义与顺序 2026-10-09 起与解释器一致；直接 `print` 同上 |
| 元组 `()` | ✅ | ✅ | ✅ | 索引（含负索引）与 `.Count()` 可用 |
| 整数、步长及字符范围 | ✅ | ✅ | ✅ | 例如 `[1~10]`、`[1~10:2]`、`['A'~'Z']` |
| 可空类型 `int?` | ✅ | ✅ | ✅ | |
| 泛型集合 `list<T>` 等 | ✅ | ✅ | ✅ | |
| 联合 / 交叉类型 | ✅ | ✅ | ✅ | `A \| B`、`A & B` |

### 4. 表达式

| 特性 | 解释器 | IL | 虚拟机 | 备注 |
|------|--------|-----|--------|------|
| 基本表达式 | ✅ | ✅ | ✅ | |
| 成员访问 / 索引访问 | ✅ | ✅ | ✅ | |
| 切片 `arr[1:3]` | ✅ | ✅ | ✅ | |
| 类型转换 `as` | ✅ | ✅ | ✅ | VM 支持将字典转换为本地或导入类的实例。 |
| 三元表达式 | ✅ | ✅ | ✅ | |
| 字符串模板 `$"..."` | ✅ | ✅ | ✅ | |
| Match 表达式 | ✅ | ✅ | ✅ | 语法为 `x <- match v { case p -> e ... }`，分支必须带 `case` 关键字；三种模式均支持值、绑定、范围、类型、多元素元组解构与 `guard`（IL 通过 `IlMatchRegistry` 复用解释器匹配语义；VM 于 2026-11-28 补齐原生多元组展平解构） |
| 列表推导式 | ✅ | ✅ | ✅ | |

### 5. 语句与控制流

| 特性 | 解释器 | IL | 虚拟机 | 备注 |
|------|--------|-----|--------|------|
| 块语句 / 变量声明 / 类型注解 | ✅ | ✅ | ✅ | `{ }`、`<-`、`:type` |
| `if`-`elif`-`else` | ✅ | ✅ | ✅ | |
| `for` / `while` / `for-in` | ✅ | ✅ | ✅ | 范围写法是 `for i in [0~10]` |
| `switch-case` | ✅ | ✅ | ✅ | |
| `break` / `continue` / `return` | ✅ | ✅ | ✅ | |
| `using` 语句 | ✅ | ✅ | ✅ | |
| `select` 语句 | ✅ | ✅ | ✅ | Channel 多路选择 |
| `defer` 语句 | ✅ | ✅ | ✅ | |
| `yield`（同步生成器） | ✅ | ❌ | ✅ | IL 模式不支持同步生成器，编译期明确报 `CompilerException`（含 `yield` 的异步生成器见第 8 节，三模式均支持） |

### 6. 函数

| 特性 | 解释器 | IL | 虚拟机 | 备注 |
|------|--------|-----|--------|------|
| 基本函数声明 | ✅ | ✅ | ✅ | `func name() { }`；`function` 不是关键字 |
| 参数 / 默认参数 / 可变参数 | ✅ | ✅ | ✅ | |
| 返回类型注解 | ✅ | ✅ | ✅ | IL 模式必需 |
| Lambda 表达式 | ✅ | ✅ | ✅ | |
| 高阶函数 | ✅ | ✅ | ✅ | |
| 闭包（只读捕获） | ✅ | ✅ | ✅ | |
| 具名函数 / lambda / 类方法访问顶层变量 | ✅ | ✅ | ✅ | IL 模式自 2026-11-11 起把被函数引用的顶层变量放入运行期全局槽位；同名参数、局部变量和字段仍遮蔽它。异步函数自 2026-11-22 起通过 `AsyncLocal` 上下文读写同一份槽位 |
| **闭包写回外层局部变量** | ✅ | ✅ | ✅ | 虚拟机自 2026-10-09 起由共享单元（`UpValueCell`）支持；解释器自 2026-10-24 起在“外层帧仍存活”时直接在外层帧上执行闭包；IL 自 2026-11-11~28 起支持局部变量读写、形参捕获、嵌套闭包隔离、循环每轮快照与函数类型参数再捕获 |
| 泛型函数 | ✅ | ⚠️ | ⚠️ | IL（通过 `GenericFunctionSpecializer`）与虚拟机均需显式给出类型参数（`id<int>(1)`），解释器支持实参推断 |
| 泛型约束 | ✅ | ✅ | ✅ | 三模式均支持 `where` / `new()` / `class` / `struct` / 接口 / 交叉类型参数约束及泛型扩展方法约束（2026-11-28 补齐） |
| 函数装饰器 | ✅ | ✅ | ✅ | 三模式均支持普通函数与异步函数装饰器（含参数化、堆叠、改变返回类型及读写顶层变量，2026-11-23~28 补齐） |
| 访问修饰符 | ✅ | ✅ | ✅ | `public` / `private` / `static` |

### 7. 类和面向对象

| 特性 | 解释器 | IL | 虚拟机 | 备注 |
|------|--------|-----|--------|------|
| 基本类声明 / 字段 / 方法 | ✅ | ✅ | ✅ | |
| 构造函数 (`init`) | ✅ | ✅ | ✅ | |
| 访问修饰符 | ✅ | ✅ | ✅ | |
| 类继承 (`extends`) | ✅ | ✅ | ✅ | |
| 接口 / 接口实现 | ✅ | ✅ | ✅ | IL 自 2026-11-15 起通过 `MergeMixinMembers` 合并接口默认方法实现，类自身实现优先 |
| Mixin (`mixin` / `with`) | ✅ | ✅ | ⚠️ | IL 自 2026-11-15 起递归合并 mixin（含父 mixin）的字段与方法；虚拟机可绑定 mixin 方法，mixin 字段待进一步核验 |
| 泛型类 | ✅ | ⚠️ | ✅ | 三模式支持显式写 `Box<int>()`（含可空 `T?` 与静态成员）；解释器额外支持由构造函数实参推断（`Box(123)`），IL 省略类型参数时报明确编译错误；**`new` 关键字不存在** |
| `this` / `super` | ✅ | ✅ | ✅ | |
| 枚举 | ✅ | ✅ | ✅ | 含显式值、混合值、成员访问 |

### 8. 异步编程

| 特性 | 解释器 | IL | 虚拟机 | 备注 |
|------|--------|-----|--------|------|
| `async` / `await` | ✅ | ⚠️ | ✅ | IL 支持顶层、`try`/`catch`/`finally`/`using` 及复合表达式内部 `await`；仅 `catch` 过滤器、`defer` 与非异步生成器 `yield` 保留编译期拒绝 |
| 异步生成器 | ✅ | ✅ | ✅ | 三模式均支持在异步生成器内混用 `await` 与 `yield`；IL 自 2026-11-28 起将 `yield` 纳入异步状态机实现非阻塞挂起（0 线程占用）；VM 见「已知限制」第 1 条 |
| `async for-in` | ✅ | ✅ | ✅ | 三模式均支持 `async for k, v in ...` 多标识符解构，并兼容异步流与普通可枚举集合 |
| Task API | ✅ | ✅ | ✅ | 实例方法 `Then` / `Catch` / `Finally` / `ContinueWith` 自 2026-10-10 起三种模式都可用（IL 用原生 `Task` 表示，VM 用 `TaskLangValue`）；静态方法见「已知限制」第 3 条 |

### 9. 并发编程

| 特性 | 解释器 | IL | 虚拟机 | 备注 |
|------|--------|-----|--------|------|
| `Mutex` | ✅ | ✅ | ✅ | 5 个函数 |
| `Semaphore` | ✅ | ✅ | ✅ | 5 个函数 |
| `AtomicInt` | ✅ | ✅ | ✅ | 8 个函数 |
| `Channel` | ✅ | ✅ | ✅ | 8 个函数 |
| `ReadWriteLock` | ✅ | ✅ | ✅ | 8 个函数 |
| `CountDownLatch` | ✅ | ✅ | ✅ | 6 个函数 |
| `CyclicBarrier` | ✅ | ✅ | ✅ | 6 个函数 |
| `CancellationTokenSource`（函数形式） | ✅ | ✅ | ✅ | 4 个函数 |
| `TaskCompletionSource`（类形式） | ✅ | ❌ | ✅ | 2026-10-10 起虚拟机登记了构造函数与 6 个实例方法（`SetResult` / `SetException` / `SetCanceled` / `TrySet*`）；`IL` 模式本就不支持该类型 |
| `CancellationTokenSource`（类形式） | ✅ | ❌ | ✅ | 2026-10-10 起虚拟机支持 `CancellationTokenSource()` / `(timeout)` 及 `Cancel` / `CancelAfter` / `Dispose` / `IsCancellationRequested` |
| `spawn` | ✅ | ✅ | ✅ | **只创建线程，必须再调用 `Start()` 才会执行**，之后 `Join()` 等待并取回返回值；三种模式一致 |
| `Sleep` / `GetCurrentThreadId` / `GetProcessorCount` | ✅ | ✅ | ✅ | 全局函数形式，无需 `Thread.` 前缀 |
| Thread API（静态类） | ✅ | ✅ | ✅ | 三种模式均支持 `Thread.Sleep`、`Thread.CurrentThread`（属性及无参调用）、`Lock` 共享锁变量及 `spawn` 全局上下文同步 |
| Thread 实例方法 `Then` / `Cancel` / `Retry` / `WithTimeout` | ✅ | ✅ | ✅ | IL 自 2026-10-10 起可用（配合 `spawn`）；虚拟机同日起可用（`VMThreadLangValue` 专属实现）。`Cancel` 是**协作式**的：只对尚未 `Start()` 的线程生效；`Retry` 只能用于 `spawn(...)` 创建的线程 |

### 10. 异常处理

| 特性 | 解释器 | IL | 虚拟机 | 备注 |
|------|--------|-----|--------|------|
| `try-catch-finally` | ✅ | ✅ | ✅ | |
| `throw` | ✅ | ✅ | ✅ | |
| 异常类型捕获 / 变量绑定 / 嵌套异常 | ✅ | ✅ | ✅ | |

### 11. 模块系统

| 特性 | 解释器 | IL | 虚拟机 | 备注 |
|------|--------|-----|--------|------|
| `import` 简单导入 | ✅ | ✅ | ✅ | 2026-11-28 起三模式已打通 `Math`/`Regex`/`Crypto`/`Csv`/`Time`/`Vector`/`Net` 等标准库导入、函数体内调用、命名参数及原生静态类导出（含 VM 字节码往返） |
| 命名导入 / 别名导入 / 通配符导入 | ✅ | ✅ | ✅ | 三模式均支持别名导入、选择性导入与模块作用域隔离（2026-11-28 补齐） |
| 模块注册表 / 加载器 | ✅ | ✅ | ✅ | `ModuleRegistry` / `ModuleLoader` |

### 12. 外部互操作

| 特性 | 解释器 | IL | 虚拟机 | 备注 |
|------|--------|-----|--------|------|
| 原生 C 库（`extern "dll" func`） | ✅ | ⚠️ | ⚠️ | 虚拟机自 2026-10-09 起覆盖三种调用约定、批量/单函数导入、别名与参数转换；缺库报 `IO_ERROR`、缺符号报 `METHOD_NOT_FOUND_ERROR` |
| `extern` 导入（Python / JavaScript / C# DLL） | ✅ | ❌ | ❌ | 三类在 IL 与虚拟机下均未打通 |
| 托管库导入（`extern "Old8LangLib" ...`） | ✅ | ❌ | ❌ | 调用报名称/方法未找到 |
| Extern 函数包装 | ✅ | ✅ | ✅ | `ExternFunctionWrapper` |

### 13. 预编译指令

| 特性 | 解释器 | IL | 虚拟机 | 备注 |
|------|--------|-----|--------|------|
| `#define` / `#undef` | ✅ | ✅ | ✅ | |
| `#if` / `#elif` / `#else` / `#endif` | ✅ | ✅ | ✅ | |
| 条件表达式 (`&&` `\|\|` `!`) | ✅ | ✅ | ✅ | |
| 命令行 `-D` 参数 | ✅ | ✅ | ✅ | **必须写在文件名之后**：`-f file.old8 -D SYMBOL` |

### 14. 内置函数

| 特性 | 解释器 | IL | 虚拟机 | 备注 |
|------|--------|-----|--------|------|
| `Print` / `PrintLine` | ✅ | ✅ | ✅ | 2026-10-10 起虚拟机直接打印集合与 `.ToStr()` 一致（`[1, 2, 3]` / `{"a": 1}` / `(1, 2, 3)`） |
| `ReadLine` | ✅ | ✅ | ✅ | |
| `ToStr` / `ToInt` / `ToDouble` / `ToBool` | ✅ | ✅ | ✅ | |
| 集合方法（`Add` `Remove` `Count` 等） | ✅ | ✅ | ✅ | 元组 `.Count()` 自 2026-10-10 起可用 |
| 字符串方法（`Length` `ToUpper` `Split` 等） | ✅ | ✅ | ✅ | |
| `JsonSerialize` / `json` | ✅ | ❌ | ✅ | VM 支持类实例字段、嵌套字典、列表、数组和标量。 |
| 反射函数与 `TypeLangValue` 实例方法 | ✅ | ✅ | ✅ | 虚拟机下 9 个类型值方法自 2026-10-10 起可用；IL 的 `CreateInstance` / `GetField` / `SetField` / `InvokeMethod` 自 2026-11-15 起对原生对象可用；`TypeOf` / `GetType` 见 [API_REFERENCE.md](./API_REFERENCE.md#24-反射) |

### 15. 类型系统

| 特性 | 解释器 | IL | 虚拟机 | 备注 |
|------|--------|-----|--------|------|
| 动态类型 / 类型注解 / 类型推断 | ✅ | ✅ | ✅ | IL 模式要求完整注解 |
| 泛型集合 / 泛型函数 | ✅ | ⚠️ | ⚠️ | 三模式均支持泛型集合与泛型函数；IL 与虚拟机下泛型函数需显式提供类型参数 |
| 泛型类 | ✅ | ⚠️ | ✅ | IL 与虚拟机均支持显式类型参数泛型类；解释器额外支持构造实参推断 |
| 可空类型 / 联合类型 / 交叉类型 | ✅ | ✅ | ✅ | |
| 类型转换 (`as`) | ✅ | ✅ | ✅ | |

### 16. 其他特性

| 特性 | 解释器 | IL | 虚拟机 | 备注 |
|------|--------|-----|--------|------|
| LINQ 查询 | ✅ | ✅ | ✅ | |
| 断言 API（`Assert`） | ✅ | ⚠️ | ✅ | 虚拟机下 24 个方法均可用，失败抛 `AssertionError`（**语言层 `try/catch` 能捕获**；解释器抛的普通异常捕获不到） |
| 字节码序列化 | ❌ | ❌ | ✅ | `BytecodeFile` / `ConstantPool`；`-compile` 产出、`-execute` 载入，有 CLI 与测试覆盖 |
| 调试器 | ✅ | ❌ | ✅ | 解释器侧提供 `debug-start <文件>` 交互调试；虚拟机侧自 2026-11-27 起提供 `Old8Lang.App --dap`（`VmDapServer`）标准 DAP 调试与 VS Code 集成 |
| 性能分析器 | ✅ | ❌ | ❌ | 解释器侧 `ProfilerManager`，入口是交互式 `profile start` 命令 |
| 反汇编器 | ❌ | ❌ | ❌ | 字节码 `Disassembler` 类存在但**全仓库无调用点**，当前无法使用 |

---

## 已知限制

### 三种模式跨模式语义核对表（2026-11-28 校正）

本表保留**仍存在的既定模式差异**（前 3 项）以及近期已逐项统一并有回归覆盖的跨模式行为：

| 场景 | 解释器 | IL | 虚拟机 | 说明 |
|------|--------|-----|--------|------|
| 32 位 `int` 溢出（`2147483647 + 2147483646`） | 提升为 `double`（`4294967293`） | 32 位 `unchecked` 回绕（`-3`） | 提升为 `double`（`4294967293`） | **既定差异**：IL 刻意保持 `int` 静态映射 CLR `Int32` 以保性能；超范围计算请使用 `double` 字面量 |
| `(-1.0).Sqrt()` | ❌ 没有该方法（用 `Math.Sqrt`） | 抛「平方根函数的参数必须是非负数」 | 抛同样的错 | **既定差异**：有效值在 IL/VM 返回 `2`；跨模式推荐使用 `Math.Sqrt(value)` |
| 私有成员访问 / 未声明变量 / 未知方法（如 `"".Match(...)`） | 运行期报错 | 编译期报错 | 运行期报错 | **既定差异**：2026-11-23 起 VM 补齐私有字段访问控制（`ACCESS_VIOLATION_ERROR`）；编译期 vs 运行期阶段差异属后端模型本身；正则请用 `Regex.RegexIsMatch` / `Regex.RegexMatch` |
| `obj + 1`（`obj` 静态类型 `object`，运行期是 `double`） | ✅ | ✅ | ✅ | 2026-11-28 起 IL 六种算术运算均走 `IlDynamicArithmetic` 并在写回已定型变量时做动态转换 |
| 数组 / 列表 `+` 拼接 | ✅ | ✅ | ✅ | 2026-11-15 统一：同类型按顺序拼接；数组 + 列表混合一致报错 |
| 集合的 `.Length` 属性（无括号） | ✅ | ✅ | ✅ | 2026-11-15 统一：数组/列表/字符串/字典均支持，等价 `len(x)`；字典键名 `"Length"` 用 `d["Length"]` |
| `PrintLine(null)`、`null.ToStr()` 与字符串拼接显示 | `null` / `xtrue` / `x[1, 2]` | `null` / `xtrue` / `x[1, 2]` | `null` / `xtrue` / `x[1, 2]` | 2026-11-15 统一为小写 `null` 与语言显示规则；类型名仍是 `Null` |
| 字典缺失键 `d["missing"]` | `KEY_ERROR` | `KEY_ERROR` | `KEY_ERROR` | 2026-11-15 统一；安全访问用 `GetOrElse` / `TryGet` |
| `int?` / `string?` 联合类型的返回值 | ✅ | ✅ | ✅ | 2026-11-15 起 IL 用 `object` 承载并在返回路径做校验转换 |
| 集合注解的元素类型不匹配（`list<int> <- {1, "x"}`） | ❌ 运行期 `TYPE_ERROR` | ❌ 编译期报错 | ❌ 编译期报错 | 2026-11-28 统一拒绝不匹配元素（含嵌套集合） |
| `try { } catch (e) { x <- e }` 之后读 `x` | 保持原值 | 保持原值 | 保持原值 | 2026-11-28 起 IL 覆盖值类型与引用类型局部变量 |
| 线程内 `throw` 后 `Join()` | 保留原始语言错误 | 保留原始语言错误 | 保留原始语言错误 | 2026-11-28 统一：`catch (e)` 均获得原始抛出的错误消息 |
| LINQ `join` 内层变量与 `join ... into g` 分组延续 | ✅ | ✅ | ✅ | 2026-11-23~28 统一：支持多段 `join`/`let`/`orderby`/`group by`/`into`（165/165 全通过） |
| 类字段可变集合初值（如 `public items <- {1, 2}`） | 每实例独立 | 每实例独立 | 每实例独立 | 2026-11-28 统一：VM 实例化时深拷贝常量池集合初值，修改互不影响 |
| `type(枚举值)` / `TypeOf(枚举值)` 与内建类型名 | `Int` | `Int` | `Int` | 2026-11-28 统一返回语言类型名（`Int`/`Double`/`String`/`List`/`Array`/`Dictionary`/`Null` 等） |

### 三种模式共同

1. **`new` 关键字不存在**：类实例化直接写 `ClassName()` 或 `Box<int>()`。
2. **一元运算符重载**不支持。
3. **`spawn` 必须配合 `Start()`**：只创建不启动时线程函数不会执行，未启动就 `Join()` 报 `Thread has not been started`。
4. **变量声明用 `<-`**，函数关键字是 `func`（不是 `function`），注释用 `//`（不是 `#`）。

### 解释器模式

1. 性能低于 IL 模式与虚拟机模式，缺少编译时优化。

### IL 模式

1. **类型注解要求**：函数参数与返回值必须有类型注解或默认值。lambda 也必须有参数类型注解，
   否则生成的委托形参是 `object`。
   **2026-11-15 更新**：lambda 参数注解的白名单已补上 `array` / `dictionary` / `object` / `function`；
   lambda 现在也能捕获外层局部变量、函数形参以及外层闭包已捕获的变量。
   `object` 静态类型的操作数参与算术运算已于 2026-11-28 起走 `IlDynamicArithmetic` 运行期分派。
2. **动态与泛型边界**：不支持用户定义运算符重载与同步生成器（`yield`，编译期明确报错）；泛型函数与泛型类支持显式类型参数特化（不支持由构造函数实参省略推断）；普通与异步函数装饰器均已通过转发委托支持。
3. 不支持 C/C++ P/Invoke、Python 互操作与托管库导入。
4. **异步功能支持边界**（2026-11-28 更新）：异步状态机在 `try` / `catch` / `finally` / `using` 区域以及
   复合表达式内部（如 `1 + await f()`、`PrintLine(await f())`、集合/字典/元组/模板字面量、短路逻辑与三元表达式）的
   `await` 均已通过 `AsyncExpressionAwaitLowerer` 支持；含 `yield` 的异步生成器已直接纳入异步状态机实现非阻塞挂起（0 线程池线程占用）并支持 `async for-in` 迭代；
   用户 `finally` 与 using 的 Dispose 只在函数真正退出时执行一次。仍不支持的位置会在编译期给出明确错误：
   `catch` 过滤器中的 `await`/`yield`、`defer` 中的 `await`/`yield`，以及非异步生成器内的 `yield`。
   `Task.Completed` / `Task.WaitAll` / `Task.WaitAny` 已于 2026-11-23 在三种模式下统一实现。
5. **IL 校验失败已清空（2026-11-15）**：`Common Language Runtime detected an invalid program`
   在 `TestFiles/CompilerTests/` 的 157 个用例里已不再出现。**2026-11-15 实测**：
   `dotnet test Old8Lang.Tests --filter "FullyQualifiedName~Compiler"` 逐组运行共 2017 例 →
   **26 失败 / 1965 通过 / 26 跳过**（含本日新增用例）。当天失败数从 252（更早尚有 323 失败，
   且 `Compiler.Classes` / `Compiler.Expressions` 会让测试主机直接崩溃）逐步降到 224，再到 212；
   期间修掉了字段写入段错误、`in` / `Task` 静态方法 / 类方法实参 / 异步状态机 / LINQ `where` /
   字符串实例方法与语言值相等 / 泛型 `T?` 实参 / 并发函数 int 实参等一批无效 IL；
   并统一了数组/列表 `+` 拼接、虚拟机 `in` 运算、范围端点排除语义、`.Length` 属性、null 显示，
   字符串实例方法的 IL 操作数包装（`"abc".StartsWith("a")` 曾生成无效 IL），
   元组实例方法在 IL / 虚拟机下的跨模式支持，链式比较 `a < b < c` 的 bool 比较拒绝，字典缺失键统一为 `KEY_ERROR`，以及实例 `Sqrt` 负数、VM 集合字段初值、
   IL 包装字典显示、枚举成员按 int 返回、object 间动态比较、`as` 转换走解释器语义、枚举函数签名、extern 提示、除零错误码、未解析函数调用回退、装饰器转发、lambda `object` 类型注解、集合注解元素类型检查、mixin/接口默认方法合并、类型错误运行期化、异常反射解包、命名实参校验、循环动态槽收敛、边界用例、逻辑运算校验、装饰器槽位同步、async 用例 await 迁移等修复。
   其余失败以异步、函数、类、表达式、集合的结果/类型推断缺口为主。另修掉「类体在文件结尾
   被截断时解析器死循环」的进程挂死问题。
   对 `TestFiles/CompilerTests`（157）、`InterpreterTests`（188）、`SyntaxTests`（91）
   共 436 个用例逐个用 `-il` 运行（10 秒超时）：段错误、invalid program、超时均为 0。
6. **高阶实例方法自 2026-10-10 起可用**：`List` 的 `FindAll`/`FlatMap`/`GroupAdjacentBy`/`Partition`/
   `Single`/`SingleOrDefault`/`SkipWhile(Indexed)`/`TakeWhile(Indexed)`、`Dictionary` 的
   `Map`/`Filter`/`ForEach`、泛型列表的 `SelectMany`/`SortBy`/`Zip3`/`Sum(selector)`/
   `Average(selector)`/`Max(selector)`/`Min(selector)`、`Array.GroupAdjacentBy` 在 IL 下都能真正执行
   （lambda / 具名函数会被编译成委托，运行期以「函数值」传给解释器语义的 helper）。
   用法限制：
   - lambda 必须有类型注解；
   - ~~lambda 内部不能读写外层普通局部变量~~：2026-11-11 起可读写，2026-11-15 起还能捕获
     函数形参与外层闭包的捕获变量；
   - ~~不能在方法调用结果上直接链式调用~~：2026-11-11 起 `l.Count().ToStr()`、
     `l.FindAll(f).Count()` 可用。
7. **扩展方法自 2026-10-10 起可用**：`extension X { func f(...) }` 的声明在编译前的解释器预执行阶段
   注册，调用点按普通实例方法解析，扩展方法体仍由解释器执行（因此函数体内可以使用全局函数/变量）。
8. **线程自 2026-10-10 起可用**：`spawn(f)` 会真的创建线程对象，
   `Thread.Start` / `Join` / `IsAlive` / `Then` / `Cancel` / `Retry` / `WithTimeout` 都可用；
   `Retry` 必须写成 `spawn(f).Retry(n)`（解释器把 `Retry` 特判为「重新执行函数调用」）。
9. **顶层 C 风格 `for` 循环变量作用域（已于 2026-11-13~15 修复）**：IL 模式现已将循环初始化变量收敛到独立局部绑定并在循环退出后恢复外层作用域，同名嵌套循环与每轮闭包捕获语义均与解释器、虚拟机一致。
10. **类内 `this.method()` 直接分派（2026-11-28 优化）**：普通类与泛型特化类在构建期间预先登记所有 `MethodBuilder`，类内 `this.method()`（含前向调用、私有方法、重载、命名实参与继承父类方法）直接发射 `callvirt`/`call` 并保留精确返回类型推断；闭包 `DynamicMethod`、异步状态机及函数值字段仍安全回退运行期分派。

### 虚拟机模式

1. **异步生成器与 `async for-in`（2026-10-10 修复）**：原先体内含 `await` 时报
   `STATE_ERROR` 求值栈为空（指令 `IteratorMoveNext`）。真实原因是恢复异步生成器时用了
   `await`：虚拟机的求值栈、调用栈与帧池都按线程存放，`await` 让线程池线程接管后，
   恢复出来的是那个线程上的空栈（`_callStack.Pop()` 也会抛 `Stack empty`）。
   现已改为同步恢复（生成器体内的 `await` 改为阻塞等待），并让异步生成器实现 `IEnumerable`
   以复用普通 for-in 的迭代协议；`async for k, v in ...` 多标识符解构同时可用。
   同步生成器（`yield`）不受影响；`yield` 集合/类实例也不再丢值。
2. **异步函数的装饰器（已于 2026-11-28 支持）**：三模式现已补齐异步 Lambda / 异步函数值以及简单、参数化与堆叠异步函数装饰器（含 VM 字节码往返）。
3. **静态类 API 大部分可用**：编译期把 `类名.方法(...)` 改写成静态类原生调用，支持集合集中在
   `VmStaticClassRegistry`。可用：`Task.Delay`/`WhenAll`/`WhenAny`/`WaitAll`/`WaitAny`/`Completed`/`FromResult`/`FromException`/
   `Run`/`StartNew`、`Task.Factory`（属性）与 `Task.Factory.StartNew`、`TaskScheduler.Default`、
   `Thread.Sleep`、`Thread.CurrentThread`（属性 / 无参调用）、`Assert` 的 24 个断言方法、
   `TestRunner`（`Describe` / `It` / `Test` / `RunTest` 由虚拟机执行传入的函数值，其余复用解释器实现）、
   `Mock` 的全部静态方法、`TaskCompletionSource` / `CancellationTokenSource`（类形式）。
   剩余限制：静态类方法的命名参数、裸引用静态类名（如 `value <- Task`）仍报 `VM_UNSUPPORTED_ERROR`
   （裸引用具体方法如 `f <- Task.Delay` 已可用）。
   上述静态类/属性支持自 2026-10-24 起生效。
4. **`await` 的结果 + 函数返回类型注解（2026-10-10 修复）**：原先 `func f() -> int { return await g() }`
   报 `类型不匹配: 期望 int，但得到 IntLangValue`。现在 `CheckTypeMatch` 会先拆开基本类型的
   `LangValueType` 包装，且异步任务结果改为无损转换（集合/类实例不再退化成 `VoidLangValue`），
   `-> int` / `-> string` / `-> list` 都能直接返回 `await` 的结果。
   2026-10-24 起，异步函数里**显式 `return null`** 也会保留为 `NullLangValue`
   （`Task(Completed: Null)`，与解释器一致），不再与「没有返回值」混为 `VoidLangValue`。
5. **集合直接打印（2026-10-10 修复）**：原先输出 `System.Collections.Generic.List`1[System.Object]`
   这类 .NET 类型名，现与 `.ToStr()` 用同一套格式化，输出 `[1, 2, 3]` / `{"a": 1}` / `(1, 2, 3)`。
6. **元组（2026-10-10 修复）**：`.Count()` 已可用；顺带修好原生 `List<object?>` / `object[]` /
   `string` / 元组的**负索引**（`l[-1]` 原先直接把 -1 交给 `IList` 索引器而抛
   `ArgumentOutOfRangeException`）。
7. **原生函数「无返回值」与 `null`（2026-10-10 修复）**：无返回值的原生/内置函数
   （`Sleep`、`PrintLine`、并发原语等）现在统一归一化成 `VoidLangValue`，
   因此 `r <- Sleep(1)` 后 `r == null` 为 `false`（与解释器一致）。
   判定依据是 IL 侧声明的返回类型（`GetReturnTypeInternal` 返回 `typeof(void)`），不另设清单。
8. **运行期错误位置（2026-10-10 修复）**：编译期现在把 AST 节点的位置写进每条指令
   （`Instruction.LineNumber/ColumnNumber/SourceFile`），运行期错误会带行列与源码上下文。
   同时整数除/模零改为语言层的 `ZERO_DIVISION_ERROR`（原先直接抛 .NET `DivideByZeroException`，
   既没有位置信息也不可被语言层 `try/catch` 识别）。
9. Python / JavaScript / C# DLL 三类 `extern` 未验证；C/C++ P/Invoke 已可用。
10. **调试与性能工具现状（2026-11-27 更新）**：虚拟机调试现已通过 `VmDapServer` 与 `Old8Lang.App --dap`（标准 DAP 协议，含 VS Code 集成）提供；旧的 `VMProfiler` 与字节码 `Disassembler` 仍无命令行入口（解释器侧保留交互式 `debug-start` 与 `profile start`）。
11. **Thread 实例方法自 2026-10-10 起可用**：`Then` / `WithTimeout` / `Retry` / `Cancel` 注册在
    `VMThreadLangValue` 上（VM 的线程对象是整数线程 id + `Concurrency.ResourceManager`，
    与解释器的 `ThreadLangValue` 不同构）。语义与解释器对齐，但有两条协作式限制：
    `Cancel()` 只对尚未 `Start()` 的线程生效（启动后无法中断，解释器同样如此）；
    `Retry(n)` 只能用于 `spawn(...)` 创建的线程（需要重新执行原始函数）。
    顺带修复：`PrintLine(线程)` 不再栈溢出（`VMThreadLangValue` 补了 `ToString/ToDisplayString`）。
12. **数组排序自 2026-10-10 起可用**：`Array.QuickSort`/`HeapSort`/`SelectionSort`/`InsertionSort`/
    `MergeSort`/`BubbleSort` 以及通用 `Sort()` / `IsSorted()` 在 `object?[]` 上已实现
    （字符串按字典序排序，与解释器 `StringLangValue.Less` 的 `CompareOrdinal` 语义一致）。
13. **Task 实例方法自 2026-10-10 起可用**：`Then` / `Catch` / `Finally` / `ContinueWith` 在
    `TaskLangValue` 上已实现；continuation 会切到 worker 虚拟机执行（`VMContext.CurrentVM` 是
    `[ThreadStatic]`，不能在线程池线程上复用当前 VM 的求值栈）。

---

## 测试覆盖情况

| 套件 | 2026-11-28 实测结果 | 说明 |
|------|--------------------|------|
| `Old8Lang.Tests` 全量默认档 | **7,297 通过 / 0 失败 / 3 跳过**（总计 7,300，耗时 45 s） | 覆盖解释器、IL、VM、Parser、LSP 与 Architecture，无挂起 |
| `Old8Lang.Tests.Architecture` | **73 通过 / 0 失败 / 0 跳过** | 程序集边界、宿主模块隔离与语法节点规范 |
| `IlAsyncGeneratorStreamTests`（`performance.runsettings` 性能档） | **1 通过 / 0 失败 / 0 跳过**（248 ms） | 1,000 挂起流 + 300 并发编译期异步生成器无线程池阻塞 |
| `SpawnTests`（`performance.runsettings` 性能档） | **13 通过 / 0 失败 / 0 跳过** | 含线程 `Sleep` 的并发用例分档隔离 |
| `TestFiles/run_syntax_tests.sh` | **90 通过 / 0 失败** | 全部语法正反例 |
| `TestFiles/run_interpreter_tests.sh` | **188 通过 / 0 失败** | 全部解释器脚本用例 |
| `TestFiles/run_compiler_tests.sh` | **154 通过 / 0 失败** | 全部 IL 编译器脚本用例 |
| `TestFiles/run_vm_tests.sh` | **80 通过 / 0 失败** | 全部 VM 与字节码脚本用例 |

测试覆盖待复核事项见 [统一待办清单](../changes/Todo.md)。

## 推荐使用策略

| 场景 | 推荐模式 | 理由 |
|------|---------|------|
| 开发调试、快速原型 | 解释器 `-f` | 启动最快，动态特性最完整 |
| 性能敏感的生产环境 | IL 模式 `-il` | 运行性能最高，类型安全 |
| 跨平台分发、序列化、调试工具 | 虚拟机 `-vm` | 字节码可序列化，带反汇编器与性能分析器 |

## 相关文档

- [语法文档](./Old8Lang_Grammar.md) - 各语法的完整说明与模式标记
- [EBNF 规范](./Old8Lang.ebnf) - 形式化语法定义
- [API 模式实现对比](./API_Mode_Comparison.md) - 三种模式的内部实现机制差异
- [API 参考](./API_REFERENCE.md) - 全局函数、实例方法与标准库模块
- [开发路线图](./ROADMAP.md) - 方向性重点与各领域待办入口
- [开发流程](./DEVELOPMENT_WORKFLOW.md) - 新语法如何依次落进三种模式

## 更新日志

- **2026-10-10（续 2）**: 按「虚拟机模式缺口」逐条实测并修复 9 处：
  异步生成器含 `await`（改成同步恢复，`async for-in` 支持多标识符解构）、元组 `.Count()`、
  集合直接打印、原生函数「无返回值 vs `null`」、`await` 结果与返回类型注解（含异步结果无损转换）、
  `TypeLangValue` 实例方法整族、`TypeOf`/`GetType` 的内建类型与非类实例、运行期错误源码位置
  （并新增原生容器负索引、除零错误改为 `ZERO_DIVISION_ERROR`）、`TaskCompletionSource` /
  `CancellationTokenSource` 类形式。其中三处是**文档纠正**：异步生成器的缺口只在「体内含 `await`」；
  反射不是「固定返回 false」而是「整族抛错」；`TypeOf`/`GetType` 原文档未记录限制。
  测试报告：`archive/Reports/2026-10-10-23-40-虚拟机模式缺口修复测试.md`。
- **2026-10-10（续）**: 虚拟机模式补上 Thread 的 `Then` / `WithTimeout` / `Retry` / `Cancel`
  （注册在 `VMThreadLangValue` 上，`ResourceManager`/`VMThreadWrapper` 增加取消、超时等待与 spawn 负载记录），
  原「已知限制」第 11 条由「不可用」改为「可用 + 协作式限制」。**至此第 5 节清单的 14 个 VM 方法全部落地。**
  测试报告：`archive/Reports/2026-10-10-19-23-VM线程方法补齐测试.md`。
- **2026-10-10**: 实例方法跨模式补齐。IL 模式新增「函数值」桥接（lambda / 具名函数 → 委托 → 函数值），
  29 个此前显式抛 `NotSupportedException` 的高阶实例方法、扩展方法、`spawn` 与
  `Thread.Start/Join/IsAlive/Then/Cancel/Retry/WithTimeout` 全部可用；虚拟机模式新增
  数组 6 个排序（含 `Sort()` / `IsSorted()` 兜底）与 Task 的 `Then`/`Catch`/`Finally`/`ContinueWith`。
  VM 的 Thread 4 个实例方法确认为线程模型缺口（已知限制第 11 条）。
  实测报告：`archive/Reports/2026-10-10-19-03-实例方法跨模式补齐测试.md`。
- **2026-10-09（补）**: 核查「其他特性」表里的四项虚拟机专属能力，发现 `VMProfiler`、`VMDebugger`、
  字节码 `Disassembler` 三个类均无调用点（死代码），原表的 ✅ 系照抄类名而非验证可用性；
  真正的调试器与性能分析器都在解释器侧。已按实况修正并记入已知限制第 10 条。
- **2026-10-09**: 由 `MODE_COMPLETION_STATUS.md` 与 `Mode_Support_Summary.md` 合并而成。
  合并前对两份文档逐格 diff，实测裁决 7 处冲突单元格，并补齐 5 处旧标「未单独验证」的项。
  新增「运算符」「预编译指令」「内置函数」三节的合并来源标注与三种模式共同的限制一节。
- **2026-10-08（再续）**: 虚拟机模式支持静态类 API（Task / Thread / Assert）。
- **2026-10-08（续）**: 虚拟机模式支持函数装饰器；函数装饰器（虚拟机）标记 ❌ → ✅。
- **2026-10-08**: 按三种模式的实测结果全量复核支持矩阵，修正「标低了」与「标高了」两类错误。
- **2026-01-17**: 初始版本，基于 Old8Lang_Grammar.md 创建。
