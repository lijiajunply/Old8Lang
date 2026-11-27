using System.Diagnostics;
using Old8Lang.Bytecode.Closures;
using Old8Lang.Bytecode.Core;
using Old8Lang.Bytecode.Metadata;
using Xunit.Abstractions;
using VM = Old8Lang.Bytecode.VM.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Basic;

/// <summary>
/// 类型化值表示（<see cref="VmValue"/>）、类型化栈、局部/全局槽位与闭包共享单元语义及零分配回归测试。
/// </summary>
[Collection("Sequential")]
public class VMValueRepresentationTests(ITestOutputHelper output)
{
    [Fact]
    public void VmValue_PrimitiveRoundTrips_PreserveExactTypesAndValues()
    {
        Assert.Equal(VmValueKind.Null, default(VmValue).Kind);
        Assert.Null(VmValue.Null.ToObject());

        var intMin = VmValue.FromInt(int.MinValue);
        Assert.Equal(VmValueKind.Int, intMin.Kind);
        Assert.Equal(int.MinValue, intMin.AsInt);
        Assert.Equal(int.MinValue, intMin.ToObject());

        var intSmall = VmValue.FromInt(42);
        Assert.Equal(42, intSmall.ToObject());
        // 小整数边界复用共享装箱实例
        Assert.Same(VmValue.FromInt(42).ToObject(), VmValue.FromInt(42).ToObject());

        var longVal = VmValue.FromLong(3_000_000_000L);
        Assert.Equal(VmValueKind.Long, longVal.Kind);
        Assert.Equal(3_000_000_000L, longVal.AsLong);
        Assert.Equal(3_000_000_000L, longVal.ToObject());

        var doubleVal = VmValue.FromDouble(-3.1415926535);
        Assert.Equal(VmValueKind.Double, doubleVal.Kind);
        Assert.Equal(-3.1415926535, doubleVal.AsDouble);
        Assert.Equal(-3.1415926535, doubleVal.ToObject());

        var boolTrue = VmValue.FromBool(true);
        var boolFalse = VmValue.FromBool(false);
        Assert.True(boolTrue.AsBool);
        Assert.False(boolFalse.AsBool);
        Assert.Same(boolTrue.ToObject(), VmValue.FromBool(true).ToObject());
        Assert.Same(boolFalse.ToObject(), VmValue.FromBool(false).ToObject());

        var charVal = VmValue.FromChar('中');
        Assert.Equal(VmValueKind.Char, charVal.Kind);
        Assert.Equal('中', charVal.AsChar);
        Assert.Equal('中', charVal.ToObject());

        var strVal = VmValue.FromObject("hello");
        Assert.Equal(VmValueKind.Object, strVal.Kind);
        Assert.Equal("hello", strVal.ToObject());
    }

    [Fact]
    public void ConstantPool_GetConstantValue_MatchesObjectConstants()
    {
        var pool = new ConstantPool();
        int iIdx = pool.AddConstant(123);
        int lIdx = pool.AddConstant(5_000_000_000L);
        int dIdx = pool.AddConstant(2.5);
        int bIdx = pool.AddConstant(true);
        int cIdx = pool.AddConstant('Z');
        int sIdx = pool.AddConstant("old8");

        Assert.Equal(123, pool.GetConstantValue(iIdx).AsInt);
        Assert.Equal(5_000_000_000L, pool.GetConstantValue(lIdx).AsLong);
        Assert.Equal(2.5, pool.GetConstantValue(dIdx).AsDouble);
        Assert.True(pool.GetConstantValue(bIdx).AsBool);
        Assert.Equal('Z', pool.GetConstantValue(cIdx).AsChar);
        Assert.Equal("old8", pool.GetConstantValue(sIdx).AsObject);
    }

    [Fact]
    public void Instruction_IntOperand_StaysInSyncAfterOperandPatch()
    {
        var inst = new Instruction(OpCode.Jump, 10);
        Assert.Equal(10, inst.Operand);

        // 模拟编译期回填跳转目标
        inst.Operand = 42;
        Assert.Equal(42, inst.Operand);

        inst.Operand = "globalName";
        Assert.Equal("globalName", inst.Operand);
    }

    [Fact]
    public void IntOverflowToLong_AndMixedNumericComparisons_MatchSemantics()
    {
        var vm = new VM(CompileHelper.CompileToBytecode("""
            big <- 2147483647 + 10
            bigMul <- 100000 * 100000
            eqIntDouble <- 3 == 3.0
            geIntDouble <- 3 >= 3.0
            leIntDouble <- 3.0 <= 3
            gtBig <- big > 2147483647
            subResult <- 100 - 37
            negDouble <- -2.5
            """));

        vm.Execute();

        Assert.Equal(2147483657L, vm.GetGlobalVariable("big"));
        Assert.Equal(10_000_000_000L, vm.GetGlobalVariable("bigMul"));
        Assert.Equal(true, vm.GetGlobalVariable("eqIntDouble"));
        Assert.Equal(true, vm.GetGlobalVariable("geIntDouble"));
        Assert.Equal(true, vm.GetGlobalVariable("leIntDouble"));
        Assert.Equal(true, vm.GetGlobalVariable("gtBig"));
        Assert.Equal(63, vm.GetGlobalVariable("subResult"));
        Assert.Equal(-2.5, vm.GetGlobalVariable("negDouble"));
    }

    [Fact]
    public void UpValueCell_TypedClosureWriteBack_AndLoopRefresh_WorkCorrectly()
    {
        var vm = new VM(CompileHelper.CompileToBytecode("""
            func makeCounter(start:int) {
                n <- start
                inc <- (delta:int) -> {
                    n <- n + delta
                    return n
                }
                get <- () -> {
                    return n
                }
                return [inc, get]
            }

            pair <- makeCounter(10)
            inc <- pair[0]
            get <- pair[1]
            r1 <- inc(5)
            r2 <- inc(7)
            r3 <- get()

            funcs <- {}
            for i <- 0, i < 5, i <- i + 1 {
                funcs.Add(() -> { return i * 10 })
            }
            loopSum <- funcs[0]() + funcs[1]() + funcs[2]() + funcs[3]() + funcs[4]()
            """));

        vm.Execute();

        Assert.Equal(15, Convert.ToInt32(vm.GetGlobalVariable("r1")));
        Assert.Equal(22, Convert.ToInt32(vm.GetGlobalVariable("r2")));
        Assert.Equal(22, Convert.ToInt32(vm.GetGlobalVariable("r3")));
        Assert.Equal(100, Convert.ToInt32(vm.GetGlobalVariable("loopSum")));
    }

    [Fact]
    public void GlobalSlot_ConcurrentReadsAndWritesAcrossWorkers_NeverTear()
    {
        var vm = new VM(CompileHelper.CompileToBytecode("""
            shared <- 0
            done <- AtomicIntCreate(0)
            errors <- AtomicIntCreate(0)

            func writer() -> void {
                for i <- 0, i < 2000, i <- i + 1 {
                    if i % 3 == 0 {
                        shared <- i
                    } elif i % 3 == 1 {
                        shared <- "tag-" + i.ToStr()
                    } else {
                        shared <- i * 1.5
                    }
                }
                AtomicIntIncrement(done)
            }

            func reader() -> void {
                for i <- 0, i < 4000, i <- i + 1 {
                    v <- shared
                    if v is int {
                        if v < 0 || v >= 2000 {
                            AtomicIntIncrement(errors)
                        }
                    } elif v is double {
                        if v < 0.0 || v > 3000.0 {
                            AtomicIntIncrement(errors)
                        }
                    } elif v is string {
                        if v.Length < 5 {
                            AtomicIntIncrement(errors)
                        }
                    } else {
                        AtomicIntIncrement(errors)
                    }
                }
            }

            w1 <- spawn(writer)
            w2 <- spawn(writer)
            r1 <- spawn(reader)
            r2 <- spawn(reader)
            w1.Start()
            w2.Start()
            r1.Start()
            r2.Start()
            w1.Join()
            w2.Join()
            r1.Join()
            r2.Join()

            errorCount <- AtomicIntGet(errors)
            doneCount <- AtomicIntGet(done)
            AtomicIntDispose(done)
            AtomicIntDispose(errors)
            """));

        vm.Execute();

        Assert.Equal(2, Convert.ToInt32(vm.GetGlobalVariable("doneCount")));
        Assert.Equal(0, Convert.ToInt32(vm.GetGlobalVariable("errorCount")));
    }

    [Fact]
    [Trait("Category", "PerformanceSmoke")]
    public void OneMillionIncrementLoop_LocalAndGlobal_ZeroPerIterationAllocation()
    {
        const string localLoopCode = """
            func run() -> int {
                sum <- 0
                for i <- 0, i < 1000000, i <- i + 1 {
                    sum <- sum + 1
                }
                return sum
            }
            result <- run()
            """;

        const string globalLoopCode = """
            sum <- 0
            for i <- 0, i < 1000000, i <- i + 1 {
                sum <- sum + 1
            }
            result <- sum
            """;

        var (localMs, localBytes, localResult) = MeasureExecution(localLoopCode, "result");
        var (globalMs, globalBytes, globalResult) = MeasureExecution(globalLoopCode, "result");

        output.WriteLine($"1M local  loop: {localMs:F2} ms, {localBytes} B ({localBytes / 1_000_000.0:F4} B/iter)");
        output.WriteLine($"1M global loop: {globalMs:F2} ms, {globalBytes} B ({globalBytes / 1_000_000.0:F4} B/iter)");

        Assert.Equal(1_000_000, Convert.ToInt32(localResult));
        Assert.Equal(1_000_000, Convert.ToInt32(globalResult));
        // P4 改造前为 45.78 MB（45.78 B/次）；类型化后每轮零装箱，仅剩 VM 启动的固定几 KB 分配
        Assert.True(localBytes < 64 * 1024, $"1M 局部循环分配超预期: {localBytes} B (期望 < 64 KB)");
        Assert.True(globalBytes < 64 * 1024, $"1M 全局循环分配超预期: {globalBytes} B (期望 < 64 KB)");
    }

    [Fact]
    [Trait("Category", "PerformanceSmoke")]
    public void ClosureUpValueWriteBackLoop_ZeroPerIterationAllocation()
    {
        const string code = """
            func run() -> int {
                acc <- 0
                addOne <- (delta:int) -> {
                    acc <- acc + delta
                    return acc
                }
                for i <- 0, i < 50000, i <- i + 1 {
                    addOne(1)
                }
                return acc
            }
            result <- run()
            """;

        var (elapsedMs, allocatedBytes, result) = MeasureExecution(code, "result");
        output.WriteLine($"50k UpValue closure write-back: {elapsedMs:F2} ms, {allocatedBytes} B ({allocatedBytes / 50_000.0:F4} B/iter)");

        Assert.Equal(50_000, Convert.ToInt32(result));
        Assert.True(allocatedBytes < 64 * 1024, $"50k 闭包 UpValue 写回分配超预期: {allocatedBytes} B (期望 < 64 KB)");
    }

    private static (double ElapsedMs, long AllocatedBytes, object? Result) MeasureExecution(string code, string resultName)
    {
        var warmupBytecode = CompileHelper.CompileToBytecode(code);
        new VM(warmupBytecode).Execute();

        var bytecode = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecode);

        var before = GC.GetAllocatedBytesForCurrentThread();
        var sw = Stopwatch.StartNew();
        vm.Execute();
        sw.Stop();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        return (sw.Elapsed.TotalMilliseconds, allocated, vm.GetGlobalVariable(resultName));
    }
}
