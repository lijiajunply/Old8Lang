using Old8Lang.Bytecode;
using Old8Lang.Interpreter;
using VM = Old8Lang.Bytecode.VirtualMachine;

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
        // Arrange
        var code = @"
            mutex <- MutexCreate()
            MutexLock(mutex)

            spawn(() -> {
                result <- MutexTryLock(mutex, 100)
                if result {
                    PrintLine(""Lock acquired"")
                    MutexUnlock(mutex)
                } else {
                    PrintLine(""Lock timeout"")
                }
            })

            Sleep(200)
            MutexUnlock(mutex)
            Sleep(100)
            MutexDispose(mutex)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Contains("Lock timeout", output);
    }

    [Fact]
    public void Mutex_ProtectsCriticalSection_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            mutex <- MutexCreate()
            counter <- 0

            func increment() -> void {
                for i in [1~10] {
                    MutexLock(mutex)
                    counter <- counter + 1
                    MutexUnlock(mutex)
                }
            }

            spawn(increment)
            spawn(increment)

            Sleep(500)

            result <- counter
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
        // Arrange
        var code = @"
            mutex <- MutexCreate()
            results <- {}

            func addResult(id:int) -> void {
                MutexLock(mutex)
                results.Add(id)
                Sleep(50)
                MutexUnlock(mutex)
            }

            spawn(() -> addResult(1))
            spawn(() -> addResult(2))
            spawn(() -> addResult(3))

            Sleep(300)

            result <- results.Count()
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
        // Arrange
        var code = @"
            mutex <- MutexCreate()
            counter <- 0
            threadCount <- 5
            iterationsPerThread <- 20

            func incrementCounter() -> void {
                for i in [1~iterationsPerThread] {
                    MutexLock(mutex)
                    counter <- counter + 1
                    MutexUnlock(mutex)
                }
            }

            for i in [1~threadCount] {
                spawn(incrementCounter)
            }

            Sleep(1000)

            result <- counter
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
