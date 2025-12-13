using Old8Lang.Error;
using Old8Lang.LangParser;

namespace Old8Lang.Tests.Language;

/// <summary>
/// 括号匹配测试，测试各种括号（圆括号、方括号、大括号）的匹配错误
/// </summary>
[Collection("Sequential")]
public class BracketMatchingTests
{
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
