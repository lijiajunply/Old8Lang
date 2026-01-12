namespace Old8Lang.Bytecode.Debugger;

/// <summary>
/// 断点信息
/// </summary>
public class Breakpoint
{
    /// <summary>断点ID</summary>
    public int Id { get; set; }

    /// <summary>源文件路径</summary>
    public string? FilePath { get; set; }

    /// <summary>行号</summary>
    public int Line { get; set; }

    /// <summary>指令偏移</summary>
    public int InstructionOffset { get; set; }

    /// <summary>是否启用</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>命中次数</summary>
    public int HitCount { get; set; }
}

/// <summary>
/// 调试器状态
/// </summary>
public enum DebuggerState
{
    /// <summary>未启动</summary>
    NotStarted,

    /// <summary>运行中</summary>
    Running,

    /// <summary>已暂停</summary>
    Paused,

    /// <summary>单步执行中</summary>
    Stepping,

    /// <summary>已完成</summary>
    Finished
}
