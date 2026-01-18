using Old8Lang.Bytecode;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.VirtualMachine.Statements;

/// <summary>
/// 虚拟机 select 语句测试
/// 测试 Channel 多路选择功能
/// </summary>
[Collection("Sequential")]
public class VMSelectTests
{
    /// <summary>
    /// 执行虚拟机代码并捕获控制台输出
    /// </summary>
    private string ExecuteVMCode(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        // 编译为字节码
        var compiler = new BytecodeCompiler();
        var bytecodeFile = compiler.Compile(ast);

        // 捕获控制台输出
        var originalOut = Console.Out;
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            // 执行字节码
            var vm = new Bytecode.VirtualMachine(bytecodeFile);
            vm.Execute();

            return stringWriter.ToString().Trim();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact(Skip = "虚拟机 select 语句实现可能不完整")]
    public void SelectStatement_SendOperation_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            ch <- ChannelCreate()

            select {
                case ch <- 100 -> {
                    PrintLine(""sent"")
                }
                default -> {
                    PrintLine(""not ready"")
                }
            }

            ChannelDispose(ch)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Contains("sent", output);
    }

    [Fact(Skip = "虚拟机 select 语句实现可能不完整")]
    public void SelectStatement_ReceiveOperation_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            ch <- ChannelCreate()
            ChannelSend(ch, 42)

            select {
                case val from ch -> {
                    PrintLine(""received: "" + val.ToStr())
                }
                default -> {
                    PrintLine(""not ready"")
                }
            }

            ChannelDispose(ch)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Contains("received: 42", output);
    }

    [Fact(Skip = "虚拟机 select 语句实现可能不完整")]
    public void SelectStatement_DefaultBranch_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            ch1 <- ChannelCreateBounded(1)
            ch2 <- ChannelCreateBounded(1)

            // 填满 ch1，使其无法发送
            ChannelSend(ch1, 1)

            select {
                case ch1 <- 100 -> {
                    PrintLine(""sent to ch1"")
                }
                case val from ch2 -> {
                    PrintLine(""received from ch2"")
                }
                default -> {
                    PrintLine(""no channel ready"")
                }
            }

            ChannelDispose(ch1)
            ChannelDispose(ch2)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Contains("no channel ready", output);
    }

    [Fact(Skip = "虚拟机 select 语句实现可能不完整")]
    public void SelectStatement_MultipleChannels_SelectsFirstReady()
    {
        // Arrange
        var code = @"
            ch1 <- ChannelCreate()
            ch2 <- ChannelCreate()
            ch3 <- ChannelCreate()

            // 预先向 ch2 发送数据
            ChannelSend(ch2, 42)

            select {
                case val from ch1 -> {
                    PrintLine(""received from ch1: "" + val.ToStr())
                }
                case val from ch2 -> {
                    PrintLine(""received from ch2: "" + val.ToStr())
                }
                case val from ch3 -> {
                    PrintLine(""received from ch3: "" + val.ToStr())
                }
                default -> {
                    PrintLine(""no channel ready"")
                }
            }

            ChannelDispose(ch1)
            ChannelDispose(ch2)
            ChannelDispose(ch3)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Contains("received from ch2: 42", output);
    }

    [Fact(Skip = "虚拟机 select 语句实现可能不完整")]
    public void SelectStatement_SendAndReceive_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            ch1 <- ChannelCreate()
            ch2 <- ChannelCreate()

            // 预先向 ch1 发送数据
            ChannelSend(ch1, 100)

            select {
                case ch2 <- 200 -> {
                    PrintLine(""sent to ch2"")
                }
                case val from ch1 -> {
                    PrintLine(""received from ch1: "" + val.ToStr())
                }
            }

            ChannelDispose(ch1)
            ChannelDispose(ch2)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Contains("received from ch1: 100", output);
    }

    [Fact(Skip = "虚拟机 select 语句实现可能不完整")]
    public void SelectStatement_WithoutDefault_BlocksUntilReady()
    {
        // Arrange
        var code = @"
            ch <- ChannelCreate()

            // 在另一个线程中发送数据
            spawn(() -> {
                Sleep(100)
                ChannelSend(ch, 42)
            })

            select {
                case val from ch -> {
                    PrintLine(""received: "" + val.ToStr())
                }
            }

            ChannelDispose(ch)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Contains("received: 42", output);
    }

    [Fact(Skip = "虚拟机 select 语句实现可能不完整")]
    public void SelectStatement_NestedSelect_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            ch1 <- ChannelCreate()
            ch2 <- ChannelCreate()

            ChannelSend(ch1, 1)

            select {
                case val1 from ch1 -> {
                    PrintLine(""outer received: "" + val1.ToStr())
                    ChannelSend(ch2, 2)
                    select {
                        case val2 from ch2 -> {
                            PrintLine(""inner received: "" + val2.ToStr())
                        }
                    }
                }
            }

            ChannelDispose(ch1)
            ChannelDispose(ch2)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Contains("outer received: 1", lines);
        Assert.Contains("inner received: 2", lines);
    }

    [Fact(Skip = "虚拟机 select 语句实现可能不完整")]
    public void SelectStatement_InLoop_ExecutesMultipleTimes()
    {
        // Arrange
        var code = @"
            ch <- ChannelCreate()

            for i in [1~3] {
                ChannelSend(ch, i)
                select {
                    case val from ch -> {
                        PrintLine(""received: "" + val.ToStr())
                    }
                }
            }

            ChannelDispose(ch)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        Assert.Contains("received: 1", lines);
        Assert.Contains("received: 2", lines);
        Assert.Contains("received: 3", lines);
    }

    [Fact(Skip = "虚拟机 select 语句实现可能不完整")]
    public void SelectStatement_WithClosedChannel_HandlesCorrectly()
    {
        // Arrange
        var code = @"
            ch <- ChannelCreate()
            ChannelClose(ch)

            select {
                case val from ch -> {
                    if val == null {
                        PrintLine(""channel closed"")
                    } else {
                        PrintLine(""received: "" + val.ToStr())
                    }
                }
                default -> {
                    PrintLine(""no channel ready"")
                }
            }

            ChannelDispose(ch)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.True(output.Contains("channel closed") || output.Contains("no channel ready"));
    }

    [Fact(Skip = "虚拟机 select 语句实现可能不完整")]
    public void SelectStatement_BoundedChannel_SendBlocks()
    {
        // Arrange
        var code = @"
            ch <- ChannelCreateBounded(1)

            // 填满 channel
            ChannelSend(ch, 1)

            select {
                case ch <- 2 -> {
                    PrintLine(""sent"")
                }
                default -> {
                    PrintLine(""channel full"")
                }
            }

            ChannelDispose(ch)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Contains("channel full", output);
    }

    [Fact(Skip = "虚拟机 select 语句实现可能不完整")]
    public void SelectStatement_WithUsing_DisposesCorrectly()
    {
        // Arrange
        var code = @"
            using ch <- ChannelCreate() {
                ChannelSend(ch, 42)
                select {
                    case val from ch -> {
                        PrintLine(""received: "" + val.ToStr())
                    }
                }
            }
            PrintLine(""channel disposed"")
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Contains("received: 42", lines);
        Assert.Contains("channel disposed", lines);
    }
}
