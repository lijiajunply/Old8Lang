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
    private readonly Task<LangValueType> Task;
    private readonly CancellationToken _cancellationToken;
    private TaskStatus _status = TaskStatus.Pending;
    private LangValueType? _result;
    private Exception? _exception;
    private readonly Lock Lock = new();

    /// <summary>
    /// 外部变量管理器，用于访问外部作用域和Interpreter
    /// 类似于AnyLangValue的ExternalManager
    /// </summary>
    public VariateManager? ExternalManager { get; set; }

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
    public TaskLangValue(Task<LangValueType> task, CancellationToken cancellationToken = default,
        SourcePosition position = default)
        : base(position)
    {
        Task = task;
        _cancellationToken = cancellationToken;

        // 注册任务状态变化的回调
        Task.ContinueWith(t =>
        {
            lock (Lock)
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
        if (Task.Status == System.Threading.Tasks.TaskStatus.Running)
        {
            lock (Lock)
            {
                _status = TaskStatus.Running;
            }
        }

        // 注册取消回调
        if (_cancellationToken.CanBeCanceled)
        {
            _cancellationToken.Register(() =>
            {
                lock (Lock)
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
            var result = Task.Result;

            // 更新缓存状态
            lock (Lock)
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
            lock (Lock)
            {
                _status = innerException is OperationCanceledException ? TaskStatus.Canceled : TaskStatus.Failed;
                _exception = innerException;
                _result = null;
            }

            throw innerException;
        }
        catch (Exception ex)
        {
            lock (Lock)
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
            lock (Lock)
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
                result = await Task;
            }
            else
            {
                // 带超时，使用 Task.WhenAny 实现超时机制
                var timeoutTask = System.Threading.Tasks.Task.Delay(timeoutMs, _cancellationToken);
                var completedTask = await System.Threading.Tasks.Task.WhenAny(Task, timeoutTask);

                // 检查取消请求
                _cancellationToken.ThrowIfCancellationRequested();

                if (completedTask == timeoutTask)
                {
                    // 超时
                    throw new TimeoutException($"Task 等待超时（{timeoutMs}ms）");
                }

                // 任务已完成，获取结果
                result = await Task;
            }

            // 线程安全地更新完成状态
            lock (Lock)
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
            lock (Lock)
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
            lock (Lock)
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
            lock (Lock)
            {
                return _status == TaskStatus.Completed || _status == TaskStatus.Failed ||
                       _status == TaskStatus.Canceled;
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
            lock (Lock)
            {
                return _status;
            }
        }
    }

    /// <summary>
    /// 获取底层 Task 对象
    /// </summary>
    public override object GetValue() => Task;

    /// <summary>
    /// 类型字符串表示
    /// </summary>
    public override string TypeToString() => "Task";

    /// <summary>
    /// 值的字符串表示
    /// </summary>
    public override string ToString()
    {
        lock (Lock)
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
    /// Dot 方法：支持属性访问和方法调用
    /// </summary>
    /// <remarks>
    /// Then 方法通过扩展方法实现（在 FuncStatic.cs 中），通过 ExternalManager 访问 Interpreter
    /// Retry 方法在 Operation.cs 中特殊处理，因为需要重新执行原始函数调用
    /// </remarks>
    public override LangValueType Dot(LangExpression dotExpression)
    {
        // 处理属性访问（ClassMemberId 继承自 LangId）
        if (dotExpression is LangId id)
        {
            var propertyName = id.IdName;

            return propertyName switch
            {
                "IsCompleted" => new BoolLangValue(IsCompleted, Position),
                "Status" => new StringLangValue(Status.ToString(), Position),
                _ => throw new AttributeError(dotExpression.Position, propertyName, "Task")
            };
        }

        // 处理方法调用（Instance）
        if (dotExpression is Instance instance)
        {
            // 调用扩展方法（在 Instance.FromClassToResult 中处理）
            return instance.FromClassToResult(this);
        }

        // 其他情况使用基类实现
        return base.Dot(dotExpression);
    }

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
    public override Type OutputType(LocalManager local)
    {
        return typeof(Task<object>);
    }

    #region 任务组合方法

    /// <summary>
    /// 并行执行多个任务，等待所有任务完成
    /// </summary>
    public static TaskLangValue WhenAll(IEnumerable<TaskLangValue> tasks, SourcePosition position = default)
    {
        var dotnetTasks = tasks.Select(t => t.Task).ToList();
        var whenAllTask = System.Threading.Tasks.Task.WhenAll(dotnetTasks)
            .ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    throw (t.Exception?.InnerException ?? t.Exception)!;
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
        var dotnetTasks = tasks.Select(t => t.Task).ToList();
        var whenAnyTask = System.Threading.Tasks.Task.WhenAny(dotnetTasks)
            .ContinueWith(t => t.Result.Result);
        return new TaskLangValue(whenAnyTask, CancellationToken.None, position);
    }

    /// <summary>
    /// 创建延迟执行的任务
    /// </summary>
    public static TaskLangValue Delay(int delayMs, CancellationToken cancellationToken = default,
        SourcePosition position = default)
    {
        var delayTask = System.Threading.Tasks.Task.Delay(delayMs, cancellationToken)
            .ContinueWith(LangValueType (_) => new VoidLangValue(position), cancellationToken);
        return new TaskLangValue(delayTask, cancellationToken, position);
    }

    /// <summary>
    /// 任务完成后执行下一个任务
    /// </summary>
    public TaskLangValue ThenTask(Func<LangValueType, TaskLangValue> continuation, SourcePosition position = default)
    {
        var thenTask = Task.ContinueWith(async t =>
        {
            if (t.IsFaulted)
            {
                throw (t.Exception?.InnerException ?? t.Exception)!;
            }

            var result = continuation(t.Result);
            return await result.AwaitAsync();
        }, _cancellationToken).Unwrap();
        return new TaskLangValue(thenTask, _cancellationToken, position);
    }

    /// <summary>
    /// 为任务添加超时限制
    /// </summary>
    public TaskLangValue WithTimeout(int timeoutMs)
    {
        var timeoutTask = System.Threading.Tasks.Task
            .WhenAny(Task, System.Threading.Tasks.Task.Delay(timeoutMs, _cancellationToken))
            .ContinueWith(t =>
            {
                if (t.Result != Task)
                {
                    throw new TimeoutException($"Task 等待超时（{timeoutMs}ms）");
                }

                return Task.Result;
            }, _cancellationToken);
        return new TaskLangValue(timeoutTask, _cancellationToken);
    }

    /// <summary>
    /// 实现任务重试机制
    /// </summary>
    public TaskLangValue RetryTask(int retryCount, int delayMs = 0, SourcePosition position = default)
    {
        var retryTask = System.Threading.Tasks.Task.Run(async () =>
        {
            Exception? lastException = null;

            for (int i = 0; i <= retryCount; i++)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    return await Task;
                }
                catch (Exception ex)
                {
                    lastException = ex;

                    if (i < retryCount)
                    {
                        // 重试前延迟
                        await System.Threading.Tasks.Task.Delay(delayMs, _cancellationToken);
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