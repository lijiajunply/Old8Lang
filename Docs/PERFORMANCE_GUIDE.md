# Old8Lang 性能指南

**最后更新**: 2026-10-09

本文档是 Old8Lang 性能相关的唯一说明，分两部分：

- **第一部分 · 写代码**：怎么选执行模式、怎么写得更快、怎么测量。
- **第二部分 · 运行时内部优化**：解析器与解释器内部做了哪些优化、有哪些监控 API、性能目标。

> 本文档由 `PERFORMANCE_GUIDE.md`、`PERFORMANCE_OPTIMIZATION.md`、`PERFORMANCE_BEST_PRACTICES.md`
> 三份合并而成——三份标题几乎同名（前两份都叫「Old8Lang 性能优化指南」），读者无法判断该看哪份。

## 目录

- 第一部分 · 写代码
  - [执行模式性能](#执行模式性能) · [性能分析工具](#性能分析工具) · [IL 模式 vs 解释器模式](#IL-模式-vs-解释器模式)
  - [类型系统优化](#类型系统优化) · [内存管理](#内存管理) · [并发优化](#并发优化) · [数据结构选择](#数据结构选择)
  - [常见性能陷阱](#常见性能陷阱) · [基准测试最佳实践](#基准测试最佳实践) · [性能优化检查清单](#性能优化检查清单)
- 第二部分 · 运行时内部优化
  - [解析器优化技术](#解析器优化技术) · [解释器优化技术](#解释器优化技术)
  - [性能监控 API](#性能监控-api) · [性能目标](#性能目标) · [常见问题](#常见问题)

---

## 执行模式性能

Old8Lang 支持三种执行模式，每种模式有不同的性能特征。选择合适的执行模式对应用性能至关重要。

### 三种模式性能对比

| 性能指标 | 解释模式 | IL 模式 | VM 模式 |
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

#### 何时使用 IL 模式

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
dotnet run --project Old8Lang.App -- -il services/api_server.old8

# 计算密集型任务
dotnet run --project Old8Lang.App -- -il algorithms/matrix_multiply.old8

# 生产环境部署
dotnet run --project Old8Lang.App -- -il production/app.old8
```

#### 何时使用 VM 模式 ⚠️ 实验性

**优势**:
- 平衡启动速度和运行性能
- 支持字节码序列化和跨平台分发
- 内置性能分析器，便于优化
- 支持完整的语言特性

**劣势**:
- 性能介于解释模式和 IL 模式之间
- 实验性功能，可能不稳定

**推荐场景**:
```bash
# 跨平台分发：编译为字节码，再分发执行
dotnet run --project Old8Lang.App -- -compile app.old8 app.o8c
dotnet run --project Old8Lang.App -- -execute app.o8c

# 调试输出
dotnet run --project Old8Lang.App -- -vm app.old8 --debug
```

### 性能基准测试

以下是三种模式在典型任务下的性能对比：

#### 测试 1: 循环计算（计算前 1,000,000 个数字的和）

```old8lang
// 解释模式版本
func sum_interpreted() {
    total <- 0
    for i <- 0, i < 1000000, i++ {
        total <- total + i
    }
    return total
}

// IL 模式版本（需要类型注解）
func sum_compiled() -> number {
    total: number <- 0
    for i: number <- 0, i < 1000000, i++ {
        total <- total + i
    }
    return total
}
```

**性能结果**:
- 解释模式: ~2.5 秒
- IL 模式: ~0.5 秒 (5x 更快)
- VM 模式: ~1.2 秒 (2x 更快)

#### 测试 2: 递归计算（斐波那契数列 fib(35)）

```old8lang
func fibonacci(n: number) -> number {
    if (n <= 1) {
        return n
    }
    return fibonacci(n - 1) + fibonacci(n - 2)
}
```

**性能结果**:
- 解释模式: ~8.0 秒
- IL 模式: ~1.5 秒 (5.3x 更快)
- VM 模式: ~3.5 秒 (2.3x 更快)

#### 测试 3: 字符串操作（拼接 10,000 次）

```old8lang
func string_concat() {
    result <- ""
    for i <- 0, i < 10000, i++ {
        result <- result + "x"
    }
    return result
}
```

**性能结果**:
- 解释模式: ~1.8 秒
- IL 模式: ~0.4 秒 (4.5x 更快)
- VM 模式: ~0.9 秒 (2x 更快)

### 性能优化建议

#### 1. 根据场景选择模式

```bash
# 开发阶段：使用解释模式
dotnet run --project Old8Lang.App -- -f src/app.old8

# 性能测试：使用 IL 模式
dotnet run --project Old8Lang.App -- -il src/app.old8

# 生产部署：使用 IL 模式
dotnet run --project Old8Lang.App -- -il production/app.old8

# 跨平台分发：使用 VM 模式
dotnet run --project Old8Lang.App -- -compile src/app.old8 app.o8c
```

#### 2. 混合使用模式

对于复杂应用，可以混合使用不同模式：
- 主程序使用 IL 模式（高性能）
- 配置脚本使用解释模式（灵活性）
- 插件系统使用 VM 模式（隔离和安全）

#### 3. 性能分析

**虚拟机模式目前没有可用的性能分析器**：`VMProfiler` 类在代码库里存在，但没有任何调用点，
`-vm` 也只接受 `-D` / `--debug` / `--no-type-check` / `--no-type-inference` / `--type-inference-debug`，
没有性能分析开关。虚拟机侧的性能评估走基准测试套件：

```bash
# VM Quick（PR 场景快速回归）
dotnet run --project Old8Lang.Benchmarks --configuration Release -- --vm-report-quick

# VM Nightly（全量回归，出现性能 FAIL 时退出码为 1）
dotnet run --project Old8Lang.Benchmarks --configuration Release -- --vm-report-nightly
```

针对**函数级热点**的分析用交互式会话里的 `profile start` 命令（作用于解释模式）。
定位到热点后按情况改用 IL 模式运行。

---

## 性能分析工具

### 内置性能分析器

Old8Lang 提供内置的性能分析器（Profiler）用于识别性能瓶颈。

**启用方法**（`-f` 的性能参数必须写在**文件名之后**）：

```bash
# 基础监控
dotnet run --project Old8Lang.App -- -f mycode.old8 --perf

# 详细监控（包含函数级指标）
dotnet run --project Old8Lang.App -- -f mycode.old8 --perf-detailed

# 报告写入文件（支持 .txt / .json / .csv）
dotnet run --project Old8Lang.App -- -f mycode.old8 --perf --perf-output report.json
```

**实测输出**（对一个 500 行脚本运行 `--perf`）：

```
=== Old8Lang 性能报告 ===
开始时间: 2026-10-09 21:03:12
结束时间: 2026-10-09 21:03:12

--- 执行统计 ---
执行时间:       149 ms
内存使用:       1115.77 KB
GC 回收次数:    0

--- 运行时统计 ---
函数调用次数:   0
变量查找次数:   746
循环迭代次数:   0
对象分配次数:   0
缓存命中率:     66.5%

--- 对象池统计 ---
  BoolPool: 分配=250, 归还=0, 活跃=250
  IntPool: 分配=498, 归还=0, 活跃=498
  ...
```

对象池那一节给出各类值的分配/归还/活跃计数，可直接看出池化是否生效。

> 需要**按函数聚合**的分析（调用次数、总时间、平均时间、热点排序）用交互式会话里的
> `profile start <文件>` 系列命令，见 [开发工具 · 性能分析工具](DEVELOPER_TOOLS.md#性能分析工具)。

### BenchmarkDotNet 基准测试

对于精确的性能测试,使用 BenchmarkDotNet 项目:

```bash
dotnet run --project Old8Lang.Benchmarks --configuration Release

# VM 基线（基础场景）
dotnet run --project Old8Lang.Benchmarks --configuration Release -- --vm

# VM Quick（PR 快速回归）
dotnet run --project Old8Lang.Benchmarks --configuration Release -- --vm-report-quick

# VM Nightly（全量回归，遇到 FAIL 返回非零退出码）
dotnet run --project Old8Lang.Benchmarks --configuration Release -- --vm-report-nightly

# VM 扩展（手动排障/趋势分析）
dotnet run --project Old8Lang.Benchmarks --configuration Release -- --vm-report-extended
```

---

## IL 模式 vs 解释器模式

### 性能对比

| 模式 | 执行速度 | 启动时间 | 内存使用 | 适用场景 |
|------|---------|----------|---------|---------|
| IL 模式 (`-il`) | ⚡ **快** (5-10x) | 中等 | 较高 | 生产环境、长时间运行 |
| 解释器模式 (`-f`) | 较慢 | **快** | 较低 | 开发调试、脚本执行 |

### 选择建议

**使用 IL 模式**:
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
- IL 模式: ~500ms
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

### IL 模式的类型要求

IL 模式要求所有函数参数和返回值必须有类型标注:

```old8lang
// ✅ 正确 - 完整类型标注
func add(a:int, b:int) -> int {
    return a + b
}

// ✅ 正确 - 使用默认值推断类型
func greet(name:string, prefix: "Hello") -> string {
    return prefix + ", " + name
}

// ❌ 错误 - IL 模式缺少类型
func multiply(x, y) {
    return x * y
}
```

### 避免不必要的类型转换

**慢**（频繁转换）:
```old8lang
total:int <- 0
for i in [0~1000] {
    value:double <- ToDouble(i)
    total <- total + ToInt(value * 2.5)
}
```

**快**（直接使用合适类型）:
```old8lang
total:double <- 0.0
for i in [0~1000] {
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
    for i in [0~1000] {
        counter <- counter + 1
    }
}
```

### 避免不必要的对象创建

**慢**（频繁创建对象）:
```old8lang
for i in [0~10000] {
    temp <- {i, i*2, i*3}  // 每次循环创建新列表
    process(temp)
}
```

**快**（复用对象）:
```old8lang
temp <- {0, 0, 0}
for i in [0~10000] {
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
    for i in [0~10000] {
        MutexLock(mutex)
        counter <- counter + 1
        MutexUnlock(mutex)
    }
}
```

**快**（使用 AtomicInt，快 3-5 倍）:
```old8lang
using counter <- AtomicIntCreate(0) {
    for i in [0~10000] {
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
        for i in [0~100] {
            MutexLock(mutex)
            sharedQueue.Add(i)
            MutexUnlock(mutex)
        }
    }
}
```

**推荐方式**（通道通信）:
```old8lang
using ch <- ChannelCreateBounded(10) {
    async func producer() -> void {
        for i in [0~100] {
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

for i in [0~1000] {
    if users.Contains("Charlie") {  // O(n) 查找
        // ...
    }
}
```

**快**（使用字典，快 10-100 倍）:
```old8lang
users <- {"Alice": true, "Bob": true, "Charlie": true, "Dave": true, "Eve": true}

for i in [0~1000] {
    if users["Charlie"] != null {  // O(1) 查找
        // ...
    }
}
```

### 预分配容量

**慢**（动态扩容）:
```old8lang
result <- {}
for i in [0~10000] {
    result.Add(i)  // 多次内存重新分配
}
```

**快**（预分配）（假设有预分配函数）:
```old8lang
result <- {}
// 注：Old8Lang 当前不支持预分配,但这是一般优化原则
for i in [0~10000] {
    result.Add(i)
}
```

---

## 常见性能陷阱

### 1. 字符串拼接

**慢**（循环中拼接字符串）:
```old8lang
result <- ""
for i in [0~1000] {
    result <- result + i.ToStr() + ","  // 每次创建新字符串 O(n²)
}
```

**快**（使用列表再合并）:
```old8lang
parts <- {}
for i in [0~1000] {
    parts.Add(i.ToStr())
}
// 假设有 Join 函数
result <- Join(parts, ",")
```

### 2. 嵌套循环

**慢**（O(n²) 算法）:
```old8lang
for i in [0~n] {
    for j in [0~n] {
        if data[i] == data[j] {
            // ...
        }
    }
}
```

**快**（使用字典 O(n) 算法）:
```old8lang
seen <- {}
for i in [0~n] {
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
    for i in [1~n+1] {
        result <- result * i
    }
    return result
}
```

### 4. 不必要的函数调用

**慢**（重复计算）:
```old8lang
for i in [0~data.Count()] {  // 每次循环都调用 Count()
    process(data[i])
}
```

**快**（缓存结果）:
```old8lang
len <- data.Count()
for i in [0~len] {
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

> Old8Lang 目前没有取当前时间的全局函数（`GetCurrentTimeMs` 之类的写法并不存在），
> 因此脚本内计时需要靠外部工具或内置的性能监控。命令行最快的做法是 `--perf`：

```bash
# 跑完直接输出执行时间、内存、GC、变量查找次数、缓存命中率与对象池统计
dotnet run --project Old8Lang.App -- -f test_performance.old8 --perf

# 需要函数级指标时
dotnet run --project Old8Lang.App -- -f test_performance.old8 --perf-detailed

# 报告存成文件（支持 .txt / .json / .csv）
dotnet run --project Old8Lang.App -- -f test_performance.old8 --perf --perf-output report.json
```

`--perf` 系列参数必须写在**文件名之后**。

---

# 第二部分 · 运行时内部优化

这一部分讲 Old8Lang 自己做了哪些优化：对使用者不可见，但解释了「为什么这样写更快」，
并在需要定位性能问题时给出可用的监控 API。

## 解析器优化技术

### 零拷贝 (Zero-Copy)

**原理**：用 `Span<T>` / `ReadOnlySpan<char>` 直接在原字符串上切片，避免 `StringBuilder` 与 `Substring` 的对象分配。

**实现位置**：`Old8Lang/LangParser/LangToken.cs`

```csharp
// 优化前：逐字符 Append 再 ToString
var sb = new StringBuilder();
for (int i = startIndex; i <= endIndex; i++) sb.Append(code[i]);
var numberStr = sb.ToString();

// 优化后：零拷贝切片
var numberSpan = code.AsSpan(startIndex, endIndex - startIndex + 1);
var numberStr = new string(numberSpan);
```

**适用场景**：数字、字符串字面量、标识符、文件头指令的解析。

### 内存池化 (Memory Pooling)

**实现位置**：`Old8Lang/LangParser/Optimization/CharBufferPool.cs`、`TokenListPool.cs`

```csharp
// 字符缓冲区：Dispose 时自动归还
using var buffer = CharBufferPool.Rent(256);
var bufferSpan = buffer.Span;

// 临时 Token 列表：需显式归还
var list = TokenListPool.Rent();
try
{
    list.Add(token1);
    list.Add(token2);
}
finally
{
    TokenListPool.Return(list);
}
```

**注意**：对象池只适合短期临时对象，不要长期持有；按实际需要租用合适容量。

### 字符串缓存 (String Caching)

**实现位置**：`Old8Lang/LangParser/Optimization/StringCache.cs`

用 `ConcurrentDictionary` 缓存**长度 ≤ 64** 的字符串，默认上限 10000 条，缓存满时拒绝新条目。

```csharp
var cache = new StringCache(maxCacheSize: 10000);
var cachedStr = cache.GetOrAdd("identifier".AsSpan());

var stats = cache.GetStatistics();
Console.WriteLine($"命中率: {stats.HitRate:P2}");
```

适用场景：标识符、关键字、常用字符串字面量。

### 算法优化：文档注释合并的 O(n²) → O(n)

**实现位置**：`Old8Lang/LangParser/LangToken.cs:818-879`

```csharp
// 优化前：每次 InsertRange(0, group) 都要移动全部已有元素
foreach (var group in docCommentGroups) relevantDocs.InsertRange(0, group);

// 优化后：先追加，最后反转一次
foreach (var group in docCommentGroups) relevantDocs.AddRange(group);
relevantDocs.Reverse();
```

这是当年收益最大的一处改动，大文件解析时间的绝大部分来自这里。

### 递归深度保护

**实现位置**：`Old8Lang/LangParser/Core/ParserContext.cs`、`Old8Lang/LangParser/Parsers/ExpressionParser.cs`

```csharp
Context.EnterRecursion();
try
{
    // 递归解析逻辑
}
finally
{
    Context.ExitRecursion();
}
```

**默认上限 500 层**，超过时报「表达式嵌套过深，超过最大限制（500层）」。对正常代码无开销。

### 预计算：SourceLines 提前切分

**实现位置**：`Old8Lang/LangParser/Core/ParserContext.cs`

```csharp
// 优化前：延迟初始化，报错路径上可能重复切分
public string[] SourceLines => _sourceLines ??= SourceCode.Split('\n');

// 优化后：构造时切分一次
public ParserContext(string sourceCode, ...)
{
    if (!string.IsNullOrEmpty(sourceCode))
    {
        SourceLines = sourceCode.Split('\n');
    }
}
```

## 解释器优化技术

### 对象池

解释器对高频值类型做了池化：`BoolLangValue`、`IntLangValue`、`DoubleLangValue`、
`StringLangValue`、`CharLangValue`、`ControlFlowState`。

```csharp
var intVal = IntLangValue.Create(42);  // 自动从池获取
intVal.ReturnToPool();                  // 不再需要时归还
```

`VoidLangValue.Instance` 是无状态单例，避免频繁分配。
池的实时分配/归还/活跃计数可以直接看 `--perf` 报告末尾的「对象池统计」。

### 作用域缓存

`VariateManager` 用 `ThreadLocal<Stack<Dictionary<string, LangValueType>>>` 缓存作用域字典，
进入/退出作用域时复用字典对象，减少 GC 压力。

### 函数调用缓存

`FunctionCallExpression` 缓存已解析的函数引用，避免每次调用重复查找：

```csharp
if (!TryGetCachedFunction(manager, out var func))
{
    func = LookupFunction(manager);
    CacheFunctionReference(func);
}
```

### 变量查找优化

`VariateManager` 用多级缓存加速变量查找：`_lookupCache`（变量名 → 作用域索引）与
`_globalVariableCache`（全局变量快速访问）。

### 递归深度控制

解释器限制 **1000 层**递归（`MaxRecursionDepth = 1000`），超过抛 `StackOverflowError`。
注意这与解析器的 500 层是两回事：一个是表达式嵌套深度，一个是函数调用深度。

## 性能监控 API

### 解释器：PerformanceMonitor

命令行方式见上文的[性能分析工具](#性能分析工具)。以下是编程方式：

```csharp
var monitor = new PerformanceMonitor();
monitor.StartMonitoring(PerformanceMonitorConfig.Detailed);

var interpreter = new LangInterpreter(monitor);
var ast = interpreter.Build(code);
ast.Run(interpreter.Manager);

monitor.StopMonitoring();
var metrics = monitor.GetMetrics();

var reporter = new PerformanceReporter();
Console.WriteLine(reporter.GenerateTextReport(metrics));
```

| 指标 | 说明 |
|------|------|
| `ExecutionTimeMs` | 总执行时间（毫秒） |
| `MemoryUsageBytes` | 内存使用增量（字节） |
| `FunctionCallCount` | 函数调用总次数 |
| `VariableLookupCount` | 变量查找总次数 |
| `LoopIterationCount` | 循环迭代总次数 |
| `CacheHitRate` | 变量查找缓存命中率（0-1） |
| `GCCollectionCount` | GC 回收次数 |

### 解析器：ParserPerformanceMetrics

**实现位置**：`Old8Lang/LangParser/Optimization/ParserPerformanceMetrics.cs`

```csharp
var (metrics, stopwatch, memoryBefore, gc0Before, gc1Before, gc2Before) =
    ParserPerformanceMetrics.BeginCollection(sourceCode.Length);

var tokens = LangTokenizer.Tokenize(sourceCode);

ParserPerformanceMetrics.EndCollection(
    metrics, stopwatch, memoryBefore, gc0Before, gc1Before, gc2Before, tokens.Count);

Console.WriteLine(metrics.ToString());
```

| 指标 | 说明 |
|------|------|
| TokenizationTimeMs / ParsingTimeMs / TotalTimeMs | 词法、语法、总耗时 |
| TokenCount / SourceCodeLength | Token 数、源码字符数 |
| MemoryAllocatedBytes / PeakMemoryUsageBytes | 内存分配量与峰值 |
| GCGen0Collections / GCGen1Collections / GCGen2Collections | 各级 GC 次数 |
| TokensPerSecond / CharsPerSecond | 吞吐量 |

### 解析器：两个快捷入口

```csharp
// 生产环境：集成全部优化，无额外开销
var tokens = LangTokenizer.TokenizeOptimized(code);

// 性能测试：附带指标（有轻微开销，会强制 GC 收集）
var (tokens2, metrics2) = LangTokenizer.TokenizeWithMetrics(code);
```

## 性能目标

**解析器**（`Old8Lang.Benchmarks` 的 `PerformanceValidator` 断言）：

| 场景 | 目标 |
|------|------|
| 小型脚本（500 行） | < 100 ms |
| 中型项目（3000 行） | < 500 ms |
| 大型脚本（5000 行） | < 800 ms |
| StringCache 命中率 | > 50% |

**解释器**：

| 场景 | 目标 |
|------|------|
| 小型脚本（50 行） | < 100 ms |
| 中等程序（1000 行） | < 2 s |
| 长时间运行（5 分钟+） | 内存增长 < 5% |
| 变量查找缓存命中率 | > 70% |

> **关于「优化前后」的对比数字**：旧版本给过一组优化后快照（500/3000/5000 行 → 126/32/30 ms），
> 那是 2026-02 在特定测量口径下的结果。2026-10-09 用 `-s` 冷启动口径复测约为 41/53/64 ms，
> 两者差异很大、无法互相印证。**具体数字请以本机 `Old8Lang.Benchmarks` 现场测量为准**，
> 本文不再照抄历史快照。

## 常见问题

### Q1: 为什么小型脚本的性能没有明显提升？

小型脚本的耗时主要被固定开销占据：JIT 编译（首次运行）、系统负载、冷启动效应。
建议多次运行取平均，或用 BenchmarkDotNet 精确测量，并关注中大型文件的收益。

### Q2: 如何选择合适的缓存大小？

`StringCache` 的默认 10000 条适用于大多数场景。调整依据是代码中唯一标识符的数量、
可用内存与命中率要求；建议监控命中率（目标 > 50%）后再调整。

### Q3: 对象池何时归还资源？

`CharBufferPool` 在 `Dispose` 时自动归还（用 `using` 模式）；
`TokenListPool` 需要显式调用 `Return()`。优先用 `using`，或在 `finally` 中归还，
不要让池化对象长期挂在你手里。

### Q4: 如何处理递归深度限制？

解析器的 500 层对正常代码足够。触限时先检查代码是否有过深嵌套，用中间变量拆解表达式。
**不建议调高上限**——那只会把「清晰的报错」换成「栈溢出」。

### Q5: 性能优化是否影响向后兼容性？

不影响。这些都是内部实现，公开 API 与语言行为均未改变，既有代码无需修改。

---

## 性能优化检查清单

在优化性能时,按照以下顺序检查:

- [ ] **1. 使用 IL 模式** (`-il`) 而非解释器模式
- [ ] **2. 添加类型标注** 到所有函数参数和返回值
- [ ] **3. 性能分析** 用 `--perf` / `--perf-detailed`（或交互式的 `profile start` 命令）找到热点
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
- [开发工具 · 性能分析工具](DEVELOPER_TOOLS.md#性能分析工具) - 性能分析器详细文档
- [开发工具 · 调试器](DEVELOPER_TOOLS.md#调试器) - 调试工具使用
- [CLI 指南 · 性能监控和基准测试](CLI_GUIDE.md#性能监控和基准测试) - `--perf` 参数与基准测试命令
- [模式支持矩阵](MODE_SUPPORT.md) - 三种执行模式的实测支持情况
- [API_REFERENCE.md](API_REFERENCE.md) - 标准库 API 参考
- [架构文档 · 性能优化架构](ARCHITECTURE.md#性能优化架构) - 优化在整体架构中的位置
