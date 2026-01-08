using Old8Lang.Interpreter;
using Old8Lang.LangParser;

namespace Old8Lang.Tests.EdgeCases;

/// <summary>
/// 预编译指令边界情况测试
/// </summary>
[Collection("Sequential")]
public class PreprocessorEdgeCasesTests
{
    [Fact]
    public void Preprocessor_EmptyCode_ReturnsEmptyResult()
    {
        // Arrange
        var code = "";
        var symbols = new PreprocessorSymbols();
        var preprocessor = new PreprocessorTokenizer(code, symbols);

        // Act
        var result = preprocessor.Process();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Preprocessor_OnlyWhitespace_ReturnsWhitespace()
    {
        // Arrange
        var code = "   \n\t\n  ";
        var symbols = new PreprocessorSymbols();
        var preprocessor = new PreprocessorTokenizer(code, symbols);

        // Act
        var result = preprocessor.Process();

        // Assert
        Assert.Contains("\n", result);
    }

    [Fact]
    public void Preprocessor_HashInString_NotProcessed()
    {
        // Arrange
        var code = @"
PrintLine(""This # is not a directive"")
PrintLine(""#define is not processed"")
";
        var symbols = new PreprocessorSymbols();
        var preprocessor = new PreprocessorTokenizer(code, symbols);

        // Act
        var result = preprocessor.Process();

        // Assert
        Assert.Contains("This # is not a directive", result);
        Assert.Contains("#define is not processed", result);
    }

    [Fact]
    public void Preprocessor_HashInComment_NotProcessed()
    {
        // Arrange
        var code = @"
// This # is in a comment
/* This #define is also in a comment */
PrintLine(""Test"")
";
        var symbols = new PreprocessorSymbols();
        var preprocessor = new PreprocessorTokenizer(code, symbols);

        // Act
        var result = preprocessor.Process();

        // Assert
        Assert.Contains("// This # is in a comment", result);
        Assert.Contains("/* This #define is also in a comment */", result);
    }

    [Fact]
    public void Preprocessor_MultipleDefinesOnSameLine_NotSupported()
    {
        // Arrange - 预编译指令应该每行一个
        var code = @"
result:int <- 0
#define A
#define B
#if A && B
    result <- 1
#endif
Assert.Equal(1, result)
";
        var symbols = new PreprocessorSymbols();
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code, null, symbols);

        // Assert - 如果 Old8Lang 断言失败会抛出异常
        var exception = Record.Exception(() => ast.Run(interpreter.Manager));
        Assert.Null(exception);
    }

    [Fact]
    public void Preprocessor_WhitespaceBeforeDirective_Allowed()
    {
        // Arrange
        var code = @"
result:int <- 0
    #define DEBUG
    #if DEBUG
        result <- 1
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
    public void Preprocessor_DirectiveNotAtLineStart_NotProcessed()
    {
        // Arrange
        var code = @"
result:int <- 0
PrintLine(""test"") #define DEBUG
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

        // Assert - DEBUG 不应该被定义，因为 #define 不在行首
        var exception = Record.Exception(() => ast.Run(interpreter.Manager));
        Assert.Null(exception);
    }

    [Fact]
    public void Preprocessor_VeryLongSymbolName_WorksCorrectly()
    {
        // Arrange
        var longSymbol = "VERY_LONG_SYMBOL_NAME_WITH_MANY_CHARACTERS_ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var code = $@"
result:int <- 0
#define {longSymbol}
#if {longSymbol}
    result <- 1
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
    public void Preprocessor_DeeplyNestedIf_WorksCorrectly()
    {
        // Arrange
        var code = @"
result:int <- 0
#define L1
#define L2
#define L3
#define L4
#if L1
    #if L2
        #if L3
            #if L4
                result <- 4
            #endif
        #endif
    #endif
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
    public void Preprocessor_MixedLineEndings_HandledCorrectly()
    {
        // Arrange - 测试 CRLF 和 LF 混合
        var code = "#define DEBUG\r\n#if DEBUG\nresult <- 1\r\n#endif\n";
        var symbols = new PreprocessorSymbols();
        var preprocessor = new PreprocessorTokenizer(code, symbols);

        // Act
        var result = preprocessor.Process();

        // Assert
        Assert.Contains("result <- 1", result);
    }

    [Fact]
    public void Preprocessor_MultipleCommandLineSymbols_AllDefined()
    {
        // Arrange
        var code = @"
result:int <- 0
#if SYMBOL1 && SYMBOL2 && SYMBOL3
    result <- 123
#endif
Assert.Equal(123, result)
";
        var symbols = new PreprocessorSymbols(new[] { "SYMBOL1", "SYMBOL2", "SYMBOL3" });
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code, null, symbols);

        // Assert
        var exception = Record.Exception(() => ast.Run(interpreter.Manager));
        Assert.Null(exception);
    }

    [Fact]
    public void Preprocessor_RedefineSameSymbol_LastDefinitionWins()
    {
        // Arrange
        var code = @"
result:int <- 0
#define TEST
#undef TEST
#define TEST
#if TEST
    result <- 1
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
    public void Preprocessor_ComplexNestedConditions_WorkCorrectly()
    {
        // Arrange
        var code = @"
result:int <- 0
#define A
#define B
#if A
    #if B
        result <- 1
    #else
        result <- 2
    #endif
#elif B
    result <- 3
#else
    result <- 4
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
}
