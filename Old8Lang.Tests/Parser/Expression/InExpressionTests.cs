using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Parser.Expression;

/// <summary>
/// In表达式解析测试
/// 测试in表达式的解析和功能，以及与for-in循环的区分
/// </summary>
[Collection("Sequential")]
public class InExpressionTests
{
    #region 基本in表达式解析

    /// <summary>
    /// 测试基本的in表达式解析
    /// </summary>
    [Fact]
    public void ParseProgram_BasicInExpression_ShouldSucceed()
    {
        // Arrange
        var code = "a <- 1 in [1, 2, 3]";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var program = parser.ParseProgram();

        // Assert
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试in表达式作为条件的解析
    /// </summary>
    [Fact]
    public void ParseProgram_InExpressionAsCondition_ShouldSucceed()
    {
        // Arrange
        var code = "if 5 in [1, 3, 5, 7] { a <- 1 }";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var program = parser.ParseProgram();

        // Assert
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试in表达式与其他操作符混合使用
    /// </summary>
    [Fact]
    public void ParseProgram_InExpressionWithOtherOperators_ShouldSucceed()
    {
        // Arrange
        var code = "a <- 1 in [1, 2, 3] and 4 in [3, 4, 5]";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var program = parser.ParseProgram();

        // Assert
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试in表达式在字符串中的应用
    /// </summary>
    [Fact]
    public void ParseProgram_InExpressionWithString_ShouldSucceed()
    {
        // Arrange
        var code = "a <- 'a' in 'abc'";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var program = parser.ParseProgram();

        // Assert
        Assert.NotNull(program);
    }

    #endregion

    #region for-in循环与in表达式区分

    /// <summary>
    /// 测试for-in循环解析
    /// </summary>
    [Fact]
    public void ParseProgram_ForInLoop_ShouldSucceed()
    {
        // Arrange
        var code = "for item in [1, 2, 3] { a <- item }";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var program = parser.ParseProgram();

        // Assert
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试for-in循环与in表达式在同一代码中的解析
    /// </summary>
    [Fact]
    public void ParseProgram_ForInLoopAndInExpression_ShouldSucceed()
    {
        // Arrange
        var code = @"for item in [1, 2, 3] {
            if item in [2, 4, 6] {
                a <- item
            }
        }";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var program = parser.ParseProgram();

        // Assert
        Assert.NotNull(program);
    }

    /// <summary>
    /// 测试for-in循环与in表达式的区分
    /// </summary>
    [Fact]
    public void ParseProgram_DistinguishForInAndInExpression_ShouldSucceed()
    {
        // Arrange
        var code = @"// for-in循环
a <- 0
for item in [1, 2, 3] {
    a <- a + 1
}

// in表达式
b <- 2 in [1, 2, 3]";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var program = parser.ParseProgram();

        // Assert
        Assert.NotNull(program);
    }

    #endregion

    #region in表达式错误处理

    /// <summary>
    /// 测试不完整的in表达式
    /// </summary>
    [Fact]
    public void ParseProgram_IncompleteInExpression_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- 1 in";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试缺少右侧表达式的in表达式
    /// </summary>
    [Fact]
    public void ParseProgram_MissingRightExpressionInIn_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a <- 1 in ";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion
}
