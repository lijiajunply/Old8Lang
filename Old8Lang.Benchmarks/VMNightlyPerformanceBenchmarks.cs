using BenchmarkDotNet.Attributes;
using Old8Lang.Bytecode;

namespace Old8Lang.Benchmarks;

/// <summary>
/// VM Nightly 性能基准（夜间完整回归）
/// </summary>
public class VMNightlyPerformanceBenchmarks : VMBenchmarkNightlyBase
{
    private const int LargeFileSeed = 20260301;

    private string _largeFile10kCode = string.Empty;
    private string _largeFile50kCode = string.Empty;
    private string _largeFile100kCode = string.Empty;
    private string _generatedFile50kPath = string.Empty;
    private string _generatedFile100kPath = string.Empty;

    private BytecodeFile _largeFile10kBytecode = null!;
    private BytecodeFile _largeFile50kBytecode = null!;
    private BytecodeFile _largeFile100kBytecode = null!;
    private BytecodeFile _largeFileModuleStyleBytecode = null!;
    private BytecodeFile _edgeDeepRecursionBytecode = null!;
    private BytecodeFile _edgeHighArgCountBytecode = null!;
    private BytecodeFile _edgeHighThrowRateTryCatchBytecode = null!;
    private BytecodeFile _edgeClosureHighFreqBytecode = null!;
    private BytecodeFile _concurrencyMpmc2Bytecode = null!;
    private BytecodeFile _concurrencyMpmc4Bytecode = null!;
    private BytecodeFile _concurrencyMpmc8Bytecode = null!;
    private BytecodeFile _concurrencyMutex2Bytecode = null!;
    private BytecodeFile _concurrencyMutex4Bytecode = null!;
    private BytecodeFile _concurrencyMutex8Bytecode = null!;
    private BytecodeFile _concurrencySemaphoreContentionBytecode = null!;
    private BytecodeFile _concurrencySpawnJoinBytecode = null!;

    [GlobalSetup]
    public void Setup()
    {
        var fixedPath = Path.Combine(AppContext.BaseDirectory, "TestData", "vm_large_10000.old8");
        if (!File.Exists(fixedPath))
        {
            throw new FileNotFoundException($"固定大文件测试数据不存在: {fixedPath}");
        }

        _largeFile10kCode = File.ReadAllText(fixedPath);
        _generatedFile50kPath = Path.Combine(Path.GetTempPath(), "vm_large_50000.nightly.generated.old8");
        _generatedFile100kPath = Path.Combine(Path.GetTempPath(), "vm_large_100000.nightly.generated.old8");
        TestDataGenerator.GenerateVmLargeScript(50_000, LargeFileSeed, _generatedFile50kPath);
        TestDataGenerator.GenerateVmLargeScript(100_000, LargeFileSeed + 1, _generatedFile100kPath);
        _largeFile50kCode = File.ReadAllText(_generatedFile50kPath);
        _largeFile100kCode = File.ReadAllText(_generatedFile100kPath);

        _largeFile10kBytecode = CompileToBytecode(_largeFile10kCode);
        _largeFile50kBytecode = CompileToBytecode(_largeFile50kCode);
        _largeFile100kBytecode = CompileToBytecode(_largeFile100kCode);
        _largeFileModuleStyleBytecode = CompileToBytecode(GenerateModuleStyleLargeFileCode());

        _edgeDeepRecursionBytecode = CompileToBytecode(EdgeDeepRecursionCode);
        _edgeHighArgCountBytecode = CompileToBytecode(EdgeHighArgCountCode);
        _edgeHighThrowRateTryCatchBytecode = CompileToBytecode(EdgeHighThrowRateTryCatchCode);
        _edgeClosureHighFreqBytecode = CompileToBytecode(EdgeClosureHighFreqCode);

        _concurrencyMpmc2Bytecode = CompileToBytecode(BuildConcurrencyChannelMpmcCode(2, 50_000));
        _concurrencyMpmc4Bytecode = CompileToBytecode(BuildConcurrencyChannelMpmcCode(4, 50_000));
        _concurrencyMpmc8Bytecode = CompileToBytecode(BuildConcurrencyChannelMpmcCode(8, 50_000));

        _concurrencyMutex2Bytecode = CompileToBytecode(BuildConcurrencyMutexAtomicCode(2, 150_000));
        _concurrencyMutex4Bytecode = CompileToBytecode(BuildConcurrencyMutexAtomicCode(4, 150_000));
        _concurrencyMutex8Bytecode = CompileToBytecode(BuildConcurrencyMutexAtomicCode(8, 150_000));
        _concurrencySemaphoreContentionBytecode = CompileToBytecode(ConcurrencySemaphoreContentionCode);
        _concurrencySpawnJoinBytecode = CompileToBytecode(ConcurrencySpawnJoinCode);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        DeleteIfExists(_generatedFile50kPath);
        DeleteIfExists(_generatedFile100kPath);
    }

    [Benchmark(Description = "VMXN_LargeFile_CompileAndExecute_10k")]
    public void LargeFileCompileAndExecute10k() => ExecuteBytecode(_largeFile10kBytecode);

    [Benchmark(Description = "VMXN_LargeFile_CompileAndExecute_50k_Generated")]
    public void LargeFileCompileAndExecute50kGenerated() => ExecuteBytecode(_largeFile50kBytecode);

    [Benchmark(Description = "VMXN_LargeFile_CompileAndExecute_100k_Generated")]
    public void LargeFileCompileAndExecute100kGenerated() => ExecuteBytecode(_largeFile100kBytecode);

    [Benchmark(Description = "VMXN_LargeFile_ModuleStyle_ImportLike")]
    public void LargeFileModuleStyleImportLike() => ExecuteBytecode(_largeFileModuleStyleBytecode);

    [Benchmark(Description = "VMXN_Edge_DeepRecursion_NearLimit")]
    public void EdgeDeepRecursionNearLimit() => ExecuteBytecode(_edgeDeepRecursionBytecode);

    [Benchmark(Description = "VMXN_Edge_HighArgCount_CallHotPath")]
    public void EdgeHighArgCountCallHotPath() => ExecuteBytecode(_edgeHighArgCountBytecode);

    [Benchmark(Description = "VMXN_Edge_HighThrowRate_TryCatch")]
    public void EdgeHighThrowRateTryCatch() => ExecuteBytecode(_edgeHighThrowRateTryCatchBytecode);

    [Benchmark(Description = "VMXN_Edge_LargeClosureCapture_HighFreq")]
    public void EdgeLargeClosureCaptureHighFreq() => ExecuteBytecode(_edgeClosureHighFreqBytecode);

    [Benchmark(Description = "VMXN_Concurrency_Channel_MPMC_2Workers")]
    public void ConcurrencyChannelMpmc2Workers() => ExecuteAndAssertGlobalInt(_concurrencyMpmc2Bytecode, "result", 100_000);

    [Benchmark(Description = "VMXN_Concurrency_Channel_MPMC_4Workers")]
    public void ConcurrencyChannelMpmc4Workers() => ExecuteAndAssertGlobalInt(_concurrencyMpmc4Bytecode, "result", 200_000);

    [Benchmark(Description = "VMXN_Concurrency_Channel_MPMC_8Workers")]
    public void ConcurrencyChannelMpmc8Workers() => ExecuteAndAssertGlobalInt(_concurrencyMpmc8Bytecode, "result", 400_000);

    [Benchmark(Description = "VMXN_Concurrency_MutexAtomicCounter_2Workers")]
    public void ConcurrencyMutexAtomicCounter2Workers() => ExecuteAndAssertGlobalInt(_concurrencyMutex2Bytecode, "result", 300_000);

    [Benchmark(Description = "VMXN_Concurrency_MutexAtomicCounter_4Workers")]
    public void ConcurrencyMutexAtomicCounter4Workers() => ExecuteAndAssertGlobalInt(_concurrencyMutex4Bytecode, "result", 600_000);

    [Benchmark(Description = "VMXN_Concurrency_MutexAtomicCounter_8Workers")]
    public void ConcurrencyMutexAtomicCounter8Workers() => ExecuteAndAssertGlobalInt(_concurrencyMutex8Bytecode, "result", 1_200_000);

    [Benchmark(Description = "VMXN_Concurrency_Semaphore_Contention")]
    public void ConcurrencySemaphoreContention() => ExecuteAndAssertGlobalInt(_concurrencySemaphoreContentionBytecode, "result", 120_000);

    [Benchmark(Description = "VMXN_Concurrency_SpawnJoin_Throughput")]
    public void ConcurrencySpawnJoinThroughput() => ExecuteAndAssertGlobalInt(_concurrencySpawnJoinBytecode, "result", 200_010_000);

    private static void DeleteIfExists(string path)
    {
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static string GenerateModuleStyleLargeFileCode()
    {
        var lines = new List<string>
        {
            "func mod_a(x:int) -> int { return x + 1 }",
            "func mod_b(x:int) -> int { return x * 2 }",
            "func mod_c(x:int) -> int { return x - 3 }",
            "sum <- 0"
        };

        for (var i = 0; i < 60_000; i++)
        {
            lines.Add($"sum <- sum + mod_c(mod_b(mod_a({i % 97})))");
        }

        lines.Add("result <- sum");
        return string.Join("\n", lines);
    }

    private const string EdgeDeepRecursionCode = @"
func deep(n:int) -> int {
    if n <= 0 {
        return 0
    }
    return 1 + deep(n - 1)
}

result <- deep(220)
";

    private const string EdgeHighArgCountCode = @"
func fold8(a:int, b:int, c:int, d:int, e:int, f:int, g:int, h:int) -> int {
    return a + b + c + d + e + f + g + h
}

sum <- 0
for i <- 0, i < 70000, i <- i + 1 {
    sum <- sum + fold8(i, 1, 2, 3, 4, 5, 6, 7)
}
result <- sum
";

    private const string EdgeHighThrowRateTryCatchCode = @"
sum <- 0
for i <- 0, i < 120000, i <- i + 1 {
    try {
        if i % 2 == 0 {
            throw ""nightly-throw""
        }
        sum <- sum + i
    } catch {
        sum <- sum + 5
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
for i <- 0, i < 150000, i <- i + 1 {
    sum <- sum + adder(i)
}
result <- sum
";

    private const string ConcurrencySemaphoreContentionCode = @"
sem <- SemaphoreCreate(4)
counter <- AtomicIntCreate(0)

func worker() -> void {
    for i <- 0, i < 30000, i <- i + 1 {
        SemaphoreAcquire(sem)
        AtomicIntIncrement(counter)
        SemaphoreRelease(sem)
    }
}

w1 <- spawn(worker)
w2 <- spawn(worker)
w3 <- spawn(worker)
w4 <- spawn(worker)
w1.Start()
w2.Start()
w3.Start()
w4.Start()
w1.Join()
w2.Join()
w3.Join()
w4.Join()

result <- AtomicIntGet(counter)
AtomicIntDispose(counter)
SemaphoreDispose(sem)
";

    private const string ConcurrencySpawnJoinCode = @"
func worker(x:int) -> int {
    return x + 1
}

sum <- 0
for i <- 0, i < 20000, i <- i + 1 {
    t <- spawn(worker, i)
    t.Start()
    sum <- sum + t.Join()
}
result <- sum
";

    private static string BuildConcurrencyChannelMpmcCode(int workers, int itemsPerWorker)
    {
        return $@"
ch <- ChannelCreateBounded(4096)
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
