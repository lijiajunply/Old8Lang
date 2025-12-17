using Old8Lang.Error;

namespace Old8Lang.Tests.Parser.Lambda;

/// <summary>
/// Lambda表达式变量捕获测试
/// </summary>
[Collection("Sequential")]
public class LambdaCapturingTests
{
    #region Lambda变量捕获正确语法

    /// <summary>
    /// 测试基本变量捕获
    /// </summary>
    [Fact]
    public void ParseProgram_BasicVariableCapturing_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
x <- 10
lambda1 <- (y) -> x + y
result <- lambda1(5)";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试函数工厂中的变量捕获
    /// </summary>
    [Fact]
    public void ParseProgram_FunctionFactoryVariableCapturing_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func makeAdder(x) {
    return (y) -> x + y
}

add10 <- makeAdder(10)
result <- add10(5)";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试嵌套Lambda的变量捕获
    /// </summary>
    [Fact]
    public void ParseProgram_NestedLambdaCapturing_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
x <- 10
outer <- (y) -> {
    z <- 5
    return (w) -> x + y + z + w
}

inner <- outer(3)
result <- inner(2)";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        var program = parser.ParseProgram();
        Assert.NotNull(program);
    }

    #endregion

    #region 错误的Lambda捕获语法

    /// <summary>
    /// 测试不完整的Lambda定义
    /// </summary>
    [Fact]
    public void ParseProgram_IncompleteLambdaCapturing_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
lambda1 <- (x) -> ";
        var tokens = LangParser.LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion
}