using Xunit;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Interpreter.Threading;

/// <summary>
/// 线程基础测试 - 测试 Thread 类基础功能
/// </summary>
[Collection("Sequential")]
public class ThreadTests
{
    #region Thread.Sleep 测试

    /// <summary>
    /// 测试 Thread.Sleep 基础用法
    /// </summary>
    [Fact]
    public void Run_ThreadSleep_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
before <- GetMilliseconds()
Thread.Sleep(100)
after <- GetMilliseconds()
elapsed <- after - before
result <- elapsed >= 100";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<BoolLangValue>(result);
        Assert.True(((BoolLangValue)result).Value);
    }

    /// <summary>
    /// 测试 Thread.Sleep 零毫秒
    /// </summary>
    [Fact]
    public void Run_ThreadSleepZero_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
Thread.Sleep(0)
result <- 42";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(42, ((IntLangValue)result).Value);
    }

    #endregion

    #region Thread.CurrentThread 测试

    /// <summary>
    /// 测试获取当前线程
    /// </summary>
    [Fact]
    public void Run_GetCurrentThread_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
currentThread <- Thread.CurrentThread()
result <- currentThread != null";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<BoolLangValue>(result);
        Assert.True(((BoolLangValue)result).Value);
    }

    /// <summary>
    /// 测试当前线程的 ManagedThreadId
    /// </summary>
    [Fact]
    public void Run_CurrentThreadManagedThreadId_ReturnsPositiveNumber()
    {
        // Arrange
        var code = @"
currentThread <- Thread.CurrentThread()
threadId <- currentThread.ManagedThreadId
result <- threadId > 0";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<BoolLangValue>(result);
        Assert.True(((BoolLangValue)result).Value);
    }

    #endregion

    #region 线程创建和启动

    /// <summary>
    /// 测试创建新线程
    /// </summary>
    [Fact]
    public async Task Run_CreateNewThread_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
counter <- Lock(0)

func threadFunc() -> void {
    val <- counter.Value
    counter.Set(val + 1)
}

thread <- Spawn(threadFunc)
thread.Start()";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Give thread time to execute
        await Task.Delay(100);

        // Assert
        var counter = interpreter.Manager.GetValue(new LangId("counter"));
        Assert.IsType<LockedVariableLangValue>(counter);
        var counterValue = ((LockedVariableLangValue)counter).GetLockedValue();
        Assert.IsType<IntLangValue>(counterValue);
        Assert.Equal(1, ((IntLangValue)counterValue).Value);
    }

    /// <summary>
    /// 测试线程执行带参数的函数
    /// </summary>
    [Fact]
    public async Task Run_ThreadWithParameter_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
result <- 0

func threadFunc(value: int) -> void {
    result <- value * 2
}

thread <- Spawn(threadFunc)
thread.Start(21)";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Give thread time to execute
        await Task.Delay(100);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(42, ((IntLangValue)result).Value);
    }

    #endregion

    #region 线程状态

    /// <summary>
    /// 测试线程 IsAlive 属性
    /// </summary>
    [Fact]
    public async Task Run_CheckThreadIsAlive_ReturnsCorrectStatus()
    {
        // Arrange
        var code = @"
func longRunningTask() -> void {
    Thread.Sleep(200)
}

thread <- Spawn(longRunningTask)
beforeStart <- thread.IsAlive()
thread.Start()
afterStart <- thread.IsAlive()";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var beforeStart = interpreter.Manager.GetValue(new LangId("beforeStart"));
        var afterStart = interpreter.Manager.GetValue(new LangId("afterStart"));

        Assert.IsType<BoolLangValue>(beforeStart);
        Assert.False(((BoolLangValue)beforeStart).Value);

        Assert.IsType<BoolLangValue>(afterStart);
        Assert.True(((BoolLangValue)afterStart).Value);

        // Wait for thread to complete
        await Task.Delay(300);
    }

    #endregion

    #region 线程Join

    /// <summary>
    /// 测试线程 Join 等待完成
    /// </summary>
    [Fact]
    public void Run_ThreadJoin_WaitsForCompletion()
    {
        // Arrange
        var code = @"
value <- 0

func threadFunc() -> void {
    Thread.Sleep(100)
    value <- 42
}

thread <- Spawn(threadFunc)
thread.Start()
thread.Join()
result <- value";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(42, ((IntLangValue)result).Value);
    }

    /// <summary>
    /// 测试线程 Join 带超时
    /// </summary>
    [Fact]
    public void Run_ThreadJoinWithTimeout_ReturnsCorrectly()
    {
        // Arrange
        var code = @"
func longTask() -> void {
    Thread.Sleep(500)
}

thread <- Spawn(longTask)
thread.Start()
joinResult <- thread.Join(100)
result <- not joinResult";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<BoolLangValue>(result);
        Assert.True(((BoolLangValue)result).Value); // Should timeout
    }

    #endregion

    #region 多线程场景

    /// <summary>
    /// 测试多个线程并发执行
    /// </summary>
    [Fact]
    public async Task Run_MultipleThreadsConcurrent_ExecuteSuccessfully()
    {
        // Arrange
        var code = @"
sum <- 0

func addToSum(value: int) -> void {
    Thread.Sleep(50)
    sum <- sum + value
}

t1 <- Spawn(addToSum)
t2 <- Spawn(addToSum)
t3 <- Spawn(addToSum)

t1.Start(10)
t2.Start(20)
t3.Start(30)";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Give threads time to complete
        await Task.Delay(200);

        // Assert
        var sum = interpreter.Manager.GetValue(new LangId("sum"));
        Assert.IsType<IntLangValue>(sum);
        Assert.Equal(60, ((IntLangValue)sum).Value);
    }

    /// <summary>
    /// 测试线程执行顺序
    /// </summary>
    [Fact]
    public async Task Run_ThreadExecutionOrder_WorksCorrectly()
    {
        // Arrange
        var code = @"
results <- {}

func appendResult(id: int) -> void {
    Thread.Sleep(10)
    results.Add(id)
}

t1 <- Spawn(appendResult)
t2 <- Spawn(appendResult)
t3 <- Spawn(appendResult)

t1.Start(1)
t2.Start(2)
t3.Start(3)";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Give threads time to complete
        await Task.Delay(100);

        // Assert
        var results = interpreter.Manager.GetValue(new LangId("results"));
        Assert.NotNull(results);
        Assert.IsType<ListLangValue>(results);
        Assert.Equal(3, ((ListLangValue)results).Values.Count);
    }

    #endregion

    #region 线程名称

    /// <summary>
    /// 测试设置和获取线程名称
    /// </summary>
    [Fact]
    public void Run_SetAndGetThreadName_WorksCorrectly()
    {
        // Arrange
        var code = @"
func simpleTask() -> void {
    Thread.Sleep(10)
}

thread <- Spawn(simpleTask)
thread.Name <- ""WorkerThread""
threadName <- thread.Name";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var threadName = interpreter.Manager.GetValue(new LangId("threadName"));
        Assert.IsType<StringLangValue>(threadName);
        Assert.Equal("WorkerThread", ((StringLangValue)threadName).Value);
    }

    #endregion

    #region 线程优先级

    /// <summary>
    /// 测试设置线程优先级
    /// </summary>
    [Fact]
    public void Run_SetThreadPriority_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
func task() -> void {
    Thread.Sleep(10)
}

thread <- Spawn(task)
thread.Priority <- 2
priority <- thread.Priority";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var priority = interpreter.Manager.GetValue(new LangId("priority"));
        Assert.IsType<IntLangValue>(priority);
        Assert.Equal(2, ((IntLangValue)priority).Value);
    }

    #endregion

    #region 后台线程

    /// <summary>
    /// 测试设置后台线程
    /// </summary>
    [Fact]
    public void Run_SetThreadAsBackground_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
func backgroundTask() -> void {
    Thread.Sleep(1000)
}

thread <- Spawn(backgroundTask)
thread.IsBackground <- true
isBackground <- thread.IsBackground";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Assert
        var isBackground = interpreter.Manager.GetValue(new LangId("isBackground"));
        Assert.IsType<BoolLangValue>(isBackground);
        Assert.True(((BoolLangValue)isBackground).Value);
    }

    #endregion
}

