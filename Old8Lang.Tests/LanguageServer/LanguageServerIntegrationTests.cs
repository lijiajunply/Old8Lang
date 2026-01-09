using Old8Lang.LanguageServer.Services;
using Old8Lang.LanguageServer.Handlers;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit.Abstractions;

namespace Old8Lang.Tests.LanguageServer;

public class LanguageServerIntegrationTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task TestFullCompletionWorkflow()
    {
        // Arrange
        var documentManager = new DocumentManager();
        var code = @"
func add(a:int, b:int) -> int {
    return a + b
}

class Calculator {
    public static PI <- 3.14159
    
    static func multiply(x:int, y:int) -> int {
        return x * y
    }
}

result1 <- add(1, 2)
result2 <- Calculator.multiply(3, 4)
pi <- Calculator.PI
";

        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);
        var handler = new CompletionHandler(documentManager);

        // Act - Test completion at different positions
        var functionCompletion = await handler.Handle(new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(13, 0) // Line 14: result1 <- add(1, 2)
        }, CancellationToken.None);

        var classCompletion = await handler.Handle(new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(6, 0) // Line 7: inside Calculator class
        }, CancellationToken.None);

        var variableCompletion = await handler.Handle(new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(14, 0) // Line 15: result2 <- Calculator.multiply(3, 4)
        }, CancellationToken.None);

        // Assert
        Assert.NotNull(functionCompletion);
        Assert.NotNull(classCompletion);
        Assert.NotNull(variableCompletion);

        testOutputHelper.WriteLine($"Function completion items: {functionCompletion.Items.Count()}");
        testOutputHelper.WriteLine($"Class completion items: {classCompletion.Items.Count()}");
        testOutputHelper.WriteLine($"Variable completion items: {variableCompletion.Items.Count()}");

        // Should include function completions
        Assert.Contains(functionCompletion.Items, item => item.Label == "add");

        // Should include class completions  
        Assert.Contains(classCompletion.Items, item => item.Label == "Calculator");

        // Should include variable completions
        Assert.Contains(variableCompletion.Items, item => item.Label == "result1");

        // Should include static members when completing class name
        var staticMemberCompletion = await handler.Handle(new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(14, 22) // Line 15: result2 <- Calculator.|multiply(3, 4) - after the dot
        }, CancellationToken.None);

        Assert.Contains(staticMemberCompletion.Items, item => item.Label == "multiply");
        Assert.Contains(staticMemberCompletion.Items, item => item.Label == "PI");
    }

    [Fact]
    public async Task TestDefinitionWorkflow()
    {
        // Arrange
        var documentManager = new DocumentManager();
        var code = @"
func testFunction() -> int {
    return 42
}

class TestClass {
    public value <- ""
}

instance <- TestClass()
result <- instance.value
reference <- testFunction()
";

        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);
        var handler = new DefinitionHandler(documentManager);

        // Act - Test go to definition
        var functionDefinition = await handler.Handle(new DefinitionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(11, 14) // Line 12: reference <- |testFunction()
        }, CancellationToken.None);

        var classDefinition = await handler.Handle(new DefinitionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(9, 14) // Line 10: instance <- |TestClass()
        }, CancellationToken.None);

        var variableDefinition = await handler.Handle(new DefinitionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(10, 11) // Line 11: result <- |instance.value
        }, CancellationToken.None);

        // Assert
        Assert.NotNull(functionDefinition);
        Assert.NotNull(classDefinition);
        Assert.NotNull(variableDefinition);

        // Function definition should point to function declaration
        var funcLocation = functionDefinition.First().Location!;
        Assert.Equal(1, funcLocation.Range.Start.Line); // Line 2 (0-based is 1)

        // Class definition should point to class declaration
        var classLocation = classDefinition.First().Location!;
        Assert.Equal(5, classLocation.Range.Start.Line); // Line 6 (0-based is 5)

        // Variable definition should point to variable declaration
        var varLocation = variableDefinition.First().Location!;
        Assert.Equal(9, varLocation.Range.Start.Line); // Line 10 (0-based is 9)
    }

    [Fact]
    public async Task TestHoverWorkflow()
    {
        // Arrange
        var documentManager = new DocumentManager();
        var code = @"
/// Add two numbers together
/// First number to add
/// Second number to add  
/// Returns the sum of both numbers
func addNumbers(a:int, b:int) -> int {
    return a + b
}

/// Simple value holder
class ValueHolder {
    /// The stored value
    public value <- 0
}
";

        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, code);
        var handler = new HoverHandler(documentManager);

        // Act
        var functionHover = await handler.Handle(new HoverParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(5, 5) // Line 6: func |addNumbers(a:int, b:int) -> int
        }, CancellationToken.None);

        var classHover = await handler.Handle(new HoverParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(10, 6) // Line 11: class |ValueHolder {
        }, CancellationToken.None);

        // Assert
        Assert.NotNull(functionHover);
        Assert.NotNull(classHover);

        // Function hover should contain signature and documentation
        var funcContent = functionHover.Contents.MarkupContent!.Value;
        Assert.Contains("addNumbers", funcContent);
        Assert.Contains("Add two numbers together", funcContent);

        // Class hover should contain class information
        var classContent = classHover.Contents.MarkupContent!.Value;
        Assert.Contains("ValueHolder", classContent);
        Assert.Contains("Simple value holder", classContent);
    }

    [Fact]
    public async Task TestRenameWorkflow()
    {
        // Arrange
        var documentManager = new DocumentManager();
        var originalCode = @"
func originalFunction() -> int {
    return 42
}

originalVar <- 100
result1 <- originalFunction()
result2 <- originalVar + 1
";

        var uri = "file:///test.old8";
        documentManager.UpdateDocument(uri, originalCode);
        var handler = new RenameHandler(documentManager);

        // Act - Rename function
        var functionRename = await handler.Handle(new RenameParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(1, 5), // 'originalFunction' definition
            NewName = "newFunctionName"
        }, CancellationToken.None);

        // Act - Rename variable
        var variableRename = await handler.Handle(new RenameParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(5, 0), // 'originalVar' definition  
            NewName = "newVarName"
        }, CancellationToken.None);

        // Assert
        Assert.NotNull(functionRename);
        Assert.NotNull(variableRename);

        // Function rename should have 2 edits (definition + usage)
        var funcChanges = functionRename.Changes[new Uri(uri)];
        Assert.Equal(2, funcChanges.Count());

        // Variable rename should have 2 edits (definition + usage)
        var varChanges = variableRename.Changes[new Uri(uri)];
        Assert.Equal(2, varChanges.Count());

        // Check that all edits use the new name
        foreach (var edit in funcChanges.Concat(varChanges))
        {
            Assert.True(edit.NewText == "newFunctionName" || edit.NewText == "newVarName");
        }

        testOutputHelper.WriteLine($"Function rename edits: {funcChanges.Count()}");
        testOutputHelper.WriteLine($"Variable rename edits: {varChanges.Count()}");
    }

    [Fact]
    public void TestDocumentManagerIntegration()
    {
        // Arrange
        var documentManager = new DocumentManager();
        var uri1 = "file:///test1.old8";
        var uri2 = "file:///test2.old8";
        var code1 = "func test1() -> int { return 1 }";
        var code2 = "func test2() -> int { return 2 }";

        // Act - Update multiple documents
        var result1 = documentManager.UpdateDocument(uri1, code1);
        var result2 = documentManager.UpdateDocument(uri2, code2);

        // Update first document again
        var result1Updated = documentManager.UpdateDocument(uri1, "func updated1() -> int { return 10 }");

        // Close second document
        documentManager.CloseDocument(uri2);

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotNull(result1Updated);

        // Check document contents
        var stored1 = documentManager.GetDocument(uri1);
        var stored2 = documentManager.GetDocument(uri2);

        Assert.NotNull(stored1);
        Assert.Equal("func updated1() -> int { return 10 }", stored1.Text);
        Assert.Null(stored2); // Should be closed

        testOutputHelper.WriteLine($"Document 1: {stored1.Text}");
        testOutputHelper.WriteLine($"Document 2: {(stored2?.Text ?? "null")}");
    }

    [Fact]
    public void TestSymbolTableBuilding()
    {
        // Arrange
        var documentManager = new DocumentManager();
var complexCode = @"
func globalFunction(param:string) -> void {
    PrintLine(param)
}

class GlobalClass {
    public static CONST <- 42
    
    public func instanceMethod() -> string {
        return ""instance""
    }
    
    static func staticMethod() -> string {
        return ""static""
    }
}

globalVar <- ""global""
localVar <- ""local""
";

        // Act
        var result = documentManager.UpdateDocument("file:///complex.old8", complexCode);

        // Assert
        Assert.NotNull(result.SymbolTable);
        Assert.True(result.SymbolTable.Count >= 4); // globalFunction, GlobalClass, globalVar, localVar

        // Check function symbol
        Assert.True(result.SymbolTable.ContainsKey("globalFunction"));
        var funcSymbol = result.SymbolTable["globalFunction"];
        Assert.Equal(Old8Lang.LanguageServer.Models.SymbolKind.Function, funcSymbol.Kind);

        // Check class symbol
        Assert.True(result.SymbolTable.ContainsKey("GlobalClass"));
        var classSymbol = result.SymbolTable["GlobalClass"];
        Assert.Equal(Old8Lang.LanguageServer.Models.SymbolKind.Class, classSymbol.Kind);
        Assert.Equal(3, classSymbol.Members.Count); // CONST, instanceMethod, staticMethod

        // Check class members
        Assert.True(classSymbol.Members.ContainsKey("CONST"));
        Assert.True(classSymbol.Members.ContainsKey("instanceMethod"));
        Assert.True(classSymbol.Members.ContainsKey("staticMethod"));

        var constMember = classSymbol.Members["CONST"];
        Assert.True(constMember.IsStatic);

        var instanceMember = classSymbol.Members["instanceMethod"];
        Assert.False(instanceMember.IsStatic);

        var staticMember = classSymbol.Members["staticMethod"];
        Assert.True(staticMember.IsStatic);

        // Check variables
        Assert.True(result.SymbolTable.ContainsKey("globalVar"));
        Assert.True(result.SymbolTable.ContainsKey("localVar"));

        testOutputHelper.WriteLine($"Symbol table entries: {result.SymbolTable.Count}");
        foreach (var (name, symbol) in result.SymbolTable)
        {
            testOutputHelper.WriteLine($"  {name}: {symbol.Kind}");
        }
    }

    [Fact]
    public void TestSemanticAnalysis()
    {
        // Arrange
        var documentManager = new DocumentManager();
        var codeWithErrors = @"
func definedFunction() -> int {
    return 42
}

result1 <- definedFunction()
result2 <- undefinedFunction()
result3 <- undefinedVar + 1
";

        // Act
        var result = documentManager.UpdateDocument("file:///errors.old8", codeWithErrors);

        // Assert
        Assert.NotNull(result.Diagnostics);

        // Should have semantic errors for undefined symbols
        var semanticErrors = result.Diagnostics.Where(d =>
            d.Severity == Old8Lang.LanguageServer.Models.DiagnosticSeverity.Error &&
            d.Source == "Old8Lang Semantic").ToList();

        Assert.True(semanticErrors.Count >= 2); // undefinedFunction and undefinedVar

        var undefinedFunctionError = semanticErrors.FirstOrDefault(d => d.Message.Contains("undefinedFunction"));
        var undefinedVarError = semanticErrors.FirstOrDefault(d => d.Message.Contains("undefinedVar"));

        Assert.NotNull(undefinedFunctionError);
        Assert.NotNull(undefinedVarError);

        testOutputHelper.WriteLine($"Semantic errors found: {semanticErrors.Count}");
        foreach (var error in semanticErrors)
        {
            testOutputHelper.WriteLine($"  {error.Message} at line {error.Line}");
        }
    }

    [Fact]
    public void TestErrorHandling()
    {
        // Arrange
        var documentManager = new DocumentManager();
        var invalidCode = @"
func testFunction() -> int {
    return 42
// Missing closing brace and syntax errors
";

        // Act
        var result = documentManager.UpdateDocument("file:///invalid.old8", invalidCode);

        // Assert
        Assert.NotNull(result.Diagnostics);
        Assert.NotEmpty(result.Diagnostics);

        // Should have syntax errors from parser
        var syntaxErrors = result.Diagnostics.Where(d =>
            d.Severity == Old8Lang.LanguageServer.Models.DiagnosticSeverity.Error).ToList();

        Assert.NotEmpty(syntaxErrors);

        testOutputHelper.WriteLine($"Total errors: {result.Diagnostics.Count}");
        foreach (var error in result.Diagnostics)
        {
            testOutputHelper.WriteLine($"  [{error.Severity}] {error.Message}");
        }
    }

    [Fact]
    public async Task TestMemberAccessChain()
    {
        // Arrange
        var documentManager = new DocumentManager();
var code = @"
class Outer {
    public inner <- InnerClass()
    
    public func createInner() -> InnerClass {
        return InnerClass()
    }
}

class InnerClass {
    public value <- """"
    
    public func getValue() -> string {
        return this.value
    }
    
    public static func getStaticValue() -> string {
        return ""static""
    }
}

outer <- Outer()
inner <- outer.createInner()
result1 <- inner.getValue()
result2 <- inner.getStaticValue()
result3 <- InnerClass.getStaticValue()
";

        var uri = "file:///chained.old8";
        documentManager.UpdateDocument(uri, code);
        var completionHandler = new CompletionHandler(documentManager);

        // Act - Test member access completion
        var instanceMethodCompletion = await completionHandler.Handle(new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(23, 18) // Line 24: result1 <- inner.|getValue() - after the dot
        }, CancellationToken.None);

        var staticMethodCompletion = await completionHandler.Handle(new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier { Uri = new Uri(uri) },
            Position = new Position(24, 18) // Line 25: result2 <- inner.|getStaticValue() - after the dot
        }, CancellationToken.None);

        // Assert
        Assert.NotNull(instanceMethodCompletion);
        Assert.NotNull(staticMethodCompletion);

        // Should include instance method
        Assert.Contains(instanceMethodCompletion.Items, item => item.Label == "getValue");

        // Should include static method
        Assert.Contains(staticMethodCompletion.Items, item => item.Label == "getStaticValue");

        testOutputHelper.WriteLine($"Instance method completions: {instanceMethodCompletion.Items.Count()}");
        testOutputHelper.WriteLine($"Static method completions: {staticMethodCompletion.Items.Count()}");
    }
}