namespace Old8Lang.Profiler;

/// <summary>
/// Profiler 核心管理器
/// </summary>
public class ProfilerManager
{
    private ProfilingSession? _currentSession;
    private PerformanceCollector? _collector;
    private readonly PerformanceAnalyzer _analyzer = new();
    private readonly ReportGenerator _reportGenerator = new();
    private readonly Lock _lockObject = new();

    /// <summary>
    /// 是否正在分析
    /// </summary>
    public bool IsProfiling => _currentSession is { IsActive: true };

    /// <summary>
    /// 当前会话
    /// </summary>
    public ProfilingSession? CurrentSession => _currentSession;

    /// <summary>
    /// 开始性能分析
    /// </summary>
    /// <param name="sessionName">会话名称</param>
    /// <param name="sourceFilePath">源文件路径</param>
    /// <param name="executionMode">执行模式</param>
    /// <returns>会话ID</returns>
    public Guid StartProfiling(string sessionName = "", string? sourceFilePath = null, string? executionMode = null)
    {
        lock (_lockObject)
        {
            if (IsProfiling)
            {
                throw new InvalidOperationException("已有性能分析会话正在进行中");
            }

            _currentSession = new ProfilingSession
            {
                Name = string.IsNullOrEmpty(sessionName)
                    ? $"ProfilingSession_{DateTime.Now:yyyyMMdd_HHmmss}"
                    : sessionName,
                SourceFilePath = sourceFilePath,
                ExecutionMode = executionMode
            };

            _collector = new PerformanceCollector(_currentSession);
            _collector.StartProfiling();

            return _currentSession.SessionId;
        }
    }

    /// <summary>
    /// 停止性能分析
    /// </summary>
    /// <returns>性能摘要</returns>
    public PerformanceSummary StopProfiling()
    {
        lock (_lockObject)
        {
            if (!IsProfiling || _collector is null || _currentSession is null)
            {
                throw new InvalidOperationException("当前没有正在进行的性能分析会话");
            }

            _collector.StopProfiling();
            var summary = _analyzer.GenerateSummary(_currentSession);

            _currentSession = null;
            _collector = null;

            return summary;
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
        if (_collector is not null)
        {
            _collector.RecordFunctionStart(functionName, sourceFile, lineNumber);
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
        if (_collector is not null)
        {
            _collector.RecordFunctionEnd(functionName, sourceFile, lineNumber);
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
        if (_collector is not null)
        {
            _collector.RecordStatementExecution(statementType, executionTimeMs, sourceFile, lineNumber);
        }
    }

    /// <summary>
    /// 记录解析时间
    /// </summary>
    /// <param name="parseTimeMs">解析时间（毫秒）</param>
    public void RecordParsingTime(double parseTimeMs)
    {
        if (_collector is not null)
        {
            _collector.RecordParsingTime(parseTimeMs);
        }
    }

    /// <summary>
    /// 记录编译时间
    /// </summary>
    /// <param name="compilationTimeMs">编译时间（毫秒）</param>
    public void RecordCompilationTime(double compilationTimeMs)
    {
        if (_collector is not null)
        {
            _collector.RecordCompilationTime(compilationTimeMs);
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
        if (_collector is not null)
        {
            _collector.RecordCustomData(name, value, unit, tags);
        }
    }

    /// <summary>
    /// 生成性能报告
    /// </summary>
    /// <param name="format">报告格式</param>
    /// <returns>报告文本</returns>
    public string GenerateReport(ReportFormat format = ReportFormat.Text)
    {
        if (_currentSession is null)
        {
            throw new InvalidOperationException("当前没有性能分析会话");
        }

        var summary = _analyzer.GenerateSummary(_currentSession);
        return _reportGenerator.GenerateReport(summary, format);
    }

    /// <summary>
    /// 保存性能报告到文件
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <param name="format">报告格式</param>
    public async Task SaveReportAsync(string filePath, ReportFormat format = ReportFormat.Text)
    {
        if (_currentSession is null)
        {
            throw new InvalidOperationException("当前没有性能分析会话");
        }

        var summary = _analyzer.GenerateSummary(_currentSession);
        await _reportGenerator.SaveReportAsync(summary, filePath, format);
    }

    /// <summary>
    /// 获取当前会话状态
    /// </summary>
    /// <returns>状态信息</returns>
    public Dictionary<string, object> GetSessionStatus()
    {
        lock (_lockObject)
        {
            var status = new Dictionary<string, object>
            {
                ["isProfiling"] = IsProfiling,
                ["hasSession"] = _currentSession is not null
            };

            if (_currentSession is not null)
            {
                status["sessionId"] = _currentSession.SessionId.ToString();
                status["sessionName"] = _currentSession.Name;
                status["durationMs"] = _currentSession.DurationMs;
                status["functionCount"] = _currentSession.FunctionStats.Count;
                status["dataPointCount"] = _currentSession.DataPoints.Count;
                status["memorySnapshotCount"] = _currentSession.MemoryHistory.Count;
            }

            if (_collector is not null)
            {
                var collectorStats = _collector.GetCollectorStats();
                foreach (var kvp in collectorStats)
                {
                    status[$"collector_{kvp.Key}"] = kvp.Value;
                }
            }

            return status;
        }
    }

    /// <summary>
    /// 清除当前会话（强制停止）
    /// </summary>
    public void ClearSession()
    {
        lock (_lockObject)
        {
            if (_collector is not null)
            {
                try
                {
                    _collector.StopProfiling();
                }
                catch
                {
                    // 忽略停止异常
                }
            }

            _currentSession = null;
            _collector = null;
        }
    }
}

public enum ReportFormat
{
    Text,
    Json,
    Csv,
    Html,
    Markdown
}