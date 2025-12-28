using Old8Lang.AST;
using Old8Lang.Interpreter;

namespace Old8Lang.Debugger;

/// <summary>
/// 调试状态
/// </summary>
public enum DebuggerState
{
    /// <summary>
    /// 未启动
    /// </summary>
    NotStarted,
    
    /// <summary>
    /// 运行中
    /// </summary>
    Running,
    
    /// <summary>
    /// 暂停（命中断点或单步执行）
    /// </summary>
    Paused,
    
    /// <summary>
    /// 单步执行中
    /// </summary>
    Stepping,
    
    /// <summary>
    /// 已完成
    /// </summary>
    Completed,
    
    /// <summary>
    /// 出错
    /// </summary>
    Error
}

/// <summary>
/// 单步执行类型
/// </summary>
public enum StepType
{
    /// <summary>
    /// 单步执行（进入函数）
    /// </summary>
    StepInto,
    
    /// <summary>
    /// 单步执行（跳过函数）
    /// </summary>
    StepOver,
    
    /// <summary>
    /// 单步执行（跳出函数）
    /// </summary>
    StepOut
}

/// <summary>
/// 调试器事件参数
/// </summary>
public class DebuggerEventArgs : EventArgs
{
    /// <summary>
    /// 事件类型
    /// </summary>
    public string EventType { get; set; } = string.Empty;
    
    /// <summary>
    /// 消息
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// 位置信息
    /// </summary>
    public SourcePosition Position { get; set; }
    
    /// <summary>
    /// 当前函数名
    /// </summary>
    public string? CurrentFunction { get; set; }
    
    /// <summary>
    /// 断点信息（如果是断点事件）
    /// </summary>
    public Breakpoint? Breakpoint { get; set; }
    
    /// <summary>
    /// 错误信息（如果是错误事件）
    /// </summary>
    public Exception? Error { get; set; }
}

/// <summary>
/// 调试器核心引擎
/// </summary>
public class Debugger
{
    private readonly BreakpointManager _breakpointManager = new();
    private readonly VariableWatcher _variableWatcher = new();
    private readonly CallStack _callStack = new();
    
    private DebuggerState _state = DebuggerState.NotStarted;
    private StepType? _pendingStep;
    private int _initialCallStackDepth;
    private readonly object _stateLock = new();
    
    /// <summary>
    /// 调试状态变化事件
    /// </summary>
    public event EventHandler<DebuggerEventArgs>? StateChanged;
    
    /// <summary>
    /// 断点命中事件
    /// </summary>
    public event EventHandler<DebuggerEventArgs>? BreakpointHit;
    
    /// <summary>
    /// 错误事件
    /// </summary>
    public event EventHandler<DebuggerEventArgs>? ErrorOccurred;
    
    /// <summary>
    /// 当前调试状态
    /// </summary>
    public DebuggerState State
    {
        get
        {
            lock (_stateLock)
            {
                return _state;
            }
        }
        private set
        {
            lock (_stateLock)
            {
                _state = value;
            }
        }
    }
    
    /// <summary>
    /// 断点管理器
    /// </summary>
    public BreakpointManager BreakpointManager => _breakpointManager;
    
    /// <summary>
    /// 变量监视器
    /// </summary>
    public VariableWatcher VariableWatcher => _variableWatcher;
    
    /// <summary>
    /// 调用栈
    /// </summary>
    public CallStack CallStack => _callStack;
    
    /// <summary>
    /// 启动调试
    /// </summary>
    /// <param name="filePath">文件路径</param>
    public void StartDebugging(string filePath)
    {
        State = DebuggerState.Running;
        RaiseStateChanged("调试开始", $"开始调试文件: {filePath}");
    }
    
    /// <summary>
    /// 停止调试
    /// </summary>
    public void StopDebugging()
    {
        State = DebuggerState.Completed;
        _callStack.Clear();
        RaiseStateChanged("调试结束", "调试会话已结束");
    }
    
    /// <summary>
    /// 暂停执行
    /// </summary>
    public void Pause()
    {
        State = DebuggerState.Paused;
        RaiseStateChanged("暂停执行", "程序已暂停");
    }
    
    /// <summary>
    /// 继续执行
    /// </summary>
    public void Continue()
    {
        _pendingStep = null;
        State = DebuggerState.Running;
        RaiseStateChanged("继续执行", "程序继续运行");
    }
    
    /// <summary>
    /// 单步执行
    /// </summary>
    /// <param name="stepType">单步类型</param>
    public void Step(StepType stepType)
    {
        _pendingStep = stepType;
        _initialCallStackDepth = _callStack.Depth;
        State = DebuggerState.Stepping;
        RaiseStateChanged("单步执行", $"开始单步执行: {stepType}");
    }
    
    /// <summary>
    /// 在语句执行前检查调试点
    /// </summary>
    /// <param name="statement">要执行的语句</param>
    /// <param name="manager">变量管理器</param>
    /// <param name="filePath">文件路径</param>
    /// <param name="currentFunction">当前函数名</param>
    /// <returns>是否应该暂停执行</returns>
    public bool CheckStatementExecution(OldStatement statement, VariateManager manager, string filePath, string? currentFunction = null)
    {
        // 如果调试器未运行，不进行检查
        if (State == DebuggerState.NotStarted || State == DebuggerState.Completed)
            return false;
        
        var position = statement.Position;
        var shouldPause = false;
        Breakpoint? hitBreakpoint = null;
        
        // 检查断点
        hitBreakpoint = _breakpointManager.CheckBreakpoint(position, filePath, currentFunction, manager);
        if (hitBreakpoint != null)
        {
            shouldPause = true;
            RaiseBreakpointHit(hitBreakpoint, position, currentFunction);
        }
        
        // 检查单步执行
        if (!shouldPause && _pendingStep.HasValue)
        {
            shouldPause = CheckStepping(_pendingStep.Value, position, currentFunction);
        }
        
        // 如果需要暂停
        if (shouldPause)
        {
            State = DebuggerState.Paused;
            UpdateCurrentContext(manager, filePath, currentFunction);
            
            // 等待用户命令（这里需要与调试器UI集成）
            WaitForUserCommand();
        }
        
        return shouldPause;
    }
    
    /// <summary>
    /// 检查函数调用
    /// </summary>
    /// <param name="functionName">函数名</param>
    /// <param name="manager">变量管理器</param>
    /// <param name="filePath">文件路径</param>
    /// <param name="position">位置信息</param>
    /// <returns>是否应该暂停执行</returns>
    public bool CheckFunctionCall(string functionName, VariateManager manager, string filePath, SourcePosition position)
    {
        // 检查函数断点
        var breakpoint = _breakpointManager.CheckBreakpoint(position, filePath, functionName, manager);
        if (breakpoint != null)
        {
            RaiseBreakpointHit(breakpoint, position, functionName);
            State = DebuggerState.Paused;
            UpdateCurrentContext(manager, filePath, functionName);
            WaitForUserCommand();
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// 进入函数
    /// </summary>
    /// <param name="functionName">函数名</param>
    /// <param name="filePath">文件路径</param>
    /// <param name="position">位置信息</param>
    /// <param name="manager">变量管理器</param>
    public void EnterFunction(string functionName, string filePath, SourcePosition position, VariateManager manager)
    {
        var frame = new StackFrame
        {
            FunctionName = functionName,
            FilePath = filePath,
            Line = position.Line,
            Column = position.Column,
            LocalVariables = GetLocalVariables(manager)
        };
        
        _callStack.PushFrame(frame);
    }
    
    /// <summary>
    /// 离开函数
    /// </summary>
    /// <returns>弹出的栈帧</returns>
    public StackFrame? ExitFunction()
    {
        return _callStack.PopFrame();
    }
    
    /// <summary>
    /// 处理错误
    /// </summary>
    /// <param name="exception">异常</param>
    /// <param name="position">位置信息</param>
    /// <param name="currentFunction">当前函数名</param>
    public void HandleError(Exception exception, SourcePosition position, string? currentFunction = null)
    {
        State = DebuggerState.Error;
        RaiseErrorOccurred(exception, position, currentFunction);
    }
    
    /// <summary>
    /// 检查单步执行条件
    /// </summary>
    /// <param name="stepType">单步类型</param>
    /// <param name="position">位置信息</param>
    /// <param name="currentFunction">当前函数名</param>
    /// <returns>是否应该暂停</returns>
    private bool CheckStepping(StepType stepType, SourcePosition position, string? currentFunction)
    {
        switch (stepType)
        {
            case StepType.StepInto:
                // StepInto: 在每个语句处暂停
                return true;
                
            case StepType.StepOver:
                // StepOver: 在当前层级的每个语句处暂停
                return _callStack.Depth <= _initialCallStackDepth;
                
            case StepType.StepOut:
                // StepOut: 在调用栈变浅时暂停
                return _callStack.Depth < _initialCallStackDepth;
                
            default:
                return false;
        }
    }
    
    /// <summary>
    /// 获取当前作用域的局部变量
    /// </summary>
    /// <param name="manager">变量管理器</param>
    /// <returns>局部变量字典</returns>
    private static Dictionary<string, string> GetLocalVariables(VariateManager manager)
    {
        var variables = manager.GetVariableStates(50); // 限制变量数量
        return variables.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value
        );
    }
    
    /// <summary>
    /// 更新当前上下文信息
    /// </summary>
    /// <param name="manager">变量管理器</param>
    /// <param name="filePath">文件路径</param>
    /// <param name="currentFunction">当前函数名</param>
    private void UpdateCurrentContext(VariateManager manager, string filePath, string? currentFunction)
    {
        // 更新监视变量
        _variableWatcher.UpdateAllWatches(manager);
        
        // 更新当前栈帧的变量信息
        if (_callStack.CurrentFrame != null)
        {
            _callStack.CurrentFrame.LocalVariables = GetLocalVariables(manager);
        }
    }
    
    /// <summary>
    /// 等待用户命令（这里需要与UI集成）
    /// </summary>
    private void WaitForUserCommand()
    {
        // 这里应该与调试器UI集成
        // 当前的实现是一个简化的版本
        // 在实际应用中，这里应该阻塞执行直到用户输入继续命令
    }
    
    /// <summary>
    /// 引发状态变化事件
    /// </summary>
    /// <param name="message">消息</param>
    /// <param name="detail">详细信息</param>
    private void RaiseStateChanged(string eventType, string message)
    {
        StateChanged?.Invoke(this, new DebuggerEventArgs
        {
            EventType = eventType,
            Message = message
        });
    }
    
    /// <summary>
    /// 引发断点命中事件
    /// </summary>
    /// <param name="breakpoint">断点</param>
    /// <param name="position">位置</param>
    /// <param name="currentFunction">当前函数</param>
    private void RaiseBreakpointHit(Breakpoint breakpoint, SourcePosition position, string? currentFunction)
    {
        BreakpointHit?.Invoke(this, new DebuggerEventArgs
        {
            EventType = "断点命中",
            Message = $"命中断点 {breakpoint} (第{breakpoint.HitCount}次)",
            Position = position,
            CurrentFunction = currentFunction,
            Breakpoint = breakpoint
        });
    }
    
    /// <summary>
    /// 引发错误事件
    /// </summary>
    /// <param name="exception">异常</param>
    /// <param name="position">位置</param>
    /// <param name="currentFunction">当前函数</param>
    private void RaiseErrorOccurred(Exception exception, SourcePosition position, string? currentFunction)
    {
        ErrorOccurred?.Invoke(this, new DebuggerEventArgs
        {
            EventType = "运行时错误",
            Message = exception.Message,
            Position = position,
            CurrentFunction = currentFunction,
            Error = exception
        });
    }
}