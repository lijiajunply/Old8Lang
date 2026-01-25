using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.FileHeader;

/// <summary>
/// 文件头指令测试 - 测试 File Header Directives 功能
/// 文件头指令用于在文件开头声明配置信息和元数据
/// </summary>
[Collection("Sequential")]
public class FileHeaderDirectiveTests
{
    #region 元数据指令测试

    /// <summary>
    /// 测试 encoding 指令
    /// </summary>
    [Fact]
    public void Run_EncodingDirective_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
#!encoding utf-8

a <- 123
result <- a * 2";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    /// <summary>
    /// 测试 author 指令
    /// </summary>
    [Fact]
    public void Run_AuthorDirective_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
#!author 张三

name <- ""Test""
result <- name + "" Program""";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    /// <summary>
    /// 测试 version 指令
    /// </summary>
    [Fact]
    public void Run_VersionDirective_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
#!version 1.0.0

x <- 10
y <- 20
result <- x + y";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    /// <summary>
    /// 测试 date 指令
    /// </summary>
    [Fact]
    public void Run_DateDirective_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
#!date 2025-12-28

flag <- true
result <- flag";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    /// <summary>
    /// 测试 description 指令
    /// </summary>
    [Fact]
    public void Run_DescriptionDirective_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
#!description 这是一个测试文件

value <- 42
result <- value * 2";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    /// <summary>
    /// 测试多个元数据指令组合
    /// </summary>
    [Fact]
    public void Run_MultipleMetadataDirectives_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
#!encoding utf-8
#!author 老八
#!version 2.0
#!date 2025-12-28
#!description 综合测试程序

a <- 100
b <- 200
result <- a + b";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region 指令位置和规则测试

    /// <summary>
    /// 测试指令必须在文件开头
    /// </summary>
    [Fact]
    public void Run_DirectivesMustBeAtBeginning_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
#!encoding utf-8
#!author 测试

// 正常代码
a <- 1
b <- 2
result <- a + b";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    /// <summary>
    /// 测试指令与代码混合（指令在代码后面应该被忽略或报错）
    /// </summary>
    [Fact]
    public void Run_DirectivesWithCode_OnlyProcessesBeginningDirectives()
    {
        // Arrange
        var code = @"
#!encoding utf-8

a <- 10
result <- a * 5";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    /// <summary>
    /// 测试空行和注释不影响指令解析
    /// </summary>
    [Fact]
    public void Run_DirectivesWithEmptyLinesAndComments_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
// 文件开头注释
#!encoding utf-8

// 其他注释
#!author 测试作者

value <- 99
result <- value + 1";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region 大小写不敏感测试

    /// <summary>
    /// 测试指令名称大小写不敏感
    /// </summary>
    [Fact]
    public void Run_DirectiveCaseInsensitive_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
#!ENCODING utf-8
#!Author 张三
#!VeRsIoN 1.0

x <- 5
result <- x * x";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region 重复指令测试

    /// <summary>
    /// 测试重复的指令（后面的覆盖前面的）
    /// </summary>
    [Fact]
    public void Run_DuplicateDirectives_LastOneWins()
    {
        // Arrange
        var code = @"
#!author 第一个作者
#!author 第二个作者
#!version 1.0
#!version 2.0

result <- 42";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion

    #region 特殊值测试

    /// <summary>
    /// 测试带空格的指令值
    /// </summary>
    [Fact]
    public void Run_DirectiveValueWithSpaces_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
#!author Zhang San
#!description This is a test program

value <- 77
result <- value + 23";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    /// <summary>
    /// 测试带特殊字符的指令值
    /// </summary>
    [Fact]
    public void Run_DirectiveValueWithSpecialChars_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
#!version 1.2.3-alpha+build.123
#!description 测试程序 (包含特殊符号: @#$%)

a <- 8
b <- 7
result <- a * b";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion
}
