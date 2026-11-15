using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Interpreter.Collections;

/// <summary>
/// 解释器：数组 / 列表的 <c>+</c> 按顺序拼接。
/// </summary>
/// <remarks>
/// 2026-11-15 起 <c>+</c> 对同类型集合做拼接（此前解释器报 INVALID_OPERATION_ERROR，
/// 只有虚拟机支持数组拼接）。数组 + 数组得到数组，列表 + 列表得到列表。
/// </remarks>
[Collection("Sequential")]
public class CollectionConcatTests
{
    [Fact]
    public void ArrayPlusArray_ConcatenatesInOrder()
    {
        const string code = @"
left <- [1, 2]
right <- [3, 4]
joined <- left + right
";

        var (values, interpreter) = Run(code);

        var joined = Assert.IsType<ArrayLangValue>(values["joined"]);
        Assert.Equal(4, joined.GetLength());
        Assert.Equal("[1, 2, 3, 4]", joined.ToString());
        // 原数组不被修改
        Assert.Equal(2, Assert.IsType<ArrayLangValue>(values["left"]).GetLength());
    }

    [Fact]
    public void ListPlusList_ConcatenatesInOrder()
    {
        const string code = @"
left <- {1, 2}
right <- {3, 4}
joined <- left + right
";

        var (values, _) = Run(code);

        var joined = Assert.IsType<ListLangValue>(values["joined"]);
        Assert.Equal(4, joined.GetLength());
        Assert.Equal("[1, 2, 3, 4]", joined.ToString());
        Assert.Equal(2, Assert.IsType<ListLangValue>(values["left"]).GetLength());
    }

    [Fact]
    public void MixedArrayAndList_StillReportsInvalidOperation()
    {
        const string code = @"
left <- [1, 2]
right <- {3, 4}
joined <- left + right
";

        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        var exception = Record.Exception(() => ast.Run(interpreter.Manager));
        Assert.NotNull(exception);
        Assert.Contains("加法", exception.Message);
    }

    private static (Dictionary<string, LangValueType> Values, LangInterpreter Interpreter) Run(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var values = new Dictionary<string, LangValueType>();
        foreach (var name in new[] { "left", "right", "joined" })
        {
            values[name] = interpreter.Manager.GetValue(new LangId(name));
        }

        return (values, interpreter);
    }
}
