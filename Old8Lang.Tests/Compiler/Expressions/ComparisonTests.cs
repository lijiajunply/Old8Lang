using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Expressions;

/// <summary>
/// 比较表达式编译模式测试
/// </summary>
[Collection("Sequential")]
public class ComparisonTests
{
    [Theory]
    [InlineData(5, 3, true)]
    [InlineData(3, 5, false)]
    [InlineData(5, 5, false)]
    [InlineData(-1, -2, true)]
    [InlineData(0, 0, false)]
    public void GreaterThan_TwoIntegers_CompilesAndExecutesCorrectly(int a, int b, bool expected)
    {
        // Arrange
        var code = $"result <- {a} > {b}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.Equal(expected, ((BoolLangValue)result).Value);
    }

    [Theory]
    [InlineData(3, 5, true)]
    [InlineData(5, 3, false)]
    [InlineData(5, 5, false)]
    [InlineData(-2, -1, true)]
    [InlineData(0, 0, false)]
    public void LessThan_TwoIntegers_CompilesAndExecutesCorrectly(int a, int b, bool expected)
    {
        // Arrange
        var code = $"result <- {a} < {b}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.Equal(expected, ((BoolLangValue)result).Value);
    }

    [Theory]
    [InlineData(5, 3, true)]
    [InlineData(3, 5, false)]
    [InlineData(5, 5, true)]
    [InlineData(-1, -2, true)]
    [InlineData(0, 0, true)]
    public void GreaterThanOrEqual_TwoIntegers_CompilesAndExecutesCorrectly(int a, int b, bool expected)
    {
        // Arrange
        var code = $"result <- {a} >= {b}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.Equal(expected, ((BoolLangValue)result).Value);
    }

    [Theory]
    [InlineData(3, 5, true)]
    [InlineData(5, 3, false)]
    [InlineData(5, 5, true)]
    [InlineData(-2, -1, true)]
    [InlineData(0, 0, true)]
    public void LessThanOrEqual_TwoIntegers_CompilesAndExecutesCorrectly(int a, int b, bool expected)
    {
        // Arrange
        var code = $"result <- {a} <= {b}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.Equal(expected, ((BoolLangValue)result).Value);
    }

    [Theory]
    [InlineData(5, 5, true)]
    [InlineData(3, 5, false)]
    [InlineData(5, 3, false)]
    [InlineData(-1, -1, true)]
    [InlineData(0, 0, true)]
    public void Equal_TwoIntegers_CompilesAndExecutesCorrectly(int a, int b, bool expected)
    {
        // Arrange
        var code = $"result <- {a} == {b}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.Equal(expected, ((BoolLangValue)result).Value);
    }

    [Theory]
    [InlineData(5, 5, false)]
    [InlineData(3, 5, true)]
    [InlineData(5, 3, true)]
    [InlineData(-1, -1, false)]
    [InlineData(0, 0, false)]
    public void NotEqual_TwoIntegers_CompilesAndExecutesCorrectly(int a, int b, bool expected)
    {
        // Arrange
        var code = $"result <- {a} != {b}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.Equal(expected, ((BoolLangValue)result).Value);
    }

    [Theory]
    [InlineData(3.14, 3.14, true)]
    [InlineData(1.5, 1.500001, false)]
    [InlineData(-2.5, -2.5, true)]
    public void Equal_TwoDoubles_CompilesAndExecutesCorrectly(double a, double b, bool expected)
    {
        // Arrange
        var code = $"result <- {a} == {b}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.Equal(expected, ((BoolLangValue)result).Value);
    }

    [Theory]
    [InlineData("hello", "hello", true)]
    [InlineData("hello", "world", false)]
    [InlineData("", "", true)]
    [InlineData("test", "test ", false)]
    public void Equal_TwoStrings_CompilesAndExecutesCorrectly(string a, string b, bool expected)
    {
        // Arrange
        var escapedA = a.Replace("\"", "\\\"");
        var escapedB = b.Replace("\"", "\\\"");
        var code = $"result <- \"{escapedA}\" == \"{escapedB}\"";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.Equal(expected, ((BoolLangValue)result).Value);
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    public void Equal_TwoBooleans_CompilesAndExecutesCorrectly(bool a, bool b, bool expected)
    {
        // Arrange
        var code = $"result <- {a.ToString().ToLower()} == {b.ToString().ToLower()}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.Equal(expected, ((BoolLangValue)result).Value);
    }

    [Fact]
    public void ComplexComparisonExpression_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            a <- 10
            b <- 20
            c <- 15
            result1 <- a < b and b > c
            result2 <- a >= c or c <= b
            result3 <- a != b and b == c
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        Assert.IsType<BoolLangValue>(result1);
        Assert.True(((BoolLangValue)result1).Value);

        var result2 = interpreter.Manager.GetValue(new LangId("result2"));
        Assert.IsType<BoolLangValue>(result2);
        Assert.True(((BoolLangValue)result2).Value);

        var result3 = interpreter.Manager.GetValue(new LangId("result3"));
        Assert.IsType<BoolLangValue>(result3);
        Assert.False(((BoolLangValue)result3).Value);
    }

    [Fact]
    public void ComparisonWithArithmetic_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            a <- 10
            b <- 5
            result1 <- a + b > 10
            result2 <- a - b >= 5
            result3 <- a * b == 50
            result4 <- a / b <= 2
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        Assert.IsType<BoolLangValue>(result1);
        Assert.True(((BoolLangValue)result1).Value);

        var result2 = interpreter.Manager.GetValue(new LangId("result2"));
        Assert.IsType<BoolLangValue>(result2);
        Assert.True(((BoolLangValue)result2).Value);

        var result3 = interpreter.Manager.GetValue(new LangId("result3"));
        Assert.IsType<BoolLangValue>(result3);
        Assert.True(((BoolLangValue)result3).Value);

        var result4 = interpreter.Manager.GetValue(new LangId("result4"));
        Assert.IsType<BoolLangValue>(result4);
        Assert.True(((BoolLangValue)result4).Value);
    }

    [Fact]
    public void ChainedComparisons_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            a <- 5
            b <- 10
            c <- 15
            result1 <- a < b < c
            result2 <- a <= b <= c
            result3 <- c > b > a
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        Assert.IsType<BoolLangValue>(result1);
        Assert.True(((BoolLangValue)result1).Value);

        var result2 = interpreter.Manager.GetValue(new LangId("result2"));
        Assert.IsType<BoolLangValue>(result2);
        Assert.True(((BoolLangValue)result2).Value);

        var result3 = interpreter.Manager.GetValue(new LangId("result3"));
        Assert.IsType<BoolLangValue>(result3);
        Assert.True(((BoolLangValue)result3).Value);
    }
}