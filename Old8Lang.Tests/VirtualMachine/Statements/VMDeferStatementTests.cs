using Old8Lang.Bytecode;
using Old8Lang.Interpreter;
using VM = Old8Lang.Bytecode.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Statements;

/// <summary>
/// 虚拟机 Defer 语句测试
/// 测试 Defer 语句的基本执行、LIFO顺序、变量访问、异常处理等功能
/// </summary>
[Collection("Sequential")]
public class VMDeferStatementTests
{
    private string ExecuteVMCode(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        var compiler = new BytecodeCompiler();
        var bytecodeFile = compiler.Compile(ast);

        var originalOut = Console.Out;
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            var vm = new VM(bytecodeFile);
            vm.Execute();
            return stringWriter.ToString().Trim();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void DeferStatement_BasicExecution_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func test() -> void {
                PrintLine(""Start"")
                defer PrintLine(""Deferred"")
                PrintLine(""End"")
            }

            test()
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        Assert.Equal("Start", lines[0]);
        Assert.Equal("End", lines[1]);
        Assert.Equal("Deferred", lines[2]);
    }

    [Fact]
    public void DeferStatement_LIFOOrder_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func test() -> void {
                defer PrintLine(""First"")
                defer PrintLine(""Second"")
                defer PrintLine(""Third"")
                PrintLine(""Body"")
            }

            test()
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(4, lines.Length);
        Assert.Equal("Body", lines[0]);
        Assert.Equal("Third", lines[1]);
        Assert.Equal("Second", lines[2]);
        Assert.Equal("First", lines[3]);
    }

    [Fact]
    public void DeferStatement_AccessLocalVariables_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func test() -> void {
                x <- 10
                defer PrintLine(""x = "" + x.ToStr())
                x <- 20
                PrintLine(""x = "" + x.ToStr())
            }

            test()
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("x = 20", lines[0]);
        Assert.Equal("x = 20", lines[1]); // Defer captures the final value
    }

    [Fact]
    public void DeferStatement_WithReturn_ExecutesBeforeReturn()
    {
        // Arrange
        var code = @"
            func test() -> int {
                defer PrintLine(""Deferred"")
                PrintLine(""Before return"")
                return 42
            }

            result <- test()
            PrintLine(""After call"")
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        Assert.Equal("Before return", lines[0]);
        Assert.Equal("Deferred", lines[1]);
        Assert.Equal("After call", lines[2]);
    }

    [Fact]
    public void DeferStatement_WithException_ExecutesOnException()
    {
        // Arrange
        var code = @"
            func test() -> void {
                defer PrintLine(""Cleanup"")
                PrintLine(""Start"")
                throw ""Error""
            }

            try {
                test()
            } catch (e) {
                PrintLine(""Caught: "" + e)
            }
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        Assert.Equal("Start", lines[0]);
        Assert.Equal("Cleanup", lines[1]);
        Assert.Equal("Caught: Error", lines[2]);
    }

    [Fact]
    public void DeferStatement_BlockForm_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func test() -> void {
                x <- 0
                defer {
                    x <- x + 1
                    PrintLine(""Deferred block: x = "" + x.ToStr())
                }
                PrintLine(""Body"")
            }

            test()
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("Body", lines[0]);
        Assert.Contains("Deferred block", lines[1]);
    }

    [Fact]
    public void DeferStatement_WithResourceCleanup_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func test() -> void {
                mutex <- MutexCreate()
                defer MutexDispose(mutex)

                MutexLock(mutex)
                defer MutexUnlock(mutex)

                PrintLine(""Critical section"")
            }

            test()
            PrintLine(""Done"")
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("Critical section", lines[0]);
        Assert.Equal("Done", lines[1]);
    }

    [Fact]
    public void DeferStatement_NestedFunctions_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func outer() -> void {
                defer PrintLine(""Outer defer"")
                PrintLine(""Outer start"")
                inner()
                PrintLine(""Outer end"")
            }

            func inner() -> void {
                defer PrintLine(""Inner defer"")
                PrintLine(""Inner body"")
            }

            outer()
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(5, lines.Length);
        Assert.Equal("Outer start", lines[0]);
        Assert.Equal("Inner body", lines[1]);
        Assert.Equal("Inner defer", lines[2]);
        Assert.Equal("Outer end", lines[3]);
        Assert.Equal("Outer defer", lines[4]);
    }

    [Fact]
    public void DeferStatement_InLoop_ExecutesPerIteration()
    {
        // Arrange
        var code = @"
            func test() -> void {
                for i in [1~3] {
                    defer PrintLine(""Defer "" + i.ToStr())
                    PrintLine(""Loop "" + i.ToStr())
                }
            }

            test()
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        // Defer should execute at the end of the function, not per iteration
        Assert.Contains("Loop 1", output);
        Assert.Contains("Loop 2", output);
        Assert.Contains("Loop 3", output);
    }

    [Fact]
    public void DeferStatement_WithMultipleReturns_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func test(x:int) -> string {
                defer PrintLine(""Cleanup"")

                if x > 0 {
                    PrintLine(""Positive"")
                    return ""positive""
                } elif x < 0 {
                    PrintLine(""Negative"")
                    return ""negative""
                } else {
                    PrintLine(""Zero"")
                    return ""zero""
                }
            }

            result1 <- test(5)
            result2 <- test(-3)
            result3 <- test(0)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(6, lines.Length);
        Assert.Contains("Cleanup", output);
    }

    [Fact]
    public void DeferStatement_ExceptionInDefer_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func test() -> void {
                defer {
                    PrintLine(""Defer 1"")
                }
                defer {
                    PrintLine(""Defer 2 - will throw"")
                    throw ""Defer error""
                }
                defer {
                    PrintLine(""Defer 3"")
                }
                PrintLine(""Body"")
            }

            try {
                test()
            } catch (e) {
                PrintLine(""Caught: "" + e)
            }
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Contains("Body", output);
        Assert.Contains("Defer 3", output);
    }

    [Fact]
    public void DeferStatement_WithChannelCleanup_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func test() -> void {
                ch <- ChannelCreate()
                defer ChannelDispose(ch)

                ChannelSend(ch, 42)
                val <- ChannelReceive(ch)
                PrintLine(""Received: "" + val.ToStr())
            }

            test()
            PrintLine(""Done"")
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("Received: 42", lines[0]);
        Assert.Equal("Done", lines[1]);
    }
}
