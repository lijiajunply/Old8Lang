using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Async;

/// <summary>
/// IL 模式不支持恢复的 await 位置必须给出明确编译错误，而不是无效 IL 或错误控制流。
/// </summary>
/// <remarks>
/// 这些位置要么由异常机制进入（catch / finally），要么在 IL 后端根本不生成
/// （defer / yield），要么求值栈上已有待用值（表达式内部的 await）。挂起用
/// <c>leave</c> 离开受保护区域并要求求值栈为空，因此这些位置无法安全恢复。
/// </remarks>
public class AsyncUnsupportedAwaitTests
{
    [Fact]
    public void AwaitInsideCatch_ReportsClearError()
    {
        var code = @"
            async func work() {
                try {
                    throw ""boom""
                } catch (e) {
                    await Task.FromResult(1)
                    PrintLine(e)
                }
            }

            work()
        ";

        var message = CompileExpectingError(code);
        Assert.Contains("catch / finally", message);
    }

    [Fact]
    public void AwaitInsideFinally_ReportsClearError()
    {
        var code = @"
            async func work() {
                try {
                    PrintLine(""body"")
                } finally {
                    await Task.FromResult(1)
                }
            }

            work()
        ";

        var message = CompileExpectingError(code);
        Assert.Contains("catch / finally", message);
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
