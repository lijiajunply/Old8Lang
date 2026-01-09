# Old8Lang 语言特性

本文档汇总 Old8Lang 的高级语言特性，包括模式匹配、命名参数、泛型集合和泛型类型推断。

## 目录

- [模式匹配](#模式匹配)
  - [值匹配](#值匹配)
  - [变量绑定](#变量绑定)
  - [通配符](#通配符)
  - [元组解构](#元组解构)
  - [类型匹配](#类型匹配)
  - [守卫条件](#守卫条件)
  - [范围匹配](#范围匹配)
- [命名参数](#命名参数)
- [泛型集合类型](#泛型集合类型)
  - [list<T>](#listt---泛型列表)
  - [array<T>](#arrayt---泛型数组)
  - [dict<K,V>](#dictkv---泛型字典)
- [泛型类型推断](#泛型类型推断)

---

## 模式匹配

Old8Lang 提供强大的模式匹配功能，支持多种模式类型。

### 基本语法

```old8
match expression {
    case pattern1 -> result1
    case pattern2 -> result2
    default -> defaultResult  // 可选的默认分支
}
```

### 支持的模式类型

1. ✅ **值匹配** - 匹配特定值
2. ✅ **变量绑定** - 捕获并绑定匹配的值
3. ✅ **通配符** - 匹配任意值
4. ✅ **元组解构** - 解构元组并匹配其元素
5. ✅ **类型匹配** - 根据值的类型进行匹配
6. ✅ **守卫条件** - 为模式添加额外的条件约束
7. ✅ **范围匹配** - 匹配数值范围

---

### 值匹配

匹配特定的值。

**示例**:
```old8
result <- match x {
    case 0 -> "zero"
    case 1 -> "one"
    case 2 -> "two"
    case _ -> "other"
}
```

---

### 变量绑定

捕获匹配的值并绑定到变量。

**示例**:
```old8
result <- match value {
    case x -> $"The value is {x}"
}
```

---

### 通配符

匹配任意值，不进行绑定。

**语法**:
```old8
case _ -> expression
// 或使用 default
default -> expression
```

**示例**:
```old8
result <- match score {
    case 100 -> "Perfect!"
    case _ -> "Try again"
}
```

---

### 元组解构

解构元组并匹配其元素。元组元素可以是：
- **值匹配**：匹配特定值（如 `0`, `"hello"`）
- **变量绑定**：捕获元素值（如 `x`, `y`）
- **通配符**：忽略该元素（使用 `_`）

**示例**:
```old8
point <- (5, 0)
result <- match point {
    case (0, 0) -> "Origin"
    case (x, 0) -> $"On X-axis: {x}"
    case (0, y) -> $"On Y-axis: {y}"
    case (x, y) -> $"Point: ({x}, {y})"
}
// result = "On X-axis: 5"

// 使用通配符忽略不关心的元素
result2 <- match point {
    case (_, 0) -> "On X-axis"
    case (0, _) -> "On Y-axis"
    case (_, _) -> "Other"
}
```

---

### 类型匹配

根据值的类型进行匹配，并可选择性地绑定变量。

**语法**:
```old8
case varName:typeName -> expression
```

**支持的类型**:
- `int`, `double`, `string`, `bool`, `char`
- `list`, `dict`, `tuple`, `array`

**示例**:
```old8
result <- match value {
    case x:int -> $"Integer: {x}"
    case s:string -> $"String: {s}"
    case b:bool -> $"Boolean: {b}"
    case _ -> "Unknown type"
}
```

---

### 守卫条件

为类型匹配添加额外的条件约束。守卫条件必须是布尔表达式。

**语法**:
```old8
case varName:typeName if condition -> expression
```

**示例**:
```old8
result <- match value {
    case x:int if x > 0 -> "Positive integer"
    case x:int if x < 0 -> "Negative integer"
    case x:int -> "Zero"
    case _ -> "Not an integer"
}
```

**复杂守卫条件**:
```old8
result <- match age {
    case x:int if x >= 0 and x <= 17 -> "Minor"
    case x:int if x >= 18 and x <= 64 -> "Adult"
    case x:int if x >= 65 -> "Senior"
    case _ -> "Invalid"
}
```

---

### 范围匹配

匹配数值是否在指定范围内。支持四种边界模式。

**语法**:
```old8
case [start~end] -> expression        // 包含两端 [start, end]
case [start~<end] -> expression       // 包含start，排除end [start, end)
case [start>~end] -> expression       // 排除start，包含end (start, end]
case [start>~<end] -> expression      // 排除两端 (start, end)
```

**示例**:
```old8
age <- 15
result <- match age {
    case [0~12] -> "Child"
    case [13~19] -> "Teen"
    case [20~64] -> "Adult"
    case [65~90] -> "Senior"
    default -> "Invalid age"
}
// result = "Teen"

// 使用排除边界
score <- 90
result2 <- match score {
    case [0~<60] -> "F"        // [0, 60)
    case [60~<70] -> "D"       // [60, 70)
    case [70~<80] -> "C"       // [70, 80)
    case [80~<90] -> "B"       // [80, 90)
    case [90~100] -> "A"       // [90, 100]
    default -> "Invalid"
}
// result2 = "A"
```

**注意事项**:
- 范围匹配仅支持数值类型（`int`, `double`, `char`）
- 起始值和结束值都可以是表达式

---

### 模式匹配最佳实践

1. **将更具体的模式放在前面**
2. **将通配符 `_` 或 `default` 放在最后作为兜底**
3. **确保至少有一个模式能匹配**，否则会抛出运行时错误

**推荐做法**: 总是提供 `default` 分支或通配符 `_` 作为兜底。

---

### 模式匹配限制

1. **编译器模式支持**:
   - ✅ 解释器模式（`-f`）：完全支持所有模式匹配功能
   - ⚠️ 编译器模式（`-c`）：仅支持基本的值匹配、变量绑定和通配符

2. **守卫条件限制**:
   - 守卫条件仅支持类型匹配模式（`case x:int if ...`）
   - 不支持单独的守卫条件（`case x if ...`）

3. **元组解构限制**:
   - 元组模式必须至少包含 2 个元素
   - 元素数量必须精确匹配

---

## 命名参数

Old8Lang 支持命名参数（Named Arguments）功能，允许在函数调用时通过参数名指定参数值。

### 语法

```old8
函数名(参数名: 值, 参数名: 值, ...)
```

### 特性

1. **位置参数和命名参数混合使用**: 位置参数必须出现在所有命名参数之前
2. **参数顺序无关**: 命名参数可以以任意顺序出现
3. **跳过默认参数**: 可以跳过有默认值的参数
4. **不可重复**: 同一个参数不能既作为位置参数又作为命名参数

### 基本示例

```old8
// 定义函数
func greet(name:string, age:int, message:string) -> void {
    PrintLine(message + ", " + name + "! Age: " + age.ToStr())
}

// 完全位置参数
greet("Alice", 25, "Hello")

// 完全命名参数
greet(name: "Bob", age: 30, message: "Hi")

// 混合使用（位置参数在前）
greet("Charlie", age: 35, message: "Good morning")

// 命名参数改变顺序
greet(age: 28, name: "David", message: "Welcome")
```

### 带默认值的命名参数

```old8
func display(title:string, width: 800, height: 600) -> void {
    PrintLine("Title: " + title + ", Width: " + width.ToStr() + ", Height: " + height.ToStr())
}

// 只提供必需参数和一个命名参数
display("Window", height: 1080)

// 只提供必需参数，其余使用默认值
display("Small Window")

// 提供所有参数，但顺序不同
display(height: 720, width: 1280, title: "HD Window")
```

### 规则和限制

#### 1. 位置参数必须在命名参数之前

```old8
// ✅ 正确
func_call(1, 2, c: 3, d: 4)

// ❌ 错误：位置参数不能出现在命名参数之后
func_call(a: 1, 2, 3)
```

#### 2. 不可重复指定参数

```old8
// ❌ 错误：参数 'x' 既作为位置参数又作为命名参数
func_call(1, x: 2)  // 假设第一个参数名为 x
```

#### 3. params 参数不支持命名参数

```old8
func sum(params values:array<int>) -> int {
    // ...
}

// ✅ 正确
sum(1, 2, 3, 4)

// ❌ 错误
sum(values: {1, 2, 3})
```

### 命名参数优势

1. **提高代码可读性**: 参数名称明确表达参数的含义
2. **灵活的参数顺序**: 不需要记住参数的确切顺序
3. **便于跳过默认参数**: 只需指定需要修改的参数
4. **减少错误**: 参数名称匹配降低了参数顺序错误的风险

### 注意事项

- 命名参数功能目前在**解释器模式**下完全支持
- 编译器模式的支持将在后续版本中添加

---

## 泛型集合类型

Old8Lang 支持泛型集合类型，允许为列表、数组和字典指定元素类型，在编译器模式下提供编译时类型检查。

### list\<T> - 泛型列表

列表是动态大小的集合，使用花括号 `{}` 表示。

```old8
// 基本用法
numbers:list<int> <- {1, 2, 3, 4, 5}
names:list<string> <- {"Alice", "Bob", "Charlie"}
flags:list<bool> <- {true, false, true}
prices:list<double> <- {1.5, 2.5, 3.5}

// 访问元素
PrintLine(numbers[0].ToStr())  // 输出: 1
PrintLine(names.Count.ToStr())  // 输出: 3

// 空列表
empty:list<int> <- {}
```

### array\<T> - 泛型数组

数组是固定大小的集合，使用方括号 `[]` 表示。

```old8
// 基本用法
numbers:array<int> <- [1, 2, 3, 4, 5]
names:array<string> <- ["Alice", "Bob", "Charlie"]

// 访问元素
PrintLine(numbers[0].ToStr())  // 输出: 1

// 编译器模式：使用 .Length 属性
PrintLine(numbers.Length.ToStr())  // 仅编译器模式

// 解释器模式：使用 .Length() 或 .Count() 方法
PrintLine(numbers.Length().ToStr())  // 解释器模式
PrintLine(numbers.Count().ToStr())   // 解释器模式（推荐）
```

**注意**: 数组的 `.Length` 访问方式在两种模式下不同：
- **编译器模式** (`-c`): `.Length` 是属性（C# 数组的 Length 属性）
- **解释器模式** (`-f`): 需要使用 `.Length()` 或 `.Count()` 方法调用

### dict\<K,V> - 泛型字典

字典是键值对集合，使用带冒号的花括号 `{"key": value}` 表示。

```old8
// 基本用法
ages:dict<string, int> <- {"Alice": 30, "Bob": 25, "Charlie": 35}
scores:dict<string, double> <- {"math": 95.5, "english": 88.0}
flags:dict<string, bool> <- {"enabled": true, "debug": false}

// 访问元素
PrintLine(ages["Alice"].ToStr())  // 输出: 30

// 其他键类型
ids:dict<int, string> <- {1: "Alice", 2: "Bob", 3: "Charlie"}
```

### 嵌套泛型类型

泛型集合类型支持任意深度的嵌套。

```old8
// 二维矩阵
matrix:list<list<int>> <- {
    {1, 2, 3},
    {4, 5, 6},
    {7, 8, 9}
}

// 字典的值为列表
groups:dict<string, list<int>> <- {
    "even": {2, 4, 6, 8},
    "odd": {1, 3, 5, 7, 9}
}

// 列表的元素为数组
data:list<array<int>> <- {
    [1, 2, 3],
    [4, 5, 6],
    [7, 8, 9]
}
```

### 编译时类型检查

在**编译器模式** (`-c`) 下，泛型集合类型会进行严格的类型检查。

```old8
// ✅ 正确：所有元素类型一致
items:list<int> <- {1, 2, 3}

// ❌ 错误：类型不匹配
items:list<int> <- {1, "hello", 3}
// 编译错误：列表元素类型不匹配
```

### 向后兼容性

Old8Lang 的泛型集合类型设计遵循完全向后兼容原则。

```old8
// 不带类型注解（传统方式）- 完全支持
mixed <- {1, "hello", 3.14, true}

// 带类型注解（新特性）- 编译时检查
items:list<int> <- {1, 2, 3}
```

**兼容性保证**:
- ✅ 所有不带类型注解的现有代码继续正常工作
- ✅ 泛型类型注解是**可选特性**，不强制使用
- ✅ 解释器模式保持动态类型的灵活性
- ✅ 编译器模式提供更严格的类型安全

### 执行模式差异

**编译器模式** (`-c`):
- ✅ 进行编译时类型检查
- ✅ 捕获类型不匹配错误
- ⚠️ 数组使用 `.Length` 属性（无括号）

**解释器模式** (`-f`):
- ✅ 不强制类型检查
- ✅ 支持混合类型集合
- ⚠️ 数组使用 `.Length()` 或 `.Count()` 方法（带括号）

---

## 泛型类型推断

Old8Lang 支持从函数调用参数自动推断泛型类型参数。

### 自动类型推断

用户现在可以省略泛型类型参数，由编译器自动推断：

```old8
func identity<T>(value:T) -> T {
    return value
}

// 以前：需要显式指定类型
result1 <- identity<int>(42)

// 现在：自动推断 T=int
result2 <- identity(42)
```

### 支持的推断场景

#### 从字面量推断

```old8
identity(42)        // 推断 T=int
identity("hello")   // 推断 T=string
identity(3.14)      // 推断 T=double
identity(true)      // 推断 T=bool
```

#### 从变量推断

```old8
x <- 100
result <- identity(x)  // 推断 T=int
```

#### 多个类型参数

```old8
func makePair<K, V>(key:K, value:V) -> string {
    return key.ToStr() + ":" + value.ToStr()
}

pair <- makePair("age", 25)  // 推断 K=string, V=int
```

#### 同一类型参数在多个位置

```old8
func add<T>(a:T, b:T) -> string {
    return a.ToStr() + "+" + b.ToStr()
}

sum <- add(10, 20)  // 推断 T=int
```

#### 嵌套函数调用

```old8
func wrap<T>(value:T) -> string {
    return "[" + value.ToStr() + "]"
}

// 推断内层和外层的类型
result <- wrap(identity(42))  // 先推断 identity 的 T=int，再推断 wrap 的 T=int
```

### 向后兼容性

- ✅ 显式类型参数语法仍然有效
- ✅ 现有泛型函数测试全部通过
- ✅ 不影响非泛型函数的调用

### 已知限制

1. **执行副作用**: 在推断嵌套函数调用的类型时，会实际执行内层函数
2. **复杂类型**: 目前主要支持基本类型和简单泛型类型
3. **类型约束**: 推断过程会验证类型约束，但不会尝试找到满足约束的最佳类型

---

## 总结

Old8Lang 提供了丰富的语言特性：

- **模式匹配**: 强大而灵活的控制流机制
- **命名参数**: 提高代码可读性和灵活性
- **泛型集合**: 编译时类型安全保障
- **泛型推断**: 简化泛型函数调用

这些特性使 Old8Lang 代码更加简洁、安全和表达力更强。

更多信息请参考：
- [Old8Lang_Grammar.md](Old8Lang_Grammar.md) - 完整语法参考
- [API_REFERENCE.md](API_REFERENCE.md) - API 文档
- [FAQ.md](FAQ.md) - 常见问题

