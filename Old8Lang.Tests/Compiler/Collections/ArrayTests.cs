using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Collections;

/// <summary>
/// 数组操作编译模式测试
/// </summary>
[Collection("Sequential")]
public class ArrayTests
{
    [Fact]
    public void EmptyArray_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            arr <- []
            Assert.Equal(0, Len(arr))
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 如果 Old8Lang 断言失败会抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void SingleElementArray_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            arr <- [42]
            Assert.Equal(1, Len(arr))
            Assert.Equal(42, arr[0])
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 如果 Old8Lang 断言失败会抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void MultiElementArray_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            arr <- [1, 2, 3, 4, 5]
            Assert.Equal(5, Len(arr))
            Assert.Equal(1, arr[0])
            Assert.Equal(2, arr[1])
            Assert.Equal(3, arr[2])
            Assert.Equal(4, arr[3])
            Assert.Equal(5, arr[4])
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 如果 Old8Lang 断言失败会抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ArrayAccessByIndex_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            arr <- [10, 20, 30, 40, 50]
            first <- arr[0]
            third <- arr[2]
            last <- arr[4]
            
            Assert.Equal(10, first)
            Assert.Equal(30, third)
            Assert.Equal(50, last)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 如果 Old8Lang 断言失败会抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ArrayElementAssignment_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            arr <- [1, 2, 3]
            arr[1] <- 99
            arr[2] <- 100
            
            Assert.Equal(1, arr[0])
            Assert.Equal(99, arr[1])
            Assert.Equal(100, arr[2])
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 如果 Old8Lang 断言失败会抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ArrayIndexOutOfBounds_ThrowsError()
    {
        // Arrange
        var code = @"
            arr <- [1, 2, 3]
            element <- arr[10]
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        Assert.Throws<Old8Exception>(() => compiledAction());
    }

    [Fact]
    public void ArrayConcatenation_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            arr1 <- [1, 2, 3]
            arr2 <- [4, 5, 6]
            combined <- arr1 + arr2
            
            Assert.Equal(6, Len(combined))
            Assert.Equal(1, combined[0])
            Assert.Equal(2, combined[1])
            Assert.Equal(3, combined[2])
            Assert.Equal(4, combined[3])
            Assert.Equal(5, combined[4])
            Assert.Equal(6, combined[5])
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 如果 Old8Lang 断言失败会抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void ArrayInForLoop_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            numbers <- [1, 2, 3, 4, 5]
            sum <- 0
            for num in numbers {
                sum <- sum + num
            }
            Assert.Equal(15.0, sum)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 如果 Old8Lang 断言失败会抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void NestedArrays_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            matrix <- [[1, 2], [3, 4]]
            firstRow <- matrix[0]
            element <- matrix[1][1]
            
            Assert.Equal(2, Len(matrix))
            Assert.Equal(2, Len(firstRow))
            Assert.Equal(1, firstRow[0])
            Assert.Equal(2, firstRow[1])
            Assert.Equal(4, element)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 如果 Old8Lang 断言失败会抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void NegativeArrayIndex_ThrowsError()
    {
        // Arrange
        var code = @"
            arr <- [1, 2, 3]
            element <- arr[-1]
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        Assert.Throws<Old8Exception>(() => compiledAction());
    }

    [Fact]
    public void ArrayLength_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            arr <- [1, 2, 3, 4, 5]
            length <- Len(arr)
            Assert.Equal(5.0, length)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 如果 Old8Lang 断言失败会抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void LargeArray_CompilesAndExecutesCorrectly()
    {
        // Arrange - 创建一个较小的测试数组以避免过大的代码
        var code = @"
            arr <- [0, 1, 2, 3, 4, 5, 6, 7, 8, 9]
            Assert.Equal(10, Len(arr))
            Assert.Equal(0, arr[0])
            Assert.Equal(9, arr[9])
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 如果 Old8Lang 断言失败会抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }
}