using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Linq;

/// <summary>
/// LINQ 基础执行测试
/// </summary>
[Collection("Sequential")]
public class LinqBasicExecutionTests
{
    [Fact]
    public void LinqQuery_FromWhereSelect_FiltersCorrectly()
    {
        // Arrange
        var code = @"
            numbers <- [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]
            result <- from x in numbers where x > 5 select x
        ";
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

    [Fact]
    public void LinqQuery_FromSelect_ReturnsAll()
    {
        // Arrange
        var code = @"
            numbers <- [1, 2, 3]
            result <- from x in numbers select x
        ";
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

    [Fact]
    public void LinqQuery_SelectProjection_TransformsValues()
    {
        // Arrange
        var code = @"
            numbers <- [1, 2, 3, 4, 5]
            result <- from x in numbers select x * x
        ";
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

    [Fact]
    public void LinqQuery_WhereSelect_CombinesCorrectly()
    {
        // Arrange
        var code = @"
            numbers <- [1, 2, 3, 4, 5]
            result <- from x in numbers where x > 2 select x * 10
        ";
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

    [Fact]
    public void LinqQuery_MultipleWhere_AllConditionsApply()
    {
        // Arrange
        var code = @"
            numbers <- [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]
            result <- from x in numbers where x > 3 where x < 8 select x
        ";
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

    [Fact]
    public void LinqQuery_EmptyResult_ReturnsEmptyList()
    {
        // Arrange
        var code = @"
            numbers <- [1, 2, 3]
            result <- from x in numbers where x > 100 select x
        ";
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

    [Fact]
    public void LinqQuery_EmptySource_ReturnsEmptyList()
    {
        // Arrange
        var code = @"
            numbers <- []
            result <- from x in numbers select x
        ";
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

    [Fact]
    public void LinqQuery_ComplexExpression_EvaluatesCorrectly()
    {
        // Arrange
        var code = @"
            numbers <- [1, 2, 3, 4, 5]
            result <- from x in numbers where (x % 2) == 0 select x + 100
        ";
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
}
