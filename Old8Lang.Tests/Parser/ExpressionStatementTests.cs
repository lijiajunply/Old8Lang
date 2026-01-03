using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Parser;

/// <summary>
/// 表达式语句测试 - 验证单独表达式被正确禁止
/// </summary>
[Collection("Sequential")]
public class ExpressionStatementTests
{
    /// <summary>
    /// 测试单独标识符被禁止
    /// </summary>
    [Fact]
    public void ParseProgram_SingleIdentifier_ThrowsSyntaxError()
    {
        // Arrange
        var code = "x";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        var exception = Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
        Assert.Contains("不能作为独立语句使用", exception.Message);
    }

    /// <summary>
    /// 测试算术表达式被禁止
    /// </summary>
    [Fact]
    public void ParseProgram_ArithmeticExpression_ThrowsSyntaxError()
    {
        // Arrange
        var code = "x + y";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        var exception = Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
        Assert.Contains("不能作为独立语句使用", exception.Message);
    }

    /// <summary>
    /// 测试比较表达式被禁止
    /// </summary>
    [Fact]
    public void ParseProgram_ComparisonExpression_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a > b";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        var exception = Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
        Assert.Contains("不能作为独立语句使用", exception.Message);
    }

    /// <summary>
    /// 测试逻辑表达式被禁止
    /// </summary>
    [Fact]
    public void ParseProgram_LogicalExpression_ThrowsSyntaxError()
    {
        // Arrange
        var code = "a && b";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        var exception = Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
        Assert.Contains("不能作为独立语句使用", exception.Message);
    }

    /// <summary>
    /// 测试函数调用是允许的
    /// </summary>
    [Fact]
    public void ParseProgram_FunctionCall_ParsesSuccessfully()
    {
        // Arrange
        var code = "PrintLine(\"Hello\")";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
    }

    /// <summary>
    /// 测试赋值语句是允许的
    /// </summary>
    [Fact]
    public void ParseProgram_AssignmentStatement_ParsesSuccessfully()
    {
        // Arrange
        var code = "a <- x + y";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
    }

    /// <summary>
    /// 测试 return 语句是允许的（作为顶层函数声明）
    /// </summary>
    [Fact]
    public void ParseProgram_FunctionWithReturnStatement_ParsesSuccessfully()
    {
        // Arrange
        var code = @"func test() {
    return x + y
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        // 函数声明被添加到 ImportStatements 中，不计入 Count
        // 检查解析结果包含函数声明内容
        var output = result.ToString();
        Assert.Contains("func test()", output);
        Assert.Contains("return", output);
    }

    /// <summary>
    /// 测试块中的单独表达式被禁止
    /// </summary>
    [Fact]
    public void ParseProgram_ExpressionInBlock_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"if true {
    x
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        var exception = Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
        Assert.Contains("不能作为独立语句使用", exception.Message);
    }

    /// <summary>
    /// 测试复杂表达式被禁止
    /// </summary>
    [Fact]
    public void ParseProgram_ComplexExpression_ThrowsSyntaxError()
    {
        // Arrange
        var code = "(x + y) * (a - b)";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        var exception = Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
        Assert.Contains("不能作为独立语句使用", exception.Message);
    }

    /// <summary>
    /// 测试成员访问表达式被禁止
    /// </summary>
    [Fact]
    public void ParseProgram_MemberAccessExpression_ThrowsSyntaxError()
    {
        // Arrange
        var code = "obj.field";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        var exception = Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
        Assert.Contains("不能作为独立语句使用", exception.Message);
    }

    /// <summary>
    /// 测试数组/列表字面量被禁止
    /// </summary>
    [Fact]
    public void ParseProgram_ArrayLiteral_ThrowsSyntaxError()
    {
        // Arrange
        var code = "[1, 2, 3]";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试字符串字面量被禁止
    /// </summary>
    [Fact]
    public void ParseProgram_StringLiteral_ThrowsSyntaxError()
    {
        // Arrange
        var code = "\"Hello\"";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        var exception = Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
        Assert.Contains("不能作为独立语句使用", exception.Message);
    }

    /// <summary>
    /// 测试数字字面量被禁止
    /// </summary>
    [Fact]
    public void ParseProgram_NumberLiteral_ThrowsSyntaxError()
    {
        // Arrange
        var code = "42";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        var exception = Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
        Assert.Contains("不能作为独立语句使用", exception.Message);
    }

    /// <summary>
    /// 测试自增语句是允许的
    /// </summary>
    [Fact]
    public void ParseProgram_IncrementStatement_ParsesSuccessfully()
    {
        // Arrange
        var code = "i++";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
    }

    /// <summary>
    /// 测试自减语句是允许的
    /// </summary>
    [Fact]
    public void ParseProgram_DecrementStatement_ParsesSuccessfully()
    {
        // Arrange
        var code = "i--";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
    }

    /// <summary>
    /// 测试成员访问赋值语句
    /// </summary>
    [Fact]
    public void ParseProgram_MemberAccessAssignment_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- obj.field";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
    }

    /// <summary>
    /// 测试错误消息包含有用的建议
    /// </summary>
    [Fact]
    public void ParseProgram_ExpressionError_ContainsUsefulSuggestions()
    {
        // Arrange
        var code = "x + y";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        var exception = Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
        Assert.Contains("建议", exception.Message);
        Assert.Contains("<-", exception.Message); // 应该建议使用赋值
        Assert.Contains("return", exception.Message); // 应该建议使用 return
    }

    /// <summary>
    /// 测试成员方法调用是允许的 (obj.method() 形式)
    /// </summary>
    [Fact]
    public void ParseProgram_MemberMethodCall_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
class Person {
    public func sayHello() {
        PrintLine(""Hello"")
    }
}
person <- Person()
person.sayHello()";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        // 验证解析成功（没有抛出异常）
        Assert.True(result.Count >= 0);
    }
}
