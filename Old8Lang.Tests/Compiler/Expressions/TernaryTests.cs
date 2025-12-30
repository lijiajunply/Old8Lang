using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Expressions;

/// <summary>
/// 三元运算符编译模式测试
/// </summary>
[Collection("Sequential")]
public class TernaryTests
{
    [Fact]
    public void SimpleTernary_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            condition <- true
            result <- condition ? 42 : 24
            Assert.Equal(42, result)
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
    public void FalseConditionTernary_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            condition <- false
            result <- condition ? 42 : 24
            Assert.Equal(24, result)
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
    [InlineData(0, true, true)]
    [InlineData(0, false, false)]
    [InlineData(1, true, true)]
    [InlineData(1, false, false)]
    public void ComparisonConditionTernary_CompilesAndExecutesCorrectly(int value, bool condition, bool expected)
    {
        // Arrange
        var expectedValue = condition ? 1 : 0;
        var code = $@"
            value <- {value}
            result <- value > 0 ? {expectedValue} : {-expectedValue}
            Assert.Equal({expectedValue}, result)
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
    public void NestedTernary_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            a <- 10
            b <- 5
            c <- 15
            result <- a > b ? (b > c ? 1 : 2) : (c > a ? 3 : 4)
            Assert.Equal(2, result)  // a > b = true, b > c = false, so result = 2
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
    public void TernaryWithStringResult_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            condition <- true
            result <- condition ? ""yes"" : ""no""
            Assert.Equal(""yes"", result)
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
    public void TernaryWithDoubleResult_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            condition <- false
            result <- condition ? 3.14 : 2.71
            Assert.Equal(2.71, result)
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
    public void TernaryWithBooleanResult_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            x <- 5
            y <- 10
            result <- x > y ? true : false
            Assert.False(result)  // 5 > 10 = false
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
    public void TernaryInExpression_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            base <- 10
            condition <- true
            result <- base + (condition ? 5 : 10)
            Assert.Equal(15.0, result)  // 10 + 5 = 15
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
    public void MultipleTernaryOperations_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            x <- 10
            y <- 20
            z <- 30
            
            result1 <- x > y ? 1 : 2
            result2 <- y > z ? 3 : 4
            result3 <- x < z ? 5 : 6

            Assert.Equal(2, result1)  // 10 > 20 = false
            Assert.Equal(4, result2)  // 20 > 30 = false
            Assert.Equal(5, result3)  // 10 < 30 = true
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
    public void TernaryWithFunctionCalls_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func getValue(a:int, b:int):int {
                return a + b
            }
            
            condition <- true
            result <- condition ? getValue(5, 10) : getValue(1, 2)
            Assert.Equal(15.0, result)  // 5 + 10 = 15
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
    public void ComplexTernaryExpression_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            a <- 10
            b <- 5
            c <- 20
            d <- 15
            
            result <- (a > b) ? ((c > d) ? 1 : 2) : ((b > a) ? 3 : 4)
            Assert.Equal(1, result)  // a > b = true, c > d = true, so result = 1
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
    public void TernaryAsCondition_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            x <- 5
            y <- 10
            z <- 15
            
            result <- (x > y ? true : false) ? z : y
            Assert.Equal(10.0, result)  // x > y = false, so result = y = 10
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
    public void TernaryInLoop_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            sum <- 0
            for i <- 0, i < 5, i++ {
                sum <- sum + (i % 2 == 0 ? i * 2 : i)
            }
            // i = 0,2,4: i % 2 == 0 = true, add i * 2 (0, 4, 8)
            // i = 1,3: i % 2 == 0 = false, add i (1, 3)
            // sum = 0 + 1 + 4 + 3 + 8 = 16
            Assert.Equal(16.0, sum)
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