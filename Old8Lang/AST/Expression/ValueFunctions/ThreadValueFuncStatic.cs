using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;

namespace Old8Lang.AST.Expression.ValueFunctions;

/// <summary>
/// ThreadLangValue类型的扩展方法类，提供线程组合功能
/// </summary>
[Serializable]
public static class ThreadValueFuncStatic
{
    extension(ThreadLangValue thread)
    {
        /// <summary>
        /// 线程完成后执行下一个线程
        /// </summary>
        /// <param name="continuation">接收前一个线程结果并返回新线程的函数</param>
        /// <returns>新的 ThreadLangValue</returns>
        public ThreadLangValue Then(FuncLangValue continuation)
        {
            if (thread.ExternalManager is null)
            {
                throw new InvalidOperationError(thread.Position, "Then 方法需要有效的执行上下文（ExternalManager）");
            }

            var manager = thread.ExternalManager;

            return thread.Then(result =>
            {
                // 使用 ExternalManager（它有有效的 Interpreter）
                var closedFunc = continuation.Run(manager);
                if (closedFunc is FuncLangValue closedFuncValue)
                {
                    continuation = closedFuncValue;
                }

                // 现在调用带参数的 Run 方法
                var args = new List<LangExpression> { result };
                var nextThreadResult = continuation.Run(manager, args);

                if (nextThreadResult is ThreadLangValue threadValue)
                {
                    return threadValue;
                }

                throw new InvalidOperationError(thread.Position, "Then 的 continuation 函数必须返回一个 Thread");
            });
        }

        /// <summary>
        /// 为线程添加超时限制
        /// </summary>
        /// <param name="timeoutMs">超时时间（毫秒）</param>
        /// <returns>带超时限制的 ThreadLangValue</returns>
        public ThreadLangValue WithTimeout(IntLangValue timeoutMs)
        {
            return thread.WithTimeout(timeoutMs.Value);
        }

        /// <summary>
        /// 实现线程重试机制
        /// </summary>
        /// <param name="retryCount">最大重试次数</param>
        /// <param name="delayMs">重试之间的延迟（毫秒）</param>
        /// <returns>带重试机制的 ThreadLangValue</returns>
        public ThreadLangValue Retry(IntLangValue retryCount, IntLangValue? delayMs = null)
        {
            return thread.Retry(retryCount.Value, delayMs?.Value ?? 0);
        }

        /// <summary>
        /// 取消线程执行
        /// </summary>
        public void Cancel()
        {
            thread.Cancel();
        }
    }
}