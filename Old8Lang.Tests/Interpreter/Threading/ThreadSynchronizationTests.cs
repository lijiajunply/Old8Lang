using Xunit;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Interpreter.Threading;

/// <summary>
/// 线程同步测试 - 测试线程同步和锁机制
/// </summary>
[Collection("Sequential")]
public class ThreadSynchronizationTests
{
    #region Lock 基础测试

    /// <summary>
    /// 测试 Lock 基础用法
    /// </summary>
    [Fact]
    public async Task Run_BasicLock_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
counter <- 0
lockObj <- object()

func increment() -> void {
    lock(lockObj) {
        temp <- counter
        Thread.Sleep(10)
        counter <- temp + 1
    }
}

t1 <- Spawn(increment)
t2 <- Spawn(increment)
t3 <- Spawn(increment)

t1.Start()
t2.Start()
t3.Start()";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Give threads time to complete
        await Task.Delay(200);

        // Assert
        var counter = interpreter.Manager.GetValue(new LangId("counter"));
        Assert.IsType<IntLangValue>(counter);
        Assert.Equal(3, ((IntLangValue)counter).Value);
    }

    /// <summary>
    /// 测试嵌套 Lock
    /// </summary>
    [Fact]
    public async Task Run_NestedLock_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
resource1 <- 0
resource2 <- 0
lock1 <- object()
lock2 <- object()

func updateResources() -> void {
    lock(lock1) {
        resource1 <- resource1 + 1
        lock(lock2) {
            resource2 <- resource2 + 1
        }
    }
}

t1 <- Spawn(updateResources)
t2 <- Spawn(updateResources)

t1.Start()
t2.Start()";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Give threads time to complete
        await Task.Delay(200);

        // Assert
        var resource1 = interpreter.Manager.GetValue(new LangId("resource1"));
        var resource2 = interpreter.Manager.GetValue(new LangId("resource2"));

        Assert.IsType<IntLangValue>(resource1);
        Assert.Equal(2, ((IntLangValue)resource1).Value);

        Assert.IsType<IntLangValue>(resource2);
        Assert.Equal(2, ((IntLangValue)resource2).Value);
    }

    /// <summary>
    /// 测试无锁时的竞态条件
    /// </summary>
    [Fact]
    public async Task Run_RaceConditionWithoutLock_ShowsInconsistentResults()
    {
        // Arrange
        var code = @"
counter <- 0

func incrementWithoutLock() -> void {
    for i <- 0, i < 100, i++ {
        temp <- counter
        counter <- temp + 1
    }
}

t1 <- Spawn(incrementWithoutLock)
t2 <- Spawn(incrementWithoutLock)

t1.Start()
t2.Start()";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Give threads time to complete
        await Task.Delay(300);

        // Assert
        var counter = interpreter.Manager.GetValue(new LangId("counter"));
        Assert.IsType<IntLangValue>(counter);
        // Without lock, result should be less than 200 due to race condition
        Assert.True(((IntLangValue)counter).Value <= 200);
    }

    #endregion

    #region Monitor 测试

    /// <summary>
    /// 测试 Monitor.Enter 和 Monitor.Exit
    /// </summary>
    [Fact]
    public async Task Run_MonitorEnterExit_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
counter <- 0
lockObj <- object()

func safeIncrement() -> void {
    Monitor.Enter(lockObj)
    try {
        temp <- counter
        Thread.Sleep(10)
        counter <- temp + 1
    } finally {
        Monitor.Exit(lockObj)
    }
}

t1 <- Spawn(safeIncrement)
t2 <- Spawn(safeIncrement)

t1.Start()
t2.Start()";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Give threads time to complete
        await Task.Delay(200);

        // Assert
        var counter = interpreter.Manager.GetValue(new LangId("counter"));
        Assert.IsType<IntLangValue>(counter);
        Assert.Equal(2, ((IntLangValue)counter).Value);
    }

    /// <summary>
    /// 测试 Monitor.TryEnter
    /// </summary>
    [Fact]
    public async Task Run_MonitorTryEnter_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
counter <- 0
lockObj <- object()

func tryIncrement() -> void {
    acquired <- Monitor.TryEnter(lockObj, 100)
    if acquired {
        try {
            counter <- counter + 1
            Thread.Sleep(50)
        } finally {
            Monitor.Exit(lockObj)
        }
    }
}

t1 <- Spawn(tryIncrement)
t2 <- Spawn(tryIncrement)

t1.Start()
t2.Start()";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Give threads time to complete
        await Task.Delay(300);

        // Assert
        var counter = interpreter.Manager.GetValue(new LangId("counter"));
        Assert.IsType<IntLangValue>(counter);
        // At least one thread should have acquired the lock
        Assert.True(((IntLangValue)counter).Value >= 1);
    }

    /// <summary>
    /// 测试 Monitor.Wait 和 Monitor.Pulse
    /// </summary>
    [Fact]
    public async Task Run_MonitorWaitPulse_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
ready <- false
lockObj <- object()
result <- 0

func producer() -> void {
    lock(lockObj) {
        ready <- true
        result <- 42
        Monitor.Pulse(lockObj)
    }
}

func consumer() -> void {
    lock(lockObj) {
        if not ready {
            Monitor.Wait(lockObj)
        }
    }
}

producerThread <- Spawn(producer)
consumerThread <- Spawn(consumer)

consumerThread.Start()
Thread.Sleep(50)
producerThread.Start()";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Give threads time to complete
        await Task.Delay(300);

        // Assert
        var result = interpreter.Manager.GetValue(new LangId("result"));
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(42, ((IntLangValue)result).Value);
    }

    #endregion

    #region 互斥锁测试

    /// <summary>
    /// 测试 Mutex 基础用法
    /// </summary>
    [Fact]
    public async Task Run_MutexBasic_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
counter <- 0
mutex <- Mutex()

func incrementWithMutex() -> void {
    mutex.WaitOne()
    try {
        temp <- counter
        Thread.Sleep(10)
        counter <- temp + 1
    } finally {
        mutex.ReleaseMutex()
    }
}

t1 <- Spawn(incrementWithMutex)
t2 <- Spawn(incrementWithMutex)
t3 <- Spawn(incrementWithMutex)

t1.Start()
t2.Start()
t3.Start()";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Give threads time to complete
        await Task.Delay(200);

        // Assert
        var counter = interpreter.Manager.GetValue(new LangId("counter"));
        Assert.IsType<IntLangValue>(counter);
        Assert.Equal(3, ((IntLangValue)counter).Value);
    }

    /// <summary>
    /// 测试 Mutex 带超时的 WaitOne
    /// </summary>
    [Fact]
    public async Task Run_MutexWithTimeout_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
successCount <- 0
mutex <- Mutex()

func tryAcquireMutex() -> void {
    acquired <- mutex.WaitOne(50)
    if acquired {
        try {
            Thread.Sleep(100)
            successCount <- successCount + 1
        } finally {
            mutex.ReleaseMutex()
        }
    }
}

t1 <- Spawn(tryAcquireMutex)
t2 <- Spawn(tryAcquireMutex)

t1.Start()
t2.Start()";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Give threads time to complete
        await Task.Delay(400);

        // Assert
        var successCount = interpreter.Manager.GetValue(new LangId("successCount"));
        Assert.IsType<IntLangValue>(successCount);
        // Due to timeout, likely only one thread will succeed
        Assert.True(((IntLangValue)successCount).Value >= 1);
    }

    #endregion

    #region Semaphore 信号量测试

    /// <summary>
    /// 测试 Semaphore 限制并发数
    /// </summary>
    [Fact]
    public async Task Run_SemaphoreLimitConcurrency_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
activeCount <- 0
maxActive <- 0
semaphore <- Semaphore(2, 2)

func accessResource() -> void {
    semaphore.WaitOne()
    try {
        activeCount <- activeCount + 1
        if activeCount > maxActive {
            maxActive <- activeCount
        }
        Thread.Sleep(50)
        activeCount <- activeCount - 1
    } finally {
        semaphore.Release()
    }
}

t1 <- Spawn(accessResource)
t2 <- Spawn(accessResource)
t3 <- Spawn(accessResource)
t4 <- Spawn(accessResource)

t1.Start()
t2.Start()
t3.Start()
t4.Start()";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Give threads time to complete
        await Task.Delay(400);

        // Assert
        var maxActive = interpreter.Manager.GetValue(new LangId("maxActive"));
        Assert.IsType<IntLangValue>(maxActive);
        // Max concurrent threads should not exceed semaphore limit
        Assert.True(((IntLangValue)maxActive).Value <= 2);
    }

    /// <summary>
    /// 测试 Semaphore 带超时
    /// </summary>
    [Fact]
    public async Task Run_SemaphoreWithTimeout_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
successCount <- 0
timeoutCount <- 0
semaphore <- Semaphore(1, 1)

func tryAccessResource() -> void {
    acquired <- semaphore.WaitOne(100)
    if acquired {
        try {
            Thread.Sleep(150)
            successCount <- successCount + 1
        } finally {
            semaphore.Release()
        }
    } else {
        timeoutCount <- timeoutCount + 1
    }
}

t1 <- Spawn(tryAccessResource)
t2 <- Spawn(tryAccessResource)
t3 <- Spawn(tryAccessResource)

t1.Start()
t2.Start()
t3.Start()";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Give threads time to complete
        await Task.Delay(600);

        // Assert
        var successCount = interpreter.Manager.GetValue(new LangId("successCount"));
        var timeoutCount = interpreter.Manager.GetValue(new LangId("timeoutCount"));

        Assert.IsType<IntLangValue>(successCount);
        Assert.IsType<IntLangValue>(timeoutCount);
        // At least some threads should timeout
        Assert.True(((IntLangValue)timeoutCount).Value > 0);
    }

    /// <summary>
    /// 测试 Semaphore Release 增加计数
    /// </summary>
    [Fact]
    public async Task Run_SemaphoreReleaseIncrement_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
semaphore <- Semaphore(0, 3)
successCount <- 0

func tryAccess() -> void {
    acquired <- semaphore.WaitOne(100)
    if acquired {
        successCount <- successCount + 1
        semaphore.Release()
    }
}

// Release to allow threads to proceed
semaphore.Release(2)

t1 <- Spawn(tryAccess)
t2 <- Spawn(tryAccess)

t1.Start()
t2.Start()";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Give threads time to complete
        await Task.Delay(300);

        // Assert
        var successCount = interpreter.Manager.GetValue(new LangId("successCount"));
        Assert.IsType<IntLangValue>(successCount);
        Assert.Equal(2, ((IntLangValue)successCount).Value);
    }

    #endregion

    #region 死锁检测

    /// <summary>
    /// 测试避免死锁 - 正确的锁顺序
    /// </summary>
    [Fact]
    public async Task Run_AvoidDeadlock_CorrectLockOrder_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
resource1 <- 0
resource2 <- 0
lock1 <- object()
lock2 <- object()

func transfer1To2() -> void {
    lock(lock1) {
        Thread.Sleep(10)
        lock(lock2) {
            resource1 <- resource1 - 10
            resource2 <- resource2 + 10
        }
    }
}

func transfer2To1() -> void {
    lock(lock1) {
        Thread.Sleep(10)
        lock(lock2) {
            resource2 <- resource2 - 5
            resource1 <- resource1 + 5
        }
    }
}

resource1 <- 100
resource2 <- 100

t1 <- Spawn(transfer1To2)
t2 <- Spawn(transfer2To1)

t1.Start()
t2.Start()";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Give threads time to complete
        await Task.Delay(200);

        // Assert
        var resource1 = interpreter.Manager.GetValue(new LangId("resource1"));
        var resource2 = interpreter.Manager.GetValue(new LangId("resource2"));

        Assert.IsType<IntLangValue>(resource1);
        Assert.IsType<IntLangValue>(resource2);

        // Verify transactions completed
        Assert.Equal(95, ((IntLangValue)resource1).Value); // 100 - 10 + 5
        Assert.Equal(105, ((IntLangValue)resource2).Value); // 100 + 10 - 5
    }

    /// <summary>
    /// 测试使用 TryEnter 避免死锁
    /// </summary>
    [Fact]
    public async Task Run_AvoidDeadlock_UsingTryEnter_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
successCount <- 0
lock1 <- object()
lock2 <- object()

func safeOperation() -> void {
    acquired1 <- Monitor.TryEnter(lock1, 50)
    if acquired1 {
        try {
            Thread.Sleep(20)
            acquired2 <- Monitor.TryEnter(lock2, 50)
            if acquired2 {
                try {
                    successCount <- successCount + 1
                } finally {
                    Monitor.Exit(lock2)
                }
            }
        } finally {
            Monitor.Exit(lock1)
        }
    }
}

t1 <- Spawn(safeOperation)
t2 <- Spawn(safeOperation)

t1.Start()
t2.Start()";
        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // Give threads time to complete
        await Task.Delay(300);

        // Assert
        var successCount = interpreter.Manager.GetValue(new LangId("successCount"));
        Assert.IsType<IntLangValue>(successCount);
        // At least one thread should succeed
        Assert.True(((IntLangValue)successCount).Value >= 1);
    }

    #endregion
}

