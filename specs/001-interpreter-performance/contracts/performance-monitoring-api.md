# Performance Monitoring API Contract

**Feature**: 001-interpreter-performance
**Date**: 2026-02-18
**Version**: 1.0.0

## 概述

本文档定义解释器性能监控的 API 接口。这些 API 允许开发者启用性能监控、收集性能数据并生成报告。

## API 设计原则

1. **最小开销**: 默认情况下性能监控是禁用的，开销为零
2. **渐进式详细**: 支持从基础监控到详细监控的多个级别
3. **易于使用**: API 简单直观，无需复杂配置
4. **线程安全**: 所有 API 都是线程安全的
5. **向后兼容**: API 设计考虑未来扩展

## 核心接口

### IPerformanceMonitor

**用途**: 性能监控的主接口

```csharp
public interface IPerformanceMonitor
{
    /// <summary>
    /// 启动性能监控
    /// </summary>
    /// <param name="config">监控配置（可选）</param>
    void StartMonitoring(PerformanceMonitorConfig? config = null);

    /// <summary>
    /// 停止性能监控
    /// </summary>
    void StopMonitoring();

    /// <summary>
    /// 获取当前性能指标
    /// </summary>
    /// <returns>性能指标快照</returns>
    PerformanceMetrics GetMetrics();

    /// <summary>
    /// 重置性能指标
    /// </summary>
    void ResetMetrics();

    /// <summary>
    /// 检查监控是否启用
    /// </summary>
    bool IsMonitoring { get; }

    /// <summary>
    /// 记录函数调用
    /// </summary>
    /// <param name="functionName">函数名</param>
    /// <param name="executionTimeMs">执行时间（毫秒）</param>
    void RecordFunctionCall(string functionName, long executionTimeMs);

    /// <summary>
    /// 记录变量查找
    /// </summary>
    /// <param name="scopeId">作用域ID</param>
    /// <param name="cacheHit">是否命中缓存</param>
    void RecordVariableLookup(string scopeId, bool cacheHit);

    /// <summary>
    /// 记录对象分配
    /// </summary>
    /// <param name="objectType">对象类型</param>
    void RecordObjectAllocation(string objectType);
}
```

### IPerformanceReporter

**用途**: 生成性能报告

```csharp
public interface IPerformanceReporter
{
    /// <summary>
    /// 生成文本格式的性能报告
    /// </summary>
    /// <param name="metrics">性能指标</param>
    /// <returns>格式化的报告文本</returns>
    string GenerateTextReport(PerformanceMetrics metrics);

    /// <summary>
    /// 生成 JSON 格式的性能报告
    /// </summary>
    /// <param name="metrics">性能指标</param>
    /// <returns>JSON 字符串</returns>
    string GenerateJsonReport(PerformanceMetrics metrics);

    /// <summary>
    /// 生成 CSV 格式的性能报告
    /// </summary>
    /// <param name="metrics">性能指标</param>
    /// <returns>CSV 字符串</returns>
    string GenerateCsvReport(PerformanceMetrics metrics);

    /// <summary>
    /// 将报告保存到文件
    /// </summary>
    /// <param name="metrics">性能指标</param>
    /// <param name="filePath">文件路径</param>
    /// <param name="format">报告格式</param>
    void SaveReport(PerformanceMetrics metrics, string filePath, ReportFormat format);
}
```

## 使用场景

### 场景 1: 基础性能监控

**用户操作**: 开发者想要查看脚本的基本性能指标

**API 调用流程**:

```csharp
// 1. 创建性能监控器
var monitor = new PerformanceMonitor();

// 2. 启动监控（使用默认配置）
monitor.StartMonitoring();

// 3. 执行脚本
var interpreter = new LangInterpreter(monitor);
interpreter.Execute(code);

// 4. 停止监控
monitor.StopMonitoring();

// 5. 获取指标
var metrics = monitor.GetMetrics();

// 6. 显示结果
Console.WriteLine($"执行时间: {metrics.ExecutionTimeMs}ms");
Console.WriteLine($"内存使用: {metrics.MemoryUsageBytes / 1024 / 1024}MB");
Console.WriteLine($"函数调用: {metrics.FunctionCallCount}次");
```

**预期响应**:
```
执行时间: 150ms
内存使用: 10MB
函数调用: 1000次
```

### 场景 2: 详细性能分析

**用户操作**: 开发者想要识别性能瓶颈

**API 调用流程**:

```csharp
// 1. 创建详细监控配置
var config = new PerformanceMonitorConfig
{
    Enabled = true,
    DetailedMonitoring = true,
    EnableMemoryTracking = true,
    EnableCacheTracking = true
};

// 2. 启动详细监控
var monitor = new PerformanceMonitor();
monitor.StartMonitoring(config);

// 3. 执行脚本
var interpreter = new LangInterpreter(monitor);
interpreter.Execute(code);

// 4. 停止监控
monitor.StopMonitoring();

// 5. 获取详细指标
var metrics = monitor.GetMetrics();

// 6. 生成报告
var reporter = new PerformanceReporter();
var report = reporter.GenerateTextReport(metrics);
Console.WriteLine(report);
```

**预期响应**:
```
=== 性能报告 ===
总执行时间: 150ms
内存使用峰值: 10MB
函数调用总数: 1000次
变量查找总数: 5000次
缓存命中率: 85%

=== 热点函数 ===
1. fibonacci: 50ms (100次调用, 平均0.5ms)
2. calculate: 30ms (200次调用, 平均0.15ms)
3. process: 20ms (50次调用, 平均0.4ms)

=== 作用域统计 ===
1. 全局作用域: 1000次查找, 命中率90%
2. function_main: 2000次查找, 命中率85%
3. function_helper: 2000次查找, 命中率80%
```

### 场景 3: 性能对比

**用户操作**: 开发者想要对比优化前后的性能

**API 调用流程**:

```csharp
// 1. 运行基线测试
var baselineMetrics = RunWithMonitoring(code);

// 2. 应用优化
ApplyOptimizations();

// 3. 运行优化后测试
var optimizedMetrics = RunWithMonitoring(code);

// 4. 对比结果
var improvement = CalculateImprovement(baselineMetrics, optimizedMetrics);
Console.WriteLine($"性能提升: {improvement}%");

// 辅助方法
PerformanceMetrics RunWithMonitoring(string code)
{
    var monitor = new PerformanceMonitor();
    monitor.StartMonitoring();

    var interpreter = new LangInterpreter(monitor);
    interpreter.Execute(code);

    monitor.StopMonitoring();
    return monitor.GetMetrics();
}
```

**预期响应**:
```
基线执行时间: 200ms
优化后执行时间: 120ms
性能提升: 40%
```

### 场景 4: CLI 集成

**用户操作**: 通过命令行启用性能监控

**命令行接口**:

```bash
# 基础监控
dotnet run --project Old8Lang.App -- -f script.old8 --perf

# 详细监控
dotnet run --project Old8Lang.App -- -f script.old8 --perf-detailed

# 保存报告到文件
dotnet run --project Old8Lang.App -- -f script.old8 --perf --perf-output report.json
```

**API 调用流程**:

```csharp
// 在 CLI 命令处理中
if (options.EnablePerformance)
{
    var config = new PerformanceMonitorConfig
    {
        Enabled = true,
        DetailedMonitoring = options.DetailedPerformance
    };

    var monitor = new PerformanceMonitor();
    monitor.StartMonitoring(config);

    // 执行脚本
    interpreter.Execute(code);

    monitor.StopMonitoring();
    var metrics = monitor.GetMetrics();

    // 输出或保存报告
    if (!string.IsNullOrEmpty(options.PerfOutputFile))
    {
        var reporter = new PerformanceReporter();
        reporter.SaveReport(metrics, options.PerfOutputFile, ReportFormat.Json);
    }
    else
    {
        DisplayMetrics(metrics);
    }
}
```

## API 响应格式

### PerformanceMetrics JSON 格式

```json
{
  "executionTimeMs": 150,
  "memoryUsageBytes": 10485760,
  "functionCallCount": 1000,
  "variableLookupCount": 5000,
  "loopIterationCount": 10000,
  "objectAllocationCount": 500,
  "cacheHitRate": 0.85,
  "gcCollectionCount": 2,
  "startTime": "2026-02-18T10:00:00Z",
  "endTime": "2026-02-18T10:00:00.150Z",
  "functionMetrics": [
    {
      "functionName": "fibonacci",
      "callCount": 100,
      "totalTimeMs": 50,
      "averageTimeMs": 0.5,
      "minTimeMs": 0,
      "maxTimeMs": 2,
      "recursionDepth": 10
    }
  ],
  "scopeMetrics": [
    {
      "scopeId": "global",
      "scopeLevel": 0,
      "lookupCount": 1000,
      "cacheHitCount": 900,
      "cacheMissCount": 100,
      "variableCount": 5
    }
  ]
}
```

### 文本报告格式

```
=== Old8Lang 性能报告 ===
生成时间: 2026-02-18 10:00:00

[总体指标]
执行时间: 150ms
内存使用: 10.00MB
函数调用: 1000次
变量查找: 5000次
循环迭代: 10000次
对象分配: 500次
缓存命中率: 85.00%
GC 回收: 2次

[热点函数 Top 5]
1. fibonacci        50.00ms  (100次, 平均0.50ms, 递归深度10)
2. calculate        30.00ms  (200次, 平均0.15ms)
3. process          20.00ms  (50次, 平均0.40ms)
4. helper           15.00ms  (300次, 平均0.05ms)
5. validate         10.00ms  (100次, 平均0.10ms)

[作用域统计]
全局作用域        1000次查找  命中率90.00%  变量数5
function_main     2000次查找  命中率85.00%  变量数10
function_helper   2000次查找  命中率80.00%  变量数8

[性能建议]
✓ 缓存命中率良好 (85%)
✓ 内存使用稳定
⚠ fibonacci 函数递归深度较深，考虑优化
⚠ function_helper 作用域缓存命中率偏低，检查变量访问模式
```

## 错误处理

### 错误码

| 错误码 | 描述 | HTTP 状态码（如适用） |
|--------|------|---------------------|
| PERF_001 | 监控未启用 | N/A |
| PERF_002 | 监控已在运行 | N/A |
| PERF_003 | 无效的配置参数 | N/A |
| PERF_004 | 报告生成失败 | N/A |
| PERF_005 | 文件保存失败 | N/A |

### 错误响应示例

```csharp
// 尝试在未启动监控时获取指标
try
{
    var metrics = monitor.GetMetrics();
}
catch (InvalidOperationException ex)
{
    // 错误码: PERF_001
    // 消息: "性能监控未启用。请先调用 StartMonitoring()。"
    Console.WriteLine($"错误: {ex.Message}");
}

// 尝试重复启动监控
try
{
    monitor.StartMonitoring();
    monitor.StartMonitoring(); // 抛出异常
}
catch (InvalidOperationException ex)
{
    // 错误码: PERF_002
    // 消息: "性能监控已在运行。请先调用 StopMonitoring()。"
    Console.WriteLine($"错误: {ex.Message}");
}
```

## 性能考虑

### API 调用开销

| API 方法 | 开销（微秒） | 说明 |
|---------|------------|------|
| StartMonitoring | <10 | 一次性开销 |
| StopMonitoring | <10 | 一次性开销 |
| RecordFunctionCall | <1 | 每次调用 |
| RecordVariableLookup | <0.5 | 每次调用 |
| RecordObjectAllocation | <0.5 | 每次调用 |
| GetMetrics | <100 | 创建快照 |
| GenerateTextReport | <1000 | 格式化输出 |

### 内存开销

- 基础监控: ~100KB
- 详细监控: ~1MB（取决于函数和作用域数量）
- 报告生成: ~10KB（文本格式）

### 线程安全

所有 API 方法都是线程安全的，使用以下机制：
- 原子操作用于计数器更新
- 读写锁用于复杂数据结构
- 无锁数据结构用于高频操作

## 版本兼容性

### 版本 1.0.0（当前版本）

- 初始 API 发布
- 支持基础和详细监控
- 支持文本、JSON、CSV 报告格式

### 未来版本计划

**版本 1.1.0**（计划）:
- 添加实时监控 API
- 添加性能警报功能
- 添加分布式追踪支持

**版本 2.0.0**（计划）:
- 添加可视化报告生成
- 添加性能对比 API
- 添加自动优化建议

### 向后兼容性保证

- 不会删除或重命名现有 API 方法
- 新增方法使用可选参数保持兼容性
- 数据格式支持版本标识

## 测试要求

### 单元测试

- 每个 API 方法必须有单元测试
- 覆盖率目标: >90%
- 测试错误处理和边界条件

### 集成测试

- 测试完整的监控流程
- 测试与解释器的集成
- 测试报告生成

### 性能测试

- 验证 API 开销 <1%
- 验证内存使用在预期范围内
- 验证线程安全性

## 示例代码

完整的使用示例请参考 `quickstart.md`。
