using Old8Lang.Error;
using Old8Lang.Interpreter;
using Old8Lang.LangParser;

namespace Old8Lang.Tests.Parser.Advanced;

/// <summary>
/// 字符串模板错误测试
/// </summary>
[Collection("Sequential")]
public class StringTemplateTests
{
    #region 字符串模板错误

    /// <summary>
    /// 测试字符串模板缺少右花括号
    /// </summary>
    [Fact]
    public void ParseProgram_StringTemplateMissingRightBrace_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- $\"Hello {name\"";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试字符串模板花括号内为空
    /// </summary>
    [Fact]
    public void ParseProgram_StringTemplateEmptyBraces_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- $\"Hello {}\"";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion
}
