using Old8Lang.AST.Expression;

namespace Old8Lang.Generators;

/// <summary>
/// 生成器执行上下文
/// 用于在生成器执行期间保存和恢复状态，替代全局的IsYield和IsInGenerator标志
/// 参考C#的生成器状态机设计，每个生成器实例都有独立的执行上下文
/// </summary>
public class GeneratorExecutionContext
{
    /// <summary>
    /// 当前执行的语句索引（在BlockStatement中的位置）
    /// 【旧架构】用于基于索引的状态恢复
    /// </summary>
    public int CurrentStatementIndex { get; set; } = 0;

    /// <summary>
    /// 是否遇到了yield语句
    /// </summary>
    public bool HasYielded { get; set; }

    /// <summary>
    /// 当前yield的值
    /// </summary>
    public LangValueType? CurrentValue { get; set; }

    /// <summary>
    /// 是否已完成（遇到return或执行完所有语句）
    /// </summary>
    public bool IsCompleted { get; set; } = false;

    /// <summary>
    /// 执行栈，用于跟踪嵌套的BlockStatement执行位置
    /// 例如：在if语句或循环内部的BlockStatement
    /// 【旧架构】基于索引的栈帧
    /// </summary>
    public Stack<BlockExecutionFrame> ExecutionStack { get; set; } = new();

    /// <summary>
    /// 执行路径（新架构）
    /// 记录从函数体根节点到当前执行位置的完整路径
    /// 例如："/block[0]/for-in/block[1]/yield"
    /// </summary>
    public string? ExecutionPath { get; set; }

    /// <summary>
    /// 循环状态字典（新架构）
    /// Key: 循环路径（如 "/block[0]/for-in"）
    /// Value: 当前迭代的索引
    /// </summary>
    public Dictionary<string, int> LoopStates { get; set; } = new();

    /// <summary>
    /// 重置上下文状态
    /// </summary>
    public void Reset()
    {
        CurrentStatementIndex = 0;
        HasYielded = false;
        CurrentValue = null;
        IsCompleted = false;
        ExecutionStack.Clear();
        ExecutionPath = null;
        LoopStates.Clear();
    }

    /// <summary>
    /// 块语句执行帧，用于保存嵌套块的执行位置
    /// 【旧架构】
    /// </summary>
    public class BlockExecutionFrame
    {
        /// <summary>
        /// 块中的语句索引
        /// </summary>
        public int StatementIndex { get; set; }

        /// <summary>
        /// 块的标识符（用于调试）
        /// </summary>
        public string? BlockId { get; set; }

        /// <summary>
        /// 循环迭代次数（如果在循环中）
        /// </summary>
        public int? LoopIteration { get; set; }
    }
}
