using Old8Lang.AST.Expression.Value;
using Old8Lang.Bytecode;
using Old8Lang.Interpreter;
using VM = Old8Lang.Bytecode.VM.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Concurrency;

/// <summary>
/// 虚拟机 Semaphore 并发原语测试
/// 测试 Semaphore 的创建、获取、释放和释放功能
/// </summary>
[Collection("Sequential")]
public class VMConcurrencySemaphoreTests
{
    /// <summary>
    /// 辅助方法：从全局变量中获取整数值
    /// </summary>
    private static int GetIntValue(object? value)
    {
        return value switch
        {
            int i => i,
            long l => (int)l,
            IntLangValue ilv => ilv.Value,
            _ => Convert.ToInt32(value)
        };
    }

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
    public void SemaphoreCreate_BasicUsage_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            sem <- SemaphoreCreate(1, 1)
            result <- sem != null
            SemaphoreDispose(sem)
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
            if result {
                PrintLine(""Acquired"")
                SemaphoreRelease(sem)
            }
            SemaphoreDispose(sem)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Acquired", output);
    }

    [Fact]
    public void Semaphore_LimitsAccess_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            sem <- SemaphoreCreate(2, 2)
            counter <- 0

            func accessResource() -> void {
                SemaphoreAcquire(sem)
                counter <- counter + 1
                Sleep(100)
                counter <- counter - 1
                SemaphoreRelease(sem)
            }

            t1 <- spawn(accessResource)
            t2 <- spawn(accessResource)
            t3 <- spawn(accessResource)

            t1.Start()
            t2.Start()
            t3.Start()

            Sleep(50)
            maxConcurrent <- counter

            Sleep(300)
            SemaphoreDispose(sem)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var maxConcurrent = vm.GetGlobalVariable("maxConcurrent");
        Assert.NotNull(maxConcurrent);
        Assert.True(GetIntValue(maxConcurrent) <= 2);
    }

    [Fact]
    public void Semaphore_MultiplePermits_ExecutesCorrectly()
    {
        // Arrange - 使用 AtomicInt 来确保计数操作是原子的
        var code = @"
            sem <- SemaphoreCreate(3, 3)
            counter <- AtomicIntCreate(0)

            func worker() -> void {
                SemaphoreAcquire(sem)
                AtomicIntIncrement(counter)
                Sleep(50)
                SemaphoreRelease(sem)
            }

            t1 <- spawn(worker)
            t2 <- spawn(worker)
            t3 <- spawn(worker)
            t4 <- spawn(worker)
            t5 <- spawn(worker)

            t1.Start()
            t2.Start()
            t3.Start()
            t4.Start()
            t5.Start()

            Sleep(500)
            result <- AtomicIntGet(counter)
            AtomicIntDispose(counter)
            SemaphoreDispose(sem)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(5, GetIntValue(result));
    }

    [Fact]
    public void Semaphore_WithUsing_DisposesAutomatically()
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
    public void Semaphore_ZeroInitialCount_BlocksAcquire()
    {
        // Arrange
        var code = @"
            sem <- SemaphoreCreate(0, 1)

            t <- spawn(() -> {
                result <- SemaphoreTryAcquire(sem, 100)
                if result {
                    PrintLine(""Acquired"")
                } else {
                    PrintLine(""Timeout"")
                }
            })
            t.Start()

            Sleep(200)
            SemaphoreDispose(sem)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Contains("Timeout", output);
    }

    [Fact]
    public void Semaphore_MultipleReleases_IncreasesCount()
    {
        // Arrange
        var code = @"
            sem <- SemaphoreCreate(0, 3)

            SemaphoreRelease(sem)
            SemaphoreRelease(sem)

            result1 <- SemaphoreTryAcquire(sem, 100)
            result2 <- SemaphoreTryAcquire(sem, 100)
            result3 <- SemaphoreTryAcquire(sem, 100)

            SemaphoreDispose(sem)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result1 = vm.GetGlobalVariable("result1");
        var result2 = vm.GetGlobalVariable("result2");
        var result3 = vm.GetGlobalVariable("result3");
        Assert.True((bool)result1);
        Assert.True((bool)result2);
        Assert.False((bool)result3);
    }

    [Fact]
    public void Semaphore_WithDefer_ReleasesAutomatically()
    {
        // Arrange
        var code = @"
            sem <- SemaphoreCreate(1, 1)

            func criticalSection() -> void {
                SemaphoreAcquire(sem)
                defer SemaphoreRelease(sem)
                PrintLine(""In critical section"")
            }

            criticalSection()
            PrintLine(""After critical section"")
            SemaphoreDispose(sem)
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
    public void Semaphore_ProducerConsumer_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            sem <- SemaphoreCreate(0, 10)
            items <- {}

            func producer() -> void {
                for i in [1~5] {
                    items.Add(i)
                    SemaphoreRelease(sem)
                    Sleep(50)
                }
            }

            func consumer() -> void {
                for i in [1~5] {
                    SemaphoreAcquire(sem)
                    Sleep(30)
                }
            }

            t1 <- spawn(producer)
            t2 <- spawn(consumer)

            t1.Start()
            t2.Start()

            Sleep(500)
            result <- items.Count()
            SemaphoreDispose(sem)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(5, GetIntValue(result));
    }
}
