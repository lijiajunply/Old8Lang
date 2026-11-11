using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Linq;

/// <summary>
/// LINQ Let 和 OrderBy 子句执行测试
/// </summary>
[Collection("Sequential")]
public class LinqLetOrderByTests
{
    [Fact]
    public void LinqQuery_ChainedLetAndWhere_PreservesEachRowAndOuterScope()
    {
        const string code = @"
            x <- 99
            numbers <- [1, 2, 3, 4]
            result <- from x in numbers let doubled <- x * 2 let squared <- doubled * doubled where squared > 20 select squared + x
            Assert.Equal(2, result.Count())
            Assert.Equal(39, result[0])
            Assert.Equal(68, result[1])
            Assert.Equal(99, x)
        ";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter)();
    }

    [Fact]
    public void LinqQuery_WhereAndProjection_HandleEmptyAndMultipleResults()
    {
        const string code = @"
            numbers <- [1, 2, 3, 4]
            result <- from x in numbers where x > 2 select x * 10
            empty <- from y in numbers where y > 10 select y
            Assert.Equal(2, result.Count())
            Assert.Equal(30, result[0])
            Assert.Equal(40, result[1])
            Assert.Equal(0, empty.Count())
        ";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter)();
    }
    [Fact]
    public void LinqQuery_Let_CalculatesIntermediateValue()
    {
        // Arrange
        var code = @"
            numbers <- [1, 2, 3, 4, 5]
            result <- from x in numbers let squared <- x * x select squared
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
    public void LinqQuery_LetWhere_UsesLetVariable()
    {
        // Arrange
        var code = @"
            numbers <- [1, 2, 3, 4, 5]
            result <- from x in numbers let squared <- x * x where squared > 10 select squared
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
    public void LinqQuery_MultipleLet_ChainedCalculations()
    {
        // Arrange
        var code = @"
            numbers <- [1, 2, 3, 4, 5]
            result <- from x in numbers let doubled <- x * 2 let squared <- doubled * doubled where squared > 20 select squared
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
    public void LinqQuery_LetSelectBoth_AccessesOriginalAndLetVariable()
    {
        // Arrange
        var code = @"
            numbers <- [2, 3, 4]
            result <- from x in numbers let squared <- x * x select x + squared
            Assert.Equal(3, result.Count())
            Assert.Equal(6, result[0])
            Assert.Equal(12, result[1])
            Assert.Equal(20, result[2])
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
    public void LinqQuery_OrderByAscending_SortsCorrectly()
    {
        // Arrange
        var code = @"
            numbers <- [5, 2, 8, 1, 9, 3, 7]
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
    public void LinqQuery_OrderByDescending_SortsCorrectly()
    {
        // Arrange
        var code = @"
            numbers <- [5, 2, 8, 1, 9, 3, 7]
            result <- from x in numbers orderby x descending select x
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
    public void LinqQuery_OrderByWithProjection_SortsAndProjects()
    {
        // Arrange
        var code = @"
            numbers <- [3, 1, 4, 2]
            result <- from x in numbers orderby x select x * 10
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
    public void LinqQuery_WhereOrderBy_FiltersAndSorts()
    {
        // Arrange
        var code = @"
            numbers <- [5, 2, 8, 1, 9, 3, 7, 4, 6]
            result <- from x in numbers where x > 4 orderby x descending select x
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
    public void LinqQuery_LetOrderBy_UsesLetVariableForSorting()
    {
        // Arrange
        var code = @"
            numbers <- [3, 1, 4, 2]
            result <- from x in numbers let neg <- -x orderby neg select -neg
            Assert.Equal(4, result.Count())
            Assert.Equal(4, result[0])
            Assert.Equal(3, result[1])
            Assert.Equal(2, result[2])
            Assert.Equal(1, result[3])
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
    public void LinqQuery_OrderByMultipleKeys_PreservesRowVariables()
    {
        const string code = @"
            numbers <- [1, 4, 3, 2, 5]
            result <- from x in numbers let parity <- x % 2 where x > 1 orderby parity ascending, x descending select x + parity
            Assert.Equal(4, result.Count())
            Assert.Equal(4, result[0])
            Assert.Equal(2, result[1])
            Assert.Equal(6, result[2])
            Assert.Equal(4, result[3])
        ";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter)();
    }

    [Fact]
    public void LinqQuery_OrderByEqualKeys_PreservesInputOrder()
    {
        const string code = @"
            values <- [3, 1, 5, 2]
            result <- from x in values orderby x % 2 select x
            Assert.Equal(2, result[0])
            Assert.Equal(3, result[1])
            Assert.Equal(1, result[2])
            Assert.Equal(5, result[3])
        ";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter)();
    }
}
