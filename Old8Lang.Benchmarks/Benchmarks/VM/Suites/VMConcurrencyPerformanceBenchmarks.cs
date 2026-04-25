using BenchmarkDotNet.Attributes;
using Old8Lang.Benchmarks.Benchmarks.VM.Base;
using Old8Lang.Bytecode;

namespace Old8Lang.Benchmarks.Benchmarks.VM.Suites;

/// <summary>
/// VM 并发与异步专项基准
/// </summary>
public class VMConcurrencyPerformanceBenchmarks : VMBenchmarkBase
{
    private BytecodeFile _spawnJoinColdStartBytecode = null!;
    private BytecodeFile _spawnJoinHotLoopBytecode = null!;
    private BytecodeFile _taskNewTaskAwaitBytecode = null!;
    private BytecodeFile _asyncFanOutFanInBytecode = null!;
    private BytecodeFile _channelSpscNoTimeoutBytecode = null!;
    private BytecodeFile _channelMpmcWithTimeoutBytecode = null!;

    [GlobalSetup]
    public void Setup()
    {
        _spawnJoinColdStartBytecode = CompileToBytecode(SpawnJoinColdStartCode);
        _spawnJoinHotLoopBytecode = CompileToBytecode(SpawnJoinHotLoopCode);
        _taskNewTaskAwaitBytecode = CompileToBytecode(TaskNewTaskAwaitCode);
        _asyncFanOutFanInBytecode = CompileToBytecode(AsyncFanOutFanInCode);
        _channelSpscNoTimeoutBytecode = CompileToBytecode(ChannelSpscNoTimeoutCode);
        _channelMpmcWithTimeoutBytecode = CompileToBytecode(ChannelMpmcWithTimeoutCode);
    }

    [Benchmark(Description = "VMXC_Latency_SpawnJoin_ColdStart")]
    public void SpawnJoinColdStart()
    {
        ExecuteAndAssertGlobalInt(_spawnJoinColdStartBytecode, "result", 500_500);
    }

    [Benchmark(Description = "VMXC_Throughput_SpawnJoin_HotLoop")]
    public void SpawnJoinHotLoop()
    {
        ExecuteAndAssertGlobalInt(_spawnJoinHotLoopBytecode, "result", 50_005_000);
    }

    [Benchmark(Description = "VMXC_Latency_Task_NewTask_Await")]
    public void TaskNewTaskAwait()
    {
        ExecuteAndAssertGlobalInt(_taskNewTaskAwaitBytecode, "result", 500_500);
    }

    [Benchmark(Description = "VMXC_Throughput_Async_FanOutFanIn")]
    public void AsyncFanOutFanIn()
    {
        ExecuteAndAssertGlobalInt(_asyncFanOutFanInBytecode, "result", 2_001_000);
    }

    [Benchmark(Description = "VMXC_Allocation_Channel_SPSC_NoTimeout")]
    public void ChannelSpscNoTimeout()
    {
        ExecuteAndAssertGlobalInt(_channelSpscNoTimeoutBytecode, "result", 120_000);
    }

    [Benchmark(Description = "VMXC_Throughput_Channel_MPMC_WithTimeout")]
    public void ChannelMpmcWithTimeout()
    {
        ExecuteAndAssertGlobalInt(_channelMpmcWithTimeoutBytecode, "result", 120_000);
    }

    private const string SpawnJoinColdStartCode = @"
func worker(x:int) -> int {
    return x
}

sum <- 0
for i <- 1, i <= 1000, i <- i + 1 {
    t <- spawn(worker, i)
    t.Start()
    sum <- sum + t.Join()
}
result <- sum
";

    private const string SpawnJoinHotLoopCode = @"
func worker(x:int) -> int {
    return x
}

sum <- 0
for i <- 1, i <= 10000, i <- i + 1 {
    t <- spawn(worker, i)
    t.Start()
    sum <- sum + t.Join()
}
result <- sum
";

    private const string TaskNewTaskAwaitCode = @"
async func worker(x:int) -> int {
    return x + 1
}

sum <- 0
for i <- 0, i < 1000, i <- i + 1 {
    t <- worker(i)
    sum <- sum + await t
}
result <- sum
";

    private const string AsyncFanOutFanInCode = @"
async func worker(x:int) -> int {
    return x + 1
}

tasks <- {}
for i <- 0, i < 2000, i <- i + 1 {
    tasks.Add(worker(i))
}

sum <- 0
for t in tasks {
    sum <- sum + await t
}
result <- sum
";

    private const string ChannelSpscNoTimeoutCode = @"
ch <- ChannelCreateBounded(2048)
count <- AtomicIntCreate(0)
producerDone <- AtomicIntCreate(0)

producer <- spawn(() -> {
    for i <- 0, i < 120000, i <- i + 1 {
        ChannelSend(ch, i)
    }
    AtomicIntSet(producerDone, 1)
    ChannelClose(ch)
})
producer.Start()

while true {
    v <- ChannelTryReceive(ch, 0)
    if v != null {
        AtomicIntIncrement(count)
    } elif AtomicIntGet(producerDone) == 1 {
        break
    }
}

producer.Join()
result <- AtomicIntGet(count)
AtomicIntDispose(count)
AtomicIntDispose(producerDone)
ChannelDispose(ch)
";

    private const string ChannelMpmcWithTimeoutCode = @"
ch <- ChannelCreateBounded(4096)
producerDone <- AtomicIntCreate(0)
consumed <- AtomicIntCreate(0)

func producer(start:int, end:int) -> void {
    for i <- start, i < end, i <- i + 1 {
        ChannelSend(ch, i)
    }
    done <- AtomicIntIncrement(producerDone)
    if done == 4 {
        ChannelClose(ch)
    }
}

func consumer() -> void {
    while true {
        v <- ChannelTryReceive(ch, 5)
        if v != null {
            AtomicIntIncrement(consumed)
        } elif AtomicIntGet(producerDone) == 4 {
            break
        }
    }
}

p0 <- spawn(producer, 0, 30000)
p1 <- spawn(producer, 30000, 60000)
p2 <- spawn(producer, 60000, 90000)
p3 <- spawn(producer, 90000, 120000)
c0 <- spawn(consumer)
c1 <- spawn(consumer)
c2 <- spawn(consumer)
c3 <- spawn(consumer)

p0.Start()
p1.Start()
p2.Start()
p3.Start()
c0.Start()
c1.Start()
c2.Start()
c3.Start()

p0.Join()
p1.Join()
p2.Join()
p3.Join()
c0.Join()
c1.Join()
c2.Join()
c3.Join()

result <- AtomicIntGet(consumed)
AtomicIntDispose(producerDone)
AtomicIntDispose(consumed)
ChannelDispose(ch)
";
}
