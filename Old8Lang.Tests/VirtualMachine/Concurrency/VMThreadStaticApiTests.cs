using Old8Lang.AST.Expression.Value;
using Old8Lang.Bytecode;
using VM = Old8Lang.Bytecode.VM.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Concurrency;

/// <summary>
/// 虚拟机下的 Thread 静态类测试
/// </summary>
/// <remarks>
/// 虚拟机只支持 <c>Thread.Sleep</c>（转发给解释器同名实现，参数必须是整数）。
/// 其余 Thread 静态 API（<c>CurrentThread</c> / <c>Delay</c> / <c>WhenAll</c> / <c>WhenAny</c>）
/// 围绕解释器的 <c>ThreadLangValue</c> 构造，与虚拟机的 <c>VMThreadLangValue</c> 不同构，
/// 遇到时会报 <c>VmUnsupportedError</c>（见 <c>VMUnsupportedFeatureTests</c>）。
/// </remarks>
[Collection("Sequential")]
public class VMThreadStaticApiTests
{
    private static BytecodeFile Compile(string code) => CompileHelper.CompileToBytecode(code);

    [Fact]
    public void ThreadSleep_ExecutesCorrectly()
    {
        var vm = new VM(Compile("""
            Thread.Sleep(1)
            result <- 1
            """));

        vm.Execute();

        Assert.Equal(1, vm.GetGlobalVariable("result"));
    }

    [Fact]
    public void ThreadSleep_InsideFunction_ExecutesCorrectly()
    {
        var vm = new VM(Compile("""
            func wait() {
                Thread.Sleep(1)
            }
            wait()
            result <- 1
            """));

        vm.Execute();

        Assert.Equal(1, vm.GetGlobalVariable("result"));
    }

    [Fact]
    public void ThreadSleep_NonIntegerArgument_ReportsTypeError()
    {
        // 解释器下 Thread.Sleep 只接受整数；这里刻意复用同一个实现，
        // 而不是转发给会做 Convert.ToInt32 的全局 Sleep 函数。
        var vm = new VM(Compile("""Thread.Sleep("10")"""));

        Assert.Throws<Old8Lang.Error.TypeError>(() => vm.Execute());
    }
}
