using Old8Lang.LanguageServer.Handlers;
using Old8Lang.LanguageServer.Models;
using Old8Lang.LanguageServer.Services;
using Xunit.Abstractions;

namespace Old8Lang.Tests.LanguageServer.Handlers.Sync;

public class TextDocumentSyncHandlerTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void TestHandlerCreation()
    {
        // Arrange & Act
        var documentManager = new DocumentManager();
        var handler = new TextDocumentSyncHandler(documentManager, null);

        // Assert
        Assert.NotNull(handler);
    }

    [Fact]
    public void TestDocumentManagerIntegration()
    {
        // Arrange
        var documentManager = new DocumentManager();

        // Act - Test document creation
        var result = documentManager.UpdateDocument("file:///test.old8", "func test() -> int { return 42 }");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("file:///test.old8", result.Uri);
        Assert.Equal("func test() -> int { return 42 }", result.Text);
        Assert.NotNull(result.Tokens);
        Assert.NotNull(result.Ast);
        Assert.NotNull(result.SymbolTable);

        // Test document retrieval
        var retrieved = documentManager.GetDocument("file:///test.old8");
        Assert.NotNull(retrieved);
        Assert.Same(result, retrieved);

        // Test document closing
        documentManager.CloseDocument("file:///test.old8");
        var closed = documentManager.GetDocument("file:///test.old8");
        Assert.Null(closed);
    }

    [Fact]
    public void TestDocumentManagerWithInvalidCode()
    {
        // Arrange
        var documentManager = new DocumentManager();
        var invalidCode = @"func testFunction() -> int {
    return 42
    // Missing closing brace";

        // Act
        var result = documentManager.UpdateDocument("file:///invalid.old8", invalidCode);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(invalidCode, result.Text);
        Assert.NotEmpty(result.Diagnostics);

        // Should have error diagnostics
        var errors = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void TestDocumentManagerMultipleDocuments()
    {
        // Arrange
        var documentManager = new DocumentManager();

        // Act
        var result1 = documentManager.UpdateDocument("file:///test1.old8", "func test1() -> int { return 1 }");
        var result2 = documentManager.UpdateDocument("file:///test2.old8", "func test2() -> int { return 2 }");

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotSame(result1, result2);

        // Verify both documents exist
        var doc1 = documentManager.GetDocument("file:///test1.old8");
        var doc2 = documentManager.GetDocument("file:///test2.old8");
        Assert.NotNull(doc1);
        Assert.NotNull(doc2);
        Assert.Same(result1, doc1);
        Assert.Same(result2, doc2);

        // Close one document
        documentManager.CloseDocument("file:///test1.old8");
        Assert.Null(documentManager.GetDocument("file:///test1.old8"));
        Assert.NotNull(documentManager.GetDocument("file:///test2.old8"));
    }

    [Fact]
    public void TestDocumentManagerProperties()
    {
        // Arrange & Act
        var documentManager = new DocumentManager();

        // Assert
        Assert.False(documentManager.DebugModeEnabled);
        Assert.False(documentManager.ProfilingEnabled);

        // Act
        documentManager.DebugModeEnabled = true;
        documentManager.ProfilingEnabled = true;

        // Assert
        Assert.True(documentManager.DebugModeEnabled);
        Assert.True(documentManager.ProfilingEnabled);
    }

    [Fact]
    public void TestDocumentUpdateOverwritesExisting()
    {
        // Arrange
        var documentManager = new DocumentManager();

        // Add initial document
        var result1 = documentManager.UpdateDocument("file:///test.old8", "func original() -> int { return 1 }");
        Assert.NotNull(result1);
        Assert.Equal("func original() -> int { return 1 }", result1.Text);

        // Act - Update with new content
        var result2 = documentManager.UpdateDocument("file:///test.old8", "func updated() -> int { return 2 }");

        // Assert
        Assert.NotNull(result2);
        Assert.Equal("func updated() -> int { return 2 }", result2.Text);

        // Should be same document instance (updated in place)
        var retrieved = documentManager.GetDocument("file:///test.old8");
        Assert.NotNull(retrieved);
        Assert.Same(result2, retrieved);
        Assert.Equal("func updated() -> int { return 2 }", retrieved.Text);
    }

    [Fact]
    public void TestDocumentParseResultProperties()
    {
        // Arrange & Act
        var documentManager = new DocumentManager();
        var code = @"func test() -> int {
    return 42
}

class TestClass {
    public value <- ""test""
}";

        var result = documentManager.UpdateDocument("file:///test.old8", code);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("file:///test.old8", result.Uri);
        Assert.Equal(code, result.Text);
        Assert.NotNull(result.Tokens);
        Assert.NotEmpty(result.Tokens);
        Assert.NotNull(result.Ast);
        Assert.NotNull(result.SymbolTable);
        Assert.NotNull(result.Diagnostics);

        // Should contain both function and class symbols
        Assert.True(result.SymbolTable.Count >= 2);
        Assert.True(result.SymbolTable.ContainsKey("test"));
        Assert.True(result.SymbolTable.ContainsKey("TestClass"));
    }

    [Fact]
    public void TestHandlerProperties()
    {
        // Arrange
        var documentManager = new DocumentManager();
        var handler = new TextDocumentSyncHandler(documentManager, null);

        // Act & Assert - Verify handler has expected properties
        // Note: The actual handler properties would depend on the LSP implementation
        Assert.NotNull(handler);
    }
}