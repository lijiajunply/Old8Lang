# Data Model: 解释器性能优化

**Feature**: 001-interpreter-performance
**Date**: 2026-02-18
**Status**: Complete

## 概述

本文档定义解释器性能优化所需的核心数据结构和实体。这些实体主要用于性能监控、缓存管理和对象池优化。

## 核心实体

### 1. PerformanceMetrics（性能指标）

**用途**: 存储和报告解释器执行的性能数据

**字段**:

| 字段名 | 类型 | 描述 | 验证规则 |
|--------|------|------|---------|
| ExecutionTimeMs | long | 总执行时间（毫秒） | >= 0 |
| MemoryUsageBytes | long | 内存使用峰值（字节） | >= 0 |
| FunctionCallCount | int | 函数调用总次数 | >= 0 |
| VariableLookupCount | int | 变量查找总次数 | >= 0 |
| LoopIterationCount | long | 循环迭代总次数 | >= 0 |
| ObjectAllocationCount | int | 对象分配次数 | >= 0 |
| CacheHitRate | double | 缓存命中率（0-1） | 0 <= x <= 1 |
| GCCollectionCount | int | GC 回收次数 | >= 0 |
| StartTime | DateTime | 监控开始时间 | 不可为空 |
| EndTime | DateTime? | 监控结束时间 | 可为空 |

**关系**:
- 包含多个 `FunctionMetrics`（函数级别指标）
- 包含多个 `ScopeMetrics`（作用域级别指标）

**状态转换**:
```
[Created] -> [Monitoring] -> [Completed]
```

**示例**:
```csharp
var metrics = new PerformanceMetrics
{
    ExecutionTimeMs = 150,
    MemoryUsageBytes = 1024 * 1024 * 10, // 10MB
    FunctionCallCount = 1000,
    VariableLookupCount = 5000,
    CacheHitRate = 0.85
};
```

### 2. FunctionMetrics（函数性能指标）

**用途**: 存储单个函数的性能数据

**字段**:

| 字段名 | 类型 | 描述 | 验证规则 |
|--------|------|------|---------|
| FunctionName | string | 函数名称 | 不可为空 |
| CallCount | int | 调用次数 | >= 0 |
| TotalTimeMs | long | 总执行时间（毫秒） | >= 0 |
| AverageTimeMs | double | 平均执行时间（毫秒） | >= 0 |
| MinTimeMs | long | 最小执行时间（毫秒） | >= 0 |
| MaxTimeMs | long | 最大执行时间（毫秒） | >= 0 |
| RecursionDepth | int | 最大递归深度 | >= 0 |

**关系**:
- 属于一个 `PerformanceMetrics`

**示例**:
```csharp
var funcMetrics = new FunctionMetrics
{
    FunctionName = "fibonacci",
    CallCount = 100,
    TotalTimeMs = 50,
    AverageTimeMs = 0.5,
    RecursionDepth = 10
};
```

### 3. ScopeMetrics（作用域性能指标）

**用途**: 存储作用域级别的变量查找性能数据

**字段**:

| 字段名 | 类型 | 描述 | 验证规则 |
|--------|------|------|---------|
| ScopeId | string | 作用域标识符 | 不可为空 |
| ScopeLevel | int | 作用域层级（0=全局） | >= 0 |
| LookupCount | int | 查找次数 | >= 0 |
| CacheHitCount | int | 缓存命中次数 | >= 0 |
| CacheMissCount | int | 缓存未命中次数 | >= 0 |
| VariableCount | int | 变量数量 | >= 0 |

**关系**:
- 属于一个 `PerformanceMetrics`

**计算属性**:
- `CacheHitRate = CacheHitCount / (CacheHitCount + CacheMissCount)`

**示例**:
```csharp
var scopeMetrics = new ScopeMetrics
{
    ScopeId = "function_main",
    ScopeLevel = 1,
    LookupCount = 1000,
    CacheHitCount = 850,
    CacheMissCount = 150,
    VariableCount = 10
};
```

### 4. VariableCache（变量缓存）

**用途**: 缓存变量查找结果以提高性能

**字段**:

| 字段名 | 类型 | 描述 | 验证规则 |
|--------|------|------|---------|
| VariableName | string | 变量名 | 不可为空 |
| ScopeLevel | int | 所在作用域层级 | >= 0 |
| Value | object | 变量值 | 可为空 |
| LastAccessTime | DateTime | 最后访问时间 | 不可为空 |
| AccessCount | int | 访问次数 | >= 0 |
| IsDirty | bool | 是否已修改 | 默认 false |

**关系**:
- 属于一个 `VariateManager`

**缓存策略**:
- LRU（最近最少使用）淘汰策略
- 缓存大小限制: 1000 个条目
- 作用域销毁时清除对应缓存

**示例**:
```csharp
var cache = new VariableCache
{
    VariableName = "counter",
    ScopeLevel = 1,
    Value = 42,
    LastAccessTime = DateTime.Now,
    AccessCount = 10,
    IsDirty = false
};
```

### 5. ObjectPoolStats（对象池统计）

**用途**: 跟踪对象池的使用情况

**字段**:

| 字段名 | 类型 | 描述 | 验证规则 |
|--------|------|------|---------|
| PoolName | string | 对象池名称 | 不可为空 |
| ObjectType | string | 对象类型名称 | 不可为空 |
| PoolSize | int | 池大小 | > 0 |
| ActiveCount | int | 活跃对象数 | >= 0 |
| AvailableCount | int | 可用对象数 | >= 0 |
| TotalAllocations | long | 总分配次数 | >= 0 |
| TotalReturns | long | 总归还次数 | >= 0 |
| CacheHitRate | double | 池命中率（0-1） | 0 <= x <= 1 |

**关系**:
- 属于一个 `ObjectPoolManager`

**验证规则**:
- `ActiveCount + AvailableCount <= PoolSize`
- `TotalReturns <= TotalAllocations`

**示例**:
```csharp
var poolStats = new ObjectPoolStats
{
    PoolName = "ScopeObjectPool",
    ObjectType = "VariateScope",
    PoolSize = 100,
    ActiveCount = 30,
    AvailableCount = 70,
    TotalAllocations = 1000,
    TotalReturns = 970,
    CacheHitRate = 0.90
};
```

### 6. PerformanceMonitorConfig（性能监控配置）

**用途**: 配置性能监控的行为

**字段**:

| 字段名 | 类型 | 描述 | 验证规则 |
|--------|------|------|---------|
| Enabled | bool | 是否启用监控 | 默认 false |
| DetailedMonitoring | bool | 是否启用详细监控 | 默认 false |
| SampleRate | double | 采样率（0-1） | 0 < x <= 1 |
| MaxFunctionMetrics | int | 最大函数指标数 | > 0 |
| MaxScopeMetrics | int | 最大作用域指标数 | > 0 |
| EnableMemoryTracking | bool | 是否启用内存跟踪 | 默认 true |
| EnableCacheTracking | bool | 是否启用缓存跟踪 | 默认 true |

**默认配置**:
```csharp
var defaultConfig = new PerformanceMonitorConfig
{
    Enabled = false,
    DetailedMonitoring = false,
    SampleRate = 1.0,
    MaxFunctionMetrics = 100,
    MaxScopeMetrics = 50,
    EnableMemoryTracking = true,
    EnableCacheTracking = true
};
```

## 实体关系图

```
PerformanceMetrics (1) ----< (N) FunctionMetrics
PerformanceMetrics (1) ----< (N) ScopeMetrics

VariateManager (1) ----< (N) VariableCache

ObjectPoolManager (1) ----< (N) ObjectPoolStats

PerformanceMonitor (1) ---- (1) PerformanceMonitorConfig
PerformanceMonitor (1) ---- (1) PerformanceMetrics
```

## 数据流

### 性能监控数据流

```
1. 用户启用性能监控
   ↓
2. PerformanceMonitor 创建 PerformanceMetrics
   ↓
3. 解释器执行过程中收集数据
   - 函数调用 → 更新 FunctionMetrics
   - 变量查找 → 更新 ScopeMetrics
   - 对象分配 → 更新 ObjectPoolStats
   ↓
4. 执行完成后生成报告
   ↓
5. 用户查看 PerformanceMetrics
```

### 变量缓存数据流

```
1. 变量查找请求
   ↓
2. 检查 VariableCache
   ├─ 命中 → 返回缓存值（更新 AccessCount）
   └─ 未命中 → 从 VariateManager 查找
       ↓
       添加到 VariableCache
       ↓
       返回值
```

### 对象池数据流

```
1. 请求对象
   ↓
2. 检查 ObjectPool
   ├─ 有可用对象 → 返回（更新 ActiveCount）
   └─ 无可用对象 → 创建新对象
       ↓
       更新 TotalAllocations
       ↓
       返回对象

3. 归还对象
   ↓
4. 对象重置
   ↓
5. 放回池中（更新 AvailableCount, TotalReturns）
```

## 数据持久化

**注意**: 性能监控数据是临时的，不需要持久化存储。

- 性能指标在内存中收集
- 执行完成后可以导出为 JSON 或 CSV 格式
- 对象池统计可以定期记录到日志

## 数据验证规则总结

### 通用规则

1. 所有计数器字段必须 >= 0
2. 所有时间字段必须是有效的 DateTime
3. 所有比率字段必须在 0-1 之间
4. 所有名称字段不可为空或空字符串

### 业务规则

1. `PerformanceMetrics.EndTime` 必须 >= `StartTime`（如果不为空）
2. `FunctionMetrics.AverageTimeMs` = `TotalTimeMs / CallCount`
3. `ScopeMetrics.CacheHitRate` = `CacheHitCount / LookupCount`
4. `ObjectPoolStats.ActiveCount + AvailableCount` <= `PoolSize`
5. `VariableCache` 的 LRU 淘汰必须在达到大小限制时触发

## 性能考虑

### 内存使用

- `PerformanceMetrics`: ~1KB（不包括详细指标）
- `FunctionMetrics`: ~100 bytes × 函数数量
- `ScopeMetrics`: ~80 bytes × 作用域数量
- `VariableCache`: ~50 bytes × 缓存条目数（最多1000个）
- `ObjectPoolStats`: ~100 bytes × 对象池数量

**总计**: 基础监控 <100KB，详细监控 <1MB

### 访问性能

- `VariableCache` 查找: O(1) - 使用哈希表
- `PerformanceMetrics` 更新: O(1) - 直接字段访问
- `ObjectPoolStats` 更新: O(1) - 原子操作

## 扩展性

### 未来可能的扩展

1. **分布式追踪**: 添加 TraceId 和 SpanId 字段
2. **性能对比**: 添加基线性能数据用于对比
3. **热点分析**: 添加代码行级别的性能数据
4. **内存快照**: 添加内存分配的详细快照

### 向后兼容性

- 所有新增字段必须有默认值
- 不得删除或重命名现有字段
- 序列化格式必须支持版本控制
