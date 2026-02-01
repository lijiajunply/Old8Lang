using Old8Lang.Bytecode;
using Old8Lang.Interpreter;
using VM = Old8Lang.Bytecode.VM.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Concurrency;

/// <summary>
/// 虚拟机 CyclicBarrier 并发原语测试
/// 测试 CyclicBarrier 的创建、等待和释放功能
/// </summary>
[Collection("Sequential")]
public class VMConcurrencyCyclicBarrierTests
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
    public void CyclicBarrierCreate_BasicUsage_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            barrier <- CyclicBarrierCreate(3)
            result <- barrier != null
            CyclicBarrierDispose(barrier)
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
    public void CyclicBarrierGetParticipantCount_BasicUsage_ReturnsCount()
    {
        // Arrange
        var code = @"
            barrier <- CyclicBarrierCreate(5)
            result <- CyclicBarrierGetParticipantCount(barrier)
            CyclicBarrierDispose(barrier)
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
    public void CyclicBarrierAwait_AllParticipantsArrive_Unblocks()
    {
        // Arrange
        var code = @"
            barrier <- CyclicBarrierCreate(3)

            func worker(id:int) -> void {
                PrintLine(""Worker "" + id.ToStr() + "" waiting"")
                CyclicBarrierAwait(barrier)
                PrintLine(""Worker "" + id.ToStr() + "" passed"")
            }

            t1 <- spawn(() -> worker(1))
            t2 <- spawn(() -> worker(2))
            t3 <- spawn(() -> worker(3))

            t1.Start()
            t2.Start()
            t3.Start()

            Sleep(500)
            CyclicBarrierDispose(barrier)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Contains("Worker 1 waiting", output);
        Assert.Contains("Worker 2 waiting", output);
        Assert.Contains("Worker 3 waiting", output);
        Assert.Contains("Worker 1 passed", output);
        Assert.Contains("Worker 2 passed", output);
        Assert.Contains("Worker 3 passed", output);
    }

    [Fact]
    public void CyclicBarrierAwaitTimeout_Success_ReturnsTrue()
    {
        // Arrange
        var code = @"
            barrier <- CyclicBarrierCreate(2)

            t <- spawn(() -> {
                Sleep(50)
                CyclicBarrierAwait(barrier)
            })

            t.Start()

            result <- CyclicBarrierAwaitTimeout(barrier, 1000)
            CyclicBarrierDispose(barrier)
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
    public void CyclicBarrierAwaitTimeout_Timeout_ReturnsFalse()
    {
        // Arrange
        var code = @"
            barrier <- CyclicBarrierCreate(2)
            result <- CyclicBarrierAwaitTimeout(barrier, 100)
            CyclicBarrierDispose(barrier)
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
    public void CyclicBarrierGetWaitingCount_BasicUsage_ReturnsCount()
    {
        // Arrange
        var code = @"
            barrier <- CyclicBarrierCreate(3)

            t1 <- spawn(() -> {
                CyclicBarrierAwait(barrier)
            })
            
            t1.Start()
            Sleep(100)
            result <- CyclicBarrierGetWaitingCount(barrier)

            t2 <- spawn(() -> {
                CyclicBarrierAwait(barrier)
            })
            t3 <- spawn(() -> {
                CyclicBarrierAwait(barrier)
            })

            t2.Start()
            t3.Start()

            Sleep(200)
            CyclicBarrierDispose(barrier)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(1, result);
    }

    [Fact]
    public void CyclicBarrier_Reusable_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            barrier <- CyclicBarrierCreate(2)
            counter <- AtomicIntCreate(0)

            func worker() -> void {
                for i in [1~3] {
                    AtomicIntIncrement(counter)
                    CyclicBarrierAwait(barrier)
                }
            }

            t1 <- spawn(worker)
            t2 <- spawn(worker)

            t1.Start()
            t2.Start()

            Sleep(500)

            result <- AtomicIntGet(counter)
            CyclicBarrierDispose(barrier)
            AtomicIntDispose(counter)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(6, result); // 2 workers * 3 iterations = 6
    }

    [Fact]
    public void CyclicBarrier_WithUsing_DisposesAutomatically()
    {
        // Arrange
        var code = @"
            using barrier <- CyclicBarrierCreate(1) {
                CyclicBarrierAwait(barrier)
                PrintLine(""Passed"")
            }
            PrintLine(""Disposed"")
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("Passed", lines[0]);
        Assert.Equal("Disposed", lines[1]);
    }

    [Fact]
    public void CyclicBarrier_PhaseCoordination_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            barrier <- CyclicBarrierCreate(3)
            results <- {}

            func worker(id:int) -> void {
                results.Add(""Phase1-"" + id.ToStr())
                CyclicBarrierAwait(barrier)

                results.Add(""Phase2-"" + id.ToStr())
                CyclicBarrierAwait(barrier)

                results.Add(""Phase3-"" + id.ToStr())
            }

            t1 <- spawn(() -> worker(1))
            t2 <- spawn(() -> worker(2))
            t3 <- spawn(() -> worker(3))

            t1.Start()
            t2.Start()
            t3.Start()

            Sleep(500)

            result <- results.Count()
            CyclicBarrierDispose(barrier)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(9, result); // 3 workers * 3 phases = 9
    }

    [Fact]
    public void CyclicBarrier_SingleParticipant_PassesImmediately()
    {
        // Arrange
        var code = @"
            barrier <- CyclicBarrierCreate(1)
            PrintLine(""Before await"")
            CyclicBarrierAwait(barrier)
            PrintLine(""After await"")
            CyclicBarrierDispose(barrier)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("Before await", lines[0]);
        Assert.Equal("After await", lines[1]);
    }

    [Fact]
    public void CyclicBarrier_StressTest_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            barrier <- CyclicBarrierCreate(5)
            counter <- AtomicIntCreate(0)

            func worker() -> void {
                for i in [1~10] {
                    AtomicIntIncrement(counter)
                    CyclicBarrierAwait(barrier)
                }
            }

            for i in [1~5] {
                t <- spawn(worker)
                t.Start()
            }

            Sleep(1000)

            result <- AtomicIntGet(counter)
            CyclicBarrierDispose(barrier)
            AtomicIntDispose(counter)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(50, result); // 5 workers * 10 iterations = 50
    }
}
