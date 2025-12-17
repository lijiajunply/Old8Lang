using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Parser.Expression;

/// <summary>
/// 算术表达式和括号错误验证测试
/// </summary>
[Collection("Sequential")]
public class ArithmeticExpressionTests
{
    #region 算术表达式错误

    /// <summary>
    /// 测试缺少操作数的加法
    /// </summary>
    [Fact]
    public void ParseProgram_IncompletePlusExpression_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- 1 +";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试缺少左操作数的加法
    /// </summary>
    [Fact]
    public void ParseProgram_MissingLeftOperandPlus_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- + 1";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试连续的运算符
    /// </summary>
    [Fact]
    public void ParseProgram_ConsecutiveOperators_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- 1 + + 2";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试不完整的乘法表达式
    /// </summary>
    [Fact]
    public void ParseProgram_IncompleteMultiplication_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- 5 *";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试不完整的除法表达式
    /// </summary>
    [Fact]
    public void ParseProgram_IncompleteDivision_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- 10 /";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试not后缺少表达式
    /// </summary>
    [Fact]
    public void ParseProgram_NotWithoutExpression_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- not";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion

    #region 括号错误

    /// <summary>
    /// 测试不匹配的左括号
    /// </summary>
    [Fact]
    public void ParseProgram_UnmatchedLeftParen_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- (1 + 2";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试不匹配的右括号
    /// </summary>
    [Fact]
    public void ParseProgram_UnmatchedRightParen_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- 1 + 2)";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试空括号
    /// </summary>
    [Fact]
    public void ParseProgram_EmptyParentheses_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- ()";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试多个连续的赋值运算符
    /// </summary>
    [Fact]
    public void ParseProgram_ConsecutiveAssignmentOperators_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- <- 1";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion
}
