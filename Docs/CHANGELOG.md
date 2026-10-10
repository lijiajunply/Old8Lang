# 更新记录

本文档**只记录语法与 API 的变更**，且只做精简介绍。
实现细节、重构、测试调整、文档整理都**不在此列**——那些看 git log。
收录范围与写法见 [AGENTS.md 的 CHANGELOG 规则](../AGENTS.md#changelog-规则)。

各特性的模式支持现状见 [MODE_SUPPORT.md](./MODE_SUPPORT.md)，
完整语法见 [Old8Lang_Grammar.md](./Old8Lang_Grammar.md)，API 见 [API_REFERENCE.md](./API_REFERENCE.md)。

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
