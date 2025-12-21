# Old8Lang 语法文档

**最后更新**: 2025年12月

## 1. 简介

Old8Lang（老八语言）是一种动态类型编程语言，具有类似 C#/Java 的语法。它支持两种执行模式：

- **解释模式** (`-f`): 直接执行代码，无需编译
- **编译模式** (`-c`): 先编译为中间代码再执行，性能更高

## 2. 词法规则

### 2.1 注释

Old8Lang 仅支持单行注释，使用 `//` 开头：

```old8
// 这是一个注释
a <- 123  // 行尾注释也支持
```

### 2.2 标识符

标识符用于表示变量、函数、类等名称，规则如下：

- 必须以字母或下划线 `_` 开头
- 可以包含字母、数字和下划线
- 区分大小写
- 不能是关键字

示例：`myVar`, `_private`, `MyClass123`

### 2.3 关键字

Old8Lang 有以下关键字（不可作为标识符使用）：

```
if elif else for while switch case default
func async class mixin interface extends implements with
return break continue throw yield
try catch finally
import from as native
true false null
and or xor not in
public private static
this super
await spawn
```

### 2.4 字面量

#### 2.4.1 整数字面量

表示整数的值：

```old8
123
-456
0
999_999_999  // 数字分隔符（可选）
```

#### 2.4.2 浮点数字面量

表示浮点数值，支持科学计数法：

```old8
3.14
-0.5
1.0
1.23e3      // 1230
1.23E-4     // 0.000123
2e10        // 20000000000
```

#### 2.4.3 字符串字面量

使用双引号 `"` 包围，支持转义序列：

```old8
"hello world"
"line1\nline2"
"quote: \"hello\""
""  // 空字符串
```

支持的转义序列：`\n`, `\t`, `\r`, `\\`, `\"`

#### 2.4.4 字符字面量

使用单引号 `'` 包围，表示单个字符：

```old8
'a'
'1'
'\n'
'\\'
```

#### 2.4.5 布尔字面量

```old8
true
false
```

#### 2.4.6 空值字面量

```old8
null
```

### 2.5 运算符和分隔符

#### 算术运算符

| 运算符 | 含义 | 示例 | 结果 |
|-------|------|------|------|
| `+` | 加法 | `5 + 3` | `8` |
| `-` | 减法 | `5 - 3` | `2` |
| `*` | 乘法 | `5 * 3` | `15` |
| `/` | 除法 | `6 / 2` | `3` |
| `%` | 取模 | `7 % 3` | `1` |
| `^` | 幂运算 | `2 ^ 3` | `8` |

#### 比较运算符

| 运算符 | 含义 | 示例 |
|-------|------|------|
| `==` | 等于 | `5 == 5` → `true` |
| `!=` | 不等于 | `5 != 3` → `true` |
| `<` | 小于 | `3 < 5` → `true` |
| `>` | 大于 | `5 > 3` → `true` |
| `<=` | 小于等于 | `3 <= 5` → `true` |
| `>=` | 大于等于 | `5 >= 5` → `true` |

#### 逻辑运算符

| 运算符 | 含义 | 示例 |
|-------|------|------|
| `and` 或 `&&` | 逻辑与 | `true and false` → `false` |
| `or` 或 `\|\|` | 逻辑或 | `true or false` → `true` |
| `xor` | 逻辑异或 | `true xor false` → `true` |
| `not` | 逻辑非 | `not true` → `false` |

#### 赋值和成员访问

| 运算符 | 含义 |
|-------|------|
| `<-` | 赋值 |
| `.` | 成员访问 |
| `:` | 类型注解 |
| `->` | 函数返回类型注解 |
| `?` | 三元条件 |

## 3. 数据类型

Old8Lang 是动态类型语言，变量在运行时可以改变类型。但支持可选的类型注解进行静态检查。

### 3.1 基本数据类型

| 类型名 | 描述 | 示例 |
|--------|------|------|
| `int` | 整数 | `123`, `-456` |
| `double` | 浮点数 | `3.14`, `1.23e-4` |
| `string` | 字符串 | `"hello"` |
| `char` | 字符 | `'a'` |
| `bool` | 布尔值 | `true`, `false` |
| `null` | 空值 | `null` |

### 3.2 复合数据类型

#### 3.2.1 数组（Array）

有序集合，使用方括号 `[]`，长度固定：

```old8
arr <- [1, 2, 3, 4, 5]
arr <- [1, "two", 3.0]  // 元素可以是不同类型
arr[0]  // 访问第一个元素，结果为 1
arr[1]  // 访问第二个元素，结果为 "two"
```

#### 3.2.2 列表（List）

动态数组，使用花括号 `{}`，长度可变：

```old8
list <- {1, 2, 3}
list <- {"apple", "banana", "cherry"}
list.Add(4)          // 添加元素
list.Remove(2)       // 删除元素
list[0]              // 访问元素
```

#### 3.2.3 字典（Dictionary）

键值对集合，使用花括号 `{}` 和冒号 `:`：

```old8
dict <- {"name": "Alice", "age": 30}
dict <- {1: "one", 2: "two", 3: "three"}
dict["name"]  // 访问值，结果为 "Alice"
dict["city"] <- "New York"  // 添加或更新
```

**列表和字典的区分**：
- 列表：`{1, 2, 3}` 或 `{}`（空列表）
- 字典：`{"key": value, ...}`（包含冒号）

#### 3.2.4 元组（Tuple）

固定长度的有序集合，使用圆括号 `()`：

```old8
tuple <- (1, "hello")
tuple <- (10, 20, 30)
// 访问方式：通过结构化模式或索引
```

注意：Old8Lang 中元组可以嵌套，如 `(1, ("a", true))`

#### 3.2.5 范围（Range）

表示数值范围，使用方括号和波浪号：

```old8
range <- [1~10]      // 从 1 到 10（包含两端）
range <- [1~<10]     // 从 1 到 10（不包含 10）
range <- [1>~10]     // 从 1 到 10（不包含 1）
range <- [1>~<10]    // 从 1 到 10（两端都不包含）
```

## 4. 表达式

### 4.1 运算符优先级（从高到低）

1. 后缀运算符：`++`, `--`
2. 一元运算符：`-`, `not`
3. 幂运算：`^`
4. 乘除模：`*`, `/`, `%`
5. 加减：`+`, `-`
6. 比较：`<`, `>`, `<=`, `>=`
7. 相等：`==`, `!=`
8. 逻辑与：`and`, `&&`
9. 逻辑异或：`xor`
10. 逻辑或：`or`, `||`
11. 三元运算符：`?` `:`
12. 赋值：`<-`

### 4.2 基本表达式

```old8
a <- 10
b <- 20
c <- a + b
d <- a > b ? "a is greater" : "b is greater"
```

### 4.3 成员访问

使用点号 `.` 访问对象成员和方法：

```old8
obj.field              // 访问字段
obj.method()           // 调用方法
obj.method(arg1, arg2) // 带参数的方法调用
```

### 4.4 索引访问

使用方括号 `[]` 访问数组、列表、字典元素：

```old8
arr[0]       // 数组访问
list[1]      // 列表访问
dict["key"]  // 字典访问
dict[1]      // 字典也可以用数字作为键
```

### 4.5 切片（Slice）

对数组或列表进行切片操作：

```old8
arr <- [1, 2, 3, 4, 5]
slice <- arr[1:3]  // 从索引 1 到 3（不包含 3）
```

### 4.6 类型转换

使用 `as` 关键字进行显式类型转换：

```old8
a <- 123
b <- a as double      // 转换为 double
c <- b as int         // 转换为 int
d <- a as string      // 转换为 string
```

### 4.7 三元表达式

条件表达式：

```old8
max <- a > b ? a : b
message <- x == 0 ? "zero" : "non-zero"
```

### 4.8 String Templates（字符串模板）

使用 C# 风格的字符串模板，`$"..."` 中使用 `{}` 嵌入表达式：

```old8
name <- "Alice"
age <- 30
message <- $"My name is {name}, I'm {age} years old."

// 转义大括号
escaped <- $"This is {{escaped}} bracket"
mixed <- $"Value: {x + 1}, Literal: {{5}}"
```

## 5. 语句

### 5.1 块语句

用花括号 `{}` 包围多个语句：

```old8
{
    a <- 1
    b <- 2
    c <- a + b
}
```

### 5.2 变量声明

使用赋值运算符 `<-` 声明和初始化变量：

```old8
a <- 123              // 声明并赋值
b <- 3.14             // 类型自动推断为 double
name <- "Alice"       // 类型自动推断为 string
```

### 5.3 类型注解

使用冒号 `:` 为变量添加类型注解：

```old8
a:int <- 123          // 明确指定为 int 类型
b:double <- 3.14      // 明确指定为 double 类型
c:string <- "hello"   // 明确指定为 string 类型

// 类型不匹配会导致错误
x:int <- 123
x <- "hello"  // ❌ 错误：不能将 string 赋值给 int 类型的变量
```

### 5.4 控制流语句

#### 5.4.1 if-elif-else 语句

```old8
if x > 10 {
    PrintLine("x is greater than 10")
} elif x > 5 {
    PrintLine("x is greater than 5")
} else {
    PrintLine("x is less than or equal to 5")
}
```

#### 5.4.2 for 循环

基于条件的循环：

```old8
for i <- 0, i < 10, i++ {
    PrintLine("i = " + i)
}

// 也可以使用 -- 递减
for i <- 10, i > 0, i-- {
    PrintLine("i = " + i)
}
```

#### 5.4.3 while 循环

```old8
while x < 100 {
    x <- x * 2
}
```

#### 5.4.4 for-in 循环

遍历集合的元素：

```old8
items <- [1, 2, 3, 4, 5]
for item in items {
    PrintLine(item)
}

// 遍历字典（键值对）
dict <- {"a": 1, "b": 2}
for key, value in dict {
    PrintLine(key + " = " + value)
}

// ⚠️ 注意：不能在 for-in 循环中修改循环变量
for x in list {
    x <- x + 1  // ❌ 错误
}
```

#### 5.4.5 switch 语句

```old8
switch x {
    case 1 {
        PrintLine("x is 1")
    }
    case 2 {
        PrintLine("x is 2")
    }
    default {
        PrintLine("x is something else")
    }
}
```

### 5.5 函数声明

#### 5.5.1 基本函数声明

```old8
// 方式1：使用 func 关键字
func add(a, b) {
    return a + b
}

// 方式2：省略 func 关键字
add(a, b) {
    return a + b
}

// 方式3：使用 -> 指定返回类型
func add(a, b) -> int {
    return a + b
}

// 方式4：函数签名完整
func add(a:int, b:int) -> int {
    return a + b
}
```

#### 5.5.2 参数和默认值

```old8
// 有类型注解
func greet(name:string, age:int) {
    PrintLine("Hello, " + name)
}

// 有默认值
func greet(name:string, greeting: "Hello") {
    PrintLine(greeting + ", " + name)
}

// 混合使用
func configure(host:string, port: 8080, debug: false) {
    // ...
}
```

#### 5.5.3 编译模式类型要求

在编译模式下 (`-c`)，函数声明有以下额外要求：

**必须满足的规则**：

1. **所有参数必须有类型信息**
   - 显式类型注解：`param:int`
   - 或提供默认值用于类型推断：`param: 123`

2. **返回类型必须显式声明**
   - 使用 `->` 指定返回类型
   - 或使用返回类型注解语法：`func name:int(...)`

3. **Lambda 参数类型必须显式声明**
   - Lambda 不支持默认值推断

**正确示例**（编译模式）：

```old8
// ✅ 完整类型注解
func calculate(x:int, y:int) -> int {
    return x + y
}

// ✅ 使用默认值推断参数类型
func process(value: 0, name: "default") -> string {
    return name
}

// ✅ Lambda 参数需要类型注解
multiply <- (a:int, b:int) -> a * b

// ✅ void 返回类型
func printMsg(msg:string) -> void {
    PrintLine(msg)
}
```

**错误示例**（编译模式）：

```old8
// ❌ 缺少返回类型
func add(a:int, b:int) {
    return a + b
}

// ❌ 参数既没有类型也没有默认值
func process(a, b) -> int {
    return a + b
}

// ❌ Lambda 参数缺少类型
transform <- (x) -> x * 2
```

#### 5.5.4 Lambda 表达式

匿名函数，用于简洁表达：

```old8
// 表达式形式
square <- (x:int) -> x * x
add <- (a:int, b:int) -> a + b

// 块形式
greet <- (name:string) -> {
    PrintLine("Hello, " + name)
}
```

#### 5.5.5 访问修饰符

可以为函数添加访问修饰符：

```old8
public func publicFunc() {
    // 公开函数
}

private func privateFunc() {
    // 私有函数
}

static func staticFunc() {
    // 静态函数
}
```

### 5.6 类声明

#### 5.6.1 基本类声明

```old8
class Person {
    public name <- ""
    age <- 0
    
    func init(n:string, a:int) {
        name <- n
        age <- a
    }
    
    func sayHello() {
        PrintLine("Hello, I'm " + name)
    }
}

// 实例化
p <- Person("Alice", 30)
p.sayHello()
```

#### 5.6.2 访问修饰符

```old8
class Example {
    public publicField <- 1      // 公开字段
    private privateField <- 2    // 私有字段
    static staticField <- 3      // 静态字段
    
    public func publicMethod() { }
    private func privateMethod() { }
    static func staticMethod() { }
}
```

#### 5.6.3 类继承

```old8
class Animal {
    public name <- ""
    
    func speak() {
        PrintLine("Some sound")
    }
}

class Dog extends Animal {
    func speak() {
        PrintLine("Woof!")
    }
}

dog <- Dog()
dog.speak()  // 输出：Woof!
```

#### 5.6.4 Mixin（混入）

Mixin 提供可重用的功能模块：

```old8
mixin Logger {
    public func log(message:string) {
        PrintLine("[LOG] " + message)
    }
}

mixin Serializable {
    public func serialize() {
        return "Serialized"
    }
}

class User extends BaseClass with Logger, Serializable {
    public name <- ""
    
    func init(n:string) {
        name <- n
        this.log("User created")
    }
}

user <- User("Bob")
user.log("Doing something")
```

#### 5.6.5 接口声明

接口定义方法签名但不实现方法体：

```old8
interface Drawable {
    func draw() -> void
}

interface Resizable {
    func resize(width:int, height:int) -> void
}

class Rectangle implements Drawable, Resizable {
    func draw() -> void {
        PrintLine("Drawing rectangle")
    }
    
    func resize(width:int, height:int) -> void {
        PrintLine("Resizing to " + width.ToStr() + "x" + height.ToStr())
    }
}
```

### 5.7 异步编程

#### 5.7.1 async/await 基础

```old8
// 定义异步函数
async func fetchData() -> string {
    return "Data from server"
}

// 在异步函数中使用 await
async func main() {
    data <- await fetchData()
    PrintLine(data)
}
```

#### 5.7.2 异步生成器（Async Generator）

结合 `yield` 和 `async` 创建异步流：

```old8
async func countAsync() {
    i <- 1
    while i <= 5 {
        await Task.Delay(100)
        yield i
        i <- i + 1
    }
}

// 使用 async for-in 消费异步流
async func processStream() {
    stream <- countAsync()
    async for num in stream {
        PrintLine(num.ToStr())
    }
}
```

#### 5.7.3 Task API

```old8
// 延迟执行
await Task.Delay(1000)  // 延迟 1 秒

// 等待多个任务
results <- await Task.WhenAll([
    asyncAdd(1, 2),
    asyncAdd(3, 4),
    asyncAdd(5, 6)
])

// 等待第一个完成
first <- await Task.WhenAny([asyncFunc1(), asyncFunc2()])
```

### 5.8 多线程编程

#### 5.8.1 创建线程

使用 `spawn` 函数：

```old8
func worker(id:int) {
    PrintLine("Worker " + id.ToStr() + " started")
    // 执行工作
}

// 创建并启动线程
t <- spawn(worker(1))

// 等待线程完成
t.Join()
```

#### 5.8.2 线程管理

```old8
// 当前线程
currentThread <- Thread.CurrentThread()

// 线程休眠
Thread.Sleep(1000)

// 检查线程状态
if t.IsAlive() {
    PrintLine("Thread is running")
}
```

### 5.9 异常处理

#### 5.9.1 try-catch-finally

```old8
try {
    result <- 10 / 0
} catch (e) {
    PrintLine("Caught error: " + e)
} finally {
    PrintLine("Cleanup")
}
```

#### 5.9.2 throw 语句

```old8
throw "Something went wrong"
throw 42
throw [1, 2, 3]

// 在函数中
func divide(a:int, b:int) {
    if b == 0 {
        throw "Division by zero"
    }
    return a / b
}
```

### 5.10 其他语句

#### 5.10.1 return 语句

```old8
func getValue() {
    return 42
}

func noReturn() {
    return  // 无返回值
}
```

#### 5.10.2 break 和 continue

```old8
for i <- 0, i < 10, i++ {
    if i == 5 {
        break  // 跳出循环
    }
    if i % 2 == 0 {
        continue  // 跳过当前迭代
    }
    PrintLine(i)
}
```

#### 5.10.3 yield 语句

在生成器函数中产生值：

```old8
func generateNumbers() {
    yield 1
    yield 2
    yield 3
}

for num in generateNumbers() {
    PrintLine(num)
}
```

### 5.11 导入语句

#### 5.11.1 简单导入

```old8
import "math"
import math
```

#### 5.11.2 命名导入

```old8
import { sqrt, pow } from "math"
import { sin, cos } from math
```

#### 5.11.3 带别名的导入

```old8
import { sqrt as square_root } from "math"
import { MyClass as MC } from "utils"
```

### 5.12 原生库导入

导入 C# DLL 中的功能：

#### 5.12.1 单个方法导入

```old8
native "Math.dll" MathLib Sqrt sqrt
```

#### 5.12.2 批量导入所有方法

```old8
native "Old8LangLib" MathLib *

// 之后可以直接使用所有方法
result <- Sqrt(16)
pi <- GetPi()
```

#### 5.12.3 选择性导入

```old8
native "Old8LangLib" Time { GetTimeNow, TimeStamp }
```

#### 5.12.4 类导入

```old8
native "Math.dll" MathLib -> MathLib
native "Data.dll" DataClass as DC
```

## 6. 集合操作

### 6.1 列表方法

```old8
list <- {1, 2, 3}

list.Add(4)           // 添加元素
list.Remove(2)        // 删除元素
list.Clear()          // 清空列表
list.Count()          // 获取元素数量
list.Contains(2)      // 检查是否包含元素
list.Join(",")        // 使用分隔符连接元素
```

### 6.2 数组方法

```old8
arr <- [1, 2, 3, 4, 5]

arr.Length        // 获取长度
arr.Reverse()     // 反转
arr.Sort()        // 排序
```

### 6.3 字典方法

```old8
dict <- {"a": 1, "b": 2}

dict.Add("c", 3)        // 添加
dict.Remove("a")        // 删除
dict.Clear()            // 清空
dict.ContainsKey("a")   // 检查键
dict.GetOrElse("x", 0)  // 获取或返回默认值
```

### 6.4 字符串方法

```old8
str <- "Hello, World"

str.Length              // 长度
str.ToUpper()           // 转大写
str.ToLower()           // 转小写
str.Substring(0, 5)     // 截取
str.Contains("World")   // 检查包含
str.Split(",")          // 分割
str.Replace("World", "Old8")  // 替换
```

## 7. 类型系统

### 7.1 动态类型

```old8
x <- 10      // 推断为 int
x <- "hello" // 改变为 string，推断为 string
x <- 3.14    // 改变为 double
```

### 7.2 类型注解

```old8
// 变量类型注解
x:int <- 10

// 函数参数类型注解
func add(a:int, b:int) -> int {
    return a + b
}

// 无效的赋值会抛出错误
x <- "hello"  // ❌ 类型不匹配
```

### 7.3 类型转换

```old8
a <- 123
b <- a as double      // int → double
c <- b as string      // double → string
d <- c as int         // string → int

// 类型转换规则
// int ↔ double (直接转换)
// 数值 ↔ string (通过 ToStr()/解析)
// bool ↔ string ("true"/"false")
```

## 8. 常用内置函数

### 8.1 输出函数

```old8
Print("Hello")      // 输出文本，无换行
PrintLine("Hello")  // 输出文本，有换行
```

### 8.2 类型转换函数

```old8
value.ToStr()       // 转换为字符串
value.ToInt()       // 转换为整数
value.ToDouble()    // 转换为浮点数
value.ToBool()      // 转换为布尔值
```

### 8.3 数学函数（需导入 MathLib）

```old8
native "Old8LangLib" MathLib *

result <- Sqrt(16)          // 平方根
result <- Pow(2, 3)         // 幂运算
result <- Sin(1.57)         // 正弦
result <- Abs(-42)          // 绝对值
result <- Floor(3.7)        // 向下取整
result <- Ceil(3.2)         // 向上取整
```

## 9. 执行模式对比

### 9.1 解释模式 (`-f`)

- 无需编译，直接执行
- 支持完整的类型推断
- 完整支持异步/多线程
- 性能较低，适合开发调试

```bash
dotnet run --project Old8Lang.App -- -f program.old8
```

### 9.2 编译模式 (`-c`)

- 编译为中间代码再执行
- 需要完整的类型注解
- 多线程支持良好
- 异步功能部分支持
- 性能较高，适合生产环境

```bash
dotnet run --project Old8Lang.App -- -c program.old8
```

### 9.3 语法检查模式 (`-s`)

仅检查语法，不执行代码：

```bash
dotnet run --project Old8Lang.App -- -s program.old8
```

## 10. 示例代码

### 10.1 斐波那契数列

```old8
func fibonacci(n:int) -> int {
    if n <= 1 {
        return n
    }
    return fibonacci(n - 1) + fibonacci(n - 2)
}

result <- fibonacci(10)
PrintLine("F(10) = " + result.ToStr())
```

### 10.2 类和继承

```old8
class Shape {
    public name <- ""
    
    func init(n:string) {
        name <- n
    }
    
    func getArea() {
        return 0
    }
}

class Circle extends Shape {
    public radius <- 0.0
    
    func init(n:string, r:double) {
        name <- n
        radius <- r
    }
    
    func getArea() {
        return 3.14 * radius * radius
    }
}

circle <- Circle("MyCircle", 5.0)
PrintLine("Area: " + circle.getArea().ToStr())
```

### 10.3 异步编程

```old8
async func downloadData() -> string {
    await Task.Delay(1000)
    return "Downloaded data"
}

async func main() {
    PrintLine("Starting download...")
    data <- await downloadData()
    PrintLine("Received: " + data)
}

task <- main()
await task
```

### 10.4 异常处理

```old8
func divide(a:int, b:int) {
    if b == 0 {
        throw "Cannot divide by zero"
    }
    return a / b
}

try {
    result <- divide(10, 2)
    PrintLine("Result: " + result.ToStr())
} catch (e) {
    PrintLine("Error: " + e)
} finally {
    PrintLine("Done")
}
```

## 11. 兼容性和局限

### 11.1 已知限制

- 带默认参数的函数在编译模式下可能有运行时问题
- 某些异步特性在编译模式下支持不完整
- 不支持泛型
- 不支持操作符重载

### 11.2 平台支持

- 基于 .NET 10.0 开发
- 跨平台支持（Windows, Linux, macOS）
- 与 C# 代码互操作

## 12. 总结

Old8Lang 是一个功能完整的动态类型语言，结合了脚本语言的灵活性和系统语言的性能。通过支持两种执行模式，它既适合快速开发也适合生产环境。

关键特性总结：
- ✅ 动态类型与可选类型注解
- ✅ 完整的面向对象支持（类、继承、Mixin、接口）
- ✅ 异步/等待模式
- ✅ 多线程支持
- ✅ 异常处理
- ✅ 函数式编程（Lambda、高阶函数）
- ✅ 丰富的集合操作
