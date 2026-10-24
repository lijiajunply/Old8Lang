# Old8Lang 三种模式支持矩阵

**最后更新**: 2026-10-10

本文档是 Old8Lang 三种执行模式下**各特性可用性的唯一权威说明**。
原先分散在 `MODE_COMPLETION_STATUS.md` 与 `Mode_Support_Summary.md` 两份文档里，
2026-10-09 合并为本文件——两份表出现过互相矛盾的单元格，合并前已逐格实机复核。

语法本身的定义见 [Old8Lang_Grammar.md](./Old8Lang_Grammar.md) 与 [Old8Lang.ebnf](./Old8Lang.ebnf)；
三种模式的**内部实现机制**差异见 [API_Mode_Comparison.md](./API_Mode_Comparison.md)。

## 模式说明

- **解释器模式**（`-f`）：直接执行 AST，启动最快，动态特性最完整。
- **IL 模式**（`-il`，`-c` 为等价别名）：编译为 .NET IL 后执行，要求完整的类型注解。
- **虚拟机模式**（`-vm`）：编译为自定义字节码后由 VM 执行，支持字节码序列化与调试工具。

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
| 算术 (`+ - * / % ^`) | ✅ | ✅ | ✅ | |
| 比较 (`== != < > <= >=`) | ✅ | ✅ | ✅ | |
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
| 范围 `[1~10]` | ✅ | ✅ | ✅ | |
| 可空类型 `int?` | ✅ | ✅ | ✅ | |
| 泛型集合 `list<T>` 等 | ✅ | ✅ | ✅ | |
| 联合 / 交叉类型 | ✅ | ✅ | ✅ | `A \| B`、`A & B` |

### 4. 表达式

| 特性 | 解释器 | IL | 虚拟机 | 备注 |
|------|--------|-----|--------|------|
| 基本表达式 | ✅ | ✅ | ✅ | |
| 成员访问 / 索引访问 | ✅ | ✅ | ✅ | |
| 切片 `arr[1:3]` | ✅ | ✅ | ✅ | |
| 类型转换 `as` | ✅ | ✅ | ✅ | |
| 三元表达式 | ✅ | ✅ | ✅ | |
| 字符串模板 `$"..."` | ✅ | ✅ | ✅ | |
| Match 表达式 | ✅ | ✅ | ✅ | 语法为 `x <- match v { case p -> e ... }`，分支必须带 `case` 关键字 |
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
| `yield`（同步生成器） | ✅ | ✅ | ✅ | |

### 6. 函数

| 特性 | 解释器 | IL | 虚拟机 | 备注 |
|------|--------|-----|--------|------|
| 基本函数声明 | ✅ | ✅ | ✅ | `func name() { }`；`function` 不是关键字 |
| 参数 / 默认参数 / 可变参数 | ✅ | ✅ | ✅ | |
| 返回类型注解 | ✅ | ✅ | ✅ | IL 模式必需 |
| Lambda 表达式 | ✅ | ✅ | ✅ | |
| 高阶函数 | ✅ | ✅ | ✅ | |
| 闭包（只读捕获） | ✅ | ✅ | ✅ | |
| **闭包写回外层局部变量** | ✅ | ❌ | ✅ | 虚拟机自 2026-10-09 起由共享单元（`UpValueCell`）支持；解释器自 2026-10-24 起在“外层帧仍存活”时直接在外层帧上执行闭包。IL 的 lambda 仍不能读写外层局部变量 |
| 泛型函数 | ✅ | ❌ | ✅ | 虚拟机下必须显式给出类型参数（`id<int>(1)`） |
| 泛型约束 | ✅ | ❌ | ❌ | |
| 函数装饰器 | ✅ | ❌ | ⚠️ | 虚拟机下**普通函数**的装饰器可用；**异步函数**的装饰器报 `VM_UNSUPPORTED_ERROR`。IL 模式编译期报「返回类型不匹配」 |
| 访问修饰符 | ✅ | ✅ | ✅ | `public` / `private` / `static` |

### 7. 类和面向对象

| 特性 | 解释器 | IL | 虚拟机 | 备注 |
|------|--------|-----|--------|------|
| 基本类声明 / 字段 / 方法 | ✅ | ✅ | ✅ | |
| 构造函数 (`init`) | ✅ | ✅ | ✅ | |
| 访问修饰符 | ✅ | ✅ | ✅ | |
| 类继承 (`extends`) | ✅ | ✅ | ✅ | |
| 接口 / 接口实现 | ✅ | ✅ | ✅ | `interface` / `implements` |
| Mixin (`mixin` / `with`) | ✅ | ✅ | ✅ | |
| 泛型类 | ✅ | ❌ | ✅ | 可显式写 `Box<int>()`，解释器也支持由构造函数实参推断（`Box(123)`）；**`new` 关键字不存在** |
| `this` / `super` | ✅ | ✅ | ✅ | |
| 枚举 | ✅ | ✅ | ✅ | 含显式值、混合值、成员访问 |

### 8. 异步编程

| 特性 | 解释器 | IL | 虚拟机 | 备注 |
|------|--------|-----|--------|------|
| `async` / `await` | ✅ | ⚠️ | ✅ | |
| 异步生成器 | ✅ | ❌ | ✅ | 体内含 `await` 同样可用（2026-10-10 修复，见「已知限制」第 1 条） |
| `async for-in` | ✅ | ❌ | ✅ | 支持 `async for k, v in ...` 多标识符解构 |
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
| Thread API（静态类） | ✅ | ✅ | ✅ | 虚拟机 `Thread.Sleep` / `Thread.CurrentThread` 可用（2026-10-24），见「已知限制」第 3 条 |
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
| `import` 简单导入 | ✅ | ✅ | ✅ | |
| 命名导入 / 别名导入 / 通配符导入 | ✅ | ✅ | ✅ | |
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
| 反射函数与 `TypeLangValue` 实例方法 | ✅ | — | ✅ | 虚拟机下 9 个类型值方法自 2026-10-10 起可用；`TypeOf` / `GetType` 见 [API_REFERENCE.md](./API_REFERENCE.md#24-反射) |

### 15. 类型系统

| 特性 | 解释器 | IL | 虚拟机 | 备注 |
|------|--------|-----|--------|------|
| 动态类型 / 类型注解 / 类型推断 | ✅ | ✅ | ✅ | IL 模式要求完整注解 |
| 泛型集合 / 泛型函数 | ✅ | ⚠️ | ✅ | IL 模式不支持泛型类与泛型函数 |
| 泛型类 | ✅ | ❌ | ✅ | |
| 可空类型 / 联合类型 / 交叉类型 | ✅ | ✅ | ✅ | |
| 类型转换 (`as`) | ✅ | ✅ | ✅ | |

### 16. 其他特性

| 特性 | 解释器 | IL | 虚拟机 | 备注 |
|------|--------|-----|--------|------|
| LINQ 查询 | ✅ | ✅ | ✅ | |
| 断言 API（`Assert`） | ✅ | ⚠️ | ✅ | 虚拟机下 24 个方法均可用，失败抛 `AssertionError`（**语言层 `try/catch` 能捕获**；解释器抛的普通异常捕获不到） |
| 字节码序列化 | ❌ | ❌ | ✅ | `BytecodeFile` / `ConstantPool`；`-compile` 产出、`-execute` 载入，有 CLI 与测试覆盖 |
| 调试器 | ✅ | ❌ | ❌ | 解释器侧 `Old8Lang.Debugger.Debugger` + `DebuggableInterpreter`，入口是 `debug-start <文件>` |
| 性能分析器 | ✅ | ❌ | ❌ | 解释器侧 `ProfilerManager`，入口是交互式 `profile start` 命令 |
| 反汇编器 | ❌ | ❌ | ❌ | 字节码 `Disassembler` 类存在但**全仓库无调用点**，当前无法使用 |

---

## 已知限制

### 三种模式共同

1. **`new` 关键字不存在**：类实例化直接写 `ClassName()` 或 `Box<int>()`。
2. **一元运算符重载**不支持。
3. **`spawn` 必须配合 `Start()`**：只创建不启动时线程函数不会执行，未启动就 `Join()` 报 `Thread has not been started`。
4. **变量声明用 `<-`**，函数关键字是 `func`（不是 `function`），注释用 `//`（不是 `#`）。

### 解释器模式

1. 性能低于 IL 模式与虚拟机模式，缺少编译时优化。

### IL 模式

1. **类型注解要求**：函数参数与返回值必须有类型注解或默认值。lambda 也必须有参数类型注解，
   否则生成的委托形参是 `object`，参与算术/比较会失败。
2. 不支持泛型函数、泛型类、运算符重载、装饰器。
3. 不支持 C/C++ P/Invoke、Python 互操作与托管库导入。
4. 异步功能支持不完整。
5. **本机环境下存在既有的 IL 校验失败**：`Common Language Runtime detected an invalid program`。
   **2026-10-09 实测**：`dotnet test Old8Lang.Tests --filter "FullyQualifiedName~Compiler"`
   → **737 失败 / 446 通过 / 25 跳过，共 1208 例**。
   与单个特性无关，IL 模式列的 ❌/⚠️ 需在可正常验证 IL 的环境中复核。
6. **高阶实例方法自 2026-10-10 起可用**：`List` 的 `FindAll`/`FlatMap`/`GroupAdjacentBy`/`Partition`/
   `Single`/`SingleOrDefault`/`SkipWhile(Indexed)`/`TakeWhile(Indexed)`、`Dictionary` 的
   `Map`/`Filter`/`ForEach`、泛型列表的 `SelectMany`/`SortBy`/`Zip3`/`Sum(selector)`/
   `Average(selector)`/`Max(selector)`/`Min(selector)`、`Array.GroupAdjacentBy` 在 IL 下都能真正执行
   （lambda / 具名函数会被编译成委托，运行期以「函数值」传给解释器语义的 helper）。
   用法限制：
   - lambda 必须有类型注解；
   - lambda 内部**不能读写外层普通局部变量**（可以按名字调用已定义的函数）；
   - **不能在方法调用结果上直接链式调用**（`l.FindAll(f).Count()`、`l.Count().ToStr()` 会生成无效 IL，
     既有缺陷，见 `Todo.md`），需要先赋值给变量。
7. **扩展方法自 2026-10-10 起可用**：`extension X { func f(...) }` 的声明在编译前的解释器预执行阶段
   注册，调用点按普通实例方法解析，扩展方法体仍由解释器执行（因此函数体内可以使用全局函数/变量）。
8. **线程自 2026-10-10 起可用**：`spawn(f)` 会真的创建线程对象，
   `Thread.Start` / `Join` / `IsAlive` / `Then` / `Cancel` / `Retry` / `WithTimeout` 都可用；
   `Retry` 必须写成 `spawn(f).Retry(n)`（解释器把 `Retry` 特判为「重新执行函数调用」）。
9. **顶层 C 风格 `for` 循环变量仍是全局变量**（2026-10-24 记录）：解释器与虚拟机已把循环变量
   收进循环作用域，IL 仍是全局变量——循环结束后仍可读、同名嵌套循环共用同一个变量。
   与第 5 条同属 IL 的全局/作用域模型问题，见 `Todo.md` §I。

### 虚拟机模式

1. **异步生成器与 `async for-in`（2026-10-10 修复）**：原先体内含 `await` 时报
   `STATE_ERROR` 求值栈为空（指令 `IteratorMoveNext`）。真实原因是恢复异步生成器时用了
   `await`：虚拟机的求值栈、调用栈与帧池都按线程存放，`await` 让线程池线程接管后，
   恢复出来的是那个线程上的空栈（`_callStack.Pop()` 也会抛 `Stack empty`）。
   现已改为同步恢复（生成器体内的 `await` 改为阻塞等待），并让异步生成器实现 `IEnumerable`
   以复用普通 for-in 的迭代协议；`async for k, v in ...` 多标识符解构同时可用。
   同步生成器（`yield`）不受影响；`yield` 集合/类实例也不再丢值。
2. **异步函数的装饰器不可用**：`@decorator async func` 报 `VM_UNSUPPORTED_ERROR`。
   普通函数的装饰器可用。根因不是虚拟机独有：装饰器必须返回「异步函数值」，而
   `async (x) -> ...` 这种异步 Lambda 目前解释器下也会抛 `NullReferenceException`
   （`Instance.Run`），三种模式都还没有异步函数值。等异步函数值落地后再处理。
3. **静态类 API 大部分可用**：编译期把 `类名.方法(...)` 改写成静态类原生调用，支持集合集中在
   `VmStaticClassRegistry`。可用：`Task.Delay`/`WhenAll`/`WhenAny`/`FromResult`/`FromException`/
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
10. **`VMProfiler` / `VMDebugger` / `Disassembler` 三个类是死代码**（2026-10-09 核查）：
    均在 `Old8Lang/Bytecode/` 下各自定义，但全仓库找不到任何调用点，
    因此虚拟机模式目前**没有**可用的性能分析器、调试器与反汇编器。
    可用的调试与性能分析工具都在解释器侧（见上表）。
11. **Thread 实例方法自 2026-10-10 起可用**：`Then` / `WithTimeout` / `Retry` / `Cancel` 注册在
    `VMThreadLangValue` 上（VM 的线程对象是整数线程 id + `Concurrency.ResourceManager`，
    与解释器的 `ThreadLangValue` 不同构）。语义与解释器对齐，但有两条协作式限制：
    `Cancel()` 只对尚未 `Start()` 的线程生效（启动后无法中断，解释器同样如此）；
    `Retry(n)` 只能用于 `spawn(...)` 创建的线程（需要重新执行原始函数）。
    顺带修复：`PrintLine(线程)` 不再栈溢出（`VMThreadLangValue` 补了 `ToString/ToDisplayString`）。
12. **数组排序自 2026-10-10 起可用**：`Array.QuickSort`/`HeapSort`/`SelectionSort`/`InsertionSort`/
    `MergeSort`/`BubbleSort` 以及通用 `Sort()` / `IsSorted()` 在 `object?[]` 上已实现
    （字符串按长度排序，与解释器 `StringLangValue.Less` 语义一致）。
13. **Task 实例方法自 2026-10-10 起可用**：`Then` / `Catch` / `Finally` / `ContinueWith` 在
    `TaskLangValue` 上已实现；continuation 会切到 worker 虚拟机执行（`VMContext.CurrentVM` 是
    `[ThreadStatic]`，不能在线程池线程上复用当前 VM 的求值栈）。

---

## 测试覆盖情况

| 模式 | 测试文件数 | 实测结果 |
|------|-----------|---------|
| 解释器 | 134（2026-10-08） | 通过 |
| IL | 138（2026-10-08） | **738 失败 / 2666 通过 / 28 跳过**（2026-10-10 全量档），见「已知限制」 |
| 虚拟机 | 97（2026-10-08） | 非 IL 模式全量 **4782 通过 / 0 失败 / 3 跳过**（2026-10-10） |

各模式测试文件的分类明细与覆盖缺口见 [Old8Lang.Tests/Compiler/TODO.md](../Old8Lang.Tests/Compiler/TODO.md)
与 [Old8Lang.Tests/Interpreter/TODO.md](../Old8Lang.Tests/Interpreter/TODO.md)。

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
  测试报告：`Reports/2026-10-10-23-40-虚拟机模式缺口修复测试.md`。
- **2026-10-10（续）**: 虚拟机模式补上 Thread 的 `Then` / `WithTimeout` / `Retry` / `Cancel`
  （注册在 `VMThreadLangValue` 上，`ResourceManager`/`VMThreadWrapper` 增加取消、超时等待与 spawn 负载记录），
  原「已知限制」第 11 条由「不可用」改为「可用 + 协作式限制」。**至此第 5 节清单的 14 个 VM 方法全部落地。**
  测试报告：`Reports/2026-10-10-19-23-VM线程方法补齐测试.md`。
- **2026-10-10**: 实例方法跨模式补齐。IL 模式新增「函数值」桥接（lambda / 具名函数 → 委托 → 函数值），
  29 个此前显式抛 `NotSupportedException` 的高阶实例方法、扩展方法、`spawn` 与
  `Thread.Start/Join/IsAlive/Then/Cancel/Retry/WithTimeout` 全部可用；虚拟机模式新增
  数组 6 个排序（含 `Sort()` / `IsSorted()` 兜底）与 Task 的 `Then`/`Catch`/`Finally`/`ContinueWith`。
  VM 的 Thread 4 个实例方法确认为线程模型缺口（已知限制第 11 条）。
  实测报告：`Reports/2026-10-10-19-03-实例方法跨模式补齐测试.md`。
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
