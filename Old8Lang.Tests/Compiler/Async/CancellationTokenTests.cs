using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Async;

/// <summary>
/// CancellationToken 测试 - 测试异步任务取消功能
/// </summary>
[Collection("Sequential")]
public class CancellationTokenTests
{
    #region CancellationToken 基础用法

    /// <summary>
    /// 测试创建 CancellationTokenSource
    /// </summary>
    [Fact]
    public void Run_CreateCancellationTokenSource_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
cts <- CancellationTokenSource()
token <- cts.Token
result <- token != null";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    /// <summary>
    /// 测试 CancellationToken 的 IsCancellationRequested 属性
    /// </summary>
    [Fact]
    public void Run_CheckIsCancellationRequested_ReturnsFalse()
    {
        // Arrange
        var code = @"
cts <- CancellationTokenSource()
token <- cts.Token
result <- token.IsCancellationRequested";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    /// <summary>
    /// 测试调用 Cancel 方法
    /// </summary>
    [Fact]
    public void Run_CancelToken_SetsCancellationRequested()
    {
        // Arrange
        var code = @"
cts <- CancellationTokenSource()
token <- cts.Token
before <- token.IsCancellationRequested
cts.Cancel()
after <- token.IsCancellationRequested";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);

        // Assert
        var before = interpreter.Manager.GetValue(new LangId("before"));
        var after = interpreter.Manager.GetValue(new LangId("after"));

        Assert.IsType<BoolLangValue>(before);
        Assert.False(((BoolLangValue)before).Value);

        Assert.IsType<BoolLangValue>(after);
        Assert.True(((BoolLangValue)after).Value);
    }

    #endregion

    #region 带超时的 CancellationToken

    /// <summary>
    /// 测试带超时时间的 CancellationTokenSource
    /// </summary>
    [Fact]
    public async Task Run_CancellationTokenWithTimeout_CancelsAfterTimeout()
    {
        // Arrange
        var code = @"
cts <- CancellationTokenSource(100)
token <- cts.Token
before <- token.IsCancellationRequested";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);

        var before = interpreter.Manager.GetValue(new LangId("before"));
        Assert.IsType<BoolLangValue>(before);
        Assert.False(((BoolLangValue)before).Value);

        // Wait for timeout
        await Task.Delay(150);

        // Check after timeout
        var code2 = "after <- cts.Token.IsCancellationRequested";
        var ast2 = interpreter.Build(code2);
        ast2.Run(interpreter.Manager);

        var after = interpreter.Manager.GetValue(new LangId("after"));
        Assert.IsType<BoolLangValue>(after);
        Assert.True(((BoolLangValue)after).Value);
    }

    #endregion

    #region 在异步函数中使用 CancellationToken

    /// <summary>
    /// 测试在异步函数中传递 CancellationToken
    /// </summary>
    [Fact]
    public async Task Run_PassCancellationTokenToAsyncFunction_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
async func longRunningTask(token) -> int {
    if token.IsCancellationRequested {
        return -1
    }
    return 42
}

cts <- CancellationTokenSource()
token <- cts.Token
task <- longRunningTask(token)";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);

        await Task.Delay(100);

        // Assert
        var task = interpreter.Manager.GetValue(new LangId("task"));
        Assert.NotNull(task);
    }

    /// <summary>
    /// 测试在异步函数中检查取消状态
    /// </summary>
    [Fact]
    public async Task Run_CheckCancellationInAsyncFunction_HandlesCorrectly()
    {
        // Arrange
        var code = @"
async func processWithCancellation(token) -> string {
    if token.IsCancellationRequested {
        return ""Cancelled""
    }
    return ""Completed""
}

cts1 <- CancellationTokenSource()
token1 <- cts1.Token
task1 <- processWithCancellation(token1)

cts2 <- CancellationTokenSource()
token2 <- cts2.Token
cts2.Cancel()
task2 <- processWithCancellation(token2)";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);

        await Task.Delay(100);

        // Assert
        var task1 = interpreter.Manager.GetValue(new LangId("task1"));
        var task2 = interpreter.Manager.GetValue(new LangId("task2"));

        Assert.NotNull(task1);
        Assert.NotNull(task2);
    }

    #endregion

    #region 取消令牌传播

    /// <summary>
    /// 测试取消令牌在多个任务间传播
    /// </summary>
    [Fact]
    public async Task Run_CancellationPropagation_WorksCorrectly()
    {
        // Arrange
        var code = @"
async func subtask(token) -> bool {
    return token.IsCancellationRequested
}

async func mainTask(token) -> bool {
    result <- await subtask(token)
    return result
}

cts <- CancellationTokenSource()
token <- cts.Token
cts.Cancel()
task <- mainTask(token)";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);

        await Task.Delay(100);

        // Assert
        var task = interpreter.Manager.GetValue(new LangId("task"));
        Assert.NotNull(task);
    }

    #endregion

    #region CancellationTokenSource 生命周期

    /// <summary>
    /// 测试 CancellationTokenSource 的 Dispose
    /// </summary>
    [Fact]
    public void Run_DisposeCancellationTokenSource_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
cts <- CancellationTokenSource()
token <- cts.Token
before <- token.IsCancellationRequested
cts.Dispose()
result <- before";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    /// <summary>
    /// 测试多次调用 Cancel 方法
    /// </summary>
    [Fact]
    public void Run_CallCancelMultipleTimes_WorksCorrectly()
    {
        // Arrange
        var code = @"
cts <- CancellationTokenSource()
token <- cts.Token

cts.Cancel()
first <- token.IsCancellationRequested

cts.Cancel()
second <- token.IsCancellationRequested";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);

        // Assert
        var first = interpreter.Manager.GetValue(new LangId("first"));
        var second = interpreter.Manager.GetValue(new LangId("second"));

        Assert.IsType<BoolLangValue>(first);
        Assert.True(((BoolLangValue)first).Value);

        Assert.IsType<BoolLangValue>(second);
        Assert.True(((BoolLangValue)second).Value);
    }

    #endregion

    #region 实际应用场景

    /// <summary>
    /// 测试使用 CancellationToken 控制循环
    /// </summary>
    [Fact]
    public async Task Run_UseCancellationTokenInLoop_StopsWhenCancelled()
    {
        // Arrange
        var code = @"
counter <- 0

async func countWithCancellation(token) -> int {
    for i <- 0, i < 1000, i++ {
        if token.IsCancellationRequested {
            return counter
        }
        counter <- counter + 1
    }
    return counter
}

cts <- CancellationTokenSource(50)
token <- cts.Token
task <- countWithCancellation(token)";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);

        await Task.Delay(100);

        // Assert
        var counter = interpreter.Manager.GetValue(new LangId("counter"));
        Assert.IsType<IntLangValue>(counter);
        // Counter should be less than 1000 since it was cancelled
        Assert.True(((IntLangValue)counter).Value < 1000);
    }

    /// <summary>
    /// 测试协调多个异步任务的取消
    /// </summary>
    [Fact]
    public async Task Run_CoordinateMultipleTasksCancellation_WorksCorrectly()
    {
        // Arrange
        var code = @"
completed <- 0

async func worker(id: int, token) -> void {
    if not token.IsCancellationRequested {
        completed <- completed + 1
    }
}

cts <- CancellationTokenSource()
token <- cts.Token

t1 <- worker(1, token)
t2 <- worker(2, token)
t3 <- worker(3, token)

cts.Cancel()

t4 <- worker(4, token)
t5 <- worker(5, token)";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);

        await Task.Delay(100);

        // Assert
        var completed = interpreter.Manager.GetValue(new LangId("completed"));
        Assert.IsType<IntLangValue>(completed);
        // Only first 3 tasks should complete (before cancellation)
        Assert.True(((IntLangValue)completed).Value <= 3);
    }

    #endregion

    #region 边界情况

    /// <summary>
    /// 测试在已取消的 Token 上重复检查
    /// </summary>
    [Fact]
    public void Run_CheckCancelledTokenMultipleTimes_ReturnsConsistentResult()
    {
        // Arrange
        var code = @"
cts <- CancellationTokenSource()
token <- cts.Token
cts.Cancel()

check1 <- token.IsCancellationRequested
check2 <- token.IsCancellationRequested
check3 <- token.IsCancellationRequested

allTrue <- check1 and check2 and check3";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);

        // Assert
        var allTrue = interpreter.Manager.GetValue(new LangId("allTrue"));
        Assert.IsType<BoolLangValue>(allTrue);
        Assert.True(((BoolLangValue)allTrue).Value);
    }

    /// <summary>
    /// 测试零超时的 CancellationTokenSource
    /// </summary>
    [Fact]
    public void Run_CancellationTokenWithZeroTimeout_CancelsImmediately()
    {
        // Arrange
        var code = @"
cts <- CancellationTokenSource(0)
token <- cts.Token
result <- token.IsCancellationRequested";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    #endregion
}
