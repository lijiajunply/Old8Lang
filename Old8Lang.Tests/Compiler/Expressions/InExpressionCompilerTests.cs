using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Expressions;

/// <summary>
/// In表达式编译模式测试
/// 测试in表达式和for-in循环在编译模式下的IL生成和执行
/// </summary>
[Collection("Sequential")]
public class InExpressionCompilerTests
{
    #region in表达式编译测试

    /// <summary>
    /// 测试in表达式在编译模式下的运行效果
    /// </summary>
    [Fact]
    public void CompileMode_InExpression_CompilesCorrectly()
    {
        // Arrange
        var interpreter = new LangInterpreter();
        var code = "a <- 1 in [1, 2, 3]";

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    /// <summary>
    /// 测试in表达式在数组上的编译
    /// </summary>
    [Fact]
    public void CompileMode_InExpression_Array_CompilesCorrectly()
    {
        // Arrange
        var interpreter = new LangInterpreter();
        var code = @"
            a <- 1 in [1, 2, 3]
            b <- 4 in [1, 2, 3]
        ";

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    /// <summary>
    /// 测试in表达式在字符串上的编译
    /// </summary>
    [Fact]
    public void CompileMode_InExpression_String_CompilesCorrectly()
    {
        // Arrange
        var interpreter = new LangInterpreter();
        var code = @"
            a <- 'a' in ""abc""
            b <- 'd' in ""abc""
        ";

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    /// <summary>
    /// 测试in表达式在字典上的编译
    /// </summary>
    [Fact]
    public void CompileMode_InExpression_Dictionary_CompilesCorrectly()
    {
        // Arrange
        var interpreter = new LangInterpreter();
        var code = @"
            a <- ""name"" in {""name"": ""test"", ""age"": 10}
            b <- ""gender"" in {""name"": ""test"", ""age"": 10}
        ";

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    /// <summary>
    /// 测试in表达式在范围上的编译
    /// </summary>
    [Fact]
    public void CompileMode_InExpression_Range_CompilesCorrectly()
    {
        // Arrange
        var interpreter = new LangInterpreter();
        var code = @"
            a <- 3 in [1~5]
            b <- 6 in [1~5]
        ";

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region for-in循环编译测试

    /// <summary>
    /// 测试for-in循环在编译模式下的运行效果
    /// </summary>
    [Fact]
    public void CompileMode_ForInLoop_CompilesCorrectly()
    {
        // Arrange
        var interpreter = new LangInterpreter();
        var code = "sum <- 0; for item in [1, 2, 3] { sum <- sum + item }";

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    /// <summary>
    /// 测试for-in循环在数组上的编译
    /// </summary>
    [Fact]
    public void CompileMode_ForInLoop_Array_CompilesCorrectly()
    {
        // Arrange
        var interpreter = new LangInterpreter();
        var code = @"
            sum <- 0
            for item in [1, 2, 3, 4, 5] {
                sum <- sum + item
            }
        ";

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    /// <summary>
    /// 测试for-in循环与in表达式组合的编译
    /// </summary>
    [Fact]
    public void CompileMode_ForInLoopAndInExpression_CompilesCorrectly()
    {
        // Arrange
        var interpreter = new LangInterpreter();
        var code = @"
            numbers <- [1, 2, 3, 4, 5]
            evenCount <- 0
            for num in numbers {
                if num in [2, 4, 6, 8, 10] {
                    evenCount <- evenCount + 1
                }
            }
        ";

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    /// <summary>
    /// 测试for-in循环在列表上的编译
    /// </summary>
    [Fact]
    public void CompileMode_ForInLoop_List_CompilesCorrectly()
    {
        // Arrange
        var interpreter = new LangInterpreter();
        var code = @"
            result <- 0
            for i in {1, 2, 3, 4, 5} {
                result <- result + i
            }
        ";

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    /// <summary>
    /// 测试for-in循环在范围上的编译
    /// </summary>
    [Fact]
    public void CompileMode_ForInLoop_Range_CompilesCorrectly()
    {
        // Arrange
        var interpreter = new LangInterpreter();
        var code = @"
            sum <- 0
            for i in [1~10] {
                sum <- sum + i
            }
        ";

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion
}
