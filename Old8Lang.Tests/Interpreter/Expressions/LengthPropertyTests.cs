using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Interpreter.Expressions;

/// <summary>
/// 解释器：<c>.Length</c> 属性（2026-11-15 起与 IL / 虚拟机一致）。
/// </summary>
/// <remarks>
/// 数组/列表/字符串/字典的 <c>.Length</c> 等价 <c>len(x)</c>；此前解释器报
/// <c>NAME_ERROR: 名称 'Length' 未定义</c>，而 IL 与虚拟机已支持。
/// 字典上 <c>.Length</c> 是长度，键名为 <c>"Length"</c> 时用 <c>d["Length"]</c> 访问。
/// </remarks>
[Collection("Sequential")]
public class LengthPropertyTests
{
    [Theory]
    [InlineData("array <- [1, 2, 3]", "array", 3)]
    [InlineData("list <- {1, 2}", "list", 2)]
    [InlineData("text <- \"abc\"", "text", 3)]
    [InlineData("dict <- {\"a\": 1, \"b\": 2}", "dict", 2)]
    public void LengthProperty_EqualsLen(string declaration, string name, int expected)
    {
        var code = declaration + $@"
lengthValue <- {name}.Length
lenValue <- len({name})
";

        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        Assert.Equal(expected, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("lengthValue"))).Value);
        Assert.Equal(expected, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("lenValue"))).Value);
    }

    [Fact]
    public void LengthProperty_OnDictionary_DoesNotHideKeyAccess()
    {
        const string code = @"
dict <- {""Length"": 42}
byProperty <- dict.Length
byKey <- dict[""Length""]
";

        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        Assert.Equal(1, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("byProperty"))).Value);
        Assert.Equal(42, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("byKey"))).Value);
    }
}
