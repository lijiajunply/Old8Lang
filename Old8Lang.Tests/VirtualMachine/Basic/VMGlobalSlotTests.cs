using Old8Lang.Bytecode;
using VM = Old8Lang.Bytecode.VM.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Basic;

/// <summary>
/// 全局变量槽位（GlobalSlot）相关语义测试。
/// </summary>
/// <remarks>
/// 虚拟机把「全局名 -> 槽位」的解析结果缓存在指令上（见 <c>Bytecode/VM/GlobalSlot.cs</c>），
/// 这些用例覆盖缓存最容易出错的三类场景：
/// 同一份字节码被多个虚拟机实例复用、函数读写全局变量、闭包捕获同名变量。
/// </remarks>
[Collection("Sequential")]
public class VMGlobalSlotTests
{
    private static BytecodeFile Compile(string code) => CompileHelper.CompileToBytecode(code);

    [Fact]
    public void SameBytecodeExecutedByTwoVms_DoesNotLeakGlobalValues()
    {
        // 同一份字节码（共享的 Instruction 对象）被两个虚拟机依次执行：
        // 指令上缓存的槽位属于第一个虚拟机，第二个虚拟机必须识别出来并重新解析，
        // 不能把值写进上一个虚拟机的全局表。
        var bytecode = Compile("""
            x <- 1
            x <- x + 1
            result <- x
            """);

        var first = new VM(bytecode);
        first.Execute();
        Assert.Equal(2, Convert.ToInt32(first.GetGlobalVariable("result")));
        Assert.Equal(2, Convert.ToInt32(first.GetGlobalVariable("x")));

        var second = new VM(bytecode);
        second.Execute();
        Assert.Equal(2, Convert.ToInt32(second.GetGlobalVariable("result")));
        Assert.Equal(2, Convert.ToInt32(second.GetGlobalVariable("x")));
    }

    [Fact]
    public void GlobalVariable_SharedBetweenTopLevelAndFunction()
    {
        var vm = new VM(Compile("""
            g <- 10
            func bump() -> int {
                g <- g + 5
                return g
            }
            r <- bump()
            after <- g
            """));

        vm.Execute();

        Assert.Equal(15, Convert.ToInt32(vm.GetGlobalVariable("r")));
        Assert.Equal(15, Convert.ToInt32(vm.GetGlobalVariable("after")));
    }

    [Fact]
    public void ClosureCapture_ShadowsGlobalWithSameName()
    {
        var vm = new VM(Compile("""
            x <- 100
            func make() {
                x <- 7
                return () -> {
                    return x
                }
            }
            f <- make()
            captured <- f()
            globalValue <- x
            """));

        vm.Execute();

        Assert.Equal(7, Convert.ToInt32(vm.GetGlobalVariable("captured")));
        Assert.Equal(100, Convert.ToInt32(vm.GetGlobalVariable("globalValue")));
    }

    [Fact]
    public void GlobalAccumulator_InsideLoop_StaysInSync()
    {
        var vm = new VM(Compile("""
            total <- 0
            for i <- 0, i < 1000, i <- i + 1 {
                total <- total + i
            }
            result <- total
            """));

        vm.Execute();

        Assert.Equal(499_500, Convert.ToInt32(vm.GetGlobalVariable("result")));
    }

    [Fact]
    public void GlobalVariable_AssignedInFunction_IsVisibleFromTopLevel()
    {
        var vm = new VM(Compile("""
            flag <- false
            func enable() {
                flag <- true
            }
            enable()
            result <- flag
            """));

        vm.Execute();

        Assert.Equal(true, vm.GetGlobalVariable("result"));
    }
}
