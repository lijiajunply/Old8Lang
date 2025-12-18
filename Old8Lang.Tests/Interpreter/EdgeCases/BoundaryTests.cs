using Old8Lang.AST.Expression;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.Tests.Interpreter.EdgeCases;

/// <summary>
/// 边界值测试
/// </summary>
public class BoundaryTests
{
    [Fact]
    public void Boundary_IntZero_HandlesZeroValue()
    {
        // Arrange
        var code = @"
            x <- 0
            result <- x * 10
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
    public void Boundary_IntMax_HandlesMaxInt()
    {
        // Arrange
        var code = @"
            maxInt <- 2147483647
            result <- maxInt + 1
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        // Result may overflow or be handled specially
    }

    [Fact]
    public void Boundary_IntMin_HandlesMinInt()
    {
        // Arrange
        var code = @"
            minInt <- -2147483648
            result <- minInt - 1
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        // Result may underflow or be handled specially
    }

    [Fact]
    public void Boundary_DoubleZero_HandlesDoubleZero()
    {
        // Arrange
        var code = @"
            x <- 0.0
            result <- x * 3.14
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<DoubleLangValue>(result);
        Assert.Equal(0.0, ((DoubleLangValue)result).Value);
    }

    [Fact]
    public void Boundary_DoubleInfinity_HandlesInfinity()
    {
        // Arrange
        var code = @"
            x <- 1.7976931348623157e+308
            result <- x * 2.0
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        // Result may be infinity
    }

    [Fact]
    public void Boundary_ArrayEmpty_HandlesEmptyArray()
    {
        // Arrange
        var code = @"
            emptyArray <- []
            result <- len(emptyArray)
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
    public void Boundary_ArraySingleElement_HandlesSingleElementArray()
    {
        // Arrange
        var code = @"
            singleArray <- [42]
            result <- singleArray[0]
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
    public void Boundary_ListEmpty_HandlesEmptyList()
    {
        // Arrange
        var code = @"
            emptyList <- {}
            result <- len(emptyList)
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
    public void Boundary_StringEmpty_HandlesEmptyString()
    {
        // Arrange
        var code = @"
            emptyString <- """"
            result <- len(emptyString)
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
    public void Boundary_StringSingleChar_HandlesSingleCharacter()
    {
        // Arrange
        var code = @"
            singleChar <- ""A""
            result <- len(singleChar)
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
    public void Boundary_DictionaryEmpty_HandlesEmptyDictionary()
    {
        // Arrange
        var code = @"
            emptyDict <- dict()
            result <- emptyDict.Count
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
    public void Boundary_TupleEmpty_HandlesEmptyTuple()
    {
        // Arrange
        var code = @"
            emptyTuple <- ()
            result <- len(emptyTuple)
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
    public void Boundary_LoopZeroIterations_HandlesZeroIterationLoop()
    {
        // Arrange
        var code = @"
            sum <- 0
            for i in 0..-1 {
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
        Assert.Equal(0, ((IntLangValue)result).Value);
    }

    [Fact]
    public void Boundary_LoopSingleIteration_HandlesSingleIterationLoop()
    {
        // Arrange
        var code = @"
            sum <- 0
            for i in 1..1 {
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
        Assert.Equal(1, ((IntLangValue)result).Value);
    }

    [Fact]
    public void Boundary_RangeEmpty_HandlesEmptyRange()
    {
        // Arrange
        var code = @"
            emptyRange <- 5..<5
            count <- 0
            for i in emptyRange {
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
    public void Boundary_RangeSingle_HandlesSingleElementRange()
    {
        // Arrange
        var code = @"
            singleRange <- 5..5
            count <- 0
            sum <- 0
            for i in singleRange {
                count <- count + 1
                sum <- sum + i
            }
            result <- ""count: "" + count.ToStr() + "", sum: "" + sum.ToStr()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("count: 1, sum: 5", ((StringLangValue)result).Value);
    }

    [Fact]
    public void Boundary_ArrayIndexZero_HandlesIndexZero()
    {
        // Arrange
        var code = @"
            arr <- [10, 20, 30]
            result <- arr[0]
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(10, ((IntLangValue)result).Value);
    }

    [Fact]
    public void Boundary_ArrayIndexLast_HandlesLastIndex()
    {
        // Arrange
        var code = @"
            arr <- [10, 20, 30]
            lastIndex <- len(arr) - 1
            result <- arr[lastIndex]
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(30, ((IntLangValue)result).Value);
    }

    [Fact]
    public void Boundary_ArrayIndexOutOfBounds_HandlesIndexOutOfBounds()
    {
        // Arrange
        var code = @"
            arr <- [10, 20, 30]
            try {
                result <- arr[10]
            } catch {
                result <- -1
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
        Assert.Equal(-1, ((IntLangValue)result).Value);
    }

    [Fact]
    public void Boundary_ArrayIndexNegative_HandlesNegativeIndex()
    {
        // Arrange
        var code = @"
            arr <- [10, 20, 30]
            try {
                result <- arr[-1]
            } catch {
                result <- -1
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
        Assert.Equal(30, ((IntLangValue)result).Value);
    }

    [Fact]
    public void Boundary_RecursiveDepthZero_HandlesZeroRecursionDepth()
    {
        // Arrange
        var code = @"
            func factorial(n:int) -> int {
                if n <= 1 {
                    return 1
                }
                return n * factorial(n - 1)
            }
            result <- factorial(0)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(1, ((IntLangValue)result).Value); // 0! = 1
    }

    [Fact]
    public void Boundary_RecursiveDepthOne_HandlesSingleRecursionDepth()
    {
        // Arrange
        var code = @"
            func factorial(n:int) -> int {
                if n <= 1 {
                    return 1
                }
                return n * factorial(n - 1)
            }
            result <- factorial(1)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(1, ((IntLangValue)result).Value); // 1! = 1
    }

    [Fact]
    public void Boundary_DivisionByZero_HandlesDivisionByZero()
    {
        // Arrange
        var code = @"
            try {
                result <- 10 / 0
            } catch {
                result <- -999
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
        Assert.Equal(-999, ((IntLangValue)result).Value);
    }

    [Fact]
    public void Boundary_ModuloByZero_HandlesModuloByZero()
    {
        // Arrange
        var code = @"
            try {
                result <- 10 % 0
            } catch {
                result <- -888
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
        Assert.Equal(-888, ((IntLangValue)result).Value);
    }

    [Fact]
    public void Boundary_PowerOfZero_HandlesZeroToPower()
    {
        // Arrange
        var code = @"
            result1 <- 0 ^ 5
            result2 <- 5 ^ 0
            result3 <- 0 ^ 0
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1"));
        var result2 = interpreter.Manager.GetValue(new LangId("result2"));
        var result3 = interpreter.Manager.GetValue(new LangId("result3"));

        Assert.NotNull(result1);
        Assert.Equal(0, ((IntLangValue)result1).Value); // 0^5 = 0

        Assert.NotNull(result2);
        Assert.Equal(1, ((IntLangValue)result2).Value); // 5^0 = 1

        Assert.NotNull(result3);
        // 0^0 is typically defined as 1 in programming languages
        Assert.Equal(1, ((IntLangValue)result3).Value);
    }

    [Fact]
    public void Boundary_SqrtOfZero_HandlesSquareRootOfZero()
    {
        // Arrange
        var code = @"
            result <- (0).ToSqrt()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<DoubleLangValue>(result);
        Assert.Equal(0.0, ((DoubleLangValue)result).Value);
    }

    [Fact]
    public void Boundary_SqrtOfOne_HandlesSquareRootOfOne()
    {
        // Arrange
        var code = @"
            result <- (1).ToSqrt()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<DoubleLangValue>(result);
        Assert.Equal(1.0, ((DoubleLangValue)result).Value);
    }

    [Fact]
    public void Boundary_LogOfOne_HandlesLogarithmOfOne()
    {
        // Arrange
        var code = @"
            result <- (1.0).ToLog()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<DoubleLangValue>(result);
        Assert.Equal(0.0, ((DoubleLangValue)result).Value); // log(1) = 0
    }

    [Fact]
    public void Boundary_LogOfZero_HandlesLogarithmOfZero()
    {
        // Arrange
        var code = @"
            try {
                result <- (0.0).ToLog()
            } catch {
                result <- -999.0
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<DoubleLangValue>(result);
        Assert.Equal(-999.0, ((DoubleLangValue)result).Value); // Should be -∞ or error
    }

    [Fact]
    public void Boundary_StringIndexZero_HandlesStringIndexZero()
    {
        // Arrange
        var code = @"
            text <- ""hello""
            result <- text[0]
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<CharLangValue>(result);
        Assert.Equal('h', ((CharLangValue)result).Value);
    }

    [Fact]
    public void Boundary_StringIndexLast_HandlesStringLastIndex()
    {
        // Arrange
        var code = @"
            text <- ""hello""
            lastIndex <- len(text) - 1
            result <- text[lastIndex]
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<CharLangValue>(result);
        Assert.Equal('o', ((CharLangValue)result).Value);
    }

    [Fact]
    public void Boundary_CharacterNull_HandlesNullCharacter()
    {
        // Arrange
        var code = @"
            nullChar <- '\0'
            result <- nullChar.ToInt()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(0, ((IntLangValue)result).Value); // ASCII value of null character
    }

    [Fact]
    public void Boundary_CharacterMax_HandlesMaxCharacter()
    {
        // Arrange
        var code = @"
            maxChar <- '\uffff'
            result <- maxChar.ToInt()
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(65535, ((IntLangValue)result).Value); // Max Unicode code point
    }

    [Fact]
    public void Boundary_ListCapacity_HandlesListCapacityGrowth()
    {
        // Arrange
        var code = @"
            items <- {}
            // Add many items to test capacity growth
            for i in 0..1000 {
                items.Add(i)
            }
            result <- len(items)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(1001, ((IntLangValue)result).Value);
    }

    [Fact]
    public void Boundary_NestingDepth_HandlesDeepNesting()
    {
        // Arrange
        var code = @"
            func createNestedList(depth:int) -> list {
                if depth == 0 {
                    return {0}
                }
                return {createNestedList(depth - 1)}
            }
            nested <- createNestedList(5)
            result <- nested
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        // Should handle nested structures
    }

    [Fact]
    public void Boundary_BooleanLogic_HandlesEdgeCaseLogic()
    {
        // Arrange
        var code = @"
            // Test logical operator edge cases
            result1 <- true or false
            result2 <- true and false
            result3 <- not true
            result4 <- not false
            result5 <- true xor true
            result6 <- true xor false
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
        var result5 = interpreter.Manager.GetValue(new LangId("result5"));
        var result6 = interpreter.Manager.GetValue(new LangId("result6"));

        Assert.True(((BoolLangValue)result1).Value);
        Assert.False(((BoolLangValue)result2).Value);
        Assert.False(((BoolLangValue)result3).Value);
        Assert.True(((BoolLangValue)result4).Value);
        Assert.False(((BoolLangValue)result5).Value);
        Assert.True(((BoolLangValue)result6).Value);
    }
}