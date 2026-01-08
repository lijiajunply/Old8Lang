using Old8Lang.Interpreter;
using Old8Lang.LangParser;

namespace Old8Lang.Tests.Compiler.Basic;

/// <summary>
/// 编译器模式预编译指令测试
/// </summary>
[Collection("Sequential")]
public class PreprocessorCompilerTests
{
    [Fact]
    public void Compiler_DefineAndUse_ExecutesCorrectBranch()
    {
        // Arrange
        var code = @"
result:int <- 0
#define DEBUG
#if DEBUG
    result <- 1
#else
    result <- 2
#endif
Assert.Equal(1, result)
";
        var symbols = new PreprocessorSymbols();
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code, null, symbols);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, null, interpreter);

        // Assert - 如果 Old8Lang 断言失败会抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void Compiler_UndefSymbol_ExecutesElseBranch()
    {
        // Arrange
        var code = @"
result:int <- 0
#define DEBUG
#undef DEBUG
#if DEBUG
    result <- 1
#else
    result <- 2
#endif
Assert.Equal(2, result)
";
        var symbols = new PreprocessorSymbols();
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code, null, symbols);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, null, interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void Compiler_CommandLineSymbols_WorkCorrectly()
    {
        // Arrange
        var code = @"
result:int <- 0
#if PRODUCTION
    result <- 10
#else
    result <- 5
#endif
Assert.Equal(10, result)
";
        var symbols = new PreprocessorSymbols(new[] { "PRODUCTION" });
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code, null, symbols);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, null, interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void Compiler_ConditionalFunction_CompilesCorrectly()
    {
        // Arrange
        var code = @"
#define DEBUG

func getMode() -> string {
    #if DEBUG
        return ""debug""
    #else
        return ""release""
    #endif
}

result:string <- getMode()
Assert.Equal(""debug"", result)
";
        var symbols = new PreprocessorSymbols();
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code, null, symbols);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, null, interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void Compiler_ComplexCondition_WorksCorrectly()
    {
        // Arrange
        var code = @"
result:int <- 0
#define DEBUG
#define FEATURE_A
#if DEBUG && (FEATURE_A || FEATURE_B)
    result <- 99
#endif
Assert.Equal(99, result)
";
        var symbols = new PreprocessorSymbols();
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code, null, symbols);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, null, interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void Compiler_NestedConditions_CompileCorrectly()
    {
        // Arrange
        var code = @"
result:int <- 0
#define OUTER
#define INNER
#if OUTER
    result <- 1
    #if INNER
        result <- 2
    #endif
#endif
Assert.Equal(2, result)
";
        var symbols = new PreprocessorSymbols();
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code, null, symbols);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, null, interpreter);

        // Assert
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    [Fact]
    public void Compiler_InactiveBranchNotCompiled()
    {
        // Arrange
        var code = @"
result:int <- 0
#if UNDEFINED
    // 这段代码不应该被编译
    invalidSyntax!!!
    result <- 999
#else
    result <- 42
#endif
Assert.Equal(42, result)
";
        var symbols = new PreprocessorSymbols();
        var interpreter = new LangInterpreter();

        // Act & Assert - 不应该抛出编译错误
        var ast = interpreter.Build(code, null, symbols);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, null, interpreter);
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }
}
