using Old8Lang.Error;

namespace Old8Lang.Tests.Parser.Expression;

/// <summary>
/// 集合、索引和成员访问表达式错误验证测试
/// </summary>
[Collection("Sequential")]
public class CollectionExpressionTests
{
    #region 数组/列表错误

    /// <summary>
    /// 测试数组缺少逗号分隔符
    /// </summary>
    [Fact]
    public void ParseProgram_ArrayMissingComma_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- [1 2 3]";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试数组缺少右括号
    /// </summary>
    [Fact]
    public void ParseProgram_ArrayMissingRightBracket_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- [1, 2, 3";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试数组末尾多余的逗号
    /// </summary>
    [Fact]
    public void ParseProgram_ArrayTrailingComma_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- [1, 2, 3,]";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion

    #region 字典错误

    /// <summary>
    /// 测试字典缺少冒号
    /// </summary>
    [Fact]
    public void ParseProgram_DictionaryMissingColon_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- {\"key\" \"value\"}";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试字典缺少值
    /// </summary>
    [Fact]
    public void ParseProgram_DictionaryMissingValue_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- {\"key\":}";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试字典缺少键
    /// </summary>
    [Fact]
    public void ParseProgram_DictionaryMissingKey_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- {:\"value\"}";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion

    #region 函数调用错误

    /// <summary>
    /// 测试函数调用缺少右括号
    /// </summary>
    [Fact]
    public void ParseProgram_FunctionCallMissingRightParen_ThrowsSyntaxError()
    {
        // Arrange
        var code = "PrintLine(\"Hello\"";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试函数参数缺少逗号
    /// </summary>
    [Fact]
    public void ParseProgram_FunctionArgumentsMissingComma_ThrowsSyntaxError()
    {
        // Arrange
        var code = "Add(1 2)";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试函数调用多余的逗号
    /// </summary>
    [Fact]
    public void ParseProgram_FunctionCallTrailingComma_ThrowsSyntaxError()
    {
        // Arrange
        var code = "Add(1, 2,)";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion

    #region 索引访问错误

    /// <summary>
    /// 测试索引缺少右括号
    /// </summary>
    [Fact]
    public void ParseProgram_IndexMissingRightBracket_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- array[0";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试索引缺少索引表达式
    /// </summary>
    [Fact]
    public void ParseProgram_IndexMissingExpression_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- array[]";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion

    #region 成员访问错误

    /// <summary>
    /// 测试点号后缺少成员名
    /// </summary>
    [Fact]
    public void ParseProgram_MemberAccessMissingMember_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- obj.";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试连续的点号
    /// </summary>
    [Fact]
    public void ParseProgram_ConsecutiveDots_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- obj..field";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion

    #region 范围表达式错误

    /// <summary>
    /// 测试范围缺少结束值
    /// </summary>
    [Fact]
    public void ParseProgram_RangeMissingEndValue_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- [1~]";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试范围缺少开始值
    /// </summary>
    [Fact]
    public void ParseProgram_RangeMissingStartValue_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- [~10]";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion
}
