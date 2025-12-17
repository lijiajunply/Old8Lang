using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Language;

/// <summary>
/// 边界情况测试，测试各种极端值和边界条件
/// </summary>
[Collection("Sequential")]
public class EdgeCaseTests
{
    #region 空值测试

    /// <summary>
    /// 测试空字符串
    /// </summary>
    [Fact]
    public void ParseProgram_EmptyString_Success()
    {
        // Arrange
        var code = "a <- \"\"";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        var stringValue = Assert.IsType<StringLangValue>(setStmt.Value);
        Assert.Equal("", stringValue.Value);
    }

    /// <summary>
    /// 测试空代码块
    /// </summary>
    [Fact]
    public void ParseProgram_EmptyBlock_Success()
    {
        // Arrange
        var code = "if true {}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        Assert.IsType<IfStatement>(result[0]);
    }

    #endregion

    #region 极值测试

    /// <summary>
    /// 测试最大整数值
    /// </summary>
    [Fact]
    public void ParseProgram_MaxIntValue_Success()
    {
        // Arrange
        var code = $"a <- {int.MaxValue}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        var intValue = Assert.IsType<IntLangValue>(setStmt.Value);
        Assert.Equal(int.MaxValue, intValue.Value);
    }

    /// <summary>
    /// 测试最小整数值
    /// </summary>
    [Fact]
    public void ParseProgram_MinIntValue_Success()
    {
        // Arrange
        var code = $"a <- {int.MinValue}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        // 注意：负数可能会被解析为一元负号操作
        Assert.NotNull(result[0]);
    }

    /// <summary>
    /// 测试零值
    /// </summary>
    [Fact]
    public void ParseProgram_ZeroValue_Success()
    {
        // Arrange
        var code = "a <- 0";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        var intValue = Assert.IsType<IntLangValue>(setStmt.Value);
        Assert.Equal(0, intValue.Value);
    }

    /// <summary>
    /// 测试极大浮点数
    /// </summary>
    [Fact]
    public void ParseProgram_LargeDoubleValue_Success()
    {
        // Arrange
        var code = "a <- 1.7976931348623157E+308";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
    }

    /// <summary>
    /// 测试极小浮点数（接近零）
    /// </summary>
    [Fact]
    public void ParseProgram_TinyDoubleValue_Success()
    {
        // Arrange
        var code = "a <- 0.0000000001";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        var doubleValue = Assert.IsType<DoubleLangValue>(setStmt.Value);
        Assert.Equal(0.0000000001, doubleValue.Value, 15);
    }

    #endregion

    #region 嵌套结构测试

    /// <summary>
    /// 测试深层嵌套的if语句
    /// </summary>
    [Fact]
    public void ParseProgram_DeeplyNestedIf_Success()
    {
        // Arrange
        var code = @"
if true {
    if true {
        if true {
            if true {
                a <- 1
            }
        }
    }
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        Assert.IsType<IfStatement>(result[0]);
    }

    /// <summary>
    /// 测试嵌套数组
    /// </summary>
    [Fact]
    public void ParseProgram_NestedArray_Success()
    {
        // Arrange
        var code = "a <- [[1, 2], [3, 4]]";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<ArrayLangValue>(setStmt.Value);
    }

    /// <summary>
    /// 测试嵌套字典
    /// </summary>
    [Fact]
    public void ParseProgram_NestedDictionary_Success()
    {
        // Arrange
        var code = "a <- {\"outer\": {\"inner\": 1}}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
    }

    /// <summary>
    /// 测试深层嵌套的算术表达式
    /// </summary>
    [Fact]
    public void ParseProgram_DeeplyNestedArithmetic_Success()
    {
        // Arrange
        var code = "a <- ((((1 + 2) * 3) - 4) / 5)";
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

    #region 长度极限测试

    /// <summary>
    /// 测试超长字符串
    /// </summary>
    [Fact]
    public void ParseProgram_VeryLongString_Success()
    {
        // Arrange
        var longString = new string('a', 10000);
        var code = $"a <- \"{longString}\"";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        var stringValue = Assert.IsType<StringLangValue>(setStmt.Value);
        Assert.Equal(10000, stringValue.Value.Length);
    }

    /// <summary>
    /// 测试超长标识符
    /// </summary>
    [Fact]
    public void ParseProgram_VeryLongIdentifier_Success()
    {
        // Arrange
        var longId = new string('a', 1000);
        var code = $"{longId} <- 1";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        var id = Assert.IsType<LangId>(setStmt.Id);
        Assert.Equal(1000, id.IdName.Length);
    }

    /// <summary>
    /// 测试大量数组元素
    /// </summary>
    [Fact]
    public void ParseProgram_LargeArray_Success()
    {
        // Arrange
        var elements = string.Join(", ", Enumerable.Range(1, 100));
        var code = $"a <- [{elements}]";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<ArrayLangValue>(setStmt.Value);
    }

    #endregion

    #region 特殊字符测试

    /// <summary>
    /// 测试包含换行符的字符串
    /// </summary>
    [Fact]
    public void ParseProgram_StringWithNewline_Success()
    {
        // Arrange
        var code = "a <- \"Hello\\nWorld\"";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
    }

    /// <summary>
    /// 测试包含制表符的字符串
    /// </summary>
    [Fact]
    public void ParseProgram_StringWithTab_Success()
    {
        // Arrange
        var code = "a <- \"Hello\\tWorld\"";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
    }

    /// <summary>
    /// 测试包含引号的字符串
    /// </summary>
    [Fact]
    public void ParseProgram_StringWithQuotes_Success()
    {
        // Arrange
        var code = "a <- \"Hello \\\"World\\\"\"";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
    }

    /// <summary>
    /// 测试Unicode字符
    /// </summary>
    [Fact]
    public void ParseProgram_UnicodeString_Success()
    {
        // Arrange
        var code = "a <- \"你好世界🌍\"";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        var stringValue = Assert.IsType<StringLangValue>(setStmt.Value);
        Assert.Contains("你好", stringValue.Value);
    }

    #endregion

    #region 浮点数边界测试

    /// <summary>
    /// 测试浮点数精度
    /// </summary>
    [Fact]
    public void ParseProgram_FloatPrecision_Success()
    {
        // Arrange
        var code = "a <- 0.123456789012345";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        var doubleValue = Assert.IsType<DoubleLangValue>(setStmt.Value);
        Assert.Equal(0.123456789012345, doubleValue.Value, 15);
    }

    /// <summary>
    /// 测试科学计数法
    /// </summary>
    [Fact]
    public void ParseProgram_ScientificNotation_Success()
    {
        // Arrange
        var code = "a <- 1.23e10";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
    }

    /// <summary>
    /// 测试负科学计数法
    /// </summary>
    [Fact]
    public void ParseProgram_NegativeScientificNotation_Success()
    {
        // Arrange
        var code = "a <- 1.23e-10";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
    }

    #endregion

    #region 单字符测试

    /// <summary>
    /// 测试单字符变量名
    /// </summary>
    [Fact]
    public void ParseProgram_SingleCharVariable_Success()
    {
        // Arrange
        var code = "x <- 1";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        var id = Assert.IsType<LangId>(setStmt.Id);
        Assert.Equal("x", id.IdName);
    }

    /// <summary>
    /// 测试下划线开头的变量名
    /// </summary>
    [Fact]
    public void ParseProgram_UnderscoreVariable_Success()
    {
        // Arrange
        var code = "_test <- 1";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        var id = Assert.IsType<LangId>(setStmt.Id);
        Assert.Equal("_test", id.IdName);
    }

    #endregion

    #region 混合边界测试

    /// <summary>
    /// 测试极端复杂的嵌套表达式
    /// </summary>
    [Fact]
    public void ParseProgram_ComplexNestedExpression_Success()
    {
        // Arrange
        var code = "result <- (1 + 2) * (3 - 4) / (5 + 6) and true or false";
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
    /// 测试多个连续的赋值语句
    /// </summary>
    [Fact]
    public void ParseProgram_MultipleConsecutiveAssignments_Success()
    {
        // Arrange
        var code = @"
a <- 1
b <- 2
c <- 3
d <- 4
e <- 5";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.Count);
        for (int i = 0; i < result.Count; i++)
        {
            Assert.IsType<SetStatement>(result[i]);
        }
    }

    #endregion
}
