using Old8Lang.Syntax;

namespace Old8Lang.Tests.Architecture;

/// <summary>
/// 验证标量字面量的解析数据可在不加载 Runtime 的 Syntax 程序集中生成。
/// </summary>
public class ScalarLiteralSyntaxTests
{
    [Fact]
    public void SyntaxAssembly_DoesNotReferenceRuntime()
    {
        var references = typeof(LiteralSyntax).Assembly.GetReferencedAssemblies().Select(assembly => assembly.Name).ToArray();

        Assert.DoesNotContain("Old8Lang.Runtime", references);
        Assert.Contains("Old8Lang.Core", references);
    }

    [Theory]
    [InlineData("42", LiteralKind.Int, 42)]
    [InlineData("2147483648", LiteralKind.Double, 2147483648d)]
    [InlineData("1.25e2", LiteralKind.Double, 125d)]
    public void IntegerSyntax_PreservesExistingNumericClassification(string text, LiteralKind kind, object expected)
    {
        var parsed = LiteralSyntax.TryParseInteger(text, new SourcePosition(3, 5), out var literal);

        Assert.True(parsed);
        Assert.NotNull(literal);
        Assert.Equal(kind, literal.Kind);
        Assert.Equal(expected, literal.LiteralValue);
        Assert.Equal(new SourcePosition(3, 5), literal.Position);
    }

    [Fact]
    public void CharSyntax_DecodesEscapedCharacterWithoutRuntimeValue()
    {
        var literal = LiteralSyntax.Char("'\\n'", new SourcePosition(1, 2));

        Assert.Equal(LiteralKind.Char, literal.Kind);
        Assert.Equal('\n', literal.LiteralValue);
    }
}
