using System.Diagnostics;

namespace Old8Lang.Profiler;

/// <summary>
/// 性能数据收集器
/// </summary>
public class PerformanceCollector
{
    private readonly ProfilingSession _session;
    private readonly Stopwatch _stopwatch = new();
    private readonly Timer _memoryMonitorTimer;
    private readonly Lock _lockObject = new();

    // 当前执行的函数栈
    private readonly Stack<string> _functionCallStack = new();

    // 函数执行开始时间
    private readonly Dictionary<string, DateTime> _functionStartTimes = new();

    /// <summary>
    /// 是否启用内存监控
    /// </summary>
    public bool MemoryMonitoringEnabled { get; set; } = true;

    /// <summary>
    /// 内存监控间隔（毫秒）
    /// </summary>
    public int MemoryMonitoringIntervalMs { get; set; } = 100;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="session">性能分析会话</param>
    public PerformanceCollector(ProfilingSession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));

        // 初始化内存监控定时器
        _memoryMonitorTimer = new Timer(RecordMemoryUsage, null, Timeout.Infinite, Timeout.Infinite);
    }

    /// <summary>
    /// 开始性能分析
    /// </summary>
    public void StartProfiling()
    {
        lock (_lockObject)
        {
            _stopwatch.Restart();

            if (MemoryMonitoringEnabled)
            {
                _memoryMonitorTimer.Change(TimeSpan.Zero, TimeSpan.FromMilliseconds(MemoryMonitoringIntervalMs));
            }

            // 记录初始内存状态
            RecordMemoryUsage(null);

            _session.AddDataPoint(new PerformanceDataPoint
            {
                Name = "ProfilingStarted",
                Type = PerformanceCounterType.ExecutionTime,
                Value = 0,
                Unit = "ms"
            });
        }
    }

    /// <summary>
    /// 停止性能分析
    /// </summary>
    public void StopProfiling()
    {
        lock (_lockObject)
        {
            _stopwatch.Stop();

            // 停止内存监控
            _memoryMonitorTimer.Change(Timeout.Infinite, Timeout.Infinite);

            // 记录最终内存状态
            RecordMemoryUsage(null);

            _session.AddDataPoint(new PerformanceDataPoint
            {
                Name = "ProfilingStopped",
                Type = PerformanceCounterType.ExecutionTime,
                Value = _stopwatch.Elapsed.TotalMilliseconds,
                Unit = "ms"
            });

            _session.EndSession();
        }
    }

    /// <summary>
    /// 记录函数开始执行
    /// </summary>
    /// <param name="functionName">函数名</param>
    /// <param name="sourceFile">源文件</param>
    /// <param name="lineNumber">行号</param>
    public void RecordFunctionStart(string functionName, string? sourceFile = null, int? lineNumber = null)
    {
        lock (_lockObject)
        {
            _functionCallStack.Push(functionName);
            _functionStartTimes[functionName] = DateTime.Now;

            // 记录函数调用次数
            _session.AddDataPoint(new PerformanceDataPoint
            {
                Name = functionName + "_CallCount",
                Type = PerformanceCounterType.FunctionCallCount,
                Value = _session.FunctionStats.TryGetValue(functionName, out var stat)
                    ? stat.CallCount + 1
                    : 1,
                Unit = "count",
                Tags = new Dictionary<string, string> { ["function"] = functionName }
            });
        }
    }

    /// <summary>
    /// 记录函数结束执行
    /// </summary>
    /// <param name="functionName">函数名</param>
    /// <param name="sourceFile">源文件</param>
    /// <param name="lineNumber">行号</param>
    public void RecordFunctionEnd(string functionName, string? sourceFile = null, int? lineNumber = null)
    {
        lock (_lockObject)
        {
            // 检查函数是否在调用栈中
            if (_functionCallStack.Count == 0 || _functionCallStack.Peek() != functionName)
            {
                // 可能是异步函数或异常退出，尝试移除
                if (_functionCallStack.Contains(functionName))
                {
                    var tempStack = new Stack<string>();
                    while (_functionCallStack.Count > 0)
                    {
                        var currentFunction = _functionCallStack.Pop();
                        if (currentFunction == functionName)
                        {
                            break;
                        }

                        tempStack.Push(currentFunction);
                    }

                    // 恢复栈状态
                    while (tempStack.Count > 0)
                    {
                        _functionCallStack.Push(tempStack.Pop());
                    }
                }
            }
            else
            {
                _functionCallStack.Pop();
            }

            if (_functionStartTimes.TryGetValue(functionName, out var startTime))
            {
                var executionTime = DateTime.Now.Subtract(startTime).TotalMilliseconds;
                _session.RecordFunctionExecution(functionName, executionTime, sourceFile, lineNumber);

                _functionStartTimes.Remove(functionName);
            }
        }
    }

    /// <summary>
    /// 记录语句执行时间
    /// </summary>
    /// <param name="statementType">语句类型</param>
    /// <param name="executionTimeMs">执行时间（毫秒）</param>
    /// <param name="sourceFile">源文件</param>
    /// <param name="lineNumber">行号</param>
    public void RecordStatementExecution(string statementType, double executionTimeMs, string? sourceFile = null,
        int? lineNumber = null)
    {
        lock (_lockObject)
        {
            _session.AddDataPoint(new PerformanceDataPoint
            {
                Name = statementType + "_ExecutionTime",
                Type = PerformanceCounterType.ExecutionTime,
                Value = executionTimeMs,
                Unit = "ms",
                Tags = new Dictionary<string, string>
                {
                    ["statement_type"] = statementType,
                    ["source_file"] = sourceFile ?? "",
                    ["line_number"] = lineNumber?.ToString() ?? ""
                }
            });
        }
    }

    /// <summary>
    /// 记录解析时间
    /// </summary>
    /// <param name="parseTimeMs">解析时间（毫秒）</param>
    public void RecordParsingTime(double parseTimeMs)
    {
        lock (_lockObject)
        {
            _session.AddDataPoint(new PerformanceDataPoint
            {
                Name = "ParsingTime",
                Type = PerformanceCounterType.ParsingTime,
                Value = parseTimeMs,
                Unit = "ms"
            });
        }
    }

    /// <summary>
    /// 记录编译时间
    /// </summary>
    /// <param name="compilationTimeMs">编译时间（毫秒）</param>
    public void RecordCompilationTime(double compilationTimeMs)
    {
        lock (_lockObject)
        {
            _session.AddDataPoint(new PerformanceDataPoint
            {
                Name = "CompilationTime",
                Type = PerformanceCounterType.CompilationTime,
                Value = compilationTimeMs,
                Unit = "ms"
            });
        }
    }

    /// <summary>
    /// 记录垃圾回收信息
    /// </summary>
    public void RecordGarbageCollection()
    {
        lock (_lockObject)
        {
            _session.AddDataPoint(new PerformanceDataPoint
            {
                Name = "Gen0Collections",
                Type = PerformanceCounterType.GarbageCollectionCount,
                Value = GC.CollectionCount(0),
                Unit = "count"
            });

            _session.AddDataPoint(new PerformanceDataPoint
            {
                Name = "Gen1Collections",
                Type = PerformanceCounterType.GarbageCollectionCount,
                Value = GC.CollectionCount(1),
                Unit = "count"
            });

            _session.AddDataPoint(new PerformanceDataPoint
            {
                Name = "Gen2Collections",
                Type = PerformanceCounterType.GarbageCollectionCount,
                Value = GC.CollectionCount(2),
                Unit = "count"
            });
        }
    }

    /// <summary>
    /// 记录自定义性能数据
    /// </summary>
    /// <param name="name">数据名称</param>
    /// <param name="value">值</param>
    /// <param name="unit">单位</param>
    /// <param name="tags">标签</param>
    public void RecordCustomData(string name, double value, string unit = "", Dictionary<string, string>? tags = null)
    {
        lock (_lockObject)
        {
            _session.AddDataPoint(new PerformanceDataPoint
            {
                Name = name,
                Type = PerformanceCounterType.ExecutionTime, // 默认类型，可以扩展
                Value = value,
                Unit = unit,
                Tags = tags ?? new Dictionary<string, string>()
            });
        }
    }

    /// <summary>
    /// 获取当前调用栈深度
    /// </summary>
    /// <returns>调用栈深度</returns>
    public int GetCallStackDepth()
    {
        lock (_lockObject)
        {
            return _functionCallStack.Count;
        }
    }

    /// <summary>
    /// 获取当前正在执行的函数
    /// </summary>
    /// <returns>当前函数名，如果没有则返回null</returns>
    public string? GetCurrentFunction()
    {
        lock (_lockObject)
        {
            return _functionCallStack.Count > 0 ? _functionCallStack.Peek() : null;
        }
    }

    /// <summary>
    /// 定时器回调：记录内存使用情况
    /// </summary>
    /// <param name="state">状态对象</param>
    private void RecordMemoryUsage(object? state)
    {
        try
        {
            _session.RecordMemoryUsage();
        }
        catch (Exception)
        {
            // 忽略内存监控异常，避免影响性能分析
        }
    }

    /// <summary>
    /// 获取性能收集统计信息
    /// </summary>
    /// <returns>统计信息</returns>
    public Dictionary<string, object> GetCollectorStats()
    {
        lock (_lockObject)
        {
            return new Dictionary<string, object>
            {
                ["SessionDurationMs"] = _stopwatch.Elapsed.TotalMilliseconds,
                ["CallStackDepth"] = _functionCallStack.Count,
                ["MemoryMonitoringEnabled"] = MemoryMonitoringEnabled,
                ["MemoryMonitoringIntervalMs"] = MemoryMonitoringIntervalMs,
                ["DataPointsCollected"] = _session.DataPoints.Count,
                ["FunctionStatsCollected"] = _session.FunctionStats.Count,
                ["MemorySnapshotsCollected"] = _session.MemoryHistory.Count
            };
        }
    }
}