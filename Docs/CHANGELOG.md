# 更新记录

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

#### 已有 LSP 功能
- **语义诊断 (Diagnostics)**: 实时语法错误和语义错误检查（已在之前版本实现）
- **自动补全 (Completion)**: 关键字、符号、代码片段、成员访问补全
- **跳转定义 (Go to Definition)**: 跳转到符号定义位置
- **查找引用 (Find References)**: 查找符号的所有引用
- **重命名 (Rename)**: 符号重命名
- **悬停提示 (Hover)**: 显示符号信息

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
- 编译器模式和解释器模式完全支持
  - 编译器模式：动态生成 P/Invoke 方法定义和委托类型
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
- 编译器模式和解释器模式完全支持
  - 编译器模式使用 IL 指令（`Newarr`、`Stelem`）在运行时创建数组
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
- 在编译器模式下提供编译时类型检查
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
- 编译器模式和解释器模式均完全支持

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
- **限制**: 仅解释器模式支持，编译器模式会抛出 NotImplementedException

### 系统优化

#### 2. 解释器模式增强
- 为数组类型添加 `Length()` 方法，与 `Count()` 方法等效
- 提升解释器模式与编译器模式的 API 一致性

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
  - 编译器模式测试：5 个测试验证 IL 代码生成和运行时行为
  - 所有测试在编译器模式和解释器模式下均通过
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
- 所有测试在编译器模式和解释器模式下均通过

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
- **注意**: 编译模式仍在测试和完善中，建议优先使用解释模式

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
