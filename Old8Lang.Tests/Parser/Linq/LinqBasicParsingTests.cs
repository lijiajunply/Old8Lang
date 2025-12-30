using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Linq;
using Old8Lang.AST.Statement;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Parser.Linq;

/// <summary>
/// LINQ 基础语法解析测试
/// </summary>
[Collection("Sequential")]
public class LinqBasicParsingTests
{
    [Fact]
    public void ParseLinq_FromWhereSelect_ParsesCorrectly()
    {
        // Arrange
        var code = "result <- from x in numbers where x > 5 select x";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act
        var program = parser.ParseProgram();

        // Assert
        Assert.NotNull(program);
        Assert.Equal(1, program.Count);

        var statement = program[0];
        Assert.IsType<SetStatement>(statement);

        var setStmt = (SetStatement)statement;
        Assert.IsType<LinqExpression>(setStmt.Value);

        var linqExpr = (LinqExpression)setStmt.Value;
        Assert.NotNull(linqExpr.FromClause);
        Assert.Equal("x", linqExpr.FromClause.RangeVariable);
        Assert.Single(linqExpr.BodyClauses);
        Assert.IsType<WhereClause>(linqExpr.BodyClauses[0]);
        Assert.IsType<SelectClause>(linqExpr.TerminationClause);
    }

    [Fact]
    public void ParseLinq_FromSelect_ParsesWithoutWhere()
    {
        // Arrange
        var code = "result <- from x in list select x";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act
        var program = parser.ParseProgram();

        // Assert
        Assert.NotNull(program);
        Assert.Equal(1, program.Count);

        var setStmt = (SetStatement)program[0];
        var linqExpr = (LinqExpression)setStmt.Value;
        Assert.Empty(linqExpr.BodyClauses);
        Assert.IsType<SelectClause>(linqExpr.TerminationClause);
    }

    [Fact]
    public void ParseLinq_SelectProjection_ParsesExpression()
    {
        // Arrange
        var code = "result <- from x in numbers select x * 2";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new Old8Lang.LangParser.LangParser(tokens, code);

        // Act
        var program = parser.ParseProgram();

        // Assert
        Assert.NotNull(program);
        Assert.Equal(1, program.Count);

        var setStmt = (SetStatement)program[0];
        var linqExpr = (LinqExpression)setStmt.Value;
        var selectClause = (SelectClause)linqExpr.TerminationClause;
        Assert.NotNull(selectClause.Projection);
        Assert.IsType<Operation>(selectClause.Projection);
    }

    [Fact]
    public void ParseLinq_OrderByAscending_ParsesCorrectly()
    {
        // Arrange
        var code = "result <- from x in numbers orderby x select x";
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
        Assert.IsType<OrderByClause>(linqExpr.BodyClauses[0]);

        var orderBy = (OrderByClause)linqExpr.BodyClauses[0];
        Assert.Single(orderBy.Orderings);
        Assert.True(orderBy.Orderings[0].IsAscending);
    }

    [Fact]
    public void ParseLinq_OrderByDescending_ParsesCorrectly()
    {
        // Arrange
        var code = "result <- from x in numbers orderby x descending select x";
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
        Assert.Single(orderBy.Orderings);
        Assert.False(orderBy.Orderings[0].IsAscending);
    }

    [Fact]
    public void ParseLinq_Let_ParsesCorrectly()
    {
        // Arrange
        var code = "result <- from x in numbers let squared <- x * x select squared";
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
        Assert.IsType<LetClause>(linqExpr.BodyClauses[0]);

        var letClause = (LetClause)linqExpr.BodyClauses[0];
        Assert.Equal("squared", letClause.Variable);
        Assert.NotNull(letClause.Expression);
    }

    [Fact]
    public void ParseLinq_MultipleWhere_ParsesAll()
    {
        // Arrange
        var code = "result <- from x in numbers where x > 5 where x < 10 select x";
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
        Assert.All(linqExpr.BodyClauses, clause => Assert.IsType<WhereClause>(clause));
    }

    [Fact]
    public void ParseLinq_LetWhereSelect_ParsesInOrder()
    {
        // Arrange
        var code = "result <- from x in list let y <- x * 2 where y > 10 select y";
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
        Assert.IsType<LetClause>(linqExpr.BodyClauses[0]);
        Assert.IsType<WhereClause>(linqExpr.BodyClauses[1]);
    }
}
