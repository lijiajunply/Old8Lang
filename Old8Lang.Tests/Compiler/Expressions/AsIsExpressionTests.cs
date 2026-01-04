using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Expressions;

/// <summary>
/// as 和 is 表达式编译模式测试
/// 测试编译器模式下 as 和 is 操作符的 IL 生成和执行
/// </summary>
[Collection("Sequential")]
public class AsIsExpressionTests
{
    #region as 表达式测试 - 编译模式

    [Fact]
    public void AsExpression_ValidConversion_IntToDouble_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            intValue <- 42
            result <- intValue as double
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
    public void AsExpression_ValidConversion_DoubleToInt_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            doubleValue <- 3.14
            result <- doubleValue as int
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
    public void AsExpression_StringToInt_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            stringValue <- ""123""
            result <- stringValue as int
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
    public void AsExpression_InvalidConversion_ReturnsNull_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            stringValue <- ""abc""
            result <- stringValue as int
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
    public void AsExpression_BoolToInt_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            boolValue <- true
            result <- boolValue as int
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
    public void AsExpression_NestedInComplexExpression_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            value <- ""42""
            result <- (value as int) * 2 + 10
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

    #region is 表达式测试 - 编译模式

    [Fact]
    public void IsExpression_IntCheck_IsInt_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            value <- 42
            result <- value is int
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
    public void IsExpression_DoubleCheck_IsDouble_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            value <- 3.14
            result <- value is double
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
    public void IsExpression_StringCheck_IsString_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            value <- ""hello""
            result <- value is string
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
    public void IsExpression_BoolCheck_IsBool_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            value <- true
            result <- value is bool
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
    public void IsExpression_TypeMismatch_IsDifferentType_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            value <- 42
            result <- value is string
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
    public void IsExpression_InConditional_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            value <- 42
            result <- """"
            if value is int {
                result <- ""It's an integer""
            } else {
                result <- ""It's not an integer""
            }
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
    public void IsExpression_ComplexLogic_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            value <- ""hello""
            result1 <- value is string
            result2 <- value is int
            result3 <- (value is string) and (value is not int)
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

    #region 混合表达式测试 - 编译模式

    [Fact]
    public void AsIsExpression_MixedUsage_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            value <- ""42""
            isInt <- value is int
            asInt <- value as int
            isNotNull <- asInt is not null
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
    public void AsIsExpression_ChainedOperations_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            stringValue <- ""123""
            intValue <- stringValue as int
            doubleValue <- intValue as double
            isString <- stringValue is string
            isDouble <- doubleValue is double
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
    public void AsIsExpression_InLoop_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            values <- [""1"", ""2"", ""abc"", ""4""]
            sum <- 0
            for item in values {
                if item is int {
                    sum <- sum + (item as int)
                } else {
                    intItem <- item as int
                    if intItem is not null {
                        sum <- sum + intItem
                    }
                }
            }
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

    #region 边界情况测试 - 编译模式

    [Fact]
    public void AsExpression_NullValue_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            value <- null
            result <- value as string
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
    public void IsExpression_NullValue_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            value <- null
            result1 <- value is string
            result2 <- value is int
            result3 <- value is null
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
    public void AsExpression_UnknownType_CompilesCorrectly()
    {
        // Arrange
        var code = @"
            value <- 42
            result <- value as unknown
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