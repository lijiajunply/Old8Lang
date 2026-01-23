using Old8Lang.Bytecode;
using Old8Lang.Interpreter;
using VM = Old8Lang.Bytecode.VM.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Concurrency;

/// <summary>
/// 虚拟机 Channel 并发原语测试
/// 测试 Channel 的创建、发送、接收、关闭和释放功能
/// </summary>
[Collection("Sequential")]
public class VMConcurrencyChannelTests
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
    public void ChannelCreate_BasicUsage_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            ch <- ChannelCreate()
            result <- ch != null
            ChannelDispose(ch)
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
    public void ChannelCreateBounded_BasicUsage_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            ch <- ChannelCreateBounded(5)
            result <- ch != null
            ChannelDispose(ch)
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
    public void ChannelSendReceive_BasicUsage_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            ch <- ChannelCreate()

            spawn(() -> {
                ChannelSend(ch, 42)
            })

            result <- ChannelReceive(ch)
            ChannelDispose(ch)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(42, result);
    }

    [Fact]
    public void ChannelTrySend_Success_ReturnsTrue()
    {
        // Arrange
        var code = @"
            ch <- ChannelCreateBounded(1)
            result <- ChannelTrySend(ch, 100, 1000)
            ChannelDispose(ch)
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
    public void ChannelTryReceive_Success_ReturnsValue()
    {
        // Arrange
        var code = @"
            ch <- ChannelCreate()

            spawn(() -> {
                Sleep(50)
                ChannelSend(ch, 123)
            })

            result <- ChannelTryReceive(ch, 1000)
            ChannelDispose(ch)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(123, result);
    }

    [Fact]
    public void ChannelTryReceive_Timeout_ReturnsNull()
    {
        // Arrange
        var code = @"
            ch <- ChannelCreate()
            result <- ChannelTryReceive(ch, 100)
            isNull <- result == null
            ChannelDispose(ch)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var isNull = vm.GetGlobalVariable("isNull");
        Assert.NotNull(isNull);
        Assert.True((bool)isNull);
    }

    [Fact]
    public void ChannelClose_BasicUsage_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            ch <- ChannelCreate()
            ChannelClose(ch)
            PrintLine(""Channel closed"")
            ChannelDispose(ch)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Channel closed", output);
    }

    [Fact]
    public void Channel_ProducerConsumer_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            ch <- ChannelCreate()
            results <- {}

            func producer() -> void {
                for i in [1~5] {
                    ChannelSend(ch, i)
                }
                ChannelClose(ch)
            }

            func consumer() -> void {
                for i in [1~5] {
                    val <- ChannelReceive(ch)
                    results.Add(val)
                }
            }

            spawn(producer)
            spawn(consumer)

            Sleep(500)

            result <- results.Count()
            ChannelDispose(ch)
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
    public void Channel_BoundedCapacity_BlocksWhenFull()
    {
        // Arrange
        var code = @"
            ch <- ChannelCreateBounded(2)

            ChannelSend(ch, 1)
            ChannelSend(ch, 2)

            result <- ChannelTrySend(ch, 3, 100)
            ChannelDispose(ch)
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
    public void Channel_WithUsing_DisposesAutomatically()
    {
        // Arrange
        var code = @"
            using ch <- ChannelCreate() {
                ChannelSend(ch, 42)
                result <- ChannelReceive(ch)
            }
            PrintLine(""Disposed"")
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Disposed", output);
    }

    [Fact]
    public void Channel_MultipleProducers_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            ch <- ChannelCreate()
            results <- {}

            func producer(id:int) -> void {
                for i in [1~3] {
                    ChannelSend(ch, id * 10 + i)
                }
            }

            spawn(() -> producer(1))
            spawn(() -> producer(2))
            spawn(() -> producer(3))

            for i in [1~9] {
                val <- ChannelReceive(ch)
                results.Add(val)
            }

            result <- results.Count()
            ChannelDispose(ch)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(9, result);
    }

    [Fact]
    public void Channel_MultipleConsumers_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            ch <- ChannelCreate()
            counter <- AtomicIntCreate(0)

            func producer() -> void {
                for i in [1~10] {
                    ChannelSend(ch, i)
                }
            }

            func consumer() -> void {
                for i in [1~5] {
                    val <- ChannelReceive(ch)
                    AtomicIntIncrement(counter)
                }
            }

            spawn(producer)
            spawn(consumer)
            spawn(consumer)

            Sleep(500)

            result <- AtomicIntGet(counter)
            ChannelDispose(ch)
            AtomicIntDispose(counter)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(10, result);
    }

    [Fact]
    public void Channel_SendDifferentTypes_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            ch <- ChannelCreate()

            spawn(() -> {
                ChannelSend(ch, 42)
                ChannelSend(ch, ""hello"")
                ChannelSend(ch, true)
            })

            result1 <- ChannelReceive(ch)
            result2 <- ChannelReceive(ch)
            result3 <- ChannelReceive(ch)

            ChannelDispose(ch)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result1 = vm.GetGlobalVariable("result1");
        var result2 = vm.GetGlobalVariable("result2");
        var result3 = vm.GetGlobalVariable("result3");
        Assert.Equal(42, result1);
        Assert.Equal("hello", result2);
        Assert.True((bool)result3);
    }

    [Fact]
    public void Channel_Pipeline_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            ch1 <- ChannelCreate()
            ch2 <- ChannelCreate()
            results <- {}

            func stage1() -> void {
                for i in [1~5] {
                    ChannelSend(ch1, i)
                }
                ChannelClose(ch1)
            }

            func stage2() -> void {
                for i in [1~5] {
                    val <- ChannelReceive(ch1)
                    ChannelSend(ch2, val * 2)
                }
                ChannelClose(ch2)
            }

            func stage3() -> void {
                for i in [1~5] {
                    val <- ChannelReceive(ch2)
                    results.Add(val)
                }
            }

            spawn(stage1)
            spawn(stage2)
            spawn(stage3)

            Sleep(500)

            result <- results.Count()
            ChannelDispose(ch1)
            ChannelDispose(ch2)
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
}
