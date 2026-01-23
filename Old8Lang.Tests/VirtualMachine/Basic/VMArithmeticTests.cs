using Old8Lang.Bytecode;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.VirtualMachine.Basic;

/// <summary>
/// 虚拟机算术运算测试
/// 测试虚拟机执行基本算术运算的正确性
/// </summary>
[Collection("Sequential")]
public class VMArithmeticTests
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
    public void SimpleAddition_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result <- 10 + 5
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("15", output);
    }

    [Fact]
    public void SimpleSubtraction_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result <- 20 - 8
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("12", output);
    }

    [Fact]
    public void SimpleMultiplication_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result <- 6 * 7
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("42", output);
    }

    [Fact]
    public void SimpleDivision_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result <- 100 / 4
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("25", output);
    }

    [Fact]
    public void SimpleModulo_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result <- 17 % 5
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("2", output);
    }

    [Fact]
    public void Negation_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            a <- 42
            result <- -a
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("-42", output);
    }

    [Fact]
    public void ComplexExpression_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result <- (10 + 5) * 2 - 3
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("27", output);
    }

    [Fact]
    public void DoubleArithmetic_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result <- 10.5 + 2.3
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("12.8", output);
    }

    [Fact]
    public void MixedIntAndDouble_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            result <- 10 + 2.5
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("12.5", output);
    }

    [Fact]
    public void MultipleOperations_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            a <- 10
            b <- 20
            c <- 5
            result <- a + b * c
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("110", output);
    }
}
