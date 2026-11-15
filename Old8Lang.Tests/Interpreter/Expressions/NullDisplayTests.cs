using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Interpreter.Expressions;

/// <summary>
/// 解释器：null 的显示形式统一为小写 <c>null</c>。
/// </summary>
/// <remarks>
/// <see cref="NullLangValue.ToString"/> 原返回 <c>Null</c>，与 IL/虚拟机、字符串转换
/// （<c>Converse(String)</c> 返回 <c>"null"</c>）以及字面量写法都不一致。
/// 2026-11-15 统一为 <c>null</c>；类型名仍由 <c>TypeToString()</c> 返回 <c>Null</c>。
/// </remarks>
[Collection("Sequential")]
public class NullDisplayTests
{
    [Fact]
    public void Null_ToStrAndInterpolation_UseLowercaseNull()
    {
        const string code = @"
x <- null
asString <- x.ToStr()
interpolated <- $""v={x}""
";

        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        Assert.Equal("null", Assert.IsType<StringLangValue>(
            interpreter.Manager.GetValue(new LangId("asString"))).Value);
        Assert.Equal("v=null", Assert.IsType<StringLangValue>(
            interpreter.Manager.GetValue(new LangId("interpolated"))).Value);
        // 类型名不受影响
        Assert.Equal("Null", new NullLangValue().TypeToString());
    }
}
