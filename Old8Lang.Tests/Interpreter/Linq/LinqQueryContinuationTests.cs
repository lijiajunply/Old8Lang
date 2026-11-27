using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Interpreter.Linq;

/// <summary>
/// LINQ 查询延续（into）解释模式测试
/// 覆盖 select ... into、group ... into、延续中的 let/where/orderby 与多层链式延续
/// </summary>
[Collection("Sequential")]
public class LinqQueryContinuationTests
{
    [Fact]
    public void LinqContinuation_SelectIntoWhereSelect_TransformsAndFilters()
    {
        var code = @"
numbers <- {1, 2, 3, 4, 5}
result <- from x in numbers
          select x * 2 into doubled
          where doubled > 4
          select doubled + 1
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code).Run(interpreter.Manager);

        var list = Assert.IsType<ListLangValue>(interpreter.Manager.GetValue(new LangId("result")));
        Assert.Equal([7, 9, 11], list.Values.Cast<IntLangValue>().Select(v => v.Value).ToArray());
    }

    [Fact]
    public void LinqContinuation_GroupByIntoWithLetWhereOrderBy_EvaluatesPerGroup()
    {
        var code = @"
numbers <- {1, 2, 3, 4, 5, 6, 7}
result <- from n in numbers
          group n by n % 3 into g
          let total <- g[""Values""].Sum()
          where total >= 9
          orderby total descending
          select total
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code).Run(interpreter.Manager);

        // key 1: 1+4+7=12, key 2: 2+5=7 (filtered), key 0: 3+6=9 => descending: [12, 9]
        var list = Assert.IsType<ListLangValue>(interpreter.Manager.GetValue(new LangId("result")));
        Assert.Equal([12, 9], list.Values.Cast<IntLangValue>().Select(v => v.Value).ToArray());
    }

    [Fact]
    public void LinqContinuation_TwoStageQueryPipeline_ExecutesStagesInOrder()
    {
        var code = @"
numbers <- {1, 2, 3, 4, 5, 6}
stage1Result <- from n in numbers
                where n % 2 == 0
                select n * 3 into stage1
                where stage1 >= 12
                select stage1 - 2

result <- from s in stage1Result
          orderby s descending
          select s * 10 into finalVal
          select finalVal
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code).Run(interpreter.Manager);

        // even: 2,4,6 -> *3: 6,12,18 -> >=12: 12,18 -> -2: 10,16 -> desc: 16,10 -> *10: 160,100
        var list = Assert.IsType<ListLangValue>(interpreter.Manager.GetValue(new LangId("result")));
        Assert.Equal([160, 100], list.Values.Cast<IntLangValue>().Select(v => v.Value).ToArray());
    }

    [Fact]
    public void LinqContinuation_SelectIntoGroupBy_ChainsProjectionIntoGrouping()
    {
        var code = @"
words <- {""a"", ""bb"", ""c"", ""dd"", ""eee""}
grouped <- from w in words
           select w.Length into len
           group len by len

groupCount <- grouped.Count()
firstKey <- grouped[0][""Key""]
firstCount <- grouped[0][""Values""].Count()
secondKey <- grouped[1][""Key""]
secondCount <- grouped[1][""Values""].Count()
thirdKey <- grouped[2][""Key""]
thirdCount <- grouped[2][""Values""].Count()
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code).Run(interpreter.Manager);

        // lengths: 1,2,1,2,3 => keys 1(2), 2(2), 3(1)
        Assert.Equal(3, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("groupCount"))).Value);
        Assert.Equal(1, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("firstKey"))).Value);
        Assert.Equal(2, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("firstCount"))).Value);
        Assert.Equal(2, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("secondKey"))).Value);
        Assert.Equal(2, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("secondCount"))).Value);
        Assert.Equal(3, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("thirdKey"))).Value);
        Assert.Equal(1, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("thirdCount"))).Value);
    }

    [Fact]
    public void LinqContinuation_DoesNotLeakRangeVariablesToOuterScope()
    {
        var code = @"
y <- 999
numbers <- {1, 2, 3}
result <- from x in numbers
          select x + 10 into y
          select y * 2
outerY <- y
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code).Run(interpreter.Manager);

        var list = Assert.IsType<ListLangValue>(interpreter.Manager.GetValue(new LangId("result")));
        Assert.Equal([22, 24, 26], list.Values.Cast<IntLangValue>().Select(v => v.Value).ToArray());
        Assert.Equal(999, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("outerY"))).Value);
    }
}
