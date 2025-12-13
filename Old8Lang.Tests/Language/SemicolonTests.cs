using Old8Lang.LangParser;

namespace Old8Lang.Tests.Language;

/// <summary>
/// 分号语句分隔符测试
/// </summary>
[Collection("Sequential")]
public class SemicolonTests
{
    /// <summary>
    /// 测试基本分号用法 - 单行多个语句
    /// </summary>
    [Fact]
    public void ParseProgram_SingleLineMultipleStatementsWithSemicolon_ParsesSuccessfully()
    {
        // Arrange
        var code = "a <- 1; b <- 2; c <- 3";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
    }

    /// <summary>
    /// 测试混合使用分号和换行
    /// </summary>
    [Fact]
    public void ParseProgram_MixedSemicolonAndNewline_ParsesSuccessfully()
    {
        // Arrange
        var code = @"a <- 1;
b <- 2; c <- 3
d <- 4";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.Count);
    }

    /// <summary>
    /// 测试连续分号（空语句）
    /// </summary>
    [Fact]
    public void ParseProgram_ConsecutiveSemicolons_ParsesSuccessfully()
    {
        // Arrange
        var code = "a <- 1;; b <- 2";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        // 连续分号被跳过，只解析两个赋值语句
        Assert.Equal(2, result.Count);
    }

    /// <summary>
    /// 测试尾部分号
    /// </summary>
    [Fact]
    public void ParseProgram_TrailingSemicolon_ParsesSuccessfully()
    {
        // Arrange
        var code = "a <- 1;";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
    }

    /// <summary>
    /// 测试块内分号
    /// </summary>
    [Fact]
    public void ParseProgram_SemicolonInBlock_ParsesSuccessfully()
    {
        // Arrange
        var code = @"if true {
    a <- 1; b <- 2
    c <- 3
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count); // 一个 if 语句
    }

    /// <summary>
    /// 测试 for 循环中的分号
    /// </summary>
    [Fact]
    public void ParseProgram_SemicolonInForLoop_ParsesSuccessfully()
    {
        // Arrange
        var code = @"for i <- 0, i < 5, i <- i + 1 {
    a <- i; b <- i * 2; c <- a + b
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
    }

    /// <summary>
    /// 测试函数调用后的分号
    /// </summary>
    [Fact]
    public void ParseProgram_SemicolonAfterFunctionCall_ParsesSuccessfully()
    {
        // Arrange
        var code = "PrintLine(\"Hello\"); PrintLine(\"World\")";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    /// <summary>
    /// 测试复杂嵌套结构中的分号
    /// </summary>
    [Fact]
    public void ParseProgram_SemicolonInNestedStructures_ParsesSuccessfully()
    {
        // Arrange
        var code = @"if x > 0 {
    a <- 1; b <- 2
    if y > 0 {
        c <- 3; d <- 4; e <- 5
    }
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
    }

    /// <summary>
    /// 测试只有分号的行
    /// </summary>
    [Fact]
    public void ParseProgram_OnlySemicolons_ParsesSuccessfully()
    {
        // Arrange
        var code = ";;;";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        // 所有分号都被跳过，没有实际语句
        Assert.Equal(0, result.Count);
    }

    /// <summary>
    /// 测试赋值语句与分号的组合
    /// </summary>
    [Fact]
    public void ParseProgram_AssignmentWithSemicolon_ParsesSuccessfully()
    {
        // Arrange
        var code = "x <- 10; y <- x + 5; z <- x * y";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
    }
}
