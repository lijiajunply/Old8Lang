using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Interpreter.Linq;

/// <summary>
/// LINQ Let 和 OrderBy 子句执行测试
/// </summary>
[Collection("Sequential")]
public class LinqLetOrderByTests
{
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
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<ListLangValue>(result);

        var list = (ListLangValue)result;
        Assert.Equal(5, list.Values.Count);
        Assert.Equal(1, ((IntLangValue)list.Values[0]).Value);
        Assert.Equal(4, ((IntLangValue)list.Values[1]).Value);
        Assert.Equal(9, ((IntLangValue)list.Values[2]).Value);
        Assert.Equal(16, ((IntLangValue)list.Values[3]).Value);
        Assert.Equal(25, ((IntLangValue)list.Values[4]).Value);
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
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<ListLangValue>(result);

        var list = (ListLangValue)result;
        Assert.Equal(2, list.Values.Count);
        Assert.Equal(16, ((IntLangValue)list.Values[0]).Value);
        Assert.Equal(25, ((IntLangValue)list.Values[1]).Value);
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
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<ListLangValue>(result);

        var list = (ListLangValue)result;
        Assert.Equal(3, list.Values.Count);
        Assert.Equal(36, ((IntLangValue)list.Values[0]).Value);  // (3*2)^2 = 36
        Assert.Equal(64, ((IntLangValue)list.Values[1]).Value);  // (4*2)^2 = 64
        Assert.Equal(100, ((IntLangValue)list.Values[2]).Value); // (5*2)^2 = 100
    }

    [Fact]
    public void LinqQuery_LetSelectBoth_AccessesOriginalAndLetVariable()
    {
        // Arrange
        var code = @"
            numbers <- [2, 3, 4]
            result <- from x in numbers let squared <- x * x select x + squared
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<ListLangValue>(result);

        var list = (ListLangValue)result;
        Assert.Equal(3, list.Values.Count);
        Assert.Equal(6, ((IntLangValue)list.Values[0]).Value);   // 2 + 4
        Assert.Equal(12, ((IntLangValue)list.Values[1]).Value);  // 3 + 9
        Assert.Equal(20, ((IntLangValue)list.Values[2]).Value);  // 4 + 16
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
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<ListLangValue>(result);

        var list = (ListLangValue)result;
        Assert.Equal(7, list.Values.Count);
        Assert.Equal(1, ((IntLangValue)list.Values[0]).Value);
        Assert.Equal(2, ((IntLangValue)list.Values[1]).Value);
        Assert.Equal(3, ((IntLangValue)list.Values[2]).Value);
        Assert.Equal(5, ((IntLangValue)list.Values[3]).Value);
        Assert.Equal(7, ((IntLangValue)list.Values[4]).Value);
        Assert.Equal(8, ((IntLangValue)list.Values[5]).Value);
        Assert.Equal(9, ((IntLangValue)list.Values[6]).Value);
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
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<ListLangValue>(result);

        var list = (ListLangValue)result;
        Assert.Equal(7, list.Values.Count);
        Assert.Equal(9, ((IntLangValue)list.Values[0]).Value);
        Assert.Equal(8, ((IntLangValue)list.Values[1]).Value);
        Assert.Equal(7, ((IntLangValue)list.Values[2]).Value);
        Assert.Equal(5, ((IntLangValue)list.Values[3]).Value);
        Assert.Equal(3, ((IntLangValue)list.Values[4]).Value);
        Assert.Equal(2, ((IntLangValue)list.Values[5]).Value);
        Assert.Equal(1, ((IntLangValue)list.Values[6]).Value);
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
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<ListLangValue>(result);

        var list = (ListLangValue)result;
        Assert.Equal(4, list.Values.Count);
        Assert.Equal(10, ((IntLangValue)list.Values[0]).Value);
        Assert.Equal(20, ((IntLangValue)list.Values[1]).Value);
        Assert.Equal(30, ((IntLangValue)list.Values[2]).Value);
        Assert.Equal(40, ((IntLangValue)list.Values[3]).Value);
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
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<ListLangValue>(result);

        var list = (ListLangValue)result;
        Assert.Equal(5, list.Values.Count);
        Assert.Equal(9, ((IntLangValue)list.Values[0]).Value);
        Assert.Equal(8, ((IntLangValue)list.Values[1]).Value);
        Assert.Equal(7, ((IntLangValue)list.Values[2]).Value);
        Assert.Equal(6, ((IntLangValue)list.Values[3]).Value);
        Assert.Equal(5, ((IntLangValue)list.Values[4]).Value);
    }

    [Fact]
    public void LinqQuery_LetOrderBy_UsesLetVariableForSorting()
    {
        // Arrange
        var code = @"
            numbers <- [3, 1, 4, 2]
            result <- from x in numbers let neg <- -x orderby neg select x
        ";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<ListLangValue>(result);

        var list = (ListLangValue)result;
        Assert.Equal(4, list.Values.Count);
        // 排序依据 neg（-x），所以 -4 < -3 < -2 < -1，对应 x 的顺序是 4, 3, 2, 1
        Assert.Equal(4, ((IntLangValue)list.Values[0]).Value);
        Assert.Equal(3, ((IntLangValue)list.Values[1]).Value);
        Assert.Equal(2, ((IntLangValue)list.Values[2]).Value);
        Assert.Equal(1, ((IntLangValue)list.Values[3]).Value);
    }
}
