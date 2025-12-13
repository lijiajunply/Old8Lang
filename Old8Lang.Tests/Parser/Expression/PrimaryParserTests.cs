using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.Error;
using Old8Lang.LangParser;

namespace Old8Lang.Tests.Parser.Expression;

/// <summary>
/// PrimaryParser 单元测试 - 测试各种字面量和基础表达式解析
/// </summary>
[Collection("Sequential")]
public class PrimaryParserTests
{
    #region 数字字面量测试

    /// <summary>
    /// 测试整数字面量解析 - 123
    /// </summary>
    [Fact]
    public void ParseIntLiteral_ValidInteger_ParsesSuccessfully()
    {
        // Arrange
        var code = "a <- 123";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        var intValue = Assert.IsType<IntLangValue>(setStmt.Value);
        Assert.Equal(123, intValue.Value);
    }

    /// <summary>
    /// 测试零值整数
    /// </summary>
    [Fact]
    public void ParseIntLiteral_Zero_ParsesSuccessfully()
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
    /// 测试浮点数字面量解析 - 3.14
    /// </summary>
    [Fact]
    public void ParseDoubleLiteral_ValidDouble_ParsesSuccessfully()
    {
        // Arrange
        var code = "a <- 3.14";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        var doubleValue = Assert.IsType<DoubleLangValue>(setStmt.Value);
        Assert.Equal(3.14, doubleValue.Value, 2);
    }

    /// <summary>
    /// 测试科学计数法 - 1.23e10
    /// </summary>
    [Fact]
    public void ParseDoubleLiteral_ScientificNotation_ParsesSuccessfully()
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
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        var doubleValue = Assert.IsType<DoubleLangValue>(setStmt.Value);
        Assert.Equal(1.23e10, doubleValue.Value, 2);
    }

    /// <summary>
    /// 测试负指数科学计数法 - 1.23e-5
    /// </summary>
    [Fact]
    public void ParseDoubleLiteral_NegativeExponentScientificNotation_ParsesSuccessfully()
    {
        // Arrange
        var code = "a <- 1.23e-5";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        var doubleValue = Assert.IsType<DoubleLangValue>(setStmt.Value);
        Assert.Equal(1.23e-5, doubleValue.Value, 10);
    }

    #endregion

    #region 字符串字面量测试

    /// <summary>
    /// 测试字符串字面量解析 - "hello"
    /// </summary>
    [Fact]
    public void ParseStringLiteral_ValidString_ParsesSuccessfully()
    {
        // Arrange
        var code = @"a <- ""hello""";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        var stringValue = Assert.IsType<StringLangValue>(setStmt.Value);
        Assert.Equal("hello", stringValue.Value);
    }

    /// <summary>
    /// 测试空字符串
    /// </summary>
    [Fact]
    public void ParseStringLiteral_EmptyString_ParsesSuccessfully()
    {
        // Arrange
        var code = @"a <- """"";
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
    /// 测试包含转义序列的字符串 - "hello\nworld"
    /// </summary>
    [Fact]
    public void ParseStringLiteral_WithEscapeSequences_ParsesSuccessfully()
    {
        // Arrange
        var code = @"a <- ""hello\nworld""";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<StringLangValue>(setStmt.Value);
    }

    #endregion

    #region 字符字面量测试

    /// <summary>
    /// 测试字符字面量解析 - 'a'
    /// </summary>
    [Fact]
    public void ParseCharLiteral_ValidChar_ParsesSuccessfully()
    {
        // Arrange
        var code = "a <- 'x'";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        var charValue = Assert.IsType<CharLangValue>(setStmt.Value);
        Assert.Equal('x', charValue.Value);
    }

    /// <summary>
    /// 测试转义字符 - '\n'
    /// </summary>
    [Fact]
    public void ParseCharLiteral_EscapeChar_ParsesSuccessfully()
    {
        // Arrange
        var code = @"a <- '\n'";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<CharLangValue>(setStmt.Value);
    }

    #endregion

    #region 布尔字面量测试

    /// <summary>
    /// 测试 true 字面量
    /// </summary>
    [Fact]
    public void ParseBoolLiteral_True_ParsesSuccessfully()
    {
        // Arrange
        var code = "a <- true";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        var boolValue = Assert.IsType<BoolLangValue>(setStmt.Value);
        Assert.True(boolValue.Value);
    }

    /// <summary>
    /// 测试 false 字面量
    /// </summary>
    [Fact]
    public void ParseBoolLiteral_False_ParsesSuccessfully()
    {
        // Arrange
        var code = "a <- false";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        var boolValue = Assert.IsType<BoolLangValue>(setStmt.Value);
        Assert.False(boolValue.Value);
    }

    #endregion

    #region Null 字面量测试

    /// <summary>
    /// 测试 null 字面量
    /// </summary>
    [Fact]
    public void ParseNullLiteral_Null_ParsesSuccessfully()
    {
        // Arrange
        var code = "a <- null";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<NullLangValue>(setStmt.Value);
    }

    #endregion

    #region 集合字面量测试

    /// <summary>
    /// 测试空列表 - []
    /// </summary>
    [Fact]
    public void ParseList_EmptyList_ParsesSuccessfully()
    {
        // Arrange
        var code = "a <- []";
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
    /// 测试简单列表 - [1, 2, 3]
    /// </summary>
    [Fact]
    public void ParseList_SimpleList_ParsesSuccessfully()
    {
        // Arrange
        var code = "a <- [1, 2, 3]";
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
    /// 测试嵌套列表 - [[1, 2], [3, 4]]
    /// </summary>
    [Fact]
    public void ParseList_NestedList_ParsesSuccessfully()
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
    /// 测试空字典 - {}
    /// </summary>
    [Fact]
    public void ParseDictionary_EmptyDictionary_ParsesSuccessfully()
    {
        // Arrange
        var code = "a <- {}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<DictionaryLangValue>(setStmt.Value);
    }

    /// <summary>
    /// 测试简单字典 - {"name": "Alice", "age": 30}
    /// </summary>
    [Fact]
    public void ParseDictionary_SimpleDictionary_ParsesSuccessfully()
    {
        // Arrange
        var code = @"a <- {""name"": ""Alice"", ""age"": 30}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<DictionaryLangValue>(setStmt.Value);
    }

    /// <summary>
    /// 测试字典的复杂键 - {1: "one", 2: "two"}
    /// </summary>
    [Fact]
    public void ParseDictionary_ComplexKeys_ParsesSuccessfully()
    {
        // Arrange
        var code = @"a <- {1: ""one"", 2: ""two""}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<DictionaryLangValue>(setStmt.Value);
    }

    /// <summary>
    /// 测试范围表达式 - [1~10]
    /// </summary>
    [Fact]
    public void ParseArrayOrRange_RangeSyntax_ParsesSuccessfully()
    {
        // Arrange
        var code = "a <- [1~10]";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<RangeLangValue>(setStmt.Value);
    }

    /// <summary>
    /// 测试范围表达式（负数）- [-10~10]
    /// </summary>
    [Fact]
    public void ParseArrayOrRange_NegativeRange_ParsesSuccessfully()
    {
        // Arrange
        var code = "a <- [-10~10]";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<RangeLangValue>(setStmt.Value);
    }

    #endregion

    #region 实例化测试

    /// <summary>
    /// 测试简单的类实例化 - MyClass()
    /// </summary>
    [Fact]
    public void ParseInstantiate_SimpleClass_ParsesSuccessfully()
    {
        // Arrange
        var code = "a <- MyClass()";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<Instance>(setStmt.Value);
    }

    /// <summary>
    /// 测试带参数的类实例化 - MyClass(arg1, arg2)
    /// </summary>
    [Fact]
    public void ParseInstantiate_WithArguments_ParsesSuccessfully()
    {
        // Arrange
        var code = "a <- MyClass(10, 20)";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<Instance>(setStmt.Value);
    }

    #endregion

    #region 标识符测试

    /// <summary>
    /// 测试简单标识符 - x
    /// </summary>
    [Fact]
    public void ParseIdentifier_SimpleIdentifier_ParsesSuccessfully()
    {
        // Arrange
        var code = "a <- x";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<LangId>(setStmt.Value);
    }

    /// <summary>
    /// 测试下划线开头的标识符 - _test
    /// </summary>
    [Fact]
    public void ParseIdentifier_UnderscorePrefix_ParsesSuccessfully()
    {
        // Arrange
        var code = "a <- _test";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        var id = Assert.IsType<LangId>(setStmt.Value);
        Assert.Equal("_test", id.IdName);
    }

    /// <summary>
    /// 测试包含数字的标识符 - var123
    /// </summary>
    [Fact]
    public void ParseIdentifier_WithNumbers_ParsesSuccessfully()
    {
        // Arrange
        var code = "a <- var123";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        var id = Assert.IsType<LangId>(setStmt.Value);
        Assert.Equal("var123", id.IdName);
    }

    #endregion

    #region 元组测试

    /// <summary>
    /// 测试简单元组 - (1, 2)
    /// </summary>
    [Fact]
    public void ParseTuple_SimpleTuple_ParsesSuccessfully()
    {
        // Arrange
        var code = "a <- (1, 2)";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<TupleLangValue>(setStmt.Value);
    }

    /// <summary>
    /// 测试三元组 - (1, 2, 3)
    /// </summary>
    [Fact]
    public void ParseTuple_ThreeElements_ParsesSuccessfully()
    {
        // Arrange
        var code = "a <- (1, 2, 3)";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<TupleLangValue>(setStmt.Value);
    }

    /// <summary>
    /// 测试混合类型元组 - (1, "hello", true)
    /// </summary>
    [Fact]
    public void ParseTuple_MixedTypes_ParsesSuccessfully()
    {
        // Arrange
        var code = @"a <- (1, ""hello"", true)";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<TupleLangValue>(setStmt.Value);
    }

    #endregion

    #region 括号表达式测试

    /// <summary>
    /// 测试括号表达式 - (1 + 2)
    /// </summary>
    [Fact]
    public void ParseParenthesizedExpression_SimpleExpression_ParsesSuccessfully()
    {
        // Arrange
        var code = "a <- (1 + 2)";
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
    /// 测试嵌套括号表达式 - ((1 + 2) * 3)
    /// </summary>
    [Fact]
    public void ParseParenthesizedExpression_NestedExpression_ParsesSuccessfully()
    {
        // Arrange
        var code = "a <- ((1 + 2) * 3)";
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
