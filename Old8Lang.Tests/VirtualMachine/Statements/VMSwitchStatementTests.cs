using Old8Lang.Bytecode;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.VirtualMachine.Statements;

/// <summary>
/// 虚拟机 Switch 语句测试
/// 测试虚拟机执行 switch 语句的正确性
/// </summary>
[Collection("Sequential")]
public class VMSwitchStatementTests
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
            var vm = new Bytecode.VM.VirtualMachine(bytecodeFile);
            vm.Execute();

            return stringWriter.ToString().Trim();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void SwitchStatement_BasicCase_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            x <- 2
            switch x {
                case 1 {
                    PrintLine(""one"")
                }
                case 2 {
                    PrintLine(""two"")
                }
                case 3 {
                    PrintLine(""three"")
                }
            }
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("two", output);
    }

    [Fact]
    public void SwitchStatement_WithDefault_ExecutesDefault()
    {
        // Arrange
        var code = @"
            x <- 5
            switch x {
                case 1 {
                    PrintLine(""one"")
                }
                case 2 {
                    PrintLine(""two"")
                }
                default {
                    PrintLine(""other"")
                }
            }
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("other", output);
    }

    [Fact]
    public void SwitchStatement_StringCase_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            name <- ""Alice""
            switch name {
                case ""Bob"" {
                    PrintLine(""Hello Bob"")
                }
                case ""Alice"" {
                    PrintLine(""Hello Alice"")
                }
                case ""Charlie"" {
                    PrintLine(""Hello Charlie"")
                }
            }
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Hello Alice", output);
    }

    [Fact]
    public void SwitchStatement_MultipleCases_ExecutesFirstMatch()
    {
        // Arrange
        var code = @"
            x <- 2
            switch x {
                case 1 {
                    PrintLine(""case 1"")
                }
                case 2 {
                    PrintLine(""case 2 - first"")
                }
                case 2 {
                    PrintLine(""case 2 - second"")
                }
                case 3 {
                    PrintLine(""case 3"")
                }
            }
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("case 2 - first", output);
    }

    [Fact]
    public void SwitchStatement_NestedSwitch_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            x <- 1
            y <- 2
            switch x {
                case 1 {
                    PrintLine(""outer: 1"")
                    switch y {
                        case 1 {
                            PrintLine(""inner: 1"")
                        }
                        case 2 {
                            PrintLine(""inner: 2"")
                        }
                    }
                }
                case 2 {
                    PrintLine(""outer: 2"")
                }
            }
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("outer: 1", lines[0]);
        Assert.Equal("inner: 2", lines[1]);
    }

    [Fact]
    public void SwitchStatement_InLoop_ExecutesMultipleTimes()
    {
        // Arrange
        var code = @"
            for i in [1~3] {
                switch i {
                    case 1 {
                        PrintLine(""one"")
                    }
                    case 2 {
                        PrintLine(""two"")
                    }
                    case 3 {
                        PrintLine(""three"")
                    }
                }
            }
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        Assert.Equal("one", lines[0]);
        Assert.Equal("two", lines[1]);
        Assert.Equal("three", lines[2]);
    }

    [Fact]
    public void SwitchStatement_WithReturn_ReturnsFromFunction()
    {
        // Arrange
        var code = @"
            func test(x:int) -> string {
                switch x {
                    case 1 {
                        return ""one""
                    }
                    case 2 {
                        return ""two""
                    }
                    default {
                        return ""other""
                    }
                }
            }
            result <- test(2)
            PrintLine(result)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("two", output);
    }

    [Fact]
    public void SwitchStatement_BooleanCase_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            flag <- true
            switch flag {
                case true {
                    PrintLine(""true case"")
                }
                case false {
                    PrintLine(""false case"")
                }
            }
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("true case", output);
    }

    [Fact]
    public void SwitchStatement_WithExpression_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            x <- 5
            switch x * 2 {
                case 5 {
                    PrintLine(""five"")
                }
                case 10 {
                    PrintLine(""ten"")
                }
                case 15 {
                    PrintLine(""fifteen"")
                }
            }
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("ten", output);
    }

    [Fact]
    public void SwitchStatement_EmptyCase_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            x <- 1
            switch x {
                case 1 {
                }
                case 2 {
                    PrintLine(""two"")
                }
            }
            PrintLine(""done"")
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("done", output);
    }

    [Fact]
    public void SwitchStatement_WithException_PropagatesException()
    {
        // Arrange
        var code = @"
            try {
                x <- 2
                switch x {
                    case 1 {
                        PrintLine(""case 1"")
                    }
                    case 2 {
                        throw ""test exception""
                    }
                    case 3 {
                        PrintLine(""case 3"")
                    }
                }
            } catch (e) {
                PrintLine(""caught: "" + e.ToStr())
            }
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("caught: test exception", output);
    }

    [Fact]
    public void SwitchStatement_NullCase_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            x <- null
            switch x {
                case null {
                    PrintLine(""null case"")
                }
                case 1 {
                    PrintLine(""one"")
                }
                default {
                    PrintLine(""other"")
                }
            }
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("null case", output);
    }

    [Fact]
    public void SwitchStatement_DoubleCase_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            x <- 3.14
            switch x {
                case 2.71 {
                    PrintLine(""e"")
                }
                case 3.14 {
                    PrintLine(""pi"")
                }
                default {
                    PrintLine(""other"")
                }
            }
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("pi", output);
    }

    [Fact]
    public void SwitchStatement_WithDefer_ExecutesInCorrectOrder()
    {
        // Arrange
        var code = @"
            func test(x:int) -> void {
                defer PrintLine(""defer cleanup"")
                switch x {
                    case 1 {
                        PrintLine(""case 1"")
                    }
                    case 2 {
                        PrintLine(""case 2"")
                        return
                    }
                }
                PrintLine(""after switch"")
            }
            test(2)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("case 2", lines[0]);
        Assert.Equal("defer cleanup", lines[1]); // defer 在 return 前执行
    }
}
