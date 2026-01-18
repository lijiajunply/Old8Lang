using Old8Lang.Bytecode;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.VirtualMachine.Concurrency;

/// <summary>
/// 虚拟机 Mutex 并发原语测试
/// 测试 Mutex 的创建、锁定、解锁、超时和资源释放
/// </summary>
[Collection("Sequential")]
public class VMConcurrencyMutexTests
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
    public void MutexCreate_BasicUsage_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            mutex <- MutexCreate()
            PrintLine(""Mutex created: "" + (mutex > 0).ToStr())
            MutexDispose(mutex)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Mutex created: true", output);
    }

    [Fact]
    public void MutexLock_BasicUsage_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            mutex <- MutexCreate()
            MutexLock(mutex)
            PrintLine(""Lock acquired"")
            MutexUnlock(mutex)
            PrintLine(""Lock released"")
            MutexDispose(mutex)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("Lock acquired", lines[0]);
        Assert.Equal("Lock released", lines[1]);
    }

    [Fact]
    public void MutexTryLock_Success_ReturnsTrue()
    {
        // Arrange
        var code = @"
            mutex <- MutexCreate()
            result <- MutexTryLock(mutex, 1000)
            PrintLine(""TryLock result: "" + result.ToStr())
            MutexUnlock(mutex)
            MutexDispose(mutex)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("TryLock result: true", output);
    }

    [Fact]
    public void MutexUnlock_AfterLock_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            mutex <- MutexCreate()
            MutexLock(mutex)
            PrintLine(""Locked"")
            MutexUnlock(mutex)
            PrintLine(""Unlocked"")
            MutexDispose(mutex)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("Locked", lines[0]);
        Assert.Equal("Unlocked", lines[1]);
    }

    [Fact]
    public void MutexDispose_AfterUsage_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            mutex <- MutexCreate()
            MutexLock(mutex)
            MutexUnlock(mutex)
            MutexDispose(mutex)
            PrintLine(""Mutex disposed"")
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Mutex disposed", output);
    }

    #endregion

    #region 超时场景测试

    [Fact]
    public void MutexTryLock_WithTimeout_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            mutex <- MutexCreate()

            // 第一次获取锁应该成功
            result1 <- MutexTryLock(mutex, 100)
            PrintLine(""First TryLock: "" + result1.ToStr())

            // 第二次获取锁应该失败（因为已经被锁定）
            result2 <- MutexTryLock(mutex, 100)
            PrintLine(""Second TryLock: "" + result2.ToStr())

            MutexUnlock(mutex)
            MutexDispose(mutex)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("First TryLock: true", lines[0]);
        Assert.Equal("Second TryLock: false", lines[1]);
    }

    [Fact]
    public void MutexTryLock_ZeroTimeout_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            mutex <- MutexCreate()
            result <- MutexTryLock(mutex, 0)
            PrintLine(""TryLock with zero timeout: "" + result.ToStr())
            if result {
                MutexUnlock(mutex)
            }
            MutexDispose(mutex)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("TryLock with zero timeout: true", output);
    }

    #endregion

    #region 异常安全性测试

    [Fact]
    public void MutexLock_WithUsingStatement_ReleasesCorrectly()
    {
        // Arrange
        var code = @"
            using mutex <- MutexCreate() {
                MutexLock(mutex)
                PrintLine(""Inside using block"")
                MutexUnlock(mutex)
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
    public void MutexLock_InTryCatchFinally_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            mutex <- MutexCreate()
            try {
                MutexLock(mutex)
                PrintLine(""Lock acquired in try"")
            } catch (e) {
                PrintLine(""Error: "" + e)
            } finally {
                MutexUnlock(mutex)
                PrintLine(""Lock released in finally"")
                MutexDispose(mutex)
            }
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("Lock acquired in try", lines[0]);
        Assert.Equal("Lock released in finally", lines[1]);
    }

    #endregion

    #region 多线程并发测试

    [Fact]
    public void MutexLock_MultipleThreads_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            mutex <- MutexCreate()
            counter <- 0

            task1 <- spawn () -> {
                for i in [0~<10] {
                    MutexLock(mutex)
                    counter <- counter + 1
                    MutexUnlock(mutex)
                }
            }

            task2 <- spawn () -> {
                for i in [0~<10] {
                    MutexLock(mutex)
                    counter <- counter + 1
                    MutexUnlock(mutex)
                }
            }

            await task1
            await task2

            PrintLine(""Counter: "" + counter.ToStr())
            MutexDispose(mutex)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Counter: 20", output);
    }

    [Fact]
    public void MutexLock_CriticalSection_ProtectsSharedData()
    {
        // Arrange
        var code = @"
            mutex <- MutexCreate()
            sharedList <- {}

            task1 <- spawn () -> {
                for i in [0~<5] {
                    MutexLock(mutex)
                    sharedList.Add(""A"" + i.ToStr())
                    MutexUnlock(mutex)
                }
            }

            task2 <- spawn () -> {
                for i in [0~<5] {
                    MutexLock(mutex)
                    sharedList.Add(""B"" + i.ToStr())
                    MutexUnlock(mutex)
                }
            }

            await task1
            await task2

            PrintLine(""List size: "" + sharedList.Count.ToStr())
            MutexDispose(mutex)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("List size: 10", output);
    }

    #endregion

    #region 边界条件测试

    [Fact]
    public void MutexLock_ReentrantLock_BlocksCorrectly()
    {
        // Arrange
        var code = @"
            mutex <- MutexCreate()
            MutexLock(mutex)
            PrintLine(""First lock acquired"")

            // 尝试再次获取锁（应该阻塞或失败）
            result <- MutexTryLock(mutex, 100)
            PrintLine(""Reentrant lock result: "" + result.ToStr())

            MutexUnlock(mutex)
            MutexDispose(mutex)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("First lock acquired", lines[0]);
        Assert.Equal("Reentrant lock result: false", lines[1]);
    }

    [Fact]
    public void MutexLock_MultipleLockUnlock_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            mutex <- MutexCreate()

            for i in [0~<3] {
                MutexLock(mutex)
                PrintLine(""Lock "" + i.ToStr())
                MutexUnlock(mutex)
            }

            MutexDispose(mutex)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        Assert.Equal("Lock 0", lines[0]);
        Assert.Equal("Lock 1", lines[1]);
        Assert.Equal("Lock 2", lines[2]);
    }

    #endregion

    #region 性能测试

    [Fact]
    public void MutexLock_HighContentionScenario_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            mutex <- MutexCreate()
            counter <- 0
            threadCount <- 5
            iterationsPerThread <- 20

            tasks <- {}

            for t in [0~<threadCount] {
                task <- spawn () -> {
                    for i in [0~<iterationsPerThread] {
                        MutexLock(mutex)
                        counter <- counter + 1
                        MutexUnlock(mutex)
                    }
                }
                tasks.Add(task)
            }

            for task in tasks {
                await task
            }

            expectedCount <- threadCount * iterationsPerThread
            PrintLine(""Counter: "" + counter.ToStr() + "", Expected: "" + expectedCount.ToStr())
            MutexDispose(mutex)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Counter: 100, Expected: 100", output);
    }

    #endregion
}
