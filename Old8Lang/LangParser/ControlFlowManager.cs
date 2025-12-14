namespace Old8Lang.LangParser;

/// <summary>
/// 控制流管理器，用于管理break和continue等控制流语句的状态
/// </summary>
public class ControlFlowManager
{
    /// <summary>
    /// 控制流状态栈，用于处理嵌套循环的控制流
    /// </summary>
    private readonly Stack<ControlFlowState> ControlFlowStack = [];

    /// <summary>
    /// 压入新的控制流状态
    /// </summary>
    public void PushState()
    {
        ControlFlowStack.Push(new ControlFlowState());
    }

    /// <summary>
    /// 弹出当前控制流状态
    /// </summary>
    public void PopState()
    {
        if (ControlFlowStack.Count > 0)
        {
            ControlFlowStack.Pop();
        }
    }

    /// <summary>
    /// 重置当前控制流状态
    /// 用于在每次循环迭代开始时重置标志位
    /// </summary>
    public void ResetCurrentState()
    {
        if (ControlFlowStack.Count > 0)
        {
            ControlFlowStack.Pop();
            ControlFlowStack.Push(new ControlFlowState());
        }
    }

    /// <summary>
    /// Break标志位
    /// </summary>
    public bool BreakFlag
    {
        get => ControlFlowStack.Count > 0 && ControlFlowStack.Peek().BreakFlag;
        set
        {
            if (ControlFlowStack.Count > 0)
            {
                ControlFlowStack.Peek().BreakFlag = value;
            }
        }
    }

    /// <summary>
    /// Continue标志位
    /// </summary>
    public bool ContinueFlag
    {
        get => ControlFlowStack.Count > 0 && ControlFlowStack.Peek().ContinueFlag;
        set
        {
            if (ControlFlowStack.Count > 0)
            {
                ControlFlowStack.Peek().ContinueFlag = value;
            }
        }
    }

    /// <summary>
    /// 控制流状态类，用于存储break和continue标志
    /// </summary>
    private class ControlFlowState
    {
        /// <summary>
        /// Break标志
        /// </summary>
        public bool BreakFlag { get; set; }

        /// <summary>
        /// Continue标志
        /// </summary>
        public bool ContinueFlag { get; set; }
    }
}