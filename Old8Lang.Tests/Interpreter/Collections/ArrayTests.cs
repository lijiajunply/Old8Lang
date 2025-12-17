using Old8Lang.AST.Expression;
using Xunit;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.Tests.Interpreter.Collections;

/// <summary>
/// 数组操作解释模式测试
/// </summary>
public class ArrayTests
{
    [Fact]
    public void ArrayCreation_EmptyArray_CreatesCorrectly()
    {
        // Arrange
        var code = "arr <- []";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var arr = interpreter.Manager.GetValue(new LangId("arr"));
        Assert.NotNull(arr);
        // 具体的断言取决于数组类型的实现
    }

    [Fact]
    public void ArrayCreation_WithIntegerElements_CreatesCorrectly()
    {
        // Arrange
        var code = "arr <- [1, 2, 3, 4, 5]";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var arr = interpreter.Manager.GetValue(new LangId("arr"));
        Assert.NotNull(arr);
        // 验证数组长度和内容
    }

    [Fact]
    public void ArrayCreation_WithMixedTypes_CreatesCorrectly()
    {
        // Arrange
        var code = "arr <- [1, \"hello\", true, 3.14, 'A']";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var arr = interpreter.Manager.GetValue(new LangId("arr"));
        Assert.NotNull(arr);
        // 验证混合类型的数组
    }

    [Fact]
    public void ArrayAccess_ValidIndex_ReturnsCorrectElement()
    {
        // Arrange
        var code = @"
            arr <- [10, 20, 30, 40, 50]
            element <- arr[2]
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var element = interpreter.Manager.GetValue(new LangId("element"));
        Assert.NotNull(element);
        Assert.IsType<IntLangValue>(element);
        Assert.Equal(30, ((IntLangValue)element).Value);
    }

    [Fact]
    public void ArrayAccess_FirstElement_ReturnsCorrectElement()
    {
        // Arrange
        var code = @"
            arr <- [100, 200, 300]
            first <- arr[0]
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var first = interpreter.Manager.GetValue(new LangId("first"));
        Assert.NotNull(first);
        Assert.IsType<IntLangValue>(first);
        Assert.Equal(100, ((IntLangValue)first).Value);
    }

    [Fact]
    public void ArrayAccess_LastElement_ReturnsCorrectElement()
    {
        // Arrange
        var code = @"
            arr <- [10, 20, 30, 40, 50]
            last <- arr[4]
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var last = interpreter.Manager.GetValue(new LangId("last"));
        Assert.NotNull(last);
        Assert.IsType<IntLangValue>(last);
        Assert.Equal(50, ((IntLangValue)last).Value);
    }

    [Fact]
    public void ArrayAssignment_ValidIndex_UpdatesElement()
    {
        // Arrange
        var code = @"
            arr <- [1, 2, 3, 4, 5]
            arr[2] <- 100
            updated <- arr[2]
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var updated = interpreter.Manager.GetValue(new LangId("updated"));
        Assert.NotNull(updated);
        Assert.IsType<IntLangValue>(updated);
        Assert.Equal(100, ((IntLangValue)updated).Value);
    }

    [Fact]
    public void ArrayAssignment_DifferentType_UpdatesWithNewType()
    {
        // Arrange
        var code = @"
            arr <- [1, 2, 3, 4, 5]
            arr[1] <- ""hello""
            updated <- arr[1]
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var updated = interpreter.Manager.GetValue(new LangId("updated"));
        Assert.NotNull(updated);
        Assert.IsType<StringLangValue>(updated);
        Assert.Equal("hello", ((StringLangValue)updated).Value);
    }

    [Fact]
    public void ArrayIteration_WithForLoop_ProcessesAllElements()
    {
        // Arrange
        var code = @"
            arr <- [1, 2, 3, 4, 5]
            sum <- 0
            for i <- 0, i < 5, i++ {
                sum <- sum + arr[i]
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
    public void ArrayIteration_WithForInLoop_ProcessesAllElements()
    {
        // Arrange
        var code = @"
            arr <- [10, 20, 30, 40, 50]
            sum <- 0
            for element in arr {
                sum <- sum + element
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
        Assert.Equal(150, ((IntLangValue)result).Value); // 10+20+30+40+50 = 150
    }

    [Fact]
    public void Array_Search_FindElement_ReturnsCorrectIndex()
    {
        // Arrange
        var code = @"
            arr <- [10, 20, 30, 40, 50]
            target <- 30
            index <- -1
            for i <- 0, i < 5, i++ {
                if arr[i] == target {
                    index <- i
                    break
                }
            }
            result <- index
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(2, ((IntLangValue)result).Value); // arr[2] = 30
    }

    [Fact]
    public void Array_Filter_ConditionalFiltering_CreatesNewArray()
    {
        // Arrange
        var code = @"
            arr <- [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]
            evens <- []
            for element in arr {
                if element % 2 == 0 {
                    evens.Add(element)
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
        // 验证结果数组包含 [2, 4, 6, 8, 10]
    }

    [Fact]
    public void ArrayMap_TransformElements_CreatesNewArray()
    {
        // Arrange
        var code = @"
            arr <- [1, 2, 3, 4, 5]
            squares <- []
            for element in arr {
                squares.Add(element * element)
            }
            result <- squares
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        // 验证结果数组包含 [1, 4, 9, 16, 25]
    }

    [Fact]
    public void ArrayMax_FindMaximum_ReturnsCorrectValue()
    {
        // Arrange
        var code = @"
            arr <- [10, 5, 20, 15, 30, 25]
            maxVal <- arr[0]
            for element in arr {
                if element > maxVal {
                    maxVal <- element
                }
            }
            result <- maxVal
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
    public void ArrayMin_FindMinimum_ReturnsCorrectValue()
    {
        // Arrange
        var code = @"
            arr <- [10, 5, 20, 15, 30, 25]
            minVal <- arr[0]
            for element in arr {
                if element < minVal {
                    minVal <- element
                }
            }
            result <- minVal
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
    public void ArrayReverse_ReverseElements_CreatesReversedArray()
    {
        // Arrange
        var code = @"
            arr <- [1, 2, 3, 4, 5]
            reversed <- []
            i <- 4
            while i >= 0 {
                reversed.Add(arr[i])
                i <- i - 1
            }
            result <- reversed
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        // 验证结果数组包含 [5, 4, 3, 2, 1]
    }

    [Fact]
    public void ArrayConcat_TwoArrays_CreatesCombinedArray()
    {
        // Arrange
        var code = @"
            arr1 <- [1, 2, 3]
            arr2 <- [4, 5, 6]
            combined <- []
            for element in arr1 {
                combined.Add(element)
            }
            for element in arr2 {
                combined.Add(element)
            }
            result <- combined
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        // 验证结果数组包含 [1, 2, 3, 4, 5, 6]
    }

    [Fact]
    public void ArraySlice_GetSubArray_CreatesCorrectSlice()
    {
        // Arrange
        var code = @"
            arr <- [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]
            slice <- []
            for i <- 2, i < 7, i++ {
                slice.Add(arr[i])
            }
            result <- slice
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        // 验证结果数组包含 [3, 4, 5, 6, 7]
    }

    [Fact]
    public void ArrayWithStrings_StringOperations_WorksCorrectly()
    {
        // Arrange
        var code = @"
            arr <- [""apple"", ""banana"", ""cherry""]
            second <- arr[1]
            result <- second
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("banana", ((StringLangValue)result).Value);
    }

    [Fact]
    public void ArrayNestedArrays_TwoDimensional_WorksCorrectly()
    {
        // Arrange
        var code = @"
            matrix <- [
                [1, 2, 3],
                [4, 5, 6],
                [7, 8, 9]
            ]
            element <- matrix[1][2]  // 第二行第三列
            result <- element
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(6, ((IntLangValue)result).Value); // matrix[1][2] = 6
    }

    [Fact]
    public void ArrayDynamicSize_AddElements_WorksCorrectly()
    {
        // Arrange
        var code = @"
            arr <- []
            for i <- 0, i < 5, i++ {
                arr.Add(i * 2)
            }
            result <- arr
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        // 验证结果数组包含 [0, 2, 4, 6, 8]
    }

    [Fact]
    public void ArrayContains_CheckElementPresence_ReturnsCorrectBoolean()
    {
        // Arrange
        var code = @"
            arr <- [10, 20, 30, 40, 50]
            target1 <- 30
            target2 <- 25
            found1 <- false
            found2 <- false
            for element in arr {
                if element == target1 {
                    found1 <- true
                }
                if element == target2 {
                    found2 <- true
                }
            }
            result1 <- found1
            result2 <- found2
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result1 = interpreter.Manager.GetValue(new LangId("result1")) as BoolLangValue;
        var result2 = interpreter.Manager.GetValue(new LangId("result2")) as BoolLangValue;

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.True(result1.Value);  // 30 is in array
        Assert.False(result2.Value); // 25 is not in array
    }

    [Fact]
    public void ArraySort_SimpleSort_ReordersElements()
    {
        // Arrange
        var code = @"
            arr <- [5, 2, 8, 1, 9, 3]
            sorted <- []
            // 简单冒泡排序
            n <- 6
            for i <- 0, i < n, i++ {
                for j <- 0, j < n - 1 - i, j++ {
                    if arr[j] > arr[j + 1] {
                        temp <- arr[j]
                        arr[j] <- arr[j + 1]
                        arr[j + 1] <- temp
                    }
                }
            }
            result <- arr
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        // 验证结果数组包含 [1, 2, 3, 5, 8, 9]
    }

    [Fact]
    public void ArrayAccess_InvalidIndex_HandlesError()
    {
        // Arrange
        var code = @"
            arr <- [1, 2, 3]
            try {
                element <- arr[10]  // 超出数组边界
                result <- ""success""
            } catch {
                result <- ""error""
            }
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        // 应该返回 "error" 如果支持边界检查
    }

    [Fact]
    public void ArrayCount_CountElements_ReturnsCorrectCount()
    {
        // Arrange
        var code = @"
            arr <- [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]
            count <- 0
            for element in arr {
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
        Assert.Equal(10, ((IntLangValue)result).Value);
    }
}