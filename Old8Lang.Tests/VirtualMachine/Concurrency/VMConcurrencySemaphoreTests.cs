using Old8Lang.Bytecode;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.VirtualMachine.Concurrency;

/// <summary>
/// 虚拟机 Semaphore 并发原语测试
/// 测试 Semaphore 的创建、获取、释放、超时和资源管理
/// </summary>
[Collection("Sequential")]
public class VMConcurrencySemaphoreTests
{
    /// <summary>
    /// 执行虚拟机代码并捕获控制台输出
    /// </summary>
    private string ExecuteVMCode(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        // 编译为字节码
        var compiler = new BytecodeCompiler();
        var bytecodeFile = compiler.Compile(ast);

        // 捕获控制台输出
        var originalOut = Console.Out;
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            // 执行字节码
            var vm = new Old8Lang.Bytecode.VirtualMachine(bytecodeFile);
            vm.Execute();

            return stringWriter.ToString().Trim();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    #region 基本功能测试

    [Fact]
    public void SemaphoreCreate_BasicUsage_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            sem <- SemaphoreCreate(1, 3)
            PrintLine(""Semaphore created: "" + (sem > 0).ToStr())
            SemaphoreDispose(sem)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Semaphore created: true", output);
    }

    [Fact]
    public void SemaphoreAcquire_BasicUsage_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            sem <- SemaphoreCreate(1, 1)
            SemaphoreAcquire(sem)
            PrintLine(""Semaphore acquired"")
            SemaphoreRelease(sem)
            PrintLine(""Semaphore released"")
            SemaphoreDispose(sem)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("Semaphore acquired", lines[0]);
        Assert.Equal("Semaphore released", lines[1]);
    }

    [Fact]
    public void SemaphoreTryAcquire_Success_ReturnsTrue()
    {
        // Arrange
        var code = @"
            sem <- SemaphoreCreate(1, 1)
            result <- SemaphoreTryAcquire(sem, 1000)
            PrintLine(""TryAcquire result: "" + result.ToStr())
            if result {
                SemaphoreRelease(sem)
            }
            SemaphoreDispose(sem)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("TryAcquire result: true", output);
    }

    [Fact]
    public void SemaphoreRelease_AfterAcquire_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            sem <- SemaphoreCreate(2, 2)
            SemaphoreAcquire(sem)
            SemaphoreAcquire(sem)
            PrintLine(""Acquired twice"")
            SemaphoreRelease(sem)
            SemaphoreRelease(sem)
            PrintLine(""Released twice"")
            SemaphoreDispose(sem)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("Acquired twice", lines[0]);
        Assert.Equal("Released twice", lines[1]);
    }

    [Fact]
    public void SemaphoreDispose_AfterUsage_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            sem <- SemaphoreCreate(1, 1)
            SemaphoreAcquire(sem)
            SemaphoreRelease(sem)
            SemaphoreDispose(sem)
            PrintLine(""Semaphore disposed"")
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Semaphore disposed", output);
    }

    #endregion

    #region 超时场景测试

    [Fact]
    public void SemaphoreTryAcquire_WithTimeout_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            sem <- SemaphoreCreate(1, 1)

            // 第一次获取应该成功
            result1 <- SemaphoreTryAcquire(sem, 100)
            PrintLine(""First TryAcquire: "" + result1.ToStr())

            // 第二次获取应该失败（信号量已满）
            result2 <- SemaphoreTryAcquire(sem, 100)
            PrintLine(""Second TryAcquire: "" + result2.ToStr())

            SemaphoreRelease(sem)
            SemaphoreDispose(sem)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("First TryAcquire: true", lines[0]);
        Assert.Equal("Second TryAcquire: false", lines[1]);
    }

    [Fact]
    public void SemaphoreTryAcquire_ZeroTimeout_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            sem <- SemaphoreCreate(1, 1)
            result <- SemaphoreTryAcquire(sem, 0)
            PrintLine(""TryAcquire with zero timeout: "" + result.ToStr())
            if result {
                SemaphoreRelease(sem)
            }
            SemaphoreDispose(sem)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("TryAcquire with zero timeout: true", output);
    }

    #endregion

    #region 异常安全性测试

    [Fact]
    public void SemaphoreAcquire_WithUsingStatement_ReleasesCorrectly()
    {
        // Arrange
        var code = @"
            using sem <- SemaphoreCreate(1, 1) {
                SemaphoreAcquire(sem)
                PrintLine(""Inside using block"")
                SemaphoreRelease(sem)
            }
            PrintLine(""After using block"")
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("Inside using block", lines[0]);
        Assert.Equal("After using block", lines[1]);
    }

    [Fact]
    public void SemaphoreAcquire_InTryCatchFinally_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            sem <- SemaphoreCreate(1, 1)
            try {
                SemaphoreAcquire(sem)
                PrintLine(""Semaphore acquired in try"")
            } catch (e) {
                PrintLine(""Error: "" + e)
            } finally {
                SemaphoreRelease(sem)
                PrintLine(""Semaphore released in finally"")
                SemaphoreDispose(sem)
            }
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("Semaphore acquired in try", lines[0]);
        Assert.Equal("Semaphore released in finally", lines[1]);
    }

    #endregion

    #region 多线程并发测试

    [Fact]
    public void SemaphoreAcquire_MultipleThreads_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            sem <- SemaphoreCreate(2, 2)
            counter <- 0

            task1 <- spawn () -> {
                for i in [0~<5] {
                    SemaphoreAcquire(sem)
                    counter <- counter + 1
                    SemaphoreRelease(sem)
                }
            }

            task2 <- spawn () -> {
                for i in [0~<5] {
                    SemaphoreAcquire(sem)
                    counter <- counter + 1
                    SemaphoreRelease(sem)
                }
            }

            await task1
            await task2

            PrintLine(""Counter: "" + counter.ToStr())
            SemaphoreDispose(sem)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Counter: 10", output);
    }

    [Fact]
    public void SemaphoreAcquire_LimitedConcurrency_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            sem <- SemaphoreCreate(2, 2)
            activeCount <- 0
            maxActive <- 0

            task1 <- spawn () -> {
                for i in [0~<3] {
                    SemaphoreAcquire(sem)
                    activeCount <- activeCount + 1
                    if activeCount > maxActive {
                        maxActive <- activeCount
                    }
                    Sleep(10)
                    activeCount <- activeCount - 1
                    SemaphoreRelease(sem)
                }
            }

            task2 <- spawn () -> {
                for i in [0~<3] {
                    SemaphoreAcquire(sem)
                    activeCount <- activeCount + 1
                    if activeCount > maxActive {
                        maxActive <- activeCount
                    }
                    Sleep(10)
                    activeCount <- activeCount - 1
                    SemaphoreRelease(sem)
                }
            }

            await task1
            await task2

            PrintLine(""Max active: "" + maxActive.ToStr())
            SemaphoreDispose(sem)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Max active: 2", output);
    }

    #endregion

    #region 边界条件测试

    [Fact]
    public void SemaphoreCreate_WithZeroInitialCount_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            sem <- SemaphoreCreate(0, 3)
            result <- SemaphoreTryAcquire(sem, 100)
            PrintLine(""TryAcquire with zero initial count: "" + result.ToStr())
            SemaphoreDispose(sem)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("TryAcquire with zero initial count: false", output);
    }

    [Fact]
    public void SemaphoreCreate_WithMaxCount_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            sem <- SemaphoreCreate(3, 3)

            // 获取所有可用的信号量
            result1 <- SemaphoreTryAcquire(sem, 100)
            result2 <- SemaphoreTryAcquire(sem, 100)
            result3 <- SemaphoreTryAcquire(sem, 100)

            PrintLine(""Acquired 3: "" + (result1 && result2 && result3).ToStr())

            // 尝试获取第四个（应该失败）
            result4 <- SemaphoreTryAcquire(sem, 100)
            PrintLine(""Fourth acquire: "" + result4.ToStr())

            SemaphoreRelease(sem)
            SemaphoreRelease(sem)
            SemaphoreRelease(sem)
            SemaphoreDispose(sem)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("Acquired 3: true", lines[0]);
        Assert.Equal("Fourth acquire: false", lines[1]);
    }

    [Fact]
    public void SemaphoreAcquire_MultipleAcquireRelease_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            sem <- SemaphoreCreate(3, 3)

            for i in [0~<5] {
                SemaphoreAcquire(sem)
                PrintLine(""Acquired "" + i.ToStr())
                SemaphoreRelease(sem)
            }

            SemaphoreDispose(sem)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(5, lines.Length);
        for (int i = 0; i < 5; i++)
        {
            Assert.Equal($"Acquired {i}", lines[i]);
        }
    }

    #endregion

    #region 性能测试

    [Fact]
    public void SemaphoreAcquire_HighContentionScenario_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            sem <- SemaphoreCreate(3, 3)
            counter <- 0
            threadCount <- 5
            iterationsPerThread <- 10

            tasks <- {}

            for t in [0~<threadCount] {
                task <- spawn () -> {
                    for i in [0~<iterationsPerThread] {
                        SemaphoreAcquire(sem)
                        counter <- counter + 1
                        SemaphoreRelease(sem)
                    }
                }
                tasks.Add(task)
            }

            for task in tasks {
                await task
            }

            expectedCount <- threadCount * iterationsPerThread
            PrintLine(""Counter: "" + counter.ToStr() + "", Expected: "" + expectedCount.ToStr())
            SemaphoreDispose(sem)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Counter: 50, Expected: 50", output);
    }

    #endregion
}
