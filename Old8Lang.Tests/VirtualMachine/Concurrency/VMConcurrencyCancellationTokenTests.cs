using Old8Lang.Bytecode;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.VirtualMachine.Concurrency;

/// <summary>
/// 虚拟机 CancellationTokenSource 并发原语测试
/// 测试 CancellationTokenSource 的创建、取消、延迟取消和资源管理
/// </summary>
[Collection("Sequential")]
public class VMConcurrencyCancellationTokenTests
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
    public void CreateCancellationTokenSource_BasicUsage_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            cts <- CreateCancellationTokenSource()
            PrintLine(""CancellationTokenSource created: "" + (cts > 0).ToStr())
            DisposeCancellationTokenSource(cts)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("CancellationTokenSource created: true", output);
    }

    [Fact]
    public void Cancel_BasicUsage_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            cts <- CreateCancellationTokenSource()
            Cancel(cts)
            PrintLine(""Cancellation requested"")
            DisposeCancellationTokenSource(cts)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Cancellation requested", output);
    }

    [Fact]
    public void CancelAfter_BasicUsage_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            cts <- CreateCancellationTokenSource()
            CancelAfter(cts, 100)
            PrintLine(""Cancel scheduled"")
            Sleep(150)
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
    public void DisposeCancellationTokenSource_AfterUsage_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            cts <- CreateCancellationTokenSource()
            Cancel(cts)
            DisposeCancellationTokenSource(cts)
            PrintLine(""CancellationTokenSource disposed"")
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("CancellationTokenSource disposed", output);
    }

    #endregion

    #region 取消场景测试

    [Fact]
    public void Cancel_ImmediateCancel_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            cts <- CreateCancellationTokenSource()

            PrintLine(""Before cancel"")
            Cancel(cts)
            PrintLine(""Cancellation requested"")

            DisposeCancellationTokenSource(cts)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("Before cancel", lines[0]);
        Assert.Equal("Cancellation requested", lines[1]);
    }

    [Fact]
    public void CancelAfter_DelayedCancel_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            cts <- CreateCancellationTokenSource()

            PrintLine(""Before CancelAfter"")
            CancelAfter(cts, 200)
            PrintLine(""CancelAfter scheduled"")

            Sleep(100)
            PrintLine(""After 100ms"")

            Sleep(150)
            PrintLine(""After 250ms total"")

            DisposeCancellationTokenSource(cts)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(4, lines.Length);
        Assert.Equal("Before CancelAfter", lines[0]);
        Assert.Equal("CancelAfter scheduled", lines[1]);
        Assert.Equal("After 100ms", lines[2]);
        Assert.Equal("After 250ms total", lines[3]);
    }

    [Fact]
    public void Cancel_MultipleCalls_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            cts <- CreateCancellationTokenSource()

            Cancel(cts)
            PrintLine(""First cancel"")

            Cancel(cts)
            PrintLine(""Second cancel"")

            Cancel(cts)
            PrintLine(""Third cancel"")

            DisposeCancellationTokenSource(cts)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        Assert.Equal("First cancel", lines[0]);
        Assert.Equal("Second cancel", lines[1]);
        Assert.Equal("Third cancel", lines[2]);
    }

    #endregion

    #region 异常安全性测试

    [Fact]
    public void CancellationTokenSource_WithUsingStatement_DisposesCorrectly()
    {
        // Arrange
        var code = @"
            using cts <- CreateCancellationTokenSource() {
                Cancel(cts)
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
    public void CancellationTokenSource_InTryCatchFinally_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            cts <- CreateCancellationTokenSource()
            try {
                Cancel(cts)
                PrintLine(""Cancel in try"")
            } catch (e) {
                PrintLine(""Error: "" + e)
            } finally {
                PrintLine(""Finally block"")
                DisposeCancellationTokenSource(cts)
            }
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("Cancel in try", lines[0]);
        Assert.Equal("Finally block", lines[1]);
    }

    #endregion

    #region 边界条件测试

    [Fact]
    public void CancelAfter_ZeroDelay_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            cts <- CreateCancellationTokenSource()

            CancelAfter(cts, 0)
            PrintLine(""CancelAfter with zero delay"")

            Sleep(50)
            PrintLine(""After delay"")

            DisposeCancellationTokenSource(cts)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("CancelAfter with zero delay", lines[0]);
        Assert.Equal("After delay", lines[1]);
    }

    [Fact]
    public void CancelAfter_LongDelay_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            cts <- CreateCancellationTokenSource()

            CancelAfter(cts, 5000)
            PrintLine(""CancelAfter with long delay"")

            Sleep(100)
            PrintLine(""Before long delay expires"")

            DisposeCancellationTokenSource(cts)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("CancelAfter with long delay", lines[0]);
        Assert.Equal("Before long delay expires", lines[1]);
    }

    [Fact]
    public void Cancel_BeforeCancelAfter_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            cts <- CreateCancellationTokenSource()

            Cancel(cts)
            PrintLine(""Immediate cancel"")

            CancelAfter(cts, 100)
            PrintLine(""CancelAfter after immediate cancel"")

            Sleep(150)
            PrintLine(""After delay"")

            DisposeCancellationTokenSource(cts)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        Assert.Equal("Immediate cancel", lines[0]);
        Assert.Equal("CancelAfter after immediate cancel", lines[1]);
        Assert.Equal("After delay", lines[2]);
    }

    [Fact]
    public void CancelAfter_MultipleCalls_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            cts <- CreateCancellationTokenSource()

            CancelAfter(cts, 500)
            PrintLine(""First CancelAfter"")

            CancelAfter(cts, 100)
            PrintLine(""Second CancelAfter (shorter delay)"")

            Sleep(150)
            PrintLine(""After delay"")

            DisposeCancellationTokenSource(cts)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        Assert.Equal("First CancelAfter", lines[0]);
        Assert.Equal("Second CancelAfter (shorter delay)", lines[1]);
        Assert.Equal("After delay", lines[2]);
    }

    #endregion

    #region 资源管理测试

    [Fact]
    public void CancellationTokenSource_CreateAndDispose_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            for i in [0~<5] {
                cts <- CreateCancellationTokenSource()
                DisposeCancellationTokenSource(cts)
                PrintLine(""Iteration "" + i.ToStr())
            }
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(5, lines.Length);
        for (int i = 0; i < 5; i++)
        {
            Assert.Equal($"Iteration {i}", lines[i]);
        }
    }

    [Fact]
    public void CancellationTokenSource_MultipleInstances_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            cts1 <- CreateCancellationTokenSource()
            cts2 <- CreateCancellationTokenSource()
            cts3 <- CreateCancellationTokenSource()

            Cancel(cts1)
            PrintLine(""CTS1 cancelled"")

            CancelAfter(cts2, 100)
            PrintLine(""CTS2 scheduled"")

            Cancel(cts3)
            PrintLine(""CTS3 cancelled"")

            DisposeCancellationTokenSource(cts1)
            DisposeCancellationTokenSource(cts2)
            DisposeCancellationTokenSource(cts3)

            PrintLine(""All disposed"")
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(4, lines.Length);
        Assert.Equal("CTS1 cancelled", lines[0]);
        Assert.Equal("CTS2 scheduled", lines[1]);
        Assert.Equal("CTS3 cancelled", lines[2]);
        Assert.Equal("All disposed", lines[3]);
    }

    #endregion
}
