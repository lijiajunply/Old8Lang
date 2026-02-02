using Old8Lang.Bytecode;
using Old8Lang.Interpreter;
using VM = Old8Lang.Bytecode.VM.VirtualMachine;

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
            }).Start()

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

            // 先发送数据
            spawn(() -> {
                ChannelSend(ch, 100)
            }).Start()

            // 等待数据发送完成
            Sleep(50)

            select {
                case val from ch -> {
                    PrintLine(""Received: "" + val.ToStr())
                }
                default -> {
                    PrintLine(""default"")
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
        // 测试多个 case 的 select 语句，先发送数据再执行 select
        var code = @"
            ch1 <- ChannelCreate()
            ch2 <- ChannelCreate()

            // 先向 ch1 发送数据
            spawn(() -> {
                ChannelSend(ch1, 1)
            }).Start()

            // 等待数据发送完成
            Sleep(50)

            select {
                case val from ch1 -> {
                    PrintLine(""Received from ch1: "" + val.ToStr())
                }
                case val from ch2 -> {
                    PrintLine(""Received from ch2: "" + val.ToStr())
                }
                default -> {
                    PrintLine(""default"")
                }
            }

            Sleep(50)
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
        // 测试混合发送和接收的 select 语句
        // ch2 有接收者等待，所以发送操作可以立即完成
        var code = @"
            ch1 <- ChannelCreate()
            ch2 <- ChannelCreate()

            // 启动一个接收者等待 ch2
            spawn(() -> {
                val <- ChannelReceive(ch2)
                PrintLine(""ch2 received: "" + val.ToStr())
            }).Start()

            // 等待接收者准备好
            Sleep(50)

            select {
                case val from ch1 -> {
                    PrintLine(""Received from ch1: "" + val.ToStr())
                }
                case ch2 <- 456 -> {
                    PrintLine(""Sent to ch2"")
                }
                default -> {
                    PrintLine(""default"")
                }
            }

            Sleep(100)
            ChannelDispose(ch1)
            ChannelDispose(ch2)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        // 由于 ch2 有接收者等待，发送操作应该成功
        Assert.Contains("Sent to ch2", output);
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
            }).Start()

            Sleep(20)

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
            }).Start()

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
        // 简化的生产者-消费者测试
        var code = @"
            ch1 <- ChannelCreate()
            ch2 <- ChannelCreate()
            result <- 0

            // 生产者：发送一个值
            spawn(() -> {
                ChannelSend(ch1, 10)
            }).Start()

            // 等待生产者发送完成
            Sleep(50)

            // 消费者：使用 select 接收并处理
            select {
                case val from ch1 -> {
                    result <- val * 2
                }
                default -> {
                    result <- -1
                }
            }

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
        Assert.Equal(20, result);
    }

    [Fact]
    public void SelectStatement_NestedSelect_ExecutesCorrectly()
    {
        // Arrange
        // 测试嵌套的 select 语句
        var code = @"
            ch1 <- ChannelCreate()
            ch2 <- ChannelCreate()

            // 先发送数据
            spawn(() -> {
                ChannelSend(ch1, 1)
            }).Start()

            // 等待数据发送完成
            Sleep(50)

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
        // 测试多个 channel 的 select 语句
        // 在 select 之前先向 ch2 发送数据，这样 select 可以立即接收
        var code = @"
            ch1 <- ChannelCreate()
            ch2 <- ChannelCreate()
            ch3 <- ChannelCreate()

            // 先向 ch2 发送数据
            spawn(() -> {
                ChannelSend(ch2, 2)
            }).Start()

            // 等待数据发送完成
            Sleep(50)

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
