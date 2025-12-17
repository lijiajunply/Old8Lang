using Old8Lang.Error;

namespace Old8Lang.Tests.Parser.Expression;

/// <summary>
/// 运算符优先级边界情况测试
/// </summary>
[Collection("Sequential")]
public class OperatorPrecedenceEdgeCasesTests
{
    #region 运算符优先级正确语法

    /// <summary>
    /// 测试算术运算符优先级
    /// </summary>
    [Fact]
    public void ParseProgram_ArithmeticOperatorPrecedence_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- a + b * c - d / e";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试混合运算符优先级（算术、比较、逻辑）
    /// </summary>
    [Fact]
    public void ParseProgram_MixedOperatorPrecedence_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- a + b > c * d and e < f or g == h";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试括号改变优先级
    /// </summary>
    [Fact]
    public void ParseProgram_ParenthesesChangePrecedence_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- (a + b) * (c - d) and (e < f)";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试指数运算符优先级
    /// </summary>
    [Fact]
    public void ParseProgram_ExponentialOperatorPrecedence_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- a ^ b ^ c + d * e";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试模运算符优先级
    /// </summary>
    [Fact]
    public void ParseProgram_ModuloOperatorPrecedence_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- a + b % c * d - e / f";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试逻辑运算符优先级
    /// </summary>
    [Fact]
    public void ParseProgram_LogicalOperatorPrecedence_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- a and b or c xor d and not e";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试复杂的三元表达式优先级
    /// </summary>
    [Fact]
    public void ParseProgram_ComplexTernaryPrecedence_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- a > b ? c + d : e * f < g ? h : i";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试比较运算符链
    /// </summary>
    [Fact]
    public void ParseProgram_ComparisonOperatorChain_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- a < b < c > d == e != f <= g >= h";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 运算符优先级边界情况

    /// <summary>
    /// 测试相同优先级运算符组合
    /// </summary>
    [Fact]
    public void ParseProgram_SamePrecedenceOperators_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- a + b - c + d - e + f";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试深层嵌套的优先级表达式
    /// </summary>
    [Fact]
    public void ParseProgram_DeepNestedPrecedence_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- ((a + b) * (c - d)) > ((e / f) + (g % h)) ? (i and j) : (k or l)";
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
    public void ParseProgram_UnaryBinaryOperatorMix_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- -a + b - c * not d and not e";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试函数调用与运算符优先级
    /// </summary>
    [Fact]
    public void ParseProgram_FunctionCallOperatorPrecedence_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- func(a + b) * method(c - d) + prop[e / f]";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试数组索引与运算符优先级
    /// </summary>
    [Fact]
    public void ParseProgram_ArrayIndexOperatorPrecedence_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- arr[a + b] + dict[c * d] - list[e - f]";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 优先级歧义测试

    /// <summary>
    /// 测试可能的歧义表达式解析
    /// </summary>
    [Fact]
    public void ParseProgram_PotentiallyAmbiguousExpression_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- a > b > c and d < e < f";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试复杂类型的比较表达式
    /// </summary>
    [Fact]
    public void ParseProgram_ComplexTypeComparison_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- obj.method(a + b) > collection[c] and arr[0] == func(d)";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 错误的优先级表达式

    /// <summary>
    /// 测试缺少运算符的表达式
    /// </summary>
    [Fact]
    public void ParseProgram_MissingOperator_ThrowsSyntaxError()
    {
        // Arrange
        var code = "result <- a b + c";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试三元运算符不完整
    /// </summary>
    [Fact]
    public void ParseProgram_IncompleteTernary_ThrowsSyntaxError()
    {
        // Arrange
        var code = "result := a > b ? c + d";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion
}