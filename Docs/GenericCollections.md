# Old8Lang 泛型集合类型指南

## 概述

Old8Lang 1.0.0 rc4 引入了泛型集合类型支持，允许开发者为列表、数组和字典指定元素类型，在编译器模式下提供编译时类型检查。

## 支持的泛型集合类型

### 1. `list<T>` - 泛型列表

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

### 2. `array<T>` - 泛型数组

数组是固定大小的集合，使用方括号 `[]` 表示。

```old8
// 基本用法
numbers:array<int> <- [1, 2, 3, 4, 5]
names:array<string> <- ["Alice", "Bob", "Charlie"]
flags:array<bool> <- [true, false, true]
prices:array<double> <- [1.5, 2.5, 3.5]

// 访问元素
PrintLine(numbers[0].ToStr())  // 输出: 1

// 编译器模式：使用 .Length 属性
PrintLine(numbers.Length.ToStr())  // 仅编译器模式

// 解释器模式：使用 .Length() 或 .Count() 方法
PrintLine(numbers.Length().ToStr())  // 解释器模式
PrintLine(numbers.Count().ToStr())   // 解释器模式（推荐）

// 空数组
empty:array<int> <- []
```

**注意**：数组的 `.Length` 访问方式在两种模式下不同：
- **编译器模式** (`-c`)：`.Length` 是属性（C# 数组的 Length 属性）
- **解释器模式** (`-f`)：需要使用 `.Length()` 或 `.Count()` 方法调用

### 3. `dict<K, V>` - 泛型字典

字典是键值对集合，使用带冒号的花括号 `{"key": value}` 表示。

```old8
// 基本用法
ages:dict<string, int> <- {"Alice": 30, "Bob": 25, "Charlie": 35}
scores:dict<string, double> <- {"math": 95.5, "english": 88.0}
flags:dict<string, bool> <- {"enabled": true, "debug": false}

// 访问元素
PrintLine(ages["Alice"].ToStr())  // 输出: 30
PrintLine(ages.Count.ToStr())      // 输出: 3

// 其他键类型
ids:dict<int, string> <- {1: "Alice", 2: "Bob", 3: "Charlie"}
```

## 嵌套泛型类型

泛型集合类型支持任意深度的嵌套。

### 嵌套列表

```old8
// 二维矩阵
matrix:list<list<int>> <- {
    {1, 2, 3},
    {4, 5, 6},
    {7, 8, 9}
}

// 访问元素
PrintLine(matrix[0][0].ToStr())  // 输出: 1
PrintLine(matrix[1][2].ToStr())  // 输出: 6
```

### 字典与列表组合

```old8
// 字典的值为列表
groups:dict<string, list<int>> <- {
    "even": {2, 4, 6, 8},
    "odd": {1, 3, 5, 7, 9}
}

// 访问元素
PrintLine(groups["even"][0].ToStr())  // 输出: 2
PrintLine(groups["odd"].Count.ToStr()) // 输出: 5
```

### 列表与数组组合

```old8
// 列表的元素为数组
data:list<array<int>> <- {
    [1, 2, 3],
    [4, 5, 6],
    [7, 8, 9]
}

// 访问元素
PrintLine(data[0][1].ToStr())  // 输出: 2
```

## 编译时类型检查

在**编译器模式** (`-c`) 下，泛型集合类型会进行严格的类型检查。

### 列表类型检查

```old8
// ✅ 正确：所有元素类型一致
items:list<int> <- {1, 2, 3}

// ❌ 错误：类型不匹配
items:list<int> <- {1, "hello", 3}
// 编译错误：变量 'items' 列表元素类型不匹配: 第 1 个元素期望类型 int,实际类型 string
```

### 数组类型检查

```old8
// ✅ 正确：所有元素类型一致
arr:array<string> <- ["a", "b", "c"]

// ❌ 错误：类型不匹配
arr:array<string> <- [1, 2, 3]
// 编译错误：变量 'arr' 数组元素类型不匹配: 第 0 个元素期望类型 string,实际类型 int
```

### 字典类型检查

```old8
// ✅ 正确：键和值类型都匹配
ages:dict<string, int> <- {"Alice": 30, "Bob": 25}

// ❌ 错误：值类型不匹配
ages:dict<string, int> <- {"Alice": 30, "Bob": "twenty-five"}
// 编译错误：变量 'ages' 字典值类型不匹配: 第 1 个值期望类型 int,实际类型 string

// ❌ 错误：键类型不匹配
ages:dict<string, int> <- {123: 30, "Bob": 25}
// 编译错误：变量 'ages' 字典键类型不匹配: 第 0 个键期望类型 string,实际类型 int
```

## 向后兼容性

Old8Lang 的泛型集合类型设计遵循完全向后兼容原则。

### 不带类型注解（传统方式）

```old8
// 混合类型列表（完全支持）
mixed <- {1, "hello", 3.14, true}
PrintLine(mixed[0].ToStr())  // 输出: 1
PrintLine(mixed[1].ToStr())  // 输出: hello

// 混合类型数组（完全支持）
arr <- [1, "world", false]
PrintLine(arr.Count().ToStr())  // 输出: 3

// 混合类型字典（完全支持）
data <- {"a": 1, "b": "text", "c": 3.14}
PrintLine(data["b"].ToStr())  // 输出: text
```

### 带类型注解（新特性）

```old8
// 类型安全的集合（编译器模式下检查）
items:list<int> <- {1, 2, 3}
names:array<string> <- ["Alice", "Bob"]
ages:dict<string, int> <- {"Alice": 30}
```

### 兼容性保证

- ✅ 所有不带类型注解的现有代码继续正常工作
- ✅ 泛型类型注解是**可选特性**，不强制使用
- ✅ 解释器模式保持动态类型的灵活性
- ✅ 编译器模式提供更严格的类型安全

## 执行模式差异

### 编译器模式 (`-c`)

```bash
dotnet run --project Old8Lang.App -- -c program.old8
```

特点：
- ✅ 进行编译时类型检查
- ✅ 捕获类型不匹配错误
- ✅ 提供详细的错误信息
- ⚠️ 数组使用 `.Length` 属性（无括号）

示例：
```old8
// 编译器模式
items:list<int> <- {1, 2, 3}        // ✅ 通过
items:list<int> <- {1, "hello", 3}  // ❌ 编译错误

arr:array<int> <- [1, 2, 3]
PrintLine(arr.Length.ToStr())  // ✅ 使用 .Length 属性
```

### 解释器模式 (`-f`)

```bash
dotnet run --project Old8Lang.App -- -f program.old8
```

特点：
- ✅ 不强制类型检查
- ✅ 支持混合类型集合
- ✅ 类型注解仅作为元信息
- ⚠️ 数组使用 `.Length()` 或 `.Count()` 方法（带括号）

示例：
```old8
// 解释器模式
items:list<int> <- {1, 2, 3}        // ✅ 通过
items <- {1, "hello", true}         // ✅ 通过（混合类型）

arr:array<int> <- [1, 2, 3]
PrintLine(arr.Length().ToStr())  // ✅ 使用 .Length() 方法
PrintLine(arr.Count().ToStr())   // ✅ 使用 .Count() 方法（推荐）
```

## 最佳实践

### 1. 选择合适的执行模式

- **开发调试**：使用解释器模式 (`-f`)，享受灵活的动态类型
- **生产环境**：使用编译器模式 (`-c`)，获得类型安全保障

### 2. 类型注解的使用时机

```old8
// ✅ 推荐：明确的业务数据使用类型注解
users:list<User> <- fetchUsers()
ages:dict<string, int> <- {"Alice": 30, "Bob": 25}

// ✅ 推荐：临时数据可以不使用类型注解
temp <- {1, 2, 3}
mixed <- {1, "hello", true}
```

### 3. 跨模式兼容代码

如果代码需要同时在两种模式下运行，注意数组长度访问：

```old8
// ✅ 推荐：使用 .Count() 方法（两种模式都支持）
arr <- [1, 2, 3]
count <- arr.Count()

// ⚠️ 不推荐：.Length 在两种模式下行为不同
// 编译器模式：arr.Length（属性）
// 解释器模式：arr.Length()（方法）
```

### 4. 错误处理

```old8
// 编译器模式：在编译时捕获类型错误
items:list<int> <- {1, 2, 3}

// 如果不确定元素类型，不使用类型注解
dynamic <- {1, "hello", 3.14}  // 动态类型，运行时处理
```

## 示例代码

### 示例 1：学生成绩管理

```old8
// 学生信息字典
students:dict<string, int> <- {
    "Alice": 95,
    "Bob": 88,
    "Charlie": 92
}

// 遍历并打印成绩
for student in students {
    PrintLine(student + ": " + students[student].ToStr())
}
```

### 示例 2：二维矩阵操作

```old8
// 定义 3x3 矩阵
matrix:list<list<int>> <- {
    {1, 2, 3},
    {4, 5, 6},
    {7, 8, 9}
}

// 计算对角线和
sum <- 0
for i in 0~3 {
    sum <- sum + matrix[i][i]
}

PrintLine("对角线和: " + sum.ToStr())  // 输出: 15
```

### 示例 3：分组数据

```old8
// 将数字按奇偶性分组
numbers <- {1, 2, 3, 4, 5, 6, 7, 8, 9, 10}
groups:dict<string, list<int>> <- {
    "even": {},
    "odd": {}
}

for num in numbers {
    if num % 2 == 0 {
        groups["even"] <- groups["even"] + {num}
    } else {
        groups["odd"] <- groups["odd"] + {num}
    }
}

PrintLine("偶数: " + groups["even"].ToStr())
PrintLine("奇数: " + groups["odd"].ToStr())
```

## 常见问题

### Q1: 泛型类型注解是强制的吗？

**A**: 不是。泛型类型注解是**可选特性**。不使用类型注解的代码继续正常工作。

### Q2: 解释器模式下会进行类型检查吗？

**A**: 不会。在解释器模式下，泛型类型注解仅作为元信息，不影响运行时行为。只有编译器模式下才会进行严格的类型检查。

### Q3: 为什么数组的 Length 在两种模式下不同？

**A**: 这是由于底层实现的差异：
- **编译器模式**：数组编译为 C# 的 `object[]`，使用 `.Length` 属性
- **解释器模式**：数组是 Old8Lang 的 `ArrayLangValue`，使用 `.Length()` 方法

推荐使用 `.Count()` 方法，在两种模式下都支持。

### Q4: 可以在函数参数中使用泛型集合类型吗？

**A**: 目前暂不支持。当前版本的泛型集合类型主要用于变量声明的类型注解。函数参数的泛型支持将在未来版本中添加。

### Q5: 混合类型的集合会有性能问题吗？

**A**: 在解释器模式下，混合类型集合与单一类型集合性能相同。在编译器模式下，建议使用类型注解以获得更好的性能优化。

## 总结

泛型集合类型是 Old8Lang 1.0.0 rc4 的重要特性，它在保持向后兼容的前提下，为编译器模式提供了编译时类型安全保障。开发者可以根据项目需求选择使用类型注解，享受类型检查带来的好处，同时不影响现有代码的正常运行。
