using Old8Lang.AST.Expression;
using Xunit;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.Tests.Interpreter.EdgeCases;

/// <summary>
/// 极值测试
/// </summary>
public class ExtremeValuesTests
{
    [Fact]
    public void ExtremeValues_MaxInt_HandlesMaximumInteger()
    {
        // Arrange
        var code = @"
            maxInt <- 2147483647
            result <- maxInt + 1 > maxInt
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.Equal(true, ((BoolLangValue)result).Value);
    }

    [Fact]
    public void ExtremeValues_MinInt_HandlesMinimumInteger()
    {
        // Arrange
        var code = @"
            minInt <- -2147483648
            result <- minInt - 1 < minInt
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.Equal(true, ((BoolLangValue)result).Value);
    }

    [Fact]
    public void ExtremeValues_MaxDouble_HandlesMaximumDouble()
    {
        // Arrange
        var code = @"
            maxDouble <- 1.7976931348623157e+308
            result <- maxDouble > 0
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.Equal(true, ((BoolLangValue)result).Value);
    }

    [Fact]
    public void ExtremeValues_MinDouble_HandlesMinimumPositiveDouble()
    {
        // Arrange
        var code = @"
            minDouble <- 5e-324
            result <- minDouble > 0 && minDouble < 1e-323
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.Equal(true, ((BoolLangValue)result).Value);
    }

    [Fact]
    public void ExtremeValues_VeryLargeNumber_HandlesVeryLargeNumber()
    {
        // Arrange
        var code = @"
            hugeNumber <- 999999999999999999
            result <- hugeNumber.ToStr().Length > 10
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.Equal(true, ((BoolLangValue)result).Value);
    }

    [Fact]
    public void ExtremeValues_VerySmallNumber_HandlesVerySmallNumber()
    {
        // Arrange
        var code = @"
            tinyNumber <- 0.000000000000001
            result <- tinyNumber > 0 && tinyNumber < 0.000001
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.Equal(true, ((BoolLangValue)result).Value);
    }

    [Fact]
    public void ExtremeValues_DeepRecursion_HandlesDeepRecursiveCalls()
    {
        // Arrange
        var code = @"
            func recursive(n:int) -> int {
                if n <= 0 {
                    return 0
                }
                return recursive(n - 1) + 1
            }
            result <- recursive(100)
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
    public void ExtremeValues_LargeArray_HandlesLargeArray()
    {
        // Arrange
        var code = @"
            largeArray <- [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20]
            result <- largeArray.Length + largeArray[0] + largeArray[19]
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(31, ((IntLangValue)result).Value); // 20 + 1 + 20
    }

    [Fact]
    public void ExtremeValues_LargeList_HandlesLargeList()
    {
        // Arrange
        var code = @"
            largeList <- {1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15}
            sum <- 0
            for item in largeList {
                sum <- sum + item
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
        Assert.Equal(120, ((IntLangValue)result).Value); // Sum of 1 to 15
    }

    [Fact]
    public void ExtremeValues_LongString_HandlesLongString()
    {
        // Arrange
        var code = @"
            longString <- ""This is a very long string that contains many words and characters to test how the interpreter handles strings with significant length without performance issues or memory problems.""
            result <- longString.Length > 50
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.Equal(true, ((BoolLangValue)result).Value);
    }

    [Fact]
    public void ExtremeValues_ManyNestedLists_HandlesDeepNesting()
    {
        // Arrange
        var code = @"
            nested <- {{{{{{{{{{{1}}}}}}}}}}}
            result <- nested[0][0][0][0][0][0][0][0][0][0]
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<ListLangValue>(result);
        var innerList = (ListLangValue)result;
        Assert.Equal(1, innerList.Values.Count);
        Assert.Equal(1, ((IntLangValue)innerList.Values[0]).Value);
    }

    [Fact]
    public void ExtremeValues_ManyOperations_HandlesManyOperations()
    {
        // Arrange
        var code = @"
            result <- 1 + 2 + 3 + 4 + 5 + 6 + 7 + 8 + 9 + 10 + 11 + 12 + 13 + 14 + 15 + 16 + 17 + 18 + 19 + 20
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(210, ((IntLangValue)result).Value); // Sum of 1 to 20
    }

    [Fact]
    public void ExtremeValues_ManyConditions_HandlesManyNestedConditions()
    {
        // Arrange
        var code = @"
            x <- 5
            result <- false
            if x > 0 {
                if x > 1 {
                    if x > 2 {
                        if x > 3 {
                            if x > 4 {
                                result <- true
                            }
                        }
                    }
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
        Assert.IsType<BoolLangValue>(result);
        Assert.Equal(true, ((BoolLangValue)result).Value);
    }

    [Fact]
    public void ExtremeValues_ManyFunctionCalls_HandlesManyFunctionCalls()
    {
        // Arrange
        var code = @"
            func addOne(x:int) -> int {
                return x + 1
            }
            result <- addOne(addOne(addOne(addOne(addOne(0)))))
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
    public void ExtremeValues_LargeDictionary_HandlesLargeDictionary()
    {
        // Arrange
        var code = @"
            largeDict <- {
                ""a"": 1,
                ""b"": 2,
                ""c"": 3,
                ""d"": 4,
                ""e"": 5,
                ""f"": 6,
                ""g"": 7,
                ""h"": 8,
                ""i"": 9,
                ""j"": 10
            }
            result <- largeDict.Count + largeDict[""j""]
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(20, ((IntLangValue)result).Value); // 10 + 10
    }

    [Fact]
    public void ExtremeValues_DeepClassNesting_HandlesDeepClassHierarchy()
    {
        // Arrange
        var code = @"
            class Level1 {
                class Level2 {
                    class Level3 {
                        class Level4 {
                            func getValue() -> int {
                                return 42
                            }
                        }
                    }
                }
            }
            deepObj <- Level1().Level2().Level3().Level4()
            result <- deepObj.getValue()
        ";
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
    public void ExtremeValues_MaxIterations_HandlesMaximumLoopIterations()
    {
        // Arrange
        var code = @"
            count <- 0
            for i in 1..1000 {
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
        Assert.Equal(1000, ((IntLangValue)result).Value);
    }

    [Fact]
    public void ExtremeValues_InfiniteLoopPrevention_HandlesPotentialInfiniteLoops()
    {
        // Arrange
        var code = @"
            count <- 0
            while count < 100000 {
                count <- count + 1
                if count >= 50 {
                    break
                }
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
        Assert.Equal(50, ((IntLangValue)result).Value);
    }

    [Fact]
    public void ExtremeValues_MemoryIntensiveString_HandlesMemoryIntensiveOperations()
    {
        // Arrange
        var code = @"
            base <- ""Hello""
            result <- base
            for i in 1..10 {
                result <- result + base
            }
            finalLength <- result.Length
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("finalLength"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(55, ((IntLangValue)result).Value); // "Hello" * 11
    }

    [Fact]
    public void ExtremeValues_ComplexExpression_HandlesComplexNestedExpression()
    {
        // Arrange
        var code = @"
            a <- 1
            b <- 2
            c <- 3
            d <- 4
            e <- 5
            result <- (a + b) * (c + d) - e * (a - b) + (c * d) / (a + e)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<DoubleLangValue>(result);
        Assert.Equal(35.6, ((DoubleLangValue)result).Value, 0.1);
    }

    [Fact]
    public void ExtremeValues_MaximumParams_HandlesFunctionWithManyParameters()
    {
        // Arrange
        var code = @"
            func manyParams(
                p1:int, p2:int, p3:int, p4:int, p5:int,
                p6:int, p7:int, p8:int, p9:int, p10:int
            ) -> int {
                return p1 + p2 + p3 + p4 + p5 + p6 + p7 + p8 + p9 + p10
            }
            result <- manyParams(1, 2, 3, 4, 5, 6, 7, 8, 9, 10)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(55, ((IntLangValue)result).Value); // Sum of 1 to 10
    }

    [Fact]
    public void ExtremeValues_WideRange_HandlesWideRangeOfNumbers()
    {
        // Arrange
        var code = @"
            numbers <- [-1000000, -1000, -10, -1, 0, 1, 10, 1000, 1000000]
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
        Assert.Equal(0, ((IntLangValue)result).Value); // Symmetric numbers sum to 0
    }

    [Fact]
    public void ExtremeValues_Precision_HandlesFloatingPointPrecision()
    {
        // Arrange
        var code = @"
            precise <- 0.1 + 0.2
            result <- precise > 0.29 && precise < 0.31
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<BoolLangValue>(result);
        Assert.Equal(true, ((BoolLangValue)result).Value);
    }

    [Fact]
    public void ExtremeValues_LargeTuple_HandlesLargeTuple()
    {
        // Arrange
        var code = @"
            largeTuple <- (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15)
            result <- largeTuple.Length + largeTuple[0] + largeTuple[14]
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(17, ((IntLangValue)result).Value); // 15 + 1 + 15
    }

    [Fact]
    public void ExtremeValues_ManySwitchCases_HandlesManySwitchCases()
    {
        // Arrange
        var code = @"
            value <- 5
            result <- 0
            switch value {
                case 1: result <- 1
                case 2: result <- 2
                case 3: result <- 3
                case 4: result <- 4
                case 5: result <- 5
                case 6: result <- 6
                case 7: result <- 7
                case 8: result <- 8
                case 9: result <- 9
                case 10: result <- 10
                default: result <- 0
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
        Assert.Equal(5, ((IntLangValue)result).Value);
    }

    [Fact]
    public void ExtremeValues_HigherOrderFunctions_HandlesComplexHigherOrderFunctions()
    {
        // Arrange
        var code = @"
            func applyTwice(f: (int) -> int, x:int) -> int {
                return f(f(x))
            }
            func square(n:int) -> int {
                return n * n
            }
            result <- applyTwice(square, 3)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(81, ((IntLangValue)result).Value); // square(square(3)) = square(9) = 81
    }

    [Fact]
    public void ExtremeValues_RapidOperations_HandlesRapidSuccessiveOperations()
    {
        // Arrange
        var code = @"
            x <- 1
            for i in 1..100 {
                x <- x * 2
                x <- x / 2
                x <- x + 1
                x <- x - 1
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
        Assert.Equal(1, ((IntLangValue)result).Value);
    }

    [Fact]
    public void ExtremeValues_NestedLoops_HandlesDeeplyNestedLoops()
    {
        // Arrange
        var code = @"
            count <- 0
            for i in 1..5 {
                for j in 1..5 {
                    for k in 1..5 {
                        count <- count + 1
                    }
                }
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
        Assert.Equal(125, ((IntLangValue)result).Value); // 5 * 5 * 5
    }
}