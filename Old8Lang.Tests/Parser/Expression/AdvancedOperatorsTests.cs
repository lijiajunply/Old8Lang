using Old8Lang.AST.Expression;
using Old8Lang.AST.Statement;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Parser.Expression;

/// <summary>
/// 高级运算符解析测试 - 幂运算、点运算符、成员访问
/// </summary>
[Collection("Sequential")]
public class AdvancedOperatorsTests
{
    #region 幂运算测试 (^)

    /// <summary>
    /// 测试简单的幂运算 - 2 ^ 8
    /// </summary>
    [Fact]
    public void ParsePower_SimplePower_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- 2 ^ 8";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<Operation>(setStmt.Value);
    }

    /// <summary>
    /// 测试链式幂运算（右结合）- 2 ^ 3 ^ 2 应解析为 2 ^ (3 ^ 2)
    /// </summary>
    [Fact]
    public void ParsePower_ChainedPower_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- 2 ^ 3 ^ 2";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<Operation>(setStmt.Value);
    }

    /// <summary>
    /// 测试幂运算与括号 - (2 ^ 3) ^ 2
    /// </summary>
    [Fact]
    public void ParsePower_WithParentheses_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- (2 ^ 3) ^ 2";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<Operation>(setStmt.Value);
    }

    /// <summary>
    /// 测试幂运算与其他算术运算符的优先级 - 2 + 3 ^ 2
    /// </summary>
    [Fact]
    public void ParsePower_WithArithmeticOperators_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- 2 + 3 ^ 2";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<Operation>(setStmt.Value);
    }

    /// <summary>
    /// 测试幂运算与乘法运算符 - 2 * 3 ^ 2
    /// </summary>
    [Fact]
    public void ParsePower_WithMultiplication_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- 2 * 3 ^ 2";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<Operation>(setStmt.Value);
    }

    /// <summary>
    /// 测试负数的幂运算 - (-2) ^ 3
    /// </summary>
    [Fact]
    public void ParsePower_NegativeBase_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- (-2) ^ 3";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<Operation>(setStmt.Value);
    }

    /// <summary>
    /// 测试浮点数的幂运算 - 2.5 ^ 3.2
    /// </summary>
    [Fact]
    public void ParsePower_FloatNumbers_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- 2.5 ^ 3.2";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<Operation>(setStmt.Value);
    }

    /// <summary>
    /// 测试变量的幂运算 - x ^ y
    /// </summary>
    [Fact]
    public void ParsePower_Variables_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- x ^ y";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<Operation>(setStmt.Value);
    }

    /// <summary>
    /// 测试幂运算缺少右操作数 - 应该抛出语法错误
    /// </summary>
    [Fact]
    public void ParsePower_MissingRightOperand_ThrowsSyntaxError()
    {
        // Arrange
        var code = "result <- 2 ^";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion

    #region 点运算符测试 (.)

    /// <summary>
    /// 测试简单的成员访问 - obj.field
    /// </summary>
    [Fact]
    public void ParseDotExpr_SimpleMemberAccess_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- obj.field";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<Operation>(setStmt.Value);
    }

    /// <summary>
    /// 测试链式成员访问 - obj.field1.field2.field3
    /// </summary>
    [Fact]
    public void ParseDotExpr_ChainedMemberAccess_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- obj.field1.field2.field3";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<Operation>(setStmt.Value);
    }

    /// <summary>
    /// 测试成员访问后的函数调用 - obj.method()
    /// </summary>
    [Fact]
    public void ParseDotExpr_MethodCall_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- obj.method()";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<Operation>(setStmt.Value);
    }

    /// <summary>
    /// 测试成员访问后的函数调用（带参数）- obj.method(a, b)
    /// </summary>
    [Fact]
    public void ParseDotExpr_MethodCallWithArguments_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- obj.method(a, b)";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<Operation>(setStmt.Value);
    }

    /// <summary>
    /// 测试数组索引访问 - arr[0]
    /// </summary>
    [Fact]
    public void ParseDotExpr_ArrayIndex_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- arr[0]";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        // 数组索引被解析为 LangListItem
        Assert.NotNull(setStmt.Value);
    }

    /// <summary>
    /// 测试成员访问后的数组索引 - obj.field[0]
    /// </summary>
    [Fact]
    public void ParseDotExpr_MemberAccessThenArrayIndex_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- obj.field[0]";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<Operation>(setStmt.Value);
    }

    /// <summary>
    /// 测试数组索引后的成员访问 - arr[0].field
    /// </summary>
    [Fact]
    public void ParseDotExpr_ArrayIndexThenMemberAccess_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- arr[0].field";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<Operation>(setStmt.Value);
    }

    /// <summary>
    /// 测试链式数组索引 - arr[0][1][2]
    /// </summary>
    [Fact]
    public void ParseDotExpr_ChainedArrayIndex_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- arr[0][1][2]";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<Operation>(setStmt.Value);
    }

    /// <summary>
    /// 测试字典键访问 - dict["key"]
    /// </summary>
    [Fact]
    public void ParseDotExpr_DictionaryKeyAccess_ParsesSuccessfully()
    {
        // Arrange
        var code = @"result <- dict[""key""]";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        // 字典键访问被解析为 LangListItem
        Assert.NotNull(setStmt.Value);
    }

    /// <summary>
    /// 测试复杂的索引表达式 - arr[i + 1]
    /// </summary>
    [Fact]
    public void ParseDotExpr_ComplexIndexExpression_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- arr[i + 1]";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        // 复杂索引表达式被解析为 LangListItem
        Assert.NotNull(setStmt.Value);
    }

    #endregion

    #region 点运算符错误场景测试

    /// <summary>
    /// 测试点运算符后缺少成员名 - 应该抛出语法错误
    /// </summary>
    [Fact]
    public void ParseDotExpr_MissingMemberName_ThrowsSyntaxError()
    {
        // Arrange
        var code = "result <- obj.";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试索引访问缺少右括号 - 应该抛出语法错误
    /// </summary>
    [Fact]
    public void ParseDotExpr_ArrayIndexMissingRightBracket_ThrowsSyntaxError()
    {
        // Arrange
        var code = "result <- arr[0";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试索引访问缺少索引表达式 - 应该抛出语法错误
    /// </summary>
    [Fact]
    public void ParseDotExpr_ArrayIndexMissingExpression_ThrowsSyntaxError()
    {
        // Arrange
        var code = "result <- arr[]";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    #endregion

    #region 运算符优先级测试

    /// <summary>
    /// 测试点运算符与幂运算符的优先级 - obj.field ^ 2
    /// </summary>
    [Fact]
    public void ParseOperatorPrecedence_DotAndPower_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- obj.field ^ 2";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<Operation>(setStmt.Value);
    }

    /// <summary>
    /// 测试索引与幂运算符的优先级 - arr[0] ^ 2
    /// </summary>
    [Fact]
    public void ParseOperatorPrecedence_IndexAndPower_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- arr[0] ^ 2";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<Operation>(setStmt.Value);
    }

    /// <summary>
    /// 测试点运算符与算术运算符的优先级 - obj.field + 2
    /// </summary>
    [Fact]
    public void ParseOperatorPrecedence_DotAndArithmetic_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- obj.field + 2";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<Operation>(setStmt.Value);
    }

    /// <summary>
    /// 测试复杂的运算符混合 - (obj.field[0] + 2) ^ 3
    /// </summary>
    [Fact]
    public void ParseOperatorPrecedence_ComplexMixedOperators_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- (obj.field[0] + 2) ^ 3";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<Operation>(setStmt.Value);
    }

    #endregion

    #region 特殊用法测试

    /// <summary>
    /// 测试函数调用结果的成员访问 - getObject().field
    /// </summary>
    [Fact]
    public void ParseDotExpr_FunctionResultMemberAccess_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- getObject().field";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<Operation>(setStmt.Value);
    }

    /// <summary>
    /// 测试函数调用结果的数组索引 - getArray()[0]
    /// </summary>
    [Fact]
    public void ParseDotExpr_FunctionResultArrayIndex_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- getArray()[0]";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<Operation>(setStmt.Value);
    }

    /// <summary>
    /// 测试链式方法调用 - obj.method1().method2().method3()
    /// </summary>
    [Fact]
    public void ParseDotExpr_ChainedMethodCalls_ParsesSuccessfully()
    {
        // Arrange
        var code = "result <- obj.method1().method2().method3()";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        var setStmt = Assert.IsType<SetStatement>(result[0]);
        Assert.IsType<Operation>(setStmt.Value);
    }

    #endregion
}
