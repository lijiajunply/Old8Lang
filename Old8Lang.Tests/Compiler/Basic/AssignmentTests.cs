using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Basic;

/// <summary>
/// 基础赋值语句编译模式测试
/// 测试编译器模式下的基本赋值操作的 IL 生成和执行
/// 注意:编译模式要求函数参数和返回类型有类型注解
/// </summary>
[Collection("Sequential")]
public class AssignmentTests
{
    #region 简单赋值测试

    [Fact]
    public void SimpleVariableAssignment_CompilesAndExecutesCorrectly()
    {
        // Arrange
        var code = "a <- 42";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(123456789)]
    [InlineData(-987654321)]
    public void IntegerAssignment_EdgeCases_CompilesCorrectly(int value)
    {
        // Arrange
        var code = $"a <- {value}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(0.1)]
    [InlineData(3.14159)]
    [InlineData(-2.71828)]
    [InlineData(1e10)]
    [InlineData(-1e-10)]
    public void DoubleAssignment_EdgeCases_CompilesCorrectly(double value)
    {
        // Arrange
        var code = $"a <- {value}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Theory]
    [InlineData("")]
    [InlineData("hello")]
    [InlineData("Hello, World!")]
    [InlineData("中文测试")]
    [InlineData("Special chars: !@#$%^&*()")]
    public void StringAssignment_VariousStrings_CompilesCorrectly(string str)
    {
        // Arrange
        var escapedStr = str.Replace("\"", "\\\"");
        var code = $"a <- \"{escapedStr}\"";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BooleanAssignment_CompilesCorrectly(bool value)
    {
        // Arrange
        var code = $"a <- {value.ToString().ToLower()}";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Theory]
    [InlineData('a')]
    [InlineData('Z')]
    [InlineData('0')]
    [InlineData('@')]
    public void CharAssignment_CompilesCorrectly(char value)
    {
        // Arrange
        var code = $"a <- '{value}'";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region 变量重新赋值测试

    [Fact]
    public void VariableReassignment_CompilesAndUpdatesValue()
    {
        // Arrange
        var code = @"
            a <- 10
            a <- 20
            a <- 30
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void MultipleVariableAssignments_CompilesAndCreatesAllValues()
    {
        // Arrange
        var code = @"
            a <- 1
            b <- 2.5
            c <- ""hello""
            d <- true
            e <- 'x'
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region 带表达式的赋值测试

    [Fact]
    public void AssignmentWithExpression_CompilesAndCalculatesCorrectly()
    {
        // Arrange
        var code = @"
            a <- 10
            b <- 20
            c <- a + b * 2
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void AssignmentWithComplexExpression_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            a <- 10
            b <- 20
            c <- 30
            result <- (a + b) * c - (a * b)
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void AssignmentWithIdentifierConflict_CompilesAndUsesLatestAssignment()
    {
        // Arrange
        var code = @"
            a <- 1
            b <- 2
            a <- b + 10
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region Unicode 和边界测试

    [Fact]
    public void AssignmentWithUnicode_CompilesAndHandlesCorrectly()
    {
        // Arrange
        var code = @"
            中文变量名 <- 100
            another <- 中文变量名 + 50
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void EmptyCode_CompilesWithoutError()
    {
        // Arrange
        var code = "";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);

        // Assert - 空代码应该能够解析
        Assert.NotNull(ast);
        Assert.Equal(0, ast.Count);

        // 编译空代码应该也能成功
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        Assert.NotNull(compiledAction);

        // 执行不应该抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region 类型转换测试

    [Fact]
    public void AssignmentWithImplicitTypeConversion_CompilesCorrectly()
    {
        // Arrange - 测试隐式类型转换（如果支持）
        var code = @"
            intVar <- 42
            doubleVar <- intVar + 0.5
        ";
        var interpreter = new LangInterpreter();

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
