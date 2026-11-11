using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using Xunit;

namespace Old8Lang.Tests.Interpreter.Collections;

public class NegativeIndexErrorTests
{
    [Fact]
    public void Collections_ReportOriginalOutOfRangeIndex()
    {
        var values = new List<LangValueType> { new IntLangValue(1), new IntLangValue(2), new IntLangValue(3) };
        var list = new ListLangValue(values);
        var array = new ArrayLangValue(values);
        var text = new StringLangValue("abc");
        var tuple = new TupleLangValue(new List<LangExpression>(values));
        tuple.Run(new VariateManager());
        var index = new IntLangValue(-5);

        Assert.Contains("索引 -5", Assert.Throws<IndexError>(() => list.Get(index)).Message);
        Assert.Contains("索引 -5", Assert.Throws<IndexError>(() => list.Set(index, values[0])).Message);
        Assert.Contains("索引 -5", Assert.Throws<IndexError>(() => array.Get(index)).Message);
        Assert.Contains("索引 -5", Assert.Throws<IndexError>(() => array.Set(index, values[0])).Message);
        Assert.Contains("索引 -5", Assert.Throws<IndexError>(() => text.Get(index)).Message);
        Assert.Contains("越界: -5", Assert.Throws<InvalidOperationError>(() => tuple.Get(-5)).Message);
        Assert.Contains("越界: -5", Assert.Throws<InvalidOperationError>(() => tuple.Set(-5, values[0])).Message);

        Assert.Equal(3, Assert.IsType<IntLangValue>(list.Get(new IntLangValue(-1))).Value);
        Assert.Equal(3, Assert.IsType<IntLangValue>(array.Get(new IntLangValue(-1))).Value);
        Assert.Equal('c', Assert.IsType<CharLangValue>(text.Get(new IntLangValue(-1))).Value);
        Assert.Equal(3, Assert.IsType<IntLangValue>(tuple.Get(-1)).Value);
    }
}
