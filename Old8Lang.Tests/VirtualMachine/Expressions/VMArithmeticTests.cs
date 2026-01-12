using Old8Lang.Bytecode;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.VirtualMachine.Expressions;

/// <summary>
/// 虚拟机算术表达式测试
/// 测试虚拟机执行各种算术运算的正确性
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
            var vm = new Bytecode.VirtualMachine(bytecodeFile);
            vm.Execute();

            return stringWriter.ToString().Trim();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Theory]
    [InlineData(10, 5, 15)]
    [InlineData(0, 0, 0)]
    [InlineData(-5, 3, -2)]
    [InlineData(100, -50, 50)]
    public void Addition_ExecutesCorrectly(int a, int b, int expected)
    {
        // Arrange
        var code = $@"
            result <- {a} + {b}
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal(expected.ToString(), output);
    }

    [Theory]
    [InlineData(10, 5, 5)]
    [InlineData(0, 0, 0)]
    [InlineData(-5, 3, -8)]
    [InlineData(100, -50, 150)]
    public void Subtraction_ExecutesCorrectly(int a, int b, int expected)
    {
        // Arrange
        var code = $@"
            result <- {a} - {b}
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal(expected.ToString(), output);
    }

    [Theory]
    [InlineData(10, 5, 50)]
    [InlineData(0, 100, 0)]
    [InlineData(-5, 3, -15)]
    [InlineData(-4, -6, 24)]
    public void Multiplication_ExecutesCorrectly(int a, int b, int expected)
    {
        // Arrange
        var code = $@"
            result <- {a} * {b}
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal(expected.ToString(), output);
    }

    [Theory]
    [InlineData(10, 5, 2)]
    [InlineData(100, 4, 25)]
    [InlineData(-15, 3, -5)]
    [InlineData(-20, -4, 5)]
    public void Division_ExecutesCorrectly(int a, int b, int expected)
    {
        // Arrange
        var code = $@"
            result <- {a} / {b}
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal(expected.ToString(), output);
    }

    [Theory]
    [InlineData(10, 3, 1)]
    [InlineData(15, 4, 3)]
    [InlineData(100, 7, 2)]
    public void Modulo_ExecutesCorrectly(int a, int b, int expected)
    {
        // Arrange
        var code = $@"
            result <- {a} % {b}
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal(expected.ToString(), output);
    }

    [Theory]
    [InlineData(2, 3, 8)]
    [InlineData(5, 2, 25)]
    [InlineData(10, 0, 1)]
    public void Power_ExecutesCorrectly(int baseNum, int exponent, int expected)
    {
        // Arrange
        var code = $@"
            result <- {baseNum} ^ {exponent}
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal(expected.ToString(), output);
    }

    [Theory]
    [InlineData(5, -5)]
    [InlineData(-10, 10)]
    [InlineData(0, 0)]
    public void Negation_ExecutesCorrectly(int value, int expected)
    {
        // Arrange
        // 注意：当 value 为负数时，需要使用括号避免 -- 被解析为自减运算符
        var code = value < 0
            ? $@"
            result <- -({value})
            PrintLine(result.ToStr())
        "
            : $@"
            result <- -{value}
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal(expected.ToString(), output);
    }

    [Fact]
    public void OperatorPrecedence_ExecutesCorrectly()
    {
        // Arrange - 测试运算符优先级: 2 + 3 * 4 = 2 + 12 = 14
        var code = @"
            result <- 2 + 3 * 4
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("14", output);
    }

    [Fact]
    public void ParenthesesPrecedence_ExecutesCorrectly()
    {
        // Arrange - 测试括号优先级: (2 + 3) * 4 = 5 * 4 = 20
        var code = @"
            result <- (2 + 3) * 4
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("20", output);
    }

    [Fact]
    public void ComplexExpression_ExecutesCorrectly()
    {
        // Arrange - 复杂表达式: ((10 + 5) * 2 - 8) / 2 = (15 * 2 - 8) / 2 = (30 - 8) / 2 = 22 / 2 = 11
        var code = @"
            result <- ((10 + 5) * 2 - 8) / 2
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("11", output);
    }

    [Fact]
    public void DoubleArithmetic_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            a <- 3.14
            b <- 2.0
            result <- a * b
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("6.28", output);
    }

    [Fact]
    public void MixedIntDoubleArithmetic_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            intVal <- 10
            doubleVal <- 2.5
            result <- intVal * doubleVal
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("25", output);
    }

    [Fact]
    public void ChainedArithmetic_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            a <- 1
            b <- 2
            c <- 3
            d <- 4
            result <- a + b + c + d
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("10", output);
    }

    [Fact]
    public void ArithmeticWithVariables_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            x <- 10
            y <- 5
            sum <- x + y
            diff <- x - y
            prod <- x * y
            quot <- x / y
            PrintLine(sum.ToStr())
            PrintLine(diff.ToStr())
            PrintLine(prod.ToStr())
            PrintLine(quot.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(4, lines.Length);
        Assert.Equal("15", lines[0]); // 10 + 5
        Assert.Equal("5", lines[1]);  // 10 - 5
        Assert.Equal("50", lines[2]); // 10 * 5
        Assert.Equal("2", lines[3]);  // 10 / 5
    }

    [Fact]
    public void StringConcatenation_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            str1 <- ""Hello""
            str2 <- ""World""
            result <- str1 + "" "" + str2
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Hello World", output);
    }

    [Fact]
    public void StringNumberConcatenation_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            text <- ""Number: ""
            num <- 42
            result <- text + num.ToStr()
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Number: 42", output);
    }
}