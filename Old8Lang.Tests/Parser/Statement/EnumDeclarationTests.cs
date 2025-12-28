using Old8Lang.AST.Statement;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Parser.Statement;

/// <summary>
/// 枚举声明语句测试
/// 测试枚举定义的语法解析，包括正确语法、边界情况和错误处理
/// </summary>
[Collection("Sequential")]
public class EnumDeclarationTests
{
    #region 正确语法测试

    /// <summary>
    /// 测试空枚举声明
    /// </summary>
    [Fact]
    public void ParseProgram_EmptyEnum_ParsesSuccessfully()
    {
        // Arrange
        var code = "enum Empty {}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act
        var program = parser.ParseProgram();

        // Assert
        Assert.NotNull(program);
        Assert.Equal(1, program.Count);
        Assert.IsType<EnumInit>(program[0]);
    }

    /// <summary>
    /// 测试简单枚举声明（自动值）
    /// </summary>
    [Fact]
    public void ParseProgram_SimpleEnum_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
enum Color {
    Red,
    Green,
    Blue
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act
        var program = parser.ParseProgram();

        // Assert
        Assert.NotNull(program);
        Assert.Equal(1, program.Count);
        var enumDecl = Assert.IsType<EnumInit>(program[0]);
        Assert.Equal(3, enumDecl.Members.Count);
    }

    /// <summary>
    /// 测试带显式值的枚举
    /// </summary>
    [Fact]
    public void ParseProgram_EnumWithExplicitValues_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
enum HttpStatus {
    OK <- 200,
    NotFound <- 404,
    InternalServerError <- 500
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act
        var program = parser.ParseProgram();

        // Assert
        Assert.NotNull(program);
        Assert.Equal(1, program.Count);
        var enumDecl = Assert.IsType<EnumInit>(program[0]);
        Assert.Equal(3, enumDecl.Members.Count);
    }

    /// <summary>
    /// 测试混合自动和显式值的枚举
    /// </summary>
    [Fact]
    public void ParseProgram_EnumWithMixedValues_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
enum Priority {
    Low,
    Medium <- 5,
    High,
    Critical <- 10
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act
        var program = parser.ParseProgram();

        // Assert
        Assert.NotNull(program);
        Assert.Equal(1, program.Count);
        var enumDecl = Assert.IsType<EnumInit>(program[0]);
        Assert.Equal(4, enumDecl.Members.Count);
    }

    /// <summary>
    /// 测试单成员枚举
    /// </summary>
    [Fact]
    public void ParseProgram_SingleMemberEnum_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
enum SingleValue {
    OnlyOne
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act
        var program = parser.ParseProgram();

        // Assert
        Assert.NotNull(program);
        Assert.Equal(1, program.Count);
        var enumDecl = Assert.IsType<EnumInit>(program[0]);
        Assert.Single(enumDecl.Members);
    }

    /// <summary>
    /// 测试带尾随逗号的枚举
    /// </summary>
    [Fact]
    public void ParseProgram_EnumWithTrailingComma_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
enum Direction {
    North,
    South,
    East,
    West,
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act
        var program = parser.ParseProgram();

        // Assert
        Assert.NotNull(program);
        Assert.Equal(1, program.Count);
        var enumDecl = Assert.IsType<EnumInit>(program[0]);
        Assert.Equal(4, enumDecl.Members.Count);
    }

    /// <summary>
    /// 测试带访问修饰符的枚举
    /// </summary>
    [Fact]
    public void ParseProgram_EnumWithAccessModifier_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
public enum PublicEnum {
    Value1,
    Value2
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act
        var program = parser.ParseProgram();

        // Assert
        Assert.NotNull(program);
        Assert.Equal(1, program.Count);
        Assert.IsType<EnumInit>(program[0]);
    }

    #endregion

    #region 边界测试

    /// <summary>
    /// 测试枚举成员名称使用关键字（应该失败）
    /// </summary>
    [Fact]
    public void ParseProgram_EnumMemberWithKeywordName_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
enum Invalid {
    if,
    else
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试非常长的枚举成员名称
    /// </summary>
    [Fact]
    public void ParseProgram_EnumWithVeryLongMemberName_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
enum Test {
    ThisIsAVeryVeryVeryLongEnumMemberNameThatShouldStillBeValid
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act
        var program = parser.ParseProgram();

        // Assert
        Assert.NotNull(program);
        Assert.Equal(1, program.Count);
        var enumDecl = Assert.IsType<EnumInit>(program[0]);
        Assert.Single(enumDecl.Members);
    }

    /// <summary>
    /// 测试负数值的枚举
    /// </summary>
    [Fact]
    public void ParseProgram_EnumWithNegativeValues_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
enum Temperature {
    Cold <- -10,
    Freezing <- -5,
    Normal <- 0,
    Hot <- 25
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act
        var program = parser.ParseProgram();

        // Assert
        Assert.NotNull(program);
        Assert.Equal(1, program.Count);
        var enumDecl = Assert.IsType<EnumInit>(program[0]);
        Assert.Equal(4, enumDecl.Members.Count);
    }

    #endregion

    #region 错误语法测试

    /// <summary>
    /// 测试缺少枚举名称
    /// </summary>
    [Fact]
    public void ParseProgram_EnumMissingName_ThrowsSyntaxError()
    {
        // Arrange
        var code = "enum { Value1, Value2 }";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试缺少左花括号
    /// </summary>
    [Fact]
    public void ParseProgram_EnumMissingLeftBrace_ThrowsSyntaxError()
    {
        // Arrange
        var code = "enum Test Value1, Value2 }";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试缺少右花括号
    /// </summary>
    [Fact]
    public void ParseProgram_EnumMissingRightBrace_ThrowsSyntaxError()
    {
        // Arrange
        var code = "enum Test { Value1, Value2";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试枚举成员值缺少赋值运算符
    /// </summary>
    [Fact]
    public void ParseProgram_EnumMemberValueMissingAssignment_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
enum Test {
    Value1 200
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试枚举成员之间缺少逗号
    /// </summary>
    [Fact]
    public void ParseProgram_EnumMembersMissingComma_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
enum Test {
    Value1
    Value2
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试枚举名称以数字开头
    /// </summary>
    [Fact]
    public void ParseProgram_EnumNameStartsWithNumber_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
enum 123Test {
    Value1
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试枚举成员名称以数字开头
    /// </summary>
    [Fact]
    public void ParseProgram_EnumMemberNameStartsWithNumber_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
enum Test {
    123Value
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion
}
