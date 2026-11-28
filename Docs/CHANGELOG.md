# 更新记录

本文档**只记录语法与 API 的变更**，且只做精简介绍。
实现细节、重构、测试调整、文档整理都**不在此列**——那些看 git log。
收录范围与写法见 [AGENTS.md 的 CHANGELOG 规则](../AGENTS.md#changelog-规则)。

各特性的模式支持现状见 [MODE_SUPPORT.md](./MODE_SUPPORT.md)，
完整语法见 [Old8Lang_Grammar.md](./Old8Lang_Grammar.md)，API 见 [API_REFERENCE.md](./API_REFERENCE.md)。

---

## 2026-11-28

**语法 / 语义**

- 三种模式均支持步长范围 `[1~10:2]` 和字符范围 `['A'~'Z']`。
- 三种模式统一类字段可变集合初值为每实例独立（虚拟机模式下不同实例不再共享同一集合初值）。
- 三种模式均支持异步 Lambda、异步函数装饰器，以及三元及以上元组解构的 `match` 匹配。
- IL 模式支持在复合表达式内部（算术、调用实参、集合/字典/元组/模板字面量、短路逻辑与三元表达式）使用 `await`，支持对动态 `object` 操作数进行算术运算，且异步生成器的 `yield` 采用非阻塞挂起（暂停时不占用线程池线程）。
- 三种模式统一在变量赋值时递归校验带元素类型注解的集合（如 `list<int>`、`dict<string, int>`），并统一 `spawn` 线程 `Join()` 后的异常消息口径。

**API**

- 统一远程包管理协议为 `Old8Lang.PackageManager.Server`：新增 `old8lang search` 与 `old8lang registry` 命令（支持通过 `--registry`、`OLD8LANG_REGISTRY`、`o8package.json` 的 `registry` 字段或 `~/.old8lang/config.json` 切换到自建私有服务器），`old8lang publish` 支持直接上传包及签名到远程仓库（`--local-only` 保留仅本地打包），安装包自动解压到 `{packagesDir}/{packageId}@{version}/` 并支持读取 `o8package.json` 的 `main` 入口。
- 移除直接从 HTTP/HTTPS URL 导入模块（`import "https://..."`），统一改为通过 `old8lang install` 安装第三方包。
- `Type` / `type` 与 `TypeOf` 在三种模式下统一返回语言类型名（如 `Int`、`Double`、`String`、`List`、`Array`、`Dictionary`、`Null`），枚举成员值统一返回 `Int`。
- IL 与虚拟机模式补齐 `Math`/`Regex`/`Crypto`/`Csv`/`Time`/`Vector`/`Net` 等标准库的导入、别名、选择性导入与命名参数调用。

---

## 2026-11-27

**语法 / 语义**

- VM 模式支持用 `as` 将字典转换为本地或导入类的实例。字典中省略的字段保留类声明中的默认值。

**API**

- VM 模式下，`JsonSerialize` 及其别名可将类实例字段、嵌套集合和标量序列化为 JSON。
- `Old8Lang.App` 新增 `--dap` 模式，通过标准 Debug Adapter Protocol（stdio）提供虚拟机断点、条件断点、函数断点、按行单步、调用栈、局部/全局变量查看与表达式求值。
- Language Server 新增 Call Hierarchy、Type Hierarchy 与 Inline Values（支持通过 `old8lang/updateDebugSnapshot` 同步调试变量快照），成员补全按访问位置过滤私有/受保护成员。
- VS Code 扩展新增 `old8lang` 调试配置（`launch`）与「Old8Lang: 调试当前文件」命令。

---

## 2026-11-24

**语法 / 语义**

- 分组查询与查询延续修复了多处跨模式差异，包括空键分组和延续变量遮蔽外层变量。

---

## 2026-11-23

**语法 / 语义**

- IL 模式的异步函数现已支持在 `catch` 与 `finally` 块中使用 `await`，并支持含 `yield` 的异步生成器与 `async for-in` 迭代。
- 虚拟机模式对类的 `private` / `protected` 实例字段与静态字段补齐了运行期访问控制，外部读写私有字段现在会报 `ACCESS_VIOLATION_ERROR`（不再静默放行）。
- IL 模式的元组赋值可从数组或列表中解构元素，也支持运行时确定类型的右值。
- 三种模式的元组赋值支持嵌套模式，例如 `(a, (b, c)) <- (1, (2, 3))`。
- IL 查询中的连接结果可在后续过滤、投影和排序中使用对应行的内层变量与分组。

**API**

- 三种执行模式统一新增 `Task.Completed()`、`Task.WaitAll(tasks)` 与 `Task.WaitAny(tasks)` 静态方法。
- 虚拟机模式的数组和列表集合方法支持更多混合调用；数组专用排序方法对字符串按字典序排序。
- 虚拟机模式的原生全局函数支持按名称绑定参数，并校验重复、未知和缺失的参数。

---

## 2026-11-22

**语法 / 语义**

- IL 模式的异步函数现在可以读写顶层变量：`await` 前后对全局变量的读写与解释器、虚拟机一致。
- IL 模式的异步 `try/catch/finally` 组合不再导致进程崩溃；`return` 位于 `try` 中时会先执行
  `finally`，再完成任务。

**API**

- IL 模式的 `Task.Run` / `Task.StartNew` 会在线程池上真正调用传入的函数值并等待结果，
  不再返回空的已完成任务。

---

## 2026-11-15

**语法 / 语义**

- 数组与列表的 `+` 现在在三种模式下都按顺序拼接：数组 + 数组得到数组、列表 + 列表得到列表；
  混合数组与列表仍报「不支持加法」。
- 虚拟机补齐 `in` 运算符（数组/列表按值、字典按键、字符串按字符或子串），
  与解释器、IL 模式结果一致。
- 范围字面量的 `>` / `<` 只排除端点、不再翻转方向：端点相等且被排除时（如 `[5>~<5]`）
  三种模式都得到空数组。
- 数组、列表、字符串与字典支持 `.Length` 属性（等价 `len(x)`），三种模式一致；
  字典上键名为 `"Length"` 时用 `d["Length"]` 访问。
- `null` 的显示与 `null.ToStr()` 在三种模式下都输出小写 `null`（类型名仍是 `Null`）；
  IL 的字符串插值不再把 `null` 变成空串。

---

## 2026-11-14

**API**

- IL 模式的 `List` / `Dictionary` / `Array` 其余实例方法（如 `Chunk`、`Flatten`、`Slice`、`Pairwise`、
  `Aggregate`、`GroupBy`、`Zip`、`Clone`、`Merge`、`Permutations` 等）现在可以在集合字面量或原生集合接收者上调用，
  行为与解释器一致；列表与字典的增删改清、数组的 `Set` 会作用到原集合本身，别名可见修改。

---

## 2026-11-13

**语法 / 语义**

- 泛型 `class` 与 `mixin` 声明支持在类型参数列表后使用 `where` 子句声明约束，例如 `class Box<T> where T: int { ... }`；三种执行模式均可解析和执行已验证的约束类。

**API**

- .NET 宿主可通过 `Old8HostBuilder` 显式注册标准库模块并创建独立的模块目录；官方模块注册入口为
  `Old8Lang.Modules.Official.OfficialModules.RegisterAll(...)`。CLI 的现有命令和 Old8 `import` 名称不变。

---

## 2026-11-11

**语法 / 语义**

- IL 模式的具名函数、lambda 和类方法可以读写顶层变量（包括函数声明在变量之前的前向引用）；
  被函数引用的顶层变量会作为运行期全局槽位共享，函数内的同名参数、局部变量和字段仍按原有作用域遮蔽。
  异步函数尚未接入该模型。
- IL 模式的基础 LINQ 查询可按每个输入元素正确计算 `let`、`where` 和 `select`，并支持在投影前按多个键稳定排序。
- IL 模式的泛型类方法可返回引用自身或另一组类型参数的泛型类实例。
- IL 模式支持对列表 `FindAll` 的结果继续调用实例方法。
- IL 模式的泛型列表扩展方法可保留已验证的嵌套列表别名和返回接收者，并写回列表修改；部分泛型约束与集合类型仍待完善。
- 解释器与 IL 模式支持使用元组模式给变量赋值；IL 模式要求右侧具有元组类型。
- VM 模式支持通过模块名、模块别名或选择性导入调用已验证的 `Math` 标准库函数。

**API**

- IL 模式的 `Print` / `PrintLine` 会输出全部参数，布尔值和集合使用语言显示格式。
- IL 模式中捕获的异常可调用 `ToStr()` 查看错误信息。
- IL 模式的字符串 `Length()` / `Count()` 返回整数并可继续参与表达式。
- 解释器和 IL 模式支持通过 `Thread.CurrentThread` 和 `Thread.CurrentThread()` 获取实际执行线程。

---

## 2026-10-24

**语法 / 语义**

- **lambda 可写回外层局部变量**：闭包内对局部变量的赋值会传回外层，外层在闭包创建后重新赋值时闭包也能读到；
  解释器与虚拟机行为一致，也与其具名嵌套函数一致。
- **脚本顶层 C 风格 `for` 循环变量属于循环作用域**：循环体内创建的闭包捕获当轮的值（`012` 而非 `333`），
  循环结束后该变量不可见；解释器与虚拟机一致。
- **泛型类可以省略类型参数**：由构造函数实参推断（`Box(123)` 推断 `T = int`），并会按实际类型校验接口 /
  `new()` 等约束；可空类型参数 `T?` 接受具体值或 `null`。
- **同一模块的符号可以重复导入**：模块体只执行一次，再次导入返回其导出符号。
- **负索引在三模式统一**：数组 / 列表 / 字符串 / 元组都支持 `[-1]` 表示末尾元素，IL 模式同样生效；
  越界统一抛 `INDEX_ERROR`。
- **虚拟机支持数组解构赋值** `[a, b] <- 序列`（此前只有解释器支持）；右值可以是列表或数组，
  多余元素忽略。
- **虚拟机异步函数显式 `return null` 保留为 null**：任务结果显示为 `Task(Completed: Null)`，
  与解释器一致，不再与「没有返回值」混同。

**API**

- 虚拟机静态类补齐 `TestRunner`（`Describe` / `It` / `Test` / `RunTest` 可执行虚拟机里的函数值）、
  `Mock`（全部静态方法）与 `TaskScheduler.Default`。
- 虚拟机补齐 `Task.Factory.StartNew`、`Thread.CurrentThread`、`Task.Delay` 的 `CancellationToken` 参数；
  静态类方法可取值为函数后调用（`f <- Task.Delay; await f(10)`）。
- 虚拟机错误信息统一使用语言类型名（`List` / `Dict` / `Tuple` …），不再泄露 ``List`1`` / `Int32` 这类 .NET 名字。
- `-vm` 新增工具参数：`--disasm` 执行前反汇编、`--profile` 执行后打印性能报告、
  `--debug-session` + `--break <行号>` 启动交互式断点调试。

---

## 2026-10

**语法 / 语义**

- 扩展声明现在接受目标类型的泛型参数、声明级约束和 `where` 子句；当前这些参数仅参与语法解析，
  运行时仍按基础目标类型注册，约束不会自动校验。
- 执行模式的「编译模式」更名为 **IL 模式**：命令行为 `-il`，`-c` 保留为等价别名；
  `o8package.json` 的 `runtime` 取值由 `compiler` 改为 `il`，旧值继续识别。
- 虚拟机模式支持**函数装饰器**（`@decorator`）；**异步函数**的装饰器报 `VM_UNSUPPORTED_ERROR`。
- 虚拟机模式闭包改为**按引用捕获**：闭包可写回外层局部变量，外层在闭包创建后的赋值闭包也能读到。
- 虚拟机模式字典遍历语义与顺序对齐解释器 / IL：单变量绑定到 `(键, 值)` 元组，按插入顺序。
- `spawn` 明确为「只创建线程，**必须再调用 `Start()`** 才会执行」；`Join()` 可取回线程函数的返回值。
- **`super.method(args)` 修正**：此前任何 `super.xxx(...)` 都被当成父类构造函数调用（只查 `init`），
  于是 `super.speak()` 会去调父类的 `init` 或静默返回 `null`；现在只有 `super.init(...)` /
  `super.父类名(...)` 走构造函数，其余按普通父类方法调用并返回其结果。
- 线程组合方法修正：`t.Then(f)` 会自动启动 continuation 返回的线程；`t.WithTimeout(ms)` 现在真的会超时
  （超时抛 `TimeoutException`），此前只是启动定时器却仍然无限等待。

**API**

- 虚拟机模式新增 `TaskCompletionSource()` 与 `CancellationTokenSource()`（含 `(timeout)` 重载）
  的类形式：前者支持 `SetResult` / `SetException` / `SetCanceled` / `TrySet*` 与 `.Task`，
  后者支持 `Cancel` / `CancelAfter` / `Dispose` / `IsCancellationRequested`。
- **`GetType` 支持内建类型名**（`int` / `double` / `bool` / `char` / `string` / `null` /
  `array` / `list` / `dict` / `tuple`），取到的类型值可用于 `.IsPrimitive()`；
  **`TypeOf` 支持任意值**（非类实例返回 `Int` / `List` / `Dictionary` / `Tuple` 等类型名，
  错误值返回异常类名如 `ZeroDivisionError`）。
- 类型值现在可与同名的类型值或字符串比较（`GetType("A") == GetType("A")`）。
- 虚拟机模式下 `TypeLangValue` 的 9 个实例方法（`IsClass` / `IsInterface` / `IsPrimitive` /
  `IsGeneric` / `IsAssignableFrom` / `GetBaseType` / `GetInterfaces` / `GetMethodNames` /
  `GetFieldNames`）全部可用，此前整族抛 `ArgumentException`。
- **跨模式行为差异收敛（虚拟机模式）**：集合直接打印与 `.ToStr()` 一致；元组 `.Count()` 可用；
  原生容器支持负索引；无返回值的原生函数归一化为 `VoidLangValue`（`r == null` 为 `false`）；
  `await` 的结果可直接满足 `-> int` / `-> string` / `-> list` 等返回类型注解；
  运行期错误带源码行列与上下文，整数除/模零改抛语言错误 `ZERO_DIVISION_ERROR`；
  含 `await` 的异步生成器与 `async for k, v in ...` 多标识符解构均可用。
- 虚拟机模式支持静态类 API：`Task.Delay` / `WhenAll` / `WhenAny` / `FromResult` / `FromException` /
  `Run` / `StartNew`，`Thread.Sleep`，以及 `Assert` 的 24 个断言方法。
- 虚拟机模式下断言失败改抛 `AssertionError`（语言层 `try/catch` **可以**捕获；
  解释器下抛普通异常，捕获不到——这是有意的跨模式差异）。
- **IL 模式支持高阶实例方法**：列表的 `FindAll` / `FlatMap` / `GroupAdjacentBy` / `Partition` /
  `Single` / `SingleOrDefault` / `SkipWhile(Indexed)` / `TakeWhile(Indexed)`，字典的
  `Map` / `Filter` / `ForEach`，以及 `SelectMany` / `SortBy` / `Zip3` / `Sum(selector)` /
  `Average(selector)` / `Max(selector)` / `Min(selector)` / `Array.GroupAdjacentBy` 不再抛
  `NotSupportedException`（lambda 需带参数类型注解，且仍不支持读写外层局部变量）。
- **IL 模式支持扩展方法与线程**：`extension` 声明的方法可在 IL 下调用；`spawn(f)` 会创建真实线程，
  `Thread` 的 `Start` / `Join` / `IsAlive` / `Then` / `Cancel` / `Retry` / `WithTimeout` 可用
  （`Retry` 写作 `spawn(f).Retry(n)`）。
- **虚拟机模式支持数组排序与 Task 实例方法**：`Array` 的 `QuickSort` / `HeapSort` /
  `SelectionSort` / `InsertionSort` / `MergeSort` / `BubbleSort` 及通用 `Sort()` / `IsSorted()`；
  Task 的 `Then` / `Catch` / `Finally` / `ContinueWith`。字符串排序统一按长度（与解释器一致）。
  虚拟机模式下 `Thread` 的 `Then` / `WithTimeout` / `Retry` / `Cancel` 同日起可用
  （`Cancel` 只对尚未 `Start()` 的线程生效；`Retry` 只能用于 `spawn(...)` 创建的线程）。
- **移除 `Image` 标准库**：图像读写与处理（加载/保存、缩放、裁剪、旋转、翻转、滤镜、绘制等）
  已整体删除，`import "Image"` 不再可用。

---

## 1.0.0 rc8

**语法**

- **运算符重载**（Python 风格）：`_add` `_sub` `_mul` `_div` `_mod` `_pow`、比较运算 `_eq` `_lt` `_gt` `_le` `_ge`、
  索引 `_getitem` / `_setitem`；`!=` 由 `_eq` 取反自动实现。
- **函数装饰器**：`@decorator`、`@decorator(args)`、多层堆叠（自下而上应用）。
- **结构化文档注释**：`///` 支持 Google / Sphinx / JavaDoc / 中文四种风格。

**API**

- `extern` 新增 `dotnetdll:` 前缀，区分 .NET 托管 DLL 与 C/C++ 非托管 DLL。
- `extern "C#:程序集名" 类名 { ... }` 导入程序集静态方法（`C#:` / `cs:` / `csharp:` 前缀）。
- 全局函数（60+ 个）与原生函数（.NET 方法 / P/Invoke / Python）均支持**命名参数**。
- 新增 `GetFunctionInfo(...)` 查询函数的参数信息。
- **反射**：类型信息查询、动态方法调用与字段访问（含 private 成员）、动态实例创建、类型检查。

---

## 1.0.0 rc7

**语法**

- `native` 与 `native extern` 统一为 **`extern`**（旧写法需替换）。
- 一元 `!` 运算符支持前缀与嵌套（`!val`、`!!a`、`!(10 > 5)`）。

---

## 1.0.0 rc6

**API**

- 语言服务器新增：文档符号、签名帮助、代码格式化、代码操作、文档链接。

---

## 1.0.0 rc5

**语法**

- **`extern` 原生函数导入（P/Invoke）**：三种调用约定 `cdecl`（默认）/ `stdcall` / `winapi`、
  函数别名、块级批量导入、按函数指定调用约定。

---

## 1.0.0 rc4

**语法**

- `params` 可变参数：必须是最后一个参数、必须声明为数组类型（`array<T>`）。
- 泛型集合类型注解：`list<T>`、`array<T>`、`dict<K,V>`，支持嵌套；IL 模式下做编译期元素类型检查。
- `///` 结构化文档注释。
- **`using` 语句**：自动资源管理，块结束时调用对应 `Dispose`（两种形式：`using v <- 资源` 与 `using 已有变量`）。
- **`select` 语句**：Go 风格 Channel 多路选择（`case ch <- value ->`、`case value from ch ->`、`default ->`）。
- 并发原语由 `Async` 标准库迁为**内置全局函数**，共 8 类 57 个，无需导入。

**API**

- 数组新增 `Length()` 方法（与 `Count()` 等效）。

---

## 1.0.0 rc3

**语法**

- **枚举** `enum`：自动递增、显式值、混合使用。
- **Mixin、抽象类、接口**。
- **泛型**：泛型函数与泛型类、约束（`&` 组合）、`where` 子句、可空泛型类型参数（`<T?>`）。
- **类型注解**：联合类型 `A | B`、交叉类型 `A & B`、可空类型 `T?`。
- **表达式**：三元表达式、`match` 模式匹配（值匹配、变量绑定、通配符 `_`、元组解构、
  类型匹配 `case x:int`、守卫条件 `if`、范围匹配 `[0~12]`）、`is`、`in`。
- **生成器与异步**：`yield`、异步流 `async for-in`、异步生成器；`spawn`、`async`/`await`、`lock`。
- 列表声明语法改为 `{...}`，与字典 `{"key": value}` 风格统一。
- `this` / `super` 关键字。

**API**

- 新增标准库：机器学习、序列化、数据库、图像、模板引擎、网络。
- 包管理：`o8package.json` 项目文件、第三方库目录 `~/.old8lang/packages/`。

---

## 1.0.0 rc2

- 新增三元表达式、`break` / `continue`、`try-catch`。
- 列表声明语法一度改为 `list[...]`（后续版本改回 `{...}`）。

---

## 1.0.0 rc1

- 类声明调整、类型系统改进。

---

## 0.8.0（2024-10-04）

- 新增 JSON 操作与基本方法。
- 用反射支持自定义方法。
- 新增类型转换。
- 缩进解析改为大括号块。

---

## 0.2.0 / 0.3.0

- 支持字典、列表、数组、元组（当时仅二元）。

---

## 0.1.0

- 原生函数与引用语句：`import <context>`、`[import "<dll>" <class> <method> <nativemethod>]`。
