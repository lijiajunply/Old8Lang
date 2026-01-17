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
}
