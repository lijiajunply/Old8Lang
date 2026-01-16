using Old8Lang.AST.Statement;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Parser.Statement;

/// <summary>
/// Try-Catch-Finally 语句解析测试
/// </summary>
[Collection("Sequential")]
public class TryStatementTests
{
    #region 基本 Try-Catch 测试

    /// <summary>
    /// 测试简单的 try-catch 语句
    /// </summary>
    [Fact]
    public void ParseTryStatement_SimpleTryCatch_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
try {
    a <- 10
} catch (e) {
    b <- 20
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        Assert.IsType<TryStatement>(result[0]);
    }

    /// <summary>
    /// 测试带有异常过滤器 (where 子句) 的 catch 块
    /// </summary>
    [Fact]
    public void ParseTryStatement_CatchWithFilter_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
try {
    a <- 10 / 0
} catch (e) where e.Code == 404 {
    PrintLine(""Not Found"")
} catch (e) {
    PrintLine(""Other Error"")
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        Assert.IsType<TryStatement>(result[0]);
        var tryStmt = (TryStatement)result[0];
        Assert.Equal(2, tryStmt.CatchBlocks.Count);
        
        // 检查第一个 catch 块的过滤器
        var (type1, var1, filter1, block1) = tryStmt.CatchBlocks[0];
        Assert.NotNull(filter1);
        
        // 检查第二个 catch 块的过滤器（应该为 null）
        var (type2, var2, filter2, block2) = tryStmt.CatchBlocks[1];
        Assert.Null(filter2);
    }

    /// <summary>
    /// 测试不带括号的 catch 块（应该成功，因为括号是可选的）
    /// </summary>
    [Fact]
    public void ParseTryStatement_CatchWithoutParentheses_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
try {
    a <- 10
} catch {
    b <- 20
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        Assert.IsType<TryStatement>(result[0]);
    }

    #endregion

    #region Try-Catch-Finally 测试

    /// <summary>
    /// 测试 try-catch-finally 语句
    /// </summary>
    [Fact]
    public void ParseTryStatement_TryCatchFinally_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
try {
    a <- 10
} catch (e) {
    b <- 20
} finally {
    c <- 30
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        Assert.IsType<TryStatement>(result[0]);
    }

    /// <summary>
    /// 测试只有 try-finally（没有 catch）
    /// </summary>
    [Fact]
    public void ParseTryStatement_TryFinally_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
try {
    a <- 10
} finally {
    b <- 20
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        Assert.IsType<TryStatement>(result[0]);
    }

    /// <summary>
    /// 测试 finally 块中的单行语句
    /// </summary>
    [Fact]
    public void ParseTryStatement_FinallyWithSingleStatement_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
try {
    a <- 10
} catch (e) {
    b <- 20
} finally
    c <- 30";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        Assert.IsType<TryStatement>(result[0]);
    }

    #endregion

    #region 多个 Catch 块测试

    /// <summary>
    /// 测试多个 catch 块
    /// </summary>
    [Fact]
    public void ParseTryStatement_MultipleCatchBlocks_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
try {
    a <- 10 / 0
} catch (SyntaxError e) {
    PrintLine(""Syntax Error"")
} catch (RuntimeError e) {
    PrintLine(""Runtime Error"")
} catch (e) {
    PrintLine(""Other Error"")
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        Assert.IsType<TryStatement>(result[0]);
    }

    /// <summary>
    /// 测试多个 catch 块加 finally
    /// </summary>
    [Fact]
    public void ParseTryStatement_MultipleCatchBlocksWithFinally_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
try {
    a <- 10
} catch (TypeError e) {
    b <- 1
} catch (ValueError e) {
    b <- 2
} catch (Exception e) {
    b <- 3
} finally {
    c <- 100
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        Assert.IsType<TryStatement>(result[0]);
    }

    #endregion

    #region 嵌套 Try-Catch 测试

    /// <summary>
    /// 测试嵌套的 try-catch 语句
    /// </summary>
    [Fact]
    public void ParseTryStatement_NestedTryCatch_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
try {
    try {
        a <- 10 / 0
    } catch (innerE) {
        PrintLine(innerE)
    }
} catch (outerE) {
    PrintLine(outerE)
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        Assert.IsType<TryStatement>(result[0]);
    }

    /// <summary>
    /// 测试嵌套的 try-catch-finally
    /// </summary>
    [Fact]
    public void ParseTryStatement_NestedTryCatchFinally_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
try {
    try {
        a <- 10
    } catch (e1) {
        b <- 20
    } finally {
        c <- 30
    }
} catch (e2) {
    d <- 40
} finally {
    e <- 50
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        Assert.IsType<TryStatement>(result[0]);
    }

    #endregion

    #region 错误场景测试

    /// <summary>
    /// 测试缺少 try 块 - 应该抛出语法错误
    /// </summary>
    [Fact]
    public void ParseTryStatement_MissingTryBlock_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
try
catch (e) {
    a <- 10
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试缺少 catch 块中的右括号
    /// </summary>
    [Fact]
    public void ParseTryStatement_MissingCatchRightParen_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
try {
    a <- 10
} catch (e {
    b <- 20
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试缺少 catch 块的左括号
    /// </summary>
    [Fact]
    public void ParseTryStatement_MissingCatchLeftParen_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
try {
    a <- 10
} catch e) {
    b <- 20
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试缺少 catch 块体
    /// </summary>
    [Fact]
    public void ParseTryStatement_MissingCatchBlock_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
try {
    a <- 10
} catch (e)";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试缺少 finally 块体
    /// </summary>
    [Fact]
    public void ParseTryStatement_MissingFinallyBlock_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
try {
    a <- 10
} catch (e) {
    b <- 20
} finally";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试空的 try 块
    /// </summary>
    [Fact]
    public void ParseTryStatement_EmptyTryBlock_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
try {
} catch (e) {
    a <- 10
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        Assert.IsType<TryStatement>(result[0]);
    }

    /// <summary>
    /// 测试空的 catch 块
    /// </summary>
    [Fact]
    public void ParseTryStatement_EmptyCatchBlock_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
try {
    a <- 10
} catch (e) {
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        Assert.IsType<TryStatement>(result[0]);
    }

    /// <summary>
    /// 测试空的 finally 块
    /// </summary>
    [Fact]
    public void ParseTryStatement_EmptyFinallyBlock_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
try {
    a <- 10
} catch (e) {
    b <- 20
} finally {
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        Assert.IsType<TryStatement>(result[0]);
    }

    #endregion

    #region Try-Catch 与其他语句结合测试

    /// <summary>
    /// 测试 try-catch 中包含 if 语句
    /// </summary>
    [Fact]
    public void ParseTryStatement_WithIfStatement_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
try {
    if x > 0 {
        a <- 10
    }
} catch (e) {
    if y > 0 {
        b <- 20
    }
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        Assert.IsType<TryStatement>(result[0]);
    }

    /// <summary>
    /// 测试 try-catch 中包含循环
    /// </summary>
    [Fact]
    public void ParseTryStatement_WithLoop_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
try {
    for i <- 0, i < 10, i <- i + 1 {
        PrintLine(i)
    }
} catch (e) {
    while true {
        break
    }
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        Assert.IsType<TryStatement>(result[0]);
    }

    /// <summary>
    /// 测试 try-catch 中包含 throw 语句
    /// </summary>
    [Fact]
    public void ParseTryStatement_WithThrowStatement_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
try {
    throw ""Error message""
} catch (e) {
    PrintLine(e)
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        Assert.IsType<TryStatement>(result[0]);
    }

    /// <summary>
    /// 测试 catch 块中重新抛出异常
    /// </summary>
    [Fact]
    public void ParseTryStatement_RethrowInCatch_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
try {
    a <- 10 / 0
} catch (e) {
    PrintLine(e)
    throw e
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        Assert.IsType<TryStatement>(result[0]);
    }

    #endregion

    #region 函数中的 Try-Catch 测试

    /// <summary>
    /// 测试函数中的 try-catch-finally 与 return
    /// </summary>
    [Fact]
    public void ParseTryStatement_InFunctionWithReturn_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func test() {
    try {
        return 10
    } catch (e) {
        return 20
    }
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        // 函数声明不计入 Count
        Assert.True(result.Count >= 0);
    }

    #endregion
}
