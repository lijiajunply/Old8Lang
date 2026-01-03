# Old8Lang 语法文档

**最后更新**: 2025年12月

## 1. 简介

Old8Lang（老八语言）是一种动态类型编程语言，具有类似 C#/Java 的语法。它支持两种执行模式：

- **解释模式** (`-f`): 直接执行代码，无需编译
- **编译模式** (`-c`): 先编译为中间代码再执行，性能更高

## 2. 词法规则

### 2.1 文件头指令

文件头指令用于在文件开头声明配置信息和元数据，类似 Python 的 shebang 或编码声明。

**语法格式**：
```old8
#!<指令名> <指令值>
```

**规则**：
- 必须在文件最开头（空行和注释除外）
- 每行一个指令
- 遇到第一个非文件头指令的语句后，不再解析文件头指令
- 大小写不敏感

**元数据指令**（用于记录文件信息）：
```old8
#!encoding utf-8             // 文件编码
#!author 张三                 // 作者
#!version 1.0.0              // 版本号
#!date 2025-12-28           // 创建日期
#!description 文件描述       // 文件说明
```

**编译器/解释器配置指令**：
```old8
#!debug true                 // 启用调试输出
#!verify-il true             // 启用 IL 代码验证（编译模式）
#!type-inference true        // 启用类型推断
#!type-inference-debug false // 类型推断调试输出
#!optimize 2                 // 优化级别 (0-3)
```

**示例**：
```old8
#!encoding utf-8
#!author 老八
#!version 2.0
#!debug false
#!verify-il true

// 正常的代码从这里开始
func main() -> void {
    PrintLine("Hello, Old8Lang!")
}
```

### 2.2 注释

Old8Lang 支持两种注释：

#### 2.2.1 单行注释

使用 `//` 开头的单行注释：

```old8
// 这是一个注释
a <- 123  // 行尾注释也支持
```

#### 2.2.2 文档注释

使用 `///` 三斜线开头的文档注释，用于为函数、类、接口等添加文档说明：

```old8
/// 这是一个加法函数
/// 用于计算两个整数的和
/// 参数 a: 第一个整数
/// 参数 b: 第二个整数
/// 返回: 两个整数的和
func add(a:int, b:int) -> int {
    return a + b
}

/// 计算器类
/// 提供基本的算术运算功能
class Calculator {
    /// 私有字段，存储当前值
    private value:int <- 0

    /// 加法方法
    /// 将给定值加到当前值上
    func add(n:int) -> int {
        this.value <- this.value + n
        return this.value
    }
}
```

**文档注释特点**：
- 必须紧邻在函数、类或接口声明之前
- 可以有多行，每行都以 `///` 开头
- 会被保存在 AST 节点的 `DocComment` 属性中
- 可用于 IDE 集成，在 VSCode 等编辑器中显示悬停提示

### 2.3 标识符

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
func async class enum mixin interface extends implements with
return break continue throw yield
try catch finally
import from as native
true false null
and or xor not in
public private static
this super
await spawn
match
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
| `?` | 三元条件 / 可空类型后缀 |

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

### 4.9 Match 表达式

Match 表达式提供了模式匹配功能，是一种返回值的表达式（类似三元表达式）。

#### 4.9.1 基本语法

```old8
result <- match value {
    case pattern1 -> expression1
    case pattern2 -> expression2
    case _ -> defaultExpression
}
```

#### 4.9.2 模式类型

Match 表达式支持三种模式：

1. **值匹配**：匹配特定的值

```old8
result <- match 2 {
    case 0 -> "zero"
    case 1 -> "one"
    case 2 -> "two"
    case _ -> "other"
}
// result 为 "two"
```

2. **变量绑定**：将匹配的值绑定到一个变量

```old8
value <- 42
result <- match value {
    case 0 -> "zero"
    case x -> "The value is " + x.ToStr()
}
// result 为 "The value is 42"
// 变量 x 在表达式中可用，作用域仅限于该 case
```

3. **通配符**：使用 `_` 匹配任何值（通常用作默认分支）

```old8
result <- match 999 {
    case 1 -> "one"
    case 2 -> "two"
    case _ -> "unknown"
}
// result 为 "unknown"
```

#### 4.9.3 支持的类型

Match 表达式支持所有基本类型的匹配：

```old8
// 整数匹配
int_result <- match 2 {
    case 1 -> "one"
    case 2 -> "two"
    case _ -> "other"
}

// 字符串匹配
name <- "Alice"
greeting <- match name {
    case "Bob" -> "Hello Bob!"
    case "Alice" -> "Hi Alice!"
    case _ -> "Hello stranger!"
}

// 布尔值匹配
flag <- true
status <- match flag {
    case true -> "enabled"
    case false -> "disabled"
}

// 字符匹配
grade <- 'A'
description <- match grade {
    case 'A' -> "Excellent"
    case 'B' -> "Good"
    case 'C' -> "Average"
    case _ -> "Invalid"
}

// 浮点数匹配
pi <- 3.14
constant <- match pi {
    case 2.71 -> "Euler's number"
    case 3.14 -> "Pi"
    case _ -> "Unknown"
}
```

#### 4.9.4 返回值

Match 表达式可以返回任何类型的值：

```old8
// 返回字符串
message <- match status {
    case 0 -> "OK"
    case 1 -> "Error"
    case _ -> "Unknown"
}

// 返回数值
multiplier <- match level {
    case 1 -> 10
    case 2 -> 20
    case 3 -> 30
    case _ -> 1
}

// 返回复杂表达式的结果
result <- match x {
    case 0 -> calculate(0)
    case n -> calculate(n * 2)
}
```

#### 4.9.5 匹配规则

- **顺序匹配**：从上到下依次检查每个 case，执行第一个匹配的分支
- **必须有匹配**：如果没有任何 case 匹配，会抛出 `InvalidOperationError` 异常
- **建议使用通配符**：建议在最后添加 `case _ ->` 作为默认分支，避免运行时错误

```old8
// 不推荐：可能抛出异常
result <- match value {
    case 1 -> "one"
    case 2 -> "two"
}

// 推荐：添加默认分支
result <- match value {
    case 1 -> "one"
    case 2 -> "two"
    case _ -> "other"
}
```

#### 4.9.6 变量作用域

变量绑定模式中的变量作用域仅限于该 case 的表达式：

```old8
outer_x <- 100

result <- match 42 {
    case x -> "matched: " + x.ToStr()  // x = 42，仅在此表达式中有效
}

// outer_x 仍然是 100，不受影响
PrintLine(outer_x.ToStr())  // 输出: 100
```

#### 4.9.7 嵌套 Match

Match 表达式可以嵌套使用：

```old8
result <- match category {
    case "A" -> match level {
        case 1 -> "A1"
        case 2 -> "A2"
        case _ -> "A-other"
    }
    case "B" -> match level {
        case 1 -> "B1"
        case 2 -> "B2"
        case _ -> "B-other"
    }
    case _ -> "unknown"
}
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

#### 5.3.1 可空类型

可空类型注解使用问号 `?` 后缀，表示变量可以存储指定类型的值或 `null`：

```old8
// 可空类型注解
a:int? <- 123         // 可以是 int 或 null
b:int? <- null        // 赋值为 null

c:string? <- "hello"  // 可以是 string 或 null
d:string? <- null

// 函数参数中的可空类型
func processValue(value:int?) -> void {
    if value == null {
        PrintLine("值为空")
    } else {
        PrintLine("值: " + value.ToStr())
    }
}

// 函数返回值的可空类型
func findValue(arr:list, target:int) -> int? {
    for i <- 0, i < arr.Length(), i <- i + 1 {
        if arr[i] == target {
            return i
        }
    }
    return null  // 没有找到，返回 null
}
```

**可空类型的特点：**
- 可空类型的变量既可以存储指定类型的值，也可以存储 `null`
- 非可空类型（如 `int`）不能被赋值为 `null`
- 可空类型在运行时会进行类型检查，确保类型安全

**支持的可空类型：**
- `int?` - 可空整数
- `double?` - 可空双精度浮点数
- `string?` - 可空字符串
- `bool?` - 可空布尔值
- `char?` - 可空字符
- `any?` - 可空任意类型

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

#### 5.4.6 using 语句（资源管理）

using 语句提供自动资源管理，确保资源在使用完毕后自动释放：

```old8
// 形式1：带变量声明
using mutex <- MutexCreate() {
    MutexLock(mutex)
    PrintLine("持有互斥锁")
    MutexUnlock(mutex)
}  // 自动调用 MutexDispose(mutex)

// 形式2：使用已有变量
ch <- ChannelCreate()
using ch {
    ChannelSend(ch, 123)
    value <- ChannelReceive(ch)
    PrintLine(value)
}  // 自动调用 ChannelDispose(ch)
```

**特性**：
- 使用 try-finally 实现，即使发生异常也会正确释放资源
- 支持所有返回资源 ID（int）并具有对应 Dispose 函数的资源
- 解释模式和编译模式均支持

#### 5.4.7 select 语句（Channel 多路选择）

select 语句实现 Go 风格的 Channel 多路选择：

```old8
ch1 <- ChannelCreateBounded(1)
ch2 <- ChannelCreateBounded(1)

select {
    case ch1 <- 100 -> {
        PrintLine("成功发送 100 到 ch1")
    }
    case ch2 <- 200 -> {
        PrintLine("成功发送 200 到 ch2")
    }
    default -> {
        PrintLine("所有 channel 都不可用")
    }
}
```

**特性**：
- 使用轮询策略检查多个 Channel
- 执行第一个可用的 case（发送或接收）
- 如果没有 case 可用且存在 default 分支，立即执行 default
- 如果没有 case 可用且没有 default，阻塞直到某个 case 变为可用

**限制**：
- 接收 case（`val <- ch`）在 channel 为简单标识符时存在语法歧义
- 编译模式（`-c`）不支持，会抛出 NotImplementedException
- 需使用解释模式（`-f`）运行包含 select 语句的代码

#### 5.4.8 defer 语句（延迟执行）

defer 语句用于在函数返回前延迟执行语句或代码块，类似 Go 语言的 defer。

```old8
func example() -> void {
    PrintLine("开始执行")
    defer PrintLine("函数即将返回")
    PrintLine("函数主体")
}
// 输出：
// 开始执行
// 函数主体
// 函数即将返回
```

**defer 代码块**：

```old8
func cleanup() -> void {
    file <- OpenFile("data.txt")
    defer {
        CloseFile(file)
        PrintLine("文件已关闭")
    }
    // 文件操作...
}
```

**defer 执行顺序（后进先出 LIFO）**：

```old8
func testOrder() -> void {
    defer PrintLine("defer 1")
    defer PrintLine("defer 2")
    defer PrintLine("defer 3")
    PrintLine("函数体执行")
}
// 输出：
// 函数体执行
// defer 3
// defer 2
// defer 1
```

**defer 访问局部变量**：

```old8
func testVariable() -> void {
    x <- "初始值"
    defer PrintLine("defer看到的x: " + x)
    x <- "修改后的值"
    PrintLine("函数体中的x: " + x)
}
// 输出：
// 函数体中的x: 修改后的值
// defer看到的x: 修改后的值
```

**defer 与 return**：

```old8
func withReturn() -> int {
    defer PrintLine("即将返回")
    return 42
}
// defer 在 return 前执行，但不影响返回值
```

**特性**：
- **函数级作用域**：defer 在函数返回前执行，而非代码块结束时
- **LIFO 执行顺序**：多个 defer 按后进先出的顺序执行
- **变量访问**：defer 可以访问函数的局部变量，看到的是 defer 执行时的变量值
- **异常安全**：defer 使用 try-finally 实现，即使发生异常也会执行
- **defer 中的异常**：defer 语句中的异常会被捕获并打印，不会阻止其他 defer 的执行
- **适用场景**：资源清理、日志记录、性能统计等

**与 using 的区别**：
- `using`：专门用于自动资源管理（Dispose），作用域为代码块
- `defer`：通用的延迟执行机制，作用域为函数，支持任意语句

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

#### 5.5.2.5 可变参数（Params）

Old8Lang 支持使用 `params` 关键字声明可变参数，允许函数接受任意数量的参数。

**语法规则**：

```old8
func functionName(params paramName:array<T>) -> returnType {
    // 函数体
}
```

**规则说明**：

1. **位置限制**：`params` 参数必须是参数列表的最后一个参数
2. **数量限制**：一个函数只能有一个 `params` 参数
3. **类型要求**：`params` 参数必须声明为数组类型（`array<T>`）
4. **不能有默认值**：`params` 参数不能有默认值，会自动处理为空数组
5. **模式支持**：解释器模式和编译器模式都完全支持

**基本示例**：

```old8
// 定义可变参数函数
func sum(params args:array<int>) -> int {
    result:int <- 0
    for arg in args {
        result <- result + arg
    }
    return result
}

// 使用可变参数
total1 <- sum()                    // 传入 0 个参数，result = 0
total2 <- sum(1, 2, 3)             // 传入 3 个参数，result = 6
total3 <- sum(10, 20, 30, 40, 50)  // 传入 5 个参数，result = 150
```

**结合普通参数使用**：

```old8
// params 参数必须在最后
func format(prefix:string, params items:array<string>) -> string {
    result:string <- prefix
    for item in items {
        result <- result + "_" + item
    }
    return result
}

// 使用示例
str1 <- format("start")                      // "start"
str2 <- format("start", "a", "b", "c")       // "start_a_b_c"
str3 <- format("prefix", "item1", "item2")   // "prefix_item1_item2"
```

**访问 params 数组**：

```old8
func describe(params values:array<int>) -> void {
    count <- len(values)  // 获取参数数量
    PrintLine("收到 " + count.ToStr() + " 个参数")

    for i <- 0, i < count, i++ {
        PrintLine("参数 " + i.ToStr() + ": " + values[i].ToStr())
    }
}

describe(10, 20, 30)
// 输出:
// 收到 3 个参数
// 参数 0: 10
// 参数 1: 20
// 参数 2: 30
```

**支持不同类型的 params**：

```old8
// 字符串类型的可变参数
func concat(separator:string, params words:array<string>) -> string {
    if len(words) == 0 {
        return ""
    }
    result <- words[0]
    for i <- 1, i < len(words), i++ {
        result <- result + separator + words[i]
    }
    return result
}

text <- concat(", ", "apple", "banana", "cherry")  // "apple, banana, cherry"

// 双精度类型的可变参数
func average(params numbers:array<double>) -> double {
    if len(numbers) == 0 {
        return 0.0
    }
    sum:double <- 0.0
    for num in numbers {
        sum <- sum + num
    }
    return sum / len(numbers)
}

avg <- average(1.5, 2.5, 3.5, 4.5)  // 3.0
```

**编译模式注意事项**：

在编译模式下，`params` 参数的类型注解是必须的，且必须是数组类型：

```old8
// ✅ 正确：显式声明为数组类型
func sum(params args:array<int>) -> int {
    // ...
}

// ❌ 错误：缺少类型注解
func sum(params args) -> int {
    // ...
}

// ❌ 错误：类型不是数组
func sum(params args:list<int>) -> int {
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

#### 5.5.4 泛型函数

泛型函数允许定义可以处理多种类型的函数，通过类型参数实现代码复用。

**语法格式**：

```old8
func functionName<TypeParam1, TypeParam2, ...>(params) -> ReturnType {
    // 函数体
}
```

**单个类型参数示例**：

```old8
// 泛型函数：返回传入的值
func identity<T>(value:T) -> T {
    return value
}

// 使用泛型函数
intResult <- identity<int>(42)           // 返回 42 (int)
stringResult <- identity<string>("hello")  // 返回 "hello" (string)
doubleResult <- identity<double>(3.14)    // 返回 3.14 (double)
```

**多个类型参数示例**：

```old8
// 泛型函数：创建键值对字符串
func makePair<K, V>(key:K, value:V) -> string {
    return key.ToStr() + ":" + value.ToStr()
}

result1 <- makePair<string, int>("age", 25)      // "age:25"
result2 <- makePair<int, string>(1, "first")     // "1:first"
```

**泛型函数特点**：

- 类型参数在调用时必须显式指定：`funcName<Type>(...)`
- 函数体内可以使用类型参数作为参数类型或返回类型
- 每次调用都会根据指定的类型参数创建类型化的函数实例
- 类型参数在函数作用域内有效
- 支持泛型类型推断，编译器可以从函数调用参数自动推断类型参数

**泛型约束**：

泛型函数和类可以使用约束来限制类型参数必须满足的条件。

```old8
// 使用冒号语法指定约束
func process<T: IComparable>(value: T) -> T {
    // T 必须实现 IComparable 接口
    return value
}

// 使用 & 符号组合多个约束
class Container<T: ISerializable & ICloneable> {
    value: T

    func init(v: T) {
        this.value <- v
    }
}

// 使用 where 子句语法
func sort<T>(items: List<T>) -> List<T> where T: IComparable {
    // T 必须实现 IComparable 接口
    return items
}

// 多个类型参数的 where 约束
func merge<K, V>(key: K, value: V) -> any where K: IComparable, V: ISerializable {
    // K 必须实现 IComparable，V 必须实现 ISerializable
    return null
}

// 混合约束语法
func complexFunc<T: IComparable>(value: T) -> T where T: ICloneable {
    // T 必须同时实现 IComparable 和 ICloneable
    return value
}
```

**约束语法**：

- **单个约束**：`<T: IInterface>` 或 `where T: IInterface`
- **多个约束（| 分隔）**：`<T: IInterface1 | IInterface2>`（类型满足任一接口即可）
- **多个约束（& 分隔）**：`<T: IInterface1 & IInterface2>`（类型必须同时满足所有接口）
- **where 子句**：`where T: IInterface` 用于函数级别的约束
- **多个类型参数约束**：`where T: IInterface1, U: IInterface2`

**泛型可空类型参数**：

泛型类型参数可以标记为可空类型，使用 `?` 后缀表示该类型参数可以接受 `null` 值。

```old8
// 定义可空类型参数的泛型类
class Box<T?> {
    value: T?

    func init(v: T?) {
        this.value <- v
    }

    func getValue() -> T? {
        return this.value
    }

    func hasValue() -> bool {
        return this.value != null
    }
}

// 使用可空泛型类
box1 <- new Box(123)      // T? 推断为 int?
box2 <- new Box(null)     // T? 允许 null 值

// 定义可空类型参数的泛型函数
func identity<T?>(value: T?) -> T? {
    return value
}

result1 <- identity<int>(456)    // 返回 456
result2 <- identity<int>(null)   // 返回 null

// 混合可空和非可空类型参数
class Container<T, U?> {
    required: T    // 非可空类型
    optional: U?   // 可空类型

    func init(r: T, o: U?) {
        this.required <- r
        this.optional <- o
    }
}

container <- new Container(100, "text")
container2 <- new Container(200, null)  // U? 可以是 null

// 可空类型参数也可以带约束
class OptionalValue<T?: IComparable> {
    data: T?

    func init(d: T?) {
        this.data <- d
    }

    func hasValue() -> bool {
        return this.data != null
    }
}

// 可空类型参数与 where 子句结合
func process<T?>(value: T?) -> T? where T: IValue {
    return value
}
```

**可空类型参数特点**：

- 使用 `?` 后缀标记类型参数为可空类型
- 可空类型参数可以接受 `null` 值作为实参
- 可以与约束语法组合使用
- 支持在函数和类中使用
- 可以与非可空类型参数混合使用

**使用场景**：

```old8
// 获取第一个元素
func getFirst<T>(a:T, b:T) -> T {
    return a
}

// 包装值并转换为字符串
func wrap<T>(value:T) -> string {
    return "[" + value.ToStr() + "]"
}

// 组合多个类型参数
func combine<A, B, C>(a:A, b:B, c:C) -> string {
    return a.ToStr() + "-" + b.ToStr() + "-" + c.ToStr()
}

result <- combine<int, string, double>(1, "two", 3.0)  // "1-two-3"
```

#### 5.5.5 Lambda 表达式

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

#### 5.6.4 泛型类

泛型类允许定义可以处理多种类型的类，通过类型参数实现代码复用。

**语法格式**：

```old8
class ClassName<TypeParam1, TypeParam2, ...> {
    // 类体
}
```

**单个类型参数示例**：

```old8
// 定义泛型类 Box<T>
class Box<T> {
    private value:T

    func set(v:T) -> void {
        this.value <- v
    }

    func get() -> T {
        return this.value
    }
}

// 使用泛型类
box <- Box<int>()
box.set(42)
result <- box.get()  // result = 42

// 不同类型的实例
stringBox <- Box<string>()
stringBox.set("hello")
text <- stringBox.get()  // text = "hello"
```

**多个类型参数示例**：

```old8
// 定义泛型键值对类
class Pair<K, V> {
    private key:K
    private value:V

    func set(k:K, v:V) -> void {
        this.key <- k
        this.value <- v
    }

    func getKey() -> K {
        return this.key
    }

    func getValue() -> V {
        return this.value
    }
}

// 使用多个类型参数
pair <- Pair<string, int>()
pair.set("age", 25)
k <- pair.getKey()      // k = "age"
v <- pair.getValue()    // v = 25
```

**带构造函数的泛型类**：

```old8
class Wrapper<T> {
    private wrapped:T

    func init(w:T) {
        this.wrapped <- w
    }

    func unwrap() -> T {
        return this.wrapped
    }
}

// 创建实例时传入构造参数
wrapper <- Wrapper<int>(123)
result <- wrapper.unwrap()  // result = 123
```

**泛型类特点**：

- 类型参数在实例化时必须显式指定：`ClassName<Type>()`
- 类体内可以使用类型参数作为字段类型、参数类型或返回类型
- 每个实例都有独立的类型参数绑定
- 不同类型参数的实例是不同的类型
- 支持泛型约束（使用 `:` 和 `&` 符号），约束类型参数必须满足特定接口

**泛型类约束示例**：

```old8
// 单个约束
class SortedList<T: IComparable> {
    private items: list

    func add(item: T) {
        // T 必须实现 IComparable 接口
        this.items.Add(item)
    }
}

// 多个约束（& 分隔）
class Cache<T: ISerializable & ICloneable> {
    private data: T

    func store(value: T) {
        this.data <- value.clone()
    }

    func serialize() -> string {
        return this.data.serialize()
    }
}
```

**实用示例 - 泛型栈**：

```old8
class Stack<T> {
    private items:list

    func init() {
        this.items <- {}
    }

    func push(item:T) -> void {
        this.items <- this.items.Add(item)
    }

    func pop() -> T {
        lastIndex <- this.items.Count() - 1
        item <- this.items[lastIndex]
        this.items.RemoveAt(lastIndex)
        return item
    }

    func peek() -> T {
        return this.items[-1]
    }
}

// 使用泛型栈
stack <- Stack<string>()
stack.push("first")
stack.push("second")
stack.push("third")

top <- stack.peek()    // top = "third"
popped <- stack.pop()  // popped = "third"
```

#### 5.6.5 Mixin（混入）

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

#### 5.6.6 接口声明

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

### 5.7 枚举声明

#### 5.7.1 基本枚举声明

枚举（Enum）用于定义一组命名的整数常量，提供更好的代码可读性和类型安全。

**语法格式**：

```old8
enum EnumName {
    Member1,
    Member2,
    Member3
}
```

**基本示例**：

```old8
// 定义颜色枚举
enum Color {
    Red,      // 自动赋值为 0
    Green,    // 自动赋值为 1
    Blue      // 自动赋值为 2
}

// 访问枚举成员
colorCode <- Color.Red
PrintLine(colorCode.ToStr())  // 输出: 0
```

**枚举特点**：

- 枚举成员自动从 0 开始递增赋值
- 枚举成员的值是整数类型
- 使用点号 `.` 访问枚举成员
- 枚举成员可以用于比较和算术运算

#### 5.7.2 显式值枚举

可以为枚举成员显式指定整数值：

```old8
// HTTP 状态码
enum HttpStatus {
    OK <- 200,
    Created <- 201,
    BadRequest <- 400,
    NotFound <- 404,
    InternalServerError <- 500
}

status <- HttpStatus.OK
if status == 200 {
    PrintLine("请求成功")
}
```

#### 5.7.3 混合自动和显式值

可以混合使用自动值和显式值，未指定值的成员会在前一个成员值的基础上递增：

```old8
enum Priority {
    Low,          // 0（自动）
    Medium <- 5,  // 5（显式）
    High,         // 6（自动，基于上一个值 +1）
    Critical <- 10 // 10（显式）
}

PrintLine(Priority.Low.ToStr())      // 输出: 0
PrintLine(Priority.Medium.ToStr())   // 输出: 5
PrintLine(Priority.High.ToStr())     // 输出: 6
PrintLine(Priority.Critical.ToStr()) // 输出: 10
```

#### 5.7.4 枚举值的使用

**在条件语句中使用**：

```old8
enum Status {
    Success,
    Pending,
    Failed
}

currentStatus <- Status.Success

if currentStatus == 0 {
    PrintLine("操作成功")
} elif currentStatus == 1 {
    PrintLine("等待中")
} else {
    PrintLine("操作失败")
}
```

**在 switch 语句中使用**：

```old8
enum Direction {
    North,
    South,
    East,
    West
}

direction <- Direction.North

switch direction {
    case 0 {
        PrintLine("向北")
    }
    case 1 {
        PrintLine("向南")
    }
    case 2 {
        PrintLine("向东")
    }
    case 3 {
        PrintLine("向西")
    }
}
```

**在函数参数中使用**：

```old8
enum LogLevel {
    Debug,
    Info,
    Warning,
    Error
}

func log(level:int, message:string) -> void {
    levelName <- match level {
        case 0 -> "DEBUG"
        case 1 -> "INFO"
        case 2 -> "WARNING"
        case 3 -> "ERROR"
        case _ -> "UNKNOWN"
    }
    PrintLine("[" + levelName + "] " + message)
}

log(LogLevel.Info, "应用程序启动")
log(LogLevel.Error, "发生错误")
```

#### 5.7.5 空枚举和单成员枚举

```old8
// 空枚举（语法合法）
enum Empty {}

// 单成员枚举
enum SingleValue {
    OnlyOne
}

value <- SingleValue.OnlyOne  // 值为 0
```

#### 5.7.6 枚举的特性

**访问修饰符**：

枚举支持访问修饰符（`public`、`private`）：

```old8
public enum PublicEnum {
    Value1,
    Value2
}

private enum PrivateEnum {
    Value1,
    Value2
}
```

**枚举作用域**：

- 枚举在当前模块作用域中定义
- 枚举成员通过 `EnumName.MemberName` 访问
- 枚举值是不可变的整数常量

**类型兼容性**：

```old8
enum ErrorCode {
    Success <- 0,
    FileNotFound <- 1,
    AccessDenied <- 2
}

// 枚举值可以与整数进行比较和运算
code <- ErrorCode.FileNotFound
if code > 0 {
    PrintLine("发生错误")
}

// 枚举值可以参与算术运算
nextCode <- code + 1
PrintLine(nextCode.ToStr())  // 输出: 2
```

#### 5.7.7 枚举的局限性

- 枚举成员的值必须是整数常量（不支持字符串或其他类型）
- 枚举不支持方法定义
- 枚举成员在定义后不可修改
- 枚举没有命名空间隔离，所有成员必须唯一

### 5.8 异步编程

#### 5.8.1 async/await 基础

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

#### 5.8.2 异步生成器（Async Generator）

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

#### 5.8.3 Task API

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

### 5.9 多线程编程

#### 5.9.1 创建线程

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

#### 5.9.2 线程管理

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

### 5.10 异常处理

#### 5.10.1 try-catch-finally

```old8
try {
    result <- 10 / 0
} catch (e) {
    PrintLine("Caught error: " + e)
} finally {
    PrintLine("Cleanup")
}
```

#### 5.10.2 throw 语句

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

### 5.11 其他语句

#### 5.11.1 return 语句

```old8
func getValue() {
    return 42
}

func noReturn() {
    return  // 无返回值
}
```

#### 5.11.2 break 和 continue

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

#### 5.11.3 yield 语句

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

### 5.12 导入语句

#### 5.12.1 简单导入

```old8
import "math"
import math
```

#### 5.12.2 命名导入

```old8
import { sqrt, pow } from "math"
import { sin, cos } from math
```

#### 5.12.3 带别名的导入

```old8
import { sqrt as square_root } from "math"
import { MyClass as MC } from "utils"
```

### 5.13 原生库导入

导入 C# DLL 中的功能：

#### 5.13.1 单个方法导入

```old8
native "Math.dll" MathLib Sqrt sqrt
```

#### 5.13.2 批量导入所有方法

```old8
native "Old8LangLib" MathLib *

// 之后可以直接使用所有方法
result <- Sqrt(16)
pi <- GetPi()
```

#### 5.13.3 选择性导入

```old8
native "Old8LangLib" Time { GetTimeNow, TimeStamp }
```

#### 5.13.4 类导入

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

### 7.3 泛型集合类型 (Generic Collection Types)

Old8Lang 支持泛型集合类型注解，提供编译时类型检查。

**支持的泛型集合类型**：
- `list<T>`：列表，单类型参数
- `array<T>`：数组，单类型参数
- `dict<K,V>`：字典，双类型参数（键类型和值类型）

#### 7.3.1 基本用法

```old8
// 列表：list<T>
items:list<int> <- {1, 2, 3}
names:list<string> <- {"Alice", "Bob", "Charlie"}

// 数组：array<T>
arr:array<int> <- [1, 2, 3]
prices:array<double> <- [1.5, 2.5, 3.5]

// 字典：dict<K,V>
ages:dict<string, int> <- {"Alice": 30, "Bob": 25}
mapping:dict<int, string> <- {1: "one", 2: "two"}
```

#### 7.3.2 嵌套泛型类型

```old8
// 嵌套列表：list<list<T>>
matrix:list<list<int>> <- {{1, 2}, {3, 4}, {5, 6}}

// 字典的值为列表：dict<K, list<T>>
groups:dict<string, list<int>> <- {
    "a": {1, 2, 3},
    "b": {4, 5, 6}
}

// 列表的元素为数组：list<array<T>>
arrays:list<array<int>> <- {[1, 2], [3, 4]}
```

#### 7.3.3 编译时类型检查

在**编译器模式** (`-c`) 下，泛型集合类型会进行严格的类型检查：

```old8
// ✅ 正确：所有元素类型一致
items:list<int> <- {1, 2, 3}

// ❌ 错误：类型不匹配
items:list<int> <- {1, "hello", 3}
// 编译错误：变量 'items' 列表元素类型不匹配: 第 1 个元素期望类型 int,实际类型 string

// ✅ 正确：字典类型匹配
ages:dict<string, int> <- {"Alice": 30, "Bob": 25}

// ❌ 错误：字典值类型不匹配
ages:dict<string, int> <- {"Alice": 30, "Bob": "twenty-five"}
// 编译错误：变量 'ages' 字典值类型不匹配: 第 1 个值期望类型 int,实际类型 string
```

#### 7.3.4 向后兼容性

在**解释器模式** (`-f`) 下，泛型类型注解是可选的：

```old8
// 不带类型注解：支持混合类型（向后兼容）
mixed <- {1, "hello", 3.14, true}  // ✅ 正常工作
arr <- [1, "world", false]          // ✅ 正常工作

// 带类型注解：编译器模式下会进行类型检查
items:list<int> <- {1, 2, 3}       // ✅ 两种模式都支持
```

**兼容性保证**：
- 所有不带类型注解的现有代码继续正常工作
- 泛型类型注解是可选特性，不强制使用
- 编译器模式提供更严格的类型安全，解释器模式保持灵活性

### 7.4 联合类型 (Union Types)

联合类型使用 `|` 符号，表示值可以是多个类型之一。主要用于编译时类型检查。

**语法**: `A | B | C`

**使用场景**:

1. **变量声明**：
```old8
// 基本联合类型
value: int | string <- 123
value <- "hello"  // ✅ 可以重新赋值为另一个联合类型成员

// 多类型联合
data: int | string | bool <- true
data <- 456        // ✅ 可以是 int
data <- "text"     // ✅ 可以是 string
```

2. **可空联合类型**：
```old8
// 可空类型的联合
nullable: int? | string? <- null  // ✅ null 兼容于可空类型
nullable <- 123                    // ✅ int 赋值给 int?
nullable <- "test"                 // ✅ string 赋值给 string?
```

3. **函数参数和返回值**：
```old8
// 函数参数支持联合类型
func process(x: int | string) -> void {
    PrintLine(x.ToStr())
}

process(123)      // ✅ 传入 int
process("hello")  // ✅ 传入 string

// 函数返回值支持联合类型
func getValue(flag: bool) -> int | string {
    if flag {
        return 123
    } else {
        return "hello"
    }
}
```

4. **类字段**：
```old8
class Container {
    public data: int | string | bool

    func init(value: int | string | bool) {
        data <- value
    }
}

container <- Container(123)
container.data <- "text"  // ✅ 可以改变为另一个联合类型成员
```

5. **泛型联合类型**：
```old8
// 泛型参数中的联合类型
list: List<int | string> <- {1, "hello", 2, "world"}

// 嵌套泛型联合类型
map: Map<string, int | string> <- {"age": 25, "name": "Alice"}
```

**兼容性规则**:
- `A | B` 兼容于 `A`（联合类型可以赋值给任一成员类型）
- `A` 兼容于 `A | B`（任一成员类型可以赋值给联合类型）
- `null` 兼容于任何包含可空类型的联合类型（如 `int? | string?`）

### 7.5 交叉类型 (Intersection Types)

交叉类型使用 `&` 符号，表示类型必须同时满足所有约束。主要用于接口组合和泛型约束。

**语法**: `A & B & C`

**使用场景**:

1. **泛型约束**（最常用）：
```old8
// 泛型类型必须同时实现多个接口
func sort<T>(items: List<T>) -> List<T> where T: IComparable & ICloneable {
    // T 必须同时实现 IComparable 和 ICloneable
    // 实现排序逻辑...
}

// 在泛型类中使用交叉约束
class Box<T: ISerializable & IDisposable> {
    private value: T

    func init(v: T) {
        value <- v
    }
}
```

2. **函数参数**：
```old8
// 参数必须满足多个接口
func process(obj: IReadable & IWritable) -> void {
    // obj 必须同时实现 IReadable 和 IWritable
}
```

3. **变量声明**：
```old8
// 变量类型必须满足所有接口
handler: ILogger & IMetrics <- MyHandler()
```

**兼容性规则**:
- `A & B` 兼容于 `A`（交叉类型满足所有成员，可以赋值给任一成员）
- `A & B` 兼容于 `B`
- `A` 不兼容于 `A & B`（单个类型不满足所有约束）

**注意事项**:
- 交叉类型主要用于接口组合，不能对基础类型（如 `int & string`）使用
- 在泛型约束中，`&` 和 `|` 都表示"且"关系（历史原因），但在类型注解中有明确区分

### 7.6 类型转换

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

### 8.4 并发原语函数（内置全局函数）

Old8Lang 提供了一套完整的并发原语全局函数，无需导入即可使用：

#### 8.4.1 Mutex（互斥锁）

```old8
// 创建互斥锁
mutex <- MutexCreate()

// 加锁
MutexLock(mutex)

// 尝试加锁（带超时）
success <- MutexTryLock(mutex, 1000)  // 超时时间：毫秒

// 解锁
MutexUnlock(mutex)

// 释放资源
MutexDispose(mutex)
```

#### 8.4.2 Semaphore（信号量）

```old8
// 创建信号量（初始计数，最大计数）
sem <- SemaphoreCreate(1, 3)

// 获取信号
SemaphoreAcquire(sem)

// 尝试获取信号（带超时）
success <- SemaphoreTryAcquire(sem, 1000)

// 释放信号
SemaphoreRelease(sem)

// 释放资源
SemaphoreDispose(sem)
```

#### 8.4.3 AtomicInt（原子整数）

```old8
// 创建原子整数
atomic <- AtomicIntCreate(0)

// 获取值
value <- AtomicIntGet(atomic)

// 设置值
AtomicIntSet(atomic, 10)

// 原子递增/递减
newValue <- AtomicIntIncrement(atomic)
newValue <- AtomicIntDecrement(atomic)

// 原子加法
newValue <- AtomicIntAdd(atomic, 5)

// 比较并交换（CAS）
success <- AtomicIntCompareAndSet(atomic, 10, 20)

// 释放资源
AtomicIntDispose(atomic)
```

#### 8.4.4 Channel（通道）

```old8
// 创建无界通道
ch <- ChannelCreate()

// 创建有界通道
boundedCh <- ChannelCreateBounded(10)

// 发送数据
ChannelSend(ch, "Hello")

// 尝试发送（带超时）
success <- ChannelTrySend(ch, "World", 1000)

// 接收数据
data <- ChannelReceive(ch)

// 尝试接收（带超时）
data <- ChannelTryReceive(ch, 1000)  // 超时返回 null

// 关闭通道
ChannelClose(ch)

// 释放资源
ChannelDispose(ch)
```

#### 8.4.5 ReadWriteLock（读写锁）

```old8
// 创建读写锁
rwLock <- ReadWriteLockCreate()

// 读锁
ReadLockAcquire(rwLock)
// ... 读操作
ReadLockRelease(rwLock)

// 写锁
WriteLockAcquire(rwLock)
// ... 写操作
WriteLockRelease(rwLock)

// 尝试获取读锁（带超时）
success <- ReadLockTryAcquire(rwLock, 1000)

// 尝试获取写锁（带超时）
success <- WriteLockTryAcquire(rwLock, 1000)

// 释放资源
ReadWriteLockDispose(rwLock)
```

#### 8.4.6 CountDownLatch（倒计时锁）

```old8
// 创建倒计时锁（初始计数）
latch <- CountDownLatchCreate(3)

// 减少计数
CountDownLatchCountDown(latch)

// 等待计数归零
CountDownLatchWait(latch)

// 带超时的等待
success <- CountDownLatchWaitTimeout(latch, 5000)

// 获取当前计数
count <- CountDownLatchGetCount(latch)

// 释放资源
CountDownLatchDispose(latch)
```

#### 8.4.7 CyclicBarrier（循环栅栏）

```old8
// 创建循环栅栏（参与者数量）
barrier <- CyclicBarrierCreate(3)

// 等待所有参与者到达
CyclicBarrierAwait(barrier)

// 带超时的等待
success <- CyclicBarrierAwaitTimeout(barrier, 5000)

// 获取参与者数量
count <- CyclicBarrierGetParticipantCount(barrier)

// 获取当前等待数量
waiting <- CyclicBarrierGetWaitingCount(barrier)

// 释放资源
CyclicBarrierDispose(barrier)
```

#### 8.4.8 CancellationTokenSource（取消令牌源）

```old8
// 创建取消令牌源
cts <- CreateCancellationTokenSource()

// 请求取消
Cancel(cts)

// 延时取消
CancelAfter(cts, 5000)  // 5秒后取消

// 释放资源
DisposeCancellationTokenSource(cts)
```

#### 8.4.9 并发工具函数

```old8
// 休眠（毫秒）
Sleep(1000)

// 获取当前线程 ID
threadId <- GetCurrentThreadId()

// 获取处理器数量
processors <- GetProcessorCount()
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
- 泛型支持（仅解释器模式支持，编译模式暂不支持）
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
- ✅ 泛型支持（泛型函数、泛型类）
- ✅ 异步/等待模式
- ✅ 多线程支持
- ✅ 异常处理
- ✅ 函数式编程（Lambda、高阶函数）
- ✅ 丰富的集合操作
