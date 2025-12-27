using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
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
        var code = "arr <- []";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("arr"));
        Assert.NotNull(result);
        Assert.IsType<ArrayLangValue>(result);
        Assert.Equal(0, ((ArrayLangValue)result).RunResult.Length);
    }

    [Fact]
    public void SingleElementArray_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = "arr <- [42]";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("arr"));
        Assert.NotNull(result);
        Assert.IsType<ArrayLangValue>(result);
        var arrayValue = ((ArrayLangValue)result).RunResult;
        Assert.Equal(1, arrayValue.Length);
        Assert.Equal(42, ((IntLangValue)arrayValue[0]).Value);
    }

    [Fact]
    public void MultiElementArray_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = "arr <- [1, 2, 3, 4, 5]";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("arr"));
        Assert.NotNull(result);
        Assert.IsType<ArrayLangValue>(result);
        var arrayValue = ((ArrayLangValue)result).RunResult;
        Assert.Equal(5, arrayValue.Length);
        
        for (int i = 0; i < 5; i++)
        {
            Assert.Equal(i + 1, ((IntLangValue)arrayValue[i]).Value);
        }
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
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        var first = interpreter.Manager.GetValue(new LangId("first"));
        Assert.IsType<IntLangValue>(first);
        Assert.Equal(10, ((IntLangValue)first).Value);

        var third = interpreter.Manager.GetValue(new LangId("third"));
        Assert.IsType<IntLangValue>(third);
        Assert.Equal(30, ((IntLangValue)third).Value);

        var last = interpreter.Manager.GetValue(new LangId("last"));
        Assert.IsType<IntLangValue>(last);
        Assert.Equal(50, ((IntLangValue)last).Value);
    }

    [Fact]
    public void ArrayElementAssignment_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            arr <- [1, 2, 3]
            arr[1] <- 99
            arr[2] <- 100
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("arr"));
        Assert.NotNull(result);
        Assert.IsType<ArrayLangValue>(result);
        var arrayValue = ((ArrayLangValue)result).RunResult;
        Assert.Equal(3, arrayValue.Length);
        Assert.Equal(1, ((IntLangValue)arrayValue[0]).Value);
        Assert.Equal(99, ((IntLangValue)arrayValue[1]).Value);
        Assert.Equal(100, ((IntLangValue)arrayValue[2]).Value);
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
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("combined"));
        Assert.NotNull(result);
        Assert.IsType<ArrayLangValue>(result);
        var arrayValue = ((ArrayLangValue)result).RunResult;
        Assert.Equal(6, arrayValue.Length);
        
        for (int i = 0; i < 6; i++)
        {
            Assert.Equal(i + 1, ((IntLangValue)arrayValue[i]).Value);
        }
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
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        var sum = interpreter.Manager.GetValue(new LangId("sum"));
        Assert.IsType<DoubleLangValue>(sum);
        Assert.Equal(15.0, ((DoubleLangValue)sum).Value, 2);
    }

    [Fact]
    public void NestedArrays_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = @"
            matrix <- [[1, 2], [3, 4]]
            firstRow <- matrix[0]
            element <- matrix[1][1]
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        var matrix = interpreter.Manager.GetValue(new LangId("matrix"));
        Assert.IsType<ArrayLangValue>(matrix);
        var matrixValue = ((ArrayLangValue)matrix).RunResult;
        Assert.Equal(2, matrixValue.Length);

        var firstRow = interpreter.Manager.GetValue(new LangId("firstRow"));
        Assert.IsType<ArrayLangValue>(firstRow);
        var firstRowValue = ((ArrayLangValue)firstRow).RunResult;
        Assert.Equal(2, firstRowValue.Length);
        Assert.Equal(1, ((IntLangValue)firstRowValue[0]).Value);
        Assert.Equal(2, ((IntLangValue)firstRowValue[1]).Value);

        var element = interpreter.Manager.GetValue(new LangId("element"));
        Assert.IsType<IntLangValue>(element);
        Assert.Equal(4, ((IntLangValue)element).Value);
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
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        var length = interpreter.Manager.GetValue(new LangId("length"));
        Assert.IsType<DoubleLangValue>(length);
        Assert.Equal(5.0, ((DoubleLangValue)length).Value, 2);
    }

    [Fact]
    public void LargeArray_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = "arr <- [0";
        for (int i = 1; i < 1000; i++)
        {
            code += $", {i}";
        }
        code += "]";
        
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        compiledAction();

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("arr"));
        Assert.NotNull(result);
        Assert.IsType<ArrayLangValue>(result);
        var arrayValue = ((ArrayLangValue)result).RunResult;
        Assert.Equal(1000, arrayValue.Length);
        Assert.Equal(0, ((IntLangValue)arrayValue[0]).Value);
        Assert.Equal(999, ((IntLangValue)arrayValue[999]).Value);
    }
}