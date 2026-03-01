# Old8Lang 性能优化最佳实践

本文档描述 Old8Lang 解释器的性能优化策略和最佳实践。

## 解释器性能优化

### 1. 对象池 (Object Pool)

解释器使用对象池减少频繁的内存分配和 GC 压力。

**已池化的类型**:
- `BoolLangValue` - 布尔值
- `IntLangValue` - 整数值
- `DoubleLangValue` - 浮点值
- `StringLangValue` - 字符串值
- `CharLangValue` - 字符值
- `ControlFlowState` - 控制流状态

**使用方式**:
```csharp
// 从池获取（自动）
var intVal = IntLangValue.Create(42);

// 归还到池（在不再需要时）
intVal.ReturnToPool();
```

**VoidLangValue 单例**: `VoidLangValue.Instance` 是无状态单例，避免频繁分配。

### 2. 作用域缓存 (Scope Cache)

`VariateManager` 使用 `ThreadLocal<Stack<Dictionary<string, LangValueType>>>` 缓存作用域字典，在进入/退出作用域时复用字典对象，减少 GC 压力。

### 3. 函数调用缓存 (Function Call Cache)

`FunctionCallExpression` 缓存函数引用，避免每次调用时重复查找：
```csharp
// 内部实现：缓存函数引用
if (!TryGetCachedFunction(manager, out var func))
{
    func = LookupFunction(manager);
    CacheFunctionReference(func);
}
```

### 4. 变量查找优化

`VariateManager` 使用多级缓存加速变量查找：
- `_lookupCache`: 快速查找缓存（变量名 → 作用域索引）
- `_globalVariableCache`: 全局变量快速访问

### 5. 递归深度控制

解释器支持最多 1000 层递归深度（`MaxRecursionDepth = 1000`），超过时抛出 `StackOverflowError`。

## 性能监控

### 使用 CLI 监控

```bash
# 基础监控
dotnet run --project Old8Lang.App -- -f app.old8 --perf

# 详细监控（包含函数级别指标）
dotnet run --project Old8Lang.App -- -f app.old8 --perf-detailed

# 保存报告
dotnet run --project Old8Lang.App -- -f app.old8 --perf --perf-output report.json
```

### 编程方式使用

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

### 性能指标说明

| 指标 | 说明 |
|------|------|
| `ExecutionTimeMs` | 总执行时间（毫秒） |
| `MemoryUsageBytes` | 内存使用增量（字节） |
| `FunctionCallCount` | 函数调用总次数 |
| `VariableLookupCount` | 变量查找总次数 |
| `LoopIterationCount` | 循环迭代总次数 |
| `CacheHitRate` | 变量查找缓存命中率（0-1） |
| `GCCollectionCount` | GC 回收次数 |

## 性能目标

| 场景 | 目标 |
|------|------|
| 小型脚本（50行） | <100ms |
| 中等程序（1000行） | <2s |
| 长时间运行（5分钟+） | 内存增长 <5% |
| 递归深度 | 支持 100+ 层 |
| 缓存命中率 | >70% |

## 代码编写建议

1. **避免深层递归**: 超过 100 层的递归考虑改用迭代
2. **复用变量**: 减少临时变量创建，利用对象池
3. **使用局部变量**: 局部变量查找比全局变量快
4. **避免字符串拼接循环**: 大量字符串拼接使用列表收集后合并
