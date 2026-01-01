# 命名参数功能说明

Old8Lang 现在支持命名参数（Named Arguments）功能，允许在函数调用时通过参数名指定参数值。

## 语法

```old8
函数名(参数名: 值, 参数名: 值, ...)
```

## 特性

1. **位置参数和命名参数混合使用**：位置参数必须出现在所有命名参数之前
2. **参数顺序无关**：命名参数可以以任意顺序出现
3. **跳过默认参数**：可以跳过有默认值的参数
4. **不可重复**：同一个参数不能既作为位置参数又作为命名参数

## 示例

### 基本使用

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

### 带返回值的函数

```old8
func calculate(x:int, y:int, operation:string) -> int {
    if operation == "add" {
        return x + y
    } elif operation == "mul" {
        return x * y
    } else {
        return x / y
    }
}

// 使用命名参数改变顺序
result <- calculate(operation: "mul", y: 3, x: 7)
PrintLine("7 * 3 = " + result.ToStr())
```

## 规则和限制

### 1. 位置参数必须在命名参数之前

```old8
// ✅ 正确
func_call(1, 2, c: 3, d: 4)

// ❌ 错误：位置参数不能出现在命名参数之后
func_call(a: 1, 2, 3)
```

### 2. 不可重复指定参数

```old8
// ❌ 错误：参数 'x' 既作为位置参数又作为命名参数
func_call(1, x: 2)  // 假设第一个参数名为 x
```

### 3. 参数名必须匹配

```old8
// ❌ 错误：函数没有名为 'invalid' 的参数
func_call(invalid: 123)
```

### 4. params 参数不支持命名参数

```old8
func sum(params values:array<int>) -> int {
    // ...
}

// ✅ 正确：params 参数通过位置参数传递
sum(1, 2, 3, 4)

// ❌ 错误：不支持对 params 参数使用命名参数
sum(values: {1, 2, 3})
```

## 优势

1. **提高代码可读性**：参数名称明确表达参数的含义
2. **灵活的参数顺序**：不需要记住参数的确切顺序
3. **便于跳过默认参数**：只需指定需要修改的参数
4. **减少错误**：参数名称匹配降低了参数顺序错误的风险

## 注意事项

- 命名参数功能目前在**解释器模式**下完全支持
- 编译器模式的支持将在后续版本中添加
- 使用命名参数时，建议保持一致的代码风格，要么全部使用位置参数，要么全部使用命名参数（或合理混合使用）
