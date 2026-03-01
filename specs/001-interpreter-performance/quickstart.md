# Quick Start: 解释器性能优化

**Feature**: 001-interpreter-performance
**Date**: 2026-02-18
**Audience**: Old8Lang 开发者和贡献者

## 概述

本指南帮助你快速开始使用 Old8Lang 解释器的性能优化功能和性能监控 API。

## 前置条件

- .NET 10.0 SDK
- Old8Lang 源代码
- 基本的 C# 和 Old8Lang 知识

## 5 分钟快速开始

### 1. 启用性能监控

最简单的方式是通过 CLI 启用性能监控：

```bash
# 基础性能监控
dotnet run --project Old8Lang.App -- -f your-script.old8 --perf

# 详细性能监控
dotnet run --project Old8Lang.App -- -f your-script.old8 --perf-detailed
```

**预期输出**:
```
=== 性能报告 ===
执行时间: 150ms
内存使用: 10MB
函数调用: 1000次
变量查找: 5000次
缓存命中率: 85%
```

### 2. 编程方式使用性能监控

```csharp
using Old8Lang.Interpreter;

// 创建性能监控器
var monitor = new PerformanceMonitor();

// 启动监控
monitor.StartMonitoring();

// 创建解释器并执行代码
var interpreter = new LangInterpreter(monitor);
interpreter.Execute(code);

// 停止监控并获取结果
monitor.StopMonitoring();
var metrics = monitor.GetMetrics();

// 显示结果
Console.WriteLine($"执行时间: {metrics.ExecutionTimeMs}ms");
Console.WriteLine($"缓存命中率: {metrics.CacheHitRate:P}");
```

### 3. 生成性能报告

```csharp
using Old8Lang.Interpreter;

// ... 执行代码并收集指标 ...

// 创建报告生成器
var reporter = new PerformanceReporter();

// 生成文本报告
var textReport = reporter.GenerateTextReport(metrics);
Console.WriteLine(textReport);

// 保存 JSON 报告
reporter.SaveReport(metrics, "performance-report.json", ReportFormat.Json);
```

## 常见使用场景

### 场景 1: 识别性能瓶颈

**问题**: 你的 Old8Lang 脚本运行缓慢，想找出瓶颈在哪里。

**解决方案**:

```bash
# 1. 运行详细性能监控
dotnet run --project Old8Lang.App -- -f slow-script.old8 --perf-detailed --perf-output report.json

# 2. 查看报告
cat report.json | jq '.functionMetrics | sort_by(.totalTimeMs) | reverse | .[0:5]'
```

这会显示执行时间最长的前 5 个函数。

### 场景 2: 验证优化效果

**问题**: 你优化了代码，想验证性能是否真的提升了。

**解决方案**:

```csharp
// 1. 建立基线
var baselineMetrics = MeasurePerformance(originalCode);

// 2. 测试优化后的代码
var optimizedMetrics = MeasurePerformance(optimizedCode);

// 3. 计算改进
var improvement = (baselineMetrics.ExecutionTimeMs - optimizedMetrics.ExecutionTimeMs)
                  / (double)baselineMetrics.ExecutionTimeMs * 100;

Console.WriteLine($"性能提升: {improvement:F1}%");

// 辅助方法
PerformanceMetrics MeasurePerformance(string code)
{
    var monitor = new PerformanceMonitor();
    monitor.StartMonitoring();

    var interpreter = new LangInterpreter(monitor);
    interpreter.Execute(code);

    monitor.StopMonitoring();
    return monitor.GetMetrics();
}
```

### 场景 3: 监控长时间运行的脚本

**问题**: 你的脚本需要运行很长时间，想确保内存使用稳定。

**解决方案**:

```csharp
var config = new PerformanceMonitorConfig
{
    Enabled = true,
    EnableMemoryTracking = true,
    DetailedMonitoring = false // 减少开销
};

var monitor = new PerformanceMonitor();
monitor.StartMonitoring(config);

// 执行长时间运行的脚本
var interpreter = new LangInterpreter(monitor);
interpreter.Execute(longRunningCode);

monitor.StopMonitoring();
var metrics = monitor.GetMetrics();

// 检查内存增长
var memoryGrowthRate = CalculateMemoryGrowthRate(metrics);
if (memoryGrowthRate > 0.05) // 5%
{
    Console.WriteLine($"警告: 内存增长率过高 ({memoryGrowthRate:P})");
}
```

### 场景 4: 对比不同实现

**问题**: 你有两种实现方式，想知道哪种更快。

**解决方案**:

```csharp
// 实现 A
var metricsA = MeasurePerformance(implementationA);

// 实现 B
var metricsB = MeasurePerformance(implementationB);

// 对比
Console.WriteLine("实现对比:");
Console.WriteLine($"实现 A: {metricsA.ExecutionTimeMs}ms");
Console.WriteLine($"实现 B: {metricsB.ExecutionTimeMs}ms");

if (metricsA.ExecutionTimeMs < metricsB.ExecutionTimeMs)
{
    var faster = (metricsB.ExecutionTimeMs - metricsA.ExecutionTimeMs)
                 / (double)metricsB.ExecutionTimeMs * 100;
    Console.WriteLine($"实现 A 快 {faster:F1}%");
}
else
{
    var faster = (metricsA.ExecutionTimeMs - metricsB.ExecutionTimeMs)
                 / (double)metricsA.ExecutionTimeMs * 100;
    Console.WriteLine($"实现 B 快 {faster:F1}%");
}
```

## 性能优化最佳实践

### 1. 减少变量查找

**问题**: 频繁查找外层作用域的变量会降低性能。

**不好的做法**:
```old8
global_var = 100

function process() {
    for (i = 0; i < 1000; i++) {
        result = global_var * i  // 每次循环都查找 global_var
    }
}
```

**好的做法**:
```old8
global_var = 100

function process() {
    local_var = global_var  // 缓存到局部变量
    for (i = 0; i < 1000; i++) {
        result = local_var * i  // 使用局部变量
    }
}
```

### 2. 避免深度嵌套循环

**问题**: 深度嵌套循环会导致性能急剧下降。

**不好的做法**:
```old8
for (i = 0; i < 100; i++) {
    for (j = 0; j < 100; j++) {
        for (k = 0; k < 100; k++) {
            // 100万次迭代
            process(i, j, k)
        }
    }
}
```

**好的做法**:
```old8
// 尝试减少嵌套层数或使用更高效的算法
items = generate_items(100, 100, 100)
for (item in items) {
    process(item)
}
```

### 3. 复用对象

**问题**: 频繁创建和销毁对象会增加 GC 压力。

**不好的做法**:
```old8
for (i = 0; i < 1000; i++) {
    obj = new MyClass()  // 每次循环创建新对象
    obj.process()
}
```

**好的做法**:
```old8
obj = new MyClass()  // 在循环外创建
for (i = 0; i < 1000; i++) {
    obj.reset()  // 重置对象状态
    obj.process()
}
```

### 4. 使用合适的数据结构

**问题**: 不合适的数据结构会导致性能问题。

**不好的做法**:
```old8
// 使用数组进行频繁的查找操作
items = [1, 2, 3, ..., 1000]
for (i = 0; i < 1000; i++) {
    if (i in items) {  // O(n) 查找
        process(i)
    }
}
```

**好的做法**:
```old8
// 使用字典进行快速查找
items = {1: true, 2: true, ..., 1000: true}
for (i = 0; i < 1000; i++) {
    if (i in items) {  // O(1) 查找
        process(i)
    }
}
```

## 性能监控配置

### 基础配置

```csharp
var config = new PerformanceMonitorConfig
{
    Enabled = true,
    DetailedMonitoring = false,  // 最小开销
    SampleRate = 1.0,            // 100% 采样
    EnableMemoryTracking = true,
    EnableCacheTracking = true
};
```

### 详细配置

```csharp
var config = new PerformanceMonitorConfig
{
    Enabled = true,
    DetailedMonitoring = true,   // 收集详细信息
    SampleRate = 1.0,
    MaxFunctionMetrics = 100,    // 最多跟踪 100 个函数
    MaxScopeMetrics = 50,        // 最多跟踪 50 个作用域
    EnableMemoryTracking = true,
    EnableCacheTracking = true
};
```

### 生产环境配置

```csharp
var config = new PerformanceMonitorConfig
{
    Enabled = true,
    DetailedMonitoring = false,  // 减少开销
    SampleRate = 0.1,            // 10% 采样
    EnableMemoryTracking = true,
    EnableCacheTracking = false  // 禁用缓存跟踪
};
```

## 运行基准测试

### 使用 BenchmarkDotNet

```bash
# 1. 构建 Benchmarks 项目
dotnet build Old8Lang.Benchmarks -c Release

# 2. 运行所有基准测试
dotnet run --project Old8Lang.Benchmarks -c Release

# 3. 运行特定基准测试
dotnet run --project Old8Lang.Benchmarks -c Release -- --filter *VariableLookup*
```

### 查看基准测试结果

基准测试结果会保存在 `BenchmarkDotNet.Artifacts/results/` 目录中。

```bash
# 查看结果
cat BenchmarkDotNet.Artifacts/results/Old8Lang.Benchmarks.InterpreterBenchmarks-report.md
```

## 故障排查

### 问题 1: 性能监控开销过大

**症状**: 启用性能监控后，脚本执行时间显著增加。

**解决方案**:
1. 禁用详细监控: `DetailedMonitoring = false`
2. 降低采样率: `SampleRate = 0.1`
3. 禁用不需要的跟踪: `EnableCacheTracking = false`

### 问题 2: 内存使用持续增长

**症状**: 长时间运行的脚本内存使用不断增加。

**解决方案**:
1. 启用内存跟踪: `EnableMemoryTracking = true`
2. 检查对象池使用情况
3. 查看 GC 回收次数
4. 检查是否有循环引用

### 问题 3: 缓存命中率低

**症状**: 变量查找缓存命中率 <50%。

**解决方案**:
1. 检查变量访问模式
2. 减少外层作用域变量访问
3. 使用局部变量缓存常用值
4. 检查作用域嵌套深度

## 下一步

- 阅读 [data-model.md](./data-model.md) 了解性能数据结构
- 阅读 [contracts/performance-monitoring-api.md](./contracts/performance-monitoring-api.md) 了解完整 API
- 查看 [research.md](./research.md) 了解优化策略
- 运行基准测试建立性能基线

## 获取帮助

- 查看 [ARCHITECTURE.md](../../Docs/ARCHITECTURE.md) 了解解释器架构
- 查看 [CLAUDE.md](../../CLAUDE.md) 了解项目指南
- 提交 Issue: https://github.com/your-repo/Old8Lang/issues

## 示例脚本

### 性能测试脚本

创建 `performance-test.old8`:

```old8
// 变量查找测试
global_var = 100

function variable_lookup_test() {
    for (i = 0; i < 10000; i++) {
        x = global_var
    }
}

// 循环嵌套测试
function nested_loop_test() {
    sum = 0
    for (i = 0; i < 100; i++) {
        for (j = 0; j < 100; j++) {
            sum = sum + i * j
        }
    }
    return sum
}

// 函数调用测试
function fibonacci(n) {
    if (n <= 1) {
        return n
    }
    return fibonacci(n - 1) + fibonacci(n - 2)
}

function function_call_test() {
    result = fibonacci(20)
    return result
}

// 运行测试
print("开始性能测试...")
variable_lookup_test()
nested_loop_test()
function_call_test()
print("性能测试完成")
```

运行测试:

```bash
dotnet run --project Old8Lang.App -- -f performance-test.old8 --perf-detailed
```

## 总结

本快速开始指南介绍了：
- ✅ 如何启用性能监控
- ✅ 如何生成性能报告
- ✅ 常见使用场景
- ✅ 性能优化最佳实践
- ✅ 故障排查方法

现在你可以开始使用 Old8Lang 的性能优化功能了！
