using Old8Lang.Error;
using Old8Lang.Interpreter;
using Old8Lang.LangParser;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// Thread 重试功能的辅助类，提供重新执行原始函数调用的重试机制
/// </summary>
public static class ThreadRetryHelper
{
    /// <summary>
    /// 创建一个能够重新执行原始函数调用的重试线程
    /// </summary>
    /// <param name="funcCall">原始函数调用表达式</param>
    /// <param name="manager">变量管理器</param>
    /// <param name="retryCount">重试次数</param>
    /// <param name="delayMs">重试之间的延迟（毫秒）</param>
    /// <param name="position">源代码位置</param>
    /// <returns>包含重试逻辑的 ThreadLangValue</returns>
    public static ThreadLangValue CreateRetryThread(
        Instance funcCall,
        VariateManager manager,
        int retryCount,
        int delayMs,
        SourcePosition position)
    {
        var tcs = new CancellationTokenSource();
        ThreadLangValue? retryThread = null;

        retryThread = new ThreadLangValue(() =>
        {
            Exception? lastException = null;

            for (int i = 0; i <= retryCount; i++)
            {
                try
                {
                    // 重新执行函数调用以获取新的 Thread
                    var newThreadValue = funcCall.Run(manager);
                    if (newThreadValue is not ThreadLangValue newThread)
                    {
                        throw new TypeError(funcCall, "Retry 只能用于返回 Thread 的函数");
                    }

                    // 等待 Thread 完成
                    var result = newThread.Join();

                    // 执行成功，设置结果并返回
                    retryThread?.SetResult(result.GetValue());
                    return;
                }
                catch (Exception ex)
                {
                    lastException = ex;

                    if (i < retryCount)
                    {
                        // 重试前延迟
                        Thread.Sleep(delayMs);
                    }
                }
            }

            // 重试次数耗尽，抛出最后一次异常
            throw lastException ?? new Exception("线程执行失败，重试次数耗尽");
        }, position, tcs.Token)
        {
            // 设置外部管理器
            ExternalManager = manager
        };

        return retryThread;
    }
}