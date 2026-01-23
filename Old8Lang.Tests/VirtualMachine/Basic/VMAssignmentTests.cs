using Old8Lang.Bytecode;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.VirtualMachine.Basic;

/// <summary>
/// 虚拟机基础赋值语句测试
/// 测试虚拟机执行基本变量赋值操作的正确性
/// </summary>
[Collection("Sequential")]
public class VMAssignmentTests
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
    public void SimpleIntegerAssignment_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            a <- 42
            PrintLine(a.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("42", output);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(123456789)]
    [InlineData(-987654321)]
    public void IntegerAssignment_EdgeCases_ExecuteCorrectly(int value)
    {
        // Arrange
        var code = $@"
            a <- {value}
            PrintLine(a.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal(value.ToString(), output);
    }

    [Theory]
    [InlineData(0.1)]
    [InlineData(3.14159)]
    [InlineData(-2.71828)]
    public void DoubleAssignment_ExecutesCorrectly(double value)
    {
        // Arrange
        var code = $@"
            a <- {value}
            PrintLine(a.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal(value.ToString(), output);
    }

    [Theory]
    [InlineData("hello")]
    [InlineData("Hello, World!")]
    [InlineData("")]
    public void StringAssignment_ExecutesCorrectly(string str)
    {
        // Arrange
        var escapedStr = str.Replace("\"", "\\\"");
        var code = $@"
            a <- ""{escapedStr}""
            PrintLine(a.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal(str, output);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BooleanAssignment_ExecutesCorrectly(bool value)
    {
        // Arrange
        var code = $@"
            a <- {value.ToString().ToLower()}
            PrintLine(a.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal(value.ToString(), output);
    }

    [Fact]
    public void MultipleAssignments_ExecuteCorrectly()
    {
        // Arrange
        var code = @"
            a <- 10
            b <- 20
            c <- 30
            PrintLine(a.ToStr())
            PrintLine(b.ToStr())
            PrintLine(c.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        Assert.Equal("10", lines[0]);
        Assert.Equal("20", lines[1]);
        Assert.Equal("30", lines[2]);
    }

    [Fact]
    public void VariableReassignment_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            a <- 10
            PrintLine(a.ToStr())
            a <- 20
            PrintLine(a.ToStr())
            a <- 30
            PrintLine(a.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        Assert.Equal("10", lines[0]);
        Assert.Equal("20", lines[1]);
        Assert.Equal("30", lines[2]);
    }

    [Fact]
    public void AssignmentWithExpression_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            a <- 10
            b <- 20
            c <- a + b
            PrintLine(c.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("30", output);
    }

    [Fact]
    public void ComplexExpressionAssignment_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            a <- 10
            b <- 20
            c <- 30
            result <- (a + b) * c - (a * b)
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        // (10 + 20) * 30 - (10 * 20) = 30 * 30 - 200 = 900 - 200 = 700
        Assert.Equal("700", output);
    }

    [Fact]
    public void StringConcatenationAssignment_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            first <- ""Hello""
            second <- ""World""
            result <- first + "" "" + second
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Hello World", output);
    }

    [Fact]
    public void MixedTypeAssignments_ExecuteCorrectly()
    {
        // Arrange
        var code = @"
            intVar <- 42
            doubleVar <- 3.14
            stringVar <- ""test""
            boolVar <- true
            PrintLine(intVar.ToStr())
            PrintLine(doubleVar.ToStr())
            PrintLine(stringVar.ToStr())
            PrintLine(boolVar.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(4, lines.Length);
        Assert.Equal("42", lines[0]);
        Assert.Equal("3.14", lines[1]);
        Assert.Equal("test", lines[2]);
        Assert.Equal("True", lines[3]);
    }
}