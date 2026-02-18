namespace Old8Lang.Interpreter;

/// <summary>
/// 性能监控接口
/// </summary>
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

    /// <summary>
    /// 记录循环迭代
    /// </summary>
    void RecordLoopIteration();
}
