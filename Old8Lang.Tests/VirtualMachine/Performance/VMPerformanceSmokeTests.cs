using System.Diagnostics;
using VM = Old8Lang.Bytecode.VM.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Performance;

/// <summary>
/// VM 性能 Smoke 测试 — 宽松上限，防止灾难性回归
/// 对应基准场景：VMXQ_Edge_HighArgCount_CallHotPath / HighThrowRate_TryCatch / LargeClosureCapture_HighFreq
/// </summary>
[Collection("Sequential")]
public class VMPerformanceSmokeTests
{
    /// <summary>
    /// HighArgCount — 30k 次 8 参数函数调用，上限 200ms（宽松，避免环境差异误判）
    /// </summary>
    [Fact]
    public void HighArgCount_CallHotPath_CompletesWithinSmokeLimit()
    {
        var code = @"
            func fold8(a:int, b:int, c:int, d:int, e:int, f:int, g:int, h:int) -> int {
                return a + b + c + d + e + f + g + h
            }

            sum <- 0
            for i <- 0, i < 30000, i <- i + 1 {
                sum <- sum + fold8(i, 1, 2, 3, 4, 5, 6, 7)
            }
            result <- sum
        ";

        var elapsed = ExecuteTimed(code);
        Assert.True(elapsed.TotalMilliseconds < 200,
            $"HighArgCount_CallHotPath 超出 smoke 上限: {elapsed.TotalMilliseconds:F1}ms (上限 200ms)");
    }

    /// <summary>
    /// HighThrowRate — 40k 迭代、10k throw，上限 200ms（Sprint A 内联分发后预计 ~15ms）
    /// </summary>
    [Fact]
    public void HighThrowRate_TryCatch_CompletesWithinSmokeLimit()
    {
        var code = @"
            sum <- 0
            for i <- 0, i < 40000, i <- i + 1 {
                try {
                    if i % 4 == 0 {
                        throw ""err""
                    }
                    sum <- sum + i
                } catch {
                    sum <- sum + 1
                }
            }
            result <- sum
        ";

        var elapsed = ExecuteTimed(code);
        Assert.True(elapsed.TotalMilliseconds < 200,
            $"HighThrowRate_TryCatch 超出 smoke 上限: {elapsed.TotalMilliseconds:F1}ms (上限 200ms)");
    }

    /// <summary>
    /// LargeClosureCapture — 50k 次高频闭包调用，上限 300ms（Sprint B+C 后预计 ~25ms）
    /// </summary>
    [Fact]
    public void LargeClosureCapture_HighFreq_CompletesWithinSmokeLimit()
    {
        var code = @"
            func makeAdder(base:int) -> any {
                return (x:int) -> base + x
            }

            adder <- makeAdder(100)
            sum <- 0
            for i <- 0, i < 50000, i <- i + 1 {
                sum <- sum + adder(i)
            }
            result <- sum
        ";

        var elapsed = ExecuteTimed(code);
        Assert.True(elapsed.TotalMilliseconds < 300,
            $"LargeClosureCapture_HighFreq 超出 smoke 上限: {elapsed.TotalMilliseconds:F1}ms (上限 300ms)");
    }

    private static TimeSpan ExecuteTimed(string code)
    {
        // 预热：编译一次并丢弃（避免 JIT 影响计时）
        CompileHelper.CompileToBytecode(code);

        var sw = Stopwatch.StartNew();
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();
        sw.Stop();
        return sw.Elapsed;
    }
}
