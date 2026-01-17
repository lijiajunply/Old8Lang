# Old8Lang 语言特性

本文档汇总 Old8Lang 的高级语言特性，包括模式匹配、高级控制流、并发模型、高级类型系统和互操作性。

## 目录

- [模式匹配](#模式匹配)
- [高级控制流](#高级控制流)
  - [Select 语句](#select-语句)
  - [Defer 语句](#defer-语句)
  - [Using 语句](#using-语句)
- [并发模型](#并发模型)
  - [Async/Await](#asyncawait)
  - [生成器 (Generators)](#生成器-generators)
  - [多线程 (Spawn)](#多线程-spawn)
- [运算符重载](#运算符重载)
- [高级类型系统](#高级类型系统)
  - [联合类型 (Union Types)](#联合类型-union-types)
  - [交叉类型 (Intersection Types)](#交叉类型-intersection-types)
  - [可空类型 (Nullable Types)](#可空类型-nullable-types)
  - [泛型集合](#泛型集合)
  - [泛型类型推断](#泛型类型推断)
- [互操作性](#互操作性)
  - [原生 P/Invoke](#原生-pinvoke)
  - [Python 互操作](#python-互操作)
- [命名参数](#命名参数)

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

*(详细示例请参考旧版文档或 Grammar 文档)*

---

## 高级控制流

Old8Lang 引入了多种现代控制流结构，简化资源管理和并发处理。

### Select 语句

类似 Go 语言的 `select`，用于处理多个通道（Channel）的发送和接收操作。它会阻塞直到其中一个 case 准备就绪。

```old8
ch1 <- ChannelCreate()
ch2 <- ChannelCreate()

select {
    // 发送操作
    case ch1 <- 100 -> {
        PrintLine("Sent 100 to ch1")
    }
    // 接收操作
    case msg from ch2 -> {
        PrintLine("Received from ch2: " + msg.ToStr())
    }
    // 默认分支（非阻塞）
    default -> {
        PrintLine("No channel ready")
    }
}
```

### Defer 语句

`defer` 语句用于推迟函数的执行直到包含它的作用域（通常是函数）结束。常用于清理工作。

```old8
func processFile(path:string) -> void {
    file <- OpenFile(path)
    // 确保文件在函数退出时关闭，无论是否发生异常
    defer CloseFile(file)
    
    // 处理文件...
    if ErrorOccurred() {
        return // CloseFile 会在这里自动调用
    }
}
```

### Using 语句

`using` 语句提供了一种确定性的资源释放机制（Deterministic Disposal）。它确保在代码块结束时自动调用资源的 `Dispose` 方法。

```old8
// 形式 1: 声明并使用
using mutex <- MutexCreate() {
    MutexLock(mutex)
    // 临界区...
} // 自动调用 MutexDispose(mutex)

// 形式 2: 使用已有资源
ch <- ChannelCreate()
using ch {
    // 使用通道...
} // 自动调用 ChannelDispose(ch)
```

---

## 并发模型

Old8Lang 内置了丰富的并发编程支持。

### Async/Await

支持基于 `async` 和 `await` 的异步编程模型，简化非阻塞代码的编写。

```old8
async func downloadUrl(url:string) -> string {
    // 模拟网络请求
    await Sleep(1000)
    return "Content of " + url
}

async func main() -> void {
    print("Downloading...")
    content <- await downloadUrl("http://example.com")
    print("Done: " + content)
}
```

### 生成器 (Generators)

使用 `yield` 关键字轻松创建迭代器。

```old8
func range(start:int, end:int) -> object {
    for i in start..end {
        yield i
    }
}

for num in range(1, 5) {
    PrintLine(num) // 输出 1, 2, 3, 4, 5
}
```

### 多线程 (Spawn)

使用 `spawn` 关键字启动新的线程（或协程）。

```old8
func worker(id:int) -> void {
    PrintLine("Worker " + id.ToStr() + " started")
}

// 启动并发任务
spawn worker(1)
spawn worker(2)
```

---

## 运算符重载

Old8Lang 支持 Python 风格的运算符重载，允许用户为自定义类定义运算符行为。

### 支持的运算符

通过在类中定义特殊方法（以 `_` 开头），可以重载以下运算符：

| 运算符 | 特殊方法 | 说明 |
|--------|---------|------|
| `+` | `_add(other)` | 加法运算 |
| `-` | `_sub(other)` | 减法运算 |
| `*` | `_mul(other)` | 乘法运算 |
| `/` | `_div(other)` | 除法运算 |
| `%` | `_mod(other)` | 取模运算 |
| `^` | `_pow(other)` | 幂运算 |
| `==` | `_eq(other)` | 相等比较 |
| `!=` | 自动实现 | 自动通过 `!_eq(other)` 实现 |
| `<` | `_lt(other)` | 小于比较 |
| `>` | `_gt(other)` | 大于比较 |
| `<=` | `_le(other)` | 小于等于比较 |
| `>=` | `_ge(other)` | 大于等于比较 |
| `obj[key]` | `_getitem(key)` | 索引获取 |
| `obj[key] <- value` | `_setitem(key, value)` | 索引设置 |

### 基本示例

```old8
class Vector {
    public x
    public y

    init(x, y) {
        this.x <- x
        this.y <- y
    }

    // 向量加法
    _add(other) {
        return Vector(this.x + other.x, this.y + other.y)
    }

    // 向量数乘
    _mul(scalar) {
        return Vector(this.x * scalar, this.y * scalar)
    }

    // 相等比较
    _eq(other) {
        return this.x == other.x && this.y == other.y
    }
}

v1 <- Vector(1, 2)
v2 <- Vector(3, 4)

// 使用重载的运算符
v3 <- v1 + v2        // 调用 v1._add(v2)，结果: Vector(4, 6)
v4 <- v1 * 2         // 调用 v1._mul(2)，结果: Vector(2, 4)

if v1 == v2 {        // 调用 v1._eq(v2)
    PrintLine("相等")
}

if v1 != v2 {        // 自动实现为 !v1._eq(v2)
    PrintLine("不相等")
}
```

### 复数运算示例

```old8
class Complex {
    public real
    public imag

    init(real, imag) {
        this.real <- real
        this.imag <- imag
    }

    // 复数加法：(a + bi) + (c + di) = (a+c) + (b+d)i
    _add(other) {
        return Complex(this.real + other.real, this.imag + other.imag)
    }

    // 复数乘法：(a + bi) * (c + di) = (ac-bd) + (ad+bc)i
    _mul(other) {
        newReal <- this.real * other.real - this.imag * other.imag
        newImag <- this.real * other.imag + this.imag * other.real
        return Complex(newReal, newImag)
    }

    // 相等比较
    _eq(other) {
        return this.real == other.real && this.imag == other.imag
    }
}

c1 <- Complex(1, 2)
c2 <- Complex(3, 4)
c3 <- c1 + c2        // Complex(4, 6)
c4 <- c1 * c2        // Complex(-5, 10)
```

### 链式运算

运算符重载支持链式运算和复杂表达式：

```old8
v1 <- Vector(1, 1)
v2 <- Vector(2, 2)
v3 <- Vector(3, 3)

// 链式加法
result <- v1 + v2 + v3  // 依次调用 v1._add(v2) 和 result._add(v3)

// 复杂表达式
result <- (v1 + v2) * 2  // 先加法，再乘法
```

### 比较运算符

比较运算符的特殊方法必须返回 `bool` 类型：

```old8
class Point {
    public x
    public y

    init(x, y) {
        this.x <- x
        this.y <- y
    }

    // 按距离原点的距离比较
    _lt(other) {
        thisDistance <- this.x * this.x + this.y * this.y
        otherDistance <- other.x * other.x + other.y * other.y
        return thisDistance < otherDistance
    }

    _gt(other) {
        thisDistance <- this.x * this.x + this.y * this.y
        otherDistance <- other.x * other.x + other.y * other.y
        return thisDistance > otherDistance
    }
}

p1 <- Point(1, 1)
p2 <- Point(2, 2)

if p1 < p2 {  // 调用 p1._lt(p2)
    PrintLine("p1 距离原点更近")
}
```

### 错误处理

如果类未定义对应的运算符方法，会抛出清晰的错误信息：

```old8
class SimpleClass {
    public value
    init(value) { this.value <- value }
}

obj1 <- SimpleClass(10)
obj2 <- SimpleClass(5)

try {
    result <- obj1 + obj2  // 错误：未定义 _add 方法
} catch (e) {
    // 错误信息：类型 'SimpleClass' 不支持加法操作（未定义 _add 方法）
    PrintLine(e.ToStr())
}
```

### 注意事项

1. **返回类型**：比较运算符（`_eq`、`_lt`、`_gt` 等）必须返回 `bool` 类型
2. **参数数量**：
   - 算术和比较运算符方法必须接受恰好 1 个参数
   - `_setitem` 方法必须接受恰好 2 个参数（key 和 value）
3. **自动实现**：`!=` 运算符会自动通过 `_eq` 方法的取反实现，无需单独定义
4. **当前限制**:
   - 仅支持解释器模式（`-f`），编译器模式（`-c`）将在后续版本中支持
   - 暂不支持一元运算符重载（如 `-x`、`!x`）
   - 索引运算符的 `_setitem` 方法中修改引用类型字段的内部状态可能需要特殊处理

### 索引运算符示例

```old8
class SparseArray {
    private data

    init() {
        this.data <- {"dummy": 0}
    }

    // 索引获取：arr[index]
    _getitem(index) {
        key <- index.ToStr()
        return this.data[key]
    }

    // 索引设置：arr[index] <- value
    _setitem(index, value) {
        key <- index.ToStr()
        this.data[key] <- value
    }
}

arr <- SparseArray()
arr[0] <- 10         // 调用 arr._setitem(0, 10)
arr[5] <- 50         // 调用 arr._setitem(5, 50)

val1 <- arr[0]       // 调用 arr._getitem(0)
val2 <- arr[5]       // 调用 arr._getitem(5)

PrintLine("arr[0] = " + val1.ToStr())
PrintLine("arr[5] = " + val2.ToStr())
```

---

## 高级类型系统

Old8Lang 的类型系统非常灵活，支持多种组合类型。

### 联合类型 (Union Types)

表示一个值可以是多种类型之一。

```old8
// x 可以是 int 或 string
func printId(id: int | string) -> void {
    PrintLine("ID: " + id.ToStr())
}
```

### 交叉类型 (Intersection Types)

表示一个值必须同时满足多个类型约束（通常用于接口）。

```old8
// obj 必须同时实现 IDrawable 和 IResizable
func process(obj: IDrawable & IResizable) -> void {
    obj.Draw()
    obj.Resize(100, 100)
}
```

### 可空类型 (Nullable Types)

明确标识可能为 `null` 的类型。

```old8
name: string? <- null // 合法
// age: int <- null   // 非法，int 不可为空
```

### 泛型集合

支持 `list<T>`, `array<T>`, `dict<K,V>`，提供编译时类型检查。

```old8
numbers: list<int> <- {1, 2, 3}
map: dict<string, int> <- {"one": 1}
```

### 泛型类型推断

编译器可根据参数自动推断泛型类型。

```old8
func identity<T>(val: T) -> T { return val }

x <- identity(42) // 自动推断 T 为 int
```

---

## 互操作性

Old8Lang 提供了强大的外部语言交互能力。

### 原生 P/Invoke

直接调用 C/C++ 动态链接库（DLL）。

```old8
// 导入 Windows Kernel32.dll
extern "extern" "kernel32.dll" stdcall func GetTickCount() -> int

start <- GetTickCount()
```

### Python 互操作

直接在 Old8Lang 中调用 Python 代码和库。

```old8
// 导入 Python 标准库 math
extern "extern" "pymodule:math" {
    func sqrt(x:double) -> double
}

result <- sqrt(16.0) // 4.0
```

---

## 命名参数

允许在函数调用时指定参数名称，提高可读性并支持跳过默认参数。

```old8
func window(title:string, width:int = 800, height:int = 600) -> void { ... }

// 乱序调用，且使用默认 height
window(width: 1024, title: "App")
```
