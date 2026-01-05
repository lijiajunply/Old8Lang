using Old8Lang.AST.Expression;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.Tests.Interpreter.Expressions;

/// <summary>
/// Match 表达式增强功能测试
/// 测试元组解构、类型匹配、守卫条件和范围匹配
/// </summary>
public class MatchExpressionEnhancedTests
{
    #region 元组解构匹配测试

    [Fact]
    public void Match_TupleDeconstruction_MatchesOrigin()
    {
        // Arrange
        var code = @"
            point <- (0, 0)
            result <- match point {
                case (0, 0) -> ""Origin""
                case (x, 0) -> ""X-axis""
                case (0, y) -> ""Y-axis""
                case (x, y) -> ""Other""
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
        Assert.Equal("Origin", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Match_TupleDeconstruction_MatchesXAxis()
    {
        // Arrange
        var code = @"
            point <- (5, 0)
            result <- match point {
                case (0, 0) -> ""Origin""
                case (x, 0) -> ""On X-axis: "" + x.ToStr()
                case (0, y) -> ""On Y-axis: "" + y.ToStr()
                case (x, y) -> ""Point""
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
        Assert.Equal("On X-axis: 5", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Match_TupleDeconstruction_MatchesYAxis()
    {
        // Arrange
        var code = @"
            point <- (0, 3)
            result <- match point {
                case (0, 0) -> ""Origin""
                case (x, 0) -> ""On X-axis""
                case (0, y) -> ""On Y-axis: "" + y.ToStr()
                case (x, y) -> ""Point""
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
        Assert.Equal("On Y-axis: 3", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Match_TupleDeconstruction_MatchesGenericPoint()
    {
        // Arrange
        var code = @"
            point <- (2, 4)
            result <- match point {
                case (0, 0) -> ""Origin""
                case (x, 0) -> ""X-axis""
                case (0, y) -> ""Y-axis""
                case (x, y) -> x.ToStr() + "","" + y.ToStr()
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
        Assert.Equal("2,4", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Match_TupleDeconstruction_WithWildcard()
    {
        // Arrange
        var code = @"
            point <- (10, 20)
            result <- match point {
                case (0, 0) -> ""Origin""
                case (_, 0) -> ""On X-axis""
                case (0, _) -> ""On Y-axis""
                case (_, _) -> ""Other point""
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
        Assert.Equal("Other point", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Match_TupleDeconstruction_ThreeElements()
    {
        // Arrange
        var code = @"
            triple <- (1, 2, 3)
            result <- match triple {
                case (0, 0, 0) -> ""Origin""
                case (x, y, z) -> x.ToStr() + "","" + y.ToStr() + "","" + z.ToStr()
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
        Assert.Equal("1,2,3", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Match_TupleDeconstruction_VariablesDoNotLeakScope()
    {
        // Arrange
        var code = @"
            x <- 100
            y <- 200
            point <- (5, 10)
            result <- match point {
                case (x, y) -> x + y
            }
            final_x <- x
            final_y <- y
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        var finalX = interpreter.Manager.GetValue(new LangId("final_x"));
        var finalY = interpreter.Manager.GetValue(new LangId("final_y"));

        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(15, ((IntLangValue)result).Value); // 5 + 10

        // 外部变量不应该被覆盖
        Assert.NotNull(finalX);
        Assert.IsType<IntLangValue>(finalX);
        Assert.Equal(100, ((IntLangValue)finalX).Value);

        Assert.NotNull(finalY);
        Assert.IsType<IntLangValue>(finalY);
        Assert.Equal(200, ((IntLangValue)finalY).Value);
    }

    #endregion

    #region 类型匹配测试

    [Fact]
    public void Match_TypeMatch_MatchesIntType()
    {
        // Arrange
        var code = @"
            value:int <- 42
            result <- match value {
                case x:int -> ""Integer: "" + x.ToStr()
                case s:string -> ""String""
                case _ -> ""Other""
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
        Assert.Equal("Integer: 42", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Match_TypeMatch_MatchesStringType()
    {
        // Arrange
        var code = @"
            value <- ""hello""
            result <- match value {
                case x:int -> ""Integer""
                case s:string -> ""String: "" + s
                case _ -> ""Other""
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
        Assert.Equal("String: hello", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Match_TypeMatch_MatchesDoubleType()
    {
        // Arrange
        var code = @"
            value <- 3.14
            result <- match value {
                case x:int -> ""Integer""
                case d:double -> ""Double: "" + d.ToStr()
                case _ -> ""Other""
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
        Assert.Equal("Double: 3.14", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Match_TypeMatch_MatchesBoolType()
    {
        // Arrange
        var code = @"
            value <- true
            result <- match value {
                case x:int -> ""Integer""
                case b:bool -> ""Boolean: "" + b.ToStr()
                case _ -> ""Other""
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
        Assert.Equal("Boolean: true", ((StringLangValue)result).Value);
    }

    #endregion

    #region 守卫条件测试

    [Fact]
    public void Match_Guard_PositiveInteger()
    {
        // Arrange
        var code = @"
            value:int <- 42
            result <- match value {
                case x:int if x > 0 -> ""Positive""
                case x:int if x < 0 -> ""Negative""
                case x:int -> ""Zero""
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
        Assert.Equal("Positive", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Match_Guard_NegativeInteger()
    {
        // Arrange
        var code = @"
            value:int <- -10
            result <- match value {
                case x:int if x > 0 -> ""Positive""
                case x:int if x < 0 -> ""Negative""
                case x:int -> ""Zero""
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
        Assert.Equal("Negative", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Match_Guard_ZeroInteger()
    {
        // Arrange
        var code = @"
            value:int <- 0
            result <- match value {
                case x:int if x > 0 -> ""Positive""
                case x:int if x < 0 -> ""Negative""
                case x:int -> ""Zero""
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
        Assert.Equal("Zero", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Match_Guard_ComplexCondition()
    {
        // Arrange
        var code = @"
            value:int <- 15
            result <- match value {
                case x:int if x >= 0 and x <= 10 -> ""Low""
                case x:int if x >= 11 and x <= 20 -> ""Medium""
                case x:int if x >= 21 and x <= 30 -> ""High""
                case _ -> ""Out of range""
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
        Assert.Equal("Medium", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Match_Guard_WithVariableInCondition()
    {
        // Arrange
        var code = @"
            threshold <- 50
            value:int <- 75
            result <- match value {
                case x:int if x > threshold -> ""Above threshold""
                case x:int if x < threshold -> ""Below threshold""
                case _ -> ""At threshold""
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
        Assert.Equal("Above threshold", ((StringLangValue)result).Value);
    }

    #endregion

    #region 范围匹配测试

    [Fact]
    public void Match_Range_MatchesChild()
    {
        // Arrange
        var code = @"
            age <- 8
            result <- match age {
                case [0~12] -> ""Child""
                case [13~19] -> ""Teen""
                case [20~64] -> ""Adult""
                case [65~90] -> ""Senior""
                default -> ""Invalid""
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
        Assert.Equal("Child", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Match_Range_MatchesTeen()
    {
        // Arrange
        var code = @"
            age <- 16
            result <- match age {
                case [0~12] -> ""Child""
                case [13~19] -> ""Teen""
                case [20~64] -> ""Adult""
                default -> ""Other""
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
        Assert.Equal("Teen", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Match_Range_MatchesAdult()
    {
        // Arrange
        var code = @"
            age <- 35
            result <- match age {
                case [0~12] -> ""Child""
                case [13~19] -> ""Teen""
                case [20~64] -> ""Adult""
                default -> ""Other""
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
        Assert.Equal("Adult", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Match_Range_BoundaryInclusive()
    {
        // Arrange
        var code = @"
            value <- 10
            result <- match value {
                case [0~10] -> ""In range""
                default -> ""Out of range""
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
        Assert.Equal("In range", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Match_Range_ExcludeEnd()
    {
        // Arrange
        var code = @"
            value <- 10
            result <- match value {
                case [0~<10] -> ""In range""
                default -> ""Out of range""
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
        Assert.Equal("Out of range", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Match_Range_ExcludeStart()
    {
        // Arrange
        var code = @"
            value <- 0
            result <- match value {
                case [0>~10] -> ""In range""
                default -> ""Out of range""
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
        Assert.Equal("Out of range", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Match_Range_ExcludeBoth()
    {
        // Arrange
        var code = @"
            value <- 5
            result <- match value {
                case [0>~<10] -> ""In range""
                default -> ""Out of range""
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
        Assert.Equal("In range", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Match_Range_WithDoubleValues()
    {
        // Arrange
        var code = @"
            score <- 85.5
            result <- match score {
                case [0.0~60.0] -> ""F""
                case [60.0~70.0] -> ""D""
                case [70.0~80.0] -> ""C""
                case [80.0~90.0] -> ""B""
                case [90.0~100.0] -> ""A""
                default -> ""Invalid""
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
        Assert.Equal("B", ((StringLangValue)result).Value);
    }

    #endregion

    #region Default 分支测试

    [Fact]
    public void Match_DefaultBranch_MatchesWhenNoCaseMatches()
    {
        // Arrange
        var code = @"
            value <- 100
            result <- match value {
                case 1 -> ""one""
                case 2 -> ""two""
                default -> ""unknown""
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

    #endregion

    #region 混合模式测试

    [Fact]
    public void Match_Mixed_TupleAndGuard()
    {
        // Arrange - 注意：目前元组模式不支持直接类型注解，需要分步
        var code = @"
            point <- (5, 10)
            result <- match point {
                case (0, 0) -> ""Origin""
                case (x, y) -> if x > 0 and y > 0 then ""First quadrant"" else ""Other""
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
        Assert.Equal("First quadrant", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Match_Mixed_TypeMatchWithMultipleTypes()
    {
        // Arrange
        var code = @"
            val1 <- 42
            val2 <- ""hello""
            val3 <- 3.14
            val4 <- true

            result1 <- match val1 {
                case x:int -> ""int""
                case s:string -> ""string""
                case d:double -> ""double""
                case b:bool -> ""bool""
                default -> ""unknown""
            }

            result2 <- match val2 {
                case x:int -> ""int""
                case s:string -> ""string""
                case d:double -> ""double""
                case b:bool -> ""bool""
                default -> ""unknown""
            }

            result3 <- match val3 {
                case x:int -> ""int""
                case s:string -> ""string""
                case d:double -> ""double""
                case b:bool -> ""bool""
                default -> ""unknown""
            }

            result4 <- match val4 {
                case x:int -> ""int""
                case s:string -> ""string""
                case d:double -> ""double""
                case b:bool -> ""bool""
                default -> ""unknown""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));
        var result3 = interpreter.Manager.GetValue(new LangId("result3"));
        var result4 = interpreter.Manager.GetValue(new LangId("result4"));

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotNull(result3);
        Assert.NotNull(result4);

        Assert.Equal("int", ((StringLangValue)result1).Value);
        Assert.Equal("string", ((StringLangValue)result2).Value);
        Assert.Equal("double", ((StringLangValue)result3).Value);
        Assert.Equal("bool", ((StringLangValue)result4).Value);
    }

    #endregion
}
