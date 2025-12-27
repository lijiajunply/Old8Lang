using Old8Lang.AST.Expression;
using Old8Lang.AST.Statement;
using Old8Lang.Interpreter;

namespace Old8Lang.Generators;

/// <summary>
/// 新的生成器状态机实现
/// 基于 C# 迭代器的设计原理：将生成器函数编译为状态机
/// </summary>
public class GeneratorStateMachine
{
    /// <summary>
    /// 状态机状态
    /// </summary>
    public enum MachineState
    {
        NotStarted = 0,    // 未开始
        Running = 1,       // 运行中
        Suspended = 2,     // 暂停（yield）
        Completed = 3      // 完成
    }

    /// <summary>
    /// 当前状态机状态
    /// </summary>
    public MachineState CurrentState { get; private set; } = MachineState.NotStarted;

    /// <summary>
    /// 当前执行到的状态点（每个 yield 对应一个状态点）
    /// </summary>
    private int StatePoint;

    /// <summary>
    /// 当前 yield 的值
    /// </summary>
    public LangValueType? CurrentValue { get; private set; }

    /// <summary>
    /// 局部变量存储（提升的局部变量）
    /// Key: 变量名, Value: 变量值
    /// </summary>
    private readonly Dictionary<string, LangValueType> Locals = new();

    /// <summary>
    /// 环境管理器（用于执行表达式）
    /// </summary>
    private readonly VariateManager Manager;

    /// <summary>
    /// 生成器函数定义
    /// </summary>
    private readonly FuncInit Function;

    /// <summary>
    /// 状态机执行器（由构建器生成）
    /// </summary>
    private readonly StateExecutor Executor;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="function">生成器函数定义</param>
    /// <param name="manager">环境管理器</param>
    /// <param name="executor">状态机执行器</param>
    public GeneratorStateMachine(FuncInit function, VariateManager manager, StateExecutor executor)
    {
        Function = function;
        Manager = manager;
        Executor = executor;
    }

    /// <summary>
    /// 移动到下一个值
    /// </summary>
    /// <returns>如果还有下一个值返回 true，否则返回 false</returns>
    public bool MoveNext()
    {
        // 如果已完成，直接返回 false
        if (CurrentState == MachineState.Completed)
        {
            return false;
        }

        try
        {
            // 设置为运行状态
            CurrentState = MachineState.Running;

            // 执行状态机
            var result = Executor.Execute(StatePoint, Locals, Manager);

            if (result.HasValue)
            {
                // 遇到 yield
                CurrentValue = result.YieldValue;
                StatePoint = result.NextState;
                CurrentState = MachineState.Suspended;
                return true;
            }

            // 执行完毕
            CurrentState = MachineState.Completed;
            return false;
        }
        catch
        {
            CurrentState = MachineState.Completed;
            throw;
        }
    }

    /// <summary>
    /// 重置状态机（不支持）
    /// </summary>
    public void Reset()
    {
        throw new NotSupportedException("Generator cannot be reset");
    }
}

/// <summary>
/// 状态机执行器
/// 负责根据状态点执行相应的代码
/// </summary>
public abstract class StateExecutor
{
    /// <summary>
    /// 执行结果
    /// </summary>
    public struct ExecutionResult
    {
        /// <summary>
        /// 是否有 yield 值
        /// </summary>
        public bool HasValue { get; set; }

        /// <summary>
        /// Yield 的值
        /// </summary>
        public LangValueType? YieldValue { get; set; }

        /// <summary>
        /// 下一个状态点
        /// </summary>
        public int NextState { get; set; }

        /// <summary>
        /// 创建 yield 结果
        /// </summary>
        public static ExecutionResult Yield(LangValueType value, int nextState)
        {
            return new ExecutionResult
            {
                HasValue = true,
                YieldValue = value,
                NextState = nextState
            };
        }

        /// <summary>
        /// 创建完成结果
        /// </summary>
        public static ExecutionResult Complete()
        {
            return new ExecutionResult
            {
                HasValue = false
            };
        }
    }

    /// <summary>
    /// 执行指定状态点的代码
    /// </summary>
    /// <param name="statePoint">状态点</param>
    /// <param name="locals">局部变量</param>
    /// <param name="manager">环境管理器</param>
    /// <returns>执行结果</returns>
    public abstract ExecutionResult Execute(int statePoint, Dictionary<string, LangValueType> locals, VariateManager manager);
}
