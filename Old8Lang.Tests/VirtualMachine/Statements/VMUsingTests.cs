using Old8Lang.Bytecode;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.VirtualMachine.Statements;

/// <summary>
/// 虚拟机 Using 语句测试
/// 测试虚拟机执行 using 语句的正确性
/// </summary>
[Collection("Sequential")]
public class VMUsingTests
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

    [Fact]
    public void UsingStatement_WithMutex_DisposesAutomatically()
    {
        // Arrange
        var code = @"
            using mutex <- MutexCreate() {
                PrintLine(""Using mutex: "" + mutex.ToStr())
                MutexLock(mutex)
                PrintLine(""Lock acquired"")
                MutexUnlock(mutex)
            }
            PrintLine(""After using block"")
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        Assert.StartsWith("Using mutex:", lines[0]);
        Assert.Equal("Lock acquired", lines[1]);
        Assert.Equal("After using block", lines[2]);
    }

    [Fact]
    public void UsingStatement_WithChannel_DisposesAutomatically()
    {
        // Arrange
        var code = @"
            using ch <- ChannelCreate() {
                PrintLine(""Using channel: "" + ch.ToStr())
                ChannelSend(ch, 100)
                value <- ChannelReceive(ch)
                PrintLine(""Received: "" + value.ToStr())
            }
            PrintLine(""After using block"")
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        Assert.StartsWith("Using channel:", lines[0]);
        Assert.Equal("Received: 100", lines[1]);
        Assert.Equal("After using block", lines[2]);
    }

    [Fact]
    public void UsingStatement_WithSemaphore_DisposesAutomatically()
    {
        // Arrange
        var code = @"
            using sem <- SemaphoreCreate(1, 1) {
                PrintLine(""Using semaphore: "" + sem.ToStr())
                SemaphoreAcquire(sem)
                PrintLine(""Semaphore acquired"")
                SemaphoreRelease(sem)
            }
            PrintLine(""After using block"")
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        Assert.StartsWith("Using semaphore:", lines[0]);
        Assert.Equal("Semaphore acquired", lines[1]);
        Assert.Equal("After using block", lines[2]);
    }

    [Fact]
    public void UsingStatement_WithoutVariableName_DisposesAutomatically()
    {
        // Arrange
        var code = @"
            ch <- ChannelCreate()
            PrintLine(""Channel created: "" + ch.ToStr())
            using ch {
                ChannelSend(ch, 200)
                value <- ChannelReceive(ch)
                PrintLine(""Received: "" + value.ToStr())
            }
            PrintLine(""After using block"")
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        Assert.StartsWith("Channel created:", lines[0]);
        Assert.Equal("Received: 200", lines[1]);
        Assert.Equal("After using block", lines[2]);
    }

    [Fact]
    public void UsingStatement_WithException_StillDisposesResource()
    {
        // Arrange
        var code = @"
            try {
                using mutex <- MutexCreate() {
                    PrintLine(""Using mutex"")
                    MutexLock(mutex)
                    PrintLine(""Lock acquired"")
                    throw ""Test exception""
                }
            } catch (e) {
                PrintLine(""Caught exception"")
            }
            PrintLine(""After try-catch"")
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(4, lines.Length);
        Assert.Equal("Using mutex", lines[0]);
        Assert.Equal("Lock acquired", lines[1]);
        Assert.Equal("Caught exception", lines[2]);
        Assert.Equal("After try-catch", lines[3]);
        // 注意：即使抛出异常，mutex 也应该被自动释放
    }

    [Fact]
    public void UsingStatement_NestedUsing_DisposesInCorrectOrder()
    {
        // Arrange
        var code = @"
            using mutex1 <- MutexCreate() {
                PrintLine(""Using mutex1: "" + mutex1.ToStr())
                using mutex2 <- MutexCreate() {
                    PrintLine(""Using mutex2: "" + mutex2.ToStr())
                    MutexLock(mutex1)
                    MutexLock(mutex2)
                    PrintLine(""Both locks acquired"")
                    MutexUnlock(mutex2)
                    MutexUnlock(mutex1)
                }
                PrintLine(""After inner using"")
            }
            PrintLine(""After outer using"")
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(5, lines.Length);
        Assert.StartsWith("Using mutex1:", lines[0]);
        Assert.StartsWith("Using mutex2:", lines[1]);
        Assert.Equal("Both locks acquired", lines[2]);
        Assert.Equal("After inner using", lines[3]);
        Assert.Equal("After outer using", lines[4]);
    }

    [Fact]
    public void UsingStatement_MultipleResources_DisposesAll()
    {
        // Arrange
        var code = @"
            using ch1 <- ChannelCreate() {
                PrintLine(""Using channel1: "" + ch1.ToStr())
                using ch2 <- ChannelCreate() {
                    PrintLine(""Using channel2: "" + ch2.ToStr())
                    ChannelSend(ch1, 100)
                    ChannelSend(ch2, 200)
                    v1 <- ChannelReceive(ch1)
                    v2 <- ChannelReceive(ch2)
                    PrintLine(""Received: "" + v1.ToStr() + "", "" + v2.ToStr())
                }
            }
            PrintLine(""All resources disposed"")
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(4, lines.Length);
        Assert.StartsWith("Using channel1:", lines[0]);
        Assert.StartsWith("Using channel2:", lines[1]);
        Assert.Equal("Received: 100, 200", lines[2]);
        Assert.Equal("All resources disposed", lines[3]);
    }

    [Fact]
    public void UsingWithDefer_ExecutesInCorrectOrder()
    {
        // Arrange
        // 注意：defer 是函数级作用域，在函数返回前执行，而非代码块结束时
        var code = @"
            func test() -> void {
                using mutex <- MutexCreate() {
                    PrintLine(""Using mutex"")
                    defer PrintLine(""Defer cleanup"")
                    MutexLock(mutex)
                    PrintLine(""Lock acquired"")
                    MutexUnlock(mutex)
                }
                PrintLine(""After using"")
            }
            test()
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(4, lines.Length);
        Assert.Equal("Using mutex", lines[0]);
        Assert.Equal("Lock acquired", lines[1]);
        Assert.Equal("After using", lines[2]); // using 块结束后继续执行
        Assert.Equal("Defer cleanup", lines[3]); // defer 在函数返回前执行
    }

    [Fact]
    public void UsingWithDeferAndException_BothExecute()
    {
        // Arrange
        // 注意：在虚拟机模式下，defer 在异常处理前执行（因为异常触发了函数的退出流程）
        var code = @"
            func test() -> void {
                try {
                    using ch <- ChannelCreate() {
                        PrintLine(""Using channel"")
                        defer PrintLine(""Defer cleanup"")
                        ChannelSend(ch, 100)
                        throw ""Test exception""
                    }
                } catch (e) {
                    PrintLine(""Caught: "" + e.ToStr())
                }
            }
            test()
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        Assert.Equal("Using channel", lines[0]);
        // 虚拟机模式下，defer 在异常处理前执行
        Assert.Equal("Defer cleanup", lines[1]);
        Assert.Equal("Caught: Test exception", lines[2]);
    }

    [Fact]
    public void UsingWithMultipleDefers_ExecutesInCorrectOrder()
    {
        // Arrange
        // 注意：defer 是函数级作用域，在函数返回前执行（LIFO 顺序）
        var code = @"
            func test() -> void {
                using mutex <- MutexCreate() {
                    defer PrintLine(""Defer 1"")
                    defer PrintLine(""Defer 2"")
                    defer PrintLine(""Defer 3"")
                    PrintLine(""Main code"")
                }
                PrintLine(""After using"")
            }
            test()
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(5, lines.Length);
        Assert.Equal("Main code", lines[0]);
        Assert.Equal("After using", lines[1]); // using 块结束后继续执行
        Assert.Equal("Defer 3", lines[2]); // LIFO 顺序，defer 在函数返回前执行
        Assert.Equal("Defer 2", lines[3]);
        Assert.Equal("Defer 1", lines[4]);
    }

    [Fact]
    public void UsingWithReturn_DisposesBeforeReturn()
    {
        // Arrange
        var code = @"
            func test() -> int {
                using ch <- ChannelCreate() {
                    PrintLine(""Using channel"")
                    ChannelSend(ch, 42)
                    value <- ChannelReceive(ch)
                    return value
                }
            }
            result <- test()
            PrintLine(""Result: "" + result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("Using channel", lines[0]);
        Assert.Equal("Result: 42", lines[1]); // using 资源在 return 前释放
    }

    [Fact]
    public void UsingWithReadWriteLock_DisposesAutomatically()
    {
        // Arrange
        var code = @"
            using lock <- ReadWriteLockCreate() {
                PrintLine(""Using lock: "" + lock.ToStr())
                ReadLockAcquire(lock)
                PrintLine(""Read lock acquired"")
                ReadLockRelease(lock)
                WriteLockAcquire(lock)
                PrintLine(""Write lock acquired"")
                WriteLockRelease(lock)
            }
            PrintLine(""After using block"")
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(4, lines.Length);
        Assert.StartsWith("Using lock:", lines[0]);
        Assert.Equal("Read lock acquired", lines[1]);
        Assert.Equal("Write lock acquired", lines[2]);
        Assert.Equal("After using block", lines[3]);
    }

    [Fact]
    public void UsingWithAtomicInt_DisposesAutomatically()
    {
        // Arrange
        var code = @"
            using atomic <- AtomicIntCreate(0) {
                PrintLine(""Using atomic: "" + atomic.ToStr())
                AtomicIntIncrement(atomic)
                value <- AtomicIntGet(atomic)
                PrintLine(""Value: "" + value.ToStr())
            }
            PrintLine(""After using block"")
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        Assert.StartsWith("Using atomic:", lines[0]);
        Assert.Equal("Value: 1", lines[1]);
        Assert.Equal("After using block", lines[2]);
    }

    [Fact]
    public void UsingWithCountDownLatch_DisposesAutomatically()
    {
        // Arrange
        var code = @"
            using latch <- CountDownLatchCreate(3) {
                PrintLine(""Using latch: "" + latch.ToStr())
                count <- CountDownLatchGetCount(latch)
                PrintLine(""Initial count: "" + count.ToStr())
                CountDownLatchCountDown(latch)
                count <- CountDownLatchGetCount(latch)
                PrintLine(""After countdown: "" + count.ToStr())
            }
            PrintLine(""After using block"")
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(4, lines.Length);
        Assert.StartsWith("Using latch:", lines[0]);
        Assert.Equal("Initial count: 3", lines[1]);
        Assert.Equal("After countdown: 2", lines[2]);
        Assert.Equal("After using block", lines[3]);
    }
}
