using System.Diagnostics;
using System.Text;

namespace Old8Lang.Bytecode.Profiler;

/// <summary>
/// 虚拟机性能分析器
/// </summary>
public class VMProfiler
{
    /// <summary>函数性能统计</summary>
    private readonly Dictionary<string, FunctionProfile> _functionProfiles = new();

    /// <summary>指令执行次数统计</summary>
    private readonly Dictionary<OpCode, long> _instructionCounts = new();

    /// <summary>是否启用性能分析</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 开始记录函数执行
    /// </summary>
    public Stopwatch? BeginFunction(string functionName)
    {
        if (!Enabled) return null;

        if (!_functionProfiles.ContainsKey(functionName))
        {
            _functionProfiles[functionName] = new FunctionProfile { Name = functionName };
        }

        _functionProfiles[functionName].CallCount++;
        return Stopwatch.StartNew();
    }

    /// <summary>
    /// 结束记录函数执行
    /// </summary>
    public void EndFunction(string functionName, Stopwatch? stopwatch)
    {
        if (!Enabled || stopwatch == null) return;

        stopwatch.Stop();
        var elapsed = stopwatch.Elapsed.TotalMilliseconds;

        var profile = _functionProfiles[functionName];
        profile.TotalTime += elapsed;
        profile.MinTime = Math.Min(profile.MinTime, elapsed);
        profile.MaxTime = Math.Max(profile.MaxTime, elapsed);
    }

    /// <summary>
    /// 记录指令执行
    /// </summary>
    public void RecordInstruction(OpCode opCode)
    {
        if (!Enabled) return;

        if (!_instructionCounts.ContainsKey(opCode))
            _instructionCounts[opCode] = 0;

        _instructionCounts[opCode]++;
    }

    /// <summary>
    /// 重置所有统计数据
    /// </summary>
    public void Reset()
    {
        _functionProfiles.Clear();
        _instructionCounts.Clear();
    }

    /// <summary>
    /// 生成性能报告
    /// </summary>
    public string GenerateReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== 虚拟机性能分析报告 ===");
        sb.AppendLine();

        // 函数性能报告
        sb.AppendLine("函数性能统计（按总时间排序）：");
        sb.AppendLine("----------------------------------------");
        sb.AppendLine($"{"函数名",-30} {"调用次数",10} {"总时间(ms)",12} {"平均(ms)",10} {"最小(ms)",10} {"最大(ms)",10}");

        var sortedFunctions = _functionProfiles.Values
            .OrderByDescending(f => f.TotalTime)
            .Take(20);

        foreach (var func in sortedFunctions)
        {
            sb.AppendLine($"{func.Name,-30} {func.CallCount,10} {func.TotalTime,12:F2} {func.AverageTime,10:F4} {func.MinTime,10:F4} {func.MaxTime,10:F4}");
        }

        sb.AppendLine();

        // 指令执行统计
        sb.AppendLine("指令执行统计（Top 20）：");
        sb.AppendLine("----------------------------------------");
        sb.AppendLine($"{"指令",-30} {"执行次数",15}");

        var sortedInstructions = _instructionCounts
            .OrderByDescending(kvp => kvp.Value)
            .Take(20);

        foreach (var (opCode, count) in sortedInstructions)
        {
            sb.AppendLine($"{opCode,-30} {count,15:N0}");
        }

        return sb.ToString();
    }
}
