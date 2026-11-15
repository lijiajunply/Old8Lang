using Old8Lang.Error;
using Old8Lang.Interpreter;

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
    /// 插值表达式里出现同种引号时词法只截到第一个引号，模板值以 `{` 结尾；
    /// 解析器必须报语法错误而不是原地死循环。
    /// </summary>
    [Fact]
    public void ParseProgram_StringTemplateNestedQuotes_ThrowsSyntaxErrorInsteadOfHanging()
    {
        // Arrange
        var code = "PrintLine($\"s={\"ab\"}\")";

        // Act & Assert
        var exception = Assert.ThrowsAny<SyntaxError>(() =>
            new LangInterpreter().Build(code));
        Assert.Contains("花括号", exception.Message);
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
