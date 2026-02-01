using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Interpreter.Linq;

/// <summary>
/// 列表高级方法测试
/// </summary>
[Collection("Sequential")]
public class ListAdvancedMethodsTests
{
    #region First/Last 方法测试

    [Fact]
    public void List_First_ReturnsFirstElement()
    {
        var code = @"
            list <- {1, 2, 3, 4, 5}
            result <- list.First()
        ";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(1, ((IntLangValue)result).Value);
    }

    [Fact]
    public void List_FirstWithPredicate_ReturnsFirstMatch()
    {
        var code = @"
            list <- {1, 2, 3, 4, 5}
            result <- list.First((x:int) -> x > 3)
        ";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(4, ((IntLangValue)result).Value);
    }

    [Fact]
    public void List_FirstOrDefault_ReturnsNullForEmpty()
    {
        var code = @"
            list <- {}
            result <- list.FirstOrDefault()
        ";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<NullLangValue>(result);
    }

    [Fact]
    public void List_Last_ReturnsLastElement()
    {
        var code = @"
            list <- {1, 2, 3, 4, 5}
            result <- list.Last()
        ";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(5, ((IntLangValue)result).Value);
    }

    [Fact]
    public void List_LastWithPredicate_ReturnsLastMatch()
    {
        var code = @"
            list <- {1, 2, 3, 4, 5}
            result <- list.Last((x:int) -> x < 4)
        ";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(3, ((IntLangValue)result).Value);
    }

    #endregion

    #region 集合操作测试

    [Fact]
    public void List_Union_CombinesWithoutDuplicates()
    {
        var code = @"
            list1 <- {1, 2, 3}
            list2 <- {3, 4, 5}
            result <- list1.Union(list2)
        ";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<ListLangValue>(result);
        var list = (ListLangValue)result;
        Assert.Equal(5, list.Values.Count);
    }

    [Fact]
    public void List_Intersect_ReturnsCommonElements()
    {
        var code = @"
            list1 <- {1, 2, 3, 4}
            list2 <- {3, 4, 5, 6}
            result <- list1.Intersect(list2)
        ";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<ListLangValue>(result);
        var list = (ListLangValue)result;
        Assert.Equal(2, list.Values.Count);
        Assert.Equal(3, ((IntLangValue)list.Values[0]).Value);
        Assert.Equal(4, ((IntLangValue)list.Values[1]).Value);
    }

    [Fact]
    public void List_Except_ReturnsDifference()
    {
        var code = @"
            list1 <- {1, 2, 3, 4}
            list2 <- {3, 4, 5, 6}
            result <- list1.Except(list2)
        ";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<ListLangValue>(result);
        var list = (ListLangValue)result;
        Assert.Equal(2, list.Values.Count);
        Assert.Equal(1, ((IntLangValue)list.Values[0]).Value);
        Assert.Equal(2, ((IntLangValue)list.Values[1]).Value);
    }

    #endregion

    #region 聚合方法测试

    [Fact]
    public void List_Sum_CalculatesSum()
    {
        var code = @"
            list <- {1, 2, 3, 4, 5}
            result <- list.Sum()
        ";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(15, ((IntLangValue)result).Value);
    }

    [Fact]
    public void List_Average_CalculatesAverage()
    {
        var code = @"
            list <- {1, 2, 3, 4, 5}
            result <- list.Average()
        ";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<DoubleLangValue>(result);
        Assert.Equal(3.0, ((DoubleLangValue)result).Value);
    }

    [Fact]
    public void List_Min_ReturnsMinimum()
    {
        var code = @"
            list <- {5, 2, 8, 1, 9}
            result <- list.Min()
        ";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(1, ((IntLangValue)result).Value);
    }

    [Fact]
    public void List_Max_ReturnsMaximum()
    {
        var code = @"
            list <- {5, 2, 8, 1, 9}
            result <- list.Max()
        ";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(9, ((IntLangValue)result).Value);
    }

    #endregion

    #region GroupBy/TakeWhile/SkipWhile 测试

    [Fact]
    public void List_GroupBy_GroupsCorrectly()
    {
        var code = @"
            list <- {1, 2, 3, 4, 5, 6}
            result <- list.GroupBy((x:int) -> x % 2)
        ";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<DictionaryLangValue>(result);
        var dict = (DictionaryLangValue)result;
        Assert.Equal(2, dict.Tuples.Count); // 两组：奇数和偶数
    }

    [Fact]
    public void List_TakeWhile_TakesWhileConditionTrue()
    {
        var code = @"
            list <- {1, 2, 3, 4, 5, 1, 2}
            result <- list.TakeWhile((x:int) -> x < 4)
        ";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<ListLangValue>(result);
        var list = (ListLangValue)result;
        Assert.Equal(3, list.Values.Count);
        Assert.Equal(1, ((IntLangValue)list.Values[0]).Value);
        Assert.Equal(2, ((IntLangValue)list.Values[1]).Value);
        Assert.Equal(3, ((IntLangValue)list.Values[2]).Value);
    }

    [Fact]
    public void List_SkipWhile_SkipsWhileConditionTrue()
    {
        var code = @"
            list <- {1, 2, 3, 4, 5, 1, 2}
            result <- list.SkipWhile((x:int) -> x < 4)
        ";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<ListLangValue>(result);
        var list = (ListLangValue)result;
        Assert.Equal(4, list.Values.Count);
        Assert.Equal(4, ((IntLangValue)list.Values[0]).Value);
    }

    #endregion

    #region Zip/SelectMany/FlatMap 测试

    [Fact]
    public void List_Zip_CombinesTwoLists()
    {
        var code = @"
            list1 <- {1, 2, 3}
            list2 <- {""a"", ""b"", ""c""}
            result <- list1.Zip(list2)
        ";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<ListLangValue>(result);
        var list = (ListLangValue)result;
        Assert.Equal(3, list.Values.Count);
        Assert.IsType<TupleLangValue>(list.Values[0]);
    }

    [Fact]
    public void List_Flatten_FlattensNestedList()
    {
        var code = @"
            list <- {{1, 2}, {3, 4}, {5}}
            result <- list.Flatten()
        ";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<ListLangValue>(result);
        var list = (ListLangValue)result;
        Assert.Equal(5, list.Values.Count);
    }

    [Fact]
    public void List_SelectMany_FlattensWithSelector()
    {
        var code = @"
            list <- {1, 2, 3}
            makeList <- (x:int) -> {
                return {x, x * 10}
            }
            result <- list.SelectMany(makeList)
        ";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<ListLangValue>(result);
        var list = (ListLangValue)result;
        Assert.Equal(6, list.Values.Count);
        Assert.Equal(1, ((IntLangValue)list.Values[0]).Value);
        Assert.Equal(10, ((IntLangValue)list.Values[1]).Value);
    }

    #endregion

    #region 其他高级方法测试

    [Fact]
    public void List_Chunk_SplitsIntoChunks()
    {
        var code = @"
            list <- {1, 2, 3, 4, 5, 6, 7}
            result <- list.Chunk(3)
        ";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<ListLangValue>(result);
        var list = (ListLangValue)result;
        Assert.Equal(3, list.Values.Count); // {1,2,3}, {4,5,6}, {7}
    }

    [Fact]
    public void List_TakeLast_ReturnsLastN()
    {
        var code = @"
            list <- {1, 2, 3, 4, 5}
            result <- list.TakeLast(3)
        ";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<ListLangValue>(result);
        var list = (ListLangValue)result;
        Assert.Equal(3, list.Values.Count);
        Assert.Equal(3, ((IntLangValue)list.Values[0]).Value);
        Assert.Equal(4, ((IntLangValue)list.Values[1]).Value);
        Assert.Equal(5, ((IntLangValue)list.Values[2]).Value);
    }

    [Fact]
    public void List_SkipLast_SkipsLastN()
    {
        var code = @"
            list <- {1, 2, 3, 4, 5}
            result <- list.SkipLast(2)
        ";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<ListLangValue>(result);
        var list = (ListLangValue)result;
        Assert.Equal(3, list.Values.Count);
        Assert.Equal(1, ((IntLangValue)list.Values[0]).Value);
        Assert.Equal(2, ((IntLangValue)list.Values[1]).Value);
        Assert.Equal(3, ((IntLangValue)list.Values[2]).Value);
    }

    [Fact]
    public void List_WithIndex_AddsIndices()
    {
        var code = @"
            list <- {""a"", ""b"", ""c""}
            result <- list.WithIndex()
        ";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<ListLangValue>(result);
        var list = (ListLangValue)result;
        Assert.Equal(3, list.Values.Count);
        Assert.IsType<TupleLangValue>(list.Values[0]);
    }

    [Fact]
    public void List_Partition_SplitsByCondition()
    {
        var code = @"
            list <- {1, 2, 3, 4, 5, 6}
            result <- list.Partition((x:int) -> x % 2 == 0)
        ";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<TupleLangValue>(result);
        var tuple = (TupleLangValue)result;
        Assert.Equal(2, tuple.ItemValues.Count);

        var evens = (ListLangValue)tuple.ItemValues[0];
        var odds = (ListLangValue)tuple.ItemValues[1];
        Assert.Equal(3, evens.Values.Count); // 2, 4, 6
        Assert.Equal(3, odds.Values.Count);  // 1, 3, 5
    }

    #endregion
}
