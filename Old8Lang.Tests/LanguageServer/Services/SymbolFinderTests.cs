using Old8Lang.LangParser;
using Old8Lang.LanguageServer.Services;
using Old8Lang.LanguageServer.Models;
using Xunit.Abstractions;

namespace Old8Lang.Tests.LanguageServer;

public class SymbolFinderTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void TestFindSymbolAtPosition_Function()
    {
        // Arrange
        var code = @"
func testFunction(a:int, b:int) -> int {
    return a + b
}

result <- testFunction(1, 2)
";
        var tokens = LangTokenizer.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code, "test.old8");
        var ast = parser.ParseProgram();
        
        var symbolTableBuilder = new SymbolTableBuilder("test.old8", tokens);
        var symbolTable = symbolTableBuilder.Build(ast);

        var document = new DocumentParseResult
        {
            Uri = "test.old8",
            Text = code,
            Tokens = tokens,
            Ast = ast,
            SymbolTable = symbolTable,
            Diagnostics = new List<DiagnosticInfo>()
        };

        // Act - Find symbol at function definition
        var symbol = SymbolFinder.FindSymbolAtPosition(document, 1, 5); // Line 2, column 6 (testFunction)

        // Assert
        Assert.NotNull(symbol);
        Assert.Equal("testFunction", symbol.Name);
        Assert.Equal(SymbolKind.Function, symbol.Kind);
    }

    [Fact]
    public void TestFindSymbolAtPosition_FunctionCall()
    {
        // Arrange
        var code = @"
func testFunction(a:int, b:int) -> int {
    return a + b
}

result <- testFunction(1, 2)
";
        var tokens = LangTokenizer.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code, "test.old8");
        var ast = parser.ParseProgram();

        var symbolTableBuilder = new SymbolTableBuilder("test.old8", tokens);
        var symbolTable = symbolTableBuilder.Build(ast);

        var document = new DocumentParseResult
        {
            Uri = "test.old8",
            Text = code,
            Tokens = tokens,
            Ast = ast,
            SymbolTable = symbolTable,
            Diagnostics = new List<DiagnosticInfo>()
        };

        // Act - Find symbol at function call
        var symbol = SymbolFinder.FindSymbolAtPosition(document, 5, 10); // Line 6, column 11 (testFunction call)

        // Assert
        Assert.NotNull(symbol);
        Assert.Equal("testFunction", symbol.Name);
        Assert.Equal(SymbolKind.Function, symbol.Kind);
    }

    [Fact]
    public void TestFindSymbolAtPosition_Variable()
    {
        // Arrange
        var code = @"
myVariable <- 42
result <- myVariable + 10
";
        var tokens = LangTokenizer.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code, "test.old8");
        var ast = parser.ParseProgram();

        var symbolTableBuilder = new SymbolTableBuilder("test.old8", tokens);
        var symbolTable = symbolTableBuilder.Build(ast);

        var document = new DocumentParseResult
        {
            Uri = "test.old8",
            Text = code,
            Tokens = tokens,
            Ast = ast,
            SymbolTable = symbolTable,
            Diagnostics = new List<DiagnosticInfo>()
        };

        // Act - Find symbol at variable definition
        var symbol = SymbolFinder.FindSymbolAtPosition(document, 1, 0); // Line 2, column 1 (myVariable)

        // Assert
        Assert.NotNull(symbol);
        Assert.Equal("myVariable", symbol.Name);
        Assert.Equal(SymbolKind.Variable, symbol.Kind);
    }

    [Fact]
    public void TestFindSymbolAtPosition_VariableUsage()
    {
        // Arrange
        var code = @"
myVariable <- 42
result <- myVariable + 10
";
        var tokens = LangTokenizer.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code, "test.old8");
        var ast = parser.ParseProgram();

        var symbolTableBuilder = new SymbolTableBuilder("test.old8", tokens);
        var symbolTable = symbolTableBuilder.Build(ast);

        var document = new DocumentParseResult
        {
            Uri = "test.old8",
            Text = code,
            Tokens = tokens,
            Ast = ast,
            SymbolTable = symbolTable,
            Diagnostics = new List<DiagnosticInfo>()
        };

        // Act - Find symbol at variable usage
        var symbol = SymbolFinder.FindSymbolAtPosition(document, 2, 10); // Line 3, column 11 (myVariable usage)

        // Assert
        Assert.NotNull(symbol);
        Assert.Equal("myVariable", symbol.Name);
        Assert.Equal(SymbolKind.Variable, symbol.Kind);
    }

    [Fact]
    public void TestFindSymbolAtPosition_Class()
    {
        // Arrange
        var code = @"
class TestClass {
    public name:string
}

instance <- TestClass()
";
        var tokens = LangTokenizer.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code, "test.old8");
        var ast = parser.ParseProgram();

        var symbolTableBuilder = new SymbolTableBuilder("test.old8", tokens);
        var symbolTable = symbolTableBuilder.Build(ast);

        var document = new DocumentParseResult
        {
            Uri = "test.old8",
            Text = code,
            Tokens = tokens,
            Ast = ast,
            SymbolTable = symbolTable,
            Diagnostics = new List<DiagnosticInfo>()
        };

        // Debug: Print tokens on line 2
        testOutputHelper.WriteLine("Tokens on line 2:");
        foreach (var token in tokens.Where(t => t.Line == 2))
        {
            testOutputHelper.WriteLine($"  Token: '{token.Value}', Type: {token.Type}, Line: {token.Line}, Column: {token.Column}");
        }

        // Act - Find symbol at class definition (TestClass is at column 7 in 1-based, column 6 in 0-based)
        var symbol = SymbolFinder.FindSymbolAtPosition(document, 1, 7); // LSP position (1, 7) for 'TestClass'

        // Assert
        Assert.NotNull(symbol);
        Assert.Equal("TestClass", symbol.Name);
        Assert.Equal(SymbolKind.Class, symbol.Kind);
    }

    [Fact]
    public void TestFindSymbolAtPosition_ClassMember()
    {
        // Arrange
        var code = @"
class User {
    public name:string
    
    func getName() -> string {
        return this.name
    }
}

user <- User()
result <- user.getName()
";
        var tokens = LangTokenizer.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code, "test.old8");
        var ast = parser.ParseProgram();
        
        var symbolTableBuilder = new SymbolTableBuilder("test.old8", tokens);
        var symbolTable = symbolTableBuilder.Build(ast);

        var document = new DocumentParseResult
        {
            Uri = "test.old8",
            Text = code,
            Tokens = tokens,
            Ast = ast,
            SymbolTable = symbolTable,
            Diagnostics = new List<DiagnosticInfo>()
        };

        // Debug info
        testOutputHelper.WriteLine($"SymbolTable count: {symbolTable.Count}");
        foreach (var (name, symbol) in symbolTable)
        {
            testOutputHelper.WriteLine($"  {name}: {symbol.Kind}");
            if (symbol.Members.Count > 0)
            {
                foreach (var (memberName, member) in symbol.Members)
                {
                    testOutputHelper.WriteLine($"    - {memberName}: {member.Kind}");
                }
            }
        }

        // Debug: Print code lines
        var lines = code.Split('\n');
        testOutputHelper.WriteLine("\nCode lines:");
        for (int i = 0; i < lines.Length; i++)
        {
            testOutputHelper.WriteLine($"Line {i}: '{lines[i]}'");
        }

        // Debug: Print tokens containing "getName"
        testOutputHelper.WriteLine("\nTokens containing 'getName':");
        foreach (var token in tokens.Where(t => t.Value.Contains("getName")))
        {
            testOutputHelper.WriteLine($"  Token: '{token.Value}', Line: {token.Line}, Column: {token.Column}");
        }

        // Debug: Print tokens on line where we expect to find result <- user.getName()
        var targetLine = tokens.Where(t => t.Value == "result").FirstOrDefault().Line;
        testOutputHelper.WriteLine($"\nTokens on line {targetLine}:");
        foreach (var token in tokens.Where(t => t.Line == targetLine))
        {
            testOutputHelper.WriteLine($"  Token: '{token.Value}', Line: {token.Line}, Column: {token.Column}");
        }

        // Act - Find symbol at member access
        var memberSymbol = SymbolFinder.FindSymbolAtPosition(document, 10, 15); // Line 11 (0-based: 10), column 15 (getName in user.getName())

        // Assert
        Assert.NotNull(memberSymbol);
        Assert.Equal("getName", memberSymbol.Name);
        Assert.Equal(SymbolKind.Method, memberSymbol.Kind);

        // Should have parent information
        Assert.NotNull(memberSymbol.Parent);
        Assert.Equal("User", memberSymbol.Parent.Name);
    }

    [Fact]
    public void TestFindSymbolAtPosition_MemberAccessProperty()
    {
        // Arrange
        var code = @"
class User {
    public name:string
}

user <- User()
result <- user.name
";
        var tokens = LangTokenizer.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code, "test.old8");
        var ast = parser.ParseProgram();
        
        var symbolTableBuilder = new SymbolTableBuilder("test.old8", tokens);
        var symbolTable = symbolTableBuilder.Build(ast);

        var document = new DocumentParseResult
        {
            Uri = "test.old8",
            Text = code,
            Tokens = tokens,
            Ast = ast,
            SymbolTable = symbolTable,
            Diagnostics = new List<DiagnosticInfo>()
        };

        // Act - Find symbol at property access
        var symbol = SymbolFinder.FindSymbolAtPosition(document, 6, 15); // Line 7 (0-based: 6), column 15 (name in user.name)

        // Assert
        Assert.NotNull(symbol);
        Assert.Equal("name", symbol.Name);
        Assert.Equal(SymbolKind.Property, symbol.Kind);

        // Should have parent information
        Assert.NotNull(symbol.Parent);
        Assert.Equal("User", symbol.Parent.Name);
    }

    [Fact]
    public void TestFindSymbolAtPosition_NotFound()
    {
        // Arrange
        var code = @"
result <- undefinedVar + 10
";
        var tokens = LangTokenizer.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code, "test.old8");
        var ast = parser.ParseProgram();
        
        var symbolTableBuilder = new SymbolTableBuilder("test.old8", tokens);
        var symbolTable = symbolTableBuilder.Build(ast);

        var document = new DocumentParseResult
        {
            Uri = "test.old8",
            Text = code,
            Tokens = tokens,
            Ast = ast,
            SymbolTable = symbolTable,
            Diagnostics = new List<DiagnosticInfo>()
        };

        // Act - Try to find undefined symbol
        var symbol = SymbolFinder.FindSymbolAtPosition(document, 1, 17); // Line 2, column 18 (undefinedVar)

        // Assert
        Assert.Null(symbol);
    }

    [Fact]
    public void TestFindSymbolAtPosition_NullDocument()
    {
        // Arrange
        var document = new DocumentParseResult
        {
            Uri = "test.old8",
            Text = "",
            Tokens = null,
            Ast = null,
            SymbolTable = null,
            Diagnostics = new List<DiagnosticInfo>()
        };

        // Act
        var symbol = SymbolFinder.FindSymbolAtPosition(document, 0, 0);

        // Assert
        Assert.Null(symbol);
    }

    [Fact]
    public void TestFindSymbolAtPosition_Whitespace()
    {
        // Arrange
        var code = @"
func test() -> int {
    return 42
}
";
        var tokens = LangTokenizer.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code, "test.old8");
        var ast = parser.ParseProgram();
        
        var symbolTableBuilder = new SymbolTableBuilder("test.old8", tokens);
        var symbolTable = symbolTableBuilder.Build(ast);

        var document = new DocumentParseResult
        {
            Uri = "test.old8",
            Text = code,
            Tokens = tokens,
            Ast = ast,
            SymbolTable = symbolTable,
            Diagnostics = new List<DiagnosticInfo>()
        };

        // Act - Try to find symbol at whitespace position
        var symbol = SymbolFinder.FindSymbolAtPosition(document, 1, 0); // Line 2, column 1 (empty line)

        // Assert
        Assert.Null(symbol);
    }

    [Fact]
    public void TestFindReferences_Function()
    {
        // Arrange
        var code = @"
func testFunction() -> int {
    return 42
}

result1 <- testFunction()
result2 <- testFunction()
result3 <- testFunction()
";
        var tokens = LangTokenizer.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code, "test.old8");
        var ast = parser.ParseProgram();
        
        var symbolTableBuilder = new SymbolTableBuilder("test.old8", tokens);
        var symbolTable = symbolTableBuilder.Build(ast);

        var document = new DocumentParseResult
        {
            Uri = "test.old8",
            Text = code,
            Tokens = tokens,
            Ast = ast,
            SymbolTable = symbolTable,
            Diagnostics = new List<DiagnosticInfo>()
        };

        // Act
        var references = SymbolFinder.FindReferences(document, "testFunction");

        // Assert
        Assert.Equal(4, references.Count); // 1 definition + 3 usages

        // Check that all references have correct positions
        var lines = references.Select(r => r.Line).ToList();
        Assert.Contains(1, lines); // Definition line (0-based)
        Assert.Contains(5, lines); // First usage
        Assert.Contains(6, lines); // Second usage
        Assert.Contains(7, lines); // Third usage
    }

    [Fact]
    public void TestFindReferences_Variable()
    {
        // Arrange
        var code = @"
myVar <- 42
result1 <- myVar + 1
result2 <- myVar * 2
";
        var tokens = LangTokenizer.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code, "test.old8");
        var ast = parser.ParseProgram();
        
        var symbolTableBuilder = new SymbolTableBuilder("test.old8", tokens);
        var symbolTable = symbolTableBuilder.Build(ast);

        var document = new DocumentParseResult
        {
            Uri = "test.old8",
            Text = code,
            Tokens = tokens,
            Ast = ast,
            SymbolTable = symbolTable,
            Diagnostics = new List<DiagnosticInfo>()
        };

        // Act
        var references = SymbolFinder.FindReferences(document, "myVar");

        // Assert
        Assert.Equal(3, references.Count); // 1 definition + 2 usages

        // Check that all references have correct positions
        var lines = references.Select(r => r.Line).ToList();
        Assert.Contains(1, lines); // Definition line (0-based)
        Assert.Contains(2, lines); // First usage
        Assert.Contains(3, lines); // Second usage
    }

    [Fact]
    public void TestFindReferences_NotFound()
    {
        // Arrange
        var code = @"
result <- 42
";
        var tokens = LangTokenizer.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code, "test.old8");
        var ast = parser.ParseProgram();
        
        var symbolTableBuilder = new SymbolTableBuilder("test.old8", tokens);
        var symbolTable = symbolTableBuilder.Build(ast);

        var document = new DocumentParseResult
        {
            Uri = "test.old8",
            Text = code,
            Tokens = tokens,
            Ast = ast,
            SymbolTable = symbolTable,
            Diagnostics = new List<DiagnosticInfo>()
        };

        // Act
        var references = SymbolFinder.FindReferences(document, "nonExistentSymbol");

        // Assert
        Assert.Empty(references);
    }

    [Fact]
    public void TestDebugTokenPositions()
    {
        // Temporary test to understand token positions
        var code = @"
func testFunction(a:int, b:int) -> int {
    return a + b
}

result <- testFunction(1, 2)
";
        var tokens = LangTokenizer.Tokenize(code);
        
        testOutputHelper.WriteLine("Code lines:");
        var lines = code.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            testOutputHelper.WriteLine($"Line {i}: '{lines[i]}'");
        }
        
        testOutputHelper.WriteLine("\nTokens:");
        foreach (var token in tokens)
        {
            testOutputHelper.WriteLine($"Line: {token.Line}, Column: {token.Column}-{token.Column + token.Value.Length - 1}, Type: {token.Type}, Value: '{token.Value}'");
        }
    }

    [Fact]
    public void TestFindReferences_NullDocument()
    {
        // Arrange
        var document = new DocumentParseResult
        {
            Uri = "test.old8",
            Text = "",
            Tokens = null,
            Ast = null,
            SymbolTable = null,
            Diagnostics = new List<DiagnosticInfo>()
        };

        // Act
        var references = SymbolFinder.FindReferences(document, "testSymbol");

        // Assert
        Assert.Empty(references);
    }
}