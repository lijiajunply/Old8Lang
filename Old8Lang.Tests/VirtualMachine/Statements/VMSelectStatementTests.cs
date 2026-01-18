using Old8Lang.Bytecode;
using Old8Lang.Interpreter;
using VM = Old8Lang.Bytecode.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Statements;

/// <summary>
/// 虚拟机 Select 语句测试
/// 测试 Select 语句的发送操作、接收操作、默认分支和多路复用功能
/// </summary>
[Collection("Sequential")]
public class VMSelectStatementTests
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
    public void SelectStatement_SendOperation_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            ch <- ChannelCreate()

            spawn(() -> {
                val <- ChannelReceive(ch)
                PrintLine(""Received: "" + val.ToStr())
            })

            Sleep(50)

            select {
                case ch <- 42 -> {
                    PrintLine(""Sent 42"")
                }
            }

            Sleep(100)
            ChannelDispose(ch)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Contains("Sent 42", output);
        Assert.Contains("Received: 42", output);
    }

    [Fact]
    public void SelectStatement_ReceiveOperation_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            ch <- ChannelCreate()

            spawn(() -> {
                Sleep(50)
                ChannelSend(ch, 100)
            })

            select {
                case val from ch -> {
                    PrintLine(""Received: "" + val.ToStr())
                }
            }

            ChannelDispose(ch)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Contains("Received: 100", output);
    }

    [Fact]
    public void SelectStatement_DefaultBranch_ExecutesWhenNoChannelReady()
    {
        // Arrange
        var code = @"
            ch <- ChannelCreate()

            select {
                case val from ch -> {
                    PrintLine(""Received: "" + val.ToStr())
                }
                default -> {
                    PrintLine(""No channel ready"")
                }
            }

            ChannelDispose(ch)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("No channel ready", output);
    }

    [Fact]
    public void SelectStatement_MultipleCases_ExecutesFirstReady()
    {
        // Arrange
        var code = @"
            ch1 <- ChannelCreate()
            ch2 <- ChannelCreate()

            spawn(() -> {
                Sleep(50)
                ChannelSend(ch1, 1)
            })

            spawn(() -> {
                Sleep(100)
                ChannelSend(ch2, 2)
            })

            select {
                case val from ch1 -> {
                    PrintLine(""Received from ch1: "" + val.ToStr())
                }
                case val from ch2 -> {
                    PrintLine(""Received from ch2: "" + val.ToStr())
                }
            }

            Sleep(150)
            ChannelDispose(ch1)
            ChannelDispose(ch2)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Contains("Received from ch1: 1", output);
    }

    [Fact]
    public void SelectStatement_SendAndReceive_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            ch1 <- ChannelCreate()
            ch2 <- ChannelCreate()

            spawn(() -> {
                Sleep(50)
                ChannelSend(ch1, 123)
            })

            spawn(() -> {
                val <- ChannelReceive(ch2)
                PrintLine(""ch2 received: "" + val.ToStr())
            })

            select {
                case val from ch1 -> {
                    PrintLine(""Received from ch1: "" + val.ToStr())
                }
                case ch2 <- 456 -> {
                    PrintLine(""Sent to ch2"")
                }
            }

            Sleep(100)
            ChannelDispose(ch1)
            ChannelDispose(ch2)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        // Either receive from ch1 or send to ch2 should execute
        Assert.True(output.Contains("Received from ch1: 123") || output.Contains("Sent to ch2"));
    }

    [Fact]
    public void SelectStatement_InLoop_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            ch <- ChannelCreate()
            results <- {}

            spawn(() -> {
                for i in [1~5] {
                    ChannelSend(ch, i)
                    Sleep(20)
                }
                ChannelClose(ch)
            })

            for i in [1~5] {
                select {
                    case val from ch -> {
                        results.Add(val)
                    }
                    default -> {
                        Sleep(10)
                    }
                }
            }

            Sleep(200)
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
        Assert.True((int)result >= 1); // At least some values should be received
    }

    [Fact]
    public void SelectStatement_WithTimeout_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            ch <- ChannelCreate()
            timeout <- false

            spawn(() -> {
                Sleep(200)
                ChannelSend(ch, 42)
            })

            select {
                case val from ch -> {
                    PrintLine(""Received: "" + val.ToStr())
                }
                default -> {
                    timeout <- true
                    PrintLine(""Timeout"")
                }
            }

            result <- timeout
            Sleep(250)
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
    public void SelectStatement_ProducerConsumer_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            ch1 <- ChannelCreate()
            ch2 <- ChannelCreate()
            done <- false

            func producer() -> void {
                for i in [1~3] {
                    ChannelSend(ch1, i)
                    Sleep(30)
                }
            }

            func consumer() -> void {
                while !done {
                    select {
                        case val from ch1 -> {
                            ChannelSend(ch2, val * 2)
                        }
                        default -> {
                            Sleep(10)
                        }
                    }
                }
            }

            spawn(producer)
            spawn(consumer)

            results <- {}
            for i in [1~3] {
                val <- ChannelReceive(ch2)
                results.Add(val)
            }

            done <- true
            Sleep(100)

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
        Assert.Equal(3, result);
    }

    [Fact]
    public void SelectStatement_NestedSelect_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            ch1 <- ChannelCreate()
            ch2 <- ChannelCreate()

            spawn(() -> {
                Sleep(50)
                ChannelSend(ch1, 1)
            })

            select {
                case val from ch1 -> {
                    PrintLine(""Outer received: "" + val.ToStr())
                    select {
                        case ch2 <- val * 2 -> {
                            PrintLine(""Inner sent: "" + (val * 2).ToStr())
                        }
                        default -> {
                            PrintLine(""Inner default"")
                        }
                    }
                }
                default -> {
                    PrintLine(""Outer default"")
                }
            }

            Sleep(100)
            ChannelDispose(ch1)
            ChannelDispose(ch2)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Contains("Outer received: 1", output);
    }

    [Fact]
    public void SelectStatement_MultipleChannels_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            ch1 <- ChannelCreate()
            ch2 <- ChannelCreate()
            ch3 <- ChannelCreate()

            spawn(() -> {
                Sleep(30)
                ChannelSend(ch2, 2)
            })

            select {
                case val from ch1 -> {
                    PrintLine(""ch1: "" + val.ToStr())
                }
                case val from ch2 -> {
                    PrintLine(""ch2: "" + val.ToStr())
                }
                case val from ch3 -> {
                    PrintLine(""ch3: "" + val.ToStr())
                }
                default -> {
                    PrintLine(""default"")
                }
            }

            Sleep(100)
            ChannelDispose(ch1)
            ChannelDispose(ch2)
            ChannelDispose(ch3)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Contains("ch2: 2", output);
    }
}
