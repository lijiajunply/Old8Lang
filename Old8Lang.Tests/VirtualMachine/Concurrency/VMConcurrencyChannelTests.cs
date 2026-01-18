using Old8Lang.Bytecode;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.VirtualMachine.Concurrency;

/// <summary>
/// 虚拟机 Channel 并发原语测试
/// 测试 Channel 的创建、发送、接收、超时和资源管理
/// </summary>
[Collection("Sequential")]
public class VMConcurrencyChannelTests
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
            var vm = new Old8Lang.Bytecode.VirtualMachine(bytecodeFile);
            vm.Execute();

            return stringWriter.ToString().Trim();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    #region 基本功能测试

    [Fact]
    public void ChannelCreate_BasicUsage_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            ch <- ChannelCreate()
            PrintLine(""Channel created: "" + (ch > 0).ToStr())
            ChannelDispose(ch)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Channel created: true", output);
    }

    [Fact]
    public void ChannelCreateBounded_BasicUsage_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            ch <- ChannelCreateBounded(5)
            PrintLine(""Bounded channel created: "" + (ch > 0).ToStr())
            ChannelDispose(ch)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Bounded channel created: true", output);
    }

    [Fact]
    public void ChannelSend_BasicUsage_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            ch <- ChannelCreate()

            ChannelSend(ch, 42)
            value <- ChannelReceive(ch)
            PrintLine(""Received: "" + value.ToStr())

            ChannelDispose(ch)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Received: 42", output);
    }

    [Fact]
    public void ChannelReceive_BasicUsage_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            ch <- ChannelCreate()

            ChannelSend(ch, ""Hello"")
            value <- ChannelReceive(ch)
            PrintLine(""Received: "" + value)

            ChannelDispose(ch)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Received: Hello", output);
    }

    [Fact]
    public void ChannelTrySend_Success_ReturnsTrue()
    {
        // Arrange
        var code = @"
            ch <- ChannelCreateBounded(1)
            result <- ChannelTrySend(ch, 100, 1000)
            PrintLine(""TrySend result: "" + result.ToStr())
            ChannelDispose(ch)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("TrySend result: true", output);
    }

    [Fact]
    public void ChannelTryReceive_Success_ReturnsValue()
    {
        // Arrange
        var code = @"
            ch <- ChannelCreate()
            ChannelSend(ch, 123)
            value <- ChannelTryReceive(ch, 1000)
            PrintLine(""TryReceive value: "" + value.ToStr())
            ChannelDispose(ch)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("TryReceive value: 123", output);
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
    public void ChannelDispose_AfterUsage_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            ch <- ChannelCreate()
            ChannelSend(ch, 1)
            ChannelReceive(ch)
            ChannelDispose(ch)
            PrintLine(""Channel disposed"")
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Channel disposed", output);
    }

    #endregion

    #region 超时场景测试

    [Fact]
    public void ChannelTrySend_Timeout_ReturnsFalse()
    {
        // Arrange
        var code = @"
            ch <- ChannelCreateBounded(1)

            // 填满 channel
            ChannelSend(ch, 1)

            // 尝试再次发送（应该超时）
            result <- ChannelTrySend(ch, 2, 100)
            PrintLine(""TrySend timeout result: "" + result.ToStr())

            ChannelDispose(ch)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("TrySend timeout result: false", output);
    }

    [Fact]
    public void ChannelTryReceive_Timeout_ReturnsNull()
    {
        // Arrange
        var code = @"
            ch <- ChannelCreate()

            // 尝试从空 channel 接收（应该超时）
            value <- ChannelTryReceive(ch, 100)
            PrintLine(""TryReceive timeout result: "" + (value == null).ToStr())

            ChannelDispose(ch)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("TryReceive timeout result: true", output);
    }

    [Fact]
    public void ChannelTrySend_ZeroTimeout_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            ch <- ChannelCreateBounded(1)
            result <- ChannelTrySend(ch, 42, 0)
            PrintLine(""TrySend with zero timeout: "" + result.ToStr())
            ChannelDispose(ch)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("TrySend with zero timeout: true", output);
    }

    #endregion

    #region 异常安全性测试

    [Fact]
    public void Channel_WithUsingStatement_DisposesCorrectly()
    {
        // Arrange
        var code = @"
            using ch <- ChannelCreate() {
                ChannelSend(ch, ""test"")
                value <- ChannelReceive(ch)
                PrintLine(""Value: "" + value)
            }
            PrintLine(""After using block"")
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("Value: test", lines[0]);
        Assert.Equal("After using block", lines[1]);
    }

    [Fact]
    public void Channel_InTryCatchFinally_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            ch <- ChannelCreate()
            try {
                ChannelSend(ch, 100)
                value <- ChannelReceive(ch)
                PrintLine(""Received in try: "" + value.ToStr())
            } catch (e) {
                PrintLine(""Error: "" + e)
            } finally {
                ChannelClose(ch)
                PrintLine(""Channel closed in finally"")
                ChannelDispose(ch)
            }
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("Received in try: 100", lines[0]);
        Assert.Equal("Channel closed in finally", lines[1]);
    }

    #endregion

    #region 边界条件测试

    [Fact]
    public void ChannelCreateBounded_WithCapacityOne_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            ch <- ChannelCreateBounded(1)

            // 发送一个值（应该成功）
            result1 <- ChannelTrySend(ch, 1, 100)
            PrintLine(""First send: "" + result1.ToStr())

            // 尝试发送第二个值（应该失败）
            result2 <- ChannelTrySend(ch, 2, 100)
            PrintLine(""Second send: "" + result2.ToStr())

            // 接收值后再发送（应该成功）
            ChannelReceive(ch)
            result3 <- ChannelTrySend(ch, 3, 100)
            PrintLine(""Third send: "" + result3.ToStr())

            ChannelDispose(ch)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        Assert.Equal("First send: true", lines[0]);
        Assert.Equal("Second send: false", lines[1]);
        Assert.Equal("Third send: true", lines[2]);
    }

    [Fact]
    public void Channel_SendReceiveMultipleTimes_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            ch <- ChannelCreate()

            for i in [0~<5] {
                ChannelSend(ch, i)
                value <- ChannelReceive(ch)
                PrintLine(""Iteration "" + i.ToStr() + "": "" + value.ToStr())
            }

            ChannelDispose(ch)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(5, lines.Length);
        for (int i = 0; i < 5; i++)
        {
            Assert.Equal($"Iteration {i}: {i}", lines[i]);
        }
    }

    [Fact]
    public void Channel_SendDifferentTypes_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            ch <- ChannelCreate()

            ChannelSend(ch, 42)
            ChannelSend(ch, ""Hello"")
            ChannelSend(ch, 3.14)
            ChannelSend(ch, true)

            v1 <- ChannelReceive(ch)
            v2 <- ChannelReceive(ch)
            v3 <- ChannelReceive(ch)
            v4 <- ChannelReceive(ch)

            PrintLine(v1.ToStr() + "", "" + v2 + "", "" + v3.ToStr() + "", "" + v4.ToStr())
            ChannelDispose(ch)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("42, Hello, 3.14, True", output);
    }

    #endregion
}
