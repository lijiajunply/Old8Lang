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
func add(a:int, b:int) {
    return a + b
}
// 或者这样
add(a:int, b:int) -> {
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
    
    func sayHello() {
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

class Test {
    func init(a:int, b:int) {
        PrintLine("init: " + a + ", " + b)
    }
}

t <- Test(1, 2)
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

Old8Lang 支持类型推断，例如在变量声明时：

```
a <- 123  // 推断为 int 类型
b <- 3.14  // 推断为 double 类型
```

> ![TIP]
>
> Old8Lang 为动态类型语言，变量的类型可以在运行时改变。但如果进行了类型假注（例如 a:int 为 int 类型），就需要保持类型不变。

### 6.3 类型检查

Old8Lang 支持类型检查，例如在赋值时：

```
a:int <- 123
b:double <- a  // 正确，a 可以隐式转换为 double
c:string <- a  // 错误，不能将 int 隐式转换为 string
```

### 6.4 类型转换规则

Old8Lang 支持以下类型转换规则：

| 源类型 | 目标类型 | 规则 |
|--------|----------|------|
| int    | double   | 直接转换 |
| double | int      | 取整 |
| string | int/double | 解析数值 |
| int/double | string | 转换为字符串 |

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

使用 `$()` 语法来创建字符串模板，在字符串内部支持 `${}` 占位符和 `{{}}` 转义：

```
name <- "Alice"
age <- 30
message <- $("My name is {name}, I'm {age} years old.")
PrintLine(message)
```

新语法支持：
1. `$(".")` - 基本字符串模板
2. `${}` - 在模板中嵌入表达式
3. `{{` - 转义 `{` 字符
4. `}}` - 转义 `}` 字符

例如：
```
// 基本使用
name <- "Alice"
result <- $("Hello, {name}")

// 转义大括号
escaped <- $("This is {{escaped}} bracket")

// 混合使用
mixed <- $("Name: {name}, Escaped: {{escaped}}")
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
dotnet run --project Old8Lang.App -- -c file.old8
```

> ![TIP]
>
> 编译执行时，建议使用类型假注。函数书写时必须使用类型假注

## 10. 总结

Old8Lang 是一种功能丰富的编程语言，具有清晰的语法结构和强大的表达能力。本文档详细介绍了 Old8Lang 的语法规则，包括词法、语法和语义。通过学习本文档，您应该能够理解和编写 Old8Lang 代码。

如需了解更多关于 Old8Lang 的信息，请参考项目中的其他文档：

- [Interpreter.md](Docs/Interpreter.md) - 解释器实现
- [Lexer.md](Docs/Lexer.md) - 词法分析器实现
- [Parser.md](Docs/Parser.md) - 语法分析器实现
- [Tutorial.md](Docs/Tutorial.md) - 入门教程
