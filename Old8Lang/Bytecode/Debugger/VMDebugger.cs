using Old8Lang.Bytecode.Metadata;

namespace Old8Lang.Bytecode.Debugger;

/// <summary>
/// 虚拟机调试器
/// </summary>
public class VMDebugger
{
    /// <summary>断点集合</summary>
    private readonly Dictionary<int, Breakpoint> _breakpoints = new();

    /// <summary>按文件和行号索引的断点</summary>
    private readonly Dictionary<string, Dictionary<int, Breakpoint>> _breakpointsByLocation = new();

    /// <summary>下一个断点ID</summary>
    private int _nextBreakpointId = 1;

    /// <summary>当前调试器状态</summary>
    public DebuggerState State { get; private set; } = DebuggerState.NotStarted;

    /// <summary>当前执行的指令偏移</summary>
    public int CurrentInstructionOffset { get; private set; }

    /// <summary>调用栈</summary>
    private readonly Stack<CallStackFrame> _callStack = new();

    /// <summary>调试信息</summary>
    private readonly DebugInfo? _debugInfo;

    /// <summary>是否启用调试器</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>单步执行模式</summary>
    private StepMode _stepMode = StepMode.None;

    /// <summary>单步执行开始时的调用栈深度</summary>
    private int _stepStartDepth;

    /// <summary>
    /// 构造函数
    /// </summary>
    public VMDebugger(DebugInfo? debugInfo = null)
    {
        _debugInfo = debugInfo;
    }

    #region 断点管理

    /// <summary>
    /// 添加断点
    /// </summary>
    public Breakpoint AddBreakpoint(string filePath, int line)
    {
        var breakpoint = new Breakpoint
        {
            Id = _nextBreakpointId++,
            FilePath = filePath,
            Line = line,
            InstructionOffset = -1,
            Enabled = true
        };

        // 如果有调试信息,尝试找到对应的指令偏移
        if (_debugInfo != null)
        {
            foreach (var (offset, location) in _debugInfo.InstructionLocations)
            {
                if (location.FilePath == filePath && location.Line == line)
                {
                    breakpoint.InstructionOffset = offset;
                    break;
                }
            }
        }

        _breakpoints[breakpoint.Id] = breakpoint;

        // 按位置索引
        if (!_breakpointsByLocation.ContainsKey(filePath))
            _breakpointsByLocation[filePath] = new Dictionary<int, Breakpoint>();

        _breakpointsByLocation[filePath][line] = breakpoint;

        return breakpoint;
    }

    /// <summary>
    /// 添加指令偏移断点
    /// </summary>
    public Breakpoint AddBreakpoint(int instructionOffset)
    {
        var breakpoint = new Breakpoint
        {
            Id = _nextBreakpointId++,
            InstructionOffset = instructionOffset,
            Enabled = true
        };

        // 如果有调试信息,尝试找到对应的源码位置
        if (_debugInfo != null)
        {
            var location = _debugInfo.GetSourceLocation(instructionOffset);
            if (location != null)
            {
                breakpoint.FilePath = location.FilePath;
                breakpoint.Line = location.Line;
            }
        }

        _breakpoints[breakpoint.Id] = breakpoint;

        return breakpoint;
    }

    /// <summary>
    /// 移除断点
    /// </summary>
    public bool RemoveBreakpoint(int breakpointId)
    {
        if (!_breakpoints.TryGetValue(breakpointId, out var breakpoint))
            return false;

        _breakpoints.Remove(breakpointId);

        // 从位置索引中移除
        if (breakpoint.FilePath != null &&
            _breakpointsByLocation.TryGetValue(breakpoint.FilePath, out var lineBreakpoints))
        {
            lineBreakpoints.Remove(breakpoint.Line);
            if (lineBreakpoints.Count == 0)
                _breakpointsByLocation.Remove(breakpoint.FilePath);
        }

        return true;
    }

    /// <summary>
    /// 启用断点
    /// </summary>
    public bool EnableBreakpoint(int breakpointId)
    {
        if (!_breakpoints.TryGetValue(breakpointId, out var breakpoint))
            return false;

        breakpoint.Enabled = true;
        return true;
    }

    /// <summary>
    /// 禁用断点
    /// </summary>
    public bool DisableBreakpoint(int breakpointId)
    {
        if (!_breakpoints.TryGetValue(breakpointId, out var breakpoint))
            return false;

        breakpoint.Enabled = false;
        return true;
    }

    /// <summary>
    /// 获取所有断点
    /// </summary>
    public IReadOnlyCollection<Breakpoint> GetBreakpoints()
    {
        return _breakpoints.Values;
    }

    /// <summary>
    /// 检查指定位置是否有断点
    /// </summary>
    public bool HasBreakpoint(int instructionOffset)
    {
        foreach (var breakpoint in _breakpoints.Values)
        {
            if (breakpoint.Enabled && breakpoint.InstructionOffset == instructionOffset)
                return true;
        }
        return false;
    }

    #endregion

    #region 执行控制

    /// <summary>
    /// 开始调试会话
    /// </summary>
    public void Start()
    {
        if (!Enabled) return;

        State = DebuggerState.Running;
        CurrentInstructionOffset = 0;
        _callStack.Clear();
    }

    /// <summary>
    /// 继续执行
    /// </summary>
    public void Continue()
    {
        if (!Enabled) return;

        State = DebuggerState.Running;
    }

    /// <summary>
    /// 暂停执行
    /// </summary>
    public void Pause()
    {
        if (!Enabled) return;

        State = DebuggerState.Paused;
    }

    /// <summary>
    /// 单步执行（跳过函数调用）
    /// </summary>
    public void StepOver()
    {
        if (!Enabled) return;

        State = DebuggerState.Stepping;
        _stepMode = StepMode.Over;
        _stepStartDepth = _callStack.Count;
    }

    /// <summary>
    /// 单步进入（进入函数调用）
    /// </summary>
    public void StepInto()
    {
        if (!Enabled) return;

        State = DebuggerState.Stepping;
        _stepMode = StepMode.Into;
    }

    /// <summary>
    /// 单步跳出（跳出当前函数）
    /// </summary>
    public void StepOut()
    {
        if (!Enabled) return;

        State = DebuggerState.Stepping;
        _stepMode = StepMode.Out;
        _stepStartDepth = _callStack.Count;
    }

    /// <summary>
    /// 结束调试会话
    /// </summary>
    public void Finish()
    {
        if (!Enabled) return;

        State = DebuggerState.Finished;
        _callStack.Clear();
    }

    #endregion

    #region 指令执行检查

    /// <summary>
    /// 在执行指令前检查是否应该暂停
    /// </summary>
    /// <returns>是否应该暂停执行</returns>
    public bool ShouldPauseBeforeInstruction(int instructionOffset)
    {
        if (!Enabled) return false;

        CurrentInstructionOffset = instructionOffset;

        // 检查断点
        if (HasBreakpoint(instructionOffset))
        {
            // 更新断点命中次数
            foreach (var breakpoint in _breakpoints.Values)
            {
                if (breakpoint.Enabled && breakpoint.InstructionOffset == instructionOffset)
                {
                    breakpoint.HitCount++;
                    break;
                }
            }

            State = DebuggerState.Paused;
            return true;
        }

        // 检查单步执行
        if (State == DebuggerState.Stepping)
        {
            bool shouldPause = _stepMode switch
            {
                StepMode.Into => true, // 总是暂停
                StepMode.Over => _callStack.Count <= _stepStartDepth, // 只在相同或更浅的调用栈深度暂停
                StepMode.Out => _callStack.Count < _stepStartDepth, // 只在更浅的调用栈深度暂停
                _ => false
            };

            if (shouldPause)
            {
                State = DebuggerState.Paused;
                _stepMode = StepMode.None;
                return true;
            }
        }

        return State == DebuggerState.Paused;
    }

    #endregion

    #region 调用栈管理

    /// <summary>
    /// 进入函数调用
    /// </summary>
    public void EnterFunction(string functionName, int instructionOffset)
    {
        if (!Enabled) return;

        var frame = new CallStackFrame
        {
            FunctionName = functionName,
            InstructionOffset = instructionOffset
        };

        // 如果有调试信息,填充源码位置
        if (_debugInfo != null)
        {
            var location = _debugInfo.GetSourceLocation(instructionOffset);
            if (location != null)
            {
                frame.FilePath = location.FilePath;
                frame.Line = location.Line;
            }
        }

        _callStack.Push(frame);
    }

    /// <summary>
    /// 退出函数调用
    /// </summary>
    public void ExitFunction()
    {
        if (!Enabled) return;

        if (_callStack.Count > 0)
            _callStack.Pop();
    }

    /// <summary>
    /// 获取当前调用栈
    /// </summary>
    public IReadOnlyCollection<CallStackFrame> GetCallStack()
    {
        return _callStack.ToArray();
    }

    /// <summary>
    /// 获取当前栈帧
    /// </summary>
    public CallStackFrame? GetCurrentFrame()
    {
        return _callStack.Count > 0 ? _callStack.Peek() : null;
    }

    #endregion

    #region 变量查看

    /// <summary>
    /// 更新当前栈帧的局部变量
    /// </summary>
    public void UpdateLocalVariable(string name, object? value)
    {
        if (!Enabled) return;

        var frame = GetCurrentFrame();
        if (frame != null)
        {
            frame.LocalVariables[name] = value;
        }
    }

    /// <summary>
    /// 获取局部变量值
    /// </summary>
    public object? GetLocalVariable(string name)
    {
        if (!Enabled) return null;

        var frame = GetCurrentFrame();
        if (frame != null && frame.LocalVariables.TryGetValue(name, out var value))
        {
            return value;
        }

        return null;
    }

    /// <summary>
    /// 获取所有局部变量
    /// </summary>
    public IReadOnlyDictionary<string, object?> GetLocalVariables()
    {
        if (!Enabled) return new Dictionary<string, object?>();

        var frame = GetCurrentFrame();
        return frame?.LocalVariables ?? new Dictionary<string, object?>();
    }

    /// <summary>
    /// 获取指定栈帧的局部变量
    /// </summary>
    public IReadOnlyDictionary<string, object?> GetLocalVariables(int frameIndex)
    {
        if (!Enabled) return new Dictionary<string, object?>();

        var frames = _callStack.ToArray();
        if (frameIndex >= 0 && frameIndex < frames.Length)
        {
            return frames[frameIndex].LocalVariables;
        }

        return new Dictionary<string, object?>();
    }

    #endregion
}

/// <summary>
/// 调用栈帧
/// </summary>
public class CallStackFrame
{
    /// <summary>函数名称</summary>
    public string FunctionName { get; set; } = "";

    /// <summary>指令偏移</summary>
    public int InstructionOffset { get; set; }

    /// <summary>源文件路径</summary>
    public string? FilePath { get; set; }

    /// <summary>行号</summary>
    public int Line { get; set; }

    /// <summary>局部变量</summary>
    public Dictionary<string, object?> LocalVariables { get; set; } = new();
}

/// <summary>
/// 单步执行模式
/// </summary>
internal enum StepMode
{
    /// <summary>无单步执行</summary>
    None,

    /// <summary>单步跳过（不进入函数）</summary>
    Over,

    /// <summary>单步进入（进入函数）</summary>
    Into,

    /// <summary>单步跳出（跳出当前函数）</summary>
    Out
}
