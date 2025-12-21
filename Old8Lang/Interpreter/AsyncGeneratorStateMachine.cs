using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Generators;

namespace Old8Lang.Interpreter;

/// <summary>
/// 异步生成器状态机
/// 类似于 GeneratorStateMachine，但支持异步操作
/// 每个异步生成器实例都有独立的状态机
/// 负责管理异步生成器的执行状态、局部变量和控制流
/// </summary>
public class AsyncGeneratorStateMachine
{
    /// <summary>
    /// 生成器状态枚举
    /// </summary>
    public enum State
    {
        /// <summary>
        /// 未开始：生成器刚创建，还未执行第一次MoveNextAsync
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
    /// 异步函数引用
    /// </summary>
    public AsyncFuncLangValue AsyncGeneratorFunction { get; }

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
    /// 取消令牌
    /// </summary>
    private CancellationToken CancellationToken { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="asyncGeneratorFunction">异步生成器函数</param>
    /// <param name="localEnvironment">局部变量环境</param>
    /// <param name="cancellationToken">取消令牌</param>
    public AsyncGeneratorStateMachine(
        AsyncFuncLangValue asyncGeneratorFunction,
        VariateManager localEnvironment,
        CancellationToken cancellationToken = default)
    {
        AsyncGeneratorFunction = asyncGeneratorFunction ??
                                 throw new ArgumentNullException(nameof(asyncGeneratorFunction));
        LocalEnvironment = localEnvironment ?? throw new ArgumentNullException(nameof(localEnvironment));
        ExecutionContext = new GeneratorExecutionContext();
        CancellationToken = cancellationToken;

        // 设置生成器上下文到环境中
        LocalEnvironment.GeneratorContext = ExecutionContext;
    }

    /// <summary>
    /// 异步执行到下一个yield点
    /// 这是异步生成器状态机的核心方法，类似C#的IAsyncEnumerator.MoveNextAsync()
    /// </summary>
    /// <returns>如果还有更多值返回true，否则返回false</returns>
    public async Task<bool> MoveNextAsync()
    {
        // 如果已完成，直接返回false
        if (CurrentState == State.Completed)
        {
            return false;
        }

        // 检查取消请求
        CancellationToken.ThrowIfCancellationRequested();

        try
        {
            // 设置为运行状态
            CurrentState = State.Running;

            // 清除上次yield的标志
            ExecutionContext.HasYielded = false;

            // 执行异步生成器函数体
            // 由于 BlockStatement.Run 是同步的，我们在Task中执行它
            // 真正的异步操作发生在 await 表达式中
            await Task.Run(() =>
            {
                CancellationToken.ThrowIfCancellationRequested();
                AsyncGeneratorFunction.BlockStatement.Run(LocalEnvironment);
            }, CancellationToken);

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
        return
            $"AsyncGeneratorStateMachine[State={CurrentState}, Position={ExecutionContext.CurrentStatementIndex}]";
    }
}
