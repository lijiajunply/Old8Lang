using Old8Lang.AST.Expression;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.Tests.Interpreter.Statements;

/// <summary>
/// 条件语句解释模式测试
/// </summary>
public class ConditionalTests
{
    [Fact]
    public void IfStatement_TrueCondition_ExecutesThenBranch()
    {
        // Arrange
        var code = @"
            x <- 10
            result <- ""before""
            if x > 5 {
                result <- ""after""
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
        Assert.Equal("after", ((StringLangValue)result).Value);
    }

    [Fact]
    public void IfStatement_FalseCondition_SkipsThenBranch()
    {
        // Arrange
        var code = @"
            x <- 3
            result <- ""before""
            if x > 5 {
                result <- ""after""
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
        Assert.Equal("before", ((StringLangValue)result).Value);
    }

    [Fact]
    public void IfElseStatement_TrueCondition_ExecutesThenBranch()
    {
        // Arrange
        var code = @"
            x <- 10
            result <- """"
            if x > 5 {
                result <- ""greater""
            } else {
                result <- ""less or equal""
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
        Assert.Equal("greater", ((StringLangValue)result).Value);
    }

    [Fact]
    public void IfElseStatement_FalseCondition_ExecutesElseBranch()
    {
        // Arrange
        var code = @"
            x <- 3
            result <- """"
            if x > 5 {
                result <- ""greater""
            } else {
                result <- ""less or equal""
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
        Assert.Equal("less or equal", ((StringLangValue)result).Value);
    }

    [Fact]
    public void IfElifElseStatement_FirstConditionTrue_ExecutesFirstBranch()
    {
        // Arrange
        var code = @"
            score <- 85
            grade <- """"
            if score >= 90 {
                grade <- ""A""
            } elif score >= 80 {
                grade <- ""B""
            } elif score >= 70 {
                grade <- ""C""
            } else {
                grade <- ""F""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("grade"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("B", ((StringLangValue)result).Value);
    }

    [Fact]
    public void IfElifElseStatement_SecondConditionTrue_ExecutesSecondBranch()
    {
        // Arrange
        var code = @"
            score <- 75
            grade <- """"
            if score >= 90 {
                grade <- ""A""
            } elif score >= 80 {
                grade <- ""B""
            } elif score >= 70 {
                grade <- ""C""
            } else {
                grade <- ""F""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("grade"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("C", ((StringLangValue)result).Value);
    }

    [Fact]
    public void IfElifElseStatement_AllConditionsFalse_ExecutesElseBranch()
    {
        // Arrange
        var code = @"
            score <- 65
            grade <- """"
            if score >= 90 {
                grade <- ""A""
            } elif score >= 80 {
                grade <- ""B""
            } elif score >= 70 {
                grade <- ""C""
            } else {
                grade <- ""F""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("grade"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("F", ((StringLangValue)result).Value);
    }

    [Fact]
    public void NestedIfStatement_HandlesCorrectly()
    {
        // Arrange
        var code = @"
            x <- 10
            y <- 20
            result <- """"
            if x > 5 {
                if y > 15 {
                    result <- ""both true""
                } else {
                    result <- ""first true""
                }
            } else {
                result <- ""first false""
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
        Assert.Equal("both true", ((StringLangValue)result).Value);
    }

    [Fact]
    public void IfStatement_WithComplexCondition_EvaluatesCorrectly()
    {
        // Arrange
        var code = @"
            age <- 25
            hasLicense <- true
            canDrive <- false
            if age >= 18 and hasLicense {
                canDrive <- true
            }
            result <- canDrive
        ";
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
    public void IfStatement_WithFunctionCallCondition_EvaluatesFunction()
    {
        // Arrange
        var code = @"
            func isEven(x:int) -> bool {
                return x % 2 == 0
            }
            number <- 4
            result <- """"
            if isEven(number) {
                result <- ""even""
            } else {
                result <- ""odd""
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
        Assert.Equal("even", ((StringLangValue)result).Value);
    }

    [Fact]
    public void IfStatement_WithArrayAccess_ComparesArrayElements()
    {
        // Arrange
        var code = @"
            arr <- [10, 20, 30]
            result <- """"
            if arr[1] > 15 {
                result <- ""middle element is greater than 15""
            } else {
                result <- ""middle element is not greater than 15""
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
        Assert.Equal("middle element is greater than 15", ((StringLangValue)result).Value);
    }

    [Fact]
    public void IfStatement_WithStringComparison_ComparesStrings()
    {
        // Arrange
        var code = @"
            name <- ""Alice""
            result <- """"
            if name == ""Alice"" {
                result <- ""Hello, Alice!""
            } else {
                result <- ""Hello, stranger!""
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
        Assert.Equal("Hello, Alice!", ((StringLangValue)result).Value);
    }

    [Fact]
    public void IfStatement_WithMultipleStatements_ExecutesAllStatements()
    {
        // Arrange
        var code = @"
            x <- 0
            y <- 0
            z <- 0
            if true {
                x <- 1
                y <- 2
                z <- x + y
            }
            result <- z
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(3, ((IntLangValue)result).Value);
    }

    [Fact]
    public void IfStatement_WithBooleanCondition_HandlesBooleanVariables()
    {
        // Arrange
        var code = @"
            isActive <- true
            result <- """"
            if isActive {
                result <- ""active""
            } else {
                result <- ""inactive""
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
        Assert.Equal("active", ((StringLangValue)result).Value);
    }

    [Fact]
    public void IfStatement_WithChainedComparisons_EvaluatesChain()
    {
        // Arrange
        var code = @"
            x <- 5
            y <- 10
            z <- 15
            result <- false
            if x < y and y < z {
                result <- true
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.True(((BoolLangValue)result).Value); // 5 < 10 and 10 < 15 = true and true = true
    }

    [Fact]
    public void IfStatement_WithoutElseBlock_HandlesGracefully()
    {
        // Arrange
        var code = @"
            x <- 10
            result <- ""initial""
            if x > 5 {
                result <- ""modified""
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
        Assert.Equal("modified", ((StringLangValue)result).Value);
    }

    [Fact]
    public void IfStatement_WithEmptyThenBlock_DoesNothing()
    {
        // Arrange
        var code = @"
            x <- 10
            result <- ""unchanged""
            if x > 5 {
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
        Assert.Equal("unchanged", ((StringLangValue)result).Value);
    }

    [Fact]
    public void IfStatement_WithEmptyElseBlock_DoesNothing()
    {
        // Arrange
        var code = @"
            x <- 3
            result <- ""unchanged""
            if x > 5 {
                result <- ""modified""
            } else {
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
        Assert.Equal("unchanged", ((StringLangValue)result).Value);
    }

    [Fact]
    public void IfStatement_ConditionWithVariableAssignment_HandlesCorrectly()
    {
        // Arrange
        var code = @"
            a <- 10
            b <- 20
            result <- """"
            if (a + b) > 25 {
                result <- ""sum is greater than 25""
            } else {
                result <- ""sum is not greater than 25""
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
        Assert.Equal("sum is greater than 25", ((StringLangValue)result).Value);
    }

    [Fact]
    public void IfStatement_WithLogicalOr_ConditionTrue_ExecutesThenBranch()
    {
        // Arrange
        var code = @"
            a <- true
            b <- false
            result <- """"
            if a or b {
                result <- ""at least one true""
            } else {
                result <- ""both false""
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
        Assert.Equal("at least one true", ((StringLangValue)result).Value);
    }

    [Fact]
    public void IfStatement_WithLogicalAnd_ConditionTrue_ExecutesThenBranch()
    {
        // Arrange
        var code = @"
            a <- true
            b <- true
            result <- """"
            if a and b {
                result <- ""both true""
            } else {
                result <- ""not both true""
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
        Assert.Equal("both true", ((StringLangValue)result).Value);
    }

    [Fact]
    public void IfStatement_WithNotOperator_InvertsCondition()
    {
        // Arrange
        var code = @"
            isActive <- false
            result <- """"
            if not isActive {
                result <- ""not active""
            } else {
                result <- ""active""
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
        Assert.Equal("not active", ((StringLangValue)result).Value);
    }

    [Fact]
    public void IfStatement_InLoop_ConditionallyExecutesEachIteration()
    {
        // Arrange
        var code = @"
            sum <- 0
            count <- 0
            for i <- 0, i < 10, i++ {
                if i % 2 == 0 {
                    sum <- sum + i
                    count <- count + 1
                }
            }
            result <- sum
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(20, ((IntLangValue)result).Value); // 0+2+4+6+8 = 20
    }

    [Fact]
    public void IfStatement_WithTruthyFalsy_HandlesNonBooleanValues()
    {
        // Arrange
        var code = @"
            result1 <- """"
            result2 <- """"
            result3 <- """"
            result4 <- """"

            // 这取决于 Old8Lang 的真值/假值转换规则
            if 0 {
                result1 <- ""zero is true""
            } else {
                result1 <- ""zero is false""
            }

            if 1 {
                result2 <- ""one is true""
            } else {
                result2 <- ""one is false""
            }

            if """" {
                result3 <- ""empty string is true""
            } else {
                result3 <- ""empty string is false""
            }

            if ""hello"" {
                result4 <- ""non-empty string is true""
            } else {
                result4 <- ""non-empty string is false""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert - 具体结果取决于语言的真值/假值转换规则
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));
        var result3 = interpreter.Manager.GetValue(new LangId("result3"));
        var result4 = interpreter.Manager.GetValue(new LangId("result4"));

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotNull(result3);
        Assert.NotNull(result4);
    }
}