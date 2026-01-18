using Old8Lang.Bytecode;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.VirtualMachine.Statements;

/// <summary>
/// 虚拟机 defer 语句测试
/// 测试 defer 语句的资源清理机制
/// </summary>
[Collection("Sequential")]
public class VMDeferTests
{
    /// <summary>
    /// 执行虚拟机代码并捕获控制台输出
    /// </summary>
    private string ExecuteVMCode(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        // 编译为字节码
        var compiler = new BytecodeCompiler();
        var bytecodeFile = compiler.Compile(ast);

        // 捕获控制台输出
        var originalOut = Console.Out;
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            // 执行字节码
            var vm = new Bytecode.VirtualMachine(bytecodeFile);
            vm.Execute();

            return stringWriter.ToString().Trim();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void SimpleDeferStatement_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func test() -> void {
                PrintLine(""start"")
                defer PrintLine(""cleanup"")
                PrintLine(""end"")
            }
            test()
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        Assert.Equal("start", lines[0]);
        Assert.Equal("end", lines[1]);
        Assert.Equal("cleanup", lines[2]); // defer 在函数退出时执行
    }

    [Fact]
    public void MultipleDeferStatements_ExecuteInReverseOrder()
    {
        // Arrange
        var code = @"
            func test() -> void {
                defer PrintLine(""first"")
                defer PrintLine(""second"")
                defer PrintLine(""third"")
                PrintLine(""main"")
            }
            test()
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(4, lines.Length);
        Assert.Equal("main", lines[0]);
        Assert.Equal("third", lines[1]);  // LIFO 顺序
        Assert.Equal("second", lines[2]);
        Assert.Equal("first", lines[3]);
    }

    [Fact]
    public void DeferWithException_StillExecutes()
    {
        // Arrange
        var code = @"
            func test() -> void {
                defer PrintLine(""cleanup"")
                PrintLine(""before error"")
                throw ""error""
                PrintLine(""after error"")
            }

            try {
                test()
            } catch (e) {
                PrintLine(""caught"")
            }
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Contains("before error", lines);
        Assert.Contains("cleanup", lines); // defer 即使在异常情况下也会执行
        Assert.Contains("caught", lines);
        Assert.DoesNotContain("after error", lines);
    }

    [Fact]
    public void DeferAccessLocalVariable_CapturesCorrectValue()
    {
        // Arrange
        var code = @"
            func test() -> void {
                x <- 10
                defer PrintLine(""x = "" + x.ToStr())
                x <- 20
                PrintLine(""x changed to: "" + x.ToStr())
            }
            test()
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("x changed to: 20", lines[0]);
        Assert.Equal("x = 20", lines[1]); // defer 捕获的是最终值
    }

    [Fact]
    public void DeferWithReturn_ExecutesBeforeReturn()
    {
        // Arrange
        var code = @"
            func test() -> int {
                defer PrintLine(""cleanup"")
                PrintLine(""before return"")
                return 42
            }
            result <- test()
            PrintLine(""result: "" + result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        Assert.Equal("before return", lines[0]);
        Assert.Equal("cleanup", lines[1]); // defer 在 return 之前执行
        Assert.Equal("result: 42", lines[2]);
    }

    [Fact]
    public void DeferWithMultipleReturns_ExecutesOnce()
    {
        // Arrange
        var code = @"
            func test(x:int) -> string {
                defer PrintLine(""cleanup"")
                if x > 0 {
                    return ""positive""
                }
                return ""non-positive""
            }
            result1 <- test(5)
            result2 <- test(-3)
            PrintLine(""done"")
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        Assert.Equal("cleanup", lines[0]); // 第一次调用的 defer
        Assert.Equal("cleanup", lines[1]); // 第二次调用的 defer
        Assert.Equal("done", lines[2]);
    }

    [Fact]
    public void DeferBlock_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func test() -> void {
                x <- 1
                defer {
                    PrintLine(""cleanup start"")
                    PrintLine(""x = "" + x.ToStr())
                    PrintLine(""cleanup end"")
                }
                x <- 2
                PrintLine(""main"")
            }
            test()
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(4, lines.Length);
        Assert.Equal("main", lines[0]);
        Assert.Equal("cleanup start", lines[1]);
        Assert.Equal("x = 2", lines[2]);
        Assert.Equal("cleanup end", lines[3]);
    }

    [Fact]
    public void DeferInLoop_ExecutesMultipleTimes()
    {
        // Arrange
        var code = @"
            func test(n:int) -> void {
                defer PrintLine(n.ToStr())
                PrintLine(n.ToStr())
            }

            for i in [1~3] {
                test(i)
            }
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(6, lines.Length);
        Assert.Equal("1", lines[0]);
        Assert.Equal("1", lines[1]);
        Assert.Equal("2", lines[2]);
        Assert.Equal("2", lines[3]);
        Assert.Equal("3", lines[4]);
        Assert.Equal("3", lines[5]);
    }

    [Fact]
    public void DeferWithExceptionInDefer_PropagatesException()
    {
        // Arrange
        var code = @"
            func test() -> void {
                defer throw ""defer error""
                PrintLine(""main"")
            }

            try {
                test()
            } catch (e) {
                PrintLine(""caught: "" + e.ToStr())
            }
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("main", lines[0]);
        Assert.Equal("caught: defer error", lines[1]);
    }

    [Fact]
    public void DeferWithNestedFunctions_ExecutesInCorrectScope()
    {
        // Arrange
        var code = @"
            func outer() -> void {
                defer PrintLine(""outer cleanup"")
                PrintLine(""outer start"")

                func inner() -> void {
                    defer PrintLine(""inner cleanup"")
                    PrintLine(""inner start"")
                }

                inner()
                PrintLine(""outer end"")
            }
            outer()
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(5, lines.Length);
        Assert.Equal("outer start", lines[0]);
        Assert.Equal("inner start", lines[1]);
        Assert.Equal("inner cleanup", lines[2]); // inner defer 先执行
        Assert.Equal("outer end", lines[3]);
        Assert.Equal("outer cleanup", lines[4]); // outer defer 最后执行
    }
}
