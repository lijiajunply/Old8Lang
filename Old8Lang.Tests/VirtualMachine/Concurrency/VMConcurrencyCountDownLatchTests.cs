using Old8Lang.Bytecode;
using Old8Lang.Interpreter;
using VM = Old8Lang.Bytecode.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Concurrency;

/// <summary>
/// 虚拟机 CountDownLatch 并发原语测试
/// 测试 CountDownLatch 的创建、倒计时、等待和释放功能
/// </summary>
[Collection("Sequential")]
public class VMConcurrencyCountDownLatchTests
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
    public void CountDownLatchCreate_BasicUsage_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            latch <- CountDownLatchCreate(3)
            result <- latch != null
            CountDownLatchDispose(latch)
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
    public void CountDownLatchGetCount_BasicUsage_ReturnsInitialCount()
    {
        // Arrange
        var code = @"
            latch <- CountDownLatchCreate(5)
            result <- CountDownLatchGetCount(latch)
            CountDownLatchDispose(latch)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(5, result);
    }

    [Fact]
    public void CountDownLatchCountDown_BasicUsage_DecrementsCount()
    {
        // Arrange
        var code = @"
            latch <- CountDownLatchCreate(3)
            CountDownLatchCountDown(latch)
            result <- CountDownLatchGetCount(latch)
            CountDownLatchDispose(latch)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(2, result);
    }

    [Fact]
    public void CountDownLatchWait_CountReachesZero_Unblocks()
    {
        // Arrange
        var code = @"
            latch <- CountDownLatchCreate(2)

            spawn(() -> {
                Sleep(100)
                CountDownLatchCountDown(latch)
                Sleep(100)
                CountDownLatchCountDown(latch)
            })

            PrintLine(""Waiting..."")
            CountDownLatchWait(latch)
            PrintLine(""Unblocked"")

            CountDownLatchDispose(latch)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("Waiting...", lines[0]);
        Assert.Equal("Unblocked", lines[1]);
    }

    [Fact]
    public void CountDownLatchWaitTimeout_Success_ReturnsTrue()
    {
        // Arrange
        var code = @"
            latch <- CountDownLatchCreate(1)

            spawn(() -> {
                Sleep(50)
                CountDownLatchCountDown(latch)
            })

            result <- CountDownLatchWaitTimeout(latch, 1000)
            CountDownLatchDispose(latch)
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
    public void CountDownLatchWaitTimeout_Timeout_ReturnsFalse()
    {
        // Arrange
        var code = @"
            latch <- CountDownLatchCreate(1)
            result <- CountDownLatchWaitTimeout(latch, 100)
            CountDownLatchDispose(latch)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.False((bool)result);
    }

    [Fact]
    public void CountDownLatch_MultipleThreads_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            latch <- CountDownLatchCreate(3)

            func worker(id:int) -> void {
                Sleep(50 * id)
                PrintLine(""Worker "" + id.ToStr() + "" done"")
                CountDownLatchCountDown(latch)
            }

            spawn(() -> worker(1))
            spawn(() -> worker(2))
            spawn(() -> worker(3))

            PrintLine(""Waiting for workers..."")
            CountDownLatchWait(latch)
            PrintLine(""All workers done"")

            CountDownLatchDispose(latch)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Contains("Waiting for workers...", output);
        Assert.Contains("All workers done", output);
        Assert.Contains("Worker 1 done", output);
        Assert.Contains("Worker 2 done", output);
        Assert.Contains("Worker 3 done", output);
    }

    [Fact]
    public void CountDownLatch_WithUsing_DisposesAutomatically()
    {
        // Arrange
        var code = @"
            using latch <- CountDownLatchCreate(1) {
                CountDownLatchCountDown(latch)
                CountDownLatchWait(latch)
                PrintLine(""Done"")
            }
            PrintLine(""Disposed"")
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("Done", lines[0]);
        Assert.Equal("Disposed", lines[1]);
    }

    [Fact]
    public void CountDownLatch_CountDownBelowZero_StaysAtZero()
    {
        // Arrange
        var code = @"
            latch <- CountDownLatchCreate(1)
            CountDownLatchCountDown(latch)
            CountDownLatchCountDown(latch)
            CountDownLatchCountDown(latch)
            result <- CountDownLatchGetCount(latch)
            CountDownLatchDispose(latch)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(0, result);
    }

    [Fact]
    public void CountDownLatch_StartGate_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            startGate <- CountDownLatchCreate(1)
            counter <- AtomicIntCreate(0)

            func worker() -> void {
                CountDownLatchWait(startGate)
                AtomicIntIncrement(counter)
            }

            spawn(worker)
            spawn(worker)
            spawn(worker)

            Sleep(100)
            CountDownLatchCountDown(startGate)
            Sleep(100)

            result <- AtomicIntGet(counter)
            CountDownLatchDispose(startGate)
            AtomicIntDispose(counter)
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
    public void CountDownLatch_MultipleWaiters_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            latch <- CountDownLatchCreate(1)
            results <- {}

            func waiter(id:int) -> void {
                CountDownLatchWait(latch)
                results.Add(id)
            }

            spawn(() -> waiter(1))
            spawn(() -> waiter(2))
            spawn(() -> waiter(3))

            Sleep(100)
            CountDownLatchCountDown(latch)
            Sleep(100)

            result <- results.Count()
            CountDownLatchDispose(latch)
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
}
