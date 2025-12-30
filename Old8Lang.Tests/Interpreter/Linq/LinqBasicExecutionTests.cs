using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Interpreter.Linq;

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
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<ListLangValue>(result);

        var list = (ListLangValue)result;
        Assert.Equal(5, list.Values.Count);
        Assert.Equal(6, ((IntLangValue)list.Values[0]).Value);
        Assert.Equal(7, ((IntLangValue)list.Values[1]).Value);
        Assert.Equal(10, ((IntLangValue)list.Values[4]).Value);
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
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<ListLangValue>(result);

        var list = (ListLangValue)result;
        Assert.Equal(3, list.Values.Count);
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
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<ListLangValue>(result);

        var list = (ListLangValue)result;
        Assert.Equal(3, list.Values.Count);
        Assert.Equal(30, ((IntLangValue)list.Values[0]).Value);
        Assert.Equal(40, ((IntLangValue)list.Values[1]).Value);
        Assert.Equal(50, ((IntLangValue)list.Values[2]).Value);
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
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<ListLangValue>(result);

        var list = (ListLangValue)result;
        Assert.Equal(4, list.Values.Count);
        Assert.Equal(4, ((IntLangValue)list.Values[0]).Value);
        Assert.Equal(5, ((IntLangValue)list.Values[1]).Value);
        Assert.Equal(6, ((IntLangValue)list.Values[2]).Value);
        Assert.Equal(7, ((IntLangValue)list.Values[3]).Value);
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
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<ListLangValue>(result);

        var list = (ListLangValue)result;
        Assert.Empty(list.Values);
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
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<ListLangValue>(result);

        var list = (ListLangValue)result;
        Assert.Empty(list.Values);
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
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<ListLangValue>(result);

        var list = (ListLangValue)result;
        Assert.Equal(2, list.Values.Count);
        Assert.Equal(102, ((IntLangValue)list.Values[0]).Value);
        Assert.Equal(104, ((IntLangValue)list.Values[1]).Value);
    }
}
