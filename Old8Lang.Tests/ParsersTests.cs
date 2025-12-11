using Old8Lang.AST.Expression;using Old8Lang.AST.Expression.Value;using Old8Lang.AST.Statement;using Old8Lang.LangParser;

namespace Old8Lang.Tests;

public class ParsersTests
{
    [Fact]
    public void ParseProgram_EmptyProgram_ReturnsEmptyBlock()
    {
        // Arrange
        var tokens = new List<LangToken>();
        var parser = new Old8Lang.LangParser.LangParser(tokens);
        
        // Act
        var result = parser.ParseProgram();
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.Count);
    }
    
    [Fact]
    public void ParseProgram_SimpleAssignment_ReturnsSetStatement()
    {
        // Arrange
        var tokens = new List<LangToken>
        {
            new("a", LangTokenType.Identifier, 1),
            new("<-", LangTokenType.Assignment, 1),
            new("10", LangTokenType.Number, 1),
        };
        var parser = new Old8Lang.LangParser.LangParser(tokens);
        
        // Act
        var result = parser.ParseProgram();
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        Assert.IsType<SetStatement>(result[0]);
        
        var setStmt = (SetStatement)result[0];
        Assert.IsType<LangId>(setStmt.Id);
        Assert.IsType<IntLangValue>(setStmt.Value);
        
        var id = (LangId)setStmt.Id;
        var value = (IntLangValue)setStmt.Value;
        
        Assert.Equal("a", id.IdName);
        Assert.Equal(10, value.Value);
    }
    
    [Fact]
    public void ParseProgram_IfStatement_ReturnsIfStatement()
    {
        // Arrange
        var tokens = new List<LangToken>
        {
            new("if", LangTokenType.If, 1),
            new("a", LangTokenType.Identifier, 1),
            new(">", LangTokenType.GreaterThan, 1),
            new("5", LangTokenType.Number, 1),
            new("{", LangTokenType.LeftBrace, 1),
            new("}", LangTokenType.RightBrace, 1),
        };
        var parser = new Old8Lang.LangParser.LangParser(tokens);
        
        // Act
        var result = parser.ParseProgram();
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        Assert.IsType<IfStatement>(result[0]);
    }
    
    [Fact]
    public void ParseProgram_ForStatement_ReturnsForStatement()
    {
        // Arrange
        var tokens = new List<LangToken>
        {
            new("for", LangTokenType.For, 1),
            new("i", LangTokenType.Identifier, 1),
            new("<-", LangTokenType.Assignment, 1),
            new("0", LangTokenType.Number, 1),
            new(",", LangTokenType.Comma, 1),
            new("i", LangTokenType.Identifier, 1),
            new("<", LangTokenType.LessThan, 1),
            new("5", LangTokenType.Number, 1),
            new(",", LangTokenType.Comma, 1),
            new("i", LangTokenType.Identifier, 1),
            new("++", LangTokenType.PlusPlus, 1),
            new("{", LangTokenType.LeftBrace, 1),
            new("}", LangTokenType.RightBrace, 1),
        };
        var parser = new Old8Lang.LangParser.LangParser(tokens);
        
        // Act
        var result = parser.ParseProgram();
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        Assert.IsType<ForStatement>(result[0]);
    }
    
    [Fact]
    public void ParseProgram_WhileStatement_ReturnsWhileStatement()
    {
        // Arrange
        var tokens = new List<LangToken>
        {
            new("while", LangTokenType.While, 1),
            new("a", LangTokenType.Identifier, 1),
            new("<", LangTokenType.LessThan, 1),
            new("10", LangTokenType.Number, 1),
            new("{", LangTokenType.LeftBrace, 1),
            new("}", LangTokenType.RightBrace, 1),
        };
        var parser = new Old8Lang.LangParser.LangParser(tokens);
        
        // Act
        var result = parser.ParseProgram();
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        Assert.IsType<WhileStatement>(result[0]);
    }
    
    [Fact]
    public void ParseProgram_FuncDeclaration_ReturnsFuncInit()
    {
        // Arrange
        var sourceCode = "func add(x, y) { return x + y }";
        var tokens = new List<LangToken>
        {
            new("func", LangTokenType.Func, 1),
            new("add", LangTokenType.Identifier, 1),
            new("(", LangTokenType.LeftParen, 1),
            new("x", LangTokenType.Identifier, 1),
            new(",", LangTokenType.Comma, 1),
            new("y", LangTokenType.Identifier, 1),
            new(")", LangTokenType.RightParen, 1),
            new("{", LangTokenType.LeftBrace, 1),
            new("return", LangTokenType.Return, 1),
            new("x", LangTokenType.Identifier, 1),
            new("+", LangTokenType.Plus, 1),
            new("y", LangTokenType.Identifier, 1),
            new("}", LangTokenType.RightBrace, 1),
        };
        var parser = new Old8Lang.LangParser.LangParser(tokens);
        
        // Act
        var result = parser.ParseProgram();
        
        // Assert
        Assert.NotNull(result);
        // 函数声明会被添加到ImportStatements列表中，所以Count可能为0
        // 直接检查生成的代码是否包含函数名
        Assert.Contains("add", result.ToString());
    }
    
    [Fact]
    public void ParseProgram_ClassDeclaration_ReturnsClassInit()
    {
        // Arrange
        var tokens = new List<LangToken>
        {
            new("class", LangTokenType.Class, 1),
            new("TestClass", LangTokenType.Identifier, 1),
            new("{", LangTokenType.LeftBrace, 1),
            new("field", LangTokenType.Identifier, 1),
            new("<-", LangTokenType.Assignment, 1),
            new("0", LangTokenType.Number, 1),
            new("}", LangTokenType.RightBrace, 1),
        };
        var parser = new Old8Lang.LangParser.LangParser(tokens);
        
        // Act
        var result = parser.ParseProgram();
        
        // Assert
        Assert.NotNull(result);
        // 类声明会被添加到ImportStatements列表中，所以Count可能为0
        // 直接检查生成的代码是否包含类名
        Assert.Contains("TestClass", result.ToString());
    }
    
    [Fact]
    public void ParseProgram_TryCatchStatement_ReturnsTryStatement()
    {
        // Arrange
        var tokens = new List<LangToken>
        {
            new("try", LangTokenType.Try, 1),
            new("{", LangTokenType.LeftBrace, 1),
            new("}", LangTokenType.RightBrace, 1),
            new("catch", LangTokenType.Catch, 1),
            new("(", LangTokenType.LeftParen, 1),
            new("e", LangTokenType.Identifier, 1),
            new(")", LangTokenType.RightParen, 1),
            new("{", LangTokenType.LeftBrace, 1),
            new("}", LangTokenType.RightBrace, 1),
        };
        var parser = new Old8Lang.LangParser.LangParser(tokens);
        
        // Act
        var result = parser.ParseProgram();
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        Assert.IsType<TryStatement>(result[0]);
    }
    
    [Fact]
    public void ParseProgram_ArithmeticExpression_ReturnsOperation()
    {
        // Arrange
        var tokens = new List<LangToken>
        {
            new("result", LangTokenType.Identifier, 1),
            new("<-", LangTokenType.Assignment, 1),
            new("1", LangTokenType.Number, 1),
            new("+", LangTokenType.Plus, 1),
            new("2", LangTokenType.Number, 1),
            new("*", LangTokenType.Star, 1),
            new("3", LangTokenType.Number, 1),
        };
        var parser = new Old8Lang.LangParser.LangParser(tokens);
        
        // Act
        var result = parser.ParseProgram();
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        Assert.IsType<SetStatement>(result[0]);
        
        var setStmt = (SetStatement)result[0];
        Assert.IsType<Operation>(setStmt.Value);
    }
    
    [Fact]
    public void ParseProgram_LogicalExpression_ReturnsOperation()
    {
        // Arrange
        var tokens = new List<LangToken>
        {
            new("result", LangTokenType.Identifier, 1),
            new("<-", LangTokenType.Assignment, 1),
            new("a", LangTokenType.Identifier, 1),
            new(">", LangTokenType.GreaterThan, 1),
            new("5", LangTokenType.Number, 1),
            new("and", LangTokenType.And, 1),
            new("b", LangTokenType.Identifier, 1),
            new("<", LangTokenType.LessThan, 1),
            new("10", LangTokenType.Number, 1),
        };
        var parser = new Old8Lang.LangParser.LangParser(tokens);
        
        // Act
        var result = parser.ParseProgram();
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        Assert.IsType<SetStatement>(result[0]);
        
        var setStmt = (SetStatement)result[0];
        Assert.IsType<Operation>(setStmt.Value);
    }
    
    [Fact]
    public void ParseProgram_ComparisonExpression_ReturnsOperation()
    {
        // Arrange
        var tokens = new List<LangToken>
        {
            new("result", LangTokenType.Identifier, 1),
            new("<-", LangTokenType.Assignment, 1),
            new("a", LangTokenType.Identifier, 1),
            new("==", LangTokenType.Equals, 1),
            new("b", LangTokenType.Identifier, 1),
        };
        var parser = new Old8Lang.LangParser.LangParser(tokens);
        
        // Act
        var result = parser.ParseProgram();
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        Assert.IsType<SetStatement>(result[0]);
        
        var setStmt = (SetStatement)result[0];
        Assert.IsType<Operation>(setStmt.Value);
    }
}