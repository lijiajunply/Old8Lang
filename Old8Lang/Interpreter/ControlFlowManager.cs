namespace Old8Lang.Interpreter;

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
    /// 压入新的控制流状态（从对象池获取）
    /// </summary>
    public void PushState()
    {
        var state = ObjectPoolManager.Instance.ControlFlowStatePool.Get();
        ControlFlowStack.Push(state);
    }

    /// <summary>
    /// 弹出当前控制流状态（归还到对象池）
    /// </summary>
    public void PopState()
    {
        if (ControlFlowStack.Count > 0)
        {
            var state = ControlFlowStack.Pop();
            ObjectPoolManager.Instance.ControlFlowStatePool.Return(state);
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
            // 直接重置当前状态，不再Pop/Push，避免对象创建
            var currentState = ControlFlowStack.Peek();
            currentState.BreakFlag = false;
            currentState.ContinueFlag = false;
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
    /// 控制流状态类，用于存储break和continue标志，支持对象池复用
    /// </summary>
    public class ControlFlowState : IPoolable
    {
        /// <summary>
        /// Break标志
        /// </summary>
        public bool BreakFlag { get; set; }

        /// <summary>
        /// Continue标志
        /// </summary>
        public bool ContinueFlag { get; set; }

        /// <summary>
        /// 重置状态，供对象池复用
        /// </summary>
        public void Reset()
        {
            BreakFlag = false;
            ContinueFlag = false;
        }
    }
}