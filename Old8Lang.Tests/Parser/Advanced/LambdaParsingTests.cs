using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Parser.Advanced;

/// <summary>
/// Lambda 表达式严格检查测试
/// </summary>
[Collection("Sequential")]
public class LambdaParsingTests
{
    /// <summary>
    /// 测试正确的 Lambda 语法 - 简写形式
    /// </summary>
    [Fact]
    public void ParseProgram_LambdaWithArrowShortForm_ParsesSuccessfully()
    {
        // Arrange
        var code = "add <- (x, y) -> x + y";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
    }

    /// <summary>
    /// 测试正确的 Lambda 语法 - 块形式
    /// </summary>
    [Fact]
    public void ParseProgram_LambdaWithArrowBlockForm_ParsesSuccessfully()
    {
        // Arrange
        var code = "square <- (n) -> { return n * n }";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
    }

    /// <summary>
    /// 测试 Lambda 缺少箭头 - 同一行有其他内容
    /// </summary>
    [Fact]
    public void ParseProgram_LambdaMissingArrowSameLine_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- (x, y) x + y";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试 Lambda 缺少箭头 - 单参数
    /// </summary>
    [Fact]
    public void ParseProgram_LambdaMissingArrowSingleParam_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- (x) x * 2";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试元组后有分号 - 不应报错
    /// </summary>
    [Fact]
    public void ParseProgram_TupleWithSemicolon_ParsesSuccessfully()
    {
        // Arrange
        var code = "a <- (x, y);";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
    }

    /// <summary>
    /// 测试元组后换行 - 不应报错
    /// </summary>
    [Fact]
    public void ParseProgram_TupleWithNewline_ParsesSuccessfully()
    {
        // Arrange
        var code = @"a <- (x, y)
b <- 10";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    /// <summary>
    /// 测试同一行多个元组（分号分隔）
    /// </summary>
    [Fact]
    public void ParseProgram_MultipleTuplesWithSemicolon_ParsesSuccessfully()
    {
        // Arrange
        var code = "a <- (1, 2); b <- (3, 4)";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    /// <summary>
    /// 测试 Lambda 缺少箭头 - 多参数
    /// </summary>
    [Fact]
    public void ParseProgram_LambdaMissingArrowMultipleParams_ThrowsSyntaxError()
    {
        // Arrange
        var code = "max <- (a, b, c) a > b";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试 Lambda 带类型注解
    /// </summary>
    [Fact]
    public void ParseProgram_LambdaWithTypeAnnotation_ParsesSuccessfully()
    {
        // Arrange
        var code = "add <- (x:int, y:int) -> x + y";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
    }

    /// <summary>
    /// 测试无参数 Lambda
    /// </summary>
    [Fact]
    public void ParseProgram_LambdaNoParams_ParsesSuccessfully()
    {
        // Arrange
        var code = "getNumber <- () -> 42";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
    }

    /// <summary>
    /// 测试复杂 Lambda - 嵌套 if
    /// </summary>
    [Fact]
    public void ParseProgram_LambdaWithNestedIf_ParsesSuccessfully()
    {
        // Arrange
        var code = """
                   max <- (x, y) -> {
                       if x > y {
                           return x
                       } else {
                           return y
                       }
                   }
                   """;
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
    }

    #region Lambda表达式错误

    /// <summary>
    /// 测试Lambda缺少箭头
    /// </summary>
    [Fact]
    public void ParseProgram_LambdaMissingArrow_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- (x, y) x + y";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试Lambda缺少参数括号
    /// </summary>
    [Fact]
    public void ParseProgram_LambdaMissingParentheses_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- x, y -> x + y";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试Lambda缺少表达式体
    /// </summary>
    [Fact]
    public void ParseProgram_LambdaMissingBody_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- (x, y) ->";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion
}
