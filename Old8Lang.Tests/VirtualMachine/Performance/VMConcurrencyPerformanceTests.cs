using Old8Lang.AST.Expression.Value;
using System.Diagnostics;
using VM = Old8Lang.Bytecode.VM.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Performance;

[Collection("Sequential")]
public class VMConcurrencyPerformanceTests
{
    [Fact]
    public void Concurrency_SpawnJoin_ThroughputScenario_CompletesAndReturnsExpected()
    {
        var code = @"
            func worker(x:int) -> int {
                return x + 1
            }

            sum <- 0
            for i <- 0, i < 2000, i <- i + 1 {
                t <- spawn(worker, i)
                t.Start()
                sum <- sum + t.Join()
            }
            result <- sum
        ";

        var (result, elapsed) = ExecuteWithTiming(code);
        Assert.True(GetIntValue(result) > 0);
        Assert.True(elapsed.TotalSeconds < 8, $"Spawn/join scenario took {elapsed.TotalSeconds:F2}s, expected < 8s");
    }

    [Fact]
    public void Concurrency_ChannelPipeline_CompletesAndConsumesAllMessages()
    {
        var code = @"
            ch <- ChannelCreateBounded(4096)
            consumed <- AtomicIntCreate(0)
            for i <- 0, i < 2000, i <- i + 1 {
                ChannelSend(ch, i)
            }
            ChannelClose(ch)

            while true {
                v <- null
                try {
                    v <- ChannelTryReceive(ch, 10)
                } catch {
                    break
                }
                if v == null {
                    break
                }
                if v != null {
                    AtomicIntIncrement(consumed)
                }
            }

            result <- AtomicIntGet(consumed)
            AtomicIntDispose(consumed)
            ChannelDispose(ch)
        ";

        var (result, elapsed) = ExecuteWithTiming(code);
        Assert.Equal(2000, GetIntValue(result));
        Assert.True(elapsed.TotalSeconds < 8, $"Channel pipeline took {elapsed.TotalSeconds:F2}s, expected < 8s");
    }

    [Fact]
    public void Concurrency_MutexAtomicCounter_CompletesAndCountsCorrectly()
    {
        var code = @"
            mutex <- MutexCreate()
            counter <- AtomicIntCreate(0)

            func worker() -> void {
                for i <- 0, i < 20000, i <- i + 1 {
                    MutexLock(mutex)
                    AtomicIntIncrement(counter)
                    MutexUnlock(mutex)
                }
            }

            t1 <- spawn(worker)
            t2 <- spawn(worker)
            t3 <- spawn(worker)
            t4 <- spawn(worker)
            t1.Start()
            t2.Start()
            t3.Start()
            t4.Start()
            t1.Join()
            t2.Join()
            t3.Join()
            t4.Join()

            result <- AtomicIntGet(counter)
            AtomicIntDispose(counter)
            MutexDispose(mutex)
        ";

        var (result, elapsed) = ExecuteWithTiming(code);
        Assert.Equal(80000, GetIntValue(result));
        Assert.True(elapsed.TotalSeconds < 10, $"Mutex+atomic scenario took {elapsed.TotalSeconds:F2}s, expected < 10s");
    }

    private static (object? result, TimeSpan elapsed) ExecuteWithTiming(string code)
    {
        var sw = Stopwatch.StartNew();
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();
        sw.Stop();
        return (vm.GetGlobalVariable("result"), sw.Elapsed);
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
