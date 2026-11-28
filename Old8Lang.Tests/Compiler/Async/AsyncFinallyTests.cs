using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Async;

/// <summary>
/// IL 模式异步函数在 await 挂起时的 finally 语义。
/// </summary>
/// <remarks>
/// 挂起时用 <c>leave</c> 离开受保护区域，CLR 会沿路执行 finally；挂起不是函数退出，
/// 用户 finally 只能在函数真正完成/异常退出时执行一次。这里用静态计数器观察次数。
/// 完成时机由有界轮询等待，不依赖固定 Sleep：CI 机器核数少、用例并行时，
/// 线程池/同步上下文调度可能明显晚于本地。
/// </remarks>
public class AsyncFinallyTests
{
    [Fact]
    public void FinallyRunsOnceAfterRealSuspension()
    {
        var code = @"
            class Counter {
                public static value <- 0
                public static func bump() -> void { Counter.value <- Counter.value + 1 }
            }

            async func work() -> int {
                try {
                    await Task.Delay(20)
                    return 1
                } finally {
                    Counter.bump()
                }
            }

            work()
            waited <- 0
            while Counter.value < 1 && waited < 5000 {
                Thread.Sleep(10)
                waited <- waited + 10
            }
            Assert.Equal(1, Counter.value)
        ";

        Assert.Null(CompileAndInvoke(code));
    }

    [Fact]
    public void FinallyRunsOnceWhenExceptionPropagates()
    {
        var code = @"
            class Counter {
                public static value <- 0
                public static func bump() -> void { Counter.value <- Counter.value + 1 }
            }

            async func work() -> int {
                try {
                    await Task.Delay(10)
                    throw ""boom""
                } finally {
                    Counter.bump()
                }
            }

            async func runner() {
                try {
                    await work()
                } catch (e) {
                    Counter.bump()
                }
            }

            runner()
            waited <- 0
            while Counter.value < 2 && waited < 5000 {
                Thread.Sleep(10)
                waited <- waited + 10
            }
            Assert.Equal(2, Counter.value)
        ";

        Assert.Null(CompileAndInvoke(code));
    }

    private static Exception? CompileAndInvoke(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        // xUnit 会在每个测试线程上安装并发同步上下文；编译产物里的 await 会捕获它，
        // 续体被排进 xUnit 的队列，而测试线程此时正阻塞在轮询里，没人泵送该队列，
        // 断言就会先于 finally 执行（CI 少核机器必现，本地多核机器偶发）。
        // 真实宿主（控制台）没有同步上下文，这里清空以还原真实运行条件。
        var previousContext = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(null);

        Old8Lang.Compiler.Compiler.IlVerificationEnabled = false;
        try
        {
            var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
            return Record.Exception(() => compiledAction());
        }
        finally
        {
            Old8Lang.Compiler.Compiler.IlVerificationEnabled = true;
            SynchronizationContext.SetSynchronizationContext(previousContext);
        }
    }
}
