using System.Diagnostics;
using Old8Lang.Bytecode;
using VM = Old8Lang.Bytecode.VM.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Performance;

/// <summary>
/// 虚拟机静态类 API 的计时测试
/// </summary>
/// <remarks>
/// 这些用例断言墙钟耗时，并行跑会因为资源争抢而抖动，因此按 CLAUDE.md 的分档约定
/// 标记为 Performance：默认档（default.runsettings）不会执行它们，
/// 需要用 performance.runsettings 单独跑。
///
/// 计时放在 C# 侧（Stopwatch）而不是语言侧：语言里没有跨模式可用的取时 API。
/// </remarks>
[Collection("Sequential")]
[Trait("Category", "Performance")]
public class VMStaticClassTimingTests
{
    private static BytecodeFile Compile(string code) => CompileHelper.CompileToBytecode(code);

    [Fact]
    public void TaskDelay_Await_ActuallyWaits()
    {
        var vm = new VM(Compile("await Task.Delay(60)"));

        var stopwatch = Stopwatch.StartNew();
        vm.Execute();
        stopwatch.Stop();

        Assert.True(stopwatch.ElapsedMilliseconds >= 50,
            $"Task.Delay 未真正等待，实际耗时 {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void ThreadSleep_ActuallyWaits()
    {
        var vm = new VM(Compile("Thread.Sleep(60)"));

        var stopwatch = Stopwatch.StartNew();
        vm.Execute();
        stopwatch.Stop();

        Assert.True(stopwatch.ElapsedMilliseconds >= 50,
            $"Thread.Sleep 未真正等待，实际耗时 {stopwatch.ElapsedMilliseconds}ms");
    }
}
