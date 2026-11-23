using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Async;

/// <summary>
/// IL 模式异步状态机的 await 位置覆盖：
/// - <c>catch</c> / <c>finally</c> 块内通过处理器体搬移支持 <c>await</c>；
/// - 仍不支持的位置（<c>defer</c>、求值栈非空的表达式内部）必须给出明确编译错误，而不是无效 IL。
/// </summary>
public class AsyncUnsupportedAwaitTests
{
    [Fact]
    public void AwaitInsideCatch_ExecutesAndResumesCorrectly()
    {
        var code = @"
            log <- """"

            async func catchAwaitWork() -> string {
                try {
                    await Task.Delay(10)
                    throw ""boom""
                } catch (e) {
                    await Task.Delay(15)
                    log <- ""caught:"" + e.ToStr()
                    return ""recovered""
                }
            }

            result <- await catchAwaitWork()
            Assert.Equal(""recovered"", result)
            Assert.Equal(""caught:boom"", log)
        ";

        Assert.Null(CompileAndRun(code));
    }

    [Fact]
    public void AwaitInsideFinally_ExecutesAndResumesCorrectly()
    {
        var code = @"
            cleaned <- false

            async func finallyAwaitWork() -> string {
                try {
                    await Task.Delay(10)
                    return ""ok""
                } finally {
                    await Task.Delay(15)
                    cleaned <- true
                }
            }

            result <- await finallyAwaitWork()
            Assert.Equal(""ok"", result)
            Assert.True(cleaned)
        ";

        Assert.Null(CompileAndRun(code));
    }

    [Fact]
    public void AwaitInsideCatchAndFinally_CombinedControlFlow()
    {
        var code = @"
            steps <- {}

            async func combinedCatchFinallyWork() -> string {
                try {
                    steps.Add(""try"")
                    await Task.Delay(10)
                    throw ""err""
                } catch (e) {
                    steps.Add(""catch_before"")
                    await Task.Delay(10)
                    steps.Add(""catch_after:"" + e.ToStr())
                    return ""from_catch""
                } finally {
                    steps.Add(""finally_before"")
                    await Task.Delay(10)
                    steps.Add(""finally_after"")
                }
            }

            result <- await combinedCatchFinallyWork()
            Assert.Equal(""from_catch"", result)
            Assert.Equal({""try"", ""catch_before"", ""catch_after:err"", ""finally_before"", ""finally_after""}, steps)
        ";

        Assert.Null(CompileAndRun(code));
    }

    [Fact]
    public void AwaitInsideFinally_PreservesRethrownException()
    {
        var code = @"
            finallyRan <- false
            caught <- """"

            async func inner() -> void {
                try {
                    await Task.Delay(10)
                    throw ""unhandled""
                } finally {
                    await Task.Delay(10)
                    finallyRan <- true
                }
            }

            async func outer() -> void {
                try {
                    await inner()
                } catch (e) {
                    caught <- e.ToStr()
                }
            }

            await outer()
            Assert.True(finallyRan)
            Assert.Equal(""unhandled"", caught)
        ";

        Assert.Null(CompileAndRun(code));
    }

    [Fact]
    public void AwaitInsideFinally_NestedReturnPreservesValueAndOrder()
    {
        var code = @"
            order <- {}

            async func nestedFinallyReturnWork() -> string {
                try {
                    try {
                        await Task.Delay(10)
                        return ""nested_val""
                    } finally {
                        await Task.Delay(10)
                        order.Add(""inner"")
                    }
                } finally {
                    await Task.Delay(10)
                    order.Add(""outer"")
                }
            }

            result <- await nestedFinallyReturnWork()
            Assert.Equal(""nested_val"", result)
            Assert.Equal({""inner"", ""outer""}, order)
        ";

        Assert.Null(CompileAndRun(code));
    }

    [Fact]
    public void AwaitInsideFinally_BreakInLoopStillRunsFinally()
    {
        var code = @"
            count <- 0
            finallyCount <- 0

            async func breakFinallyLoopWork() -> void {
                i <- 0
                while i < 5 {
                    try {
                        count <- count + 1
                        if i == 2 {
                            break
                        }
                    } finally {
                        await Task.Delay(5)
                        finallyCount <- finallyCount + 1
                    }
                    i <- i + 1
                }
            }

            await breakFinallyLoopWork()
            Assert.Equal(3, count)
            Assert.Equal(3, finallyCount)
        ";

        Assert.Null(CompileAndRun(code));
    }

    [Fact]
    public void AwaitInsideDefer_ReportsClearError()
    {
        var code = @"
            async func work() {
                defer { await Task.FromResult(1) }
                PrintLine(""body"")
            }

            work()
        ";

        var message = CompileExpectingError(code);
        Assert.Contains("defer", message);
    }

    [Fact]
    public void AwaitInsideExpression_ReportsClearErrorInsteadOfInvalidIl()
    {
        // 函数实参求值前已经构造了实参数组，栈上非空；挂起会生成无效 IL。
        var code = @"
            async func work() {
                PrintLine(await Task.FromResult(1))
            }

            work()
        ";

        var message = CompileExpectingError(code);
        Assert.Contains("表达式内部的 await", message);
    }

    private static Exception? CompileAndRun(string code)
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

    private static string CompileExpectingError(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        Old8Lang.Compiler.Compiler.IlVerificationEnabled = false;
        try
        {
            var error = Assert.ThrowsAny<Exception>(() =>
                Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter));
            Assert.DoesNotContain("InvalidProgram", error.GetType().Name);
            return error.Message;
        }
        finally
        {
            Old8Lang.Compiler.Compiler.IlVerificationEnabled = true;
        }
    }
}
