using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Async;

/// <summary>
/// IL 模式下 <c>Task</c> 静态方法的实参发射。
/// </summary>
/// <remarks>
/// <c>Task</c> 静态方法由 <c>DotOperatorILHelper</c> 手工发射，实参的装箱、数量与消费
/// 很容易与形参不一致；一旦不一致，JIT 会报 <c>InvalidProgramException</c>
/// （栈上多一个值、少一个值，或把未装箱的 int 当作 object 传入）。
/// </remarks>
public class TaskStaticMethodIlTests
{
    [Fact]
    public void FromResult_ValueTypeArgument_IsBoxedForObjectParameter()
    {
        // Task.FromResult 统一实例化为 FromResult<object>(object)：
        // 值类型实参必须先装箱，否则 int 会被当作 object 引用传入。
        var code = @"
            async func run() {
                task <- Task.FromResult(42)
                result <- await task
                PrintLine(result)
            }

            run()
        ";

        Assert.Null(CompileAndInvoke(code));
    }

    [Fact]
    public void FromResult_ReferenceTypeArgument_CompilesAndExecutes()
    {
        var code = @"
            async func run() {
                task <- Task.FromResult(""text"")
                result <- await task
                PrintLine(result)
            }

            run()
        ";

        Assert.Null(CompileAndInvoke(code));
    }

    [Fact]
    public void WhenAll_ListArgument_IsEvaluatedOnce()
    {
        // 分派前统一压栈时，WhenAll 会把同一个列表压两遍，留下一个未消费的栈值。
        var code = @"
            async func run() {
                tasks <- { Task.FromResult(1), Task.FromResult(2) }
                results <- await Task.WhenAll(tasks)
                PrintLine(results.Count())
            }

            run()
        ";

        Assert.Null(CompileAndInvoke(code));
    }

    [Fact]
    public void WhenAny_ListArgument_IsEvaluatedOnce()
    {
        var code = @"
            async func run() {
                tasks <- { Task.FromResult(1), Task.FromResult(2) }
                completed <- await Task.WhenAny(tasks)
            }

            run()
        ";

        Assert.Null(CompileAndInvoke(code));
    }

    [Fact]
    public void Run_ArgumentIsStillConsumed()
    {
        // Task.Run 会在线程池上调用函数值并等待结果；实参数组不能留下栈值。
        var code = @"
            async func work(value:int) -> int {
                return value
            }

            async func run() {
                result <- await Task.Run(work, 42)
            }

            await run()
        ";

        Assert.Null(CompileAndInvoke(code));
    }

    [Fact]
    public void UnknownStaticMethod_ReportsManagedErrorInsteadOfInvalidIl()
    {
        // 未实现的 Task 静态方法不能「什么都不发射却声明有返回值」。
        var code = @"
            async func run() {
                task <- Task.Completed()
            }

            run()
        ";

        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        Old8Lang.Compiler.Compiler.IlVerificationEnabled = false;
        try
        {
            var error = Assert.ThrowsAny<Exception>(() =>
                Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter));

            Assert.DoesNotContain("InvalidProgram", error.GetType().Name);
            Assert.Contains("Completed", error.Message);
        }
        finally
        {
            Old8Lang.Compiler.Compiler.IlVerificationEnabled = true;
        }
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
