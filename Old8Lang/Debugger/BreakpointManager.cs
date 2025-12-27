using Old8Lang.AST.Expression;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.Debugger;

/// <summary>
/// 断点类型
/// </summary>
public enum BreakpointType
{
    /// <summary>
    /// 行断点
    /// </summary>
    Line,
    
    /// <summary>
    /// 函数断点
    /// </summary>
    Function,
    
    /// <summary>
    /// 条件断点
    /// </summary>
    Conditional
}

/// <summary>
/// 断点信息
/// </summary>
public class Breakpoint
{
    /// <summary>
    /// 断点ID
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// 断点类型
    /// </summary>
    public BreakpointType Type { get; set; }
    
    /// <summary>
    /// 源文件路径
    /// </summary>
    public string FilePath { get; set; } = string.Empty;
    
    /// <summary>
    /// 行号（仅对行断点有效）
    /// </summary>
    public int Line { get; set; }
    
    /// <summary>
    /// 函数名（仅对函数断点有效）
    /// </summary>
    public string FunctionName { get; set; } = string.Empty;
    
    /// <summary>
    /// 条件表达式（仅对条件断点有效）
    /// </summary>
    public string? Condition { get; set; }
    
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>
    /// 命中次数
    /// </summary>
    public int HitCount { get; set; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    public override string ToString()
    {
        return Type switch
        {
            BreakpointType.Line => $"{FilePath}:{Line}",
            BreakpointType.Function => $"函数 {FunctionName}",
            BreakpointType.Conditional => $"{FilePath}:{Line} (条件: {Condition})",
            _ => "未知断点"
        };
    }
}

/// <summary>
/// 断点管理器
/// </summary>
public class BreakpointManager
{
    private readonly Dictionary<int, Breakpoint> _breakpoints = new();
    private int _nextId = 1;
    
    /// <summary>
    /// 添加行断点
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <param name="line">行号</param>
    /// <param name="condition">条件（可选）</param>
    /// <returns>断点ID</returns>
    public int AddLineBreakpoint(string filePath, int line, string? condition = null)
    {
        var breakpoint = new Breakpoint
        {
            Id = _nextId++,
            Type = string.IsNullOrEmpty(condition) ? BreakpointType.Line : BreakpointType.Conditional,
            FilePath = filePath,
            Line = line,
            Condition = condition
        };
        
        _breakpoints[breakpoint.Id] = breakpoint;
        return breakpoint.Id;
    }
    
    /// <summary>
    /// 添加函数断点
    /// </summary>
    /// <param name="functionName">函数名</param>
    /// <returns>断点ID</returns>
    public int AddFunctionBreakpoint(string functionName)
    {
        var breakpoint = new Breakpoint
        {
            Id = _nextId++,
            Type = BreakpointType.Function,
            FunctionName = functionName
        };
        
        _breakpoints[breakpoint.Id] = breakpoint;
        return breakpoint.Id;
    }
    
    /// <summary>
    /// 移除断点
    /// </summary>
    /// <param name="id">断点ID</param>
    /// <returns>是否成功移除</returns>
    public bool RemoveBreakpoint(int id)
    {
        return _breakpoints.Remove(id);
    }
    
    /// <summary>
    /// 启用/禁用断点
    /// </summary>
    /// <param name="id">断点ID</param>
    /// <param name="enabled">是否启用</param>
    /// <returns>是否成功设置</returns>
    public bool SetBreakpointEnabled(int id, bool enabled)
    {
        if (_breakpoints.TryGetValue(id, out var breakpoint))
        {
            breakpoint.IsEnabled = enabled;
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// 检查是否命中断点
    /// </summary>
    /// <param name="position">当前位置</param>
    /// <param name="filePath">文件路径</param>
    /// <param name="currentFunction">当前函数名</param>
    /// <param name="manager">变量管理器（用于条件断点）</param>
    /// <returns>命中的断点，如果没有命中则返回null</returns>
    public Breakpoint? CheckBreakpoint(SourcePosition position, string filePath, string? currentFunction = null, Interpreter.VariateManager? manager = null)
    {
        // 检查行断点
        foreach (var breakpoint in _breakpoints.Values.Where(b => b.IsEnabled))
        {
            switch (breakpoint.Type)
            {
                case BreakpointType.Line:
                case BreakpointType.Conditional:
                    if (breakpoint.FilePath == filePath && breakpoint.Line == position.Line)
                    {
                        // 检查条件断点
                        if (breakpoint.Type == BreakpointType.Conditional && !string.IsNullOrEmpty(breakpoint.Condition))
                        {
                            if (manager != null && EvaluateCondition(breakpoint.Condition, manager))
                            {
                                breakpoint.HitCount++;
                                return breakpoint;
                            }
                        }
                        else
                        {
                            breakpoint.HitCount++;
                            return breakpoint;
                        }
                    }
                    break;
                    
                case BreakpointType.Function:
                    if (!string.IsNullOrEmpty(currentFunction) && 
                        breakpoint.FunctionName == currentFunction)
                    {
                        breakpoint.HitCount++;
                        return breakpoint;
                    }
                    break;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// 评估条件表达式
    /// </summary>
    /// <param name="condition">条件表达式</param>
    /// <param name="manager">变量管理器</param>
    /// <returns>条件是否为真</returns>
    private static bool EvaluateCondition(string condition, Interpreter.VariateManager manager)
    {
        try
        {
            // 这里可以扩展为更复杂的条件表达式评估
            // 目前简单实现：检查变量是否存在且不为空/false
            if (condition.StartsWith("!"))
            {
                var varName = condition.Substring(1).Trim();
                var value = manager.GetValue(new LangId(varName));
                return value == null || value.ToString() == "false" || value.ToString() == "";
            }
            else
            {
                var value = manager.GetValue(new LangId(condition));
                return value != null && value.ToString() != "false" && value.ToString() != "";
            }
        }
        catch
        {
            // 条件评估失败时返回false，不中断执行
            return false;
        }
    }
    
    /// <summary>
    /// 获取所有断点
    /// </summary>
    /// <returns>断点列表</returns>
    public List<Breakpoint> GetAllBreakpoints()
    {
        return _breakpoints.Values.ToList();
    }
    
    /// <summary>
    /// 获取指定文件的所有断点
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>断点列表</returns>
    public List<Breakpoint> GetBreakpointsInFile(string filePath)
    {
        return _breakpoints.Values
            .Where(b => b.FilePath == filePath)
            .ToList();
    }
    
    /// <summary>
    /// 清除所有断点
    /// </summary>
    public void ClearAllBreakpoints()
    {
        _breakpoints.Clear();
        _nextId = 1;
    }
}