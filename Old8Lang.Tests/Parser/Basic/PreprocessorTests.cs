using Old8Lang.LangParser;

namespace Old8Lang.Tests.Parser.Basic;

/// <summary>
/// 预编译指令解析测试
/// </summary>
[Collection("Sequential")]
public class PreprocessorTests
{
    [Fact]
    public void Preprocessor_DefineSymbol_SymbolIsDefined()
    {
        // Arrange
        var symbols = new PreprocessorSymbols();

        // Act
        symbols.DefineSymbol("DEBUG");

        // Assert
        Assert.True(symbols.IsDefined("DEBUG"));
    }

    [Fact]
    public void Preprocessor_UndefSymbol_SymbolIsUndefined()
    {
        // Arrange
        var symbols = new PreprocessorSymbols();
        symbols.DefineSymbol("DEBUG");

        // Act
        symbols.UndefineSymbol("DEBUG");

        // Assert
        Assert.False(symbols.IsDefined("DEBUG"));
    }

    [Fact]
    public void Preprocessor_EvaluateCondition_SimpleSymbol_ReturnsTrue()
    {
        // Arrange
        var symbols = new PreprocessorSymbols();
        symbols.DefineSymbol("DEBUG");

        // Act
        var result = symbols.EvaluateCondition("DEBUG");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Preprocessor_EvaluateCondition_UndefinedSymbol_ReturnsFalse()
    {
        // Arrange
        var symbols = new PreprocessorSymbols();

        // Act
        var result = symbols.EvaluateCondition("DEBUG");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Preprocessor_EvaluateCondition_NotOperator_ReturnsCorrectResult()
    {
        // Arrange
        var symbols = new PreprocessorSymbols();
        symbols.DefineSymbol("DEBUG");

        // Act
        var result1 = symbols.EvaluateCondition("!DEBUG");
        var result2 = symbols.EvaluateCondition("!RELEASE");

        // Assert
        Assert.False(result1);
        Assert.True(result2);
    }

    [Fact]
    public void Preprocessor_EvaluateCondition_AndOperator_ReturnsCorrectResult()
    {
        // Arrange
        var symbols = new PreprocessorSymbols();
        symbols.DefineSymbol("DEBUG");
        symbols.DefineSymbol("FEATURE_A");

        // Act
        var result1 = symbols.EvaluateCondition("DEBUG && FEATURE_A");
        var result2 = symbols.EvaluateCondition("DEBUG && FEATURE_B");

        // Assert
        Assert.True(result1);
        Assert.False(result2);
    }

    [Fact]
    public void Preprocessor_EvaluateCondition_OrOperator_ReturnsCorrectResult()
    {
        // Arrange
        var symbols = new PreprocessorSymbols();
        symbols.DefineSymbol("DEBUG");

        // Act
        var result1 = symbols.EvaluateCondition("DEBUG || RELEASE");
        var result2 = symbols.EvaluateCondition("FEATURE_A || FEATURE_B");

        // Assert
        Assert.True(result1);
        Assert.False(result2);
    }

    [Fact]
    public void Preprocessor_EvaluateCondition_ComplexExpression_ReturnsCorrectResult()
    {
        // Arrange
        var symbols = new PreprocessorSymbols();
        symbols.DefineSymbol("DEBUG");
        symbols.DefineSymbol("FEATURE_A");

        // Act
        var result = symbols.EvaluateCondition("DEBUG && (FEATURE_A || FEATURE_B)");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Preprocessor_IfDirective_RemovesInactiveCode()
    {
        // Arrange
        var code = @"
#define DEBUG
PrintLine(""Start"")
#if DEBUG
    PrintLine(""Debug mode"")
#endif
PrintLine(""End"")
";
        var symbols = new PreprocessorSymbols();
        var preprocessor = new PreprocessorTokenizer(code, symbols);

        // Act
        var result = preprocessor.Process();

        // Assert
        Assert.Contains("PrintLine(\"Start\")", result);
        Assert.Contains("PrintLine(\"Debug mode\")", result);
        Assert.Contains("PrintLine(\"End\")", result);
        Assert.DoesNotContain("#define", result);
        Assert.DoesNotContain("#if", result);
        Assert.DoesNotContain("#endif", result);
    }

    [Fact]
    public void Preprocessor_IfElseDirective_RemovesCorrectBranch()
    {
        // Arrange
        var code = @"
#if DEBUG
    PrintLine(""Debug"")
#else
    PrintLine(""Release"")
#endif
";
        var symbols = new PreprocessorSymbols();
        var preprocessor = new PreprocessorTokenizer(code, symbols);

        // Act
        var result = preprocessor.Process();

        // Assert
        Assert.DoesNotContain("PrintLine(\"Debug\")", result);
        Assert.Contains("PrintLine(\"Release\")", result);
    }

    [Fact]
    public void Preprocessor_NestedIfDirective_HandlesCorrectly()
    {
        // Arrange
        var code = @"
#define OUTER
#define INNER
#if OUTER
    PrintLine(""Outer"")
    #if INNER
        PrintLine(""Inner"")
    #endif
#endif
";
        var symbols = new PreprocessorSymbols();
        var preprocessor = new PreprocessorTokenizer(code, symbols);

        // Act
        var result = preprocessor.Process();

        // Assert
        Assert.Contains("PrintLine(\"Outer\")", result);
        Assert.Contains("PrintLine(\"Inner\")", result);
    }

    [Fact]
    public void Preprocessor_CommandLineSymbols_ArePreDefined()
    {
        // Arrange
        var code = @"
#if PRODUCTION
    PrintLine(""Production"")
#endif
";
        var symbols = new PreprocessorSymbols(["PRODUCTION"]);
        var preprocessor = new PreprocessorTokenizer(code, symbols);

        // Act
        var result = preprocessor.Process();

        // Assert
        Assert.Contains("PrintLine(\"Production\")", result);
    }

    [Fact]
    public void Preprocessor_PreservesCommentsAndStrings()
    {
        // Arrange
        var code = @"
// This is a comment with #
PrintLine(""String with # character"")
#define DEBUG
#if DEBUG
    PrintLine(""Debug"")
#endif
";
        var symbols = new PreprocessorSymbols();
        var preprocessor = new PreprocessorTokenizer(code, symbols);

        // Act
        var result = preprocessor.Process();

        // Assert
        Assert.Contains("// This is a comment with #", result);
        Assert.Contains("PrintLine(\"String with # character\")", result);
        Assert.Contains("PrintLine(\"Debug\")", result);
    }
}
