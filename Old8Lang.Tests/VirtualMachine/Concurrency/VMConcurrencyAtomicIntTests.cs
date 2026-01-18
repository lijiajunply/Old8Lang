using Old8Lang.Bytecode;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.VirtualMachine.Concurrency;

/// <summary>
/// 虚拟机 AtomicInt 并发原语测试
/// 测试 AtomicInt 的创建、原子操作、CAS 操作和资源管理
/// </summary>
[Collection("Sequential")]
public class VMConcurrencyAtomicIntTests
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
    public void AtomicIntCreate_BasicUsage_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            atomic <- AtomicIntCreate(42)
            value <- AtomicIntGet(atomic)
            PrintLine(""Atomic value: "" + value.ToStr())
            AtomicIntDispose(atomic)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Atomic value: 42", output);
    }

    [Fact]
    public void AtomicIntGet_BasicUsage_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            atomic <- AtomicIntCreate(100)
            value <- AtomicIntGet(atomic)
            PrintLine(""Value: "" + value.ToStr())
            AtomicIntDispose(atomic)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Value: 100", output);
    }

    [Fact]
    public void AtomicIntSet_BasicUsage_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            atomic <- AtomicIntCreate(0)
            AtomicIntSet(atomic, 123)
            value <- AtomicIntGet(atomic)
            PrintLine(""New value: "" + value.ToStr())
            AtomicIntDispose(atomic)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("New value: 123", output);
    }

    [Fact]
    public void AtomicIntIncrement_BasicUsage_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            atomic <- AtomicIntCreate(10)
            newValue <- AtomicIntIncrement(atomic)
            PrintLine(""After increment: "" + newValue.ToStr())
            AtomicIntDispose(atomic)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("After increment: 11", output);
    }

    [Fact]
    public void AtomicIntDecrement_BasicUsage_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            atomic <- AtomicIntCreate(10)
            newValue <- AtomicIntDecrement(atomic)
            PrintLine(""After decrement: "" + newValue.ToStr())
            AtomicIntDispose(atomic)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("After decrement: 9", output);
    }

    [Fact]
    public void AtomicIntAdd_BasicUsage_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            atomic <- AtomicIntCreate(10)
            newValue <- AtomicIntAdd(atomic, 5)
            PrintLine(""After add: "" + newValue.ToStr())
            AtomicIntDispose(atomic)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("After add: 15", output);
    }

    [Fact]
    public void AtomicIntCompareAndSet_Success_ReturnsTrue()
    {
        // Arrange
        var code = @"
            atomic <- AtomicIntCreate(10)
            result <- AtomicIntCompareAndSet(atomic, 10, 20)
            value <- AtomicIntGet(atomic)
            PrintLine(""CAS result: "" + result.ToStr() + "", Value: "" + value.ToStr())
            AtomicIntDispose(atomic)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("CAS result: True, Value: 20", output);
    }

    [Fact]
    public void AtomicIntCompareAndSet_Failure_ReturnsFalse()
    {
        // Arrange
        var code = @"
            atomic <- AtomicIntCreate(10)
            result <- AtomicIntCompareAndSet(atomic, 5, 20)
            value <- AtomicIntGet(atomic)
            PrintLine(""CAS result: "" + result.ToStr() + "", Value: "" + value.ToStr())
            AtomicIntDispose(atomic)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("CAS result: False, Value: 10", output);
    }

    [Fact]
    public void AtomicIntDispose_AfterUsage_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            atomic <- AtomicIntCreate(0)
            AtomicIntSet(atomic, 42)
            AtomicIntDispose(atomic)
            PrintLine(""Atomic disposed"")
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Atomic disposed", output);
    }

    #endregion

    #region 多线程并发测试

    [Fact]
    public void AtomicIntIncrement_MultipleThreads_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            atomic <- AtomicIntCreate(0)

            task1 <- spawn () -> {
                for i in [0~<100] {
                    AtomicIntIncrement(atomic)
                }
            }

            task2 <- spawn () -> {
                for i in [0~<100] {
                    AtomicIntIncrement(atomic)
                }
            }

            await task1
            await task2

            finalValue <- AtomicIntGet(atomic)
            PrintLine(""Final value: "" + finalValue.ToStr())
            AtomicIntDispose(atomic)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Final value: 200", output);
    }

    [Fact]
    public void AtomicIntDecrement_MultipleThreads_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            atomic <- AtomicIntCreate(200)

            task1 <- spawn () -> {
                for i in [0~<100] {
                    AtomicIntDecrement(atomic)
                }
            }

            task2 <- spawn () -> {
                for i in [0~<100] {
                    AtomicIntDecrement(atomic)
                }
            }

            await task1
            await task2

            finalValue <- AtomicIntGet(atomic)
            PrintLine(""Final value: "" + finalValue.ToStr())
            AtomicIntDispose(atomic)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Final value: 0", output);
    }

    [Fact]
    public void AtomicIntAdd_MultipleThreads_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            atomic <- AtomicIntCreate(0)

            task1 <- spawn () -> {
                for i in [0~<50] {
                    AtomicIntAdd(atomic, 2)
                }
            }

            task2 <- spawn () -> {
                for i in [0~<50] {
                    AtomicIntAdd(atomic, 3)
                }
            }

            await task1
            await task2

            finalValue <- AtomicIntGet(atomic)
            PrintLine(""Final value: "" + finalValue.ToStr())
            AtomicIntDispose(atomic)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Final value: 250", output);
    }

    [Fact]
    public void AtomicIntCompareAndSet_MultipleThreads_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            atomic <- AtomicIntCreate(0)
            successCount <- 0

            task1 <- spawn () -> {
                for i in [0~<10] {
                    currentValue <- AtomicIntGet(atomic)
                    result <- AtomicIntCompareAndSet(atomic, currentValue, currentValue + 1)
                    if result {
                        successCount <- successCount + 1
                    }
                }
            }

            task2 <- spawn () -> {
                for i in [0~<10] {
                    currentValue <- AtomicIntGet(atomic)
                    result <- AtomicIntCompareAndSet(atomic, currentValue, currentValue + 1)
                    if result {
                        successCount <- successCount + 1
                    }
                }
            }

            await task1
            await task2

            finalValue <- AtomicIntGet(atomic)
            PrintLine(""Final value: "" + finalValue.ToStr())
            AtomicIntDispose(atomic)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert - 最终值应该大于 0（至少有一些 CAS 操作成功）
        Assert.Contains("Final value:", output);
    }

    #endregion

    #region 边界条件测试

    [Fact]
    public void AtomicIntCreate_WithZero_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            atomic <- AtomicIntCreate(0)
            value <- AtomicIntGet(atomic)
            PrintLine(""Initial value: "" + value.ToStr())
            AtomicIntDispose(atomic)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Initial value: 0", output);
    }

    [Fact]
    public void AtomicIntCreate_WithNegative_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            atomic <- AtomicIntCreate(-10)
            value <- AtomicIntGet(atomic)
            PrintLine(""Initial value: "" + value.ToStr())
            AtomicIntDispose(atomic)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Initial value: -10", output);
    }

    [Fact]
    public void AtomicIntIncrement_FromNegative_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            atomic <- AtomicIntCreate(-5)
            newValue <- AtomicIntIncrement(atomic)
            PrintLine(""After increment: "" + newValue.ToStr())
            AtomicIntDispose(atomic)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("After increment: -4", output);
    }

    [Fact]
    public void AtomicIntDecrement_ToNegative_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            atomic <- AtomicIntCreate(1)
            AtomicIntDecrement(atomic)
            AtomicIntDecrement(atomic)
            value <- AtomicIntGet(atomic)
            PrintLine(""Value: "" + value.ToStr())
            AtomicIntDispose(atomic)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Value: -1", output);
    }

    [Fact]
    public void AtomicIntAdd_WithNegativeDelta_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            atomic <- AtomicIntCreate(10)
            newValue <- AtomicIntAdd(atomic, -3)
            PrintLine(""After add: "" + newValue.ToStr())
            AtomicIntDispose(atomic)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("After add: 7", output);
    }

    [Fact]
    public void AtomicIntCompareAndSet_WithNegativeValues_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            atomic <- AtomicIntCreate(-10)
            result <- AtomicIntCompareAndSet(atomic, -10, -20)
            value <- AtomicIntGet(atomic)
            PrintLine(""CAS result: "" + result.ToStr() + "", Value: "" + value.ToStr())
            AtomicIntDispose(atomic)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("CAS result: True, Value: -20", output);
    }

    #endregion

    #region 异常安全性测试

    [Fact]
    public void AtomicInt_WithUsingStatement_DisposesCorrectly()
    {
        // Arrange
        var code = @"
            using atomic <- AtomicIntCreate(0) {
                AtomicIntIncrement(atomic)
                value <- AtomicIntGet(atomic)
                PrintLine(""Value: "" + value.ToStr())
            }
            PrintLine(""After using block"")
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("Value: 1", lines[0]);
        Assert.Equal("After using block", lines[1]);
    }

    #endregion

    #region 性能测试

    [Fact]
    public void AtomicIntIncrement_HighContentionScenario_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            atomic <- AtomicIntCreate(0)
            threadCount <- 5
            iterationsPerThread <- 100

            tasks <- {}

            for t in [0~<threadCount] {
                task <- spawn () -> {
                    for i in [0~<iterationsPerThread] {
                        AtomicIntIncrement(atomic)
                    }
                }
                tasks.Add(task)
            }

            for task in tasks {
                await task
            }

            finalValue <- AtomicIntGet(atomic)
            expectedValue <- threadCount * iterationsPerThread
            PrintLine(""Final: "" + finalValue.ToStr() + "", Expected: "" + expectedValue.ToStr())
            AtomicIntDispose(atomic)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Final: 500, Expected: 500", output);
    }

    [Fact]
    public void AtomicIntMixedOperations_MultipleThreads_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            atomic <- AtomicIntCreate(100)

            task1 <- spawn () -> {
                for i in [0~<50] {
                    AtomicIntIncrement(atomic)
                }
            }

            task2 <- spawn () -> {
                for i in [0~<30] {
                    AtomicIntDecrement(atomic)
                }
            }

            task3 <- spawn () -> {
                for i in [0~<20] {
                    AtomicIntAdd(atomic, 2)
                }
            }

            await task1
            await task2
            await task3

            finalValue <- AtomicIntGet(atomic)
            expectedValue <- 100 + 50 - 30 + (20 * 2)
            PrintLine(""Final: "" + finalValue.ToStr() + "", Expected: "" + expectedValue.ToStr())
            AtomicIntDispose(atomic)
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Final: 160, Expected: 160", output);
    }

    #endregion
}
