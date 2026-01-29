using Old8Lang.AST.Expression.Value;
using Old8Lang.Bytecode;
using Old8Lang.Interpreter;
using VM = Old8Lang.Bytecode.VM.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Concurrency;

/// <summary>
/// 虚拟机 ReadWriteLock 并发原语测试
/// 测试 ReadWriteLock 的创建、读锁、写锁和释放功能
/// </summary>
[Collection("Sequential")]
public class VMConcurrencyReadWriteLockTests
{
    /// <summary>
    /// 辅助方法：从全局变量中获取整数值
    /// </summary>
    private static int GetIntValue(object? value)
    {
        return value switch
        {
            int i => i,
            long l => (int)l,
            IntLangValue ilv => ilv.Value,
            _ => Convert.ToInt32(value)
        };
    }

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
    public void ReadWriteLockCreate_BasicUsage_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            rwLock <- ReadWriteLockCreate()
            result <- rwLock != null
            ReadWriteLockDispose(rwLock)
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
    public void ReadLock_BasicUsage_ExecutesCorrectly()
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
    public void WriteLock_BasicUsage_ExecutesCorrectly()
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
            if result {
                PrintLine(""Read lock acquired"")
                ReadLockRelease(rwLock)
            }
            ReadWriteLockDispose(rwLock)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Read lock acquired", output);
    }

    [Fact]
    public void WriteLockTryAcquire_Success_ReturnsTrue()
    {
        // Arrange
        var code = @"
            rwLock <- ReadWriteLockCreate()
            result <- WriteLockTryAcquire(rwLock, 1000)
            if result {
                PrintLine(""Write lock acquired"")
                WriteLockRelease(rwLock)
            }
            ReadWriteLockDispose(rwLock)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Write lock acquired", output);
    }

    [Fact]
    public void ReadWriteLock_MultipleReaders_ExecutesCorrectly()
    {
        // Arrange - 测试多个读者可以同时持有读锁
        // 使用 AtomicInt 来确保计数操作是原子的
        var code = @"
            rwLock <- ReadWriteLockCreate()
            readerCount <- AtomicIntCreate(0)
            maxCount <- AtomicIntCreate(0)

            func reader(id:int) -> void {
                ReadLockAcquire(rwLock)
                current <- AtomicIntIncrement(readerCount)

                // 更新最大并发数
                max <- AtomicIntGet(maxCount)
                if current > max {
                    AtomicIntCompareAndSet(maxCount, max, current)
                }

                PrintLine(""Reader "" + id.ToStr() + "" acquired lock, count: "" + current.ToStr())
                Sleep(200)
                AtomicIntDecrement(readerCount)
                ReadLockRelease(rwLock)
            }

            a <- spawn(() -> reader(1))
            b <- spawn(() -> reader(2))
            c <- spawn(() -> reader(3))

            a.Start()
            b.Start()
            c.Start()

            // 等待所有线程完成
            Sleep(500)

            result <- AtomicIntGet(maxCount)

            AtomicIntDispose(readerCount)
            AtomicIntDispose(maxCount)
            ReadWriteLockDispose(rwLock)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert - 检查最大并发读者数是否 >= 2
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.True(GetIntValue(result) >= 2, $"Expected at least 2 concurrent readers, but got {GetIntValue(result)}");
    }

    [Fact]
    public void ReadWriteLock_WriterBlocksReaders_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            rwLock <- ReadWriteLockCreate()
            WriteLockAcquire(rwLock)

            t <- spawn(() -> {
                result <- ReadLockTryAcquire(rwLock, 100)
                if result {
                    PrintLine(""Reader acquired"")
                    ReadLockRelease(rwLock)
                } else {
                    PrintLine(""Reader blocked"")
                }
            })
            t.Start()

            Sleep(200)
            WriteLockRelease(rwLock)
            Sleep(100)
            ReadWriteLockDispose(rwLock)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Contains("Reader blocked", output);
    }

    [Fact]
    public void ReadWriteLock_ReaderBlocksWriter_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            rwLock <- ReadWriteLockCreate()
            ReadLockAcquire(rwLock)

            t <- spawn(() -> {
                result <- WriteLockTryAcquire(rwLock, 100)
                if result {
                    PrintLine(""Writer acquired"")
                    WriteLockRelease(rwLock)
                } else {
                    PrintLine(""Writer blocked"")
                }
            })
            t.Start()

            Sleep(200)
            ReadLockRelease(rwLock)
            Sleep(100)
            ReadWriteLockDispose(rwLock)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Contains("Writer blocked", output);
    }

    [Fact]
    public void ReadWriteLock_WithUsing_DisposesAutomatically()
    {
        // Arrange
        var code = @"
            using rwLock <- ReadWriteLockCreate() {
                ReadLockAcquire(rwLock)
                PrintLine(""Lock acquired"")
                ReadLockRelease(rwLock)
            }
            PrintLine(""Disposed"")
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("Lock acquired", lines[0]);
        Assert.Equal("Disposed", lines[1]);
    }

    [Fact]
    public void ReadWriteLock_WithDefer_ReleasesAutomatically()
    {
        // Arrange
        var code = @"
            rwLock <- ReadWriteLockCreate()

            func criticalSection() -> void {
                WriteLockAcquire(rwLock)
                defer WriteLockRelease(rwLock)
                PrintLine(""In critical section"")
            }

            criticalSection()
            PrintLine(""After critical section"")
            ReadWriteLockDispose(rwLock)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("In critical section", lines[0]);
        Assert.Equal("After critical section", lines[1]);
    }

    [Fact]
    public void ReadWriteLock_ReaderWriterPattern_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            rwLock <- ReadWriteLockCreate()
            data <- 0

            func writer() -> void {
                for i in [1~5] {
                    WriteLockAcquire(rwLock)
                    data <- data + 1
                    WriteLockRelease(rwLock)
                    Sleep(50)
                }
            }

            func reader() -> void {
                for i in [1~5] {
                    ReadLockAcquire(rwLock)
                    val <- data
                    ReadLockRelease(rwLock)
                    Sleep(30)
                }
            }

            t1 <- spawn(writer)
            t2 <- spawn(reader)
            t3 <- spawn(reader)

            t1.Start()
            t2.Start()
            t3.Start()

            Sleep(500)

            result <- data
            ReadWriteLockDispose(rwLock)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(5, GetIntValue(result));
    }

    [Fact]
    public void ReadWriteLock_UpgradeFromReadToWrite_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            rwLock <- ReadWriteLockCreate()

            ReadLockAcquire(rwLock)
            PrintLine(""Read lock acquired"")
            ReadLockRelease(rwLock)

            WriteLockAcquire(rwLock)
            PrintLine(""Write lock acquired"")
            WriteLockRelease(rwLock)

            ReadWriteLockDispose(rwLock)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("Read lock acquired", lines[0]);
        Assert.Equal("Write lock acquired", lines[1]);
    }
}
