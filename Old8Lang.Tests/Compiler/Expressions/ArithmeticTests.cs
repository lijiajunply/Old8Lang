using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;
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
        var code = $"result <- {a} + {b}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(expected, ((IntLangValue)result).Value);
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
        var code = $"result <- {aStr} + {bStr}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<DoubleLangValue>(result);
        Assert.Equal(expected, ((DoubleLangValue)result).Value, 10);
    }

    [Theory]
    [InlineData(10, 3, 7)]
    [InlineData(5, 10, -5)]
    [InlineData(0, 5, -5)]
    [InlineData(-5, -10, 5)]
    public void Subtraction_TwoIntegers_CompilesAndExecutesCorrectly(int a, int b, int expected)
    {
        // Arrange
        var code = $"result <- {a} - {b}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(expected, ((IntLangValue)result).Value);
    }

    [Theory]
    [InlineData(4, 5, 20)]
    [InlineData(-4, 5, -20)]
    [InlineData(0, 10, 0)]
    [InlineData(7, -3, -21)]
    public void Multiplication_TwoIntegers_CompilesAndExecutesCorrectly(int a, int b, int expected)
    {
        // Arrange
        var code = $"result <- {a} * {b}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(expected, ((IntLangValue)result).Value);
    }

    [Theory]
    [InlineData(10, 2, 5)]
    [InlineData(9, 3, 3)]
    [InlineData(-10, 2, -5)]
    [InlineData(15, -3, -5)]
    public void Division_TwoIntegers_CompilesAndExecutesCorrectly(int a, int b, int expected)
    {
        // Arrange
        var code = $"result <- {a} / {b}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<DoubleLangValue>(result); // Division returns double
        Assert.Equal((double)expected, ((DoubleLangValue)result).Value, 10);
    }

    [Theory]
    [InlineData(10, 3, 1)]
    [InlineData(9, 3, 0)]
    [InlineData(10, 2, 0)]
    [InlineData(-10, 3, -1)]
    public void Modulo_TwoIntegers_CompilesAndExecutesCorrectly(int a, int b, int expected)
    {
        // Arrange
        var code = $"result <- {a} % {b}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<DoubleLangValue>(result);
        Assert.Equal((double)expected, ((DoubleLangValue)result).Value, 10);
    }

    [Fact]
    public void ComplexArithmeticExpression_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            a <- 10
            b <- 5
            c <- 3
            result <- (a + b) * c - (a - b) / c
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<DoubleLangValue>(result);
        // (10 + 5) * 3 - (10 - 5) / 3 = 15 * 3 - 5 / 3 = 45 - 1.666... = 43.333...
        var expected = (15.0 * 3.0) - (5.0 / 3.0);
        Assert.Equal(expected, ((DoubleLangValue)result).Value, 2);
    }

    [Fact]
    public void PowerOperation_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            a <- 2
            b <- 3
            result <- a ^ b
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<DoubleLangValue>(result);
        Assert.Equal(8.0, ((DoubleLangValue)result).Value, 10);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(42)]
    [InlineData(-100)]
    public void UnaryPlus_Integer_CompilesAndExecutesCorrectly(int value)
    {
        // Arrange
        var code = $"result <- +{value}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(value, ((IntLangValue)result).Value);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(42)]
    [InlineData(-100)]
    public void UnaryMinus_Integer_CompilesAndExecutesCorrectly(int value)
    {
        // Arrange
        var code = $"result <- -{value}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(-value, ((IntLangValue)result).Value);
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
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        Assert.IsType<DoubleLangValue>(result1);
        Assert.Equal(7.5, ((DoubleLangValue)result1).Value, 10);

        var result2 = interpreter.Manager.GetValue(new LangId("result2"));
        Assert.IsType<DoubleLangValue>(result2);
        Assert.Equal(12.5, ((DoubleLangValue)result2).Value, 10);

        var result3 = interpreter.Manager.GetValue(new LangId("result3"));
        Assert.IsType<DoubleLangValue>(result3);
        Assert.Equal(2.0, ((DoubleLangValue)result3).Value, 10);
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

        // Assert
        Assert.NotNull(compiledAction);
        Assert.Throws<Old8Exception>(() => compiledAction());
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

        // Assert
        Assert.NotNull(compiledAction);
        Assert.Throws<Old8Exception>(() => compiledAction());
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
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        var sum = interpreter.Manager.GetValue(new LangId("sum"));
        Assert.IsType<DoubleLangValue>(sum);
        Assert.Equal(4294967293.0, ((DoubleLangValue)sum).Value);

        var product = interpreter.Manager.GetValue(new LangId("product"));
        Assert.IsType<DoubleLangValue>(product);
        Assert.Equal(4294967294.0, ((DoubleLangValue)product).Value);
    }
}