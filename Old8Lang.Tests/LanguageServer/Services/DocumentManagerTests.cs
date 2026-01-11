using Old8Lang.LanguageServer.Models;
using Old8Lang.LanguageServer.Services;
using Xunit.Abstractions;

namespace Old8Lang.Tests.LanguageServer.Services;

public class DocumentManagerTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void TestUpdateDocument_NewDocument()
    {
        // Arrange
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        var code = @"
func testFunction() -> int {
    return 42
}
";

        // Act
        var result = documentManager.UpdateDocument(uri, code);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(uri, result.Uri);
        Assert.Equal(code, result.Text);
        Assert.NotNull(result.Tokens);
        Assert.NotNull(result.Ast);
        Assert.NotNull(result.SymbolTable);
        Assert.NotNull(result.Diagnostics);

        // Verify document is stored
        var storedDocument = documentManager.GetDocument(uri);
        Assert.NotNull(storedDocument);
        Assert.Equal(code, storedDocument.Text);
    }

    [Fact]
    public void TestUpdateDocument_ExistingDocument()
    {
        // Arrange
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        var initialCode = "func initial() -> int { return 1 }";
        
        // First update
        var result1 = documentManager.UpdateDocument(uri, initialCode);
        Assert.NotNull(result1);

        var newCode = "func updated() -> int { return 2 }";

        // Act - Update existing document
        var result2 = documentManager.UpdateDocument(uri, newCode);

        // Assert
        Assert.NotNull(result2);
        Assert.Equal(uri, result2.Uri);
        Assert.Equal(newCode, result2.Text);

        // Verify document is updated
        var storedDocument = documentManager.GetDocument(uri);
        Assert.NotNull(storedDocument);
        Assert.Equal(newCode, storedDocument.Text);
        Assert.NotEqual(initialCode, storedDocument.Text);
    }

    [Fact]
    public void TestUpdateDocument_WithSyntaxError()
    {
        // Arrange
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        var invalidCode = @"
func testFunction() -> int {
    return 42
    // Missing closing brace
";

        // Act
        var result = documentManager.UpdateDocument(uri, invalidCode);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(invalidCode, result.Text);
        Assert.NotNull(result.Tokens);
        // When syntax error occurs, AST and SymbolTable may be null
        // This is expected behavior as parsing failed

        // Should have error diagnostics
        Assert.NotEmpty(result.Diagnostics);
        Assert.Contains(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void TestUpdateDocument_EmptyDocument()
    {
        // Arrange
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        var emptyCode = "";

        // Act
        var result = documentManager.UpdateDocument(uri, emptyCode);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(uri, result.Uri);
        Assert.Equal(emptyCode, result.Text);
        Assert.NotNull(result.Tokens);
        Assert.NotNull(result.Ast);
        Assert.NotNull(result.SymbolTable);
        Assert.NotNull(result.Diagnostics);
    }

    [Fact]
    public void TestGetDocument_Existing()
    {
        // Arrange
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        var code = "func test() -> int { return 42 }";
        
        documentManager.UpdateDocument(uri, code);

        // Act
        var result = documentManager.GetDocument(uri);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(uri, result.Uri);
        Assert.Equal(code, result.Text);
    }

    [Fact]
    public void TestGetDocument_NotExisting()
    {
        // Arrange
        var documentManager = new DocumentManager();
        var uri = "file:///nonexistent.old8";

        // Act
        var result = documentManager.GetDocument(uri);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void TestCloseDocument_Existing()
    {
        // Arrange
        var documentManager = new DocumentManager();
        var uri = "file:///test.old8";
        var code = "func test() -> int { return 42 }";
        
        documentManager.UpdateDocument(uri, code);
        Assert.NotNull(documentManager.GetDocument(uri)); // Verify it exists

        // Act
        documentManager.CloseDocument(uri);

        // Assert
        var result = documentManager.GetDocument(uri);
        Assert.Null(result); // Should be removed
    }

    [Fact]
    public void TestCloseDocument_NotExisting()
    {
        // Arrange
        var documentManager = new DocumentManager();
        var uri = "file:///nonexistent.old8";

        // Act - Should not throw
        documentManager.CloseDocument(uri);

        // Assert
        var result = documentManager.GetDocument(uri);
        Assert.Null(result);
    }

    [Fact]
    public void TestMultipleDocuments()
    {
        // Arrange
        var documentManager = new DocumentManager();
        var uri1 = "file:///test1.old8";
        var uri2 = "file:///test2.old8";
        var code1 = "func test1() -> int { return 1 }";
        var code2 = "func test2() -> int { return 2 }";

        // Act
        var result1 = documentManager.UpdateDocument(uri1, code1);
        var result2 = documentManager.UpdateDocument(uri2, code2);

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);

        var stored1 = documentManager.GetDocument(uri1);
        var stored2 = documentManager.GetDocument(uri2);

        Assert.NotNull(stored1);
        Assert.NotNull(stored2);

        Assert.Equal(code1, stored1.Text);
        Assert.Equal(code2, stored2.Text);
    }

    [Fact]
    public void TestDebugModeEnabled()
    {
        // Arrange
        var documentManager = new DocumentManager();

        // Act & Assert
        Assert.False(documentManager.DebugModeEnabled);

        documentManager.DebugModeEnabled = true;
        Assert.True(documentManager.DebugModeEnabled);

        documentManager.DebugModeEnabled = false;
        Assert.False(documentManager.DebugModeEnabled);
    }

    [Fact]
    public void TestProfilingEnabled()
    {
        // Arrange
        var documentManager = new DocumentManager();

        // Act & Assert
        Assert.False(documentManager.ProfilingEnabled);

        documentManager.ProfilingEnabled = true;
        Assert.True(documentManager.ProfilingEnabled);

        documentManager.ProfilingEnabled = false;
        Assert.False(documentManager.ProfilingEnabled);
    }

    [Fact]
    public void TestUpdateDocument_WithDebugMode()
    {
        // Arrange
        var documentManager = new DocumentManager();
        documentManager.DebugModeEnabled = true;
        
        var uri = "file:///test.old8";
        var code = "func test() -> int { return 42 }";

        // Act
        var result = documentManager.UpdateDocument(uri, code);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Diagnostics);
        
        // Should have debug mode information
        Assert.Contains(result.Diagnostics, d => 
            d.Severity == DiagnosticSeverity.Information && 
            d.Message.Contains("调试模式"));
    }

    [Fact]
    public void TestUpdateDocument_WithProfilingEnabled()
    {
        // Arrange
        var documentManager = new DocumentManager();
        documentManager.ProfilingEnabled = true;
        
        var uri = "file:///test.old8";
        var code = "func test() -> int { return 42 }";

        // Act
        var result = documentManager.UpdateDocument(uri, code);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Diagnostics);
        
        // Should have profiling information
        Assert.Contains(result.Diagnostics, d => 
            d.Severity == DiagnosticSeverity.Information && 
            d.Message.Contains("性能分析"));
    }

    [Fact]
    public void TestUpdateDocument_WithBothDebugAndProfiling()
    {
        // Arrange
        var documentManager = new DocumentManager();
        documentManager.DebugModeEnabled = true;
        documentManager.ProfilingEnabled = true;
        
        var uri = "file:///test.old8";
        var code = "func test() -> int { return 42 }";

        // Act
        var result = documentManager.UpdateDocument(uri, code);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Diagnostics);
        
        // Should have both debug and profiling information
        var infoDiagnostics = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Information).ToList();
        Assert.Contains(infoDiagnostics, d => d.Message.Contains("调试模式"));
        Assert.Contains(infoDiagnostics, d => d.Message.Contains("性能分析"));
    }

    [Fact]
    public void TestComplexDocumentParsing()
    {
        // Arrange
        var documentManager = new DocumentManager();
        var uri = "file:///complex.old8";
        var complexCode = @"
class MathUtil {
    static PI <- 3.14159
    
    static func circleArea(radius:double) -> double {
        return MathUtil.PI * radius * radius
    }
}

func main() -> void {
    area <- MathUtil.circleArea(5.0)
}

counter <- 0
name <- TestName
scores <- {90, 85, 95}
";

        // Act
        var result = documentManager.UpdateDocument(uri, complexCode);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(complexCode, result.Text);
        Assert.NotNull(result.Tokens);
        Assert.NotNull(result.Ast);
        Assert.NotNull(result.SymbolTable);
        
        // Should have symbols for class, functions, and variables
        Assert.True(result.SymbolTable.Count >= 4); // MathUtil, main, counter, name, scores
        
        // Check class symbol
        Assert.True(result.SymbolTable.ContainsKey("MathUtil"));
        var mathUtilSymbol = result.SymbolTable["MathUtil"];
        Assert.Equal(SymbolKind.Class, mathUtilSymbol.Kind);
        
        // Check class members
        Assert.True(mathUtilSymbol.Members.ContainsKey("PI"));
        Assert.True(mathUtilSymbol.Members.ContainsKey("circleArea"));
        
        var piSymbol = mathUtilSymbol.Members["PI"];
        Assert.True(piSymbol.IsStatic);
        
        var circleAreaSymbol = mathUtilSymbol.Members["circleArea"];
        Assert.True(circleAreaSymbol.IsStatic);
    }
}