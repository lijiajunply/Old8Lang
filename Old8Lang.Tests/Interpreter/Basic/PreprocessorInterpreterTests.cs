using Old8Lang.Interpreter;
using Old8Lang.LangParser;

namespace Old8Lang.Tests.Interpreter.Basic;

/// <summary>
/// 解释器模式预编译指令测试
/// </summary>
[Collection("Sequential")]
public class PreprocessorInterpreterTests
{
    [Fact]
    public void Interpreter_DefineAndUse_ExecutesCorrectBranch()
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

        // Assert
        var exception = Record.Exception(() => ast.Run(interpreter.Manager));
        Assert.Null(exception);
    }

    [Fact]
    public void Interpreter_UndefSymbol_ExecutesElseBranch()
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

        // Assert
        var exception = Record.Exception(() => ast.Run(interpreter.Manager));
        Assert.Null(exception);
    }

    [Fact]
    public void Interpreter_AndCondition_WorksCorrectly()
    {
        // Arrange
        var code = @"
result:int <- 0
#define FEATURE_A
#define FEATURE_B
#if FEATURE_A && FEATURE_B
    result <- 3
#endif
Assert.Equal(3, result)
";
        var symbols = new PreprocessorSymbols();
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code, null, symbols);

        // Assert
        var exception = Record.Exception(() => ast.Run(interpreter.Manager));
        Assert.Null(exception);
    }

    [Fact]
    public void Interpreter_OrCondition_WorksCorrectly()
    {
        // Arrange
        var code = @"
result:int <- 0
#define FEATURE_A
#if FEATURE_A || FEATURE_B
    result <- 4
#endif
Assert.Equal(4, result)
";
        var symbols = new PreprocessorSymbols();
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code, null, symbols);

        // Assert
        var exception = Record.Exception(() => ast.Run(interpreter.Manager));
        Assert.Null(exception);
    }

    [Fact]
    public void Interpreter_NotCondition_WorksCorrectly()
    {
        // Arrange
        var code = @"
result:int <- 0
#if !UNDEFINED_SYMBOL
    result <- 5
#endif
Assert.Equal(5, result)
";
        var symbols = new PreprocessorSymbols();
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code, null, symbols);

        // Assert
        var exception = Record.Exception(() => ast.Run(interpreter.Manager));
        Assert.Null(exception);
    }

    [Fact]
    public void Interpreter_ElifCondition_ExecutesCorrectBranch()
    {
        // Arrange
        var code = @"
result:int <- 0
#define FEATURE_B
#if FEATURE_A
    result <- 1
#elif FEATURE_B
    result <- 2
#else
    result <- 3
#endif
Assert.Equal(2, result)
";
        var symbols = new PreprocessorSymbols();
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code, null, symbols);

        // Assert
        var exception = Record.Exception(() => ast.Run(interpreter.Manager));
        Assert.Null(exception);
    }

    [Fact]
    public void Interpreter_NestedIf_WorksCorrectly()
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

        // Assert
        var exception = Record.Exception(() => ast.Run(interpreter.Manager));
        Assert.Null(exception);
    }

    [Fact]
    public void Interpreter_CommandLineSymbols_WorkCorrectly()
    {
        // Arrange
        var code = @"
result:int <- 0
#if PRODUCTION
    result <- 10
#endif
Assert.Equal(10, result)
";
        var symbols = new PreprocessorSymbols(["PRODUCTION"]);
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code, null, symbols);

        // Assert
        var exception = Record.Exception(() => ast.Run(interpreter.Manager));
        Assert.Null(exception);
    }

    [Fact]
    public void Interpreter_ComplexCondition_WorksCorrectly()
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

        // Assert
        var exception = Record.Exception(() => ast.Run(interpreter.Manager));
        Assert.Null(exception);
    }

    [Fact]
    public void Interpreter_InactiveBranchCodeNotExecuted()
    {
        // Arrange
        var code = @"
result:int <- 0
#if UNDEFINED
    invalidSyntax!!!
    result <- 999
#else
    result <- 42
#endif
Assert.Equal(42, result)
";
        var symbols = new PreprocessorSymbols();
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code, null, symbols);

        // Assert - 不应该抛出语法错误，因为未激活的代码被移除了
        var exception = Record.Exception(() => ast.Run(interpreter.Manager));
        Assert.Null(exception);
    }
}
