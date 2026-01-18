using Old8Lang.Bytecode;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.VirtualMachine.Concurrency;

/// <summary>
/// 虚拟机 CyclicBarrier 并发原语测试
/// 测试 CyclicBarrier 的创建、等待、超时和资源管理
/// </summary>
[Collection("Sequential")]
public class VMConcurrencyCyclicBarrierTests
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
    public void CyclicBarrierCreate_BasicUsage_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            barrier <- CyclicBarrierCreate(3)
            PrintLine(""Barrier created: "" + (barrier > 0).ToStr())
            CyclicBarrierDispose(barrier)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Barrier created: true", output);
    }

    [Fact]
    public void CyclicBarrierAwait_SingleParticipant_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            barrier <- CyclicBarrierCreate(1)

            PrintLine(""Before barrier"")
            CyclicBarrierAwait(barrier)
            PrintLine(""After barrier"")

            CyclicBarrierDispose(barrier)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("Before barrier", lines[0]);
        Assert.Equal("After barrier", lines[1]);
    }

    [Fact]
    public void CyclicBarrierGetParticipantCount_BasicUsage_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            barrier <- CyclicBarrierCreate(5)
            count <- CyclicBarrierGetParticipantCount(barrier)
            PrintLine(""Participant count: "" + count.ToStr())
            CyclicBarrierDispose(barrier)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Participant count: 5", output);
    }

    [Fact]
    public void CyclicBarrierGetWaitingCount_BasicUsage_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            barrier <- CyclicBarrierCreate(1)

            // 初始等待计数应该为 0
            waitingCount <- CyclicBarrierGetWaitingCount(barrier)
            PrintLine(""Initial waiting count: "" + waitingCount.ToStr())

            CyclicBarrierDispose(barrier)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Initial waiting count: 0", output);
    }

    [Fact]
    public void CyclicBarrierDispose_AfterUsage_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            barrier <- CyclicBarrierCreate(1)
            CyclicBarrierAwait(barrier)
            CyclicBarrierDispose(barrier)
            PrintLine(""Barrier disposed"")
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Barrier disposed", output);
    }

    #endregion

    #region 超时场景测试

    [Fact]
    public void CyclicBarrierAwaitTimeout_Timeout_ReturnsFalse()
    {
        // Arrange
        var code = @"
            barrier <- CyclicBarrierCreate(2)

            // 只有一个线程等待，应该超时
            result <- CyclicBarrierAwaitTimeout(barrier, 100)
            PrintLine(""Await timeout result: "" + result.ToStr())

            CyclicBarrierDispose(barrier)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Await timeout result: false", output);
    }

    [Fact]
    public void CyclicBarrierAwaitTimeout_ZeroTimeout_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            barrier <- CyclicBarrierCreate(1)

            result <- CyclicBarrierAwaitTimeout(barrier, 0)
            PrintLine(""Await with zero timeout: "" + result.ToStr())

            CyclicBarrierDispose(barrier)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Await with zero timeout: true", output);
    }

    #endregion

    #region 异常安全性测试

    [Fact]
    public void CyclicBarrier_WithUsingStatement_DisposesCorrectly()
    {
        // Arrange
        var code = @"
            using barrier <- CyclicBarrierCreate(1) {
                CyclicBarrierAwait(barrier)
                PrintLine(""Inside using block"")
            }
            PrintLine(""After using block"")
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("Inside using block", lines[0]);
        Assert.Equal("After using block", lines[1]);
    }

    [Fact]
    public void CyclicBarrier_InTryCatchFinally_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            barrier <- CyclicBarrierCreate(1)
            try {
                CyclicBarrierAwait(barrier)
                PrintLine(""Await in try"")
            } catch (e) {
                PrintLine(""Error: "" + e)
            } finally {
                PrintLine(""Finally block"")
                CyclicBarrierDispose(barrier)
            }
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("Await in try", lines[0]);
        Assert.Equal("Finally block", lines[1]);
    }

    #endregion

    #region 边界条件测试

    [Fact]
    public void CyclicBarrierCreate_WithOneParticipant_ExecutesCorrectly()
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
    public void CyclicBarrier_ReusableAcrossPhases_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            barrier <- CyclicBarrierCreate(1)

            for i in [0~<3] {
                CyclicBarrierAwait(barrier)
                PrintLine(""Completed phase "" + i.ToStr())
            }

            CyclicBarrierDispose(barrier)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        for (int i = 0; i < 3; i++)
        {
            Assert.Equal($"Completed phase {i}", lines[i]);
        }
    }

    [Fact]
    public void CyclicBarrier_GetWaitingCount_TracksCorrectly()
    {
        // Arrange
        var code = @"
            barrier <- CyclicBarrierCreate(1)

            waiting1 <- CyclicBarrierGetWaitingCount(barrier)
            PrintLine(""Waiting before await: "" + waiting1.ToStr())

            CyclicBarrierAwait(barrier)

            waiting2 <- CyclicBarrierGetWaitingCount(barrier)
            PrintLine(""Waiting after await: "" + waiting2.ToStr())

            CyclicBarrierDispose(barrier)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("Waiting before await: 0", lines[0]);
        Assert.Equal("Waiting after await: 0", lines[1]);
    }

    #endregion
}
