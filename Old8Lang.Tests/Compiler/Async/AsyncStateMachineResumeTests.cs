using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Async;

/// <summary>
/// IL 模式异步状态机从真实挂起中恢复。
/// </summary>
/// <remarks>
/// 状态派发必须"跳到正确的 await"，而不是从头重新执行前面的 await：
/// 否则前面的 await 会被再次求值，计数或副作用就会多出来。
/// 这些用例用静态计数器观察执行次数，顶层用 <c>Thread.Sleep</c> 等续体跑完，
/// 断言写在顶层同步代码里，避免异步函数内的断言被吞掉。
/// </remarks>
public class AsyncStateMachineResumeTests
{
    [Fact]
    public void TwoSequentialAwaits_DoNotReevaluateEarlierAwait()
    {
        var code = @"
            class Counter {
                public static value <- 0
                public static func bump() -> void { Counter.value <- Counter.value + 1 }
            }

            async func work() {
                Counter.bump()
                await Task.Delay(10)
                Counter.bump()
                await Task.Delay(10)
            }

            await work()
            Assert.Equal(2, Counter.value)
        ";

        Assert.Null(CompileAndInvoke(code));
    }

    [Fact]
    public void AwaitInsideTryLoop_ResumesEachIteration()
    {
        var code = @"
            class Counter {
                public static value <- 0
            }

            async func work() {
                i <- 0
                while i < 3 {
                    try {
                        await Task.Delay(5)
                        Counter.value <- Counter.value + i
                    } catch (e) {
                        PrintLine(e)
                    }
                    i <- i + 1
                }
            }

            await work()
            Assert.Equal(3, Counter.value)
        ";

        Assert.Null(CompileAndInvoke(code));
    }

    [Fact]
    public void AwaitInsideNestedTry_ResumesAtInnerAwait()
    {
        var code = @"
            class Counter {
                public static value <- 0
            }

            async func work() {
                try {
                    try {
                        await Task.Delay(10)
                        Counter.value <- Counter.value + 1
                    } catch (e) {
                        PrintLine(e)
                    }
                    Counter.value <- Counter.value + 10
                } finally {
                    Counter.value <- Counter.value + 100
                }
            }

            await work()
            Assert.Equal(111, Counter.value)
        ";

        Assert.Null(CompileAndInvoke(code));
    }

    [Fact]
    public void SiblingTriesEachWithAwait_ResumeInCorrectRegion()
    {
        var code = @"
            class Counter {
                public static value <- 0
            }

            async func work() {
                try {
                    await Task.Delay(5)
                    Counter.value <- Counter.value + 1
                } catch (e) {
                    PrintLine(e)
                }
                try {
                    await Task.Delay(5)
                    Counter.value <- Counter.value + 10
                } catch (e) {
                    PrintLine(e)
                }
            }

            await work()
            Assert.Equal(11, Counter.value)
        ";

        Assert.Null(CompileAndInvoke(code));
    }

    [Fact]
    public void OuterAndInnerTryBothWithAwait_ResumeCorrectly()
    {
        var code = @"
            class Counter {
                public static value <- 0
            }

            async func work() {
                try {
                    await Task.Delay(5)
                    Counter.value <- Counter.value + 1
                    try {
                        await Task.Delay(5)
                        Counter.value <- Counter.value + 10
                    } catch (e) {
                        PrintLine(e)
                    }
                    Counter.value <- Counter.value + 100
                } finally {
                    Counter.value <- Counter.value + 1000
                }
            }

            await work()
            Assert.Equal(1111, Counter.value)
        ";

        Assert.Null(CompileAndInvoke(code));
    }

    [Fact]
    public void UsingBlockWithAwait_ResumesInsideRegion()
    {
        var code = @"
            class Counter {
                public static value <- 0
            }

            async func work() {
                using mutex <- MutexCreate() {
                    await Task.Delay(10)
                    Counter.value <- Counter.value + 1
                }
                Counter.value <- Counter.value + 10
            }

            await work()
            Assert.Equal(11, Counter.value)
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
