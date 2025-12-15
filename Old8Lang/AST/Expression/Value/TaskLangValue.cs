using Old8Lang.AST;
using Old8Lang.LangParser;
using System.Reflection.Emit;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Compiler;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// Task 值类型，表示异步操作
/// 包装 .NET Task&lt;LangValueType&gt;，支持异步执行和 await 等待
/// </summary>
public class TaskLangValue : LangValueType
{
    private readonly Task<LangValueType> _task;
    private bool _isCompleted = false;
    private LangValueType? _result = null;
    private Exception? _exception = null;

    /// <summary>
    /// 获取任务结果（如果已完成）
    /// </summary>
    public LangValueType? Result => _result;

    /// <summary>
    /// 获取任务异常（如果已失败）
    /// </summary>
    public Exception? Exception => _exception;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="task">.NET Task对象</param>
    /// <param name="position">源代码位置</param>
    public TaskLangValue(Task<LangValueType> task, SourcePosition position = default)
        : base(position)
    {
        _task = task;

        // 注册完成回调，缓存结果
        _task.ContinueWith(t =>
        {
            _isCompleted = true;
            if (t.IsFaulted)
            {
                _exception = t.Exception?.InnerException;
            }
            else if (t.IsCompletedSuccessfully)
            {
                _result = t.Result;
            }
        });
    }

    /// <summary>
    /// 等待 Task 完成并返回结果（阻塞）
    /// </summary>
    /// <returns>任务的执行结果</returns>
    /// <exception cref="Exception">任务执行过程中发生的异常</exception>
    public LangValueType Await()
    {
        try
        {
            // 同步等待任务完成并获取结果
            var result = _task.Result;
            
            // 更新缓存状态
            _isCompleted = true;
            _result = result;
            
            return result;
        }
        catch (AggregateException aggEx)
        {
            // 展开 AggregateException，抛出内部异常
            var innerException = aggEx.InnerException ?? aggEx;
            _isCompleted = true;
            _exception = innerException;
            throw innerException;
        }
    }

    /// <summary>
    /// 异步等待 Task 完成并返回结果（非阻塞）
    /// </summary>
    public async Task<LangValueType> AwaitAsync()
    {
        return await AwaitAsync(-1); // 默认无超时
    }

    /// <summary>
    /// 异步等待 Task 完成并返回结果（非阻塞，带超时）
    /// </summary>
    /// <param name="timeoutMs">超时时间（毫秒），-1 表示无超时</param>
    /// <returns>任务结果</returns>
    /// <exception cref="TimeoutException">任务超时</exception>
    public async Task<LangValueType> AwaitAsync(int timeoutMs)
    {
        try
        {
            LangValueType result;
            if (timeoutMs <= 0)
            {
                result = await _task;
            }
            else
            {
                var completedTask = await Task.WhenAny(_task, Task.Delay(timeoutMs));
                if (completedTask != _task)
                {
                    throw new TimeoutException($"Task 等待超时（{timeoutMs}ms）");
                }
                result = await _task;
            }

            // 线程安全地更新状态
            lock (this)
            {
                _isCompleted = true;
                _result = result;
                _exception = null;
            }

            return result;
        }
        catch (Exception ex)
        {
            // 线程安全地更新异常状态
            lock (this)
            {
                _isCompleted = true;
                _exception = ex;
                _result = null;
            }
            throw;
        }
    }

    /// <summary>
    /// 尝试异步等待 Task 完成并返回结果（非阻塞，带超时）
    /// </summary>
    /// <param name="timeoutMs">超时时间（毫秒）</param>
    /// <returns>任务结果，如果超时则返回 null</returns>
    public async Task<LangValueType?> TryAwaitAsync(int timeoutMs)
    {
        try
        {
            return await AwaitAsync(timeoutMs);
        }
        catch (TimeoutException)
        {
            return null;
        }
    }

    /// <summary>
    /// 非阻塞检查任务是否完成
    /// </summary>
    public bool IsCompleted => _isCompleted;

    /// <summary>
    /// 获取任务的状态
    /// </summary>
    public TaskStatus Status => _task.Status;

    /// <summary>
    /// 获取底层 Task 对象
    /// </summary>
    public override object GetValue() => _task;

    /// <summary>
    /// 类型字符串表示
    /// </summary>
    public override string TypeToString() => "Task";

    /// <summary>
    /// 值的字符串表示
    /// </summary>
    public override string ToString()
    {
        if (_isCompleted)
        {
            if (_exception != null)
                return $"Task(Failed: {_exception.Message})";
            return $"Task(Completed: {_result?.ToString() ?? "void"})";
        }
        return $"Task(Status: {Status})";
    }

    /// <summary>
    /// Run 方法：返回自身
    /// </summary>
    public override LangValueType Run(VariateManager manager) => this;

    /// <summary>
    /// 生成 IL 代码（编译器模式暂不支持）
    /// </summary>
    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        throw new Error.NotImplementedError(
            Position,
            "编译模式暂不支持 Task 类型"
        );
    }

    /// <summary>
    /// 获取 .NET 类型（编译器模式暂不支持）
    /// </summary>
    public override Type? OutputType(LocalManager local)
    {
        return typeof(Task<object>);
    }
}
