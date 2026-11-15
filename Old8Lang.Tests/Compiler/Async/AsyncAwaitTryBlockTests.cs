using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Async;

/// <summary>
/// IL 模式异步状态机在 try 区域里的 await。
/// </summary>
/// <remarks>
/// 这些用例针对一类 <c>InvalidProgramException</c>：状态派发在 <c>MoveNext</c> 开头用
/// <c>switch</c> 直接跳到 await 的恢复标签，而恢复标签位于 try 区域内部。
/// CLR 的 JIT 拒绝从 try 外部跳进 try 区域中间，因此整个方法被判为无效 IL。
/// 由 JIT 在方法首次执行时校验，所以只要异步函数被调用就能稳定复现，
/// 不需要等待任务真正完成。
/// </remarks>
public class AsyncAwaitTryBlockTests
{
    [Fact]
    public void AwaitInsideTryCatch_CompilesAndExecutesCorrectly()
    {
        var code = @"
            async func run() {
                try {
                    value <- await Task.FromResult(1)
                    PrintLine(value)
                } catch (e) {
                    PrintLine(e)
                }
                PrintLine(""done"")
            }

            run()
        ";

        var exception = CompileAndInvoke(code);

        Assert.Null(exception);
    }

    [Fact]
    public void AwaitInsideTryFinally_CompilesAndExecutesCorrectly()
    {
        var code = @"
            async func run() {
                try {
                    await Task.FromResult(1)
                } finally {
                    PrintLine(""fin"")
                }
                PrintLine(""done"")
            }

            run()
        ";

        var exception = CompileAndInvoke(code);

        Assert.Null(exception);
    }

    [Fact]
    public void AwaitInsideNestedTryBlocks_CompilesAndExecutesCorrectly()
    {
        var code = @"
            async func run() {
                try {
                    try {
                        await Task.FromResult(1)
                    } catch (e) {
                        PrintLine(e)
                    }
                } finally {
                    PrintLine(""outer fin"")
                }
                PrintLine(""done"")
            }

            run()
        ";

        var exception = CompileAndInvoke(code);

        Assert.Null(exception);
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
