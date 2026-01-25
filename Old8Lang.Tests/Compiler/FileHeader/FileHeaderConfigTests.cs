using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.FileHeader;

/// <summary>
/// 文件头配置指令测试 - 测试编译器/解释器配置指令
/// </summary>
[Collection("Sequential")]
public class FileHeaderConfigTests
{
    #region debug 指令测试

    /// <summary>
    /// 测试 debug 指令 - 启用调试
    /// </summary>
    [Fact]
    public void Run_DebugDirectiveTrue_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
#!debug true

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
    /// 测试 debug 指令 - 禁用调试
    /// </summary>
    [Fact]
    public void Run_DebugDirectiveFalse_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
#!debug false

a <- 5
b <- 3
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

    #region verify-il 指令测试

    /// <summary>
    /// 测试 verify-il 指令（编译模式）
    /// </summary>
    [Fact]
    public void Run_VerifyIlDirective_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
#!verify-il true

value <- 42
result <- value + 8";
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

    #region type-inference 指令测试

    /// <summary>
    /// 测试 type-inference 指令 - 启用类型推断
    /// </summary>
    [Fact]
    public void Run_TypeInferenceTrue_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
#!type-inference true

x <- 100
y <- 50
result <- x - y";
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
    /// 测试 type-inference 指令 - 禁用类型推断
    /// </summary>
    [Fact]
    public void Run_TypeInferenceFalse_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
#!type-inference false

a <- 7
b <- 6
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

    #region type-inference-debug 指令测试

    /// <summary>
    /// 测试 type-inference-debug 指令
    /// </summary>
    [Fact]
    public void Run_TypeInferenceDebugDirective_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
#!type-inference-debug false

num <- 99
result <- num + 1";
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

    #region optimize 指令测试

    /// <summary>
    /// 测试 optimize 指令 - 优化级别 0
    /// </summary>
    [Fact]
    public void Run_OptimizeLevel0_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
#!optimize 0

x <- 2
y <- 3
result <- x * y";
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
    /// 测试 optimize 指令 - 优化级别 1
    /// </summary>
    [Fact]
    public void Run_OptimizeLevel1_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
#!optimize 1

a <- 10
b <- 5
result <- a / b";
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
    /// 测试 optimize 指令 - 优化级别 2
    /// </summary>
    [Fact]
    public void Run_OptimizeLevel2_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
#!optimize 2

x <- 8
y <- 4
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
    /// 测试 optimize 指令 - 优化级别 3
    /// </summary>
    [Fact]
    public void Run_OptimizeLevel3_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
#!optimize 3

m <- 15
n <- 3
result <- m - n";
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

    #region 多个配置指令组合测试

    /// <summary>
    /// 测试多个配置指令组合
    /// </summary>
    [Fact]
    public void Run_MultipleConfigDirectives_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
#!debug false
#!verify-il true
#!type-inference true
#!optimize 2

a <- 20
b <- 10
c <- 5
result <- (a + b) * c";
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
    /// 测试元数据和配置指令混合
    /// </summary>
    [Fact]
    public void Run_MetadataAndConfigMixed_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
#!encoding utf-8
#!author 测试作者
#!version 1.0
#!debug true
#!type-inference true
#!optimize 1

x <- 7
y <- 8
result <- x * y";
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

    #region 配置值类型测试

    /// <summary>
    /// 测试布尔类型配置值
    /// </summary>
    [Fact]
    public void Run_BooleanConfigValues_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
#!debug TRUE
#!type-inference FALSE

value <- 33
result <- value + 67";
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
    /// 测试数字类型配置值
    /// </summary>
    [Fact]
    public void Run_NumericConfigValues_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
#!optimize 2

x <- 25
y <- 25
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

    #endregion

    #region 边界情况测试

    /// <summary>
    /// 测试无效的优化级别（应该使用默认值或忽略）
    /// </summary>
    [Fact]
    public void Run_InvalidOptimizeLevel_UsesDefaultOrIgnores()
    {
        // Arrange
        var code = @"
#!optimize 999

a <- 12
b <- 8
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
    /// 测试未知的配置指令（应该被忽略）
    /// </summary>
    [Fact]
    public void Run_UnknownConfigDirective_IsIgnored()
    {
        // Arrange
        var code = @"
#!unknown-directive some-value
#!another-unknown true

x <- 45
y <- 55
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

    #endregion
}
