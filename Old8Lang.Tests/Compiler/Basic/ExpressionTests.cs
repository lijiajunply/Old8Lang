using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Basic;

/// <summary>
/// 基础表达式编译模式测试
/// 测试编译器模式下各类表达式的 IL 生成和执行
/// </summary>
[Collection("Sequential")]
public class ExpressionTests
{
    #region 字面量表达式测试

    [Fact]
    public void LiteralExpression_Integer_CompilesCorrectly()
    {
        // Arrange
        var code = "result <- 42";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void LiteralExpression_Double_CompilesCorrectly()
    {
        // Arrange
        var code = "result <- 3.14159";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void LiteralExpression_String_CompilesCorrectly()
    {
        // Arrange
        var code = "result <- \"Hello, World!\"";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void LiteralExpression_Boolean_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            result1 <- true
            result2 <- false
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void LiteralExpression_Char_CompilesCorrectly()
    {
        // Arrange
        var code = "result <- 'A'";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region 一元运算符表达式测试

    [Fact]
    public void UnaryExpression_NotOperator_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            func main() -> int {
                a <- true
                b <- !a
                if b == false {
                    return 0
                } else {
                    return 1
                }
            }

            result <- main()";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void UnaryExpression_DoubleNotOperator_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            func main() -> int {
                a <- true
                b <- !!a
                if b == true {
                    return 0
                } else {
                    return 1
                }
            }

            result <- main()";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void UnaryExpression_NotOnComparison_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            func main() -> int {
                e <- !(10 > 5)
                if e == false {
                    return 0
                } else {
                    return 1
                }
            }

            result <- main()";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void UnaryExpression_MinusOperator_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            func main() -> int {
                c <- 5
                d <- -c
                if d == -5 {
                    return 0
                } else {
                    return 1
                }
            }

            result <- main()";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region 变量表达式测试

    [Fact]
    public void VariableExpression_ExistingVariable_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            x <- 100
            result <- x
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void IdentifierExpression_VariableReference_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            value <- 42
            reference <- value
            value <- 100
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region 算术表达式测试

    [Fact]
    public void ArithmeticExpression_Addition_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            a <- 10
            b <- 20
            result <- a + b
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ArithmeticExpression_Subtraction_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            a <- 50
            b <- 30
            result <- a - b
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ArithmeticExpression_Multiplication_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            a <- 6
            b <- 7
            result <- a * b
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ArithmeticExpression_Division_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            a <- 100
            b <- 4
            result <- a / b
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ComplexExpression_NestedOperations_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            a <- 10.0
            b <- 20
            c <- 30
            result <- (a + b) * c / (a + b + c)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ChainedExpression_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            x <- 1
            y <- 2
            z <- 3
            result <- x + y + z + x * y * z
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region 比较表达式测试

    [Fact]
    public void ComparisonExpression_AllOperators_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            a <- 10
            b <- 20
            c <- 10
            result1 <- a < b
            result2 <- a == c
            result3 <- a > b
            result4 <- a != b
            result5 <- a <= c
            result6 <- b >= a
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region 逻辑表达式测试

    [Fact]
    public void LogicalExpression_BooleanLogic_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            a <- true
            b <- false
            c <- true
            result <- a and b or c
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void NegationExpression_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            a <- true
            b <- false
            result1 <- not a
            result2 <- not b
            result3 <- -(5 + 3)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region 类型转换测试

    [Fact]
    public void ExpressionWithMixedTypes_CompilesAndHandlesTypeConversion()
    {
        // Arrange
        var code = @"
            intVal <- 5
            doubleVal <- 2.5
            stringVal <- ""10""
            result1 <- intVal + doubleVal
            result2 <- intVal + doubleVal + stringVal
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region 条件表达式测试

    [Fact]
    public void ExpressionInCondition_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            a <- 10
            b <- 20
            result <- """"
            if a + b > 25 {
                result <- ""greater than 25""
            } else {
                result <- ""less than or equal to 25""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region 集合访问表达式测试

    [Fact]
    public void ExpressionWithArrayAccess_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            arr <- [10, 20, 30, 40, 50]
            result <- arr[2] + arr[4]
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ExpressionWithDictionaryAccess_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            dict <- {""a"": 10, ""b"": 20, ""c"": 30}
            result <- dict[""a""] + dict[""c""]
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion
}
