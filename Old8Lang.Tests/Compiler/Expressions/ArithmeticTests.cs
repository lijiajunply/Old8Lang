using Old8Lang.Interpreter;
using System.Globalization;

namespace Old8Lang.Tests.Compiler.Expressions;

/// <summary>
/// 算术表达式编译模式测试
/// </summary>
[Collection("Sequential")]
public class ArithmeticTests
{
    [Theory]
    [InlineData(1, 2, 3)]
    [InlineData(-1, 1, 0)]
    [InlineData(0, 0, 0)]
    [InlineData(1000000, 2000000, 3000000)]
    [InlineData(-100, -200, -300)]
    public void Addition_TwoIntegers_CompilesAndExecutesCorrectly(int a, int b, int expected)
    {
        // Arrange
        var code = $@"
            result <- {a} + {b}
            Assert.Equal({expected}, result)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 如果 Old8Lang 断言失败会抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(1.5, 2.5, 4.0)]
    [InlineData(-1.5, 1.5, 0.0)]
    [InlineData(0.0, 0.0, 0.0)]
    [InlineData(1e10, 1e10, 2e10)]
    [InlineData(-1.1, -2.2, -3.3)]
    public void Addition_TwoDoubles_CompilesAndExecutesCorrectly(double a, double b, double expected)
    {
        // Arrange
        var aStr = a.ToString(CultureInfo.InvariantCulture);
        var bStr = b.ToString(CultureInfo.InvariantCulture);
        var expectedStr = expected.ToString(CultureInfo.InvariantCulture);
        var code = $@"
            result <- {aStr} + {bStr}
            Assert.Equal({expectedStr}, result)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 如果 Old8Lang 断言失败会抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(10, 3, 7)]
    [InlineData(5, 10, -5)]
    [InlineData(0, 5, -5)]
    [InlineData(-5, -10, 5)]
    public void Subtraction_TwoIntegers_CompilesAndExecutesCorrectly(int a, int b, int expected)
    {
        // Arrange
        var code = $@"
            result <- {a} - {b}
            Assert.Equal({expected}, result)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 如果 Old8Lang 断言失败会抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(4, 5, 20)]
    [InlineData(-4, 5, -20)]
    [InlineData(0, 10, 0)]
    [InlineData(7, -3, -21)]
    public void Multiplication_TwoIntegers_CompilesAndExecutesCorrectly(int a, int b, int expected)
    {
        // Arrange
        var code = $@"
            result <- {a} * {b}
            Assert.Equal({expected}, result)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 如果 Old8Lang 断言失败会抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(10, 2, 5.0)]
    [InlineData(9, 3, 3.0)]
    [InlineData(-10, 2, -5.0)]
    [InlineData(15, -3, -5.0)]
    public void Division_TwoIntegers_CompilesAndExecutesCorrectly(int a, int b, double expected)
    {
        // Arrange
        var expectedStr = expected.ToString(CultureInfo.InvariantCulture);
        var code = $@"
            result <- {a} / {b}
            Assert.Equal({expectedStr}, result)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 如果 Old8Lang 断言失败会抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(10, 3, 1.0)]
    [InlineData(9, 3, 0.0)]
    [InlineData(10, 2, 0.0)]
    [InlineData(-10, 3, -1.0)]
    public void Modulo_TwoIntegers_CompilesAndExecutesCorrectly(int a, int b, double expected)
    {
        // Arrange
        var expectedStr = expected.ToString(CultureInfo.InvariantCulture);
        var code = $@"
            result <- {a} % {b}
            Assert.Equal({expectedStr}, result)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 如果 Old8Lang 断言失败会抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ComplexArithmeticExpression_CompilesAndExecutesCorrectly()
    {
        // Arrange
        // (10 + 5) * 3 - (10 - 5) / 3 = 15 * 3 - 5 / 3 = 45 - 1.666... = 43.333...
        var expected = 15.0 * 3.0 - 5.0 / 3.0;
        var expectedStr = expected.ToString(CultureInfo.InvariantCulture);
        var code = $@"
            a <- 10.0
            b <- 5.0
            c <- 3.0
            result <- (a + b) * c - (a - b) / c
            Assert.Equal({expectedStr}, result)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 如果 Old8Lang 断言失败会抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void PowerOperation_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            a <- 2
            b <- 3
            result <- a ^ b
            Assert.Equal(8.0, result)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 如果 Old8Lang 断言失败会抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(42)]
    [InlineData(-100)]
    public void UnaryMinus_Integer_CompilesAndExecutesCorrectly(int value)
    {
        // Arrange
        var negatedValue = -value;
        var code = $@"
            result <- -{value}
            Assert.Equal({negatedValue}, result)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 如果 Old8Lang 断言失败会抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void MixedTypeArithmetic_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            intVal <- 5
            doubleVal <- 2.5
            result1 <- intVal + doubleVal
            result2 <- intVal * doubleVal
            result3 <- intVal / doubleVal

            Assert.Equal(7.5, result1)
            Assert.Equal(12.5, result2)
            Assert.Equal(2.0, result3)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 如果 Old8Lang 断言失败会抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void DivisionByZero_ThrowsRuntimeException()
    {
        // Arrange
        var code = "result <- 10 / 0";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 应该抛出异常
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.NotNull(exception);
    }

    [Fact]
    public void ModuloByZero_ThrowsRuntimeException()
    {
        // Arrange
        var code = "result <- 10 % 0";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 应该抛出异常
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.NotNull(exception);
    }

    [Fact]
    public void VeryLargeNumbers_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            large1 <- 2147483647
            large2 <- 2147483646
            sum <- large1 + large2
            product <- large1 * 2

            Assert.Equal(4294967293.0, sum)
            Assert.Equal(4294967294.0, product)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 如果 Old8Lang 断言失败会抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }
}
