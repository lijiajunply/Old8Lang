using Old8Lang.AST;
using Old8Lang.LangParser;
using System.Reflection.Emit;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Compiler;
using Old8Lang.Error;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// Task 值类型，表示异步操作
/// 包装 .NET Task&lt;LangValueType&gt;，支持异步执行和 await 等待
/// </summary>
public class TaskLangValue : LangValueType
{
    private readonly Task<LangValueType> _task;
    private readonly CancellationToken _cancellationToken;
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
    /// 获取取消令牌
    /// </summary>
    public CancellationToken CancellationToken => _cancellationToken;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="task">.NET Task对象</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <param name="position">源代码位置</param>
    public TaskLangValue(Task<LangValueType> task, CancellationToken cancellationToken = default, SourcePosition position = default)
        : base(position)
    {
        _task = task;
        _cancellationToken = cancellationToken;

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

        // 注册取消回调
        if (_cancellationToken.CanBeCanceled)
        {
            _cancellationToken.Register(() =>
            {
                // 任务无法直接取消，但可以在等待时检查取消状态
            });
        }
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
        throw new NotImplementedError(
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

    #region 任务组合方法

    /// <summary>
    /// 并行执行多个任务，等待所有任务完成
    /// </summary>
    public static TaskLangValue WhenAll(IEnumerable<TaskLangValue> tasks, SourcePosition position = default)
    {
        var dotnetTasks = tasks.Select(t => t._task).ToList();
        var whenAllTask = Task.WhenAll(dotnetTasks)
            .ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    throw t.Exception?.InnerException ?? t.Exception;
                }
                return new ListLangValue(t.Result.ToList(), position) as LangValueType;
            });
        return new TaskLangValue(whenAllTask, CancellationToken.None, position);
    }

    /// <summary>
    /// 等待第一个完成的任务
    /// </summary>
    public static TaskLangValue WhenAny(IEnumerable<TaskLangValue> tasks, SourcePosition position = default)
    {
        var dotnetTasks = tasks.Select(t => t._task).ToList();
        var whenAnyTask = Task.WhenAny(dotnetTasks)
            .ContinueWith(t => t.Result.Result);
        return new TaskLangValue(whenAnyTask, CancellationToken.None, position);
    }

    /// <summary>
    /// 创建延迟执行的任务
    /// </summary>
    public static TaskLangValue Delay(int delayMs, CancellationToken cancellationToken = default, SourcePosition position = default)
    {
        var delayTask = Task.Delay(delayMs, cancellationToken)
            .ContinueWith(t => (LangValueType)new VoidLangValue(position));
        return new TaskLangValue(delayTask, cancellationToken, position);
    }

    /// <summary>
    /// 任务完成后执行下一个任务
    /// </summary>
    public TaskLangValue Then(Func<LangValueType, TaskLangValue> continuation, SourcePosition position = default)
    {
        var thenTask = _task.ContinueWith(t =>
        {
            if (t.IsFaulted)
            {
                throw t.Exception?.InnerException ?? t.Exception;
            }
            var result = continuation(t.Result);
            return result.AwaitAsync().Result;
        }, _cancellationToken);
        return new TaskLangValue(thenTask, _cancellationToken, position);
    }

    /// <summary>
    /// 为任务添加超时限制
    /// </summary>
    public TaskLangValue WithTimeout(int timeoutMs, SourcePosition position = default)
    {
        var timeoutTask = Task.WhenAny(_task, Task.Delay(timeoutMs, _cancellationToken))
            .ContinueWith(t =>
            {
                if (t.Result != _task)
                {
                    throw new TimeoutException($"Task 等待超时（{timeoutMs}ms）");
                }
                return _task.Result;
            }, _cancellationToken);
        return new TaskLangValue(timeoutTask, _cancellationToken, position);
    }

    /// <summary>
    /// 实现任务重试机制
    /// </summary>
    public TaskLangValue Retry(int retryCount, int delayMs = 0, SourcePosition position = default)
    {
        var retryTask = Task.Run(async () =>
        {
            Exception? lastException = null;
            
            for (int i = 0; i <= retryCount; i++)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                
                try
                {
                    return await _task;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    
                    if (i < retryCount)
                    {
                        // 重试前延迟
                        await Task.Delay(delayMs, _cancellationToken);
                    }
                }
            }
            
            // 重试次数耗尽，抛出最后一次异常
            throw lastException ?? new Exception("任务执行失败，重试次数耗尽");
        }, _cancellationToken);
        
        return new TaskLangValue(retryTask, _cancellationToken, position);
    }

    #endregion
}
