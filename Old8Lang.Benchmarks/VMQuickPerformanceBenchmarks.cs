using BenchmarkDotNet.Attributes;
using Old8Lang.Bytecode;

namespace Old8Lang.Benchmarks;

/// <summary>
/// VM Quick 性能基准（PR 快速回归）
/// </summary>
public class VMQuickPerformanceBenchmarks : VMBenchmarkQuickBase
{
    private const int LargeFileSeed = 20260301;

    private string _largeFile10kCode = string.Empty;
    private string _largeFile50kCode = string.Empty;
    private string _generatedFilePath = string.Empty;

    private BytecodeFile _largeFile10kBytecode = null!;
    private BytecodeFile _largeFile50kBytecode = null!;
    private BytecodeFile _edgeDeepRecursionBytecode = null!;
    private BytecodeFile _edgeHighArgCountBytecode = null!;
    private BytecodeFile _edgeHighThrowRateTryCatchBytecode = null!;
    private BytecodeFile _edgeClosureHighFreqBytecode = null!;
    private BytecodeFile _concurrencyChannelMpmc4Bytecode = null!;
    private BytecodeFile _concurrencyMutexAtomic4Bytecode = null!;

    [GlobalSetup]
    public void Setup()
    {
        var fixedPath = Path.Combine(AppContext.BaseDirectory, "TestData", "vm_large_10000.old8");
        if (!File.Exists(fixedPath))
        {
            throw new FileNotFoundException($"固定大文件测试数据不存在: {fixedPath}");
        }

        _largeFile10kCode = File.ReadAllText(fixedPath);
        _generatedFilePath = Path.Combine(Path.GetTempPath(), "vm_large_50000.quick.generated.old8");
        TestDataGenerator.GenerateVmLargeScript(50_000, LargeFileSeed, _generatedFilePath);
        _largeFile50kCode = File.ReadAllText(_generatedFilePath);

        _largeFile10kBytecode = CompileToBytecode(_largeFile10kCode);
        _largeFile50kBytecode = CompileToBytecode(_largeFile50kCode);
        _edgeDeepRecursionBytecode = CompileToBytecode(EdgeDeepRecursionCode);
        _edgeHighArgCountBytecode = CompileToBytecode(EdgeHighArgCountCode);
        _edgeHighThrowRateTryCatchBytecode = CompileToBytecode(EdgeHighThrowRateTryCatchCode);
        _edgeClosureHighFreqBytecode = CompileToBytecode(EdgeClosureHighFreqCode);
        _concurrencyChannelMpmc4Bytecode = CompileToBytecode(BuildConcurrencyChannelMpmcCode(4, 25_000));
        _concurrencyMutexAtomic4Bytecode = CompileToBytecode(BuildConcurrencyMutexAtomicCode(4, 80_000));
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (!string.IsNullOrWhiteSpace(_generatedFilePath) && File.Exists(_generatedFilePath))
        {
            File.Delete(_generatedFilePath);
        }
    }

    [Benchmark(Description = "VMXQ_LargeFile_CompileAndExecute_10k")]
    public void LargeFileCompileAndExecute10k()
    {
        ExecuteBytecode(_largeFile10kBytecode);
    }

    [Benchmark(Description = "VMXQ_LargeFile_CompileAndExecute_50k_Generated")]
    public void LargeFileCompileAndExecute50kGenerated()
    {
        ExecuteBytecode(_largeFile50kBytecode);
    }

    [Benchmark(Description = "VMXQ_Edge_DeepRecursion_NearLimit")]
    public void EdgeDeepRecursionNearLimit()
    {
        ExecuteBytecode(_edgeDeepRecursionBytecode);
    }

    [Benchmark(Description = "VMXQ_Edge_HighArgCount_CallHotPath")]
    public void EdgeHighArgCountCallHotPath()
    {
        ExecuteBytecode(_edgeHighArgCountBytecode);
    }

    [Benchmark(Description = "VMXQ_Edge_HighThrowRate_TryCatch")]
    public void EdgeHighThrowRateTryCatch()
    {
        ExecuteBytecode(_edgeHighThrowRateTryCatchBytecode);
    }

    [Benchmark(Description = "VMXQ_Edge_LargeClosureCapture_HighFreq")]
    public void EdgeLargeClosureCaptureHighFreq()
    {
        ExecuteBytecode(_edgeClosureHighFreqBytecode);
    }

    [Benchmark(Description = "VMXQ_Concurrency_Channel_MPMC_4Workers")]
    public void ConcurrencyChannelMpmc4Workers()
    {
        ExecuteAndAssertGlobalInt(_concurrencyChannelMpmc4Bytecode, "result", 100_000);
    }

    [Benchmark(Description = "VMXQ_Concurrency_MutexAtomicCounter_4Workers")]
    public void ConcurrencyMutexAtomicCounter4Workers()
    {
        ExecuteAndAssertGlobalInt(_concurrencyMutexAtomic4Bytecode, "result", 320_000);
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

    private const string EdgeHighThrowRateTryCatchCode = @"
sum <- 0
for i <- 0, i < 40000, i <- i + 1 {
    try {
        if i % 4 == 0 {
            throw ""edge-high-throw""
        }
        sum <- sum + i
    } catch {
        sum <- sum + 3
    }
}
result <- sum
";

    private const string EdgeClosureHighFreqCode = @"
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

    private static string BuildConcurrencyChannelMpmcCode(int workers, int itemsPerWorker)
    {
        return $@"
ch <- ChannelCreateBounded(2048)
producerDone <- AtomicIntCreate(0)
consumed <- AtomicIntCreate(0)

func producer(start:int, end:int) -> void {{
    for i <- start, i < end, i <- i + 1 {{
        ChannelSend(ch, i)
    }}
    done <- AtomicIntIncrement(producerDone)
    if done == {workers} {{
        ChannelClose(ch)
    }}
}}

func consumer() -> void {{
    while true {{
        v <- null
        try {{
            v <- ChannelTryReceive(ch, 10)
        }} catch {{
            break
        }}
        if v != null {{
            AtomicIntIncrement(consumed)
        }} elif AtomicIntGet(producerDone) == {workers} {{
            break
        }}
    }}
}}

" + BuildSpawnAndJoin("producer", "consumer", workers, itemsPerWorker) + $@"
result <- AtomicIntGet(consumed)
AtomicIntDispose(producerDone)
AtomicIntDispose(consumed)
ChannelDispose(ch)
";
    }

    private static string BuildConcurrencyMutexAtomicCode(int workers, int iterationsPerWorker)
    {
        return $@"
mutex <- MutexCreate()
counter <- AtomicIntCreate(0)

func worker() -> void {{
    for i <- 0, i < {iterationsPerWorker}, i <- i + 1 {{
        MutexLock(mutex)
        AtomicIntIncrement(counter)
        MutexUnlock(mutex)
    }}
}}

" + BuildSpawnAndJoin("worker", null, workers, 0) + @"
result <- AtomicIntGet(counter)
AtomicIntDispose(counter)
MutexDispose(mutex)
";
    }

    private static string BuildSpawnAndJoin(string producerFunc, string? consumerFunc, int workers, int itemsPerWorker)
    {
        var lines = new List<string>();
        for (var i = 0; i < workers; i++)
        {
            if (producerFunc == "producer")
            {
                var start = i * itemsPerWorker;
                var end = (i + 1) * itemsPerWorker;
                lines.Add($"p{i} <- spawn(producer, {start}, {end})");
            }
            else
            {
                lines.Add($"w{i} <- spawn(worker)");
            }
        }

        if (!string.IsNullOrWhiteSpace(consumerFunc))
        {
            for (var i = 0; i < workers; i++)
            {
                lines.Add($"c{i} <- spawn(consumer)");
            }
        }

        for (var i = 0; i < workers; i++)
        {
            lines.Add(producerFunc == "producer" ? $"p{i}.Start()" : $"w{i}.Start()");
        }

        if (!string.IsNullOrWhiteSpace(consumerFunc))
        {
            for (var i = 0; i < workers; i++)
            {
                lines.Add($"c{i}.Start()");
            }
        }

        for (var i = 0; i < workers; i++)
        {
            lines.Add(producerFunc == "producer" ? $"p{i}.Join()" : $"w{i}.Join()");
        }

        if (!string.IsNullOrWhiteSpace(consumerFunc))
        {
            for (var i = 0; i < workers; i++)
            {
                lines.Add($"c{i}.Join()");
            }
        }

        return string.Join("\n", lines) + "\n";
    }
}
