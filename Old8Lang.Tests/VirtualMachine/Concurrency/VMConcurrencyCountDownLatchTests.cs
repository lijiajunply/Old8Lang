using Old8Lang.Bytecode;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.VirtualMachine.Concurrency;

/// <summary>
/// 虚拟机 CountDownLatch 并发原语测试
/// 测试 CountDownLatch 的创建、倒计数、等待、超时和资源管理
/// </summary>
[Collection("Sequential")]
public class VMConcurrencyCountDownLatchTests
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
    public void CountDownLatchCreate_BasicUsage_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            latch <- CountDownLatchCreate(3)
            PrintLine(""Latch created: "" + (latch > 0).ToStr())
            CountDownLatchDispose(latch)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Latch created: true", output);
    }

    [Fact]
    public void CountDownLatchCountDown_BasicUsage_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            latch <- CountDownLatchCreate(3)

            count1 <- CountDownLatchGetCount(latch)
            PrintLine(""Initial count: "" + count1.ToStr())

            CountDownLatchCountDown(latch)
            count2 <- CountDownLatchGetCount(latch)
            PrintLine(""After countdown: "" + count2.ToStr())

            CountDownLatchDispose(latch)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("Initial count: 3", lines[0]);
        Assert.Equal("After countdown: 2", lines[1]);
    }

    [Fact]
    public void CountDownLatchWait_BasicUsage_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            latch <- CountDownLatchCreate(1)

            PrintLine(""Before countdown"")
            CountDownLatchCountDown(latch)
            PrintLine(""After countdown"")

            CountDownLatchWait(latch)
            PrintLine(""Wait completed"")

            CountDownLatchDispose(latch)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        Assert.Equal("Before countdown", lines[0]);
        Assert.Equal("After countdown", lines[1]);
        Assert.Equal("Wait completed", lines[2]);
    }

    [Fact]
    public void CountDownLatchWaitTimeout_Success_ReturnsTrue()
    {
        // Arrange
        var code = @"
            latch <- CountDownLatchCreate(1)

            CountDownLatchCountDown(latch)
            result <- CountDownLatchWaitTimeout(latch, 1000)
            PrintLine(""Wait result: "" + result.ToStr())

            CountDownLatchDispose(latch)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Wait result: true", output);
    }

    [Fact]
    public void CountDownLatchGetCount_BasicUsage_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            latch <- CountDownLatchCreate(5)
            count <- CountDownLatchGetCount(latch)
            PrintLine(""Count: "" + count.ToStr())
            CountDownLatchDispose(latch)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Count: 5", output);
    }

    [Fact]
    public void CountDownLatchDispose_AfterUsage_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            latch <- CountDownLatchCreate(1)
            CountDownLatchCountDown(latch)
            CountDownLatchDispose(latch)
            PrintLine(""Latch disposed"")
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Latch disposed", output);
    }

    #endregion

    #region 超时场景测试

    [Fact]
    public void CountDownLatchWaitTimeout_Timeout_ReturnsFalse()
    {
        // Arrange
        var code = @"
            latch <- CountDownLatchCreate(1)

            // 不进行倒计数，等待应该超时
            result <- CountDownLatchWaitTimeout(latch, 100)
            PrintLine(""Wait timeout result: "" + result.ToStr())

            CountDownLatchDispose(latch)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Wait timeout result: false", output);
    }

    [Fact]
    public void CountDownLatchWaitTimeout_ZeroTimeout_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            latch <- CountDownLatchCreate(1)
            CountDownLatchCountDown(latch)

            result <- CountDownLatchWaitTimeout(latch, 0)
            PrintLine(""Wait with zero timeout: "" + result.ToStr())

            CountDownLatchDispose(latch)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Wait with zero timeout: true", output);
    }

    #endregion

    #region 异常安全性测试

    [Fact]
    public void CountDownLatch_WithUsingStatement_DisposesCorrectly()
    {
        // Arrange
        var code = @"
            using latch <- CountDownLatchCreate(1) {
                CountDownLatchCountDown(latch)
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
    public void CountDownLatch_InTryCatchFinally_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            latch <- CountDownLatchCreate(1)
            try {
                CountDownLatchCountDown(latch)
                PrintLine(""Countdown in try"")
            } catch (e) {
                PrintLine(""Error: "" + e)
            } finally {
                PrintLine(""Finally block"")
                CountDownLatchDispose(latch)
            }
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("Countdown in try", lines[0]);
        Assert.Equal("Finally block", lines[1]);
    }

    #endregion

    #region 边界条件测试

    [Fact]
    public void CountDownLatchCreate_WithZeroCount_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            latch <- CountDownLatchCreate(0)

            // 计数为 0，等待应该立即返回
            result <- CountDownLatchWaitTimeout(latch, 100)
            PrintLine(""Wait result with zero count: "" + result.ToStr())

            CountDownLatchDispose(latch)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Wait result with zero count: true", output);
    }

    [Fact]
    public void CountDownLatchCreate_WithOneCount_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            latch <- CountDownLatchCreate(1)

            count1 <- CountDownLatchGetCount(latch)
            PrintLine(""Initial: "" + count1.ToStr())

            CountDownLatchCountDown(latch)
            count2 <- CountDownLatchGetCount(latch)
            PrintLine(""After countdown: "" + count2.ToStr())

            CountDownLatchDispose(latch)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("Initial: 1", lines[0]);
        Assert.Equal("After countdown: 0", lines[1]);
    }

    [Fact]
    public void CountDownLatchCountDown_BeyondZero_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            latch <- CountDownLatchCreate(1)

            CountDownLatchCountDown(latch)
            count1 <- CountDownLatchGetCount(latch)
            PrintLine(""After first countdown: "" + count1.ToStr())

            // 再次倒计数（计数已经为 0）
            CountDownLatchCountDown(latch)
            count2 <- CountDownLatchGetCount(latch)
            PrintLine(""After second countdown: "" + count2.ToStr())

            CountDownLatchDispose(latch)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("After first countdown: 0", lines[0]);
        Assert.Equal("After second countdown: 0", lines[1]);
    }

    [Fact]
    public void CountDownLatch_MultipleCountDowns_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            latch <- CountDownLatchCreate(5)

            for i in [0~<5] {
                count <- CountDownLatchGetCount(latch)
                PrintLine(""Count before "" + i.ToStr() + "": "" + count.ToStr())
                CountDownLatchCountDown(latch)
            }

            finalCount <- CountDownLatchGetCount(latch)
            PrintLine(""Final count: "" + finalCount.ToStr())

            CountDownLatchDispose(latch)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(6, lines.Length);
        Assert.Equal("Count before 0: 5", lines[0]);
        Assert.Equal("Count before 1: 4", lines[1]);
        Assert.Equal("Count before 2: 3", lines[2]);
        Assert.Equal("Count before 3: 2", lines[3]);
        Assert.Equal("Count before 4: 1", lines[4]);
        Assert.Equal("Final count: 0", lines[5]);
    }

    #endregion
}
