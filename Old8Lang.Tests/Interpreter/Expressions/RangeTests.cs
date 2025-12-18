using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.Tests.Interpreter.Expressions;

/// <summary>
/// 范围表达式测试
/// </summary>
public class RangeTests
{
    [Fact]
    public void Range_BasicInclusiveRange_CreatesCorrectRange()
    {
        // Arrange
        var code = @"
            range1 <- [1~5]
            result1 <- range1
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result1"));
        Assert.NotNull(result);
        Assert.IsType<ArrayLangValue>(result);
    }

    [Fact]
    public void Range_BasicExclusiveRange_CreatesCorrectRange()
    {
        // Arrange
        var code = @"
            range2 <- [1~5]
            result2 <- range2
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result2"));
        Assert.NotNull(result);
        Assert.IsType<ArrayLangValue>(result);
    }

    [Fact]
    public void Range_ForLoopWithInclusiveRange_IteratesCorrectly()
    {
        // Arrange
        var code = @"
            sum <- 0
            for i in [1~5] {
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
        Assert.Equal(15, ((IntLangValue)result).Value); // 1+2+3+4+5 = 15
    }

    [Fact]
    public void Range_ForLoopWithExclusiveRange_IteratesCorrectly()
    {
        // Arrange
        var code = @"
            sum <- 0
            for i in [1~5] {
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
        Assert.Equal(15, ((IntLangValue)result).Value); // 1+2+3+4+5 = 15
    }

    [Fact]
    public void Range_NegativeNumbers_HandlesNegativeRanges()
    {
        // Arrange
        var code = @"
            sum <- 0
            for i in [-3~2] {
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
        Assert.Equal(-3, ((IntLangValue)result).Value); // (-3)+(-2)+(-1)+0+1+2 = -3
    }

    [Fact]
    public void Range_ZeroRange_HandlesZeroBasedRanges()
    {
        // Arrange
        var code = @"
            sum <- 0
            for i in [0~4] {
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
        Assert.Equal(10, ((IntLangValue)result).Value); // 0+1+2+3+4 = 10
    }

    [Fact]
    public void Range_SingleValueRange_HandlesSingleValue()
    {
        // Arrange
        var code = @"
            count <- 0
            for i in [5~5] {
                count <- count + 1
            }
            result <- count
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(1, ((IntLangValue)result).Value);
    }

    [Fact]
    public void Range_EmptyExclusiveRange_HandlesEmptyRange()
    {
        // Arrange
        var code = @"
            count <- 0
            for i in [5~5] {
                count <- count + 1
            }
            result <- count
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
    public void Range_LargeNumbers_HandlesLargeRanges()
    {
        // Arrange
        var code = @"
            count <- 0
            for i in [1000~1005] {
                count <- count + 1
            }
            result <- count
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(6, ((IntLangValue)result).Value);
    }

    [Fact]
    public void Range_ReverseRange_HandlesDescendingRanges()
    {
        // Arrange
        var code = @"
            sum <- 0
            for i in [5~1] {
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
        Assert.Equal(15, ((IntLangValue)result).Value); // 5+4+3+2+1 = 15
    }

    [Fact]
    public void Range_WithVariables_UsesVariablesInRange()
    {
        // Arrange
        var code = @"
            start <- 3
            end <- 8
            sum <- 0
            for i in [start~end] {
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
        Assert.Equal(33, ((IntLangValue)result).Value); // 3+4+5+6+7+8 = 33
    }

    [Fact]
    public void Range_WithExpressions_EvaluatesExpressionsInRange()
    {
        // Arrange
        var code = @"
            base <- 2
            multiplier <- 3
            sum <- 0
            for i in [base~(base * multiplier)] {
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
        Assert.Equal(12, ((IntLangValue)result).Value); // 2+3+4+5+6 = 20
    }

    [Fact]
    public void Range_NestedLoops_HandlesNestedRangeLoops()
    {
        // Arrange
        var code = @"
            product <- 0
            for i in [1~3] {
                for j in [1~2] {
                    product <- product + (i * j)
                }
            }
            result <- product
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(18, ((IntLangValue)result).Value); // (1*1)+(1*2)+(2*1)+(2*2)+(3*1)+(3*2) = 1+2+2+4+3+6 = 18
    }

    [Fact]
    public void Range_WithContinue_SkipsIterations()
    {
        // Arrange
        var code = @"
            sum <- 0
            for i in [1~10] {
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
    public void Range_WithBreak_ExitsLoop()
    {
        // Arrange
        var code = @"
            sum <- 0
            for i in [1~100] {
                sum <- sum + i
                if i >= 5 {
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
        Assert.Equal(15, ((IntLangValue)result).Value); // 1+2+3+4+5 = 15
    }

    [Fact]
    public void Range_ArrayIndexing_UsesRangeForArrayAccess()
    {
        // Arrange
        var code = @"
            numbers <- [10, 20, 30, 40, 50, 60, 70, 80, 90, 100]
            sum <- 0
            for i in [2~5] {
                sum <- sum + numbers[i]
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
        Assert.Equal(180, ((IntLangValue)result).Value); // 30+40+50+60 = 180
    }

    [Fact]
    public void Range_ListIndexing_UsesRangeForListAccess()
    {
        // Arrange
        var code = @"
            items <- {""a"", ""b"", ""c"", ""d"", ""e""}
            resultString <- """"
            for i in [1~4] {
                resultString <- resultString + items[i]
            }
            result <- resultString
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("bcde", ((StringLangValue)result).Value); // items[1]+items[2]+items[3]
    }

    [Fact]
    public void Range_StringIteration_IteratesOverStringCharacters()
    {
        // Arrange
        var code = @"
            text <- ""hello""
            resultString <- """"
            for i in [0~(len(text)-1)] {
                charValue <- text[i]
                resultString <- resultString + charValue.ToStr()
            }
            result <- resultString
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("hello", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Range_WithStep_HandlesSteppedIteration()
    {
        // Arrange
        var code = @"
            sum <- 0
            for i in [0~10] {
                if i % 2 == 0 { // Simulate step of 2
                    sum <- sum + i
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
        Assert.Equal(30, ((IntLangValue)result).Value); // 0+2+4+6+8+10 = 30
    }

    [Fact]
    public void Range_InFunctionParameter_PassesRangeToFunction()
    {
        // Arrange
        var code = @"
            func sumRange(start:int, end:int) -> int {
                sum <- 0
                for i in [start~end] {
                    sum <- sum + i
                }
                return sum
            }
            result <- sumRange(3, 7)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(25, ((IntLangValue)result).Value); // 3+4+5+6+7 = 25
    }

    [Fact]
    public void Range_CollectionGeneration_GeneratesCollectionFromRange()
    {
        // Arrange
        var code = @"
            numbers <- {}
            for i in [1~5] {
                numbers.Add(i)
            }
            result <- numbers
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<ListLangValue>(result);
        var list = (ListLangValue)result;
        Assert.Equal(5, list.Values.Count);
        Assert.Equal(1, ((IntLangValue)list.Values[0]).Value);
        Assert.Equal(2, ((IntLangValue)list.Values[1]).Value);
        Assert.Equal(3, ((IntLangValue)list.Values[2]).Value);
        Assert.Equal(4, ((IntLangValue)list.Values[3]).Value);
        Assert.Equal(5, ((IntLangValue)list.Values[4]).Value);
    }

    [Fact]
    public void Range_BoundaryConditions_HandlesEdgeCases()
    {
        // Arrange
        var code = @"
            // Test very large range
            largeCount <- 0
            for i in [1000000~1000002] {
                largeCount <- largeCount + 1
            }

            // Test negative to positive range
            crossZeroSum <- 0
            for i in [-2~2] {
                crossZeroSum <- crossZeroSum + i
            }

            result1 <- largeCount
            result2 <- crossZeroSum
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));

        Assert.NotNull(result1);
        Assert.IsType<IntLangValue>(result1);
        Assert.Equal(3, ((IntLangValue)result1).Value);

        Assert.NotNull(result2);
        Assert.IsType<IntLangValue>(result2);
        Assert.Equal(0, ((IntLangValue)result2).Value); // -2+-1+0+1+2 = 0
    }

    [Fact]
    public void Range_WithComplexOperations_PerformsComplexCalculations()
    {
        // Arrange
        var code = @"
            factorial <- 1
            for i in [1~5] {
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
    public void Range_MultipleRanges_HandlesMultipleRangeOperations()
    {
        // Arrange
        var code = @"
            sum1 <- 0
            sum2 <- 0
            sum3 <- 0

            for i in [1~3] {
                sum1 <- sum1 + i
            }

            for j in [5~7] {
                sum2 <- sum2 + j
            }

            for k in [10~13] {
                sum3 <- sum3 + k
            }

            totalSum <- sum1 + sum2 + sum3
            result <- totalSum
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(70, ((IntLangValue)result).Value); // (1+2+3)+(5+6+7)+(10+11+12+13) = 6+18+33 = 57
    }
}