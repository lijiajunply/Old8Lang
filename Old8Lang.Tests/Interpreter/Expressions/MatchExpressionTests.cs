using Old8Lang.AST.Expression;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;

namespace Old8Lang.Tests.Interpreter.Expressions;

/// <summary>
/// Match 表达式解释模式测试
/// </summary>
public class MatchExpressionTests
{
    [Fact]
    public void Match_ValueMatching_ReturnsCorrectValue()
    {
        // Arrange
        var code = @"
            result <- match 1 {
                case 0 -> ""zero""
                case 1 -> ""one""
                case 2 -> ""two""
                case _ -> ""other""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("one", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Match_VariableBinding_BindsValueCorrectly()
    {
        // Arrange
        var code = @"
            value <- 42
            result <- match value {
                case 0 -> ""zero""
                case x -> ""value is "" + x.ToStr()
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("value is 42", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Match_Wildcard_CatchesAllValues()
    {
        // Arrange
        var code = @"
            result <- match 999 {
                case 1 -> ""one""
                case 2 -> ""two""
                case _ -> ""unknown""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("unknown", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Match_StringMatching_WorksCorrectly()
    {
        // Arrange
        var code = @"
            name <- ""Alice""
            greeting <- match name {
                case ""Bob"" -> ""Hello Bob!""
                case ""Alice"" -> ""Hi Alice!""
                case _ -> ""Hello stranger!""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("greeting"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Hi Alice!", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Match_BooleanMatching_WorksCorrectly()
    {
        // Arrange
        var code = @"
            flag <- true
            result <- match flag {
                case true -> ""yes""
                case false -> ""no""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("yes", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Match_WithExpression_EvaluatesCorrectly()
    {
        // Arrange
        var code = @"
            x <- 5
            y <- 3
            result <- match x + y {
                case 8 -> ""eight""
                case 10 -> ""ten""
                case _ -> ""other""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("eight", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Match_ReturnNumericValue_WorksCorrectly()
    {
        // Arrange
        var code = @"
            input <- 2
            result <- match input {
                case 0 -> 0
                case 1 -> 10
                case 2 -> 20
                case _ -> -1
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(20, ((IntLangValue)result).Value);
    }

    [Fact]
    public void Match_NestedExpression_WorksCorrectly()
    {
        // Arrange
        var code = @"
            value <- 1
            result <- match value {
                case 0 -> ""zero""
                case x -> match x {
                    case 1 -> ""nested one""
                    case _ -> ""nested other""
                }
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("nested one", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Match_NoMatchingCase_ThrowsException()
    {
        // Arrange
        var code = @"
            result <- match 5 {
                case 1 -> ""one""
                case 2 -> ""two""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act & Assert
        var ast = interpreter.Build(code);
        Assert.Throws<InvalidOperationError>(() => ast.Run(interpreter.Manager));
    }

    [Fact]
    public void Match_InFunction_WorksCorrectly()
    {
        // Arrange
        var code = @"
            func getDayName(day:int) -> string {
                result <- match day {
                    case 1 -> ""Monday""
                    case 2 -> ""Tuesday""
                    case 3 -> ""Wednesday""
                    case _ -> ""Unknown""
                }
                return result
            }

            result <- getDayName(2)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("Tuesday", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Match_FirstMatchingCaseExecutes_SubsequentCasesIgnored()
    {
        // Arrange
        var code = @"
            count <- 0
            inc <- () -> { count <- count + 1 }
            result <- match 1 {
                case 1 -> ""first""
                case 1 -> ""second""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("first", ((StringLangValue)result).Value); // 只执行了第一个 case
    }

    [Fact]
    public void Match_VariableBindingDoesNotLeakScope()
    {
        // Arrange
        var code = @"
            outer_x <- 100
            result <- match 42 {
                case x -> ""matched: "" + x.ToStr()
            }
            // outer_x 应该仍然是 100，不应该被绑定的 42 覆盖
            final_x <- outer_x
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        var finalX = interpreter.Manager.GetValue(new LangId("final_x"));

        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("matched: 42", ((StringLangValue)result).Value);

        Assert.NotNull(finalX);
        Assert.IsType<IntLangValue>(finalX);
        Assert.Equal(100, ((IntLangValue)finalX).Value); // outer_x 没有被覆盖
    }

    [Fact]
    public void Match_DoubleValue_WorksCorrectly()
    {
        // Arrange
        var code = @"
            pi <- 3.14
            result <- match pi {
                case 2.71 -> ""e""
                case 3.14 -> ""pi""
                case _ -> ""unknown""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("pi", ((StringLangValue)result).Value);
    }
}
