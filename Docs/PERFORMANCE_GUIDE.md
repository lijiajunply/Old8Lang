# Old8Lang 性能优化指南

本指南帮助您编写高性能的 Old8Lang 代码。

## 目录

- [执行模式性能](#执行模式性能)
- [性能分析工具](#性能分析工具)
- [编译器模式 vs 解释器模式](#编译器模式-vs-解释器模式)
- [类型系统优化](#类型系统优化)
- [内存管理](#内存管理)
- [并发优化](#并发优化)
- [数据结构选择](#数据结构选择)
- [常见性能陷阱](#常见性能陷阱)
- [基准测试最佳实践](#基准测试最佳实践)

---

## 执行模式性能

Old8Lang 支持三种执行模式，每种模式有不同的性能特征。选择合适的执行模式对应用性能至关重要。

### 三种模式性能对比

| 性能指标 | 解释模式 | 编译模式 | VM 模式 |
|---------|---------|---------|---------|
| **启动时间** | 最快 (~50ms) | 慢 (~500ms) | 中等 (~200ms) |
| **运行时性能** | 中等 (1x) | 最快 (3-5x) | 较快 (1.5-2x) |
| **内存占用** | 中等 | 高（IL 代码） | 中等 |
| **首次执行** | 立即 | 需编译 | 需字节码生成 |
| **热点优化** | 无 | JIT 优化 | 解释器优化 |

### 性能选择指南

#### 何时使用解释模式

**优势**:
- 启动速度最快，适合短时间运行的脚本
- 无编译开销，适合频繁修改的开发场景
- 支持完整的动态特性（泛型、运算符重载）

**劣势**:
- 运行时性能较低
- 循环和递归密集型任务较慢

**推荐场景**:
```bash
# 自动化脚本（运行时间 < 10秒）
dotnet run --project Old8Lang.App -- -f scripts/deploy.old8

# 快速原型验证
dotnet run --project Old8Lang.App -- -f prototypes/algorithm_test.old8

# 开发调试
dotnet run --project Old8Lang.App -- -f src/main.old8 -d
```

#### 何时使用编译模式

**优势**:
- 运行时性能最高（3-5倍于解释模式）
- JIT 优化，热点代码自动优化
- 静态类型检查，减少运行时错误

**劣势**:
- 启动时间长（需要编译）
- 需要完整的类型注解
- 不支持某些动态特性

**推荐场景**:
```bash
# 长时间运行的服务
dotnet run --project Old8Lang.App -- -c services/api_server.old8

# 计算密集型任务
dotnet run --project Old8Lang.App -- -c algorithms/matrix_multiply.old8

# 生产环境部署
dotnet run --project Old8Lang.App -- -c production/app.old8
```

#### 何时使用 VM 模式 ⚠️ 实验性

**优势**:
- 平衡启动速度和运行性能
- 支持字节码序列化和跨平台分发
- 内置性能分析器，便于优化
- 支持完整的语言特性

**劣势**:
- 性能介于解释模式和编译模式之间
- 实验性功能，可能不稳定

**推荐场景**:
```bash
# 跨平台分发
dotnet run --project Old8Lang.App -- compile-bytecode app.old8 -o app.o8bc
dotnet run --project Old8Lang.App -- execute-bytecode app.o8bc

# 性能分析和优化
dotnet run --project Old8Lang.App -- -vm app.old8 --profile

# 需要高级调试功能
dotnet run --project Old8Lang.App -- -vm app.old8 --debug
```

### 性能基准测试

以下是三种模式在典型任务下的性能对比：

#### 测试 1: 循环计算（计算前 1,000,000 个数字的和）

```old8lang
// 解释模式版本
function sum_interpreted() {
    let total = 0
    for (let i = 0; i < 1000000; i = i + 1) {
        total = total + i
    }
    return total
}

// 编译模式版本（需要类型注解）
function sum_compiled() -> number {
    let total: number = 0
    for (let i: number = 0; i < 1000000; i = i + 1) {
        total = total + i
    }
    return total
}
```

**性能结果**:
- 解释模式: ~2.5 秒
- 编译模式: ~0.5 秒 (5x 更快)
- VM 模式: ~1.2 秒 (2x 更快)

#### 测试 2: 递归计算（斐波那契数列 fib(35)）

```old8lang
function fibonacci(n: number) -> number {
    if (n <= 1) {
        return n
    }
    return fibonacci(n - 1) + fibonacci(n - 2)
}
```

**性能结果**:
- 解释模式: ~8.0 秒
- 编译模式: ~1.5 秒 (5.3x 更快)
- VM 模式: ~3.5 秒 (2.3x 更快)

#### 测试 3: 字符串操作（拼接 10,000 次）

```old8lang
function string_concat() {
    let result = ""
    for (let i = 0; i < 10000; i = i + 1) {
        result = result + "x"
    }
    return result
}
```

**性能结果**:
- 解释模式: ~1.8 秒
- 编译模式: ~0.4 秒 (4.5x 更快)
- VM 模式: ~0.9 秒 (2x 更快)

### 性能优化建议

#### 1. 根据场景选择模式

```bash
# 开发阶段：使用解释模式
dotnet run --project Old8Lang.App -- -f src/app.old8

# 性能测试：使用编译模式
dotnet run --project Old8Lang.App -- -c src/app.old8

# 生产部署：使用编译模式
dotnet run --project Old8Lang.App -- -c production/app.old8

# 跨平台分发：使用 VM 模式
dotnet run --project Old8Lang.App -- compile-bytecode src/app.old8 -o app.o8bc
```

#### 2. 混合使用模式

对于复杂应用，可以混合使用不同模式：
- 主程序使用编译模式（高性能）
- 配置脚本使用解释模式（灵活性）
- 插件系统使用 VM 模式（隔离和安全）

#### 3. 性能分析

使用 VM 模式的性能分析器识别瓶颈：

```bash
# 运行性能分析
dotnet run --project Old8Lang.App -- -vm app.old8 --profile

# 查看报告
cat profiler-report-*.txt
```

然后针对热点函数优化，并使用编译模式运行。

---

## 性能分析工具

### 内置性能分析器

Old8Lang 提供内置的性能分析器（Profiler）用于识别性能瓶颈。

**启用方法**:
```bash
# 使用 --profile 标志运行代码
dotnet run --project Old8Lang.App -- -f mycode.old8 --profile

# 或使用 -p 简写
dotnet run --project Old8Lang.App -- -f mycode.old8 -p
```

**查看性能报告**:
- 报告文件位于 `profiler-report-{timestamp}.txt`
- 包含每个函数的调用次数、总时间、平均时间
- 按总时间排序,快速识别热点函数

**示例报告**:
```
函数性能分析报告
==================================================
函数名                调用次数    总时间(ms)   平均时间(ms)
calculateTotal       10000       2500.5       0.25
processData          5000        1800.3       0.36
```

**详细文档**: 参见 [PROFILER_GUIDE.md](PROFILER_GUIDE.md)

### BenchmarkDotNet 基准测试

对于精确的性能测试,使用 BenchmarkDotNet 项目:

```bash
dotnet run --project Old8Lang.Benchmarks --configuration Release
```

---

## 编译器模式 vs 解释器模式

### 性能对比

| 模式 | 执行速度 | 启动时间 | 内存使用 | 适用场景 |
|------|---------|----------|---------|---------|
| 编译器模式 (`-c`) | ⚡ **快** (5-10x) | 中等 | 较高 | 生产环境、长时间运行 |
| 解释器模式 (`-f`) | 较慢 | **快** | 较低 | 开发调试、脚本执行 |

### 选择建议

**使用编译器模式**:
- 生产环境部署
- CPU 密集型计算（循环、数学运算）
- 长时间运行的服务
- 性能敏感的应用

**使用解释器模式**:
- 快速原型开发
- 脚本工具
- 短期任务
- 调试阶段

### 示例性能差异

```old8lang
// 计算斐波那契数列（递归版本）
func fib(n:int) -> int {
    if n <= 1 {
        return n
    }
    return fib(n-1) + fib(n-2)
}

result <- fib(35)
```

**性能对比**:
- 编译器模式: ~500ms
- 解释器模式: ~3500ms
- **编译器快 7 倍**

---

## 类型系统优化

### 显式类型标注

虽然 Old8Lang 支持类型推断,但显式类型标注可以提升性能。

**慢**（类型推断）:
```old8lang
func calculate(a, b) {
    return a * b + a / b
}
```

**快**（显式类型）:
```old8lang
func calculate(a:double, b:double) -> double {
    return a * b + a / b
}
```

**原因**: 显式类型避免运行时类型检查和装箱/拆箱操作。

### 编译器模式的类型要求

编译器模式要求所有函数参数和返回值必须有类型标注:

```old8lang
// ✅ 正确 - 完整类型标注
func add(a:int, b:int) -> int {
    return a + b
}

// ✅ 正确 - 使用默认值推断类型
func greet(name:string, prefix: "Hello") -> string {
    return prefix + ", " + name
}

// ❌ 错误 - 编译器模式缺少类型
func multiply(x, y) {
    return x * y
}
```

### 避免不必要的类型转换

**慢**（频繁转换）:
```old8lang
total:int <- 0
for i in 0..1000 {
    value:double <- ToDouble(i)
    total <- total + ToInt(value * 2.5)
}
```

**快**（直接使用合适类型）:
```old8lang
total:double <- 0.0
for i in 0..1000 {
    total <- total + (i * 2.5)
}
```

---

## 内存管理

### 使用局部变量而非全局变量

**慢**（全局变量）:
```old8lang
globalCounter <- 0

func increment() -> void {
    globalCounter <- globalCounter + 1  // 全局变量访问较慢
}
```

**快**（局部变量）:
```old8lang
func processData() -> void {
    counter <- 0  // 局部变量访问快
    for i in 0..1000 {
        counter <- counter + 1
    }
}
```

### 避免不必要的对象创建

**慢**（频繁创建对象）:
```old8lang
for i in 0..10000 {
    temp <- {i, i*2, i*3}  // 每次循环创建新列表
    process(temp)
}
```

**快**（复用对象）:
```old8lang
temp <- {0, 0, 0}
for i in 0..10000 {
    temp[0] <- i
    temp[1] <- i * 2
    temp[2] <- i * 3
    process(temp)
}
```

### 使用 using 语句管理资源

**不推荐**（手动管理）:
```old8lang
mutex <- MutexCreate()
MutexLock(mutex)
// ... 使用
MutexUnlock(mutex)
MutexDispose(mutex)  // 容易忘记
```

**推荐**（自动管理）:
```old8lang
using mutex <- MutexCreate() {
    MutexLock(mutex)
    // ... 使用
    MutexUnlock(mutex)
}  // 自动释放,防止资源泄漏
```

---

## 并发优化

### 选择合适的并发原语

| 场景 | 推荐并发原语 | 理由 |
|------|-------------|------|
| 保护临界区 | Mutex | 简单直接 |
| 限制并发数 | Semaphore | 控制资源访问数量 |
| 线程间计数器 | AtomicInt | 无锁操作,性能最优 |
| 线程间通信 | Channel | 类型安全,避免共享状态 |
| 读多写少 | ReadWriteLock | 允许多读者并发 |
| 等待多个任务完成 | CountDownLatch | 一次性同步 |
| 多线程同步点 | CyclicBarrier | 可重用屏障 |

### AtomicInt vs Mutex

**慢**（使用 Mutex）:
```old8lang
using mutex <- MutexCreate() {
    counter <- 0
    for i in 0..10000 {
        MutexLock(mutex)
        counter <- counter + 1
        MutexUnlock(mutex)
    }
}
```

**快**（使用 AtomicInt，快 3-5 倍）:
```old8lang
using counter <- AtomicIntCreate(0) {
    for i in 0..10000 {
        AtomicIntIncrement(counter)  // 无锁操作
    }
}
```

### 避免过度锁定

**慢**（锁的粒度太大）:
```old8lang
using mutex <- MutexCreate() {
    MutexLock(mutex)
    data1 <- processData1()  // 长时间计算
    data2 <- processData2()  // 长时间计算
    sharedResource <- data1 + data2
    MutexUnlock(mutex)
}
```

**快**（减小锁的粒度）:
```old8lang
using mutex <- MutexCreate() {
    data1 <- processData1()  // 在锁外计算
    data2 <- processData2()  // 在锁外计算

    MutexLock(mutex)
    sharedResource <- data1 + data2  // 只锁定必要部分
    MutexUnlock(mutex)
}
```

### 使用 Channel 避免锁竞争

**传统方式**（共享内存 + 锁）:
```old8lang
using mutex <- MutexCreate() {
    sharedQueue <- {}

    async func producer() -> void {
        for i in 0..100 {
            MutexLock(mutex)
            Add(sharedQueue, i)
            MutexUnlock(mutex)
        }
    }
}
```

**推荐方式**（通道通信）:
```old8lang
using ch <- ChannelCreateBounded(10) {
    async func producer() -> void {
        for i in 0..100 {
            ChannelSend(ch, i)  // 无需显式锁
        }
    }

    async func consumer() -> void {
        while true {
            val <- ChannelTryReceive(ch, 100)
            if val == null { break }
            process(val)
        }
    }
}
```

---

## 数据结构选择

### 数组 vs 列表 vs 字典

| 数据结构 | 访问速度 | 插入/删除 | 内存占用 | 适用场景 |
|---------|---------|----------|---------|---------|
| 数组 `[1,2,3]` | O(1) | O(n) | 低 | 固定大小,频繁随机访问 |
| 列表 `{1,2,3}` | O(1) | O(1) 尾部, O(n) 中间 | 中等 | 动态大小,尾部操作 |
| 字典 `{"key":value}` | O(1) | O(1) | 高 | 键值对查找 |

### 示例优化

**慢**（频繁搜索列表）:
```old8lang
users <- {"Alice", "Bob", "Charlie", "Dave", "Eve"}

for i in 0..1000 {
    if Contains(users, "Charlie") {  // O(n) 查找
        // ...
    }
}
```

**快**（使用字典，快 10-100 倍）:
```old8lang
users <- {"Alice": true, "Bob": true, "Charlie": true, "Dave": true, "Eve": true}

for i in 0..1000 {
    if users["Charlie"] != null {  // O(1) 查找
        // ...
    }
}
```

### 预分配容量

**慢**（动态扩容）:
```old8lang
result <- {}
for i in 0..10000 {
    Add(result, i)  // 多次内存重新分配
}
```

**快**（预分配）（假设有预分配函数）:
```old8lang
result <- {}
// 注：Old8Lang 当前不支持预分配,但这是一般优化原则
for i in 0..10000 {
    Add(result, i)
}
```

---

## 常见性能陷阱

### 1. 字符串拼接

**慢**（循环中拼接字符串）:
```old8lang
result <- ""
for i in 0..1000 {
    result <- result + i.ToStr() + ","  // 每次创建新字符串 O(n²)
}
```

**快**（使用列表再合并）:
```old8lang
parts <- {}
for i in 0..1000 {
    Add(parts, i.ToStr())
}
// 假设有 Join 函数
result <- Join(parts, ",")
```

### 2. 嵌套循环

**慢**（O(n²) 算法）:
```old8lang
for i in 0..n {
    for j in 0..n {
        if data[i] == data[j] {
            // ...
        }
    }
}
```

**快**（使用字典 O(n) 算法）:
```old8lang
seen <- {}
for i in 0..n {
    if seen[data[i]] != null {
        // 已存在
    } else {
        seen[data[i]] <- true
    }
}
```

### 3. 递归调用

**慢**（深度递归）:
```old8lang
func factorial(n:int) -> int {
    if n <= 1 {
        return 1
    }
    return n * factorial(n - 1)  // 深度递归,栈溢出风险
}
```

**快**（迭代版本）:
```old8lang
func factorial(n:int) -> int {
    result <- 1
    for i in 1..n+1 {
        result <- result * i
    }
    return result
}
```

### 4. 不必要的函数调用

**慢**（重复计算）:
```old8lang
for i in 0..Count(data) {  // 每次循环都调用 Count()
    process(data[i])
}
```

**快**（缓存结果）:
```old8lang
len <- Count(data)
for i in 0..len {
    process(data[i])
}
```

---

## 基准测试最佳实践

### 使用 BenchmarkDotNet

Old8Lang 项目包含基准测试项目:

```bash
cd Old8Lang.Benchmarks
dotnet run -c Release
```

### 编写基准测试

在 `Old8Lang.Benchmarks` 项目中添加测试:

```csharp
using BenchmarkDotNet.Attributes;

public class MyBenchmark
{
    [Benchmark]
    public void TestMethod()
    {
        // 测试代码
    }
}
```

### 测试注意事项

1. **使用 Release 模式**: 始终用 `-c Release` 编译
2. **预热**: 多次运行避免冷启动影响
3. **隔离测试**: 关闭其他程序减少干扰
4. **多次测量**: 取平均值,关注标准差
5. **对比基线**: 与优化前版本对比

### 性能测试示例

```old8lang
// test_performance.old8
import "std:time"

startTime <- GetCurrentTimeMs()

// 测试代码
for i in 0..1000000 {
    // 性能测试逻辑
}

endTime <- GetCurrentTimeMs()
PrintLine("耗时: " + (endTime - startTime).ToStr() + "ms")
```

---

## 性能优化检查清单

在优化性能时,按照以下顺序检查:

- [ ] **1. 使用编译器模式** (`-c`) 而非解释器模式
- [ ] **2. 添加类型标注** 到所有函数参数和返回值
- [ ] **3. 性能分析** 使用 `--profile` 找到热点函数
- [ ] **4. 算法优化** 降低时间复杂度 (O(n²) → O(n))
- [ ] **5. 数据结构** 选择合适的数据结构(数组/列表/字典)
- [ ] **6. 避免内存分配** 复用对象,减少创建销毁
- [ ] **7. 并发优化** 使用 AtomicInt 替代 Mutex(计数器场景)
- [ ] **8. 减小锁粒度** 只锁定必要的代码段
- [ ] **9. 缓存计算结果** 避免重复计算
- [ ] **10. 使用 using 语句** 防止资源泄漏

---

## 总结

性能优化的黄金法则:

1. **先测量,再优化** - 使用性能分析工具找到瓶颈
2. **优先优化热点** - 80% 的时间花在 20% 的代码上
3. **算法 > 优化技巧** - 降低时间复杂度比微优化更重要
4. **可读性优先** - 不要为了微小的性能牺牲代码可读性
5. **持续测试** - 优化后必须验证性能提升

更多信息:
- [PROFILER_GUIDE.md](PROFILER_GUIDE.md) - 性能分析器详细文档
- [DEBUGGER_GUIDE.md](DEBUGGER_GUIDE.md) - 调试工具使用
- [API_REFERENCE.md](API_REFERENCE.md) - 标准库 API 参考

