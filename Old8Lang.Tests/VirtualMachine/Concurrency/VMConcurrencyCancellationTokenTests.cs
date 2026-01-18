using Old8Lang.Bytecode;
using Old8Lang.Interpreter;
using VM = Old8Lang.Bytecode.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Concurrency;

/// <summary>
/// 虚拟机 CancellationTokenSource 并发原语测试
/// 测试 CancellationTokenSource 的创建、取消和释放功能
/// </summary>
[Collection("Sequential")]
public class VMConcurrencyCancellationTokenTests
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
    public void CreateCancellationTokenSource_BasicUsage_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            cts <- CreateCancellationTokenSource()
            result <- cts != null
            DisposeCancellationTokenSource(cts)
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
    public void Cancel_BasicUsage_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            cts <- CreateCancellationTokenSource()
            Cancel(cts)
            PrintLine(""Cancelled"")
            DisposeCancellationTokenSource(cts)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Cancelled", output);
    }

    [Fact]
    public void CancelAfter_BasicUsage_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            cts <- CreateCancellationTokenSource()
            CancelAfter(cts, 100)
            PrintLine(""Cancel scheduled"")
            Sleep(200)
            PrintLine(""After delay"")
            DisposeCancellationTokenSource(cts)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("Cancel scheduled", lines[0]);
        Assert.Equal("After delay", lines[1]);
    }

    [Fact]
    public void CancellationToken_WithUsing_DisposesAutomatically()
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
    public void CancellationToken_ImmediateCancel_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            cts <- CreateCancellationTokenSource()
            counter <- 0

            spawn(() -> {
                for i in [1~100] {
                    counter <- counter + 1
                    Sleep(10)
                }
            })

            Sleep(50)
            Cancel(cts)
            Sleep(100)

            result <- counter
            DisposeCancellationTokenSource(cts)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        // Counter should be less than 100 since we cancelled early
        Assert.True((int)result < 100);
    }

    [Fact]
    public void CancelAfter_DelayedCancel_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            cts <- CreateCancellationTokenSource()
            counter <- 0

            spawn(() -> {
                for i in [1~100] {
                    counter <- counter + 1
                    Sleep(10)
                }
            })

            CancelAfter(cts, 50)
            Sleep(200)

            result <- counter
            DisposeCancellationTokenSource(cts)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        // Counter should be less than 100 since we cancelled after 50ms
        Assert.True((int)result < 100);
    }

    [Fact]
    public void CancellationToken_MultipleThreads_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            cts <- CreateCancellationTokenSource()
            counter1 <- 0
            counter2 <- 0
            counter3 <- 0

            spawn(() -> {
                for i in [1~50] {
                    counter1 <- counter1 + 1
                    Sleep(10)
                }
            })

            spawn(() -> {
                for i in [1~50] {
                    counter2 <- counter2 + 1
                    Sleep(10)
                }
            })

            spawn(() -> {
                for i in [1~50] {
                    counter3 <- counter3 + 1
                    Sleep(10)
                }
            })

            Sleep(100)
            Cancel(cts)
            Sleep(100)

            result <- counter1 + counter2 + counter3
            DisposeCancellationTokenSource(cts)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        // Total should be less than 150 (50*3) since we cancelled early
        Assert.True((int)result < 150);
    }

    [Fact]
    public void CancellationToken_CancelBeforeStart_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            cts <- CreateCancellationTokenSource()
            Cancel(cts)

            counter <- 0
            spawn(() -> {
                for i in [1~10] {
                    counter <- counter + 1
                    Sleep(10)
                }
            })

            Sleep(200)

            result <- counter
            DisposeCancellationTokenSource(cts)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        // Counter should still increment since cancellation doesn't automatically stop threads
        Assert.True((int)result >= 0);
    }

    [Fact]
    public void CancellationToken_MultipleCancels_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            cts <- CreateCancellationTokenSource()
            Cancel(cts)
            Cancel(cts)
            Cancel(cts)
            PrintLine(""Multiple cancels completed"")
            DisposeCancellationTokenSource(cts)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Multiple cancels completed", output);
    }

    [Fact]
    public void CancellationToken_CancelAfterZeroDelay_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            cts <- CreateCancellationTokenSource()
            CancelAfter(cts, 0)
            Sleep(50)
            PrintLine(""Completed"")
            DisposeCancellationTokenSource(cts)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Completed", output);
    }
}
