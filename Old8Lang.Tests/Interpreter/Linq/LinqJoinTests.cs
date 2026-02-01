using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Interpreter.Linq;

/// <summary>
/// LINQ Join 子句测试
/// </summary>
[Collection("Sequential")]
public class LinqJoinTests
{
    [Fact]
    public void LinqQuery_InnerJoin_MatchesCorrectly()
    {
        // Arrange
        var code = @"
            list1 <- {1, 2, 3}
            list2 <- {2, 3, 4}

            result <- from x in list1 join y in list2 on x == y select x
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
        Assert.Equal(2, ((IntLangValue)list.Values[0]).Value);
        Assert.Equal(3, ((IntLangValue)list.Values[1]).Value);
    }

    [Fact]
    public void LinqQuery_InnerJoin_NoMatches_ReturnsEmpty()
    {
        // Arrange
        var code = @"
            list1 <- {1, 2, 3}
            list2 <- {4, 5, 6}

            result <- from x in list1 join y in list2 on x == y select x
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
    public void LinqQuery_InnerJoin_MultipleMatches()
    {
        // Arrange
        var code = @"
            list1 <- {1, 2, 2, 3}
            list2 <- {2, 2, 4}

            result <- from x in list1 join y in list2 on x == y select x
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
        // 2 matches from list1[1] with list2[0,1], 2 matches from list1[2] with list2[0,1]
        Assert.Equal(4, list.Values.Count);
    }

    [Fact]
    public void LinqQuery_GroupJoin_GroupsCorrectly()
    {
        // Arrange
        var code = @"
            categories <- {1, 2, 3}
            items <- {1, 1, 2, 2, 2, 3}

            result <- from c in categories join i in items on c == i into g select g.Count()
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
        Assert.Equal(2, ((IntLangValue)list.Values[0]).Value); // category 1 has 2 items
        Assert.Equal(3, ((IntLangValue)list.Values[1]).Value); // category 2 has 3 items
        Assert.Equal(1, ((IntLangValue)list.Values[2]).Value); // category 3 has 1 item
    }

    [Fact]
    public void LinqQuery_JoinWithWhere_FiltersAfterJoin()
    {
        // Arrange
        var code = @"
            list1 <- {1, 2, 3, 4, 5}
            list2 <- {2, 3, 4}

            result <- from x in list1 join y in list2 on x == y where x > 2 select x
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
        Assert.Equal(3, ((IntLangValue)list.Values[0]).Value);
        Assert.Equal(4, ((IntLangValue)list.Values[1]).Value);
    }
}
