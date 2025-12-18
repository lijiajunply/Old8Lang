using System.Reflection.Emit;
using Old8Lang.Compiler;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// 异步生成器对象，用于表示异步生成器函数的实例，实现ILangList接口以支持异步迭代
/// 类似于 C# 的 IAsyncEnumerable&lt;T&gt;
/// </summary>
public class AsyncGeneratorLangValue : LangValueType, ILangList
{
    /// <summary>
    /// 异步函数引用
    /// </summary>
    public AsyncFuncLangValue AsyncFunc { get; init; }

    /// <summary>
    /// 异步生成器状态机
    /// </summary>
    private AsyncGeneratorStateMachine? StateMachine { get; set; }

    /// <summary>
    /// 生成器当前状态
    /// </summary>
    public AsyncGeneratorState State { get; set; } = AsyncGeneratorState.Suspended;

    /// <summary>
    /// 生成器迭代器的下一个值
    /// </summary>
    public LangValueType? NextValue { get; set; }

    /// <summary>
    /// 生成器函数的参数值
    /// </summary>
    private Dictionary<string, LangValueType> ParameterValues { get; } = new();

    /// <summary>
    /// 取消令牌源，用于取消异步操作
    /// </summary>
    private CancellationTokenSource? CancellationTokenSource { get; set; }

    /// <summary>
    /// 异步生成器状态枚举
    /// </summary>
    public enum AsyncGeneratorState
    {
        Suspended,   // 已暂停，等待下一个值
        Running,     // 正在执行
        Completed    // 已完成
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="asyncFunc">异步函数引用</param>
    /// <param name="position">源代码位置</param>
    public AsyncGeneratorLangValue(AsyncFuncLangValue asyncFunc, SourcePosition position = default) : base(position)
    {
        AsyncFunc = asyncFunc;
        CancellationTokenSource = new CancellationTokenSource();
    }

    /// <summary>
    /// 设置生成器函数的参数值
    /// </summary>
    /// <param name="paramName">参数名称</param>
    /// <param name="value">参数值</param>
    public void SetParameter(string paramName, LangValueType value)
    {
        ParameterValues[paramName] = value;
    }

    /// <summary>
    /// 异步运行生成器，返回下一个值的 Task
    /// </summary>
    /// <param name="manager">变量管理器</param>
    /// <returns>包含下一个值的 TaskLangValue</returns>
    public TaskLangValue RunAsync(VariateManager manager)
    {
        // 如果状态机还未初始化，则创建它
        if (StateMachine == null)
        {
            // 为生成器创建独立的变量环境
            var generatorManager = manager.CloneForGenerator();

            // 设置参数值到生成器环境中
            foreach (var (paramName, paramValue) in ParameterValues)
            {
                generatorManager.Set(new LangId(paramName), paramValue);
            }

            // 创建异步状态机
            StateMachine = new AsyncGeneratorStateMachine(AsyncFunc, generatorManager,
                CancellationTokenSource?.Token ?? default);
        }

        // 异步获取下一个值
        var task = Task.Run(async () =>
        {
            if (await StateMachine.MoveNextAsync())
            {
                // 还有更多值
                State = AsyncGeneratorState.Suspended;
                NextValue = StateMachine.Current;
                return NextValue ?? new VoidLangValue();
            }
            else
            {
                // 生成器完成
                State = AsyncGeneratorState.Completed;
                return (LangValueType)new VoidLangValue();
            }
        }, CancellationTokenSource?.Token ?? default);

        return new TaskLangValue(task, CancellationTokenSource?.Token ?? default, Position);
    }

    /// <summary>
    /// 同步运行方法，用于向后兼容，实际会阻塞等待异步操作完成
    /// </summary>
    /// <param name="manager">变量管理器</param>
    /// <returns>生成器的下一个值</returns>
    public override LangValueType Run(VariateManager manager)
    {
        // 调用异步方法并阻塞等待
        var taskResult = RunAsync(manager);
        return taskResult.Await();
    }

    /// <summary>
    /// 作为可调用对象执行，返回下一个值
    /// </summary>
    /// <param name="manager">变量管理器</param>
    /// <param name="args">参数列表（生成器调用不需要参数）</param>
    /// <param name="obj">对象实例（生成器调用不需要）</param>
    /// <returns>生成器的下一个值</returns>
    public LangValueType Run(VariateManager manager, List<LangExpression> args, object? obj = null)
    {
        // 异步生成器调用不需要参数，忽略args
        return Run(manager);
    }

    /// <summary>
    /// 取消异步生成器的执行
    /// </summary>
    public void Cancel()
    {
        CancellationTokenSource?.Cancel();
    }

    /// <summary>
    /// 重置生成器状态
    /// </summary>
    public void Reset()
    {
        State = AsyncGeneratorState.Suspended;
        NextValue = null;
        StateMachine?.Reset();

        // 创建新的取消令牌源
        CancellationTokenSource?.Dispose();
        CancellationTokenSource = new CancellationTokenSource();
    }

    /// <summary>
    /// 获取生成器的输出类型
    /// </summary>
    /// <param name="local">局部变量管理器</param>
    /// <returns>生成器的输出类型</returns>
    public override Type OutputType(LocalManager local) => typeof(object);

    /// <summary>
    /// 生成IL代码（后续实现）
    /// </summary>
    /// <param name="ilGenerator">IL生成器</param>
    /// <param name="local">局部变量管理器</param>
    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // 异步生成器的IL代码生成（后续实现）
    }

    /// <summary>
    /// 设置值到IL代码（后续实现）
    /// </summary>
    /// <param name="ilGenerator">IL生成器</param>
    /// <param name="local">局部变量管理器</param>
    /// <param name="idName">标识符名称</param>
    public override void SetValueToIl(ILGenerator ilGenerator, LocalManager local, string idName)
    {
        // 异步生成器的IL代码生成（后续实现）
    }

    /// <summary>
    /// 转换为字符串
    /// </summary>
    /// <returns>异步生成器的字符串表示</returns>
    public override string ToString() => $"AsyncGenerator({AsyncFunc.Id?.IdName ?? "anonymous"})";

    /// <summary>
    /// 获取生成器的所有项
    /// </summary>
    /// <returns>生成器项的枚举</returns>
    public IEnumerable<LangValueType> GetItems()
    {
        // 异步生成器的迭代逻辑由AsyncForInStatement处理，这里只返回空枚举
        // 避免在迭代过程中影响生成器的状态
        yield break;
    }

    /// <summary>
    /// 获取生成器的长度
    /// </summary>
    /// <returns>生成器的长度，-1表示未知长度</returns>
    public int GetLength()
    {
        // 异步生成器的长度通常是未知的，返回-1表示未知长度
        return -1;
    }

    /// <summary>
    /// 对生成器进行切片
    /// </summary>
    /// <param name="start">起始索引</param>
    /// <param name="end">结束索引</param>
    /// <returns>切片后的生成器</returns>
    public LangValueType Slice(int start, int end)
    {
        // 异步生成器不支持切片
        throw new NotSupportedException("异步生成器不支持切片操作");
    }

    /// <summary>
    /// 设置生成器中指定索引的值
    /// </summary>
    /// <param name="index">索引</param>
    /// <param name="value">值</param>
    /// <exception cref="NotSupportedException">异步生成器不支持设置值</exception>
    public void Set(LangValueType index, LangValueType value)
    {
        // 异步生成器是只读的，不支持设置值
        throw new NotSupportedException("异步生成器不支持设置值");
    }

    /// <summary>
    /// 检查值是否在生成器中
    /// </summary>
    /// <param name="value">要检查的值</param>
    /// <returns>如果值在生成器中则返回true，否则返回false</returns>
    public bool In(LangValueType value)
    {
        // 迭代生成器，检查是否包含指定值
        foreach (var item in GetItems())
        {
            if (item.ToString() == value.ToString())
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        CancellationTokenSource?.Dispose();
    }
}
