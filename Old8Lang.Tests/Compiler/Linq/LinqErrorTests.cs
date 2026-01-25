using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Linq;

/// <summary>
/// LINQ 错误处理测试
/// </summary>
[Collection("Sequential")]
public class LinqErrorTests
{
    [Fact]
    public void LinqQuery_UndefinedVariableInWhere_ThrowsError()
    {
        // Arrange
        var code = @"
            numbers <- [1, 2, 3]
            result <- from x in numbers where undefinedVar > 5 select x
        ";
        var interpreter = new LangInterpreter();

        // Act & Assert
        var ast = interpreter.Build(code);
        Assert.Throws<NameError>(() => ast.Run(interpreter.Manager));
    }

    [Fact]
    public void LinqQuery_UndefinedVariableInSelect_ThrowsError()
    {
        // Arrange
        var code = @"
            numbers <- [1, 2, 3]
            result <- from x in numbers select undefinedVar
        ";
        var interpreter = new LangInterpreter();

        // Act & Assert
        var ast = interpreter.Build(code);
        Assert.Throws<NameError>(() => ast.Run(interpreter.Manager));
    }

    [Fact]
    public void LinqQuery_InvalidDataSource_ThrowsError()
    {
        // Arrange
        var code = @"
            notAList <- 42
            result <- from x in notAList select x
        ";
        var interpreter = new LangInterpreter();

        // Act & Assert
        var ast = interpreter.Build(code);
        Assert.Throws<InvalidOperationError>(() => ast.Run(interpreter.Manager));
    }

    [Fact]
    public void LinqQuery_DivisionByZeroInSelect_ThrowsError()
    {
        // Arrange
        var code = @"
            numbers <- [1, 2, 0, 3]
            result <- from x in numbers select 10 / x
        ";
        var interpreter = new LangInterpreter();

        // Act & Assert
        var ast = interpreter.Build(code);
        Assert.Throws<ZeroDivisionError>(() => ast.Run(interpreter.Manager));
    }

    [Fact]
    public void LinqQuery_DivisionByZeroInWhere_ThrowsError()
    {
        // Arrange
        var code = @"
            numbers <- [1, 2, 0, 3]
            result <- from x in numbers where (10 / x) > 0 select x
        ";
        var interpreter = new LangInterpreter();

        // Act & Assert
        var ast = interpreter.Build(code);
        Assert.Throws<ZeroDivisionError>(() => ast.Run(interpreter.Manager));
    }

    [Fact]
    public void LinqQuery_InvalidLetExpression_ThrowsError()
    {
        // Arrange
        var code = @"
            numbers <- [1, 2, 3]
            result <- from x in numbers let y <- x / 0 select y
        ";
        var interpreter = new LangInterpreter();

        // Act & Assert
        var ast = interpreter.Build(code);
        Assert.Throws<ZeroDivisionError>(() => ast.Run(interpreter.Manager));
    }

    [Fact]
    public void LinqQuery_TypeMismatchInComparison_ThrowsError()
    {
        // Arrange
        var code = @"
            numbers <- [1, 2, 3]
            result <- from x in numbers where x > ""string"" select x
        ";
        var interpreter = new LangInterpreter();

        // Act & Assert
        var ast = interpreter.Build(code);
        Assert.Throws<TypeError>(() => ast.Run(interpreter.Manager));
    }

    [Fact]
    public void LinqQuery_UndefinedDataSource_ThrowsError()
    {
        // Arrange
        var code = @"
            result <- from x in undefinedList select x
        ";
        var interpreter = new LangInterpreter();

        // Act & Assert
        var ast = interpreter.Build(code);
        Assert.Throws<NameError>(() => ast.Run(interpreter.Manager));
    }

    [Fact]
    public void LinqQuery_LetVariableUsedBeforeDefinition_WorksCorrectly()
    {
        // Arrange - let 变量只在 let 之后可用
        var code = @"
            numbers <- [1, 2, 3]
            // 这应该失败，因为 squared 还没有定义
            result <- from x in numbers where squared > 10 let squared <- x * x select squared
        ";
        var interpreter = new LangInterpreter();

        // Act & Assert
        var ast = interpreter.Build(code);
        Assert.Throws<NameError>(() => ast.Run(interpreter.Manager));
    }

    [Fact]
    public void LinqQuery_OrderByUndefinedVariable_ThrowsError()
    {
        // Arrange
        var code = @"
            numbers <- [1, 2, 3]
            result <- from x in numbers orderby undefinedVar select x
        ";
        var interpreter = new LangInterpreter();

        // Act & Assert
        var ast = interpreter.Build(code);
        Assert.Throws<NameError>(() => ast.Run(interpreter.Manager));
    }

    [Fact]
    public void LinqQuery_NullDataSource_ThrowsError()
    {
        // Arrange
        var code = @"
            nullValue <- null
            result <- from x in nullValue select x
        ";
        var interpreter = new LangInterpreter();

        // Act & Assert
        var ast = interpreter.Build(code);
        Assert.Throws<InvalidOperationError>(() => ast.Run(interpreter.Manager));
    }

    [Fact]
    public void LinqQuery_InvalidOperationInLet_ThrowsError()
    {
        // Arrange
        var code = @"
            numbers <- [1, 2, 3]
            result <- from x in numbers let y <- x.NonExistentMethod() select y
        ";
        var interpreter = new LangInterpreter();

        // Act & Assert
        var ast = interpreter.Build(code);
        Assert.Throws<AttributeError>(() => ast.Run(interpreter.Manager));
    }

    [Fact]
    public void LinqQuery_ChainedErrorInMultipleWhere_ThrowsError()
    {
        // Arrange
        var code = @"
            numbers <- [1, 2, 0, 3]
            result <- from x in numbers where x >= 0 where 10 / x > 5 select x
        ";
        var interpreter = new LangInterpreter();

        // Act & Assert - 第二个 where 会遇到除零错误（因为 0 通过了第一个 where）
        var ast = interpreter.Build(code);
        Assert.Throws<ZeroDivisionError>(() => ast.Run(interpreter.Manager));
    }
}
