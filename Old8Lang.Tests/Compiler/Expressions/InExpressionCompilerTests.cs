using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Expressions;

/// <summary>
/// In表达式 IL 模式测试
/// 测试in表达式和for-in循环在 IL 模式下的IL生成和执行
/// </summary>
[Collection("Sequential")]
public class InExpressionCompilerTests
{
    #region in表达式编译测试

    /// <summary>
    /// 测试in表达式在 IL 模式下的运行效果
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
    /// 测试for-in循环在 IL 模式下的运行效果
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

    #region in 运算符的类型转换与拼接回归

    /// <summary>
    /// 列表的 in 必须按元素类型拆箱：左侧值在主入口统一装箱成 object，
    /// 直接传给 List&lt;T&gt;.Contains(T) 会生成无效 IL。
    /// </summary>
    [Fact]
    public void InExpression_ListWithValueTypeElements_UsesElementConversion()
    {
        var code = @"
            numbers <- {10, 20, 30}
            Assert.True(10 in numbers)
            Assert.False(25 in numbers)
        ";

        Assert.Null(CompileAndInvoke(code));
    }

    /// <summary>
    /// 字典的 in 必须按键类型拆箱；混合值类型的字典字面量尤其容易触发无效 IL。
    /// </summary>
    [Fact]
    public void InExpression_DictionaryWithStringKeys_UsesKeyConversion()
    {
        var code = @"
            data <- {""name"": ""Alice"", ""age"": 30}
            Assert.True(""name"" in data)
            Assert.False(""city"" in data)
        ";

        Assert.Null(CompileAndInvoke(code));
    }

    /// <summary>
    /// in 作为字符串拼接的右操作数：类型推断必须给出 bool。
    /// 此前推断成左操作数类型，会让 string.Concat(object, object) 收到未装箱的 bool。
    /// </summary>
    [Fact]
    public void InExpression_AsStringConcatOperand_DoesNotProduceInvalidIl()
    {
        var code = @"
            numbers <- {1, 2}
            text <- ""x"" + (1 in numbers)
            PrintLine(text)
        ";

        Assert.Null(CompileAndInvoke(code));
    }

    /// <summary>
    /// 字典混合值类型 + 拼接组合曾稳定生成无效 IL（JIT 直接拒绝）。
    /// </summary>
    [Fact]
    public void InExpression_DictionaryInsideConcat_DoesNotProduceInvalidIl()
    {
        var code = @"
            data <- {""a"": 1}
            text <- ""x"" + (""a"" in data)
            PrintLine(text)
        ";

        Assert.Null(CompileAndInvoke(code));
    }

    private static Exception? CompileAndInvoke(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        Old8Lang.Compiler.Compiler.IlVerificationEnabled = false;
        try
        {
            var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
            return Record.Exception(() => compiledAction());
        }
        finally
        {
            Old8Lang.Compiler.Compiler.IlVerificationEnabled = true;
        }
    }

    #endregion
}
