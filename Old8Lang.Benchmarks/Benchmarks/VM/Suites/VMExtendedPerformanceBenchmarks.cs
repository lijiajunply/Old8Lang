using BenchmarkDotNet.Attributes;
using Old8Lang.Benchmarks.Benchmarks.VM.Base;
using Old8Lang.Benchmarks.Tools;
using Old8Lang.Bytecode;

namespace Old8Lang.Benchmarks.Benchmarks.VM.Suites;

/// <summary>
/// 扩展 VM 模式性能基准（大文件 + 边缘 + 并发）
/// </summary>
public class VMExtendedPerformanceBenchmarks : VMBenchmarkBase
{
    private const int LargeFileSeed = 20260301;

    private string _largeFile10kCode = string.Empty;
    private string _largeFile50kGeneratedCode = string.Empty;
    private string _generatedFilePath = string.Empty;

    private BytecodeFile _largeFile10kBytecode = null!;
    private BytecodeFile _largeFile50kBytecode = null!;
    private BytecodeFile _edgeDeepRecursionBytecode = null!;
    private BytecodeFile _edgeHighArgCountBytecode = null!;
    private BytecodeFile _edgeTryCatchBytecode = null!;
    private BytecodeFile _edgeClosureCaptureBytecode = null!;
    private BytecodeFile _edgeNamedArgsStressBytecode = null!;
    private BytecodeFile _concurrencySpawnJoinBytecode = null!;
    private BytecodeFile _concurrencyChannelSpscBytecode = null!;
    private BytecodeFile _concurrencyChannelMpmcBytecode = null!;
    private BytecodeFile _concurrencySemaphoreContentionBytecode = null!;
    private BytecodeFile _concurrencyMutexAtomicBytecode = null!;
    private BytecodeFile _concurrencyAsyncFanOutBytecode = null!;

    [GlobalSetup]
    public void Setup()
    {
        var fixedPath = ResolveTestDataPath("vm_large_10000.old8");
        if (!File.Exists(fixedPath))
        {
            throw new FileNotFoundException($"固定大文件测试数据不存在: {fixedPath}");
        }

        _largeFile10kCode = File.ReadAllText(fixedPath);
        _generatedFilePath = Path.Combine(Path.GetTempPath(), "vm_large_50000.generated.old8");
        TestDataGenerator.GenerateVmLargeScript(50_000, LargeFileSeed, _generatedFilePath);
        _largeFile50kGeneratedCode = File.ReadAllText(_generatedFilePath);

        _largeFile10kBytecode = CompileToBytecode(_largeFile10kCode);
        _largeFile50kBytecode = CompileToBytecode(_largeFile50kGeneratedCode);
        _edgeDeepRecursionBytecode = CompileToBytecode(EdgeDeepRecursionCode);
        _edgeHighArgCountBytecode = CompileToBytecode(EdgeHighArgCountCode);
        _edgeTryCatchBytecode = CompileToBytecode(EdgeTryCatchCode);
        _edgeClosureCaptureBytecode = CompileToBytecode(EdgeClosureCaptureCode);
        _edgeNamedArgsStressBytecode = CompileToBytecode(EdgeNamedArgsStressCode);
        _concurrencySpawnJoinBytecode = CompileToBytecode(ConcurrencySpawnJoinCode);
        _concurrencyChannelSpscBytecode = CompileToBytecode(ConcurrencyChannelSpscCode);
        _concurrencyChannelMpmcBytecode = CompileToBytecode(ConcurrencyChannelMpmcCode);
        _concurrencySemaphoreContentionBytecode = CompileToBytecode(ConcurrencySemaphoreContentionCode);
        _concurrencyMutexAtomicBytecode = CompileToBytecode(ConcurrencyMutexAtomicCode);
        _concurrencyAsyncFanOutBytecode = CompileToBytecode(ConcurrencyAsyncFanOutCode);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (string.IsNullOrWhiteSpace(_generatedFilePath))
        {
            return;
        }

        if (File.Exists(_generatedFilePath))
        {
            File.Delete(_generatedFilePath);
        }
    }

    [Benchmark(Description = "VMX_LargeFile_CompileOnly_10k")]
    public void LargeFileCompileOnly10k()
    {
        _ = CompileToBytecode(_largeFile10kCode);
    }

    [Benchmark(Description = "VMX_LargeFile_CompileAndExecute_10k")]
    public void LargeFileCompileAndExecute10k()
    {
        ExecuteBytecode(_largeFile10kBytecode);
    }

    [Benchmark(Description = "VMX_LargeFile_CompileOnly_50k_Generated")]
    public void LargeFileCompileOnly50kGenerated()
    {
        _ = CompileToBytecode(_largeFile50kGeneratedCode);
    }

    [Benchmark(Description = "VMX_LargeFile_CompileAndExecute_50k_Generated")]
    public void LargeFileCompileAndExecute50kGenerated()
    {
        ExecuteBytecode(_largeFile50kBytecode);
    }

    [Benchmark(Description = "VMX_Edge_DeepRecursion_NearLimit")]
    public void EdgeDeepRecursionNearLimit()
    {
        ExecuteBytecode(_edgeDeepRecursionBytecode);
    }

    [Benchmark(Description = "VMX_Edge_HighArgCount_CallHotPath")]
    public void EdgeHighArgCountCallHotPath()
    {
        ExecuteBytecode(_edgeHighArgCountBytecode);
    }

    [Benchmark(Description = "VMX_Edge_HeavyTryCatch_LowThrowRate")]
    public void EdgeHeavyTryCatchLowThrowRate()
    {
        ExecuteBytecode(_edgeTryCatchBytecode);
    }

    [Benchmark(Description = "VMX_Edge_LargeClosureCapture")]
    public void EdgeLargeClosureCapture()
    {
        ExecuteBytecode(_edgeClosureCaptureBytecode);
    }

    [Benchmark(Description = "VMX_Edge_DefaultAndNamedArgs_Stress")]
    public void EdgeDefaultAndNamedArgsStress()
    {
        ExecuteBytecode(_edgeNamedArgsStressBytecode);
    }

    [Benchmark(Description = "VMX_Concurrency_SpawnJoin_Throughput")]
    public void ConcurrencySpawnJoinThroughput()
    {
        ExecuteBytecode(_concurrencySpawnJoinBytecode);
    }

    [Benchmark(Description = "VMX_Concurrency_Channel_SPSC_Throughput")]
    public void ConcurrencyChannelSpscThroughput()
    {
        ExecuteBytecode(_concurrencyChannelSpscBytecode);
    }

    [Benchmark(Description = "VMX_Concurrency_Channel_MPMC_Throughput")]
    public void ConcurrencyChannelMpmcThroughput()
    {
        ExecuteBytecode(_concurrencyChannelMpmcBytecode);
    }

    [Benchmark(Description = "VMX_Concurrency_Semaphore_Contention")]
    public void ConcurrencySemaphoreContention()
    {
        ExecuteBytecode(_concurrencySemaphoreContentionBytecode);
    }

    [Benchmark(Description = "VMX_Concurrency_MutexAtomicCounter")]
    public void ConcurrencyMutexAtomicCounter()
    {
        ExecuteBytecode(_concurrencyMutexAtomicBytecode);
    }

    [Benchmark(Description = "VMX_Concurrency_AsyncFanOutFanIn")]
    public void ConcurrencyAsyncFanOutFanIn()
    {
        ExecuteBytecode(_concurrencyAsyncFanOutBytecode);
    }

    private const string EdgeDeepRecursionCode = @"
func deep(n:int) -> int {
    if n <= 0 {
        return 0
    }
    return 1 + deep(n - 1)
}

result <- deep(120)
";

    private const string EdgeHighArgCountCode = @"
func fold8(a:int, b:int, c:int, d:int, e:int, f:int, g:int, h:int) -> int {
    return a + b + c + d + e + f + g + h
}

sum <- 0
for i <- 0, i < 30000, i <- i + 1 {
    sum <- sum + fold8(i, 1, 2, 3, 4, 5, 6, 7)
}
";

    private const string EdgeTryCatchCode = @"
sum <- 0
for i <- 0, i < 100000, i <- i + 1 {
    try {
        if i % 10000 == 0 {
            throw ""edge""
        }
        sum <- sum + i
    } catch {
        sum <- sum + 1
    }
}
";

    private const string EdgeClosureCaptureCode = @"
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
for i <- 0, i < 20000, i <- i + 1 {
    sum <- sum + adder(i)
}
";

    private const string EdgeNamedArgsStressCode = @"
func format(name:string, age:int, prefix: ""User"", suffix: ""X"") -> string {
    return prefix + "":"" + name + "":"" + age.ToStr() + "":"" + suffix
}

result <- """"
for i <- 0, i < 25000, i <- i + 1 {
    result <- format(age: i, name: ""A"", suffix: ""Z"", prefix: ""P"")
}
";

    private const string ConcurrencySpawnJoinCode = @"
func worker(x:int) -> int {
    return x + 1
}

sum <- 0
for i <- 0, i < 10000, i <- i + 1 {
    t <- spawn(worker, i)
    t.Start()
    sum <- sum + t.Join()
}
";

    private const string ConcurrencyChannelSpscCode = @"
ch <- ChannelCreateBounded(1024)
sum <- AtomicIntCreate(0)
producerDone <- AtomicIntCreate(0)
spin <- 0
spinLimit <- 10000000

producer <- spawn(() -> {
    for i <- 0, i < 200000, i <- i + 1 {
        ChannelSend(ch, i)
    }
    AtomicIntSet(producerDone, 1)
    ChannelClose(ch)
})
producer.Start()

while true {
    v <- null
    try {
        v <- ChannelTryReceive(ch, 10)
    } catch {
        break
    }
    if v == null && AtomicIntGet(producerDone) == 1 {
        break
    }
    if v != null {
        AtomicIntAdd(sum, 1)
    }
    spin <- spin + 1
    if spin > spinLimit {
        break
    }
}

result <- AtomicIntGet(sum)
AtomicIntDispose(sum)
AtomicIntDispose(producerDone)
ChannelDispose(ch)
";

    private const string ConcurrencyChannelMpmcCode = @"
ch <- ChannelCreateBounded(2048)
producerDone <- AtomicIntCreate(0)
consumed <- AtomicIntCreate(0)
spin <- AtomicIntCreate(0)
spinLimit <- 20000000

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
        v <- null
        try {
            v <- ChannelTryReceive(ch, 10)
        } catch {
            break
        }
        if v != null {
            AtomicIntIncrement(consumed)
        } elif AtomicIntGet(producerDone) == 4 {
            break
        }
        currentSpin <- AtomicIntIncrement(spin)
        if currentSpin > spinLimit {
            break
        }
    }
}

p1 <- spawn(producer, 0, 50000)
p2 <- spawn(producer, 50000, 100000)
p3 <- spawn(producer, 100000, 150000)
p4 <- spawn(producer, 150000, 200000)

c1 <- spawn(consumer)
c2 <- spawn(consumer)
c3 <- spawn(consumer)
c4 <- spawn(consumer)

p1.Start()
p2.Start()
p3.Start()
p4.Start()
c1.Start()
c2.Start()
c3.Start()
c4.Start()

p1.Join()
p2.Join()
p3.Join()
p4.Join()
c1.Join()
c2.Join()
c3.Join()
c4.Join()

result <- AtomicIntGet(consumed)
AtomicIntDispose(producerDone)
AtomicIntDispose(consumed)
AtomicIntDispose(spin)
ChannelDispose(ch)
";

    private const string ConcurrencySemaphoreContentionCode = @"
sem <- SemaphoreCreate(1, 1)
counter <- AtomicIntCreate(0)

func worker() -> void {
    for i <- 0, i < 12500, i <- i + 1 {
        SemaphoreAcquire(sem)
        AtomicIntIncrement(counter)
        SemaphoreRelease(sem)
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

t1.Start()
t2.Start()
t3.Start()
t4.Start()
t5.Start()
t6.Start()
t7.Start()
t8.Start()

t1.Join()
t2.Join()
t3.Join()
t4.Join()
t5.Join()
t6.Join()
t7.Join()
t8.Join()

result <- AtomicIntGet(counter)
AtomicIntDispose(counter)
SemaphoreDispose(sem)
";

    private const string ConcurrencyMutexAtomicCode = @"
mutex <- MutexCreate()
counter <- AtomicIntCreate(0)

func worker() -> void {
    for i <- 0, i < 125000, i <- i + 1 {
        MutexLock(mutex)
        AtomicIntIncrement(counter)
        MutexUnlock(mutex)
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

t1.Start()
t2.Start()
t3.Start()
t4.Start()
t5.Start()
t6.Start()
t7.Start()
t8.Start()

t1.Join()
t2.Join()
t3.Join()
t4.Join()
t5.Join()
t6.Join()
t7.Join()
t8.Join()

result <- AtomicIntGet(counter)
AtomicIntDispose(counter)
MutexDispose(mutex)
";

    private const string ConcurrencyAsyncFanOutCode = @"
async func work(x:int) -> int {
    return x + 1
}

tasks <- {}
for i <- 0, i < 2000, i <- i + 1 {
    tasks.Add(work(i))
}

sum <- 0
for t in tasks {
    v <- await t
    sum <- sum + v
}
";
}
