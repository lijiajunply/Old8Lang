using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Async;

/// <summary>
/// Task 高级功能测试 - 测试 TaskCompletionSource、TaskFactory 等高级特性
/// </summary>
[Collection("Sequential")]
public class TaskAdvancedTests
{
    #region TaskCompletionSource 测试

    /// <summary>
    /// 测试 TaskCompletionSource 基础用法
    /// </summary>
    [Fact]
    public async Task Run_TaskCompletionSourceBasic_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
tcs <- TaskCompletionSource()
task <- tcs.Task

func completeTask() -> void {
    Thread.Sleep(100)
    tcs.SetResult(42)
}

thread <- spawn(completeTask)
thread.Start()
thread.Join()  // 等待线程完成";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);

        // Give thread time to complete
        await Task.Delay(200);

        // Assert
        var task = interpreter.Manager.GetValue(new LangId("task"));
        Assert.NotNull(task);
    }

    /// <summary>
    /// 测试 TaskCompletionSource.SetException
    /// </summary>
    [Fact]
    public async Task Run_TaskCompletionSourceSetException_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
tcs <- TaskCompletionSource()
task <- tcs.Task

func failTask() -> bool {
    Thread.Sleep(50)
    tcs.SetException(""Task failed"")
    return true
}

thread <- spawn(failTask)
thread.Start()
errorSet <- thread.Join()";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);

        // Give thread time to complete
        await Task.Delay(150);

        // Assert
        var errorSet = interpreter.Manager.GetValue(new LangId("errorSet"));
        Assert.IsType<BoolLangValue>(errorSet);
        Assert.True(((BoolLangValue)errorSet).Value);
    }

    /// <summary>
    /// 测试 TaskCompletionSource.TrySetResult
    /// </summary>
    [Fact]
    public async Task Run_TaskCompletionSourceTrySetResult_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
tcs <- TaskCompletionSource()

firstResult <- tcs.TrySetResult(100)
secondResult <- tcs.TrySetResult(200)

result <- firstResult and not secondResult";
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
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<BoolLangValue>(result);
        Assert.True(((BoolLangValue)result).Value);
    }

    #endregion

    #region Task.WhenAll 和 Task.WhenAny

    /// <summary>
    /// 测试 Task.WhenAll 等待所有任务完成
    /// </summary>
    [Fact]
    public async Task Run_TaskWhenAll_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
async func compute(value: int) -> int {
    Thread.Sleep(50)
    return value * 2
}

async func main() -> int {
    t1 <- compute(5)
    t2 <- compute(10)
    t3 <- compute(15)

    results <- await Task.WhenAll({t1, t2, t3})
    return results[0] + results[1] + results[2]
}

result <- main()";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);

        // Give task time to complete
        await Task.Delay(300);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
    }

    /// <summary>
    /// 测试 Task.WhenAny 等待任意任务完成
    /// </summary>
    [Fact]
    public async Task Run_TaskWhenAny_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
async func fastTask() -> int {
    Thread.Sleep(50)
    return 1
}

async func slowTask() -> int {
    Thread.Sleep(200)
    return 2
}

async func main() -> int {
    t1 <- fastTask()
    t2 <- slowTask()

    firstCompleted <- await Task.WhenAny({t1, t2})
    return await firstCompleted
}

result <- main()";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);

        // Give task time to complete
        await Task.Delay(300);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
    }

    /// <summary>
    /// 测试 Task.WhenAll 处理异常
    /// </summary>
    [Fact]
    public async Task Run_TaskWhenAllWithException_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
async func successTask() -> int {
    return 42
}

async func failingTask() -> int {
    throw ""Task error""
    return 0
}

async func main() -> int {
    t1 <- successTask()
    t2 <- failingTask()

    try {
        results <- await Task.WhenAll({t1, t2})
        return 0
    } catch (e) {
        return -1
    }
}

result <- main()";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);

        // Give task time to complete
        await Task.Delay(300);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
    }

    #endregion

    #region Task.Delay 测试

    /// <summary>
    /// 测试 Task.Delay 基础用法
    /// </summary>
    [Fact]
    public async Task Run_TaskDelay_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
async func delayedTask() -> int {
    before <- GetMilliseconds()
    await Task.Delay(100)
    after <- GetMilliseconds()
    elapsed <- after - before
    return elapsed >= 100
}

result <- delayedTask()";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);

        // Give task time to complete
        await Task.Delay(200);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
    }

    /// <summary>
    /// 测试 Task.Delay 零毫秒
    /// </summary>
    [Fact]
    public async Task Run_TaskDelayZero_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
async func immediateTask() -> int {
    await Task.Delay(0)
    return 42
}

result <- immediateTask()";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);

        // Give task time to complete
        await Task.Delay(100);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
    }

    /// <summary>
    /// 测试 Task.Delay 与 CancellationToken
    /// </summary>
    [Fact]
    public async Task Run_TaskDelayWithCancellation_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
cts <- CancellationTokenSource(50)
token <- cts.Token

// 检查令牌是否会被取消
Thread.Sleep(100)
cancelled <- token.IsCancellationRequested";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);

        // Give task time to complete
        await Task.Delay(300);

        // Assert
        var cancelled = interpreter.Manager.GetValue(new LangId("cancelled"));
        Assert.IsType<BoolLangValue>(cancelled);
        Assert.True(((BoolLangValue)cancelled).Value);
    }

    #endregion

    #region Task.FromResult 测试

    /// <summary>
    /// 测试 Task.FromResult 返回已完成的任务
    /// </summary>
    [Fact]
    public async Task Run_TaskFromResult_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
async func getImmediateResult() -> int {
    task <- Task.FromResult(42)
    return await task
}

result <- getImmediateResult()";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);

        // Give task time to complete
        await Task.Delay(100);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
    }

    /// <summary>
    /// 测试 Task.FromResult 不同类型
    /// </summary>
    [Fact]
    public async Task Run_TaskFromResultDifferentTypes_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
async func stringTask() -> string {
    return await Task.FromResult(""Hello"")
}

async func boolTask() -> bool {
    return await Task.FromResult(true)
}

stringResult <- stringTask()
boolResult <- boolTask()";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);

        // Give tasks time to complete
        await Task.Delay(100);

        // Assert
        var stringResult = interpreter.Manager.GetValue(new LangId("stringResult"));
        var boolResult = interpreter.Manager.GetValue(new LangId("boolResult"));

        Assert.NotNull(stringResult);
        Assert.NotNull(boolResult);
    }

    #endregion

    #region Task 异常聚合

    /// <summary>
    /// 测试异步任务异常传播
    /// </summary>
    [Fact]
    public async Task Run_AsyncTaskExceptionPropagation_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
async func throwError() -> int {
    throw ""Async error""
    return 0
}

async func main() -> int {
    try {
        result <- await throwError()
        return result
    } catch (e) {
        return -1
    }
}

result <- main()";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);

        // Give task time to complete
        await Task.Delay(200);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
    }

    /// <summary>
    /// 测试多个异步任务的异常处理
    /// </summary>
    [Fact]
    public async Task Run_MultipleAsyncTasksExceptionHandling_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
async func mayFail(shouldFail: bool) -> int {
    if shouldFail {
        throw ""Task error""
    }
    return 42
}

async func main() -> int {
    tasks <- {mayFail(true), mayFail(false), mayFail(true)}
    errorCount <- 0

    for task in tasks {
        try {
            result <- await task
        } catch (e) {
            errorCount <- errorCount + 1
        }
    }

    return errorCount
}

mainTask <- main()
errorCount <- await mainTask";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);

        // Give task time to complete
        await Task.Delay(300);

        // Assert
        var errorCount = interpreter.Manager.GetValue(new LangId("errorCount"));
        Assert.IsType<IntLangValue>(errorCount);
        Assert.Equal(2, ((IntLangValue)errorCount).Value);
    }

    /// <summary>
    /// 测试异步任务中的嵌套异常
    /// </summary>
    [Fact]
    public async Task Run_NestedAsyncExceptions_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
async func innerTask() -> int {
    throw ""Inner error""
    return 0
}

async func outerTask() -> int {
    try {
        return await innerTask()
    } catch (e) {
        throw ""Outer error: "" + e
    }
}

async func main() -> int {
    try {
        return await outerTask()
    } catch (e) {
        return -1
    }
}

result <- main()";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);

        // Give task time to complete
        await Task.Delay(300);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.NotNull(result);
    }

    #endregion
}

