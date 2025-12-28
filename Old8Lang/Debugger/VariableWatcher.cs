using Old8Lang.AST.Expression;
using Old8Lang.Interpreter;

namespace Old8Lang.Debugger;

/// <summary>
/// 监视变量信息
/// </summary>
public class WatchedVariable
{
    /// <summary>
    /// 变量名
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 表达式（可以是简单的变量名或复杂的表达式）
    /// </summary>
    public string Expression { get; set; } = string.Empty;

    /// <summary>
    /// 当前值
    /// </summary>
    public string? CurrentValue { get; set; }

    /// <summary>
    /// 变量类型
    /// </summary>
    public string? VariableType { get; set; }

    /// <summary>
    /// 是否启用监视
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.Now;

    public override string ToString()
    {
        return $"{Expression} = {CurrentValue} ({VariableType})";
    }
}

/// <summary>
/// 变量监视管理器
/// </summary>
public class VariableWatcher
{
    private readonly List<WatchedVariable> WatchedVariables = [];

    /// <summary>
    /// 添加监视变量
    /// </summary>
    /// <param name="expression">变量表达式</param>
    /// <param name="manager">变量管理器</param>
    /// <returns>监视变量信息</returns>
    public WatchedVariable AddWatch(string expression, VariateManager manager)
    {
        var watch = new WatchedVariable
        {
            Expression = expression,
            Name = ExtractVariableName(expression)
        };

        // 初始评估
        EvaluateWatch(watch, manager);

        WatchedVariables.Add(watch);
        return watch;
    }

    /// <summary>
    /// 移除监视变量
    /// </summary>
    /// <param name="expression">变量表达式</param>
    /// <returns>是否成功移除</returns>
    public bool RemoveWatch(string expression)
    {
        var watch = WatchedVariables.FirstOrDefault(w => w.Expression == expression);
        if (watch != null)
        {
            return WatchedVariables.Remove(watch);
        }

        return false;
    }

    /// <summary>
    /// 获取所有监视变量
    /// </summary>
    /// <returns>监视变量列表</returns>
    public List<WatchedVariable> GetAllWatches()
    {
        return WatchedVariables.ToList();
    }

    /// <summary>
    /// 更新所有监视变量的值
    /// </summary>
    /// <param name="manager">变量管理器</param>
    public void UpdateAllWatches(VariateManager manager)
    {
        foreach (var watch in WatchedVariables.Where(w => w.IsEnabled))
        {
            EvaluateWatch(watch, manager);
        }
    }

    /// <summary>
    /// 启用/禁用监视变量
    /// </summary>
    /// <param name="expression">变量表达式</param>
    /// <param name="enabled">是否启用</param>
    /// <returns>是否成功设置</returns>
    public bool SetWatchEnabled(string expression, bool enabled)
    {
        var watch = WatchedVariables.FirstOrDefault(w => w.Expression == expression);
        if (watch != null)
        {
            watch.IsEnabled = enabled;
            return true;
        }

        return false;
    }

    /// <summary>
    /// 清除所有监视变量
    /// </summary>
    public void ClearAllWatches()
    {
        WatchedVariables.Clear();
    }

    /// <summary>
    /// 评估监视表达式的值
    /// </summary>
    /// <param name="watch">监视变量</param>
    /// <param name="manager">变量管理器</param>
    private static void EvaluateWatch(WatchedVariable watch, VariateManager manager)
    {
        try
        {
            var value = EvaluateExpression(watch.Expression, manager);
            watch.CurrentValue = value?.ToString() ?? "null";
            watch.VariableType = value?.GetType().Name ?? "null";
            watch.LastUpdated = DateTime.Now;
        }
        catch (Exception ex)
        {
            watch.CurrentValue = $"错误: {ex.Message}";
            watch.VariableType = "错误";
            watch.LastUpdated = DateTime.Now;
        }
    }

    /// <summary>
    /// 评估表达式的值
    /// </summary>
    /// <param name="expression">表达式</param>
    /// <param name="manager">变量管理器</param>
    /// <returns>评估结果</returns>
    private static LangValueType? EvaluateExpression(string expression, VariateManager manager)
    {
        // 简单变量名
        if (IsValidIdentifier(expression))
        {
            return manager.GetValue(new LangId(expression));
        }

        // 对于更复杂的表达式，这里可以扩展
        // 目前只支持简单的变量名
        return null;
    }

    /// <summary>
    /// 从表达式中提取变量名
    /// </summary>
    /// <param name="expression">表达式</param>
    /// <returns>变量名</returns>
    private static string ExtractVariableName(string expression)
    {
        var trimmed = expression.Trim();

        // 简单变量名
        if (IsValidIdentifier(trimmed))
        {
            return trimmed;
        }

        // 对于复杂表达式，返回整个表达式
        return trimmed;
    }

    /// <summary>
    /// 检查是否是有效的标识符
    /// </summary>
    /// <param name="identifier">标识符</param>
    /// <returns>是否有效</returns>
    private static bool IsValidIdentifier(string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
            return false;

        // 简单的标识符验证：以字母或下划线开头，只包含字母、数字和下划线
        return char.IsLetter(identifier[0]) &&
               identifier.All(c => char.IsLetterOrDigit(c) || c == '_');
    }
}

/// <summary>
/// 调用栈帧信息
/// </summary>
public class StackFrame
{
    /// <summary>
    /// 函数名
    /// </summary>
    public string FunctionName { get; set; } = string.Empty;

    /// <summary>
    /// 源文件路径
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// 行号
    /// </summary>
    public int Line { get; set; }

    /// <summary>
    /// 列号
    /// </summary>
    public int Column { get; set; }

    /// <summary>
    /// 当前作用域的变量
    /// </summary>
    public Dictionary<string, string> LocalVariables { get; set; } = new();

    /// <summary>
    /// 函数参数
    /// </summary>
    public Dictionary<string, string> Parameters { get; set; } = new();

    public override string ToString()
    {
        return $"{FunctionName} ({Path.GetFileName(FilePath)}:{Line})";
    }
}

/// <summary>
/// 调用栈管理器
/// </summary>
public class CallStack
{
    private readonly Stack<StackFrame> Frames = new();

    /// <summary>
    /// 压入新的栈帧
    /// </summary>
    /// <param name="frame">栈帧</param>
    public void PushFrame(StackFrame frame)
    {
        Frames.Push(frame);
    }

    /// <summary>
    /// 弹出栈帧
    /// </summary>
    /// <returns>弹出的栈帧，如果栈为空则返回null</returns>
    public StackFrame? PopFrame()
    {
        return Frames.Count > 0 ? Frames.Pop() : null;
    }

    /// <summary>
    /// 获取当前栈帧
    /// </summary>
    /// <returns>当前栈帧，如果栈为空则返回null</returns>
    public StackFrame? CurrentFrame => Frames.Count > 0 ? Frames.Peek() : null;

    /// <summary>
    /// 获取所有栈帧
    /// </summary>
    /// <returns>栈帧列表（从栈顶到栈底）</returns>
    public List<StackFrame> GetAllFrames()
    {
        return Frames.ToList();
    }

    /// <summary>
    /// 获取调用栈深度
    /// </summary>
    public int Depth => Frames.Count;

    /// <summary>
    /// 清空调用栈
    /// </summary>
    public void Clear()
    {
        Frames.Clear();
    }
}