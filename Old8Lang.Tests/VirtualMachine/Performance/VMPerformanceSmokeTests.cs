using Old8Lang.AST.Expression.Value;
using System.Diagnostics;
using VM = Old8Lang.Bytecode.VM.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Performance;

/// <summary>
/// VM Quick 关键场景的快速阈值测试（不依赖 BenchmarkDotNet）。
/// </summary>
/// <remarks>
/// 对应基准场景：<c>VMXQ_Edge_HighArgCount_CallHotPath</c>、<c>VMXQ_Edge_HighThrowRate_TryCatch</c>、
/// <c>VMXQ_Edge_LargeClosureCapture_HighFreq</c>、<c>VMXQ_Concurrency_Channel_MPMC_4Workers</c>。
///
/// 阈值刻意放得很宽（约为本机实测值的 5~10 倍）：这里只拦截「灾难性退化」
/// （掉回慢路径、把本该复用缓冲的调用重新分配等），精确的性能判定交给
/// BenchmarkDotNet 的 Quick 报告与 CI 回归门禁。
/// 因为足够宽松，这些用例被放进常规 <c>dotnet test</c> 档（<c>Category=PerformanceSmoke</c>）。
/// </remarks>
[Collection("Sequential")]
[Trait("Category", "PerformanceSmoke")]
public class VMPerformanceSmokeTests
{
    /// <summary>
    /// HighArgCount — 30k 次 8 参数函数调用。
    /// 实测约 15~40ms / 7.2MB，上限取 400ms / 30MB。
    /// </summary>
    [Fact]
    public void HighArgCount_CallHotPath_StaysWithinSmokeLimits()
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

        var (elapsed, allocated, result) = ExecuteMeasured(code, "result");

        // Σ(i + 28), i ∈ [0, 30000)
        Assert.Equal(450_825_000, GetIntValue(result));
        Assert.True(elapsed.TotalMilliseconds < 400,
            $"HighArgCount_CallHotPath 超出 smoke 上限: {elapsed.TotalMilliseconds:F1}ms (上限 400ms)");
        Assert.True(allocated < 30L * 1024 * 1024,
            $"HighArgCount_CallHotPath 分配超限: {allocated / 1024.0 / 1024.0:F1}MB (上限 30MB)");
    }

    /// <summary>
    /// HighThrowRate — 40k 迭代、10k 次 throw（每 4 次一次）。
    /// 实测约 8~13ms / 5.0MB，上限取 300ms / 20MB。
    /// </summary>
    [Fact]
    public void HighThrowRate_TryCatch_StaysWithinSmokeLimits()
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
                    sum <- sum + 3
                }
            }
            result <- sum
        ";

        var (elapsed, allocated, result) = ExecuteMeasured(code, "result");

        // 非 4 的倍数之和 + 3 × 被 4 整除的个数(10000)
        Assert.Equal(600_030_000, GetIntValue(result));
        Assert.True(elapsed.TotalMilliseconds < 300,
            $"HighThrowRate_TryCatch 超出 smoke 上限: {elapsed.TotalMilliseconds:F1}ms (上限 300ms)");
        Assert.True(allocated < 20L * 1024 * 1024,
            $"HighThrowRate_TryCatch 分配超限: {allocated / 1024.0 / 1024.0:F1}MB (上限 20MB)");
    }

    /// <summary>
    /// LargeClosureCapture — 50k 次高频闭包调用（外层 8 个捕获变量）。
    /// 实测约 16~40ms / 14.8MB，上限取 400ms / 45MB。
    /// </summary>
    [Fact]
    public void LargeClosureCapture_HighFreq_StaysWithinSmokeLimits()
    {
        var code = @"
            a <- 1
            b <- 2
            c <- 3
            d <- 4
            e <- 5
            f <- 6
            g <- 7
            h <- 8

            adder <- (x:int) -> {
                return x + a + b + c + d + e + f + g + h
            }

            sum <- 0
            for i <- 0, i < 50000, i <- i + 1 {
                sum <- sum + adder(i)
            }
            result <- sum
        ";

        var (elapsed, allocated, result) = ExecuteMeasured(code, "result");

        // Σ(i + 36), i ∈ [0, 50000)
        Assert.Equal(1_251_775_000, GetIntValue(result));
        Assert.True(elapsed.TotalMilliseconds < 400,
            $"LargeClosureCapture_HighFreq 超出 smoke 上限: {elapsed.TotalMilliseconds:F1}ms (上限 400ms)");
        Assert.True(allocated < 45L * 1024 * 1024,
            $"LargeClosureCapture_HighFreq 分配超限: {allocated / 1024.0 / 1024.0:F1}MB (上限 45MB)");
    }

    /// <summary>
    /// Concurrency Channel MPMC — 4 生产者 + 4 消费者共 100k 条消息。
    /// 只要求「能跑通、不丢消息、不抛异常」，上限取 5s。
    /// </summary>
    [Fact]
    public void ChannelMpmc4Workers_CompletesWithoutLosingMessages()
    {
        const int workers = 4;
        const int itemsPerWorker = 25_000;
        var code = BuildChannelMpmcCode(workers, itemsPerWorker);

        var (elapsed, _, result) = ExecuteMeasured(code, "result");

        Assert.Equal(workers * itemsPerWorker, GetIntValue(result));
        Assert.True(elapsed.TotalMilliseconds < 5000,
            $"Channel_MPMC_4Workers 超出 smoke 上限: {elapsed.TotalMilliseconds:F1}ms (上限 5000ms)");
    }

    /// <summary>
    /// 预热一次后，测量「仅执行」的耗时与当前线程分配量。
    /// 编译不计入测量，避免解析/编译开销淹没 VM 执行差异。
    /// </summary>
    private static (TimeSpan Elapsed, long AllocatedBytes, object? Result) ExecuteMeasured(
        string code,
        string? resultVariable)
    {
        // 预热：编译并执行一次，让 JIT 与首次分配不进入计时窗口
        var warmupBytecode = CompileHelper.CompileToBytecode(code);
        new VM(warmupBytecode).Execute();

        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var sw = Stopwatch.StartNew();
        vm.Execute();
        sw.Stop();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        return (sw.Elapsed, allocated, resultVariable is null ? null : vm.GetGlobalVariable(resultVariable));
    }

    private static string BuildChannelMpmcCode(int workers, int itemsPerWorker)
    {
        var lines = new List<string>
        {
            "ch <- ChannelCreateBounded(2048)",
            "producerDone <- AtomicIntCreate(0)",
            "consumed <- AtomicIntCreate(0)",
            "",
            "func producer(start:int, end:int) -> void {",
            "    for i <- start, i < end, i <- i + 1 {",
            "        ChannelSend(ch, i)",
            "    }",
            "    done <- AtomicIntIncrement(producerDone)",
            $"    if done == {workers} {{",
            "        ChannelClose(ch)",
            "    }",
            "}",
            "",
            "func consumer() -> void {",
            "    while true {",
            "        v <- null",
            "        try {",
            "            v <- ChannelTryReceive(ch, 10)",
            "        } catch {",
            "            break",
            "        }",
            $"        if v != null {{",
            "            AtomicIntIncrement(consumed)",
            $"        }} elif AtomicIntGet(producerDone) == {workers} {{",
            "            break",
            "        }",
            "    }",
            "}",
            ""
        };

        for (var i = 0; i < workers; i++)
        {
            lines.Add($"p{i} <- spawn(producer, {i * itemsPerWorker}, {(i + 1) * itemsPerWorker})");
        }

        for (var i = 0; i < workers; i++)
        {
            lines.Add($"c{i} <- spawn(consumer)");
        }

        for (var i = 0; i < workers; i++)
        {
            lines.Add($"p{i}.Start()");
        }

        for (var i = 0; i < workers; i++)
        {
            lines.Add($"c{i}.Start()");
        }

        for (var i = 0; i < workers; i++)
        {
            lines.Add($"p{i}.Join()");
        }

        for (var i = 0; i < workers; i++)
        {
            lines.Add($"c{i}.Join()");
        }

        lines.Add("result <- AtomicIntGet(consumed)");
        lines.Add("AtomicIntDispose(producerDone)");
        lines.Add("AtomicIntDispose(consumed)");
        lines.Add("ChannelDispose(ch)");

        return string.Join("\n", lines);
    }

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
}
