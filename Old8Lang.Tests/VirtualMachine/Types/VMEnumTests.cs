using Old8Lang.Bytecode;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.VirtualMachine.Types;

/// <summary>
/// 虚拟机枚举类型测试
/// 测试虚拟机执行枚举定义和使用的正确性
/// </summary>
[Collection("Sequential")]
public class VMEnumTests
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
    public void SimpleEnumDefinition_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            enum Color {
                Red,
                Green,
                Blue
            }

            PrintLine(Color.Red.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("0", output);
    }

    [Fact]
    public void EnumWithExplicitValues_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            enum Status {
                Pending <- 10,
                Active <- 20,
                Completed <- 30
            }

            PrintLine(Status.Pending.ToStr())
            PrintLine(Status.Active.ToStr())
            PrintLine(Status.Completed.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("10\n20\n30", output);
    }

    [Fact]
    public void EnumWithMixedValues_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            enum Priority {
                Low,
                Medium <- 5,
                High,
                Critical <- 10
            }

            PrintLine(Priority.Low.ToStr())
            PrintLine(Priority.Medium.ToStr())
            PrintLine(Priority.High.ToStr())
            PrintLine(Priority.Critical.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("0\n5\n6\n10", output);
    }

    [Fact]
    public void EnumInComparison_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            enum Status {
                Pending,
                Active,
                Completed
            }

            status <- Status.Active
            if status == Status.Active {
                PrintLine(""Active"")
            }
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Active", output);
    }

    [Fact]
    public void EnumInSwitchStatement_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            enum Color {
                Red,
                Green,
                Blue
            }

            color <- Color.Green
            switch color {
                case Color.Red {
                    PrintLine(""Red"")
                }
                case Color.Green {
                    PrintLine(""Green"")
                }
                case Color.Blue {
                    PrintLine(""Blue"")
                }
            }
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Green", output);
    }

    [Fact]
    public void EnumAsFunctionParameter_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            enum Status {
                Pending,
                Active,
                Completed
            }

            func printStatus(s:Status) -> void {
                if s == Status.Pending {
                    PrintLine(""Pending"")
                } elif s == Status.Active {
                    PrintLine(""Active"")
                } else {
                    PrintLine(""Completed"")
                }
            }

            printStatus(Status.Active)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Active", output);
    }

    [Fact]
    public void EnumWithNegativeValues_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            enum Temperature {
                Cold <- -10,
                Cool <- 0,
                Warm <- 10,
                Hot <- 20
            }

            PrintLine(Temperature.Cold.ToStr())
            PrintLine(Temperature.Cool.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("-10\n0", output);
    }
}
