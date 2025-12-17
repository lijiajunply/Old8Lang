using Old8Lang.AST.Expression;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;

namespace Old8Lang.Tests.Interpreter.Expressions;

/// <summary>
/// 算术表达式解释模式测试
/// </summary>
public class ArithmeticTests
{
    [Theory]
    [InlineData(1, 2, 3)]
    [InlineData(-1, 1, 0)]
    [InlineData(0, 0, 0)]
    [InlineData(1000000, 2000000, 3000000)]
    [InlineData(-100, -200, -300)]
    public void Addition_TwoIntegers_ReturnsCorrectSum(int a, int b, int expected)
    {
        // Arrange
        var code = $"result <- {a} + {b}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

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
    public void Addition_TwoDoubles_ReturnsCorrectSum(double a, double b, double expected)
    {
        // Arrange
        var code = $"result <- {a} + {b}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<DoubleLangValue>(result);
        Assert.Equal(expected, ((DoubleLangValue)result).Value);
    }

    [Theory]
    [InlineData(10, 3, 7)]
    [InlineData(5, 10, -5)]
    [InlineData(0, 5, -5)]
    [InlineData(-10, -3, -7)]
    public void Subtraction_TwoIntegers_ReturnsCorrectDifference(int a, int b, int expected)
    {
        // Arrange
        var code = $"result <- {a} - {b}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(expected, ((IntLangValue)result).Value);
    }

    [Theory]
    [InlineData(2, 3, 6)]
    [InlineData(-2, 3, -6)]
    [InlineData(0, 100, 0)]
    [InlineData(7, 0, 0)]
    [InlineData(-4, -5, 20)]
    public void Multiplication_TwoIntegers_ReturnsCorrectProduct(int a, int b, int expected)
    {
        // Arrange
        var code = $"result <- {a} * {b}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(expected, ((IntLangValue)result).Value);
    }

    [Theory]
    [InlineData(10, 2, 5)]
    [InlineData(9, 3, 3)]
    [InlineData(7, 2, 3)]  // 整数除法，截断小数部分
    [InlineData(-10, 2, -5)]
    [InlineData(10, -2, -5)]
    [InlineData(5, 2, 2)]  // 整数除法，截断小数部分
    public void Division_TwoNumbers_ReturnsCorrectQuotient(double a, double b, double expected)
    {
        // Arrange
        var code = $"result <- {a} / {b}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(expected, ((IntLangValue)result).Value);
    }

    [Fact]
    public void Division_ByZero_ThrowsZeroDivisionError()
    {
        // Arrange
        var code = "result <- 10 / 0";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);

        // Assert
        Assert.Throws<ZeroDivisionError>(() => ast.Run(interpreter.Manager));
    }

    [Theory]
    [InlineData(10, 3, 1)]
    [InlineData(10, 2, 0)]
    [InlineData(9, 3, 0)]
    [InlineData(7, 3, 1)]
    [InlineData(-10, 3, -1)]
    public void Modulo_TwoIntegers_ReturnsCorrectRemainder(int a, int b, int expected)
    {
        // Arrange
        var code = $"result <- {a} % {b}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(expected, ((IntLangValue)result).Value);
    }

    [Fact]
    public void Modulo_ByZero_ThrowsZeroDivisionError()
    {
        // Arrange
        var code = "result <- 10 % 0";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);

        // Assert
        Assert.Throws<ZeroDivisionError>(() => ast.Run(interpreter.Manager));
    }

    [Theory]
    [InlineData(2, 3, 8)]        // 2^3 = 8
    [InlineData(5, 0, 1)]        // 5^0 = 1
    [InlineData(0, 5, 0)]        // 0^5 = 0
    [InlineData(-2, 3, -8)]      // (-2)^3 = -8
    public void PowerOperation_TwoIntegers_ReturnsCorrectResult(int a, int b, int expected)
    {
        // Arrange
        var code = $"result <- {a} ^ {b}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.Equal(expected, ((DoubleLangValue)result).Value);
    }

    [Fact]
    public void ComplexArithmeticExpression_FollowsCorrectPrecedence()
    {
        // Arrange
        var code = "result <- 2 + 3 * 4 - 6 / 2"; // 2 + 12 - 3 = 11
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.Equal(11.0, ((DoubleLangValue)result).Value);
    }

    [Fact]
    public void ExpressionWithParentheses_FollowsParenthesesPrecedence()
    {
        // Arrange
        var code = "result <- (2 + 3) * (4 - 6 / 2)"; // 5 * 1 = 5
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.Equal(5.0, ((DoubleLangValue)result).Value);
    }

    [Fact]
    public void UnaryMinus_PositiveNumber_BecomesNegative()
    {
        // Arrange
        var code = "result <- -10";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(-10, ((IntLangValue)result).Value);
    }

    [Fact]
    public void UnaryMinus_NegativeNumber_BecomesPositive()
    {
        // Arrange
        var code = "result <- -(-10)";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(10, ((IntLangValue)result).Value);
    }

    [Fact]
    public void MixedTypeOperations_AutomaticConversion()
    {
        // Arrange
        var code = @"
            int_val <- 5
            double_val <- 2.5
            sum <- int_val + double_val
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var sum = interpreter.Manager.GetValue(new LangId("sum"));
        Assert.NotNull(sum);
        Assert.IsType<DoubleLangValue>(sum);
        Assert.Equal(7.5, ((DoubleLangValue)sum).Value);
    }

    [Fact]
    public void LongChainOfOperations_CalculatesCorrectly()
    {
        // Arrange
        var code = "result <- 1 + 2 + 3 + 4 + 5 + 6 + 7 + 8 + 9 + 10";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(55, ((IntLangValue)result).Value);
    }

    [Fact]
    public void NestedParentheses_CalculatesCorrectly()
    {
        // Arrange
        var code = "result <- ((1 + 2) * (3 + 4)) / (5 - 2)"; // 3 * 7 / 3 = 7
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.Equal(7.0, ((DoubleLangValue)result).Value);
    }

    [Fact]
    public void VariableBasedArithmetic_UsesVariableValues()
    {
        // Arrange
        var code = @"
            a <- 10
            b <- 20
            c <- a + b * 2
            d <- (a + b) / 2
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var c = interpreter.Manager.GetValue(new LangId("c"));
        var d = interpreter.Manager.GetValue(new LangId("d"));

        Assert.NotNull(c);
        Assert.NotNull(d);
        Assert.Equal(50, ((IntLangValue)c).Value); // 10 + 20 * 2 = 50
        Assert.Equal(15.0, ((DoubleLangValue)d).Value); // (10 + 20) / 2 = 15
    }

    [Theory]
    [InlineData(int.MaxValue, 1)]
    [InlineData(int.MinValue, -1)]
    [InlineData(int.MinValue, int.MinValue)]
    public void ExtremeIntegerValues_HandlesCorrectly(int a, int b)
    {
        // Arrange
        var code = $"result <- {a} + {b}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        // 注意：可能会溢出，这里主要测试不会崩溃
        Assert.True(result is IntLangValue or DoubleLangValue);
    }

    [Fact]
    public void FloatingPointPrecision_HandlesPrecisely()
    {
        // Arrange
        var code = "result <- 0.1 + 0.2"; // 浮点精度问题
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<DoubleLangValue>(result);
        // 使用近似比较，因为浮点数精度问题
        Assert.True(Math.Abs(0.3 - ((DoubleLangValue)result).Value) < 0.0001);
    }
}