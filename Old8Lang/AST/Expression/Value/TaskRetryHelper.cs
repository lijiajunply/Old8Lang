using Old8Lang.Error;
using Old8Lang.Interpreter;
using Old8Lang.LangParser;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// Task 重试功能的辅助类，提供重新执行原始函数调用的重试机制
/// </summary>
public static class TaskRetryHelper
{
    /// <summary>
    /// 创建一个能够重新执行原始函数调用的重试任务
    /// </summary>
    /// <param name="funcCall">原始函数调用表达式</param>
    /// <param name="manager">变量管理器</param>
    /// <param name="retryCount">重试次数</param>
    /// <param name="delayMs">重试延迟（毫秒）</param>
    /// <param name="position">源代码位置</param>
    /// <returns>包含重试逻辑的 TaskLangValue</returns>
    public static TaskLangValue CreateRetryTask(
        Instance funcCall,
        VariateManager manager,
        int retryCount,
        int delayMs,
        SourcePosition position)
    {
        // 创建一个包装的 Task，在其中实现重试逻辑
        var retryTask = Task.Run(async () =>
        {
            Exception? lastException = null;

            for (int i = 0; i <= retryCount; i++)
            {
                try
                {
                    // 重新执行函数调用以获取新的 Task
                    var newTaskValue = funcCall.Run(manager);
                    if (newTaskValue is not TaskLangValue newTask)
                    {
                        throw new TypeError(funcCall, "Retry 只能用于返回 Task 的异步函数");
                    }

                    // 等待 Task 完成
                    return await newTask.AwaitAsync();
                }
                catch (Exception ex)
                {
                    lastException = ex;

                    if (i < retryCount)
                    {
                        // 重试前延迟
                        await Task.Delay(delayMs);
                    }
                }
            }

            // 重试次数耗尽，抛出最后一次异常
            throw lastException ?? new Exception("任务执行失败，重试次数耗尽");
        });

        return new TaskLangValue(retryTask, CancellationToken.None, position);
    }
}