# Old8Lang 语法文档

## 1. 概述

Old8Lang 是一种动态类型的编程语言，具有类似 C#/Java 的语法结构，同时支持解释执行和编译执行。本文档详细介绍了 Old8Lang 的语法规则，包括词法、语法和语义。

## 2. 词法规则

### 2.1 标识符

标识符用于表示变量名、函数名、类名等，规则如下：
- 必须以字母或下划线开头
- 可以包含字母、数字和下划线
- 区分大小写

### 2.2 关键字

以下是 Old8Lang 的关键字：

```
if elif else for while switch case default func class return try catch finally import and or xor not true false in as throw async await mixin
```

### 2.3 字面量

#### 2.3.1 整数字面量

整数字面量用于表示整数值，例如：
```
123
-456
0
```

#### 2.3.2 浮点数字面量

浮点数字面量用于表示浮点数值，例如：
```
3.14
-0.5
1.0
```

#### 2.3.3 字符串字面量

字符串字面量用于表示字符串，使用双引号包围，例如：
```
"hello world"
""
```

#### 2.3.4 字符字面量

字符字面量用于表示单个字符，使用单引号包围，例如：
```
'a'
'1'
'\n'
```

#### 2.3.5 布尔字面量

布尔字面量用于表示布尔值，只有两个值：
```
true
false
```

## 3. 数据类型

### 3.1 基本数据类型

| 类型名   | 描述           | 示例               |
|---------|---------------|-------------------|
| int     | 整数类型       | `123`             |
| double  | 浮点数类型     | `3.14`            |
| string  | 字符串类型     | `"hello"`         |
| bool    | 布尔类型       | `true`            |
| char    | 字符类型       | `'a'`             |

### 3.2 复合数据类型

#### 3.2.1 数组

数组是一种有序集合，可以包含任意类型的元素，使用方括号表示：

```
[1, 2, 3, 4, 5]
["a", "b", "c"]
```

#### 3.2.2 列表

列表是一种动态数组，可以随时添加或删除元素。使用花括号 `{}` 表示：

```
{1, 2, 3}
{"a", "b", "c"}
{}  // 空列表
```

#### 3.2.3 字典

字典是一种键值对集合，使用花括号和冒号表示。通过第一个元素是否包含冒号来区分列表和字典：

```
{"name": "Alice", "age": 30}
{1: "one", 2: "two"}
```

#### 3.2.4 元组

元组是一种固定长度的有序集合，可以包含不同类型的元素：

```
(1, "a", true) // 会被解析成嵌套元组 (1, ("a", true))
("hello", 3.14)
// 不支持 (1,) 这种
```

#### 3.2.5 范围

范围用于表示一个数值范围，使用波浪号表示：

```
[1~10]
[0~5]
```

## 4. 表达式

### 4.1 算术表达式

Old8Lang 支持以下算术运算符：

| 运算符 | 描述 | 示例 |
|-------|------|------|
| +     | 加法 | `a + b` |
| -     | 减法 | `a - b` |
| *     | 乘法 | `a * b` |
| /     | 除法 | `a / b` |
| %     | 取模 | `a % b` |
| ^     | 幂运算 | `a ^ b` |

### 4.2 比较表达式

Old8Lang 支持以下比较运算符：

| 运算符 | 描述 | 示例 |
|-------|------|------|
| ==    | 等于 | `a == b` |
| !=    | 不等于 | `a != b` |
| <     | 小于 | `a < b` |
| >     | 大于 | `a > b` |
| <=    | 小于等于 | `a <= b` |
| >=    | 大于等于 | `a >= b` |

### 4.3 逻辑表达式

Old8Lang 支持以下逻辑运算符：

| 运算符 | 描述 | 示例 |
|-------|------|------|
| and   | 逻辑与 | `a and b` |
| or    | 逻辑或 | `a or b` |
| xor   | 逻辑异或 | `a xor b` |
| not   | 逻辑非 | `not a` |

### 4.5 成员访问表达式

使用点号 `.` 访问对象的成员：

```
obj.field
obj.method()
```

### 4.6 索引表达式

使用方括号 `[]` 访问数组、列表或字典的元素：

```
array[0]
myList[1]  // 访问列表元素
dict["key"]
```

### 4.7 函数调用表达式

使用圆括号 `()` 调用函数：

```
func()
func(a, b)
```

### 4.8 成员访问表达式

使用点号 `.` 访问对象的成员：

```
obj.field
obj.method()
```

### 4.9 三元表达式

使用问号 `?` 和冒号 `:` 表示条件表达式：

```
a <- 10
b <- 20
c <- a > b ? a : b
```

## 5. 语句

### 5.0 赋值语句

使用 `<-` 运算符进行赋值：

```
a <- 123
b <- a + 1
```

同时也支持给数组、列表、字典和类属性赋值：

```
array[0] <- 100
myList[1] <- "new item"
dict["key"] <- 42
obj.field <- "new value"
```

Old8Lang **不支持链式赋值语句**（如 `a <- b <- 1`） 也不支持 **赋值表达式**。

### 5.1 块语句

块语句由一对花括号 `{}` 包围，包含多个语句：

```
{
    a <- 1
    b <- 2
    c <- a + b
}
```

### 5.2 变量声明与赋值

变量声明使用 `:` 语法指定类型（类型假注），赋值使用 `<-` 运算符：

```
a <- 123
b <- 3.14
c:string <- "hello" // 类型假注
```

### 5.3 控制流语句

#### 5.3.1 if-elif-else 语句

```
if condition {
    // 条件为真时执行
} elif condition2 {
    // 条件2为真时执行
} else {
    // 所有条件都为假时执行
}
```

#### 5.3.2 for 语句

```
for i <- 0, i < 10, i++ {
    // 循环体
}
```

#### 5.3.3 while 语句

```
while condition {
    // 循环体
}
```

#### 5.3.4 for-in 语句

```
for item in collection {
    // 循环体
}
```

在 for in 中，禁止更改循环变量的值。

#### 5.3.5 switch 语句

```
switch expression {
    case value1 {
        // 匹配 value1 时执行
    }
    case value2 {
        // 匹配 value2 时执行
    }
    default {
        // 无匹配时执行
    }
}
```

### 5.4 函数声明与调用

#### 5.4.1 函数声明

```
func add(a:int, b:int) {
    return a + b
}
// 或者这样
add(a:int, b:int) -> {
    return a + b
}

// 也可以给 func 添加返回值类型假注
func add(a:int, b:int) -> int {
    return a + b
}

add:int (a:int, b:int) -> {
    return a + b
}
```

#### 5.4.2 编译模式类型注解要求

在编译模式下（使用 `-c` 选项），函数声明有额外要求：

1. **参数类型要求**：所有函数参数必须满足以下之一
   - 显式声明类型注解：`param:int`
   - 提供默认值以推断类型：`param: 123`
2. **返回类型注解是强制的**：函数返回类型必须显式声明，不能通过return推断
3. **Lambda参数类型注解是强制的**：Lambda表达式的参数必须有类型注解（不支持默认参数）
4. **Lambda返回类型可以推断**：Lambda的返回类型可以从表达式或return语句推断

| 特性 | 解释器模式 | 编译模式 |
|------|----------|---------|
| 函数参数类型注解 | 可选 | **必须**（或有默认值） |
| 函数参数默认值推断 | 支持 | **支持** |
| 函数返回类型注解 | 可选（可推断） | **必须** |
| Lambda参数类型注解 | 可选 | **必须** |
| Lambda返回类型注解 | 可选（可推断） | 可选（可推断） |

正确示例（编译模式）：

```old8
// 方式1：完整类型注解的函数
func add(a:int, b:int) -> int {
    return a + b
}

// 方式2：使用默认值推断参数类型
func greet(name:string, message: "Hello") -> void {
    PrintLine(message + ", " + name)
}

// 方式3：混合使用类型注解和默认值
func calculate(x:int, y: 0, operation: "add") -> int {
    if operation == "add" {
        return x + y
    } else {
        return x * y
    }
}

// void返回类型
func printMessage(msg:string) -> void {
    PrintLine(msg)
}

// Lambda参数有类型，返回类型可推断
multiply <- (a:int, b:int) -> a * b
```

错误示例（编译模式）：

```old8
// 错误：缺少返回类型
func calculate(x:int, y:int) {
    return x + y
}

// 错误：参数既没有类型也没有默认值
func add(a, b) -> int {
    return a + b
}

// 错误：Lambda参数缺少类型
transform <- (x) -> x * 2
```

**默认参数类型推断说明**：

当参数提供默认值时，编译器会从默认值自动推断参数类型：

```old8
func example(
    intParam: 123,           // 推断为 int
    doubleParam: 3.14,       // 推断为 double
    stringParam: "text",     // 推断为 string
    boolParam: true          // 推断为 bool
) -> void {
    // 函数体
}
```

注意：
- 默认参数推断在**验证阶段**生效，能通过编译模式的类型检查
- 由于编译器IL生成的已知限制，带默认参数的函数在编译模式下可能遇到运行时问题
- 解释器模式下默认参数完全正常工作

#### 5.4.3 函数调用

```
result <- add(1, 2)
```

#### 5.4.4 Lambda 表达式

```
add <- (a:int, b:int) -> a + b
```

#### 5.4.5 异步函数

异步函数允许在不阻塞主线程的情况下执行耗时操作，使用 `async func` 关键字定义：

```old8
// 基本异步函数定义
async func asyncAdd(a:int, b:int) -> int {
    return a + b
}

// 异步函数另一种语法
async asyncAdd2(a:int, b:int) -> int {
    return a + b
}

// 带有延迟的异步函数
async func delayedHello() -> string {
    await Task.Delay(1000)
    return "Hello after delay"
}
```

异步函数的特点：
- 内部可以使用 `await` 关键字等待其他异步操作
- 返回值会自动包装为 `Task` 对象
- 调用异步函数会立即返回 `Task` 对象，不会阻塞当前线程
- 通过 `await` 可以获取异步函数的返回结果

#### 5.4.6 await 表达式

`await` 关键字用于等待异步操作完成，只能在异步函数内部使用：

```old8
async func asyncOperation() {
    // 等待一个异步函数完成
    result <- await asyncAdd(1, 2)
    
    // 等待 Task.Delay 完成
    await Task.Delay(500)
    
    // 等待多个任务完成
    results <- await Task.WhenAll([asyncAdd(1, 2), asyncAdd(3, 4), asyncAdd(5, 6)])
    
    // 等待第一个完成的任务
    firstResult <- await Task.WhenAny([asyncAdd(1, 2), delayedHello()])
}
```

#### 5.4.7 异步流与异步迭代

Old8Lang 支持异步流（Async Stream）和异步迭代，类似于 C# 的 `IAsyncEnumerable<T>`。异步流允许以异步方式生成和遍历数据序列。

**异步生成器函数**

使用 `async func` 和 `yield` 关键字创建异步生成器：

```old8
// 创建异步流生成器
async func createAsyncStream() {
    i <- 1
    while i <= 5 {
        await Task.Delay(100)  // 模拟异步操作
        yield i                 // 生成值
        i <- i + 1
    }
}

// 带参数的异步生成器
async func rangeAsync(start:int, end:int) {
    i <- start
    while i <= end {
        await Task.Delay(50)
        yield i
        i <- i + 1
    }
}
```

**async for-in 循环**

使用 `async for-in` 语法遍历异步流：

```old8
// 遍历异步流
async func processStream() {
    stream <- createAsyncStream()

    async for item in stream {
        PrintLine("Received: " + item.ToStr())
    }
}

// 带参数的异步流
stream <- rangeAsync(1, 10)
async for num in stream {
    PrintLine(num.ToStr())
}
```

**异步流的特点**：
- 异步生成器函数使用 `async func` 声明，内部包含 `yield` 语句
- 调用异步生成器函数会返回 `AsyncGeneratorLangValue` 对象
- 使用 `async for-in` 循环遍历异步流
- 支持 `break` 和 `continue` 语句
- 每次迭代会异步等待下一个值的产生
- 可以在异步生成器中使用 `await` 执行其他异步操作

**键值对迭代**：

```old8
async func keyValueStream() {
    yield ("key1", "value1")
    yield ("key2", "value2")
    yield ("key3", "value3")
}

stream <- keyValueStream()
async for key, value in stream {
    PrintLine(key + " = " + value)
}
```

### 5.5 类声明与实例化

#### 5.5.1 类声明

```
class Person {
    public name:string <- ""
    age:int <- 0
    
    func init(name:string, age:int) {
        this.name <- name
        this.age <- age
    }
    
    public func sayHello() {
        PrintLine("Hello, my name is " + name)
    }
}
```

标识符分别为 public、private、protected、static

#### 5.5.2 Mixin 声明

Mixin 是一种特殊的类，用于提供可重用的功能模块，可以被多个类使用。

```
mixin Logger {
    public func log(message) {
        PrintLine("[LOG] " + message)
    }
}

mixin Serializable {
    public func serialize() {
        return "Serialized object"
    }
}
```

#### 5.5.3 使用 Mixin

类可以通过 `with` 关键字应用一个或多个 mixin：

```
// 应用单个 mixin
class User extends BaseClass with Logger {
    func init(name) {
        this.name <- name
        log("User created: " + name)
    }
}

// 应用多个 mixin
class Product extends BaseClass with Logger, Serializable {
    public price <- 0.0
    
    func init(name, price) {
        this.name <- name
        this.price <- price
        log("Product created: " + name)
    }
}
```

#### 5.5.4 类实例化

```
p <- Person("Alice", 30)
p.sayHello()

t <- Test(1, 2)  // 调用带有参数的构造函数
```

### 5.6 多线程

Old8Lang 支持多线程编程，允许创建和管理线程执行并行任务：

#### 5.6.1 创建和启动线程

使用 `spawn` 函数创建并启动新线程：

```old8
// 定义线程函数
func threadMain() {
    PrintLine("Thread is running")
}

// 创建并启动线程
thread <- spawn(threadMain)

// 等待线程完成
thread.Join()

// 使用带参数的线程函数
func printNumber(num) {
    PrintLine("Number: " + num)
}

// 传递参数给线程函数
thread2 <- spawn(printNumber(42))
thread2.Join()
```

#### 5.6.2 线程的其他操作

```old8
// 获取当前线程
currentThread <- Thread.CurrentThread()

// 线程休眠
Thread.Sleep(1000)  // 休眠 1 秒

// 线程状态查询
if thread.IsAlive() {
    PrintLine("Thread is still running")
} else {
    PrintLine("Thread has completed")
}
```

#### 5.6.3 多线程同步

```old8
// 使用锁进行线程同步
lockObject <- Object()

counter <- 0

func incrementCounter() {
    for i <- 0, i < 1000, i++ {
        // 锁保证同一时刻只有一个线程执行锁内代码
        lock(lockObject) {
            counter <- counter + 1
        }
    }
}

// 创建多个线程执行增量操作
threads <- []
for i <- 0, i < 10, i++ {
    thread <- spawn(incrementCounter)
    threads.Add(thread)
}

// 等待所有线程完成
for thread in threads {
    thread.Join()
}

PrintLine("Final counter: " + counter)  // 应该输出 10000
```

#### 5.6.4 spawn 函数的特点

- `spawn` 函数用于创建并立即启动新线程
- 可以接受带参数的函数调用
- 返回值是一个 `Thread` 对象，可用于后续的线程管理
- 支持传递多个参数给线程函数
- 线程函数执行完后，线程自动终止

#### 5.6.5 多线程与异步的区别

| 特性 | 多线程 (spawn) | 异步 (async/await) |
|------|---------------|-------------------|
| 资源消耗 | 较高（创建线程开销） | 较低（基于任务调度） |
| 并发模型 | 并行执行 | 异步非阻塞 |
| 适用场景 | CPU密集型任务 | I/O密集型任务 |
| 调度 | 操作系统线程调度 | 任务调度器 |
| 上下文切换 | 较高开销 | 较低开销 |
| 编程复杂度 | 较高（需要手动同步） | 较低（编译器处理异步流程） |

### 5.7 异常处理

#### 5.7.1 try-catch-finally 语句

```
try {
    // 可能抛出异常的代码
} catch (e) {
    // 捕获所有类型的异常
} finally {
    // 无论是否异常都执行
}
```

#### 5.6.2 throw 语句

throw 语句用于显式抛出异常，可以抛出任意类型的表达式作为异常信息：

```
// 抛出字符串异常
throw "错误信息"

// 抛出数字异常
throw 123

// 抛出布尔值异常
throw true

// 抛出数组异常
throw [1, 2, 3]

// 抛出函数调用结果
throw getError()
```

throw 语句可以在任何地方使用，包括函数、循环、条件语句等。当执行到 throw 语句时，程序会立即停止当前执行路径，并将异常传递给最近的 try-catch 块处理：

```
try {
    throw "异常信息"
} catch (e) {
    PrintLine("捕获到异常: " + e)
}
```

### 5.7 导入语句

Old8Lang 支持多种导入语法，包括传统导入、命名导入和重命名导入：

#### 5.7.1 传统导入

```
import "module"
import module
```

#### 5.7.2 命名导入

从模块中导入指定的函数或变量：

```
import { func1, var1 } from "module"
import { func2, var2 } from module
```

#### 5.7.3 重命名导入

将导入的项重命名为指定名称：

```
import { A as B, C as D } from "module"
import { oldName as newName } from module
```

#### 5.7.4 混合导入

同时使用命名导入和重命名导入：

```
import { func3, var3 as v3 } from "module"
```

### 5.8 原生库导入语句

Old8Lang 支持导入原生 DLL 库中的类和方法，以便调用外部功能。原生库导入语句使用 `native` 关键字，支持以下几种形式：

#### 5.8.1 单个方法导入 (nativeStatement)

**语法规则**：`native "dllName" className methodName alias?`

**描述**：导入原生 DLL 中的特定方法，并可选地指定别名。

**示例**：

```
native "console.dll" console Write print
native "console.dll" console WriteLine printline
```

#### 5.8.2 批量导入所有方法

**语法规则**：`native "dllName" className *`

**描述**：导入原生 DLL 中某个类的所有公共静态方法。大大简化了标准库的引入。

**示例**：

```
// 导入 MathLib 的所有方法（53个方法只需1行）
native "Old8LangLib" MathLib *

// 导入后可直接使用所有方法
result <- Sqrt(16)     // 调用 Sqrt 方法
pi <- GetPi()          // 调用 GetPi 方法
sinVal <- Sin(1.57)    // 调用 Sin 方法
```

**优势**：
- 从 53 行导入语句减少到 1 行
- 自动导入类中的所有公共静态方法
- 避免遗漏方法
- 代码更简洁易读

#### 5.8.3 选择性导入多个方法

**语法规则**：`native "dllName" className { method1, method2, method3 }`

**描述**：按需导入原生 DLL 中某个类的特定方法，用逗号分隔多个方法名。

**示例**：

```
// 只导入 Time 类的两个方法
native "Old8LangLib" Time { GetTimeNow, TimeStamp }

// 导入后可以使用这两个方法
timeStr <- GetTimeNow("yyyy-MM-dd HH:mm:ss")
stamp <- TimeStamp()
```

**优势**：
- 按需导入，避免命名冲突
- 代码更明确，容易理解使用了哪些方法
- 减少不必要的方法导入

#### 5.8.4 静态类导入 (nativeStatic)

**语法规则**：`native "dllName" className -> alias`

**描述**：导入原生 DLL 中的静态类或静态方法集，并指定别名。

**示例**：

```
native "Math.dll" Math -> Math
```

#### 5.8.5 完整类导入 (nativeClass)

**语法规则**：`native "dllName" className`

**描述**：导入原生 DLL 中的完整类，包括所有方法和属性。

**示例**：

```
native "Person.dll" Person
```

#### 5.8.6 带别名的类导入 (nativeClass with alias)

**语法规则**：`native "dllName" className as alias`

**描述**：导入原生 DLL 中的完整类，并使用指定的别名进行注册。这使得类导入更加明确，并且可以避免命名冲突。

**示例**：

```
// 导入 Old8LangLib.Csv 类，并命名为 CsvUtil
native "Old8LangLib" Csv as CsvUtil

// 使用别名创建实例或访问类
csv <- CsvUtil
```

**优势**：

- 提高代码可读性，明确类的来源和用途
- 避免类名冲突（例如，当不同库中有同名类时）
- 使代码重构更容易（只需修改别名，不影响使用代码）
- 支持为长类名创建简短别名

**注意事项**：

- 使用别名后，只能通过别名访问该类，原始类名不可用
- 别名必须是有效的标识符
- 建议将所有导入语句（包括别名导入）放在文件开头

#### 原生库导入的作用

- 允许 Old8Lang 代码调用外部 C# DLL 中的功能
- 支持调用静态方法和实例方法
- 支持导入整个类或特定方法
- 支持批量导入和选择性导入
- 可用于扩展 Old8Lang 的功能，如访问系统 API、使用第三方库等

#### 使用建议

1. **批量导入** (`*`)：适用于需要使用类中大部分方法的场景，如标准库
2. **选择性导入** (`{ }`)：适用于只需要使用少数几个方法的场景，避免命名冲突
3. **单个方法导入**：适用于需要为方法指定别名的场景
4. **别名类导入** (`as`)：适用于避免类名冲突、提高代码可读性或为长类名创建简短别名的场景
5. **导入位置**：建议将所有导入语句放在文件开头，避免解析歧义

## 6. 类型系统

### 6.1 动态类型

Old8Lang 为动态类型语言，变量的类型可以在运行时改变：

```
a <- 123  // a 为 int 类型
a <- "hello"  // a 变为 string 类型
```

### 6.2 类型假注

可以使用 `:` 语法为变量添加类型假注，指定变量的预期类型：

```
a:int <- 123  // a 被标记为 int 类型
```

### 6.3 类型检查

如果进行了类型假注，就需要保持类型不变，否则会产生报错：

```
a:int <- 123  // 正确，a 为 int 类型
a <- 456  // 正确，仍然是 int 类型
a <- "hello"  // 错误，不能将 string 类型赋值给标记为 int 类型的变量
```

### 6.4 类型转换

使用 `as` 关键字进行类型转换：

```
a:int <- 123
b:double <- a as double
```

### 6.5 类型转换规则

Old8Lang 支持以下类型转换规则：

| 源类型 | 目标类型 | 规则 |
|--------|----------|------|
| int    | double   | 直接转换 |
| double | int      | 取整 |
| string | int/double | 解析数值 |
| int/double | string | 转换为字符串 |

## 7. 特殊语法

### 7.1 字符串模板

使用 `$""` 语法来创建字符串模板（C# 风格），在字符串内部支持 `{}` 占位符和 `{{}}` 转义：

```
name <- "Alice"
age <- 30
message <- $"My name is {name}, I'm {age} years old."
PrintLine(message)
```

1. `$(".")` - 基本字符串模板
2. `$"This is {10}"` - 在模板中嵌入表达式
3. `{{` - 转义 `{` 字符
4. `}}` - 转义 `}` 字符

例如：
```
// 基本使用
name <- "Alice"
result <- $"Hello, {name}"

// 转义大括号
escaped <- $"This is {{escaped}} bracket"

// 混合使用
mixed <- $"Name: {name}, Escaped: {{escaped}}"
```

### 7.2 列表推导式

```
numbers <- [1, 2, 3, 4, 5]
squares <- [x * x for x in numbers]
```

### 7.3 范围表达式

```
// 创建一个从 1 到 10 的范围
range <- [1~10]

// 使用范围进行循环
for i in range {
    PrintLine(i)
}
```

## 8. 示例代码

### 8.1 基本类型和表达式

```old8
// 整数类型
a:int <- 123

// 浮点数类型
b:double <- 3.14

// 字符串类型
c:string <- "hello world"

// 布尔类型
d:bool <- true
e:bool <- false

// 字符类型
f:char <- 'a'

// 数组类型
g:array <- [1, 2, 3, 4, 5]

// 字典类型
h:dictionary <- {"name": "Alice", "age": 30}

// 表达式
i <- a + 1
j <- b * 2
k <- c + "!"
l <- d and e
m <- a > b
```

### 8.2 控制流

```old8
// if-elif-else
if a > 100 {
    PrintLine("a 大于 100")
} elif a > 50 {
    PrintLine("a 大于 50 小于等于 100")
} else {
    PrintLine("a 小于等于 50")
}

// for 循环
for i <- 0, i < 10, i++ {
    PrintLine("i = " + i)
}

// while 循环
j <- 0
while j < 5 {
    PrintLine("j = " + j)
    j <- j + 1
}

// for-in 循环
for item in g {
    PrintLine("item = " + item)
}
```

### 8.3 函数和类

```old8
// 函数声明
func add(x:int, y:int) -> {
    return x + y
}

// 函数调用
result <- add(1, 2)
PrintLine("1 + 2 = " + result)

// 类声明
class Calculator {
    func multiply(x:int, y:int) -> {
        return x * y
    }
    
    func divide(x:double, y:double) -> {
        if y == 0 {
            PrintLine("除数不能为零")
            return 0.0
        }
        return x / y
    }
}

// 类实例化和方法调用
calc <- Calculator()
product <- calc.multiply(3, 4)
PrintLine("3 * 4 = " + product)

quotient <- calc.divide(10.0, 2.0)
PrintLine("10.0 / 2.0 = " + quotient)
```

### 8.4 异常处理

```old8
try {
    // 可能抛出异常的代码
    result <- 10 / 0
} catch (e) {
    PrintLine("捕获到异常: " + e)
} finally {
    PrintLine("无论是否异常，都会执行这里")
}
```

### 8.5 异步编程示例

```old8
// 基本异步函数
async func asyncAdd(a:int, b:int) -> int {
    return a + b
}

// 带有 await 的异步函数
async func fetchData() -> string {
    PrintLine("开始获取数据...")
    await Task.Delay(1500)
    PrintLine("数据获取完成")
    return "Hello, Async!"
}

// 主异步函数
async func main() {
    // 调用并等待异步函数
    result <- await asyncAdd(5, 3)
    PrintLine("5 + 3 = " + result)
    
    // 等待获取数据
    data <- await fetchData()
    PrintLine("获取到的数据: " + data)
    
    // 并行执行多个异步操作
    tasks <- [
        Task.Delay(1000).ThenTask(() -> "Task 1 done"),
        Task.Delay(500).ThenTask(() -> "Task 2 done"),
        Task.Delay(1500).ThenTask(() -> "Task 3 done")
    ]
    
    results <- await Task.WhenAll(tasks)
    PrintLine("并行任务结果: " + results)
}

// 调用主异步函数
mainTask <- main()
mainTask.Join()
```

### 8.6 多线程示例

```old8
// 线程函数
func workerThread(threadId:int) {
    for i <- 0, i < 5, i++ {
        PrintLine("Thread " + threadId + ": " + i)
        Thread.Sleep(100)
    }
}

// 主函数
func main() {
    // 使用 spawn 函数创建并启动多个线程
    threads <- []
    for i <- 0, i < 3, i++ {
        // 创建并启动线程，传递参数
        thread <- spawn workerThread(i)
        threads.Add(thread)
    }
    
    // 等待所有线程完成
    for thread in threads {
        thread.Join()
    }
    
    PrintLine("All threads completed")
}

// 执行主函数
main()
```

## 9. 编译与执行

### 9.1 解释模式

解释模式下，Old8Lang 代码会逐条解释执行，无需编译。

**异步和多线程支持**：
- 完全支持异步函数 (`async func`)
- 完全支持 `await` 表达式
- 完全支持 `spawn` 函数创建多线程
- 支持 Task API 和异步流

### 9.2 编译模式

编译模式下，Old8Lang 代码会先被编译成中间代码，然后再执行。

**异步和多线程支持**：
- 当前编译模式下异步功能暂未完全支持
- 多线程功能 (`spawn` 函数) 已支持
- 建议在解释模式下开发和测试异步代码
- 编译模式下使用异步功能可能会遇到运行时错误

## 10. 总结

Old8Lang 是一种功能丰富的动态类型编程语言，具有清晰的语法结构和强大的表达能力。本文档详细介绍了 Old8Lang 的语法规则，包括词法、语法和语义，特别是新增的异步编程和多线程功能。

通过学习本文档，您应该能够理解和编写 Old8Lang 代码，包括：
- 基本语法和数据类型
- 控制流语句和函数
- 类、继承和 mixin
- 异常处理和导入机制
- 异步编程模型（async/await）
- Task API 和异步流
- 多线程编程（spawn 函数）

Old8Lang 支持解释模式和编译模式，为不同场景提供了灵活的选择。在解释模式下，您可以快速开发和测试代码，而编译模式则提供了更高的执行效率。