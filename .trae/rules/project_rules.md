# Old8Lang 规范

## 测试用 Old8Lang 代码文件 规范
生成 测试用 Old8Lang 代码文件 时，请使用 .old8 作为文件扩展名。
生成的 测试用代码文件 必须要符合 Old8Lang 语法规范。详情请看 /Old8Lang/Old8Lang.ebnf
测试时可使用 PrintLine 函数打印结果，方便查看。 注释为 // 而非 #

编译模式测试时，请放在 CompilerTests 目录下。
解释模式测试时，请放在 InterpreterTests 目录下。
语法测试时，请放在 SyntaxTests 目录下。

生成完测试用代码文件后，请也在 Old8Lang.XUnitTests 目录下添加对应的测试类。
如果发现有没有使用的测试用 Old8Lang 代码文件，请及时删除。

## 新语法添加 规范

1. 完成语法规则的添加和解析之后，必须先进行语法测试，确保新语法可以被正确解析。
2. 完成语法测试之后，进行解释模式测试，确保新语法在解释模式下可以正常运行。
3. 完成解释模式测试之后，进行编译模式测试，确保新语法在编译模式下可以正常运行。
4. 完成编译模式测试之后，更新 Old8Lang.ebnf 中的语法规则。

## Changelog 规范

- 在 CHANGELOG.en-US.md 和 CHANGELOG.zh-CN.md 书写每个版本的变更
- 对用户使用上无感知的改动建议（文档修补、微小的样式优化、代码风格重构等等）不要提及，保持 CHANGELOG 的内容有效性
- 非 Old8Lang 语法的改动（如工程化、构建工具、开发流程等）在 changelog 区块中写 "-"
- 用面向开发者的角度和叙述方式撰写 CHANGELOG，不描述修复细节，描述问题和对开发者的影响；描述用户的原始问题，而非你的解决方式


# Old8Lang 语法文档

## 1. 概述

Old8Lang 是一种静态类型的编程语言，具有类似 C#/Java 的语法结构，同时支持解释执行和编译执行。本文档详细介绍了 Old8Lang 的语法规则，包括词法、语法和语义。

## 2. 词法规则

### 2.1 标识符

标识符用于表示变量名、函数名、类名等，规则如下：
- 必须以字母或下划线开头
- 可以包含字母、数字和下划线
- 区分大小写

### 2.2 关键字

以下是 Old8Lang 的关键字：

```
if elif else for while switch case default func class return try catch finally import and or xor not true false in as
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

列表是一种动态数组，可以随时添加或删除元素：

```
list[1, 2, 3]
list["a", "b", "c"]
```

#### 3.2.3 字典

字典是一种键值对集合，使用花括号表示：

```
{"name": "Alice", "age": 30}
{1: "one", 2: "two"}
```

#### 3.2.4 元组

元组是一种固定长度的有序集合，可以包含不同类型的元素：

```
(1, "a", true)
("hello", 3.14)
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

### 4.4 赋值表达式

Old8Lang 使用 `<-` 作为赋值运算符：

```
a <- 123
b <- a + 1
```

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
list[1]
dict["key"]
```

### 4.7 函数调用表达式

使用圆括号 `()` 调用函数：

```
func()
func(a, b)
```

## 5. 语句

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
a:int <- 123
b:double <- 3.14
c:string <- "hello"
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
func add(a:int, b:int) -> {
    return a + b
}
```

#### 5.4.2 函数调用

```
result <- add(1, 2)
```

#### 5.4.3 Lambda 表达式

```
add <- (a:int, b:int) -> a + b
```

### 5.5 类声明与实例化

#### 5.5.1 类声明

```
class Person {
    name:string <- ""
    age:int <- 0
    
    func sayHello() -> {
        PrintLine("Hello, my name is " + name)
    }
}
```

#### 5.5.2 类实例化

```
p <- Person()
p.name <- "Alice"
p.age <- 30
p.sayHello()
```

### 5.6 异常处理

```
try {
    // 可能抛出异常的代码
} catch (e) {
    // 捕获异常并处理
} finally {
    // 无论是否异常都执行
}
```

### 5.7 导入语句

```
import "module"
import module
```

## 6. 类型系统

### 6.1 类型转换

使用 `as` 关键字进行类型转换：

```
a:int <- 123
b:double <- a as double
```

### 6.2 类型推断

Old8Lang 支持有限的类型推断，例如在变量声明时：

```
a <- 123  // 推断为 int 类型
b <- 3.14  // 推断为 double 类型
```

## 7. 示例代码

### 7.1 基本类型和表达式

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

### 7.2 控制流

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

### 7.3 函数和类

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

### 7.4 异常处理

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

## 8. 特殊语法

### 8.1 字符串模板

使用 `$` 符号和花括号 `{}` 来创建字符串模板：

```
name <- "Alice"
age <- 30
message <- $("My name is {name}, I'm {age} years old.")
PrintLine(message)
```

### 8.2 列表推导式

```
numbers <- [1, 2, 3, 4, 5]
squares <- [x * x for x in numbers]
```

### 8.3 范围表达式

```
// 创建一个从 1 到 10 的范围
range <- [1~10]

// 使用范围进行循环
for i in range {
    PrintLine(i)
}
```

## 9. 编译与执行

### 9.1 解释执行

使用解释器执行 Old8Lang 代码：

```bash
dotnet run --project Old8Lang.App -- -f file.old8
```

### 9.2 编译执行

使用编译器将 Old8Lang 代码编译为可执行文件：

```bash
dotnet run --project Old8Lang.App -- -f file.old8
```
