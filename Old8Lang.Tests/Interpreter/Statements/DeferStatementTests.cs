using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Interpreter.Statements;

/// <summary>
/// Defer 语句解释器执行测试
/// </summary>
[Collection("Sequential")]
public class DeferStatementTests
{
    #region 基本功能测试

    /// <summary>
    /// 测试基本 defer 语句执行顺序
    /// </summary>
    [Fact]
    public void DeferStatement_Basic_ExecutesAfterFunctionBody()
    {
        // Arrange
        var code = @"
order <- """"
func test() -> void {
    order <- order + ""1""
    defer order <- order + ""3""
    order <- order + ""2""
}
test()
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("order"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("123", ((StringLangValue)result).Value);
    }

    /// <summary>
    /// 测试多个 defer 按 LIFO 顺序执行
    /// </summary>
    [Fact]
    public void DeferStatement_Multiple_ExecutesInLIFOOrder()
    {
        // Arrange
        var code = @"
order <- """"
func test() -> void {
    defer order <- order + ""1""
    defer order <- order + ""2""
    defer order <- order + ""3""
}
test()
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("order"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("321", ((StringLangValue)result).Value);
    }

    /// <summary>
    /// 测试 defer 访问局部变量
    /// </summary>
    [Fact]
    public void DeferStatement_AccessLocalVariable_AccessesCorrectValue()
    {
        // Arrange
        var code = @"
result <- """"
func test() -> void {
    x <- ""initial""
    defer result <- x
    x <- ""modified""
}
test()
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        // defer 看到修改后的值
        Assert.Equal("modified", ((StringLangValue)result).Value);
    }

    #endregion

    #region Defer 代码块测试

    /// <summary>
    /// 测试 defer 代码块
    /// </summary>
    [Fact]
    public void DeferStatement_Block_ExecutesAllStatements()
    {
        // Arrange
        var code = @"
count <- 0
func test() -> void {
    defer {
        count <- count + 1
        count <- count + 1
        count <- count + 1
    }
}
test()
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("count"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(3, ((IntLangValue)result).Value);
    }

    /// <summary>
    /// 测试 defer 代码块包含条件语句
    /// </summary>
    [Fact]
    public void DeferStatement_BlockWithIf_ExecutesConditionally()
    {
        // Arrange
        var code = @"
result <- """"
func test(x:int) -> void {
    defer {
        if x > 5 {
            result <- ""large""
        } else {
            result <- ""small""
        }
    }
}
test(10)
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        Assert.Equal("large", ((StringLangValue)result).Value);
    }

    #endregion

    #region Defer 与 Return 交互测试

    /// <summary>
    /// 测试 defer 在 return 前执行
    /// </summary>
    [Fact]
    public void DeferStatement_WithReturn_ExecutesBeforeReturn()
    {
        // Arrange
        var code = @"
executed <- false
func test() -> int {
    defer executed <- true
    return 42
}
result <- test()
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var executed = interpreter.Manager.GetValue(new LangId("executed"));
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(executed);
        Assert.IsType<BoolLangValue>(executed);
        Assert.True(((BoolLangValue)executed).Value);
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(42, ((IntLangValue)result).Value);
    }

    /// <summary>
    /// 测试 defer 不影响返回值
    /// </summary>
    [Fact]
    public void DeferStatement_DoesNotAffectReturnValue()
    {
        // Arrange
        var code = @"
func test() -> int {
    value <- 100
    defer value <- 999
    return value
}
result <- test()
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        // 返回值是 100，而不是 999
        Assert.Equal(100, ((IntLangValue)result).Value);
    }

    /// <summary>
    /// 测试多个 return 路径都执行 defer
    /// </summary>
    [Fact]
    public void DeferStatement_MultipleReturns_ExecutesForAllPaths()
    {
        // Arrange
        var code = @"
count <- 0
func test(flag:bool) -> int {
    defer count <- count + 1
    if flag {
        return 1
    } else {
        return 2
    }
}
r1 <- test(true)
r2 <- test(false)
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var count = interpreter.Manager.GetValue(new LangId("count"));
        Assert.NotNull(count);
        Assert.IsType<IntLangValue>(count);
        Assert.Equal(2, ((IntLangValue)count).Value);
    }

    #endregion

    #region Defer 与函数调用测试

    /// <summary>
    /// 测试 defer 调用函数
    /// </summary>
    [Fact]
    public void DeferStatement_FunctionCall_CallsSuccessfully()
    {
        // Arrange
        var code = @"
result <- 0
func cleanup(value:int) -> void {
    result <- value
}

func test() -> void {
    defer cleanup(42)
}
test()
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(42, ((IntLangValue)result).Value);
    }

    /// <summary>
    /// 测试 defer 调用带多个参数的函数
    /// </summary>
    [Fact]
    public void DeferStatement_FunctionCallWithMultipleParams_CallsSuccessfully()
    {
        // Arrange
        var code = @"
result <- 0
func add(a:int, b:int, c:int) -> void {
    result <- a + b + c
}

func test() -> void {
    defer add(10, 20, 30)
}
test()
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(60, ((IntLangValue)result).Value);
    }

    #endregion

    #region 资源管理测试

    /// <summary>
    /// 测试使用 defer 管理 Mutex 资源
    /// </summary>
    [Fact]
    public void DeferStatement_MutexCleanup_DisposesCorrectly()
    {
        // Arrange
        var code = @"
result <- 0
func test() -> void {
    mutex <- MutexCreate()
    defer MutexDispose(mutex)

    MutexLock(mutex)
    result <- 42
    MutexUnlock(mutex)
}
test()
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(42, ((IntLangValue)result).Value);
    }

    /// <summary>
    /// 测试使用 defer 管理 Channel 资源
    /// </summary>
    [Fact]
    public void DeferStatement_ChannelCleanup_DisposesCorrectly()
    {
        // Arrange
        var code = @"
result <- 0
func test() -> void {
    ch <- ChannelCreate()
    defer ChannelDispose(ch)

    ChannelSend(ch, 100)
    result <- ChannelReceive(ch)
}
test()
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(100, ((IntLangValue)result).Value);
    }

    /// <summary>
    /// 测试多个资源的 defer 清理
    /// </summary>
    [Fact]
    public void DeferStatement_MultipleResources_DisposesInReverseOrder()
    {
        // Arrange
        var code = @"
order <- """"
func test() -> void {
    mutex <- MutexCreate()
    defer {
        MutexDispose(mutex)
        order <- order + ""M""
    }

    ch <- ChannelCreate()
    defer {
        ChannelDispose(ch)
        order <- order + ""C""
    }

    sem <- SemaphoreCreate(1, 5)
    defer {
        SemaphoreDispose(sem)
        order <- order + ""S""
    }
}
test()
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("order"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        // LIFO: Semaphore -> Channel -> Mutex
        Assert.Equal("SCM", ((StringLangValue)result).Value);
    }

    #endregion

    #region Defer 与循环测试

    /// <summary>
    /// 测试循环中的 defer（每次循环都会注册 defer）
    /// </summary>
    [Fact]
    public void DeferStatement_InLoop_RegistersMultipleTimes()
    {
        // Arrange
        var code = @"
count <- 0
func test() -> void {
    for i <- 0, i < 3, i++ {
        defer count <- count + 1
    }
}
test()
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("count"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        // 循环3次，每次注册1个defer，函数结束时执行3次
        Assert.Equal(3, ((IntLangValue)result).Value);
    }

    #endregion

    #region Defer 与 Using 结合测试

    /// <summary>
    /// 测试 defer 与 using 一起使用
    /// </summary>
    [Fact]
    public void DeferStatement_WithUsing_BothExecute()
    {
        // Arrange
        var code = @"
order <- """"
func test() -> void {
    defer order <- order + ""D""

    using mutex <- MutexCreate() {
        MutexLock(mutex)
        order <- order + ""U""
        MutexUnlock(mutex)
    }
}
test()
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("order"));
        Assert.NotNull(result);
        Assert.IsType<StringLangValue>(result);
        // using 先执行，defer 后执行
        Assert.Equal("UD", ((StringLangValue)result).Value);
    }

    #endregion

    #region 数值计算测试

    /// <summary>
    /// 测试 defer 进行数值计算
    /// </summary>
    [Fact]
    public void DeferStatement_NumericCalculation_CalculatesCorrectly()
    {
        // Arrange
        var code = @"
result <- 0
func test() -> void {
    x <- 10
    y <- 20
    defer result <- x + y
}
test()
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(30, ((IntLangValue)result).Value);
    }

    /// <summary>
    /// 测试 defer 访问修改后的变量值
    /// </summary>
    [Fact]
    public void DeferStatement_VariableModification_SeesLatestValue()
    {
        // Arrange
        var code = @"
result <- 0
func test() -> void {
    x <- 5
    defer result <- x * 2
    x <- 10
}
test()
";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        // defer 看到的是修改后的 x = 10，所以结果是 20
        Assert.Equal(20, ((IntLangValue)result).Value);
    }

    #endregion
}
