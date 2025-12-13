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
if elif else for while switch case default func class return try catch finally import and or xor not true false in as throw
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
list[1]
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
list[1] <- "new item"
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

1. **参数类型注解是强制的**：所有函数参数必须显式声明类型
2. **返回类型注解是强制的**：函数返回类型必须显式声明，不能通过return推断
3. **Lambda参数类型注解是强制的**：Lambda表达式的参数必须有类型注解
4. **Lambda返回类型可以推断**：Lambda的返回类型可以从表达式或return语句推断

| 特性 | 解释器模式 | 编译模式 |
|------|----------|---------|
| 函数参数类型注解 | 可选 | **必须** |
| 函数返回类型注解 | 可选（可推断） | **必须** |
| Lambda参数类型注解 | 可选 | **必须** |
| Lambda返回类型注解 | 可选（可推断） | 可选（可推断） |

正确示例（编译模式）：

```old8
// 完整类型注解的函数
func add(a:int, b:int) -> int {
    return a + b
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

// 错误：参数缺少类型
func add(a, b) -> int {
    return a + b
}

// 错误：Lambda参数缺少类型
transform <- (x) -> x * 2
```

#### 5.4.3 函数调用

```
result <- add(1, 2)
```

#### 5.4.4 Lambda 表达式

```
add <- (a:int, b:int) -> a + b
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

#### 5.5.2 类实例化

```
p <- Person("Alice", 30)
p.sayHello()

t <- Test(1, 2)  // 调用带有参数的构造函数
```

### 5.6 异常处理

#### 5.6.1 try-catch-finally 语句

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

```
import "module"
import module
```

### 5.8 原生库导入语句

Old8Lang 支持导入原生 DLL 库中的类和方法，以便调用外部功能。原生库导入语句使用方括号 `[]` 包围，支持以下几种形式：

#### 5.8.1 单个方法导入 (nativeStatement)

**语法规则**：`[import "dllName" className methodName alias?]`

**描述**：导入原生 DLL 中的特定方法，并可选地指定别名。

**示例**：

```
[import "console.dll" console Write print]
[import "console.dll" console WriteLine printline]
```

#### 5.8.2 批量导入所有方法 (新增)

**语法规则**：`[import "dllName" className *]`

**描述**：导入原生 DLL 中某个类的所有公共静态方法。大大简化了标准库的引入。

**示例**：

```
// 导入 MathLib 的所有方法（53个方法只需1行）
[import "Old8LangLib" MathLib *]

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

#### 5.8.3 选择性导入多个方法 (新增)

**语法规则**：`[import "dllName" className { method1, method2, method3 }]`

**描述**：按需导入原生 DLL 中某个类的特定方法，用逗号分隔多个方法名。

**示例**：

```
// 只导入 Time 类的两个方法
[import "Old8LangLib" Time { GetTimeNow, TimeStamp }]

// 导入后可以使用这两个方法
timeStr <- GetTimeNow("yyyy-MM-dd HH:mm:ss")
stamp <- TimeStamp()
```

**优势**：
- 按需导入，避免命名冲突
- 代码更明确，容易理解使用了哪些方法
- 减少不必要的方法导入

#### 5.8.4 静态类导入 (nativeStatic)

**语法规则**：`[import "dllName" className] -> alias`

**描述**：导入原生 DLL 中的静态类或静态方法集，并指定别名。

**示例**：

```
[import "Math.dll" Math] -> Math
```

#### 5.8.5 完整类导入 (nativeClass)

**语法规则**：`[import "dllName" className]`

**描述**：导入原生 DLL 中的完整类，包括所有方法和属性。

**示例**：

```
[import "Person.dll" Person]
```

#### 5.8.6 带别名的类导入 (nativeClass with alias)

**语法规则**：`[import "dllName" className as alias]`

**描述**：导入原生 DLL 中的完整类，并使用指定的别名进行注册。这使得类导入更加明确，并且可以避免命名冲突。

**示例**：

```
// 导入 Old8LangLib.Csv 类，并命名为 CsvUtil
[import "Old8LangLib" Csv as CsvUtil]

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

## 9. 编译与执行

### 9.1 解释模式

解释模式下，Old8Lang 代码会逐条解释执行，无需编译。

### 9.2 编译模式

编译模式下，Old8Lang 代码会先被编译成中间代码，然后再执行。

## 10. 总结

Old8Lang 是一种功能丰富的动态类型编程语言，具有清晰的语法结构和强大的表达能力。本文档详细介绍了 Old8Lang 的语法规则，包括词法、语法和语义。通过学习本文档，您应该能够理解和编写 Old8Lang 代码。