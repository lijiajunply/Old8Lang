using Old8Lang.Bytecode;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.VirtualMachine.Concurrency;

/// <summary>
/// 虚拟机 ReadWriteLock 并发原语测试
/// 测试 ReadWriteLock 的创建、读锁、写锁、超时和资源管理
/// </summary>
[Collection("Sequential")]
public class VMConcurrencyReadWriteLockTests
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
    public void ReadWriteLockCreate_BasicUsage_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            rwLock <- ReadWriteLockCreate()
            PrintLine(""ReadWriteLock created: "" + (rwLock > 0).ToStr())
            ReadWriteLockDispose(rwLock)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("ReadWriteLock created: true", output);
    }

    [Fact]
    public void ReadLockAcquire_BasicUsage_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            rwLock <- ReadWriteLockCreate()
            ReadLockAcquire(rwLock)
            PrintLine(""Read lock acquired"")
            ReadLockRelease(rwLock)
            PrintLine(""Read lock released"")
            ReadWriteLockDispose(rwLock)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("Read lock acquired", lines[0]);
        Assert.Equal("Read lock released", lines[1]);
    }

    [Fact]
    public void WriteLockAcquire_BasicUsage_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            rwLock <- ReadWriteLockCreate()
            WriteLockAcquire(rwLock)
            PrintLine(""Write lock acquired"")
            WriteLockRelease(rwLock)
            PrintLine(""Write lock released"")
            ReadWriteLockDispose(rwLock)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("Write lock acquired", lines[0]);
        Assert.Equal("Write lock released", lines[1]);
    }

    [Fact]
    public void ReadLockTryAcquire_Success_ReturnsTrue()
    {
        // Arrange
        var code = @"
            rwLock <- ReadWriteLockCreate()
            result <- ReadLockTryAcquire(rwLock, 1000)
            PrintLine(""TryAcquire result: "" + result.ToStr())
            if result {
                ReadLockRelease(rwLock)
            }
            ReadWriteLockDispose(rwLock)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("TryAcquire result: true", output);
    }

    [Fact]
    public void WriteLockTryAcquire_Success_ReturnsTrue()
    {
        // Arrange
        var code = @"
            rwLock <- ReadWriteLockCreate()
            result <- WriteLockTryAcquire(rwLock, 1000)
            PrintLine(""TryAcquire result: "" + result.ToStr())
            if result {
                WriteLockRelease(rwLock)
            }
            ReadWriteLockDispose(rwLock)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("TryAcquire result: true", output);
    }

    [Fact]
    public void ReadWriteLockDispose_AfterUsage_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            rwLock <- ReadWriteLockCreate()
            ReadLockAcquire(rwLock)
            ReadLockRelease(rwLock)
            ReadWriteLockDispose(rwLock)
            PrintLine(""ReadWriteLock disposed"")
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("ReadWriteLock disposed", output);
    }

    #endregion

    #region 读写锁语义测试

    [Fact]
    public void ReadLock_MultipleReaders_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            rwLock <- ReadWriteLockCreate()

            // 多个读锁可以同时获取
            ReadLockAcquire(rwLock)
            PrintLine(""First read lock acquired"")

            ReadLockAcquire(rwLock)
            PrintLine(""Second read lock acquired"")

            ReadLockRelease(rwLock)
            PrintLine(""First read lock released"")

            ReadLockRelease(rwLock)
            PrintLine(""Second read lock released"")

            ReadWriteLockDispose(rwLock)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(4, lines.Length);
        Assert.Equal("First read lock acquired", lines[0]);
        Assert.Equal("Second read lock acquired", lines[1]);
        Assert.Equal("First read lock released", lines[2]);
        Assert.Equal("Second read lock released", lines[3]);
    }

    [Fact]
    public void WriteLock_BlocksReaders_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            rwLock <- ReadWriteLockCreate()

            // 获取写锁
            WriteLockAcquire(rwLock)
            PrintLine(""Write lock acquired"")

            // 尝试获取读锁（应该失败）
            result <- ReadLockTryAcquire(rwLock, 100)
            PrintLine(""Read lock try result: "" + result.ToStr())

            WriteLockRelease(rwLock)
            PrintLine(""Write lock released"")

            // 现在读锁应该可以获取
            result2 <- ReadLockTryAcquire(rwLock, 100)
            PrintLine(""Read lock try result after write release: "" + result2.ToStr())
            if result2 {
                ReadLockRelease(rwLock)
            }

            ReadWriteLockDispose(rwLock)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(4, lines.Length);
        Assert.Equal("Write lock acquired", lines[0]);
        Assert.Equal("Read lock try result: false", lines[1]);
        Assert.Equal("Write lock released", lines[2]);
        Assert.Equal("Read lock try result after write release: true", lines[3]);
    }

    [Fact]
    public void ReadLock_BlocksWriters_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            rwLock <- ReadWriteLockCreate()

            // 获取读锁
            ReadLockAcquire(rwLock)
            PrintLine(""Read lock acquired"")

            // 尝试获取写锁（应该失败）
            result <- WriteLockTryAcquire(rwLock, 100)
            PrintLine(""Write lock try result: "" + result.ToStr())

            ReadLockRelease(rwLock)
            PrintLine(""Read lock released"")

            // 现在写锁应该可以获取
            result2 <- WriteLockTryAcquire(rwLock, 100)
            PrintLine(""Write lock try result after read release: "" + result2.ToStr())
            if result2 {
                WriteLockRelease(rwLock)
            }

            ReadWriteLockDispose(rwLock)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(4, lines.Length);
        Assert.Equal("Read lock acquired", lines[0]);
        Assert.Equal("Write lock try result: false", lines[1]);
        Assert.Equal("Read lock released", lines[2]);
        Assert.Equal("Write lock try result after read release: true", lines[3]);
    }

    #endregion

    #region 超时场景测试

    [Fact]
    public void ReadLockTryAcquire_WithTimeout_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            rwLock <- ReadWriteLockCreate()

            // 获取写锁
            WriteLockAcquire(rwLock)

            // 尝试获取读锁（应该超时）
            result <- ReadLockTryAcquire(rwLock, 100)
            PrintLine(""Read lock try with timeout: "" + result.ToStr())

            WriteLockRelease(rwLock)
            ReadWriteLockDispose(rwLock)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Read lock try with timeout: false", output);
    }

    [Fact]
    public void WriteLockTryAcquire_WithTimeout_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            rwLock <- ReadWriteLockCreate()

            // 获取读锁
            ReadLockAcquire(rwLock)

            // 尝试获取写锁（应该超时）
            result <- WriteLockTryAcquire(rwLock, 100)
            PrintLine(""Write lock try with timeout: "" + result.ToStr())

            ReadLockRelease(rwLock)
            ReadWriteLockDispose(rwLock)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Write lock try with timeout: false", output);
    }

    [Fact]
    public void ReadLockTryAcquire_ZeroTimeout_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            rwLock <- ReadWriteLockCreate()
            result <- ReadLockTryAcquire(rwLock, 0)
            PrintLine(""Read lock try with zero timeout: "" + result.ToStr())
            if result {
                ReadLockRelease(rwLock)
            }
            ReadWriteLockDispose(rwLock)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Read lock try with zero timeout: true", output);
    }

    #endregion

    #region 异常安全性测试

    [Fact]
    public void ReadWriteLock_WithUsingStatement_DisposesCorrectly()
    {
        // Arrange
        var code = @"
            using rwLock <- ReadWriteLockCreate() {
                ReadLockAcquire(rwLock)
                PrintLine(""Inside using block"")
                ReadLockRelease(rwLock)
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
    public void ReadWriteLock_InTryCatchFinally_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            rwLock <- ReadWriteLockCreate()
            try {
                WriteLockAcquire(rwLock)
                PrintLine(""Write lock acquired in try"")
            } catch (e) {
                PrintLine(""Error: "" + e)
            } finally {
                WriteLockRelease(rwLock)
                PrintLine(""Write lock released in finally"")
                ReadWriteLockDispose(rwLock)
            }
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("Write lock acquired in try", lines[0]);
        Assert.Equal("Write lock released in finally", lines[1]);
    }

    #endregion

    #region 边界条件测试

    [Fact]
    public void ReadLock_NestedAcquire_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            rwLock <- ReadWriteLockCreate()

            ReadLockAcquire(rwLock)
            PrintLine(""First read lock"")

            ReadLockAcquire(rwLock)
            PrintLine(""Second read lock"")

            ReadLockRelease(rwLock)
            PrintLine(""Released first"")

            ReadLockRelease(rwLock)
            PrintLine(""Released second"")

            ReadWriteLockDispose(rwLock)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(4, lines.Length);
    }

    [Fact]
    public void WriteLock_SequentialAcquire_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            rwLock <- ReadWriteLockCreate()

            for i in [0~<3] {
                WriteLockAcquire(rwLock)
                PrintLine(""Write lock "" + i.ToStr())
                WriteLockRelease(rwLock)
            }

            ReadWriteLockDispose(rwLock)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        Assert.Equal("Write lock 0", lines[0]);
        Assert.Equal("Write lock 1", lines[1]);
        Assert.Equal("Write lock 2", lines[2]);
    }

    #endregion
}
