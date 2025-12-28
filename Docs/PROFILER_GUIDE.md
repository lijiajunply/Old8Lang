# Old8Lang 性能分析工具

## 概述

Old8Lang Profiler 是一个强大的性能分析工具，用于分析 Old8Lang 代码的执行性能，识别性能瓶颈，并提供优化建议。

## 主要功能

### 1. 性能数据收集
- **执行时间跟踪**: 测量函数执行时间
- **内存使用监控**: 实时监控托管和非托管内存使用
- **垃圾回收统计**: 跟踪 GC 次数和频率
- **函数调用统计**: 记录函数调用次数和频率
- **自定义指标**: 支持添加自定义性能数据点

### 2. 性能瓶颈检测
- **高执行时间检测**: 识别执行时间过长的函数
- **高内存使用检测**: 监控内存泄漏和过度分配
- **频繁垃圾回收检测**: 识别 GC 频繁的代码模式
- **函数调用过频检测**: 发现可能需要缓存的热点函数
- **执行时间不稳定检测**: 识别执行时间波动大的函数

### 3. 性能分析报告
- **文本报告**: 详细的文本格式报告
- **JSON 报告**: 结构化数据，便于自动化处理
- **CSV 报告**: 适合 Excel 分析
- **Markdown 报告**: 适合文档集成

### 4. 性能评级系统
- **A 级别**: 90-100分，优秀性能
- **B 级别**: 80-89分，良好性能  
- **C 级别**: 70-79分，一般性能
- **D 级别**: 60-69分，较差性能
- **F 级别**: 0-59分，需要优化

## 使用指南

### 命令行接口

```bash
# 启动性能分析
old8lang profile start <文件> [会话名称]

# 停止性能分析
old8lang profile stop

# 查看分析状态
old8lang profile status

# 清除当前会话
old8lang profile clear
```

### 编程接口集成

```csharp
using Old8Lang.Profiler;

// 创建性能分析器
var profiler = new ProfilerManager();

// 启动分析
var sessionId = profiler.StartProfiling("MyAnalysis", "script.old8", "解释模式");

// 记录函数执行
profiler.RecordFunctionStart("myFunction", "script.old8", 10);
// ... 函数执行代码 ...
profiler.RecordFunctionEnd("myFunction", "script.old8", 15);

// 添加自定义数据
profiler.RecordCustomData("CustomMetric", 42.5, "ms");

// 停止分析并获取摘要
var summary = profiler.StopProfiling();
var report = new ReportGenerator().GenerateReport(summary, ReportFormat.Text);
Console.WriteLine(report);
```

## 性能指标说明

### 1. 函数性能指标

| 指标 | 说明 | 单位 |
|------|------|------|
| 调用次数 | 函数被调用的总次数 | 次 |
| 总执行时间 | 函数累计执行时间 | 毫秒 |
| 平均执行时间 | 函数平均执行时间 | 毫秒 |
| 最小执行时间 | 最短执行时间 | 毫秒 |
| 最大执行时间 | 最长执行时间 | 毫秒 |
| 标准差 | 执行时间的标准差 | 毫秒 |
| 中位数 | 执行时间的中位数 | 毫秒 |

### 2. 内存使用指标

| 指标 | 说明 | 单位 |
|------|------|------|
| 托管内存 | .NET 托管的堆内存 | MB |
| 非托管内存 | 非托管堆内存 | MB |
| 工作集 | 进程占用的内存 | MB |
| 专用内存 | 进程专用内存 | MB |
| GC 代数 0 | 第 0 代 GC 次数 | 次 |
| GC 代数 1 | 第 1 代 GC 次数 | 次 |
| GC 代数 2 | 第 2 代 GC 次数 | 次 |

### 3. 瓶颈类型

| 类型 | 严重程度 | 触发条件 | 建议 |
|------|----------|----------|------|
| 高执行时间 | 中等 | 平均执行时间 > 100ms | 优化算法、减少循环 |
| 高内存使用 | 高 | 峰值内存 > 100MB | 使用对象池、及时释放 |
| 频繁垃圾回收 | 高 | GC 次数 > 10/分钟 | 减少临时对象分配 |
| 函数调用过频 | 中等 | 调用次数 > 10000 | 使用缓存、内联函数 |
| 执行时间不稳定 | 低 | 标准差/平均值 > 0.5 | 检查条件分支 |

## 性能优化建议

### 1. 高执行时间优化

**问题**: 函数执行时间过长

**建议**:
- 检查算法复杂度，使用更高效的算法
- 减少不必要的循环和递归
- 使用缓存机制避免重复计算
- 考虑并行处理独立的计算任务

### 2. 内存优化

**问题**: 内存使用过高或存在内存泄漏

**建议**:
- 使用对象池减少分配开销
- 及时释放不再使用的对象
- 避免频繁的大对象分配
- 监控内存增长趋势

### 3. 垃圾回收优化

**问题**: GC 频繁，影响性能

**建议**:
- 减少临时对象创建
- 预分配和重用缓冲区
- 使用结构体替代小对象
- 避免 finalize 方法

### 4. 函数调用优化

**问题**: 函数调用过于频繁

**建议**:
- 使用函数缓存
- 内联频繁调用的小函数
- 使用委托和事件优化
- 减少不必要的函数调用层次

## 示例代码分析

### 示例 1: 计算密集型函数

```old8lang
func computePi(iterations) {
    pi <- 0.0
    i <- 1
    while i <= iterations {
        pi <- pi + (1.0 / (i * i - 1) * 4.0))
        i <- i + 1
    }
    return pi
}

# 性能分析结果
# - 函数: computePi
# - 调用次数: 1
# - 总时间: 156.23ms
# - 平均时间: 156.23ms
# - 建议: 这是一个计算密集型函数，考虑使用并行优化
```

### 示例 2: 内存密集型函数

```old8lang
func processLargeData() {
    data <- []
    i <- 0
    while i < 1000 {
        data <- data + createLargeObject()  # 假设每次创建大对象
        i <- i + 1
    }
    return data
}

# 性能分析结果
# - 函数: processLargeData
# - 调用次数: 1
# - 峰值内存: 250.5MB
# - 总时间: 45.67ms
# - 建议: 内存使用过高，建议使用对象池或流式处理
```

### 示例 3: 频繁调用函数

```old8lang
func getValue(key) {
    return expensiveLookup(key)  # 每次都要查找
}

func processData(items) {
    result <- []
    i <- 0
    while i < items.Count {
        value <- getValue(items[i])  # 频繁调用
        result <- result + processValue(value)
        i <- i + 1
    }
    return result
}

# 性能分析结果
# - 函数: getValue
# - 调用次数: 5000
# - 总时间: 12.34ms
# - 平均时间: 0.0025ms
# - 建议: 考用频率很高，建议使用缓存优化
```

## 集成到开发流程

### 1. 开发阶段性能测试

```csharp
// 在开发过程中启用性能分析
var profiler = ProfilerService.GetProfiler();

try 
{
    // 执行业务代码
    profiler.StartProfiling("dev-test");
    
    // ... 运行业务逻辑
    
    var summary = profiler.StopProfiling();
    
    if (summary.OverallScore < 70)
    {
        Console.WriteLine("⚠️ 性能分数较低，建议优化");
        Console.WriteLine(summary.Recommendation);
    }
}
finally 
{
    profiler.ClearSession();
}
```

### 2. CI/CD 性能回归测试

```bash
# 在持续集成中运行性能测试
old8lang profile start performance-test.old8
old8lang run performance-test.old8
old8lang profile stop

# 保存性能报告
old8lang profile report --format json --output performance-report.json
```

### 3. 生产环境监控

```csharp
// 生产环境中的性能监控
var profiler = ProfilerService.GetProfiler();
profiler.StartProfiling("prod-monitoring", "production.old8");

// 设置性能阈值告警
profiler.HighExecutionTimeThresholdMs = 50.0;  // 50ms
profiler.HighMemoryUsageThresholdMB = 200.0;  // 200MB

// 定期检查性能状态
Timer t = new Timer(_ => 
    var status = profiler.GetSessionStatus();
    var isProfiling = (bool)status["isProfiling"];
    if (isProfiling && 
        ((double)status["collector_memoryPeakMB"] ?? 0) > 500.0))
    {
        SendAlert("内存使用过高，需要关注");
    }
}, null, TimeSpan.FromMinutes(5));
t.Start();
```

## 高级功能

### 1. 自定义性能指标

```csharp
// 记录自定义性能数据
profiler.RecordCustomData("DatabaseQuery", 125.5, "ms", new Dictionary<string, string>
{
    ["query_type"] = "SELECT",
    ["table"] = "users"
});
```

### 2. 条件性能分析

```csharp
// 基于条件的性能分析
analyzer.HighExecutionTimeThresholdMs = 50.0;
analyzer.FrequentGCThresholdPerMinute = 15;

// 只有当条件满足时才记录
if (executionTime > analyzer.HighExecutionTimeThresholdMs)
{
    profiler.RecordCustomData("SlowQuery", executionTime, "ms");
}
```

### 3. 性能数据导出

```csharp
// 导出为不同格式
await profiler.SaveReportAsync("performance-report.md", ReportFormat.Markdown);
await profiler.SaveReportAsync("performance-report.json", ReportFormat.Json);
await profiler.SaveReportAsync("performance-report.csv", ReportFormat.Csv);
```

## 故障排除

### 常见问题

1. **无法启动性能分析**
   - 确保没有其他活跃的分析会话
   - 检查源文件是否存在且可读
   - 验证权限设置

2. **性能数据不准确**
   - 确保在真实的执行环境中测试
   - 避免在调试模式下测量性能
   - 多次运行取平均值

3. **内存监控异常**
   - 检查平台兼容性
   - 确保有足够的系统权限
   - 考虑降低监控频率

4. **报告生成失败**
   - 检查输出路径权限
   - 确保磁盘空间充足
   - 验证数据格式正确性

### 调试模式

```csharp
// 启用详细日志
analyzer.DebugMode = true;
profiler.MemoryMonitoringEnabled = true;

// 获取详细的收集状态
var status = profiler.GetSessionStatus();
foreach (var kvp in status)
{
    Console.WriteLine($"{kvp.Key}: {kvp.Value}");
}
```

## 最佳实践

1. **定期性能分析**: 在开发过程中定期运行性能分析
2. **基准测试**: 建立性能基准并定期回归测试
3. **监控趋势**: 跟踪性能指标的变化趋势
4. **阈值告警**: 设置合理的性能阈值和告警机制
5. **持续优化**: 根据分析结果持续优化代码

## 总结

Old8Lang Profiler 提供了全面的性能分析能力，帮助开发者：
- 识别性能瓶颈
- 量化性能指标
- 指导优化建议
- 监控性能趋势

通过合理使用性能分析工具，可以显著提升 Old8Lang 代码的执行效率和用户体验。