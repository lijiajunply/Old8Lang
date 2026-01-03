using Old8Lang.AST.Statement;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Parser.Statement;

/// <summary>
/// Using 语句解析测试
/// </summary>
[Collection("Sequential")]
public class UsingStatementTests
{
    #region 基本 Using 测试

    /// <summary>
    /// 测试带变量声明的 using 语句
    /// </summary>
    [Fact]
    public void ParseUsingStatement_WithVariableDeclaration_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
using mutex <- MutexCreate() {
    MutexLock(mutex)
    MutexUnlock(mutex)
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        Assert.IsType<UsingStatement>(result[0]);
        var usingStmt = (UsingStatement)result[0];
        Assert.Equal("mutex", usingStmt.VariableName);
        Assert.NotNull(usingStmt.ResourceExpression);
        Assert.NotNull(usingStmt.BlockStatement);
    }

    /// <summary>
    /// 测试不带变量声明的 using 语句
    /// </summary>
    [Fact]
    public void ParseUsingStatement_WithoutVariableDeclaration_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
ch <- ChannelCreate()
using ch {
    ChannelSend(ch, 100)
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.IsType<UsingStatement>(result[1]);
        var usingStmt = (UsingStatement)result[1];
        Assert.Null(usingStmt.VariableName);
        Assert.NotNull(usingStmt.ResourceExpression);
        Assert.NotNull(usingStmt.BlockStatement);
    }

    /// <summary>
    /// 测试单行 using 语句
    /// </summary>
    [Fact]
    public void ParseUsingStatement_SingleStatement_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
using mutex <- MutexCreate()
    MutexLock(mutex)";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        Assert.IsType<UsingStatement>(result[0]);
    }

    #endregion

    #region 复杂资源表达式测试

    /// <summary>
    /// 测试使用函数调用作为资源表达式
    /// </summary>
    [Fact]
    public void ParseUsingStatement_FunctionCallExpression_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
using sem <- SemaphoreCreate(1, 5) {
    SemaphoreAcquire(sem)
    SemaphoreRelease(sem)
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        Assert.IsType<UsingStatement>(result[0]);
    }

    /// <summary>
    /// 测试使用复杂表达式作为资源
    /// </summary>
    [Fact]
    public void ParseUsingStatement_ComplexExpression_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
capacity <- 10
using ch <- (capacity > 0 ? ChannelCreateBounded(capacity) : ChannelCreate()) {
    ChannelSend(ch, 123)
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.IsType<UsingStatement>(result[1]);
    }

    #endregion

    #region 嵌套 Using 测试

    /// <summary>
    /// 测试嵌套的 using 语句
    /// </summary>
    [Fact]
    public void ParseUsingStatement_Nested_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
using mutex1 <- MutexCreate() {
    using mutex2 <- MutexCreate() {
        MutexLock(mutex1)
        MutexLock(mutex2)
        MutexUnlock(mutex2)
        MutexUnlock(mutex1)
    }
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        Assert.IsType<UsingStatement>(result[0]);
    }

    /// <summary>
    /// 测试三层嵌套 using
    /// </summary>
    [Fact]
    public void ParseUsingStatement_TripleNested_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
using res1 <- CreateResource1() {
    using res2 <- CreateResource2() {
        using res3 <- CreateResource3() {
            ProcessResources(res1, res2, res3)
        }
    }
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        Assert.IsType<UsingStatement>(result[0]);
    }

    #endregion

    #region Using 与其他语句结合测试

    /// <summary>
    /// 测试 using 中包含 if 语句
    /// </summary>
    [Fact]
    public void ParseUsingStatement_WithIfStatement_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
using mutex <- MutexCreate() {
    if condition {
        MutexLock(mutex)
    } else {
        MutexUnlock(mutex)
    }
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        Assert.IsType<UsingStatement>(result[0]);
    }

    /// <summary>
    /// 测试 using 中包含循环
    /// </summary>
    [Fact]
    public void ParseUsingStatement_WithLoop_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
using ch <- ChannelCreate() {
    for i in [1~10] {
        ChannelSend(ch, i)
    }
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        Assert.IsType<UsingStatement>(result[0]);
    }

    /// <summary>
    /// 测试 using 中包含 try-catch
    /// </summary>
    [Fact]
    public void ParseUsingStatement_WithTryCatch_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
using mutex <- MutexCreate() {
    try {
        MutexLock(mutex)
        DoSomething()
    } catch (e) {
        PrintLine(e)
    } finally {
        MutexUnlock(mutex)
    }
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        Assert.IsType<UsingStatement>(result[0]);
    }

    #endregion

    #region 函数中的 Using 测试

    /// <summary>
    /// 测试函数中的 using 语句
    /// </summary>
    [Fact]
    public void ParseUsingStatement_InFunction_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
func processWithMutex() {
    using mutex <- MutexCreate() {
        MutexLock(mutex)
        // Critical section
        MutexUnlock(mutex)
    }
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count >= 0);
    }

    #endregion

    #region 错误场景测试

    /// <summary>
    /// 测试缺少资源表达式 - 应该抛出语法错误
    /// </summary>
    [Fact]
    public void ParseUsingStatement_MissingResourceExpression_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
using {
    DoSomething()
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试缺少 using 块 - 应该抛出语法错误
    /// </summary>
    [Fact]
    public void ParseUsingStatement_MissingBlock_ThrowsSyntaxError()
    {
        // Arrange
        var code = @"
using mutex <- MutexCreate()";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act & Assert
        Assert.ThrowsAny<SyntaxError>(() => parser.ParseProgram());
    }

    /// <summary>
    /// 测试空的 using 块
    /// </summary>
    [Fact]
    public void ParseUsingStatement_EmptyBlock_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
using mutex <- MutexCreate() {
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        Assert.IsType<UsingStatement>(result[0]);
    }

    #endregion

    #region 多个并发原语测试

    /// <summary>
    /// 测试使用 AtomicInt 的 using 语句
    /// </summary>
    [Fact]
    public void ParseUsingStatement_WithAtomicInt_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
using counter <- AtomicIntCreate(0) {
    AtomicIntIncrement(counter)
    AtomicIntDecrement(counter)
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        Assert.IsType<UsingStatement>(result[0]);
    }

    /// <summary>
    /// 测试使用 ReadWriteLock 的 using 语句
    /// </summary>
    [Fact]
    public void ParseUsingStatement_WithReadWriteLock_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
using rwLock <- ReadWriteLockCreate() {
    ReadLockAcquire(rwLock)
    ReadLockRelease(rwLock)
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        Assert.IsType<UsingStatement>(result[0]);
    }

    /// <summary>
    /// 测试使用 CountDownLatch 的 using 语句
    /// </summary>
    [Fact]
    public void ParseUsingStatement_WithCountDownLatch_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
using latch <- CountDownLatchCreate(3) {
    CountDownLatchCountDown(latch)
    CountDownLatchWait(latch)
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        Assert.IsType<UsingStatement>(result[0]);
    }

    /// <summary>
    /// 测试使用 CyclicBarrier 的 using 语句
    /// </summary>
    [Fact]
    public void ParseUsingStatement_WithCyclicBarrier_ParsesSuccessfully()
    {
        // Arrange
        var code = @"
using barrier <- CyclicBarrierCreate(5) {
    CyclicBarrierAwait(barrier)
}";
        var tokens = LangInterpreter.Tokenize(code);
        var parser = new LangParser.LangParser(tokens, code);

        // Act
        var result = parser.ParseProgram();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        Assert.IsType<UsingStatement>(result[0]);
    }

    #endregion
}
