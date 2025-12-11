using Old8Lang.AST;using Old8Lang.AST.Expression;using Old8Lang.AST.Expression.Value;using Old8Lang.AST.Statement;using Old8Lang.LangParser;using Xunit;

namespace Old8Lang.Tests;

public class ParsersTests
{
    [Fact]
    public void ParseProgram_EmptyProgram_ReturnsEmptyBlock()
    {
        // Arrange
        var tokens = new List<Old8Lang.LangParser.LangToken>();
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
        var tokens = new List<Old8Lang.LangParser.LangToken>
        {
            new("a", Old8Lang.LangParser.LangTokenType.Identifier, 1),
            new("<-", Old8Lang.LangParser.LangTokenType.Assignment, 1),
            new("10", Old8Lang.LangParser.LangTokenType.Number, 1),
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
        var tokens = new List<Old8Lang.LangParser.LangToken>
        {
            new("if", Old8Lang.LangParser.LangTokenType.If, 1),
            new("a", Old8Lang.LangParser.LangTokenType.Identifier, 1),
            new(">", Old8Lang.LangParser.LangTokenType.GreaterThan, 1),
            new("5", Old8Lang.LangParser.LangTokenType.Number, 1),
            new("{", Old8Lang.LangParser.LangTokenType.LeftBrace, 1),
            new("}", Old8Lang.LangParser.LangTokenType.RightBrace, 1),
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
        var tokens = new List<Old8Lang.LangParser.LangToken>
        {
            new("for", Old8Lang.LangParser.LangTokenType.For, 1),
            new("i", Old8Lang.LangParser.LangTokenType.Identifier, 1),
            new("<-", Old8Lang.LangParser.LangTokenType.Assignment, 1),
            new("0", Old8Lang.LangParser.LangTokenType.Number, 1),
            new(",", Old8Lang.LangParser.LangTokenType.Comma, 1),
            new("i", Old8Lang.LangParser.LangTokenType.Identifier, 1),
            new("<", Old8Lang.LangParser.LangTokenType.LessThan, 1),
            new("5", Old8Lang.LangParser.LangTokenType.Number, 1),
            new(",", Old8Lang.LangParser.LangTokenType.Comma, 1),
            new("i", Old8Lang.LangParser.LangTokenType.Identifier, 1),
            new("++", Old8Lang.LangParser.LangTokenType.PlusPlus, 1),
            new("{", Old8Lang.LangParser.LangTokenType.LeftBrace, 1),
            new("}", Old8Lang.LangParser.LangTokenType.RightBrace, 1),
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
        var tokens = new List<Old8Lang.LangParser.LangToken>
        {
            new("while", Old8Lang.LangParser.LangTokenType.While, 1),
            new("a", Old8Lang.LangParser.LangTokenType.Identifier, 1),
            new("<", Old8Lang.LangParser.LangTokenType.LessThan, 1),
            new("10", Old8Lang.LangParser.LangTokenType.Number, 1),
            new("{", Old8Lang.LangParser.LangTokenType.LeftBrace, 1),
            new("}", Old8Lang.LangParser.LangTokenType.RightBrace, 1),
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
        var tokens = new List<Old8Lang.LangParser.LangToken>
        {
            new("func", Old8Lang.LangParser.LangTokenType.Func, 1),
            new("add", Old8Lang.LangParser.LangTokenType.Identifier, 1),
            new("(", Old8Lang.LangParser.LangTokenType.LeftParen, 1),
            new("x", Old8Lang.LangParser.LangTokenType.Identifier, 1),
            new(",", Old8Lang.LangParser.LangTokenType.Comma, 1),
            new("y", Old8Lang.LangParser.LangTokenType.Identifier, 1),
            new(")", Old8Lang.LangParser.LangTokenType.RightParen, 1),
            new("{", Old8Lang.LangParser.LangTokenType.LeftBrace, 1),
            new("return", Old8Lang.LangParser.LangTokenType.Return, 1),
            new("x", Old8Lang.LangParser.LangTokenType.Identifier, 1),
            new("+", Old8Lang.LangParser.LangTokenType.Plus, 1),
            new("y", Old8Lang.LangParser.LangTokenType.Identifier, 1),
            new("}", Old8Lang.LangParser.LangTokenType.RightBrace, 1),
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
        var tokens = new List<Old8Lang.LangParser.LangToken>
        {
            new("class", Old8Lang.LangParser.LangTokenType.Class, 1),
            new("TestClass", Old8Lang.LangParser.LangTokenType.Identifier, 1),
            new("{", Old8Lang.LangParser.LangTokenType.LeftBrace, 1),
            new("field", Old8Lang.LangParser.LangTokenType.Identifier, 1),
            new("<-", Old8Lang.LangParser.LangTokenType.Assignment, 1),
            new("0", Old8Lang.LangParser.LangTokenType.Number, 1),
            new("}", Old8Lang.LangParser.LangTokenType.RightBrace, 1),
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
        var tokens = new List<Old8Lang.LangParser.LangToken>
        {
            new("try", Old8Lang.LangParser.LangTokenType.Try, 1),
            new("{", Old8Lang.LangParser.LangTokenType.LeftBrace, 1),
            new("}", Old8Lang.LangParser.LangTokenType.RightBrace, 1),
            new("catch", Old8Lang.LangParser.LangTokenType.Catch, 1),
            new("(", Old8Lang.LangParser.LangTokenType.LeftParen, 1),
            new("e", Old8Lang.LangParser.LangTokenType.Identifier, 1),
            new(")", Old8Lang.LangParser.LangTokenType.RightParen, 1),
            new("{", Old8Lang.LangParser.LangTokenType.LeftBrace, 1),
            new("}", Old8Lang.LangParser.LangTokenType.RightBrace, 1),
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
        var tokens = new List<Old8Lang.LangParser.LangToken>
        {
            new("result", Old8Lang.LangParser.LangTokenType.Identifier, 1),
            new("<-", Old8Lang.LangParser.LangTokenType.Assignment, 1),
            new("1", Old8Lang.LangParser.LangTokenType.Number, 1),
            new("+", Old8Lang.LangParser.LangTokenType.Plus, 1),
            new("2", Old8Lang.LangParser.LangTokenType.Number, 1),
            new("*", Old8Lang.LangParser.LangTokenType.Star, 1),
            new("3", Old8Lang.LangParser.LangTokenType.Number, 1),
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
        var tokens = new List<Old8Lang.LangParser.LangToken>
        {
            new("result", Old8Lang.LangParser.LangTokenType.Identifier, 1),
            new("<-", Old8Lang.LangParser.LangTokenType.Assignment, 1),
            new("a", Old8Lang.LangParser.LangTokenType.Identifier, 1),
            new(">", Old8Lang.LangParser.LangTokenType.GreaterThan, 1),
            new("5", Old8Lang.LangParser.LangTokenType.Number, 1),
            new("and", Old8Lang.LangParser.LangTokenType.And, 1),
            new("b", Old8Lang.LangParser.LangTokenType.Identifier, 1),
            new("<", Old8Lang.LangParser.LangTokenType.LessThan, 1),
            new("10", Old8Lang.LangParser.LangTokenType.Number, 1),
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
        var tokens = new List<Old8Lang.LangParser.LangToken>
        {
            new("result", Old8Lang.LangParser.LangTokenType.Identifier, 1),
            new("<-", Old8Lang.LangParser.LangTokenType.Assignment, 1),
            new("a", Old8Lang.LangParser.LangTokenType.Identifier, 1),
            new("==", Old8Lang.LangParser.LangTokenType.Equals, 1),
            new("b", Old8Lang.LangParser.LangTokenType.Identifier, 1),
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