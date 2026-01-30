using Old8Lang.AST.Expression.Value;
using Old8Lang.Bytecode;
using Old8Lang.Interpreter;
using VM = Old8Lang.Bytecode.VM.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Concurrency;

/// <summary>
/// 虚拟机 AtomicInt 并发原语测试
/// 测试 AtomicInt 的创建、获取、设置、增减和比较交换功能
/// </summary>
[Collection("Sequential")]
public class VMConcurrencyAtomicIntTests
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
    public void AtomicIntCreate_BasicUsage_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            atomic <- AtomicIntCreate(0)
            result <- atomic != null
            AtomicIntDispose(atomic)
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
    public void AtomicIntGet_BasicUsage_ReturnsInitialValue()
    {
        // Arrange
        var code = @"
            atomic <- AtomicIntCreate(42)
            result <- AtomicIntGet(atomic)
            AtomicIntDispose(atomic)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(42, GetIntValue(result));
    }

    [Fact]
    public void AtomicIntSet_BasicUsage_UpdatesValue()
    {
        // Arrange
        var code = @"
            atomic <- AtomicIntCreate(0)
            AtomicIntSet(atomic, 100)
            result <- AtomicIntGet(atomic)
            AtomicIntDispose(atomic)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(100, GetIntValue(result));
    }

    [Fact]
    public void AtomicIntIncrement_BasicUsage_IncrementsValue()
    {
        // Arrange
        var code = @"
            atomic <- AtomicIntCreate(10)
            result1 <- AtomicIntIncrement(atomic)
            result2 <- AtomicIntGet(atomic)
            AtomicIntDispose(atomic)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result1 = vm.GetGlobalVariable("result1");
        var result2 = vm.GetGlobalVariable("result2");
        Assert.Equal(11, GetIntValue(result1));
        Assert.Equal(11, GetIntValue(result2));
    }

    [Fact]
    public void AtomicIntDecrement_BasicUsage_DecrementsValue()
    {
        // Arrange
        var code = @"
            atomic <- AtomicIntCreate(10)
            result1 <- AtomicIntDecrement(atomic)
            result2 <- AtomicIntGet(atomic)
            AtomicIntDispose(atomic)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result1 = vm.GetGlobalVariable("result1");
        var result2 = vm.GetGlobalVariable("result2");
        Assert.Equal(9, GetIntValue(result1));
        Assert.Equal(9, GetIntValue(result2));
    }

    [Fact]
    public void AtomicIntAdd_BasicUsage_AddsValue()
    {
        // Arrange
        var code = @"
            atomic <- AtomicIntCreate(10)
            result1 <- AtomicIntAdd(atomic, 5)
            result2 <- AtomicIntGet(atomic)
            AtomicIntDispose(atomic)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result1 = vm.GetGlobalVariable("result1");
        var result2 = vm.GetGlobalVariable("result2");
        Assert.Equal(15, GetIntValue(result1));
        Assert.Equal(15, GetIntValue(result2));
    }

    [Fact]
    public void AtomicIntCompareAndSet_Success_UpdatesValue()
    {
        // Arrange
        var code = @"
            atomic <- AtomicIntCreate(10)
            result1 <- AtomicIntCompareAndSet(atomic, 10, 20)
            result2 <- AtomicIntGet(atomic)
            AtomicIntDispose(atomic)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result1 = vm.GetGlobalVariable("result1");
        var result2 = vm.GetGlobalVariable("result2");
        Assert.True((bool)result1);
        Assert.Equal(20, GetIntValue(result2));
    }

    [Fact]
    public void AtomicIntCompareAndSet_Failure_DoesNotUpdateValue()
    {
        // Arrange
        var code = @"
            atomic <- AtomicIntCreate(10)
            result1 <- AtomicIntCompareAndSet(atomic, 5, 20)
            result2 <- AtomicIntGet(atomic)
            AtomicIntDispose(atomic)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result1 = vm.GetGlobalVariable("result1");
        var result2 = vm.GetGlobalVariable("result2");
        Assert.False((bool)result1);
        Assert.Equal(10, GetIntValue(result2));
    }

    [Fact]
    public void AtomicInt_MultipleThreadsIncrement_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            atomic <- AtomicIntCreate(0)

            func incrementMany() -> void {
                for i in [1~100] {
                    AtomicIntIncrement(atomic)
                }
            }

            t1 <- spawn(incrementMany)
            t2 <- spawn(incrementMany)
            t3 <- spawn(incrementMany)

            t1.Start()
            t2.Start()
            t3.Start()

            Sleep(500)

            result <- AtomicIntGet(atomic)
            AtomicIntDispose(atomic)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(300, GetIntValue(result));
    }

    [Fact]
    public void AtomicInt_WithUsing_DisposesAutomatically()
    {
        // Arrange
        var code = @"
            using atomic <- AtomicIntCreate(0) {
                AtomicIntIncrement(atomic)
                result <- AtomicIntGet(atomic)
            }
            PrintLine(""Disposed"")
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("Disposed", output);
    }

    [Fact]
    public void AtomicInt_ConcurrentAddOperations_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            atomic <- AtomicIntCreate(0)

            func addValues() -> void {
                for i in [1~50] {
                    AtomicIntAdd(atomic, 2)
                }
            }

            t1 <- spawn(addValues)
            t2 <- spawn(addValues)
            t3 <- spawn(addValues)
            t4 <- spawn(addValues)

            t1.Start()
            t2.Start()
            t3.Start()
            t4.Start()

            Sleep(500)

            result <- AtomicIntGet(atomic)
            AtomicIntDispose(atomic)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(400, GetIntValue(result)); // 4 threads * 50 iterations * 2 = 400
    }

    [Fact]
    public void AtomicInt_CompareAndSetLoop_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            atomic <- AtomicIntCreate(0)

            func incrementWithCAS() -> void {
                for i in [1~10] {
                    success <- false
                    while !success {
                        current <- AtomicIntGet(atomic)
                        success <- AtomicIntCompareAndSet(atomic, current, current + 1)
                    }
                }
            }

            t1 <- spawn(incrementWithCAS)
            t2 <- spawn(incrementWithCAS)

            t1.Start()
            t2.Start()

            Sleep(500)

            result <- AtomicIntGet(atomic)
            AtomicIntDispose(atomic)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(20, GetIntValue(result));
    }

    [Fact]
    public void AtomicInt_NegativeValues_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            atomic <- AtomicIntCreate(-10)
            AtomicIntIncrement(atomic)
            result1 <- AtomicIntGet(atomic)
            AtomicIntAdd(atomic, -5)
            result2 <- AtomicIntGet(atomic)
            AtomicIntDispose(atomic)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result1 = vm.GetGlobalVariable("result1");
        var result2 = vm.GetGlobalVariable("result2");
        Assert.Equal(-9, GetIntValue(result1));
        Assert.Equal(-14, GetIntValue(result2));
    }

    [Fact]
    public void AtomicInt_StressTest_HandlesHighContention()
    {
        // Arrange - 简化测试，使用固定数量的线程
        var code = @"
            atomic <- AtomicIntCreate(0)

            func worker() -> void {
                for i in [1~100] {
                    AtomicIntIncrement(atomic)
                }
            }

            t1 <- spawn(worker)
            t2 <- spawn(worker)
            t3 <- spawn(worker)
            t4 <- spawn(worker)
            t5 <- spawn(worker)
            t6 <- spawn(worker)
            t7 <- spawn(worker)
            t8 <- spawn(worker)
            t9 <- spawn(worker)
            t10 <- spawn(worker)

            t1.Start()
            t2.Start()
            t3.Start()
            t4.Start()
            t5.Start()
            t6.Start()
            t7.Start()
            t8.Start()
            t9.Start()
            t10.Start()

            Sleep(2000)

            result <- AtomicIntGet(atomic)
            AtomicIntDispose(atomic)
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.Equal(1000, GetIntValue(result)); // 10 threads * 100 iterations = 1000
    }
}
