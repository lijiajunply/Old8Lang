using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;

namespace Old8Lang.AST.Expression.ValueFunctions;

/// <summary>
/// TaskLangValue类型的扩展方法类，提供异步任务组合功能
/// </summary>
[Serializable]
public static class TaskValueFuncStatic
{
    extension(TaskLangValue task)
    {
        /// <summary>
        /// 任务完成后执行下一个任务
        /// </summary>
        /// <param name="continuation">接收前一个任务结果并返回新任务的函数</param>
        /// <returns>新的 TaskLangValue</returns>
        public TaskLangValue Then(FuncLangValue continuation)
        {
            if (task.ExternalManager is null)
            {
                throw new InvalidOperationError(task.Position, "Then 方法需要有效的执行上下文（ExternalManager）");
            }

            var manager = task.ExternalManager;

            return task.ThenTask(result =>
            {
                // 使用 ExternalManager（它有有效的 Interpreter）
                var closedFunc = continuation.Run(manager);
                if (closedFunc is FuncLangValue closedFuncValue)
                {
                    continuation = closedFuncValue;
                }

                // 现在调用带参数的 Run 方法
                var args = new List<LangExpression> { result };
                var nextTaskResult = continuation.Run(manager, args);

                if (nextTaskResult is TaskLangValue taskValue)
                {
                    return taskValue;
                }

                // 如果返回的不是 Task，则将结果包装成一个立即完成的 Task
                return new TaskLangValue(Task.FromResult(nextTaskResult), CancellationToken.None, task.Position)
                {
                    ExternalManager = manager
                };
            }, task.Position);
        }

        /// <summary>
        /// 实现任务重试机制
        /// 注意：Retry 需要原始函数调用表达式，因此在 Operation.cs 中特殊处理
        /// 这里只是占位符，实际实现在 Operation.cs 中
        /// </summary>
        /// <param name="retryCount">最大重试次数</param>
        /// <param name="delayMs">重试之间的延迟（毫秒）</param>
        /// <returns>带重试机制的 TaskLangValue</returns>
        public TaskLangValue Retry(IntLangValue retryCount, IntLangValue? delayMs = null)
        {
            if (task.ExternalManager is null)
            {
                throw new InvalidOperationError(task.Position, "Then 方法需要有效的执行上下文（ExternalManager）");
            }

            return task.RetryTask(retryCount.Value, delayMs?.Value ?? 0);
        }
        
        /// <summary>
        /// 任务完成后执行下一个任务
        /// </summary>
        /// <param name="continuation">接收前一个任务结果并返回新任务的函数</param>
        /// <returns>新的 TaskLangValue</returns>
        public TaskLangValue ContinueWith(FuncLangValue continuation)
        {
            if (task.ExternalManager is null)
            {
                throw new InvalidOperationError(task.Position, "ContinueWith 方法需要有效的执行上下文（ExternalManager）");
            }

            var manager = task.ExternalManager;

            var continueTask = task.Task.ContinueWith(async t =>
            {
                if (t.IsFaulted)
                {
                    throw (t.Exception?.InnerException ?? t.Exception)!;
                }

                // 调用延续函数
                var args = new List<LangExpression> { t.Result };
                var result = continuation.Run(manager, args);

                if (result is TaskLangValue taskValue)
                {
                    return await taskValue.AwaitAsync();
                }

                // 如果返回的不是 Task，直接返回结果
                return result;
            },task.CancellationToken).Unwrap();

            return new TaskLangValue(continueTask, task.CancellationToken, task.Position)
            {
                ExternalManager = manager
            };
        }
    }
}