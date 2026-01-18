using Old8Lang.Bytecode;
using Old8Lang.Interpreter;
using VM = Old8Lang.Bytecode.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Statements;

/// <summary>
/// 虚拟机 Using 语句测试
/// 测试 Using 语句的资源管理、异常处理、嵌套使用等功能
/// </summary>
[Collection("Sequential")]
public class VMUsingStatementTests
{
    private string ExecuteVMCode(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        var compiler = new BytecodeCompiler();
        var bytecodeFile = compiler.Compile(ast);

        var originalOut = Console.Out;
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            var vm = new VM(bytecodeFile);
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
                MutexLock(mutex)
                PrintLine(""Locked"")
                MutexUnlock(mutex)
            }
            PrintLine(""Disposed"")
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("Locked", lines[0]);
        Assert.Equal("Disposed", lines[1]);
    }

    [Fact]
    public void UsingStatement_WithChannel_DisposesAutomatically()
    {
        // Arrange
        var code = @"
            using ch <- ChannelCreate() {
                ChannelSend(ch, 42)
                val <- ChannelReceive(ch)
                PrintLine(""Value: "" + val.ToStr())
            }
            PrintLine(""Disposed"")
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("Value: 42", lines[0]);
        Assert.Equal("Disposed", lines[1]);
    }

    [Fact]
    public void UsingStatement_WithException_DisposesOnException()
    {
        // Arrange
        var code = @"
            try {
                using mutex <- MutexCreate() {
                    PrintLine(""Before exception"")
                    throw ""Test error""
                }
            } catch (e) {
                PrintLine(""Caught: "" + e)
            }
            PrintLine(""After try-catch"")
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        Assert.Equal("Before exception", lines[0]);
        Assert.Equal("Caught: Test error", lines[1]);
        Assert.Equal("After try-catch", lines[2]);
    }

    [Fact]
    public void UsingStatement_NestedUsing_DisposesInCorrectOrder()
    {
        // Arrange
        var code = @"
            using mutex1 <- MutexCreate() {
                PrintLine(""Mutex1 created"")
                using mutex2 <- MutexCreate() {
                    PrintLine(""Mutex2 created"")
                    MutexLock(mutex1)
                    MutexLock(mutex2)
                    PrintLine(""Both locked"")
                    MutexUnlock(mutex2)
                    MutexUnlock(mutex1)
                }
                PrintLine(""Mutex2 disposed"")
            }
            PrintLine(""Mutex1 disposed"")
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(5, lines.Length);
        Assert.Equal("Mutex1 created", lines[0]);
        Assert.Equal("Mutex2 created", lines[1]);
        Assert.Equal("Both locked", lines[2]);
        Assert.Equal("Mutex2 disposed", lines[3]);
        Assert.Equal("Mutex1 disposed", lines[4]);
    }

    [Fact]
    public void UsingStatement_WithExistingVariable_DisposesCorrectly()
    {
        // Arrange
        var code = @"
            ch <- ChannelCreate()
            using ch {
                ChannelSend(ch, 100)
                val <- ChannelReceive(ch)
                PrintLine(""Value: "" + val.ToStr())
            }
            PrintLine(""Disposed"")
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("Value: 100", lines[0]);
        Assert.Equal("Disposed", lines[1]);
    }

    [Fact]
    public void UsingStatement_WithSemaphore_DisposesAutomatically()
    {
        // Arrange
        var code = @"
            using sem <- SemaphoreCreate(1, 1) {
                SemaphoreAcquire(sem)
                PrintLine(""Acquired"")
                SemaphoreRelease(sem)
            }
            PrintLine(""Disposed"")
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("Acquired", lines[0]);
        Assert.Equal("Disposed", lines[1]);
    }

    [Fact]
    public void UsingStatement_WithAtomicInt_DisposesAutomatically()
    {
        // Arrange
        var code = @"
            using atomic <- AtomicIntCreate(0) {
                AtomicIntIncrement(atomic)
                val <- AtomicIntGet(atomic)
                PrintLine(""Value: "" + val.ToStr())
            }
            PrintLine(""Disposed"")
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("Value: 1", lines[0]);
        Assert.Equal("Disposed", lines[1]);
    }

    [Fact]
    public void UsingStatement_WithReadWriteLock_DisposesAutomatically()
    {
        // Arrange
        var code = @"
            using rwLock <- ReadWriteLockCreate() {
                ReadLockAcquire(rwLock)
                PrintLine(""Read lock acquired"")
                ReadLockRelease(rwLock)
            }
            PrintLine(""Disposed"")
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("Read lock acquired", lines[0]);
        Assert.Equal("Disposed", lines[1]);
    }

    [Fact]
    public void UsingStatement_WithCountDownLatch_DisposesAutomatically()
    {
        // Arrange
        var code = @"
            using latch <- CountDownLatchCreate(1) {
                CountDownLatchCountDown(latch)
                CountDownLatchWait(latch)
                PrintLine(""Latch released"")
            }
            PrintLine(""Disposed"")
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("Latch released", lines[0]);
        Assert.Equal("Disposed", lines[1]);
    }

    [Fact]
    public void UsingStatement_WithCyclicBarrier_DisposesAutomatically()
    {
        // Arrange
        var code = @"
            using barrier <- CyclicBarrierCreate(1) {
                CyclicBarrierAwait(barrier)
                PrintLine(""Barrier passed"")
            }
            PrintLine(""Disposed"")
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("Barrier passed", lines[0]);
        Assert.Equal("Disposed", lines[1]);
    }

    [Fact]
    public void UsingStatement_WithCancellationToken_DisposesAutomatically()
    {
        // Arrange
        var code = @"
            using cts <- CreateCancellationTokenSource() {
                Cancel(cts)
                PrintLine(""Cancelled"")
            }
            PrintLine(""Disposed"")
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("Cancelled", lines[0]);
        Assert.Equal("Disposed", lines[1]);
    }

    [Fact]
    public void UsingStatement_WithReturn_DisposesBeforeReturn()
    {
        // Arrange
        var code = @"
            func test() -> int {
                using mutex <- MutexCreate() {
                    MutexLock(mutex)
                    PrintLine(""Locked"")
                    MutexUnlock(mutex)
                    return 42
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
        Assert.Equal("Locked", lines[0]);
        Assert.Equal("Result: 42", lines[1]);
    }

    [Fact]
    public void UsingStatement_MultipleResources_DisposesAll()
    {
        // Arrange
        var code = @"
            using mutex <- MutexCreate() {
                using ch <- ChannelCreate() {
                    using sem <- SemaphoreCreate(1, 1) {
                        PrintLine(""All resources created"")
                    }
                    PrintLine(""Semaphore disposed"")
                }
                PrintLine(""Channel disposed"")
            }
            PrintLine(""Mutex disposed"")
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(4, lines.Length);
        Assert.Equal("All resources created", lines[0]);
        Assert.Equal("Semaphore disposed", lines[1]);
        Assert.Equal("Channel disposed", lines[2]);
        Assert.Equal("Mutex disposed", lines[3]);
    }

    [Fact]
    public void UsingStatement_WithDefer_ExecutesInCorrectOrder()
    {
        // Arrange
        var code = @"
            func test() -> void {
                using mutex <- MutexCreate() {
                    defer PrintLine(""Defer executed"")
                    PrintLine(""Body"")
                }
                PrintLine(""After using"")
            }

            test()
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        Assert.Equal("Body", lines[0]);
        Assert.Equal("Defer executed", lines[1]);
        Assert.Equal("After using", lines[2]);
    }
}
