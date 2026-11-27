using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Interpreter.EdgeCases;

/// <summary>
/// 解析器错误测试 - 覆盖不完整语句、非法表达式、错误信息准确性与后续解析恢复
/// </summary>
[Collection("Sequential")]
public class ParserErrorTests
{
    [Theory]
    [InlineData("a <-")]
    [InlineData("if x > 0 {")]
    [InlineData("func f(a:int { return a }")]
    [InlineData("class Broken {")]
    [InlineData("arr <- [1, 2")]
    [InlineData("dict <- {\"a\": 1")]
    [InlineData("enum { Red, Green }")]
    [InlineData("1 + * 2")]
    public void Parser_IncompleteOrMalformedSyntax_ThrowsSyntaxError(string invalidCode)
    {
        var interpreter = new LangInterpreter();

        var ex = Assert.ThrowsAny<SyntaxError>(() => interpreter.Build(invalidCode));
        Assert.False(string.IsNullOrWhiteSpace(ex.Message));
    }

    [Fact]
    public void Parser_UnclosedStringTemplateBrace_ThrowsSyntaxErrorWithoutHanging()
    {
        var interpreter = new LangInterpreter();

        var ex = Assert.Throws<SyntaxError>(() => interpreter.Build("s <- $\"value={x\""));
        Assert.Contains("SYNTAX_ERROR", ex.Message);
    }

    [Fact]
    public void Parser_ErrorLocationOnSpecificLine_ReportsAccuratePositionAndMessage()
    {
        var code = @"
a <- 1
b <- 2
c <- (1 + 2
d <- 4
";
        var interpreter = new LangInterpreter();

        var ex = Assert.ThrowsAny<SyntaxError>(() => interpreter.Build(code));
        Assert.Contains("SYNTAX_ERROR", ex.Message);
        Assert.True(ex.Position.Line >= 4, $"期望错误行号 >= 4，实际为 {ex.Position.Line}");
    }

    [Fact]
    public void Parser_AfterSyntaxError_SubsequentValidCodeParsesAndRunsCleanly()
    {
        var interpreter = new LangInterpreter();

        Assert.ThrowsAny<SyntaxError>(() => interpreter.Build("class Unclosed { public x <- 1"));

        var validAst = interpreter.Build("ok <- 21 * 2");
        validAst.Run(interpreter.Manager);

        Assert.Equal(42, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("ok"))).Value);
    }

    [Fact]
    public void Parser_MissingFunctionBodyAndInvalidForHeader_ThrowsSyntaxError()
    {
        var interpreter = new LangInterpreter();

        Assert.ThrowsAny<SyntaxError>(() => interpreter.Build("func noBody(x:int) -> int"));
        Assert.ThrowsAny<SyntaxError>(() => interpreter.Build("for <- 0, i < 10, i++ {}"));
    }
}
