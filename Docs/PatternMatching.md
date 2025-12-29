# Old8Lang 模式匹配增强功能文档

## 概述

Old8Lang 的模式匹配功能已得到显著增强，现在支持以下模式类型：

1. ✅ **值匹配** - 匹配特定值
2. ✅ **变量绑定** - 捕获并绑定匹配的值
3. ✅ **通配符** - 匹配任意值
4. ✅ **元组解构** - 解构元组并匹配其元素
5. ✅ **类型匹配** - 根据值的类型进行匹配
6. ✅ **守卫条件** - 为模式添加额外的条件约束
7. ✅ **范围匹配** - 匹配数值范围
8. ✅ **default 分支** - 作为默认情况的语法糖

## 语法

### 基本语法

```old8
match expression {
    case pattern1 -> result1
    case pattern2 -> result2
    ...
    default -> defaultResult  // 可选的默认分支
}
```

## 模式类型详解

### 1. 值匹配

匹配特定的值。

**语法：**
```old8
case value -> expression
```

**示例：**
```old8
result <- match x {
    case 0 -> "zero"
    case 1 -> "one"
    case 2 -> "two"
    case _ -> "other"
}
```

### 2. 变量绑定

捕获匹配的值并绑定到变量。

**语法：**
```old8
case varName -> expression  // varName 可在 expression 中使用
```

**示例：**
```old8
result <- match value {
    case x -> $"The value is {x}"
}
```

### 3. 通配符

匹配任意值，不进行绑定。

**语法：**
```old8
case _ -> expression
```

或使用 `default`:
```old8
default -> expression
```

**示例：**
```old8
result <- match score {
    case 100 -> "Perfect!"
    case _ -> "Try again"
}
```

### 4. 元组解构匹配

解构元组并匹配其元素。元组元素可以是：
- **值匹配**：匹配特定值（如 `0`, `"hello"`）
- **变量绑定**：捕获元素值（如 `x`, `y`）
- **通配符**：忽略该元素（使用 `_`）

**语法：**
```old8
case (pattern1, pattern2, ...) -> expression
```

**示例：**
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

**注意事项：**
- 元组模式必须至少包含 2 个元素
- 元素数量必须与被匹配的元组元素数量一致

### 5. 类型匹配

根据值的类型进行匹配，并可选择性地绑定变量。

**语法：**
```old8
case varName:typeName -> expression
```

**支持的类型：**
- `int` - 整数
- `double` - 浮点数
- `string` - 字符串
- `bool` - 布尔值
- `char` - 字符
- `list` - 列表
- `dict` - 字典
- `tuple` - 元组
- `array` - 数组

**示例：**
```old8
result <- match value {
    case x:int -> $"Integer: {x}"
    case s:string -> $"String: {s}"
    case b:bool -> $"Boolean: {b}"
    case _ -> "Unknown type"
}
```

### 6. 守卫条件

为类型匹配添加额外的条件约束。守卫条件必须是布尔表达式。

**语法：**
```old8
case varName:typeName if condition -> expression
```

**示例：**
```old8
result <- match value {
    case x:int if x > 0 -> "Positive integer"
    case x:int if x < 0 -> "Negative integer"
    case x:int -> "Zero"
    case _ -> "Not an integer"
}
```

**复杂守卫条件：**
```old8
result <- match age {
    case x:int if x >= 0 and x <= 17 -> "Minor"
    case x:int if x >= 18 and x <= 64 -> "Adult"
    case x:int if x >= 65 -> "Senior"
    case _ -> "Invalid"
}
```

### 7. 范围匹配

匹配数值是否在指定范围内。支持四种边界模式：

**语法：**
```old8
case [start~end] -> expression        // 包含两端 [start, end]
case [start~<end] -> expression       // 包含start，排除end [start, end)
case [start>~end] -> expression       // 排除start，包含end (start, end]
case [start>~<end] -> expression      // 排除两端 (start, end)
```

**示例：**
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

**注意事项：**
- 范围匹配仅支持数值类型（`int`, `double`, `char`）
- 起始值和结束值都可以是表达式，会在匹配时计算

## 混合使用示例

### 示例 1: 元组解构 + 通配符

```old8
point <- (10, 20)
result <- match point {
    case (0, 0) -> "Origin"
    case (_, 0) -> "On X-axis"
    case (0, _) -> "On Y-axis"
    case (x, y) if x == y -> $"Diagonal: ({x}, {y})"
    case (x, y) -> $"Point: ({x}, {y})"
}
```

### 示例 2: 类型匹配 + 守卫条件 + 范围

```old8
func categorize(value) -> string {
    result <- match value {
        case x:int if x >= 0 and x <= 100 -> match x {
            case [0~25] -> "Low"
            case [26~50] -> "Medium-Low"
            case [51~75] -> "Medium-High"
            case [76~100] -> "High"
        }
        case x:int -> "Out of range"
        case s:string -> $"String: {s}"
        case _ -> "Unknown"
    }
    return result
}

PrintLine(categorize(30))      // 输出: Medium-Low
PrintLine(categorize("hello")) // 输出: String: hello
```

### 示例 3: 元组解构 + 类型匹配

```old8
data <- (42, "hello")
result <- match data {
    case (x:int, s:string) if x > 0 -> $"Positive int {x} and string {s}"
    // 注意：目前元组模式不直接支持类型注解，需要分步匹配
}

// 当前推荐做法：
data <- (42, "hello")
result <- match data {
    case (x, s) -> match x {
        case num:int if num > 0 -> $"Positive int {num} and string {s}"
        case _ -> "Other"
    }
}
```

## 匹配顺序和优先级

Match 表达式按从上到下的顺序检查 case 分支，一旦找到第一个匹配的分支就立即执行并返回结果，后续分支将被忽略。

```old8
value <- 10
result <- match value {
    case x if x > 5 -> "Greater than 5"  // 注意：这会报错，守卫需要类型注解
    case x:int if x > 5 -> "Greater than 5"  // 正确：匹配此分支
    case 10 -> "Ten"  // 不会执行，因为上面已经匹配
    case _ -> "Other"
}
```

**最佳实践：**
1. 将更具体的模式放在前面
2. 将通配符 `_` 或 `default` 放在最后作为兜底
3. 确保至少有一个模式能匹配，否则会抛出运行时错误

## 作用域和变量绑定

在 case 分支中绑定的变量仅在该分支的结果表达式中可见，不会泄漏到外部作用域。

```old8
outer_x <- 100
result <- match 42 {
    case x -> x + 1  // x = 42，仅在此表达式中可见
}
// outer_x 仍然是 100，不受影响
// x 在此处不可访问
```

**元组解构的多变量绑定：**
```old8
point <- (3, 4)
result <- match point {
    case (x, y) -> x * x + y * y  // x = 3, y = 4
}
// x 和 y 在此处都不可访问
```

## 错误处理

如果没有任何 case 匹配且没有 default 分支，会抛出 `InvalidOperationError`：

```old8
value <- 5
result <- match value {
    case 1 -> "one"
    case 2 -> "two"
}
// 错误: Match 表达式没有匹配的分支，值为: 5
```

**推荐做法：** 总是提供 `default` 分支或通配符 `_` 作为兜底。

## 性能考虑

1. **模式匹配顺序**：Match 按顺序检查每个分支，因此将最常见的情况放在前面可以提高性能
2. **守卫条件**：守卫条件需要额外的计算，避免在守卫中执行昂贵的操作
3. **范围匹配**：范围匹配涉及数值比较，比简单的值匹配稍慢

## 限制和注意事项

1. **编译器模式支持**：
   - ✅ 解释器模式（`-f`）：完全支持所有模式匹配功能
   - ⚠️ 编译器模式（`-c`）：仅支持基本的值匹配、变量绑定和通配符，高级模式（元组解构、类型匹配、守卫条件、范围匹配）的编译器支持正在开发中

2. **类型匹配限制**：
   - 守卫条件仅支持类型匹配模式（`case x:int if ...`）
   - 不支持单独的守卫条件（`case x if ...`，缺少类型注解）

3. **元组解构限制**：
   - 元组模式必须至少包含 2 个元素
   - 元素数量必须精确匹配
   - 不支持在元组模式中直接使用类型注解（需要分步匹配）

4. **范围匹配限制**：
   - 仅支持数值类型（int, double, char）
   - 不支持字符串范围或其他类型的范围

## 完整示例

```old8
// 复杂的分类函数
func classify(input) -> string {
    result <- match input {
        // 类型匹配 + 守卫 + 范围
        case x:int if x >= 0 -> match x {
            case [0~10] -> "Small positive"
            case [11~100] -> "Medium positive"
            case [101~1000] -> "Large positive"
            default -> "Very large positive"
        }
        case x:int if x < 0 -> match x {
            case [-10~<0] -> "Small negative"
            case [-100~<-10] -> "Medium negative"
            default -> "Large negative"
        }

        // 字符串匹配
        case s:string -> $"String: {s}"

        // 元组解构
        case (x, y) -> match (x, y) {
            case (0, 0) -> "Origin point"
            case (a, b) -> $"Point ({a}, {b})"
        }

        // 兜底
        default -> "Unknown type"
    }
    return result
}

// 测试
PrintLine(classify(5))        // Small positive
PrintLine(classify(-50))      // Medium negative
PrintLine(classify("test"))   // String: test
PrintLine(classify((0, 0)))   // Origin point
PrintLine(classify((3, 4)))   // Point (3, 4)
```

## 与其他语言的比较

Old8Lang 的模式匹配受到以下语言的启发：

- **Rust**: 元组解构、守卫条件
- **Scala**: 类型匹配、case 语法
- **Python 3.10+**: 结构化模式匹配
- **F#**: 范围模式、守卫

主要区别：
- 使用 `->` 而非 `=>`（符合 Old8Lang 风格）
- 使用 `[start~end]` 表示范围（而非 Rust 的 `start..=end`）
- 守卫条件使用 `if` 关键字（类似 Scala 和 F#）

## 总结

Old8Lang 的增强模式匹配提供了强大而灵活的控制流机制，使代码更加简洁和表达力更强。通过组合使用不同的模式类型，可以优雅地处理复杂的条件逻辑。
