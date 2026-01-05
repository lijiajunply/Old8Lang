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
        var code = $@"
            result <- {a} > {b}
            Assert.Equal({expected.ToString().ToLower()}, result)
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
    [InlineData(3, 5, true)]
    [InlineData(5, 3, false)]
    [InlineData(5, 5, false)]
    [InlineData(-2, -1, true)]
    [InlineData(0, 0, false)]
    public void LessThan_TwoIntegers_CompilesAndExecutesCorrectly(int a, int b, bool expected)
    {
        // Arrange
        var code = $@"
            result <- {a} < {b}
            Assert.Equal({expected.ToString().ToLower()}, result)
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
    [InlineData(5, 3, true)]
    [InlineData(3, 5, false)]
    [InlineData(5, 5, true)]
    [InlineData(-1, -2, true)]
    [InlineData(0, 0, true)]
    public void GreaterThanOrEqual_TwoIntegers_CompilesAndExecutesCorrectly(int a, int b, bool expected)
    {
        // Arrange
        var code = $@"
            result <- {a} >= {b}
            Assert.Equal({expected.ToString().ToLower()}, result)
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
    [InlineData(3, 5, true)]
    [InlineData(5, 3, false)]
    [InlineData(5, 5, true)]
    [InlineData(-2, -1, true)]
    [InlineData(0, 0, true)]
    public void LessThanOrEqual_TwoIntegers_CompilesAndExecutesCorrectly(int a, int b, bool expected)
    {
        // Arrange
        var code = $@"
            result <- {a} <= {b}
            Assert.Equal({expected.ToString().ToLower()}, result)
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
    [InlineData(5, 5, true)]
    [InlineData(3, 5, false)]
    [InlineData(5, 3, false)]
    [InlineData(-1, -1, true)]
    [InlineData(0, 0, true)]
    public void Equal_TwoIntegers_CompilesAndExecutesCorrectly(int a, int b, bool expected)
    {
        // Arrange
        var code = $@"
            result <- {a} == {b}
            Assert.Equal({expected.ToString().ToLower()}, result)
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
    [InlineData(5, 5, false)]
    [InlineData(3, 5, true)]
    [InlineData(5, 3, true)]
    [InlineData(-1, -1, false)]
    [InlineData(0, 0, false)]
    public void NotEqual_TwoIntegers_CompilesAndExecutesCorrectly(int a, int b, bool expected)
    {
        // Arrange
        var code = $@"
            result <- {a} != {b}
            Assert.Equal({expected.ToString().ToLower()}, result)
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
    [InlineData(3.14, 3.14, true)]
    [InlineData(1.5, 1.500001, false)]
    [InlineData(-2.5, -2.5, true)]
    public void Equal_TwoDoubles_CompilesAndExecutesCorrectly(double a, double b, bool expected)
    {
        // Arrange
        var aStr = a.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var bStr = b.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var code = $@"
            result <- {aStr} == {bStr}
            Assert.Equal({expected.ToString().ToLower()}, result)
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
    [InlineData("hello", "hello", true)]
    [InlineData("hello", "world", false)]
    [InlineData("", "", true)]
    [InlineData("test", "test ", false)]
    public void Equal_TwoStrings_CompilesAndExecutesCorrectly(string a, string b, bool expected)
    {
        // Arrange
        var escapedA = a.Replace("\"", "\\\"");
        var escapedB = b.Replace("\"", "\\\"");
        var code = $"result <- \"{escapedA}\" == \"{escapedB}\"\nAssert.Equal({expected.ToString().ToLower()}, result)";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 如果 Old8Lang 断言失败会抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    public void Equal_TwoBooleans_CompilesAndExecutesCorrectly(bool a, bool b, bool expected)
    {
        // Arrange
        var code = $@"
            result <- {a.ToString().ToLower()} == {b.ToString().ToLower()}
            Assert.Equal({expected.ToString().ToLower()}, result)
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

            Assert.True(result1)  // 10 < 20 and 20 > 15 = true and true = true
            Assert.True(result2)  // 10 >= 15 or 15 <= 20 = false or true = true
            Assert.False(result3) // 10 != 20 and 20 == 15 = true and false = false
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

            Assert.True(result1)  // 10 + 5 > 10 = 15 > 10 = true
            Assert.True(result2)  // 10 - 5 >= 5 = 5 >= 5 = true
            Assert.True(result3)  // 10 * 5 == 50 = 50 == 50 = true
            Assert.True(result4)  // 10 / 5 <= 2 = 2.0 <= 2 = true
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

            Assert.True(result1)  // 5 < 10 < 15 = true < 15 = true
            Assert.True(result2)  // 5 <= 10 <= 15 = true <= 15 = true
            Assert.True(result3)  // 15 > 10 > 5 = true > 5 = true
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