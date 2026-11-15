using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Interpreter.Expressions;

/// <summary>
/// 解释器：范围字面量在端点相等且被排除时的结果。
/// </summary>
/// <remarks>
/// 方向应由原始端点决定，排除端点只收窄区间：
/// <c>[5>~<5]</c> 表示开区间 (5, 5)，应为空。此前解释器与 IL 各自实现了一遍区间算法，
/// 都先把边界改成 6~4 再判成降序，错误地产生 <c>[6, 5, 4]</c>；现在共用
/// <c>RangeLangValue.CreateRangeArray</c>，三种模式结果一致。
/// </remarks>
[Collection("Sequential")]
public class RangeBoundaryTests
{
    [Theory]
    [InlineData("[5>~<5]", 0)]
    [InlineData("[5>~5]", 0)]
    [InlineData("[5~<5]", 0)]
    [InlineData("[5~5]", 1)]
    [InlineData("[1~<4]", 3)]
    [InlineData("[1>~4]", 3)]
    [InlineData("[10~1]", 10)]
    public void EqualAndExcludedEndpoints_ProduceExpectedLength(string expression, int expectedLength)
    {
        var code = $"range <- {expression}";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var value = interpreter.Manager.GetValue(new LangId("range"));
        var list = Assert.IsAssignableFrom<ILangList>(value);
        Assert.Equal(expectedLength, list.GetLength());
    }
}
