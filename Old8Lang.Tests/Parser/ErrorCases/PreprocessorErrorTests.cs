using Old8Lang.LangParser;
using Old8Lang.Error;

namespace Old8Lang.Tests.Parser.ErrorCases;

/// <summary>
/// 预编译指令错误处理测试
/// </summary>
[Collection("Sequential")]
public class PreprocessorErrorTests
{
    [Fact]
    public void Preprocessor_MissingEndif_ThrowsError()
    {
        // Arrange
        var code = @"
#if DEBUG
    PrintLine(""Debug"")
// 缺少 #endif
";
        var symbols = new PreprocessorSymbols();
        var preprocessor = new PreprocessorTokenizer(code, symbols);

        // Act & Assert
        var exception = Assert.Throws<SyntaxError>(() => preprocessor.Process());
        Assert.Contains("未闭合的 #if", exception.Message);
    }

    [Fact]
    public void Preprocessor_EndifWithoutIf_ThrowsError()
    {
        // Arrange
        var code = @"
PrintLine(""Test"")
#endif
";
        var symbols = new PreprocessorSymbols();
        var preprocessor = new PreprocessorTokenizer(code, symbols);

        // Act & Assert
        var exception = Assert.Throws<SyntaxError>(() => preprocessor.Process());
        Assert.Contains("#endif", exception.Message);
        Assert.Contains("没有对应的 #if", exception.Message);
    }

    [Fact]
    public void Preprocessor_ElseWithoutIf_ThrowsError()
    {
        // Arrange
        var code = @"
PrintLine(""Test"")
#else
    PrintLine(""Else"")
#endif
";
        var symbols = new PreprocessorSymbols();
        var preprocessor = new PreprocessorTokenizer(code, symbols);

        // Act & Assert
        var exception = Assert.Throws<SyntaxError>(() => preprocessor.Process());
        Assert.Contains("#else", exception.Message);
        Assert.Contains("没有对应的 #if", exception.Message);
    }

    [Fact]
    public void Preprocessor_ElifWithoutIf_ThrowsError()
    {
        // Arrange
        var code = @"
PrintLine(""Test"")
#elif DEBUG
    PrintLine(""Debug"")
#endif
";
        var symbols = new PreprocessorSymbols();
        var preprocessor = new PreprocessorTokenizer(code, symbols);

        // Act & Assert
        var exception = Assert.Throws<SyntaxError>(() => preprocessor.Process());
        Assert.Contains("#elif", exception.Message);
        Assert.Contains("没有对应的 #if", exception.Message);
    }

    [Fact]
    public void Preprocessor_DefineWithoutSymbol_ThrowsError()
    {
        // Arrange
        var code = @"
#define
";
        var symbols = new PreprocessorSymbols();
        var preprocessor = new PreprocessorTokenizer(code, symbols);

        // Act & Assert
        var exception = Assert.Throws<SyntaxError>(() => preprocessor.Process());
        Assert.Contains("#define", exception.Message);
        Assert.Contains("缺少符号名称", exception.Message);
    }

    [Fact]
    public void Preprocessor_UndefWithoutSymbol_ThrowsError()
    {
        // Arrange
        var code = @"
#undef
";
        var symbols = new PreprocessorSymbols();
        var preprocessor = new PreprocessorTokenizer(code, symbols);

        // Act & Assert
        var exception = Assert.Throws<SyntaxError>(() => preprocessor.Process());
        Assert.Contains("#undef", exception.Message);
        Assert.Contains("缺少符号名称", exception.Message);
    }

    [Fact]
    public void Preprocessor_IfWithoutCondition_ThrowsError()
    {
        // Arrange
        var code = @"
#if
    PrintLine(""Test"")
#endif
";
        var symbols = new PreprocessorSymbols();
        var preprocessor = new PreprocessorTokenizer(code, symbols);

        // Act & Assert
        var exception = Assert.Throws<SyntaxError>(() => preprocessor.Process());
        Assert.Contains("#if", exception.Message);
        Assert.Contains("缺少条件表达式", exception.Message);
    }

    [Fact]
    public void Preprocessor_ElifWithoutCondition_ThrowsError()
    {
        // Arrange
        var code = @"
#if DEBUG
    PrintLine(""Debug"")
#elif
    PrintLine(""Elif"")
#endif
";
        var symbols = new PreprocessorSymbols();
        var preprocessor = new PreprocessorTokenizer(code, symbols);

        // Act & Assert
        var exception = Assert.Throws<SyntaxError>(() => preprocessor.Process());
        Assert.Contains("#elif", exception.Message);
        Assert.Contains("缺少条件表达式", exception.Message);
    }

    [Fact]
    public void Preprocessor_UnknownDirective_ThrowsError()
    {
        // Arrange
        var code = @"
#unknown DIRECTIVE
";
        var symbols = new PreprocessorSymbols();
        var preprocessor = new PreprocessorTokenizer(code, symbols);

        // Act & Assert
        var exception = Assert.Throws<SyntaxError>(() => preprocessor.Process());
        Assert.Contains("未知的预编译指令", exception.Message);
        Assert.Contains("#unknown", exception.Message);
    }

    [Fact]
    public void Preprocessor_MultipleNestedUnmatchedIf_ThrowsError()
    {
        // Arrange
        var code = @"
#if OUTER
    #if INNER
        PrintLine(""Test"")
    // 缺少内层 #endif
// 缺少外层 #endif
";
        var symbols = new PreprocessorSymbols();
        var preprocessor = new PreprocessorTokenizer(code, symbols);

        // Act & Assert
        var exception = Assert.Throws<SyntaxError>(() => preprocessor.Process());
        Assert.Contains("未闭合的 #if", exception.Message);
    }

    [Fact]
    public void Preprocessor_EmptyConditionExpression_ThrowsError()
    {
        // Arrange
        var code = @"
#if
    PrintLine(""Test"")
#endif
";
        var symbols = new PreprocessorSymbols();
        var preprocessor = new PreprocessorTokenizer(code, symbols);

        // Act & Assert
        var exception = Assert.Throws<SyntaxError>(() => preprocessor.Process());
        Assert.Contains("缺少条件表达式", exception.Message);
    }

    [Fact]
    public void Preprocessor_InvalidConditionSyntax_HandledGracefully()
    {
        // Arrange
        var code = @"
#if DEBUG &&
    PrintLine(""Test"")
#endif
";
        var symbols = new PreprocessorSymbols();
        var preprocessor = new PreprocessorTokenizer(code, symbols);

        // Act - 不完整的表达式应该求值为 false
        var result = preprocessor.Process();

        // Assert - 应该不包含 Test，因为条件表达式解析失败
        Assert.DoesNotContain("PrintLine(\"Test\")", result);
    }
}
