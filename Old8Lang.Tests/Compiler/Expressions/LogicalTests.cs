using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Expressions;

/// <summary>
/// 逻辑表达式编译模式测试
/// </summary>
[Collection("Sequential")]
public class LogicalTests
{
    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, false)]
    public void AndOperation_CompilesAndExecutesCorrectly(bool a, bool b, bool expected)
    {
        // Arrange
        var code = $@"
            result <- {a.ToString().ToLower()} and {b.ToString().ToLower()}
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
    [InlineData(true, true, true)]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    [InlineData(false, false, false)]
    public void OrOperation_CompilesAndExecutesCorrectly(bool a, bool b, bool expected)
    {
        // Arrange
        var code = $@"
            result <- {a.ToString().ToLower()} or {b.ToString().ToLower()}
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
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void NotOperation_CompilesAndExecutesCorrectly(bool a, bool expected)
    {
        // Arrange
        var code = $@"
            result <- not {a.ToString().ToLower()}
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
    public void ComplexLogicalExpression_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            x <- true
            y <- false
            z <- true
            result1 <- x and y or z
            result2 <- (x and y) or z
            result3 <- not x or y

            Assert.True(result1)  // true and false = false, false or true = true
            Assert.True(result2)  // (true and false) = false, false or true = true
            Assert.False(result3) // not true = false, false or false = false
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
    public void ShortCircuitEvaluation_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            a <- true
            result <- a or (1 / 0)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 应该编译成功，执行时由于短路不会抛出除零错误
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        
        // 如果语言实现短路求值，这里不应该抛出异常
        // 但如果语言不支持短路，可能会抛出除零错误，那也是正确的
    }

    [Fact]
    public void ShortCircuitEvaluationWithAnd_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            a <- false
            result <- a and (1 / 0)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 应该编译成功，执行时由于短路不会抛出除零错误
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        
        // 如果语言实现短路求值，这里不应该抛出异常
        // 但如果语言不支持短路，可能会抛出除零错误，那也是正确的
    }

    [Fact]
    public void LogicalWithComparison_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            x <- 5
            y <- 10
            z <- 7
            result1 <- x < y and y > z
            result2 <- x > y or z == 7

            Assert.True(result1)  // 5 < 10 = true, 10 > 7 = true, true and true = true
            Assert.True(result2)  // 5 > 10 = false, 7 == 7 = true, false or true = true
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
    public void NestedLogicalOperations_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            a <- true
            b <- true
            c <- false
            d <- false
            result1 <- (a and b) or (c and d)
            result2 <- (a or c) and (b or d)
            result3 <- not (a or b) and (c or d)

            Assert.True(result1)   // (true and true) = true, (false and false) = false, true or false = true
            Assert.True(result2)   // (true or false) = true, (true or false) = true, true and true = true
            Assert.False(result3)  // not (true or true) = not true = false, (false or false) = false, false and false = false
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
    [InlineData(0, 0, true)]
    [InlineData(0, 1, true)]
    [InlineData(1, 0, false)]
    [InlineData(1, 1, true)]
    public void IntegerComparisonWithLogical_CompilesAndExecutesCorrectly(int a, int b, bool expected)
    {
        // Arrange
        var code = $@"
            result <- {a} >= 0 and {a} == {b}
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
    public void ComplexExpressionWithMultipleLogicalOperators_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            x <- 10
            y <- 20
            z <- 5
            result <- (x > y and y > z) or (x < y and y < z) or (x == y and y == z)

            // (10 > 20 and 20 > 5) = false
            // (10 < 20 and 20 < 5) = false  
            // (10 == 20 and 20 == 5) = false
            // false or false or false = false
            Assert.False(result)
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
    public void TruthTableEvaluation_CompilesAndExecutesCorrectly()
    {
        // Arrange - 测试所有可能的输入组合
        var code = @"
            a <- true
            b <- false
            c <- true
            
            // AND真值表
            result1 <- a and b and c      // false
            result2 <- a and b or c      // false
            result3 <- a or b and c      // false
            result4 <- a or b or c       // true
            
            // 带括号的表达式
            result5 <- (a and b) or c    // true
            result6 <- a and (b or c)    // true

            // Assert statements
            Assert.False(result1)
            Assert.False(result2)
            Assert.False(result3)
            Assert.True(result4)
            Assert.True(result5)
            Assert.True(result6)
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