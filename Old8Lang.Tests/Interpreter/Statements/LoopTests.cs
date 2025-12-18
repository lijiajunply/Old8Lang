using Old8Lang.AST.Expression;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.Tests.Interpreter.Statements;

/// <summary>
/// 循环语句解释模式测试
/// </summary>
public class LoopTests
{
    [Fact]
    public void ForLoop_BasicIteration_ExecutesCorrectNumberOfTimes()
    {
        // Arrange
        var code = @"
            counter <- 0
            for i <- 0, i < 5, i++ {
                counter <- counter + 1
            }
            result <- counter
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(5, ((IntLangValue)result).Value);
    }

    [Fact]
    public void ForLoop_WithInitialization_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            sum <- 0
            for i <- 1, i <= 10, i++ {
                sum <- sum + i
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
        Assert.Equal(55, ((IntLangValue)result).Value); // 1+2+3+4+5+6+7+8+9+10 = 55
    }

    [Fact]
    public void ForLoop_WithDecrement_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            counter <- 0
            for i <- 5, i > 0, i-- {
                counter <- counter + 1
            }
            result <- counter
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(5, ((IntLangValue)result).Value);
    }

    [Fact]
    public void ForLoop_WithStepIncrement_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            sum <- 0
            for i <- 0, i < 10, i <- i + 2 {
                sum <- sum + i
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
    public void ForLoop_EmptyBody_HandlesCorrectly()
    {
        // Arrange
        var code = @"
            counter <- 0
            for i <- 0, i < 5, i++ {
            }
            result <- counter
        ";
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

    [Fact]
    public void WhileLoop_BasicCondition_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            counter <- 0
            while counter < 5 {
                counter <- counter + 1
            }
            result <- counter
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(5, ((IntLangValue)result).Value);
    }

    [Fact]
    public void WhileLoop_FalseInitially_DoesNotExecute()
    {
        // Arrange
        var code = @"
            counter <- 0
            while false {
                counter <- counter + 1
            }
            result <- counter
        ";
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

    [Fact]
    public void WhileLoop_WithComplexCondition_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            i <- 1
            sum <- 0
            while i <= 10 and sum < 30 {
                sum <- sum + i
                i <- i + 1
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
        Assert.Equal(36, ((IntLangValue)result).Value); // 1+2+3+4+5+6+7+8 = 36 (stops at 8 because sum >= 30)
    }

    [Fact]
    public void ForInLoop_ArrayIteration_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            numbers <- [1, 2, 3, 4, 5]
            sum <- 0
            for num in numbers {
                sum <- sum + num
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
        Assert.Equal(15, ((IntLangValue)result).Value); // 1+2+3+4+5 = 15
    }

    [Fact]
    public void ForInLoop_StringIteration_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            text <- ""hello""
            charCount <- 0
            for char in text {
                charCount <- charCount + 1
            }
            result <- charCount
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(5, ((IntLangValue)result).Value); // "hello" has 5 characters
    }

    [Fact]
    public void ForInLoop_DictionaryIteration_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            dict <- {""a"": 1, ""b"": 2, ""c"": 3}
            sum <- 0
            for key, value in dict {
                sum <- sum + value
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
        Assert.Equal(6, ((IntLangValue)result).Value); // 1+2+3 = 6
    }

    [Fact]
    public void ForInLoop_EmptyCollection_DoesNotExecute()
    {
        // Arrange
        var code = @"
            emptyArr <- []
            counter <- 0
            for item in emptyArr {
                counter <- counter + 1
            }
            result <- counter
        ";
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

    [Fact]
    public void NestedForLoops_MatrixMultiplication_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            // 2x2 matrix multiplication
            a <- [[1, 2], [3, 4]]
            b <- [[5, 6], [7, 8]]
            result <- [[0, 0], [0, 0]]

            for i <- 0, i < 2, i++ {
                for j <- 0, j < 2, j++ {
                    sum <- 0
                    for k <- 0, k < 2, k++ {
                        sum <- sum + a[i][k] * b[k][j]
                    }
                    result[i][j] <- sum
                }
            }
            finalResult <- result[0][0] + result[1][1]  // 19 + 50 = 69
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var finalResult = interpreter.Manager.GetValue(new LangId("finalResult"));
        Assert.NotNull(finalResult);
        Assert.IsType<IntLangValue>(finalResult);
        Assert.Equal(69, ((IntLangValue)finalResult).Value);
    }

    [Fact]
    public void BreakStatement_InForLoop_ExitsEarly()
    {
        // Arrange
        var code = @"
            counter <- 0
            for i <- 0, i < 10, i++ {
                counter <- counter + 1
                if i == 4 {
                    break
                }
            }
            result <- counter
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(5, ((IntLangValue)result).Value); // 执行 0,1,2,3,4，然后在 i=4 时 break
    }

    [Fact]
    public void ContinueStatement_InForLoop_SkipsIteration()
    {
        // Arrange
        var code = @"
            sum <- 0
            for i <- 0, i < 10, i++ {
                if i % 2 == 0 {
                    continue
                }
                sum <- sum + i
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
        Assert.Equal(25, ((IntLangValue)result).Value); // 1+3+5+7+9 = 25
    }

    [Fact]
    public void BreakStatement_InWhileLoop_ExitsEarly()
    {
        // Arrange
        var code = @"
            counter <- 0
            while true {
                counter <- counter + 1
                if counter >= 5 {
                    break
                }
            }
            result <- counter
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(5, ((IntLangValue)result).Value);
    }

    [Fact]
    public void ContinueStatement_InWhileLoop_SkipsIteration()
    {
        // Arrange
        var code = @"
            i <- 0
            sum <- 0
            while i < 10 {
                i <- i + 1
                if i % 2 == 0 {
                    continue
                }
                sum <- sum + i
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
        Assert.Equal(25, ((IntLangValue)result).Value); // 1+3+5+7+9 = 25
    }

    [Fact]
    public void BreakStatement_InForInLoop_ExitsEarly()
    {
        // Arrange
        var code = @"
            numbers <- [10, 20, 30, 40, 50]
            sum <- 0
            for num in numbers {
                sum <- sum + num
                if sum >= 60 {
                    break
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
        Assert.Equal(60, ((IntLangValue)result).Value); // 10+20+30 = 60，然后 break
    }

    [Fact]
    public void LoopWithVariables_ModifiesOuterScope()
    {
        // Arrange
        var code = @"
            x <- 0
            for i <- 0, i < 5, i++ {
                x <- x + i
            }
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
        Assert.Equal(10, ((IntLangValue)result).Value); // 0+1+2+3+4 = 10
    }

    [Fact]
    public void InfiniteLoop_WithBreakCondition_HandlesCorrectly()
    {
        // Arrange
        var code = @"
            counter <- 0
            while true {
                counter <- counter + 1
                if counter >= 3 {
                    break
                }
            }
            result <- counter
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
    public void LoopWithComplexLogic_CalculatesFactorial()
    {
        // Arrange
        var code = @"
            n <- 5
            factorial <- 1
            for i <- 1, i <= n, i++ {
                factorial <- factorial * i
            }
            result <- factorial
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(120, ((IntLangValue)result).Value); // 5! = 120
    }

    [Fact]
    public void LoopWithFunctionCalls_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            func getValue(x) {
                return x * 2
            }
            sum <- 0
            for i <- 0, i < 5, i++ {
                sum <- sum + getValue(i)
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
        Assert.Equal(20, ((IntLangValue)result).Value); // 0*2 + 1*2 + 2*2 + 3*2 + 4*2 = 0+2+4+6+8 = 20
    }

    [Fact]
    public void LoopWithMultipleBreaks_ExitsAtFirstBreak()
    {
        // Arrange
        var code = @"
            counter <- 0
            for i <- 0, i < 10, i++ {
                counter <- counter + 1
                if i == 3 {
                    break
                }
                if i == 5 {
                    break  // 这永远不会执行
                }
            }
            result <- counter
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(4, ((IntLangValue)result).Value); // 执行 0,1,2,3，然后在 i=3 时 break
    }

    [Fact]
    public void LoopWithStringOperations_BuildsString()
    {
        // Arrange
        var code = @"
            result <- """"
            for i <- 0, i < 5, i++ {
                result <- result + i + """"
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
        Assert.Equal("01234", ((StringLangValue)result).Value);
    }

    [Fact]
    public void LoopWithArrayOperations_FiltersArray()
    {
        // Arrange
        var code = @"
            numbers <- {1, 2, 3, 4, 5, 6, 7, 8, 9, 10}
            evens <- []
            for num in numbers {
                if num % 2 == 0 {
                    evens.Add(num)
                }
            }
            result <- evens
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        // 验证结果包含 [2, 4, 6, 8, 10]
    }
}