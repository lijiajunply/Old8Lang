using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Error;
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
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(42, ((IntLangValue)result).Value);
    }

    [Fact]
    public void FalseConditionTernary_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            condition <- false
            result <- condition ? 42 : 24
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(24, ((IntLangValue)result).Value);
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
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(expectedValue, ((IntLangValue)result).Value);
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
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        // a > b = true, so first ternary branch
        // b > c = false, so inner ternary selects 2
        // result should be 2
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(2, ((IntLangValue)result).Value);
    }

    [Fact]
    public void TernaryWithStringResult_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            condition <- true
            result <- condition ? ""yes"" : ""no""
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("yes", ((StringLangValue)result).Value);
    }

    [Fact]
    public void TernaryWithDoubleResult_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            condition <- false
            result <- condition ? 3.14 : 2.71
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
        Assert.Equal(2.71, ((DoubleLangValue)result).Value, 2);
    }

    [Fact]
    public void TernaryWithBooleanResult_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            x <- 5
            y <- 10
            result <- x > y ? true : false
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.False(((BoolLangValue)result).Value); // 5 > 10 = false
    }

    [Fact]
    public void TernaryInExpression_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            base <- 10
            condition <- true
            result <- base + (condition ? 5 : 10)
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
        Assert.Equal(15.0, ((DoubleLangValue)result).Value, 2); // 10 + 5 = 15
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
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        Assert.IsType<IntLangValue>(result1);
        Assert.Equal(2, ((IntLangValue)result1).Value); // 10 > 20 = false

        var result2 = interpreter.Manager.GetValue(new LangId("result2"));
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(4, ((IntLangValue)result2).Value); // 20 > 30 = false

        var result3 = interpreter.Manager.GetValue(new LangId("result3"));
        Assert.IsType<IntLangValue>(result3);
        Assert.Equal(5, ((IntLangValue)result3).Value); // 10 < 30 = true
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
        Assert.Equal(15.0, ((DoubleLangValue)result).Value, 2); // 5 + 10 = 15
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
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        // a > b = true, so first branch
        // c > d = true, so inner ternary selects 1
        // result should be 1
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(1, ((IntLangValue)result).Value);
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
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        // x > y = false, so inner ternary selects false
        // false ? z : y = y = 10
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<DoubleLangValue>(result);
        Assert.Equal(10.0, ((DoubleLangValue)result).Value, 2);
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
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        // i = 0,2,4: i % 2 == 0 = true, add i * 2 (0, 4, 8)
        // i = 1,3: i % 2 == 0 = false, add i (1, 3)
        // sum = 0 + 1 + 4 + 3 + 8 = 16
        var result = interpreter.Manager.GetValue(new LangId("sum"));
        Assert.NotNull(result);
        Assert.IsType<DoubleLangValue>(result);
        Assert.Equal(16.0, ((DoubleLangValue)result).Value, 2);
    }
}