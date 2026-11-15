using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Async;

/// <summary>
/// IL 模式异步函数在 await 挂起时的 finally 语义。
/// </summary>
/// <remarks>
/// 挂起时用 <c>leave</c> 离开受保护区域，CLR 会沿路执行 finally；挂起不是函数退出，
/// 用户 finally 只能在函数真正完成/异常退出时执行一次。这里用静态计数器观察次数。
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
            Thread.Sleep(400)
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
            Thread.Sleep(400)
            Assert.Equal(2, Counter.value)
        ";

        Assert.Null(CompileAndInvoke(code));
    }

    private static Exception? CompileAndInvoke(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        Old8Lang.Compiler.Compiler.IlVerificationEnabled = false;
        try
        {
            var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
            return Record.Exception(() => compiledAction());
        }
        finally
        {
            Old8Lang.Compiler.Compiler.IlVerificationEnabled = true;
        }
    }
}
