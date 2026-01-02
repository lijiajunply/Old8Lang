using Xunit;
using Old8Lang.LangParser;
using Old8Lang.LanguageServer.Services;
using Old8Lang.LanguageServer.Models;
using Xunit.Abstractions;

namespace Old8Lang.Tests.LanguageServer;

public class SymbolTableBuilderTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void TestBuild_Function()
    {
        // Arrange
        var code = @"
func testFunction(a:int, b:int) -> int {
    return a + b
}
";
        var tokens = LangTokenizer.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code, "test.old8");
        var ast = parser.ParseProgram();

        // Act
        var builder = new SymbolTableBuilder("test.old8", tokens);
        var symbolTable = builder.Build(ast);

        // Assert
        Assert.Single(symbolTable);
        Assert.True(symbolTable.ContainsKey("testFunction"));
        
        var symbol = symbolTable["testFunction"];
        Assert.Equal("testFunction", symbol.Name);
        Assert.Equal(SymbolKind.Function, symbol.Kind);
        Assert.Contains("-> int", symbol.Type);
        Assert.NotNull(symbol.Location);
    }

    [Fact]
    public void TestBuild_AsyncFunction()
    {
        // Arrange
        var code = @"
async func asyncFunction(data:string) -> void {
    PrintLine(data)
}
";
        var tokens = LangTokenizer.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code, "test.old8");
        var ast = parser.ParseProgram();

        // Act
        var builder = new SymbolTableBuilder("test.old8", tokens);
        var symbolTable = builder.Build(ast);

        // Assert
        Assert.Single(symbolTable);
        Assert.True(symbolTable.ContainsKey("asyncFunction"));
        
        var symbol = symbolTable["asyncFunction"];
        Assert.Equal("asyncFunction", symbol.Name);
        Assert.Equal(SymbolKind.Function, symbol.Kind);
        Assert.Contains("async func", symbol.Type);
        Assert.NotNull(symbol.Location);
    }

    [Fact]
    public void TestBuild_Class()
    {
        // Arrange
        var code = @"
class User {
    public name:string
    private age:int
    
    public func getName() -> string {
        return this.name
    }
    
    private func getAge() -> int {
        return this.age
    }
}
";
        var tokens = LangTokenizer.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code, "test.old8");
        var ast = parser.ParseProgram();

        // Act
        var builder = new SymbolTableBuilder("test.old8", tokens);
        var symbolTable = builder.Build(ast);

        // Assert
        Assert.Single(symbolTable); // Only class at global level
        Assert.True(symbolTable.ContainsKey("User"));
        
        var classSymbol = symbolTable["User"];
        Assert.Equal("User", classSymbol.Name);
        Assert.Equal(SymbolKind.Class, classSymbol.Kind);
        Assert.Contains("class User", classSymbol.Type);
        Assert.NotNull(classSymbol.Location);

        // Check members
        Assert.Equal(4, classSymbol.Members.Count); // 2 properties + 2 methods
        
        Assert.True(classSymbol.Members.ContainsKey("name"));
        Assert.True(classSymbol.Members.ContainsKey("age"));
        Assert.True(classSymbol.Members.ContainsKey("getName"));
        Assert.True(classSymbol.Members.ContainsKey("getAge"));

        // Check member details
        var nameProp = classSymbol.Members["name"];
        Assert.Equal("name", nameProp.Name);
        Assert.Equal(SymbolKind.Property, nameProp.Kind);
        Assert.Equal(AccessModifier.Public, nameProp.AccessModifier);
        Assert.False(nameProp.IsStatic);

        var ageProp = classSymbol.Members["age"];
        Assert.Equal("age", ageProp.Name);
        Assert.Equal(SymbolKind.Property, ageProp.Kind);
        Assert.Equal(AccessModifier.Private, ageProp.AccessModifier);
        Assert.False(ageProp.IsStatic);

        var getNameMethod = classSymbol.Members["getName"];
        Assert.Equal("getName", getNameMethod.Name);
        Assert.Equal(SymbolKind.Method, getNameMethod.Kind);
        Assert.Equal(AccessModifier.Public, getNameMethod.AccessModifier);
        Assert.False(getNameMethod.IsStatic);

        var getAgeMethod = classSymbol.Members["getAge"];
        Assert.Equal("getAge", getAgeMethod.Name);
        Assert.Equal(SymbolKind.Method, getAgeMethod.Kind);
        Assert.Equal(AccessModifier.Private, getAgeMethod.AccessModifier);
        Assert.False(getAgeMethod.IsStatic);

        // Check parent relationships
        Assert.Equal(classSymbol, nameProp.Parent);
        Assert.Equal(classSymbol, ageProp.Parent);
        Assert.Equal(classSymbol, getNameMethod.Parent);
        Assert.Equal(classSymbol, getAgeMethod.Parent);
    }

    [Fact]
    public void TestBuild_ClassWithStaticMembers()
    {
        // Arrange
        var code = @"
class MathUtil {
    static func add(a:int, b:int) -> int {
        return a + b
    }
    
    static PI <- 3.14159
}
";
        var tokens = LangTokenizer.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code, "test.old8");
        var ast = parser.ParseProgram();

        // Act
        var builder = new SymbolTableBuilder("test.old8", tokens);
        var symbolTable = builder.Build(ast);

        // Assert
        Assert.Single(symbolTable);
        var classSymbol = symbolTable["MathUtil"];
        
        // Check static members
        Assert.Equal(2, classSymbol.Members.Count);
        
        var addMethod = classSymbol.Members["add"];
        Assert.Equal("add", addMethod.Name);
        Assert.Equal(SymbolKind.Method, addMethod.Kind);
        Assert.Equal(AccessModifier.Public, addMethod.AccessModifier);
        Assert.True(addMethod.IsStatic);

        var piConstant = classSymbol.Members["PI"];
        Assert.Equal("PI", piConstant.Name);
        Assert.Equal(SymbolKind.Property, piConstant.Kind);
        Assert.True(piConstant.IsStatic);
    }

    [Fact]
    public void TestBuild_Variables()
    {
        // Arrange
        var code = @"
name <- ""Alice""
age <- 25
scores <- {90, 85, 95}
";
        var tokens = LangTokenizer.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code, "test.old8");
        var ast = parser.ParseProgram();

        // Act
        var builder = new SymbolTableBuilder("test.old8", tokens);
        var symbolTable = builder.Build(ast);

        // Assert
        Assert.Equal(3, symbolTable.Count);
        
        Assert.True(symbolTable.ContainsKey("name"));
        Assert.True(symbolTable.ContainsKey("age"));
        Assert.True(symbolTable.ContainsKey("scores"));

        var nameSymbol = symbolTable["name"];
        Assert.Equal(SymbolKind.Variable, nameSymbol.Kind);
        Assert.Equal("name", nameSymbol.Name);
        Assert.NotNull(nameSymbol.Location);

        var ageSymbol = symbolTable["age"];
        Assert.Equal(SymbolKind.Variable, ageSymbol.Kind);
        Assert.Equal("age", ageSymbol.Name);
        Assert.NotNull(ageSymbol.Location);

        var scoresSymbol = symbolTable["scores"];
        Assert.Equal(SymbolKind.Variable, scoresSymbol.Kind);
        Assert.Equal("scores", scoresSymbol.Name);
        Assert.NotNull(scoresSymbol.Location);
    }

    [Fact]
    public void TestBuild_VariablesWithTypeInference()
    {
        // Arrange
        var code = @"
user <- User() // Should infer type as User
";
        var tokens = LangTokenizer.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code, "test.old8");
        var ast = parser.ParseProgram();

        // Act
        var builder = new SymbolTableBuilder("test.old8", tokens);
        var symbolTable = builder.Build(ast);

        // Assert
        Assert.Single(symbolTable);
        var symbol = symbolTable["user"];
        Assert.Equal(SymbolKind.Variable, symbol.Kind);
        Assert.Equal("User", symbol.Type); // Should infer type from constructor call
    }

    [Fact]
    public void TestBuild_MultipleSymbols()
    {
        // Arrange
        var code = @"
func calculate(a:int, b:int) -> int {
    return a + b
}

class Calculator {
    public func multiply(a:int, b:int) -> int {
        return a * b
    }
}

result <- 42
PI <- 3.14159
";
        var tokens = LangTokenizer.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code, "test.old8");
        var ast = parser.ParseProgram();

        // Act
        var builder = new SymbolTableBuilder("test.old8", tokens);
        var symbolTable = builder.Build(ast);

        // Assert
        Assert.Equal(4, symbolTable.Count); // calculate, Calculator, result, PI
        
        Assert.True(symbolTable.ContainsKey("calculate"));
        Assert.True(symbolTable.ContainsKey("Calculator"));
        Assert.True(symbolTable.ContainsKey("result"));
        Assert.True(symbolTable.ContainsKey("PI"));

        // Check function
        var calculateSymbol = symbolTable["calculate"];
        Assert.Equal(SymbolKind.Function, calculateSymbol.Kind);

        // Check class
        var calculatorSymbol = symbolTable["Calculator"];
        Assert.Equal(SymbolKind.Class, calculatorSymbol.Kind);
        Assert.Single(calculatorSymbol.Members); // multiply method

        // Check variables
        var resultSymbol = symbolTable["result"];
        Assert.Equal(SymbolKind.Variable, resultSymbol.Kind);

        var piSymbol = symbolTable["PI"];
        Assert.Equal(SymbolKind.Variable, piSymbol.Kind);
    }

    [Fact]
    public void TestBuild_DocumentationExtraction()
    {
        // Arrange
        var code = @"
/// 这是一个测试函数
/// 计算两个数的和
/// 参数 a: 第一个数
/// 参数 b: 第二个数
/// 返回: 两数之和
func addWithDoc(a:int, b:int) -> int {
    return a + b
}

/// 用户类
/// 用于存储用户信息
class UserWithDoc {
    /// 用户姓名
    public name:string
    
    /// 获取用户姓名
    /// 返回: 用户姓名字符串
    public func getName() -> string {
        return this.name
    }
}
";
        var tokens = LangTokenizer.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code, "test.old8");
        var ast = parser.ParseProgram();

        // Act
        var builder = new SymbolTableBuilder("test.old8", tokens);
        var symbolTable = builder.Build(ast);

        // Assert
        // Check function documentation
        var addSymbol = symbolTable["addWithDoc"];
        Assert.NotNull(addSymbol.Documentation);
        Assert.Contains("测试函数", addSymbol.Documentation);
        Assert.Contains("参数 a", addSymbol.Documentation);

        // Check class documentation
        var userSymbol = symbolTable["UserWithDoc"];
        Assert.NotNull(userSymbol.Documentation);
        Assert.Contains("用户类", userSymbol.Documentation);

        // Check member documentation
        var nameProp = userSymbol.Members["name"];
        Assert.NotNull(nameProp.Documentation);
        Assert.Contains("用户姓名", nameProp.Documentation);

        var getNameMethod = userSymbol.Members["getName"];
        Assert.NotNull(getNameMethod.Documentation);
        Assert.Contains("获取用户姓名", getNameMethod.Documentation);
    }

    [Fact]
    public void TestBuild_PositionInformation()
    {
        // Arrange
        var code = @"
func testFunc() -> int {
    return 42
}
";
        var tokens = LangTokenizer.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code, "test.old8");
        var ast = parser.ParseProgram();

        // Act
        var builder = new SymbolTableBuilder("test.old8", tokens);
        var symbolTable = builder.Build(ast);

        // Assert
        var symbol = symbolTable["testFunc"];
        Assert.NotNull(symbol.Location);
        Assert.Equal("test.old8", symbol.Location.Uri);
        Assert.True(symbol.Location.Line >= 0);
        Assert.True(symbol.Location.Column >= 0);
        Assert.True(symbol.Location.EndLine >= symbol.Location.Line);
        Assert.True(symbol.Location.EndColumn >= symbol.Location.Column);
    }

    [Fact]
    public void TestBuild_EmptyProgram()
    {
        // Arrange
        var code = "";
        var tokens = LangTokenizer.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code, "test.old8");
        var ast = parser.ParseProgram();

        // Act
        var builder = new SymbolTableBuilder("test.old8", tokens);
        var symbolTable = builder.Build(ast);

        // Assert
        Assert.Empty(symbolTable);
    }

    [Fact]
    public void TestBuild_OnlyStatements()
    {
        // Arrange
        var code = @"
PrintLine(""Hello"")
PrintLine(""World"")
";
        var tokens = LangTokenizer.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code, "test.old8");
        var ast = parser.ParseProgram();

        // Act
        var builder = new SymbolTableBuilder("test.old8", tokens);
        var symbolTable = builder.Build(ast);

        // Assert
        Assert.Empty(symbolTable); // No symbols from function calls only
    }
}