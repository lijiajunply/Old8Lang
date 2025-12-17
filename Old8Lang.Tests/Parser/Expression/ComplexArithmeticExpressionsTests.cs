using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Parser.Expression;

/// <summary>
/// 复杂算术表达式测试
/// </summary>
[Collection("Sequential")]
public class ComplexArithmeticExpressionsTests
{
    #region 复杂算术表达式正确语法

    /// <summary>
    /// 测试多层嵌套的算术表达式
    /// </summary>
    [Fact]
    public void ParseProgram_ComplexNestedArithmetic_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- ((a + b) * (c - d)) / (e ^ f)";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试混合运算符的算术表达式
    /// </summary>
    [Fact]
    public void ParseProgram_MixedOperatorArithmetic_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- a + b * c - d / e % f";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试长运算符链表达式
    /// </summary>
    [Fact]
    public void ParseProgram_LongOperatorChain_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- a + b + c + d + e + f + g + h";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试负数参与的复杂表达式
    /// </summary>
    [Fact]
    public void ParseProgram_NegativeNumbersArithmetic_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- -a * (b + -c) / (-d + e)";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试括号嵌套的不同层级
    /// </summary>
    [Fact]
    public void ParseProgram_DeepNestedParentheses_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- ((((a + b)))) * (((c - d)))";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试浮点数复杂运算
    /// </summary>
    [Fact]
    public void ParseProgram_FloatingPointArithmetic_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- 3.14 * (radius ^ 2) + 2.718 * exp";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 算术表达式错误语法

    /// <summary>
    /// 测试缺少右操作数的算术表达式
    /// </summary>
    [Fact]
    public void ParseProgram_MissingRightOperand_ThrowsSyntaxError()
    {
        // Arrange
        var code = "result <- a +";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试缺少左操作数的算术表达式
    /// </summary>
    [Fact]
    public void ParseProgram_MissingLeftOperand_ThrowsSyntaxError()
    {
        // Arrange
        var code = "result <- + b";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试不匹配的括号
    /// </summary>
    [Fact]
    public void ParseProgram_UnmatchedParentheses_ThrowsSyntaxError()
    {
        // Arrange
        var code = "result <- (a + b * c";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试多余的右括号
    /// </summary>
    [Fact]
    public void ParseProgram_ExtraRightParenthesis_ThrowsSyntaxError()
    {
        // Arrange
        var code = "result <- a + b)";
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
        var code = "result <- a + * b";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion

    #region 边界情况测试

    /// <summary>
    /// 测试零值运算
    /// </summary>
    [Fact]
    public void ParseProgram_ZeroArithmetic_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- 0 * a + 0 / b + 0 - a";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试大数运算
    /// </summary>
    [Fact]
    public void ParseProgram_LargeNumbersArithmetic_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- 999999999 + 111111111 * 888888888";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试小数精度运算
    /// </summary>
    [Fact]
    public void ParseProgram_PrecisionArithmetic_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- 0.1 + 0.2 - 0.3 * 1.0 / 4.0";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion
}