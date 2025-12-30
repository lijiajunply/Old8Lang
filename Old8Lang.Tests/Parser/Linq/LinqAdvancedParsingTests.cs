using Old8Lang.AST.Expression.Linq;
using Old8Lang.AST.Statement;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Parser.Linq;

/// <summary>
/// LINQ 高级语法解析测试（GroupBy, 复杂查询等）
/// </summary>
[Collection("Sequential")]
public class LinqAdvancedParsingTests
{
    [Fact]
    public void ParseLinq_GroupBy_ParsesCorrectly()
    {
        // Arrange
        var code = "result <- from x in numbers group x by x % 2";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act
        var program = parser.ParseProgram();

        // Assert
        Assert.NotNull(program);
        Assert.Equal(1, program.Count);

        var setStmt = (SetStatement)program[0];
        Assert.IsType<LinqExpression>(setStmt.Value);

        var linqExpr = (LinqExpression)setStmt.Value;
        Assert.IsType<GroupByClause>(linqExpr.TerminationClause);

        var groupBy = (GroupByClause)linqExpr.TerminationClause;
        Assert.NotNull(groupBy.ElementExpression);
        Assert.NotNull(groupBy.KeyExpression);
    }

    [Fact]
    public void ParseLinq_WhereGroupBy_ParsesCorrectly()
    {
        // Arrange
        var code = "result <- from x in numbers where x > 0 group x by x";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act
        var program = parser.ParseProgram();

        // Assert
        Assert.NotNull(program);
        Assert.Equal(1, program.Count);

        var setStmt = (SetStatement)program[0];
        var linqExpr = (LinqExpression)setStmt.Value;
        Assert.Single(linqExpr.BodyClauses);
        Assert.IsType<WhereClause>(linqExpr.BodyClauses[0]);
        Assert.IsType<GroupByClause>(linqExpr.TerminationClause);
    }

    [Fact]
    public void ParseLinq_OrderByMultipleKeys_ParsesCorrectly()
    {
        // Arrange
        var code = "result <- from x in list orderby x, x descending select x";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act
        var program = parser.ParseProgram();

        // Assert
        Assert.NotNull(program);
        Assert.Equal(1, program.Count);

        var setStmt = (SetStatement)program[0];
        var linqExpr = (LinqExpression)setStmt.Value;
        var orderBy = (OrderByClause)linqExpr.BodyClauses[0];
        Assert.Equal(2, orderBy.Orderings.Count);
        Assert.True(orderBy.Orderings[0].IsAscending);
        Assert.False(orderBy.Orderings[1].IsAscending);
    }

    [Fact]
    public void ParseLinq_MultipleLet_ParsesAllCorrectly()
    {
        // Arrange
        var code = "result <- from x in numbers let a <- x * 2 let b <- a * 3 select b";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act
        var program = parser.ParseProgram();

        // Assert
        Assert.NotNull(program);
        Assert.Equal(1, program.Count);

        var setStmt = (SetStatement)program[0];
        var linqExpr = (LinqExpression)setStmt.Value;
        Assert.Equal(2, linqExpr.BodyClauses.Count);
        Assert.All(linqExpr.BodyClauses, clause => Assert.IsType<LetClause>(clause));

        var let1 = (LetClause)linqExpr.BodyClauses[0];
        var let2 = (LetClause)linqExpr.BodyClauses[1];
        Assert.Equal("a", let1.Variable);
        Assert.Equal("b", let2.Variable);
    }

    [Fact]
    public void ParseLinq_ComplexQuery_ParsesAllClauses()
    {
        // Arrange
        var code = "result <- from x in data let y <- x * 2 where y > 10 orderby y descending select y";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act
        var program = parser.ParseProgram();

        // Assert
        Assert.NotNull(program);
        Assert.Equal(1, program.Count);

        var setStmt = (SetStatement)program[0];
        var linqExpr = (LinqExpression)setStmt.Value;
        Assert.Equal(3, linqExpr.BodyClauses.Count);
        Assert.IsType<LetClause>(linqExpr.BodyClauses[0]);
        Assert.IsType<WhereClause>(linqExpr.BodyClauses[1]);
        Assert.IsType<OrderByClause>(linqExpr.BodyClauses[2]);
        Assert.IsType<SelectClause>(linqExpr.TerminationClause);
    }
}
