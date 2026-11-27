using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Interpreter.Linq;

/// <summary>
/// LINQ 查询语法 group by 解释模式测试
/// 覆盖基础分组、元素投影、分组聚合、多键分组、分组前/后过滤及边界情况
/// </summary>
[Collection("Sequential")]
public class LinqGroupByTests
{
    [Fact]
    public void LinqGroupBy_BasicGroupingByModulo_ProducesKeyAndValuesDictionaries()
    {
        var code = @"
numbers <- {1, 2, 3, 4, 5, 6}
groups <- from x in numbers group x by x % 2

groupCount <- groups.Count()
oddKey <- groups[0][""Key""]
oddValues <- groups[0][""Values""]
evenKey <- groups[1][""Key""]
evenValues <- groups[1][""Values""]
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code).Run(interpreter.Manager);

        Assert.Equal(2, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("groupCount"))).Value);
        Assert.Equal(1, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("oddKey"))).Value);
        Assert.Equal(0, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("evenKey"))).Value);

        var oddValues = Assert.IsType<ListLangValue>(interpreter.Manager.GetValue(new LangId("oddValues")));
        Assert.Equal([1, 3, 5], oddValues.Values.Cast<IntLangValue>().Select(v => v.Value).ToArray());

        var evenValues = Assert.IsType<ListLangValue>(interpreter.Manager.GetValue(new LangId("evenValues")));
        Assert.Equal([2, 4, 6], evenValues.Values.Cast<IntLangValue>().Select(v => v.Value).ToArray());
    }

    [Fact]
    public void LinqGroupBy_WithElementProjection_ProjectsElementsInsideEachGroup()
    {
        var code = @"
numbers <- {1, 2, 3, 4}
groups <- from x in numbers group x * 10 by x % 2

oddProjected <- groups[0][""Values""]
evenProjected <- groups[1][""Values""]
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code).Run(interpreter.Manager);

        var oddProjected = Assert.IsType<ListLangValue>(interpreter.Manager.GetValue(new LangId("oddProjected")));
        Assert.Equal([10, 30], oddProjected.Values.Cast<IntLangValue>().Select(v => v.Value).ToArray());

        var evenProjected = Assert.IsType<ListLangValue>(interpreter.Manager.GetValue(new LangId("evenProjected")));
        Assert.Equal([20, 40], evenProjected.Values.Cast<IntLangValue>().Select(v => v.Value).ToArray());
    }

    [Fact]
    public void LinqGroupBy_AggregationsCountSumMax_ComputeCorrectly()
    {
        var code = @"
class Sale {
    public category:string <- """"
    public amount:int <- 0
    func init(cat:string, amt:int) {
        this.category <- cat
        this.amount <- amt
    }
}

class GroupStats {
    public cat:string <- """"
    public count:int <- 0
    public sum:int <- 0
    public max:int <- 0
    func init(cat:string, count:int, sum:int, max:int) {
        this.cat <- cat
        this.count <- count
        this.sum <- sum
        this.max <- max
    }
}

sales <- {
    Sale(""book"", 30),
    Sale(""toy"", 20),
    Sale(""book"", 50),
    Sale(""toy"", 80),
    Sale(""book"", 40)
}

summary <- from s in sales
           group s.amount by s.category into g
           select GroupStats(g[""Key""], g[""Values""].Count(), g[""Values""].Sum(), g[""Values""].Max())

bookCount <- summary[0].count
bookSum <- summary[0].sum
bookMax <- summary[0].max
toyCount <- summary[1].count
toySum <- summary[1].sum
toyMax <- summary[1].max
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code).Run(interpreter.Manager);

        Assert.Equal(3, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("bookCount"))).Value);
        Assert.Equal(120, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("bookSum"))).Value);
        Assert.Equal(50, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("bookMax"))).Value);
        Assert.Equal(2, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("toyCount"))).Value);
        Assert.Equal(100, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("toySum"))).Value);
        Assert.Equal(80, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("toyMax"))).Value);
    }

    [Fact]
    public void LinqGroupBy_MultiKeyCompositeExpression_GroupsByMultipleFields()
    {
        var code = @"
class Employee {
    public dept:string <- """"
    public level:int <- 0
    public name:string <- """"
    func init(d:string, l:int, n:string) {
        this.dept <- d
        this.level <- l
        this.name <- n
    }
}

staff <- {
    Employee(""eng"", 1, ""Alice""),
    Employee(""eng"", 2, ""Bob""),
    Employee(""eng"", 1, ""Carol""),
    Employee(""hr"", 1, ""Dave"")
}

groups <- from e in staff group e.name by (e.dept + "":"" + e.level.ToStr())

groupCount <- groups.Count()
firstGroupNames <- groups[0][""Values""]
secondGroupNames <- groups[1][""Values""]
thirdGroupNames <- groups[2][""Values""]
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code).Run(interpreter.Manager);

        Assert.Equal(3, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("groupCount"))).Value);
        var g0 = Assert.IsType<ListLangValue>(interpreter.Manager.GetValue(new LangId("firstGroupNames")));
        var g1 = Assert.IsType<ListLangValue>(interpreter.Manager.GetValue(new LangId("secondGroupNames")));
        var g2 = Assert.IsType<ListLangValue>(interpreter.Manager.GetValue(new LangId("thirdGroupNames")));
        Assert.Equal(["Alice", "Carol"], g0.Values.Cast<StringLangValue>().Select(v => v.Value).ToArray());
        Assert.Equal(["Bob"], g1.Values.Cast<StringLangValue>().Select(v => v.Value).ToArray());
        Assert.Equal(["Dave"], g2.Values.Cast<StringLangValue>().Select(v => v.Value).ToArray());
    }

    [Fact]
    public void LinqGroupBy_FilterBeforeAndAfterGrouping_WorksCorrectly()
    {
        var code = @"
numbers <- {1, 2, 3, 4, 5, 6, 7, 8, 9}
// 先过滤 > 2，再按 mod 3 分组，最后只保留元素个数 >= 2 的组并按 Key 排序
result <- from n in numbers
          where n > 2
          group n by n % 3 into g
          where g[""Values""].Count() >= 2
          orderby g[""Key""] ascending
          select g[""Key""]
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code).Run(interpreter.Manager);

        // > 2: {3,4,5,6,7,8,9} => key 0: {3,6,9} (3), key 1: {4,7} (2), key 2: {5,8} (2)
        var list = Assert.IsType<ListLangValue>(interpreter.Manager.GetValue(new LangId("result")));
        Assert.Equal([0, 1, 2], list.Values.Cast<IntLangValue>().Select(v => v.Value).ToArray());
    }

    [Fact]
    public void LinqGroupBy_EmptySource_ReturnsEmptyList()
    {
        var code = @"
empty <- {}
groups <- from x in empty group x by x
count <- groups.Count()
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code).Run(interpreter.Manager);

        Assert.Equal(0, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("count"))).Value);
    }
}
