using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Parser.Statement;

/// <summary>
/// 赋值语句测试，测试各种赋值语句的语法错误
/// </summary>
[Collection("Sequential")]
public class AssignmentStatementTests
{
    /// <summary>
    /// 测试无效赋值运算符 - 使用等号
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidAssignmentOperator_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a = 10";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试无效的类型注解 - 无效类型名
    /// </summary>
    [Fact]
    public void ParseProgram_InvalidTypeAnnotation_ThrowsSyntaxError()
    {
        // Arrange
        var code = "invalid_type <- 10 : invalid_type";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试缺少赋值运算符
    /// </summary>
    [Fact]
    public void ParseProgram_MissingAssignmentOperator_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a 123";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试赋值缺少右值
    /// </summary>
    [Fact]
    public void ParseProgram_AssignmentMissingRightValue_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <-";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试给字面量赋值
    /// </summary>
    [Fact]
    public void ParseProgram_AssignToLiteral_ThrowsSyntaxError()
    {
        // Arrange
        var code = "123 <- a";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }
}
