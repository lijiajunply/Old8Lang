using Old8Lang.Error;

namespace Old8Lang.Tests.Parser.Expression;

/// <summary>
/// 一元运算符测试
/// </summary>
[Collection("Sequential")]
public class UnaryOperatorsTests
{
    #region 一元运算符正确语法

    /// <summary>
    /// 测试一元负号运算符
    /// </summary>
    [Fact]
    public void ParseProgram_UnaryMinus_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- -a";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试逻辑非运算符
    /// </summary>
    [Fact]
    public void ParseProgram_LogicalNot_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- not condition";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试按位非运算符
    /// </summary>
    [Fact]
    public void ParseProgram_BitwiseNot_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- not a";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试自增运算符（前缀）
    /// </summary>
    [Fact]
    public void ParseProgram_PrefixIncrement_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- ++a";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试自减运算符（前缀）
    /// </summary>
    [Fact]
    public void ParseProgram_PrefixDecrement_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- --a";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试自增运算符（后缀）
    /// </summary>
    [Fact]
    public void ParseProgram_PostfixIncrement_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- a++";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试自减运算符（后缀）
    /// </summary>
    [Fact]
    public void ParseProgram_PostfixDecrement_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- a--";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 复杂一元运算符表达式

    /// <summary>
    /// 测试多个一元运算符组合
    /// </summary>
    [Fact]
    public void ParseProgram_MultipleUnaryOperators_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- - -a";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试一元运算符与二元运算符混合
    /// </summary>
    [Fact]
    public void ParseProgram_UnaryBinaryMix_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- -a + b * -c - d";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试一元运算符与括号
    /// </summary>
    [Fact]
    public void ParseProgram_UnaryWithParentheses_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- -(a + b)";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试一元运算符作用于函数调用
    /// </summary>
    [Fact]
    public void ParseProgram_UnaryOnFunctionCall_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- -func(a, b)";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试一元运算符作用于数组索引
    /// </summary>
    [Fact]
    public void ParseProgram_UnaryOnArrayIndex_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- -arr[0]";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试一元运算符作用于成员访问
    /// </summary>
    [Fact]
    public void ParseProgram_UnaryOnMemberAccess_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- -obj.property";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试一元逻辑非在复杂表达式中
    /// </summary>
    [Fact]
    public void ParseProgram_LogicalNotInComplexExpression_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- not (a > b and c < d) or not e";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 一元运算符边界情况

    /// <summary>
    /// 测试一元运算符作用于字面量
    /// </summary>
    [Fact]
    public void ParseProgram_UnaryOnLiterals_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- -123 + 456 - -789";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试一元运算符作用于浮点数
    /// </summary>
    [Fact]
    public void ParseProgram_UnaryOnFloats_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- -3.14 + 2.718 - -1.414";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试一元运算符作用于布尔值
    /// </summary>
    [Fact]
    public void ParseProgram_UnaryOnBooleans_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- not true and not false";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试嵌套的一元运算符
    /// </summary>
    [Fact]
    public void ParseProgram_NestedUnaryOperators_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- not (not condition)";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 错误的一元运算符语法

    /// <summary>
    /// 测试缺少操作数的一元运算符
    /// </summary>
    [Fact]
    public void ParseProgram_UnaryWithoutOperand_ThrowsSyntaxError()
    {
        // Arrange
        var code = "result <- -";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试逻辑非缺少操作数
    /// </summary>
    [Fact]
    public void ParseProgram_LogicalNotWithoutOperand_ThrowsSyntaxError()
    {
        // Arrange
        var code = "result <- not";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试按位非缺少操作数
    /// </summary>
    [Fact]
    public void ParseProgram_BitwiseNotWithoutOperand_ThrowsSyntaxError()
    {
        // Arrange
        var code = "result <- !";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试自增运算符缺少操作数
    /// </summary>
    [Fact]
    public void ParseProgram_IncrementWithoutOperand_ThrowsSyntaxError()
    {
        // Arrange
        var code = "result <- ++";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试不连续的一元运算符
    /// </summary>
    [Fact]
    public void ParseProgram_DiscontinuousUnaryOperators_ThrowsSyntaxError()
    {
        // Arrange
        var code = "result <- - a b";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion
}