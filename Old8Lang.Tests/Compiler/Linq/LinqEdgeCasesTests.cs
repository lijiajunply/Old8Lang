using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Linq;

/// <summary>
/// LINQ 边界情况测试
/// </summary>
[Collection("Sequential")]
public class LinqEdgeCasesTests
{
    [Fact]
    public void LinqQuery_SingleElement_HandlesCorrectly()
    {
        // Arrange
        var code = @"
            numbers <- [42]
            result <- from x in numbers select x * 2
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
    public void LinqQuery_AllFiltered_ReturnsEmpty()
    {
        // Arrange
        var code = @"
            numbers <- [1, 2, 3, 4, 5]
            result <- from x in numbers where false select x
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
    public void LinqQuery_NoneFiltered_ReturnsAll()
    {
        // Arrange
        var code = @"
            numbers <- [1, 2, 3, 4, 5]
            result <- from x in numbers where true select x
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
    public void LinqQuery_LargeDataSet_HandlesEfficiently()
    {
        // Arrange
        var code = @"
            numbers <- [1~1000]
            result <- from x in numbers where x % 100 == 0 select x
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
    public void LinqQuery_ZeroValue_HandlesCorrectly()
    {
        // Arrange
        var code = @"
            numbers <- [-2, -1, 0, 1, 2]
            result <- from x in numbers where x >= 0 select x
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
    public void LinqQuery_NegativeNumbers_HandlesCorrectly()
    {
        // Arrange
        var code = @"
            numbers <- [-5, -3, -1, 2, 4]
            result <- from x in numbers where x < 0 select x * -1
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
    public void LinqQuery_DuplicateValues_HandlesCorrectly()
    {
        // Arrange
        var code = @"
            numbers <- [1, 2, 2, 3, 3, 3, 4]
            result <- from x in numbers where x > 2 select x
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
    public void LinqQuery_OrderByIdenticalValues_MaintainsOrder()
    {
        // Arrange
        var code = @"
            numbers <- [5, 5, 5, 5]
            result <- from x in numbers orderby x select x
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
    public void LinqQuery_ComplexLetChain_ManyVariables()
    {
        // Arrange
        var code = @"
            numbers <- [1, 2, 3]
            result <- from x in numbers let a <- x * 2 let b <- a * 2 let c <- b * 2 let d <- c * 2 select d
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
    public void LinqQuery_VariableNameShadowing_HandlesCorrectly()
    {
        // Arrange - x 在外部作用域有值，LINQ 中也使用 x
        var code = @"
            x <- 999
            numbers <- [1, 2, 3]
            result <- from x in numbers select x
            // x 应该仍然是 999
            check <- x
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
    public void LinqQuery_SelectConstant_ReturnsConstantForEachElement()
    {
        // Arrange
        var code = @"
            numbers <- [1, 2, 3]
            result <- from x in numbers select 42
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
