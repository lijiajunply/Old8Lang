using Old8Lang.Error;
using Old8Lang.Interpreter;
using Old8Lang.LangParser;
using Old8Lang.LangParser.ParserHelpers;

namespace Old8Lang.Tests.Parser;

/// <summary>
/// 括号匹配测试，测试各种括号（圆括号、方括号、大括号）的匹配错误
/// </summary>
[Collection("Sequential")]
public class BracketMatchingTests
{
    [Theory]
    [InlineData(LangTokenType.LeftBrace, "左大括号 '{'")]
    [InlineData(LangTokenType.RightBrace, "右大括号 '}'")]
    public void BraceErrorTemplate_FormatsLiteralAndActualToken(LangTokenType expected, string literal)
    {
        var message = ExpectHelper.GetDetailedMessage(expected, LangTokenType.Identifier, "actual");
        Assert.Contains(literal, message);
        Assert.Contains("actual", message);
    }

    [Fact]
    public void ClassWithInvalidInheritanceSyntax_ReportsExpectedBrace()
    {
        const string code = "class Dog : Animal {}";
        var error = Assert.Throws<SyntaxError>(() => new LangInterpreter().Build(code));
        Assert.Contains("缺少左大括号 '{'", error.Message);
        Assert.DoesNotContain("correct format", error.Message);
    }
    /// <summary>
    /// 测试括号不匹配 - 缺少右括号
    /// </summary>
    [Fact]
    public void ParseProgram_MissingRightParenthesis_ThrowsSyntaxError()
    {
        // Arrange
        var code = "if (a > 5 { PrintLine(\"Hello\") }";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试括号不匹配 - 缺少左括号
    /// </summary>
    [Fact]
    public void ParseProgram_MissingLeftParenthesis_ThrowsSyntaxError()
    {
        // Arrange
        var code = "if a > 5) { PrintLine(\"Hello\") }";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试大括号不匹配 - 缺少右大括号
    /// </summary>
    [Fact]
    public void ParseProgram_MissingRightBrace_ThrowsSyntaxError()
    {
        // Arrange
        var code = "if (a > 5) { PrintLine(\"Hello\")";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试大括号不匹配 - 缺少左大括号
    /// </summary>
    [Fact]
    public void ParseProgram_MissingLeftBrace_ThrowsSyntaxError()
    {
        // Arrange
        var code = "if (a > 5) PrintLine(\"Hello\")}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试方括号不匹配 - 缺少右方括号
    /// </summary>
    [Fact]
    public void ParseProgram_MissingRightBracket_ThrowsSyntaxError()
    {
        // Arrange
        var code = "array[0 <- 10";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试方括号不匹配 - 缺少左方括号
    /// </summary>
    [Fact]
    public void ParseProgram_MissingLeftBracket_ThrowsSyntaxError()
    {
        // Arrange
        var code = "array]0] <- 10";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }
}
