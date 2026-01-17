using Old8Lang.LangParser;
using Old8Lang.LanguageServer.Models;
using Xunit.Abstractions;

namespace Old8Lang.Tests.LanguageServer.Services;

public class DocumentParseResultTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void TestDocumentParseResult_Properties()
    {
        // Arrange & Act
        var result = new DocumentParseResult
        {
            Uri = "file:///test.old8",
            Text = "func test() -> int { return 42 }",
            Diagnostics = []
        };

        // Assert
        Assert.Equal("file:///test.old8", result.Uri);
        Assert.Equal("func test() -> int { return 42 }", result.Text);
        Assert.NotNull(result.Diagnostics);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void TestDiagnosticInfo_Properties()
    {
        // Arrange & Act
        var diagnostic = new DiagnosticInfo
        {
            Severity = DiagnosticSeverity.Error,
            Message = "Test error message",
            Line = 10,
            Column = 5,
            Source = "TestSource"
        };

        // Assert
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal("Test error message", diagnostic.Message);
        Assert.Equal(10, diagnostic.Line);
        Assert.Equal(5, diagnostic.Column);
        Assert.Equal("TestSource", diagnostic.Source);
    }

    [Fact]
    public void TestDiagnosticSeverity_AllValues()
    {
        // Test all enum values
        Assert.Equal(1, (int)DiagnosticSeverity.Error);
        Assert.Equal(2, (int)DiagnosticSeverity.Warning);
        Assert.Equal(3, (int)DiagnosticSeverity.Information);
        Assert.Equal(4, (int)DiagnosticSeverity.Hint);
    }

    [Fact]
    public void TestDocumentParseResult_WithRealData()
    {
        // Arrange
        var code = @"
func testFunction() -> int {
    return 42
}

class TestClass {
    public value <- """"
}

variable <- ""test""
";
        var tokens = LangTokenizer.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code, "test.old8");
        var ast = parser.ParseProgram();

        // Act
        var result = new DocumentParseResult
        {
            Uri = "test://test.old8",
            Text = code,
            Tokens = tokens,
            Ast = ast,
            SymbolTable = new Dictionary<string, SymbolInfo>(),
            Diagnostics = []
        };

        // Assert
        Assert.Equal("test://test.old8", result.Uri);
        Assert.Equal(code, result.Text);
        Assert.NotNull(result.Tokens);
        Assert.NotEmpty(result.Tokens);
        Assert.NotNull(result.Ast);
        Assert.NotNull(result.SymbolTable);
        Assert.NotNull(result.Diagnostics);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void TestDocumentParseResult_WithDiagnostics()
    {
        // Arrange
        var code = @"
func testFunction() -> int {
    return 42
}
// Missing closing brace
";

        var tokens = LangTokenizer.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code, "test.old8");
        var ast = parser.ParseProgram();

        // Act
        var result = new DocumentParseResult
        {
            Uri = "test://invalid.old8",
            Text = code,
            Tokens = tokens,
            Ast = ast,
            SymbolTable = new Dictionary<string, SymbolInfo>(),
            Diagnostics =
            [
                new DiagnosticInfo
                {
                    Severity = DiagnosticSeverity.Error,
                    Message = "Syntax error: missing closing brace",
                    Line = 5,
                    Column = 1,
                    Source = "Parser"
                },

                new DiagnosticInfo
                {
                    Severity = DiagnosticSeverity.Warning,
                    Message = "Unused variable",
                    Line = 7,
                    Column = 9,
                    Source = "Semantic"
                }
            ]
        };

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test://invalid.old8", result.Uri);
        Assert.Equal(code, result.Text);
        Assert.NotNull(result.Tokens);
        Assert.NotNull(result.Ast);
        Assert.NotNull(result.SymbolTable);
        Assert.NotEmpty(result.Diagnostics);
        Assert.Equal(2, result.Diagnostics.Count);

        // Check error diagnostics
        var errorDiag = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        var warningDiag = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Warning).ToList();

        Assert.Single(errorDiag);
        Assert.Contains("Syntax error: missing closing brace", errorDiag.First().Message);
        Assert.Equal(5, errorDiag.First().Line);
        Assert.Equal(1, errorDiag.First().Column);
        Assert.Equal("Parser", errorDiag.First().Source);

        Assert.Single(warningDiag);
        Assert.Equal("Unused variable", warningDiag.First().Message);
        Assert.Equal(7, warningDiag.First().Line);
        Assert.Equal(9, warningDiag.First().Column);
        Assert.Equal("Semantic", warningDiag.First().Source);
    }

    [Fact]
    public void TestDocumentParseResult_NullProperties()
    {
        // Arrange
        var result = new DocumentParseResult
        {
            Uri = "test://empty.old8",
            Text = "",
            Diagnostics = []
            // Tokens, Ast, SymbolTable can be null
        };

        // Assert
        Assert.Equal("test://empty.old8", result.Uri);
        Assert.Equal("", result.Text);
        Assert.Null(result.Tokens);
        Assert.Null(result.Ast);
        Assert.Null(result.SymbolTable);
        Assert.NotNull(result.Diagnostics);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void TestDocumentParseResult_CollectionOperations()
    {
        // Arrange
        var result = new DocumentParseResult
        {
            Uri = "test://test.old8",
            Text = "test code",
            Diagnostics =
            [
                new DiagnosticInfo
                {
                    Severity = DiagnosticSeverity.Information,
                    Message = "Info message",
                    Line = 1,
                    Column = 1,
                    Source = "Test"
                }
                // Assert
            ]
        };

        // Act

        // Assert
        Assert.Single(result.Diagnostics);
        var diagnostic = result.Diagnostics[0];
        Assert.Equal(DiagnosticSeverity.Information, diagnostic.Severity);
        Assert.Equal("Info message", diagnostic.Message);
        Assert.Equal(1, diagnostic.Line);
        Assert.Equal(1, diagnostic.Column);
        Assert.Equal("Test", diagnostic.Source);
    }

    [Fact]
    public void TestDocumentParseResult_Equality()
    {
        // Arrange
        var result1 = new DocumentParseResult
        {
            Uri = "test://test1.old8",
            Text = "same content",
            Diagnostics = []
        };

        var result2 = new DocumentParseResult
        {
            Uri = "test://test2.old8",
            Text = "same content",
            Diagnostics = []
        };

        var result3 = new DocumentParseResult
        {
            Uri = "test://test3.old8",
            Text = "different content",
            Diagnostics = []
        };

        // Act & Assert - Since DocumentParseResult doesn't override Equals
        Assert.NotSame(result1, result2);
        Assert.NotSame(result1, result3);
        Assert.NotSame(result2, result3);

        // Test property inequality (different URIs)
        Assert.NotEqual(result1.Uri, result2.Uri);
        Assert.Equal(result1.Text, result2.Text);

        // Test with different content
        Assert.Equal("test://test1.old8", result1.Uri);
        Assert.Equal("test://test2.old8", result2.Uri);
        Assert.NotEqual(result1.Text, result3.Text);
        Assert.Equal("different content", result3.Text);
    }
}