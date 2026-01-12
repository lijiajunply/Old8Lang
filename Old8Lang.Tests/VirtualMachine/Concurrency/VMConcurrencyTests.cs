using Old8Lang.Bytecode;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.VirtualMachine.Concurrency;

/// <summary>
/// 虚拟机并发原语测试
/// 测试虚拟机执行Mutex、Channel、Semaphore等并发原语的正确性
/// </summary>
[Collection("Sequential")]
public class VMConcurrencyTests
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

    #region Mutex Tests

    [Fact]
    public void MutexCreate_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            mutex <- MutexCreate()
            PrintLine(""Mutex created: "" + mutex.ToStr())
            MutexDispose(mutex)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.StartsWith("Mutex created:", output);
    }

    [Fact]
    public void MutexLockUnlock_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            mutex <- MutexCreate()
            MutexLock(mutex)
            PrintLine(""Lock acquired"")
            MutexUnlock(mutex)
            PrintLine(""Lock released"")
            MutexDispose(mutex)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("Lock acquired", lines[0]);
        Assert.Equal("Lock released", lines[1]);
    }

    #endregion

    #region Channel Tests

    [Fact]
    public void ChannelCreate_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            ch <- ChannelCreate()
            PrintLine(""Channel created: "" + ch.ToStr())
            ChannelClose(ch)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.StartsWith("Channel created:", output);
    }

    [Fact]
    public void ChannelSendReceive_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            ch <- ChannelCreate()
            ChannelSend(ch, 42)
            value <- ChannelReceive(ch)
            PrintLine(""Received: "" + value.ToStr())
            ChannelClose(ch)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Received: 42", output);
    }

    [Fact]
    public void ChannelSendReceiveString_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            ch <- ChannelCreate()
            ChannelSend(ch, ""Hello"")
            value <- ChannelReceive(ch)
            PrintLine(""Received: "" + value)
            ChannelClose(ch)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Received: Hello", output);
    }

    #endregion

    #region Semaphore Tests

    [Fact]
    public void SemaphoreCreate_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            sem <- SemaphoreCreate(1, 3)
            PrintLine(""Semaphore created: "" + sem.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.StartsWith("Semaphore created:", output);
    }

    [Fact]
    public void SemaphoreAcquireRelease_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            sem <- SemaphoreCreate(1, 1)
            SemaphoreAcquire(sem)
            PrintLine(""Semaphore acquired"")
            SemaphoreRelease(sem)
            PrintLine(""Semaphore released"")
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("Semaphore acquired", lines[0]);
        Assert.Equal("Semaphore released", lines[1]);
    }

    #endregion
}
