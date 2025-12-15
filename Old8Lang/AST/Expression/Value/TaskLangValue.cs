using Old8Lang.AST;
using Old8Lang.LangParser;
using System.Reflection.Emit;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Compiler;
using Old8Lang.Error;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// Task 状态枚举，表示异步操作的完整生命周期
/// </summary>
public enum TaskStatus
{
    /// <summary>任务已创建但尚未开始</summary>
    Pending,
    /// <summary>任务正在执行</summary>
    Running,
    /// <summary>任务已成功完成</summary>
    Completed,
    /// <summary>任务执行过程中发生异常</summary>
    Failed,
    /// <summary>任务被取消</summary>
    Canceled
}

/// <summary>
/// Task 值类型，表示异步操作
/// 包装 .NET Task&lt;LangValueType&gt;，支持异步执行和 await 等待
/// </summary>
public class TaskLangValue : LangValueType
{
    private readonly Task<LangValueType> _task;
    private readonly CancellationToken _cancellationToken;
    private TaskStatus _status = TaskStatus.Pending;
    private LangValueType? _result = null;
    private Exception? _exception = null;
    private readonly object _lock = new();

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

        // 注册任务状态变化的回调
        _task.ContinueWith(t =>
        {
            lock (_lock)
            {
                if (t.IsCanceled)
                {
                    _status = TaskStatus.Canceled;
                    _exception = new OperationCanceledException("任务被取消");
                }
                else if (t.IsFaulted)
                {
                    _status = TaskStatus.Failed;
                    _exception = t.Exception?.InnerException ?? t.Exception;
                }
                else if (t.IsCompletedSuccessfully)
                {
                    _status = TaskStatus.Completed;
                    _result = t.Result;
                }
            }
        });

        // 如果任务已经开始执行，更新状态为Running
        if (_task.Status == System.Threading.Tasks.TaskStatus.Running)
        {
            lock (_lock)
            {
                _status = TaskStatus.Running;
            }
        }

        // 注册取消回调
        if (_cancellationToken.CanBeCanceled)
        {
            _cancellationToken.Register(() =>
            {
                lock (_lock)
                {
                    if (_status == TaskStatus.Pending || _status == TaskStatus.Running)
                    {
                        _status = TaskStatus.Canceled;
                        _exception = new OperationCanceledException("任务被取消");
                    }
                }
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
            lock (_lock)
            {
                _status = TaskStatus.Completed;
                _result = result;
                _exception = null;
            }
            
            return result;
        }
        catch (AggregateException aggEx)
        {
            // 展开 AggregateException，抛出内部异常
            var innerException = aggEx.InnerException ?? aggEx;
            lock (_lock)
            {
                _status = innerException is OperationCanceledException ? TaskStatus.Canceled : TaskStatus.Failed;
                _exception = innerException;
                _result = null;
            }
            throw innerException;
        }
        catch (Exception ex)
        {
            lock (_lock)
            {
                _status = ex is OperationCanceledException ? TaskStatus.Canceled : TaskStatus.Failed;
                _exception = ex;
                _result = null;
            }
            throw;
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
    /// <exception cref="OperationCanceledException">任务被取消</exception>
    public async Task<LangValueType> AwaitAsync(int timeoutMs)
    {
        try
        {
            // 检查取消请求
            _cancellationToken.ThrowIfCancellationRequested();

            // 确保任务状态更新为 Running
            lock (_lock)
            {
                if (_status == TaskStatus.Pending)
                {
                    _status = TaskStatus.Running;
                }
            }

            LangValueType result;
            if (timeoutMs <= 0)
            {
                // 无超时，异步等待任务完成
                result = await _task;
            }
            else
            {
                // 带超时，使用 Task.WhenAny 实现超时机制
                var timeoutTask = Task.Delay(timeoutMs, _cancellationToken);
                var completedTask = await Task.WhenAny(_task, timeoutTask);
                
                // 检查取消请求
                _cancellationToken.ThrowIfCancellationRequested();
                
                if (completedTask == timeoutTask)
                {
                    // 超时
                    throw new TimeoutException($"Task 等待超时（{timeoutMs}ms）");
                }
                
                // 任务已完成，获取结果
                result = await _task;
            }

            // 线程安全地更新完成状态
            lock (_lock)
            {
                _status = TaskStatus.Completed;
                _result = result;
                _exception = null;
            }

            return result;
        }
        catch (OperationCanceledException ex)
        {
            // 任务被取消
            lock (_lock)
            {
                _status = TaskStatus.Canceled;
                _exception = ex;
                _result = null;
            }
            throw;
        }
        catch (Exception ex)
        {
            // 其他异常
            lock (_lock)
            {
                _status = TaskStatus.Failed;
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
            // 超时情况下不更新任务状态，返回 null
            return null;
        }
    }

    /// <summary>
    /// 非阻塞检查任务是否完成
    /// </summary>
    public bool IsCompleted
    {
        get
        {
            lock (_lock)
            {
                return _status == TaskStatus.Completed || _status == TaskStatus.Failed || _status == TaskStatus.Canceled;
            }
        }
    }

    /// <summary>
    /// 获取任务的状态
    /// </summary>
    public TaskStatus Status
    {
        get
        {
            lock (_lock)
            {
                return _status;
            }
        }
    }

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
        lock (_lock)
        {
            return _status switch
            {
                TaskStatus.Pending => "Task(Status: Pending)",
                TaskStatus.Running => "Task(Status: Running)",
                TaskStatus.Completed => $"Task(Completed: {_result?.ToString() ?? "void"})",
                TaskStatus.Failed => $"Task(Failed: {_exception?.ToString() ?? "Unknown error"})",
                TaskStatus.Canceled => "Task(Canceled)",
                _ => "Task(Status: Unknown)"
            };
        }
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
