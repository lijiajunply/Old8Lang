using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Interpreter.Linq;

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
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<ListLangValue>(result);

        var list = (ListLangValue)result;
        Assert.Single(list.Values);
        Assert.Equal(84, ((IntLangValue)list.Values[0]).Value);
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
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<ListLangValue>(result);

        var list = (ListLangValue)result;
        Assert.Empty(list.Values);
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
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<ListLangValue>(result);

        var list = (ListLangValue)result;
        Assert.Equal(5, list.Values.Count);
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
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<ListLangValue>(result);

        var list = (ListLangValue)result;
        Assert.Equal(10, list.Values.Count);
        Assert.Equal(100, ((IntLangValue)list.Values[0]).Value);
        Assert.Equal(1000, ((IntLangValue)list.Values[9]).Value);
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
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<ListLangValue>(result);

        var list = (ListLangValue)result;
        Assert.Equal(3, list.Values.Count);
        Assert.Equal(0, ((IntLangValue)list.Values[0]).Value);
        Assert.Equal(1, ((IntLangValue)list.Values[1]).Value);
        Assert.Equal(2, ((IntLangValue)list.Values[2]).Value);
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
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<ListLangValue>(result);

        var list = (ListLangValue)result;
        Assert.Equal(3, list.Values.Count);
        Assert.Equal(5, ((IntLangValue)list.Values[0]).Value);
        Assert.Equal(3, ((IntLangValue)list.Values[1]).Value);
        Assert.Equal(1, ((IntLangValue)list.Values[2]).Value);
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
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<ListLangValue>(result);

        var list = (ListLangValue)result;
        Assert.Equal(4, list.Values.Count);
        Assert.Equal(3, ((IntLangValue)list.Values[0]).Value);
        Assert.Equal(3, ((IntLangValue)list.Values[1]).Value);
        Assert.Equal(3, ((IntLangValue)list.Values[2]).Value);
        Assert.Equal(4, ((IntLangValue)list.Values[3]).Value);
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
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<ListLangValue>(result);

        var list = (ListLangValue)result;
        Assert.Equal(4, list.Values.Count);
        Assert.All(list.Values, v => Assert.Equal(5, ((IntLangValue)v).Value));
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
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<ListLangValue>(result);

        var list = (ListLangValue)result;
        Assert.Equal(3, list.Values.Count);
        Assert.Equal(16, ((IntLangValue)list.Values[0]).Value);  // 1*2*2*2*2
        Assert.Equal(32, ((IntLangValue)list.Values[1]).Value);  // 2*2*2*2*2
        Assert.Equal(48, ((IntLangValue)list.Values[2]).Value);  // 3*2*2*2*2
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
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<ListLangValue>(result);

        var list = (ListLangValue)result;
        Assert.Equal(3, list.Values.Count);

        // 检查外部变量 x 没有被改变
        var check = interpreter.Manager.GetValue(new LangId("check"));
        Assert.Equal(999, ((IntLangValue)check).Value);
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
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<ListLangValue>(result);

        var list = (ListLangValue)result;
        Assert.Equal(3, list.Values.Count);
        Assert.All(list.Values, v => Assert.Equal(42, ((IntLangValue)v).Value));
    }
}
