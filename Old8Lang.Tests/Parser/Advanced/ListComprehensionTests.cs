using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Parser.Advanced;

/// <summary>
/// 列表推导式解析测试
/// </summary>
[Collection("Sequential")]
public class ListComprehensionTests
{
    #region 基本列表推导式测试

    /// <summary>
    /// 测试简单的列表推导式 - [x for x in list]
    /// </summary>
    [Fact]
    public void ParseListComprehension_SimpleForIn_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- [x for x in [1, 2, 3]]";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<ListComprehension>(setStmt.Value);
    }

    /// <summary>
    /// 测试列表推导式中的表达式 - [x * 2 for x in list]
    /// </summary>
    [Fact]
    public void ParseListComprehension_WithExpression_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- [x * 2 for x in numbers]";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<ListComprehension>(setStmt.Value);
    }

    /// <summary>
    /// 测试列表推导式遍历范围 - [x for x in [1~10]]
    /// </summary>
    [Fact]
    public void ParseListComprehension_WithRange_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- [x * x for x in [1~5]]";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<ListComprehension>(setStmt.Value);
    }

    #endregion

    #region 带条件的列表推导式测试

    /// <summary>
    /// 测试带单个条件的列表推导式 - [x for x in list if x > 5]
    /// </summary>
    [Fact]
    public void ParseListComprehension_WithSingleCondition_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- [x for x in numbers if x > 5]";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<ListComprehension>(setStmt.Value);
    }

    /// <summary>
    /// 测试带多个条件的列表推导式 - [x for x in list if x > 5 if x % 2 == 0]
    /// </summary>
    [Fact]
    public void ParseListComprehension_WithMultipleConditions_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- [x for x in numbers if x > 5 if x % 2 == 0]";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<ListComprehension>(setStmt.Value);
    }

    /// <summary>
    /// 测试带表达式和条件的列表推导式 - [x * x for x in list if x % 2 == 0]
    /// </summary>
    [Fact]
    public void ParseListComprehension_WithExpressionAndCondition_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- [x * x for x in numbers if x % 2 == 0]";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<ListComprehension>(setStmt.Value);
    }

    #endregion

    #region 嵌套列表推导式测试

    /// <summary>
    /// 测试嵌套列表推导式 - [x for row in matrix for x in row]
    /// </summary>
    [Fact]
    public void ParseListComprehension_NestedForIn_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- [x for row in matrix for x in row]";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<ListComprehension>(setStmt.Value);
    }

    /// <summary>
    /// 测试嵌套列表推导式中的表达式 - [x + y for x in list1 for y in list2]
    /// </summary>
    [Fact]
    public void ParseListComprehension_NestedWithExpression_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- [x + y for x in [1, 2] for y in [3, 4]]";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<ListComprehension>(setStmt.Value);
    }

    /// <summary>
    /// 测试嵌套列表推导式中的条件 - [x for row in matrix for x in row if x > 5]
    /// </summary>
    [Fact]
    public void ParseListComprehension_NestedWithCondition_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- [x for row in matrix for x in row if x > 5]";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<ListComprehension>(setStmt.Value);
    }

    #endregion

    #region 高级列表推导式测试

    /// <summary>
    /// 测试列表推导式中的三元表达式 - [x if x > 0 else 0 for x in list]
    /// </summary>
    [Fact]
    public void ParseListComprehension_WithTernaryExpression_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- [x > 0 ? x : 0 for x in numbers]";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<ListComprehension>(setStmt.Value);
    }

    /// <summary>
    /// 测试列表推导式生成元组 - [(x, y) for x in list for y in list]
    /// </summary>
    [Fact]
    public void ParseListComprehension_GeneratingTuples_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- [(x, x * x) for x in numbers]";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<ListComprehension>(setStmt.Value);
    }

    /// <summary>
    /// 测试嵌套的列表推导式 - [[x for x in row] for row in matrix]
    /// </summary>
    [Fact]
    public void ParseListComprehension_NestedListComprehension_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- [[x * 2 for x in row] for row in matrix]";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<ListComprehension>(setStmt.Value);
    }

    /// <summary>
    /// 测试列表推导式中的函数调用 - [f(x) for x in list]
    /// </summary>
    [Fact]
    public void ParseListComprehension_WithFunctionCall_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- [isPrime(x) for x in numbers]";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<ListComprehension>(setStmt.Value);
    }

    /// <summary>
    /// 测试列表推导式中的类型转换 - [x as int for x in list]
    /// </summary>
    [Fact]
    public void ParseListComprehension_WithTypeConversion_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- [x as int for x in doublesList]";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<ListComprehension>(setStmt.Value);
    }

    #endregion

    #region 错误场景测试

    /// <summary>
    /// 测试缺少 for 关键字 - 应该抛出语法错误
    /// </summary>
    [Fact]
    public void ParseListComprehension_MissingForKeyword_ThrowsSyntaxError()
    {
        // Arrange
        var code = "result <- [x x in numbers]";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试缺少 in 关键字 - 应该抛出语法错误
    /// </summary>
    [Fact]
    public void ParseListComprehension_MissingInKeyword_ThrowsSyntaxError()
    {
        // Arrange
        var code = "result <- [x for x numbers]";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试缺少右括号 - 应该抛出语法错误
    /// </summary>
    [Fact]
    public void ParseListComprehension_MissingRightBracket_ThrowsSyntaxError()
    {
        // Arrange
        var code = "result <- [x for x in numbers";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试缺少表达式部分 - 应该抛出语法错误
    /// </summary>
    [Fact]
    public void ParseListComprehension_MissingExpression_ThrowsSyntaxError()
    {
        // Arrange
        var code = "result <- [for x in numbers]";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试缺少可迭代对象 - 应该抛出语法错误
    /// </summary>
    [Fact]
    public void ParseListComprehension_MissingIterable_ThrowsSyntaxError()
    {
        // Arrange
        var code = "result <- [x for x in]";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试条件后缺少表达式 - 应该抛出语法错误
    /// </summary>
    [Fact]
    public void ParseListComprehension_MissingConditionExpression_ThrowsSyntaxError()
    {
        // Arrange
        var code = "result <- [x for x in numbers if]";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion

    #region 边界情况测试

    /// <summary>
    /// 测试空列表作为可迭代对象
    /// </summary>
    [Fact]
    public void ParseListComprehension_EmptyList_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- [x for x in []]";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<ListComprehension>(setStmt.Value);
    }

    /// <summary>
    /// 测试字符串作为可迭代对象 - [c for c in "hello"]
    /// </summary>
    [Fact]
    public void ParseListComprehension_StringAsIterable_ParsesSuccessfully()
    {
        // Arrange
        var code = @"result <- [c for c in ""hello""]";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<ListComprehension>(setStmt.Value);
    }

    /// <summary>
    /// 测试复杂的嵌套表达式
    /// </summary>
    [Fact]
    public void ParseListComprehension_ComplexNestedExpression_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- [x * y + z for x in [1, 2] for y in [3, 4] for z in [5, 6] if x + y > 3]";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<ListComprehension>(setStmt.Value);
    }

    #endregion
}
