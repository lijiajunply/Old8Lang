using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;

namespace Old8Lang.LangParser;

/// <summary>
/// 生成器状态机
/// 参考C#的生成器实现，每个生成器实例都有独立的状态机
/// 负责管理生成器的执行状态、局部变量和控制流
/// </summary>
public class GeneratorStateMachine
{
    /// <summary>
    /// 生成器状态枚举
    /// </summary>
    public enum State
    {
        /// <summary>
        /// 未开始：生成器刚创建，还未执行第一次MoveNext
        /// </summary>
        NotStarted = 0,

        /// <summary>
        /// 运行中：正在执行生成器函数体
        /// </summary>
        Running = 1,

        /// <summary>
        /// 已暂停：执行到yield语句后暂停
        /// </summary>
        Suspended = 2,

        /// <summary>
        /// 已完成：遇到return或执行完所有语句
        /// </summary>
        Completed = 3
    }

    /// <summary>
    /// 当前状态
    /// </summary>
    public State CurrentState { get; set; } = State.NotStarted;

    /// <summary>
    /// 生成器函数引用
    /// </summary>
    public FuncLangValue GeneratorFunction { get; }

    /// <summary>
    /// 生成器的局部变量环境（独立副本）
    /// 每个生成器实例都有自己的变量副本，避免多个生成器实例互相干扰
    /// </summary>
    public VariateManager LocalEnvironment { get; }

    /// <summary>
    /// 生成器执行上下文
    /// </summary>
    public GeneratorExecutionContext ExecutionContext { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="generatorFunction">生成器函数</param>
    /// <param name="localEnvironment">局部变量环境</param>
    public GeneratorStateMachine(FuncLangValue generatorFunction, VariateManager localEnvironment)
    {
        GeneratorFunction = generatorFunction ?? throw new ArgumentNullException(nameof(generatorFunction));
        LocalEnvironment = localEnvironment ?? throw new ArgumentNullException(nameof(localEnvironment));
        ExecutionContext = new GeneratorExecutionContext();

        // 设置生成器上下文到环境中
        LocalEnvironment.GeneratorContext = ExecutionContext;
    }

    /// <summary>
    /// 执行到下一个yield点
    /// 这是生成器状态机的核心方法，类似C#的IEnumerator.MoveNext()
    /// </summary>
    /// <returns>如果还有更多值返回true，否则返回false</returns>
    public bool MoveNext()
    {
        // 如果已完成，直接返回false
        if (CurrentState == State.Completed)
        {
            return false;
        }

        try
        {
            // 设置为运行状态
            CurrentState = State.Running;

            // 清除上次yield的标志
            ExecutionContext.HasYielded = false;

            // 执行生成器函数体
            // BlockStatement会检查ExecutionContext.HasYielded来决定是否继续执行
            GeneratorFunction.BlockStatement.Run(LocalEnvironment);

            // 检查执行结果
            if (ExecutionContext.HasYielded)
            {
                // 遇到yield，暂停执行
                CurrentState = State.Suspended;
                return true;
            }
            else if (LocalEnvironment.IsReturn || ExecutionContext.IsCompleted)
            {
                // 遇到return或执行完毕，标记为完成
                CurrentState = State.Completed;
                return false;
            }
            else
            {
                // 正常执行完毕（没有遇到yield或return）
                CurrentState = State.Completed;
                return false;
            }
        }
        catch (Exception)
        {
            // 发生异常，标记为完成
            CurrentState = State.Completed;
            throw;
        }
    }

    /// <summary>
    /// 获取当前yield的值
    /// </summary>
    public LangValueType? Current => ExecutionContext.CurrentValue;

    /// <summary>
    /// 重置状态机到初始状态
    /// </summary>
    public void Reset()
    {
        CurrentState = State.NotStarted;
        ExecutionContext.Reset();
    }

    /// <summary>
    /// 获取状态机的字符串表示（用于调试）
    /// </summary>
    public override string ToString()
    {
        return $"GeneratorStateMachine[State={CurrentState}, Position={ExecutionContext.CurrentStatementIndex}]";
    }
}
