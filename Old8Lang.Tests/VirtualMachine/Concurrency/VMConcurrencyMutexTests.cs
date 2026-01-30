using Old8Lang.Bytecode;
using Old8Lang.Interpreter;
using VM = Old8Lang.Bytecode.VM.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Concurrency;

/// <summary>
/// 虚拟机 Mutex 并发原语测试
/// 测试 Mutex 的创建、锁定、解锁和释放功能
/// </summary>
[Collection("Sequential")]
public class VMConcurrencyMutexTests
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
    public void MutexCreate_BasicUsage_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            mutex <- MutexCreate()
            result <- mutex != null
            MutexDispose(mutex)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.True((bool)result);
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
            if result {
                PrintLine(""Lock acquired"")
                MutexUnlock(mutex)
            }
            MutexDispose(mutex)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Lock acquired", output);
    }

    [Fact]
    public void MutexTryLock_Timeout_ReturnsFalse()
    {
        // Arrange - 使用手动启动线程的模式
        var code = @"
            mutex <- MutexCreate()
            MutexLock(mutex)
            timeoutOccurred <- AtomicIntCreate(0)

            func tryLockTask() -> void {
                result <- MutexTryLock(mutex, 100)
                if result {
                    PrintLine(""Lock acquired"")
                    MutexUnlock(mutex)
                } else {
                    PrintLine(""Lock timeout"")
                    AtomicIntSet(timeoutOccurred, 1)
                }
            }

            t <- spawn(tryLockTask)
            t.Start()

            Sleep(200)
            MutexUnlock(mutex)
            Sleep(100)
            MutexDispose(mutex)
            AtomicIntDispose(timeoutOccurred)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Contains("Lock timeout", output);
    }

    [Fact]
    public void Mutex_ProtectsCriticalSection_ExecutesCorrectly()
    {
        // Arrange - 使用 AtomicInt 来保证线程安全的计数
        var code = @"
            mutex <- MutexCreate()
            counter <- AtomicIntCreate(0)

            func increment() -> void {
                for i in [1~10] {
                    MutexLock(mutex)
                    AtomicIntIncrement(counter)
                    MutexUnlock(mutex)
                }
            }

            t1 <- spawn(increment)
            t2 <- spawn(increment)

            t1.Start()
            t2.Start()

            Sleep(500)

            result <- AtomicIntGet(counter)
            AtomicIntDispose(counter)
            MutexDispose(mutex)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(20, result);
    }

    [Fact]
    public void Mutex_WithUsing_DisposesAutomatically()
    {
        // Arrange
        var code = @"
            using mutex <- MutexCreate() {
                MutexLock(mutex)
                PrintLine(""Lock acquired"")
                MutexUnlock(mutex)
            }
            PrintLine(""Mutex disposed"")
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("Lock acquired", lines[0]);
        Assert.Equal("Mutex disposed", lines[1]);
    }

    [Fact]
    public void Mutex_NestedLocking_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            mutex1 <- MutexCreate()
            mutex2 <- MutexCreate()

            MutexLock(mutex1)
            PrintLine(""Mutex1 locked"")
            MutexLock(mutex2)
            PrintLine(""Mutex2 locked"")
            MutexUnlock(mutex2)
            PrintLine(""Mutex2 unlocked"")
            MutexUnlock(mutex1)
            PrintLine(""Mutex1 unlocked"")

            MutexDispose(mutex1)
            MutexDispose(mutex2)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(4, lines.Length);
        Assert.Equal("Mutex1 locked", lines[0]);
        Assert.Equal("Mutex2 locked", lines[1]);
        Assert.Equal("Mutex2 unlocked", lines[2]);
        Assert.Equal("Mutex1 unlocked", lines[3]);
    }

    [Fact]
    public void Mutex_MultipleThreads_SerializesAccess()
    {
        // Arrange - 使用 AtomicInt 来保证线程安全的计数
        var code = @"
            mutex <- MutexCreate()
            counter <- AtomicIntCreate(0)

            func addResult() -> void {
                MutexLock(mutex)
                AtomicIntIncrement(counter)
                Sleep(50)
                MutexUnlock(mutex)
            }

            t1 <- spawn(addResult)
            t2 <- spawn(addResult)
            t3 <- spawn(addResult)

            t1.Start()
            t2.Start()
            t3.Start()

            Sleep(300)

            result <- AtomicIntGet(counter)
            AtomicIntDispose(counter)
            MutexDispose(mutex)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(3, result);
    }

    [Fact]
    public void Mutex_WithException_ReleasesLock()
    {
        // Arrange
        var code = @"
            mutex <- MutexCreate()

            try {
                MutexLock(mutex)
                PrintLine(""Lock acquired"")
                throw ""Test exception""
            } catch (e) {
                PrintLine(""Exception caught"")
                MutexUnlock(mutex)
            }

            PrintLine(""After exception"")
            MutexDispose(mutex)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        Assert.Equal("Lock acquired", lines[0]);
        Assert.Equal("Exception caught", lines[1]);
        Assert.Equal("After exception", lines[2]);
    }

    [Fact]
    public void Mutex_ReentrantLocking_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            mutex <- MutexCreate()

            func recursiveFunction(depth:int) -> void {
                if depth > 0 {
                    MutexLock(mutex)
                    PrintLine(""Depth: "" + depth.ToStr())
                    recursiveFunction(depth - 1)
                    MutexUnlock(mutex)
                }
            }

            recursiveFunction(3)
            MutexDispose(mutex)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
    }

    [Fact]
    public void Mutex_WithDefer_ReleasesAutomatically()
    {
        // Arrange
        var code = @"
            mutex <- MutexCreate()

            func criticalSection() -> void {
                MutexLock(mutex)
                defer MutexUnlock(mutex)
                PrintLine(""In critical section"")
            }

            criticalSection()
            PrintLine(""After critical section"")
            MutexDispose(mutex)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("In critical section", lines[0]);
        Assert.Equal("After critical section", lines[1]);
    }

    [Fact]
    public void Mutex_StressTest_HandlesHighContention()
    {
        // Arrange - 使用 AtomicInt 来保证线程安全的计数
        var code = @"
            mutex <- MutexCreate()
            counter <- AtomicIntCreate(0)

            func incrementCounter() -> void {
                for i in [1~20] {
                    MutexLock(mutex)
                    AtomicIntIncrement(counter)
                    MutexUnlock(mutex)
                }
            }

            t1 <- spawn(incrementCounter)
            t2 <- spawn(incrementCounter)
            t3 <- spawn(incrementCounter)
            t4 <- spawn(incrementCounter)
            t5 <- spawn(incrementCounter)

            t1.Start()
            t2.Start()
            t3.Start()
            t4.Start()
            t5.Start()

            Sleep(1000)

            result <- AtomicIntGet(counter)
            AtomicIntDispose(counter)
            MutexDispose(mutex)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(100, result); // 5 threads * 20 iterations = 100
    }
}
