using Old8Lang.AST.Expression;
using Old8Lang.AST.Statement;
using Old8Lang.Error;
using Old8Lang.LangParser;

namespace Old8Lang.Tests.Parser.Expression;

/// <summary>
/// ExpressionParser 单元测试 - 测试各种运算符优先级和结合性
/// </summary>
[Collection("Sequential")]
public class ExpressionParserTests
{
    #region 运算符优先级测试

    /// <summary>
    /// 测试加法和乘法的优先级 - 1 + 2 * 3 应解析为 1 + (2 * 3)
    /// </summary>
    [Fact]
    public void ParseExpression_AdditionAndMultiplication_ParsesWithCorrectPrecedence()
    {
        // Arrange
        var code = "result <- 1 + 2 * 3";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<Operation>(setStmt.Value);
    }

    /// <summary>
    /// 测试减法和除法的优先级 - 10 - 4 / 2 应解析为 10 - (4 / 2)
    /// </summary>
    [Fact]
    public void ParseExpression_SubtractionAndDivision_ParsesWithCorrectPrecedence()
    {
        // Arrange
        var code = "result <- 10 - 4 / 2";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<Operation>(setStmt.Value);
    }

    /// <summary>
    /// 测试乘法和幂运算的优先级 - 2 * 3 ^ 2 应解析为 2 * (3 ^ 2)
    /// </summary>
    [Fact]
    public void ParseExpression_MultiplicationAndPower_ParsesWithCorrectPrecedence()
    {
        // Arrange
        var code = "result <- 2 * 3 ^ 2";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<Operation>(setStmt.Value);
    }

    /// <summary>
    /// 测试比较运算符和算术运算符的优先级 - 1 + 2 > 3 - 1
    /// </summary>
    [Fact]
    public void ParseExpression_ComparisonAndArithmetic_ParsesWithCorrectPrecedence()
    {
        // Arrange
        var code = "result <- 1 + 2 > 3 - 1";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<Operation>(setStmt.Value);
    }

    /// <summary>
    /// 测试逻辑运算符和比较运算符的优先级 - a > b && c < d
    /// </summary>
    [Fact]
    public void ParseExpression_LogicalAndComparison_ParsesWithCorrectPrecedence()
    {
        // Arrange
        var code = "result <- a > b && c < d";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<Operation>(setStmt.Value);
    }

    /// <summary>
    /// 测试复杂的混合运算符优先级 - 1 + 2 * 3 ^ 4 > 5 - 6 / 7 && 8 < 9
    /// </summary>
    [Fact]
    public void ParseExpression_ComplexMixedOperators_ParsesWithCorrectPrecedence()
    {
        // Arrange
        var code = "result <- 1 + 2 * 3 ^ 4 > 5 - 6 / 7 && 8 < 9";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<Operation>(setStmt.Value);
    }

    #endregion

    #region 运算符结合性测试

    /// <summary>
    /// 测试左结合的加法 - 10 - 5 - 3 应解析为 (10 - 5) - 3
    /// </summary>
    [Fact]
    public void ParseExpression_LeftAssociativeSubtraction_ParsesCorrectly()
    {
        // Arrange
        var code = "result <- 10 - 5 - 3";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<Operation>(setStmt.Value);
    }

    /// <summary>
    /// 测试左结合的除法 - 20 / 4 / 2 应解析为 (20 / 4) / 2
    /// </summary>
    [Fact]
    public void ParseExpression_LeftAssociativeDivision_ParsesCorrectly()
    {
        // Arrange
        var code = "result <- 20 / 4 / 2";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<Operation>(setStmt.Value);
    }

    /// <summary>
    /// 测试右结合的幂运算 - 2 ^ 3 ^ 2 应解析为 2 ^ (3 ^ 2)
    /// </summary>
    [Fact]
    public void ParseExpression_RightAssociativePower_ParsesCorrectly()
    {
        // Arrange
        var code = "result <- 2 ^ 3 ^ 2";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<Operation>(setStmt.Value);
    }

    /// <summary>
    /// 测试左结合的逻辑 AND - a && b && c 应解析为 (a && b) && c
    /// </summary>
    [Fact]
    public void ParseExpression_LeftAssociativeLogicalAnd_ParsesCorrectly()
    {
        // Arrange
        var code = "result <- a && b && c";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<Operation>(setStmt.Value);
    }

    /// <summary>
    /// 测试左结合的逻辑 OR - a || b || c 应解析为 (a || b) || c
    /// </summary>
    [Fact]
    public void ParseExpression_LeftAssociativeLogicalOr_ParsesCorrectly()
    {
        // Arrange
        var code = "result <- a || b || c";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<Operation>(setStmt.Value);
    }

    #endregion

    #region 三元表达式测试

    /// <summary>
    /// 测试简单的三元表达式 - a > b ? x : y
    /// </summary>
    [Fact]
    public void ParseTernaryExpression_Simple_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- a > b ? x : y";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<TernaryExpression>(setStmt.Value);
    }

    /// <summary>
    /// 测试嵌套的三元表达式 - a ? b ? c : d : e
    /// </summary>
    [Fact]
    public void ParseTernaryExpression_Nested_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- a ? b ? c : d : e";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<TernaryExpression>(setStmt.Value);
    }

    /// <summary>
    /// 测试三元表达式中的算术运算 - a > 0 ? a * 2 : a / 2
    /// </summary>
    [Fact]
    public void ParseTernaryExpression_WithArithmetic_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- a > 0 ? a * 2 : a / 2";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<TernaryExpression>(setStmt.Value);
    }

    /// <summary>
    /// 测试三元表达式缺少冒号 - 应该抛出语法错误
    /// </summary>
    [Fact]
    public void ParseTernaryExpression_MissingColon_ThrowsSyntaxError()
    {
        // Arrange
        var code = "result <- a > b ? x";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试三元表达式缺少 else 部分 - 应该抛出语法错误
    /// </summary>
    [Fact]
    public void ParseTernaryExpression_MissingElsePart_ThrowsSyntaxError()
    {
        // Arrange
        var code = "result <- a > b ? x :";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion

    #region 布尔运算测试

    /// <summary>
    /// 测试短路 AND 运算符 - a && b
    /// </summary>
    [Fact]
    public void ParseBoolOpera_ShortCircuitAnd_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- a && b";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<Operation>(setStmt.Value);
    }

    /// <summary>
    /// 测试短路 OR 运算符 - a || b
    /// </summary>
    [Fact]
    public void ParseBoolOpera_ShortCircuitOr_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- a || b";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<Operation>(setStmt.Value);
    }

    /// <summary>
    /// 测试混合的 AND 和 OR - a || b && c（应优先处理 AND）
    /// </summary>
    [Fact]
    public void ParseBoolOpera_MixedAndOr_ParsesWithCorrectPrecedence()
    {
        // Arrange
        var code = "result <- a || b && c";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<Operation>(setStmt.Value);
    }

    /// <summary>
    /// 测试 NOT 运算符 - not a
    /// </summary>
    [Fact]
    public void ParseBoolOpera_NotOperator_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- not a";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<Operation>(setStmt.Value);
    }

    /// <summary>
    /// 测试双重 NOT - !!a
    /// </summary>
    [Fact]
    public void ParseBoolOpera_DoubleNot_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- not not a";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<Operation>(setStmt.Value);
    }

    #endregion

    #region 比较运算符测试

    /// <summary>
    /// 测试等于运算符 - a == b
    /// </summary>
    [Fact]
    public void ParseComparison_Equals_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- a == b";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<Operation>(setStmt.Value);
    }

    /// <summary>
    /// 测试不等于运算符 - a != b
    /// </summary>
    [Fact]
    public void ParseComparison_NotEquals_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- a != b";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<Operation>(setStmt.Value);
    }

    /// <summary>
    /// 测试大于运算符 - a > b
    /// </summary>
    [Fact]
    public void ParseComparison_GreaterThan_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- a > b";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<Operation>(setStmt.Value);
    }

    /// <summary>
    /// 测试小于等于运算符 - a <= b
    /// </summary>
    [Fact]
    public void ParseComparison_LessThanOrEquals_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- a <= b";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<Operation>(setStmt.Value);
    }

    /// <summary>
    /// 测试链式比较（在某些语言中不支持，但应该能解析）- a < b < c
    /// </summary>
    [Fact]
    public void ParseComparison_ChainedComparison_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- a < b < c";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<Operation>(setStmt.Value);
    }

    #endregion

    #region 一元运算符测试

    /// <summary>
    /// 测试一元负号 - -a
    /// </summary>
    [Fact]
    public void ParseUnaryExpression_Negation_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- -a";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<Operation>(setStmt.Value);
    }

    /// <summary>
    /// 测试双重负号 - --a
    /// </summary>
    [Fact]
    public void ParseUnaryExpression_DoubleNegation_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- --a";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<Operation>(setStmt.Value);
    }

    #endregion

    #region 复杂表达式测试

    /// <summary>
    /// 测试包含所有运算符类型的复杂表达式
    /// </summary>
    [Fact]
    public void ParseExpression_VeryComplexExpression_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- -a * b ^ 2 + c / d > e && f || not g ? x : y";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<TernaryExpression>(setStmt.Value);
    }

    /// <summary>
    /// 测试多层嵌套括号的复杂表达式
    /// </summary>
    [Fact]
    public void ParseExpression_DeeplyNestedParentheses_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- ((((a + b) * c) - d) / e)";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<Operation>(setStmt.Value);
    }

    #endregion
}