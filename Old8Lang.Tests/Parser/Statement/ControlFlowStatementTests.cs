using Old8Lang.Error;
using Old8Lang.LangParser;

namespace Old8Lang.Tests.Parser.Statement;

/// <summary>
/// 控制流语句测试，测试各种控制流语句（if/elif/for/while/switch等）的语法错误
/// </summary>
[Collection("Sequential")]
public class ControlFlowStatementTests
{
    /// <summary>
    /// 测试无效的if语句 - 缺少条件表达式
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidIfStatementMissingCondition_ThrowsSyntaxError()
    {
        // Arrange
        var code = "if { PrintLine(\"Hello\") }";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试无效的elif语句 - 缺少条件表达式
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidElifStatementMissingCondition_ThrowsSyntaxError()
    {
        // Arrange
        var code = "if (a > 5) { PrintLine(\"Hello\") } elif { PrintLine(\"World\") }";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试无效的for循环 - 缺少表达式
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidForLoopMissingExpression_ThrowsSyntaxError()
    {
        // Arrange
        var code = "for i <- 0, i < 10 { PrintLine(i) }";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试无效的while循环 - 缺少条件表达式
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidWhileLoopMissingCondition_ThrowsSyntaxError()
    {
        // Arrange
        var code = "while { PrintLine(\"Hello\") }";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试无效的switch语句 - 缺少表达式
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidSwitchMissingExpression_ThrowsSyntaxError()
    {
        // Arrange
        var code = "switch { case 1 { PrintLine(\"One\") } }";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试if缺少条件
    /// </summary>
    [Fact]
    public void ParseProgram_IfMissingCondition_ThrowsSyntaxError()
    {
        // Arrange
        var code = "if {}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试while缺少条件
    /// </summary>
    [Fact]
    public void ParseProgram_WhileMissingCondition_ThrowsSyntaxError()
    {
        // Arrange
        var code = "while {}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试for缺少分号
    /// </summary>
    [Fact]
    public void ParseProgram_ForMissingComma_ThrowsSyntaxError()
    {
        // Arrange
        var code = "for i <- 0 i < 10 i++ {}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试for-in缺少in关键字
    /// </summary>
    [Fact]
    public void ParseProgram_ForInMissingIn_ThrowsSyntaxError()
    {
        // Arrange
        var code = "for item [1, 2, 3] {}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }
}
