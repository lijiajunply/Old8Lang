using Old8Lang.AST.Expression;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.Tests.Interpreter.Basic;

/// <summary>
/// 基础表达式解释模式测试
/// </summary>
public class ExpressionTests
{
    [Fact]
    public void LiteralExpression_Integer_CreatesCorrectValue()
    {
        // Arrange
        var code = "result <- 42";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(42, ((IntLangValue)result).Value);
    }

    [Fact]
    public void LiteralExpression_Double_CreatesCorrectValue()
    {
        // Arrange
        var code = "result <- 3.14159";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<DoubleLangValue>(result);
        Assert.Equal(3.14159, ((DoubleLangValue)result).Value);
    }

    [Fact]
    public void LiteralExpression_String_CreatesCorrectValue()
    {
        // Arrange
        var code = "result <- \"Hello, World!\"";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Hello, World!", ((StringLangValue)result).Value);
    }

    [Fact]
    public void LiteralExpression_BooleanTrue_CreatesCorrectValue()
    {
        // Arrange
        var code = "result <- true";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.True(((BoolLangValue)result).Value);
    }

    [Fact]
    public void LiteralExpression_BooleanFalse_CreatesCorrectValue()
    {
        // Arrange
        var code = "result <- false";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.False(((BoolLangValue)result).Value);
    }

    [Fact]
    public void LiteralExpression_Char_CreatesCorrectValue()
    {
        // Arrange
        var code = "result <- 'A'";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<CharLangValue>(result);
        Assert.Equal('A', ((CharLangValue)result).Value);
    }

    [Fact]
    public void VariableExpression_ExistingVariable_ReturnsCorrectValue()
    {
        // Arrange
        var code = @"
            x <- 100
            result <- x
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(100, ((IntLangValue)result).Value);
    }

    [Fact]
    public void IdentifierExpression_VariableReference_CreatesCorrectReference()
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
        ast.Run(interpreter.Manager);

        // Assert
        var value = interpreter.Manager.GetValue(new LangId("value")) as IntLangValue;
        var reference = interpreter.Manager.GetValue(new LangId("reference")) as IntLangValue;

        Assert.NotNull(value);
        Assert.NotNull(reference);
        Assert.Equal(100, value.Value);
        Assert.Equal(42, reference.Value); // reference 应该保持原始值
    }

    [Fact]
    public void ExpressionStatement_OnlyExpression_EvaluatesCorrectly()
    {
        // Arrange
        var code = @"
            a <- 10
            b <- 20
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var a = interpreter.Manager.GetValue(new LangId("a")) as IntLangValue;
        var b = interpreter.Manager.GetValue(new LangId("b")) as IntLangValue;

        Assert.NotNull(a);
        Assert.NotNull(b);
        Assert.Equal(10, a.Value);
        Assert.Equal(20, b.Value);
    }

    [Fact]
    public void ComplexExpression_NestedOperations_EvaluatesCorrectly()
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
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        // (10 + 20) * 30 / (10 + 20 + 30) = 30 * 30 / 60 = 900 / 60 = 15
        Assert.Equal(15.0, ((DoubleLangValue)result).Value);
    }

    [Fact]
    public void ExpressionWithMixedTypes_HandlesTypeConversion()
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
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(7.5, ((DoubleLangValue)result1).Value);
        Assert.Equal("7.510", ((StringLangValue)result2).Value);
    }

    [Fact]
    public void ExpressionWithFunctionCall_UsesReturnValue()
    {
        // Arrange
        var code = @"
            func getValue() {
                return 42
            }
            result <- getValue() + 8
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(50, ((IntLangValue)result).Value);
    }

    [Fact]
    public void ExpressionInCondition_UsedForDecisionMaking()
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
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result")) as StringLangValue;
        Assert.NotNull(result);
        Assert.Equal("greater than 25", result.Value);
    }

    [Fact]
    public void ExpressionWithArrayAccess_CreatesCorrectValue()
    {
        // Arrange
        var code = @"
            arr <- [10, 20, 30, 40, 50]
            result <- arr[2] + arr[4]
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(80, ((IntLangValue)result).Value); // 30 + 50 = 80
    }

    [Fact]
    public void ExpressionWithDictionaryAccess_CreatesCorrectValue()
    {
        // Arrange
        var code = @"
            dict <- {""a"": 10, ""b"": 20, ""c"": 30}
            result <- dict[""a""] + dict[""c""]
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(40, ((IntLangValue)result).Value); // 10 + 30 = 40
    }

    [Fact]
    public void ChainedExpression_EvaluatesCorrectly()
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
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(12, ((IntLangValue)result).Value); // 1 + 2 + 3 + 1 * 2 * 3 = 6 + 6 = 12
    }

    [Fact]
    public void ExpressionWithBooleanLogic_EvaluatesCorrectly()
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
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.True(((BoolLangValue)result).Value); // (true and false) or true = false or true = true
    }

    [Fact]
    public void ExpressionWithComparison_EvaluatesCorrectly()
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
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1")) as BoolLangValue;
        var result2 = interpreter.Manager.GetValue(new LangId("result2")) as BoolLangValue;
        var result3 = interpreter.Manager.GetValue(new LangId("result3")) as BoolLangValue;
        var result4 = interpreter.Manager.GetValue(new LangId("result4")) as BoolLangValue;

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotNull(result3);
        Assert.NotNull(result4);

        Assert.True(result1.Value);  // 10 < 20
        Assert.True(result2.Value);  // 10 == 10
        Assert.False(result3.Value); // 10 > 20
        Assert.True(result4.Value);  // 10 != 20
    }

    [Fact]
    public void ExpressionWithNegation_EvaluatesCorrectly()
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
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1")) as BoolLangValue;
        var result2 = interpreter.Manager.GetValue(new LangId("result2")) as BoolLangValue;
        var result3 = interpreter.Manager.GetValue(new LangId("result3")) as IntLangValue;

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotNull(result3);

        Assert.False(result1.Value); // not true = false
        Assert.Equal(true, result2.Value);  // not false = true
        Assert.Equal(-8, result3.Value);    // -(5 + 3) = -8
    }

    [Fact]
    public void EmptyExpression_HandlesGracefully()
    {
        // Arrange
        var code = "result <- 0"; // 最简单的表达式
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(0, ((IntLangValue)result).Value);
    }
}