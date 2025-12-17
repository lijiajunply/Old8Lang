using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Parser.Expression;

/// <summary>
/// 比较、逻辑和三元表达式错误验证测试
/// </summary>
[Collection("Sequential")]
public class ComparisonLogicalTests
{
    #region 比较表达式错误

    /// <summary>
    /// 测试不完整的比较表达式
    /// </summary>
    [Fact]
    public void ParseProgram_IncompleteComparison_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- x >";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试不完整的等于表达式
    /// </summary>
    [Fact]
    public void ParseProgram_IncompleteEquals_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- x ==";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试不完整的不等于表达式
    /// </summary>
    [Fact]
    public void ParseProgram_IncompleteNotEquals_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- x !=";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion

    #region 逻辑表达式错误

    /// <summary>
    /// 测试不完整的and表达式
    /// </summary>
    [Fact]
    public void ParseProgram_IncompleteAnd_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- true and";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试不完整的or表达式
    /// </summary>
    [Fact]
    public void ParseProgram_IncompleteOr_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- false or";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试不完整的xor表达式
    /// </summary>
    [Fact]
    public void ParseProgram_IncompleteXor_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- true xor";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion

    #region 三元表达式错误

    /// <summary>
    /// 测试三元表达式缺少问号
    /// </summary>
    [Fact]
    public void ParseProgram_TernaryMissingQuestionMark_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- condition 1 : 2";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试三元表达式缺少冒号
    /// </summary>
    [Fact]
    public void ParseProgram_TernaryMissingColon_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- condition ? 1";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试三元表达式缺少条件
    /// </summary>
    [Fact]
    public void ParseProgram_TernaryMissingCondition_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- ? 1 : 2";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试三元表达式缺少真值
    /// </summary>
    [Fact]
    public void ParseProgram_TernaryMissingTrueValue_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- condition ? : 2";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试三元表达式缺少假值
    /// </summary>
    [Fact]
    public void ParseProgram_TernaryMissingFalseValue_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- condition ? 1 :";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion

    #region 类型转换错误

    /// <summary>
    /// 测试as后缺少类型
    /// </summary>
    [Fact]
    public void ParseProgram_AsStatementMissingType_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- value as";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion
}
