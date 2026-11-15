using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Interpreter.Collections;

/// <summary>
/// 解释器：字典缺失键统一抛 KEY_ERROR（2026-11-15 语义统一）。
/// </summary>
/// <remarks>
/// 此前解释器 `d["missing"]` 返回 null，而 IL/VM 报错；`GetOrElse` 才是安全访问。
/// </remarks>
[Collection("Sequential")]
public class DictionaryMissingKeyTests
{
    [Fact]
    public void MissingKey_IndexAccess_ThrowsKeyError()
    {
        const string code = @"
d <- {""a"": 1}
value <- d[""missing""]
";

        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        var exception = Assert.Throws<KeyError>(() => ast.Run(interpreter.Manager));
        Assert.Contains("missing", exception.Message);
    }

    [Fact]
    public void MissingKey_GetValue_ThrowsKeyError()
    {
        const string code = @"
d <- {""a"": 1}
value <- d.GetValue(""missing"")
";

        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        var exception = Assert.Throws<KeyError>(() => ast.Run(interpreter.Manager));
        Assert.Contains("missing", exception.Message);
    }

    [Fact]
    public void GetOrElse_OnMissingKey_StillReturnsDefault()
    {
        const string code = @"
d <- {""a"": 1}
value <- d.GetOrElse(""missing"", 42)
";

        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        Assert.Equal(42, Assert.IsType<IntLangValue>(
            interpreter.Manager.GetValue(new LangId("value"))).Value);
    }
}
