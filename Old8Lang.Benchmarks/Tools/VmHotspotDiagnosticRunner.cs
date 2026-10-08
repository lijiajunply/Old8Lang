using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Old8Lang.Bytecode;
using Old8Lang.Bytecode.VM;
using Old8Lang.Interpreter;

namespace Old8Lang.Benchmarks.Tools;

/// <summary>
/// VM 热点诊断执行器 - 使用轻量级重复测量输出 compile/execute/alloc 证据。
/// </summary>
public static class VmHotspotDiagnosticRunner
{
    private const int LargeFileSeed = 20260301;
    private const int Iterations = 6;

    public static string Run(string reportsDir)
    {
        Directory.CreateDirectory(reportsDir);

        var scenarios = new[]
        {
            new VmDiagnosticScenario("LargeFile_10k", File.ReadAllText(ResolveTestDataPath("vm_large_10000.old8"))),
            new VmDiagnosticScenario("LargeFile_50k_Generated", GenerateLargeFile50kCode()),
            new VmDiagnosticScenario("LargeClosureCapture_HighFreq", EdgeClosureHighFreqCode),
            new VmDiagnosticScenario("ChannelTryReceive_Timeout0", ChannelTryReceiveTimeout0Code),
            new VmDiagnosticScenario("ChannelTryReceive_Timeout10", ChannelTryReceiveTimeout10Code),
            new VmDiagnosticScenario("SpawnJoin_10k", SpawnJoin10kCode),
            new VmDiagnosticScenario("Await_10k", Await10kCode)
        };

        var results = scenarios.Select(MeasureScenario).ToArray();
        var report = new VmHotspotDiagnosticReport(
            DateTime.Now,
            Environment.Version.ToString(),
            Iterations,
            results);

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        var markdownPath = Path.Combine(reportsDir, $"VM_Hotspot_Diagnostic_Report_{timestamp}.md");
        var jsonPath = Path.Combine(reportsDir, $"VM_Hotspot_Diagnostic_Report_{timestamp}.json");

        File.WriteAllText(markdownPath, BuildMarkdown(report));
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
        return markdownPath;
    }

    private static VmDiagnosticScenarioResult MeasureScenario(VmDiagnosticScenario scenario)
    {
        var compileSamples = new List<PhaseMeasurement>(Iterations);
        var executeSamples = new List<PhaseMeasurement>(Iterations);

        for (var i = 0; i < Iterations; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var compileMeasurement = MeasurePhase(() =>
            {
                var interpreter = new LangInterpreter();
                var ast = interpreter.Build(scenario.Code);
                var compiler = new BytecodeCompiler();
                return compiler.Compile(ast);
            }, out BytecodeFile bytecode);
            compileSamples.Add(compileMeasurement);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var executeMeasurement = MeasurePhase(() =>
            {
                var vm = new VirtualMachine(bytecode);
                vm.Execute();
                return 0;
            }, out _);
            executeSamples.Add(executeMeasurement);
        }

        return new VmDiagnosticScenarioResult(
            scenario.Name,
            CreatePhaseSummary("CompileToBytecode", compileSamples),
            CreatePhaseSummary("VMExecute", executeSamples));
    }

    private static PhaseMeasurement MeasurePhase<T>(Func<T> action, out T result)
    {
        var threadAllocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var gc0Before = GC.CollectionCount(0);
        var gc1Before = GC.CollectionCount(1);
        var gc2Before = GC.CollectionCount(2);
        var stopwatch = Stopwatch.StartNew();

        result = action();

        stopwatch.Stop();
        return new PhaseMeasurement(
            stopwatch.Elapsed.TotalMilliseconds,
            GC.GetAllocatedBytesForCurrentThread() - threadAllocatedBefore,
            GC.CollectionCount(0) - gc0Before,
            GC.CollectionCount(1) - gc1Before,
            GC.CollectionCount(2) - gc2Before);
    }

    private static VmDiagnosticPhaseSummary CreatePhaseSummary(string phaseName, IReadOnlyList<PhaseMeasurement> samples)
    {
        return new VmDiagnosticPhaseSummary(
            phaseName,
            Median(samples.Select(s => s.ElapsedMilliseconds)),
            Median(samples.Select(s => (double)s.AllocatedBytes)),
            samples.Sum(s => s.Gen0Collections),
            samples.Sum(s => s.Gen1Collections),
            samples.Sum(s => s.Gen2Collections));
    }

    private static string BuildMarkdown(VmHotspotDiagnosticReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Old8Lang VM Hotspot Diagnostic Report");
        sb.AppendLine();
        sb.AppendLine($"- GeneratedAt: {report.GeneratedAt:O}");
        sb.AppendLine($"- Runtime: `{report.RuntimeVersion}`");
        sb.AppendLine($"- IterationsPerScenario: `{report.IterationsPerScenario}`");
        sb.AppendLine();
        sb.AppendLine("| Scenario | Phase | Median(ms) | MedianAllocated(bytes) | Gen0 | Gen1 | Gen2 |");
        sb.AppendLine("|---|---|---:|---:|---:|---:|---:|");

        foreach (var result in report.Scenarios)
        {
            sb.AppendLine(
                $"| {result.Name} | {result.Compile.PhaseName} | {result.Compile.MedianMilliseconds:F3} | {result.Compile.MedianAllocatedBytes:F0} | {result.Compile.Gen0Collections} | {result.Compile.Gen1Collections} | {result.Compile.Gen2Collections} |");
            sb.AppendLine(
                $"| {result.Name} | {result.Execute.PhaseName} | {result.Execute.MedianMilliseconds:F3} | {result.Execute.MedianAllocatedBytes:F0} | {result.Execute.Gen0Collections} | {result.Execute.Gen1Collections} | {result.Execute.Gen2Collections} |");
        }

        return sb.ToString();
    }

    private static string ResolveTestDataPath(string fileName)
    {
        var directPath = Path.Combine(AppContext.BaseDirectory, "TestData", fileName);
        if (File.Exists(directPath))
        {
            return directPath;
        }

        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidateInCurrent = Path.Combine(current.FullName, "TestData", fileName);
            if (File.Exists(candidateInCurrent))
            {
                return candidateInCurrent;
            }

            var candidateInProject = Path.Combine(current.FullName, "Old8Lang.Benchmarks", "TestData", fileName);
            if (File.Exists(candidateInProject))
            {
                return candidateInProject;
            }

            current = current.Parent;
        }

        throw new FileNotFoundException($"固定大文件测试数据不存在: {directPath}");
    }

    private static string GenerateLargeFile50kCode()
    {
        var generatedFilePath = Path.Combine(Path.GetTempPath(), "vm_large_50000.diagnostic.generated.old8");
        try
        {
            TestDataGenerator.GenerateVmLargeScript(50_000, LargeFileSeed, generatedFilePath);
            return File.ReadAllText(generatedFilePath);
        }
        finally
        {
            if (File.Exists(generatedFilePath))
            {
                File.Delete(generatedFilePath);
            }
        }
    }

    private static double Median(IEnumerable<double> values)
    {
        var ordered = values.OrderBy(v => v).ToArray();
        if (ordered.Length == 0)
        {
            return 0;
        }

        var middle = ordered.Length / 2;
        return ordered.Length % 2 == 1 ? ordered[middle] : (ordered[middle - 1] + ordered[middle]) / 2d;
    }

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

    private const string ChannelTryReceiveTimeout0Code = @"
ch <- ChannelCreateBounded(2048)
count <- AtomicIntCreate(0)
producerDone <- AtomicIntCreate(0)

producer <- spawn(() -> {
    for i <- 0, i < 40000, i <- i + 1 {
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

    private const string ChannelTryReceiveTimeout10Code = @"
ch <- ChannelCreateBounded(2048)
count <- AtomicIntCreate(0)
producerDone <- AtomicIntCreate(0)

producer <- spawn(() -> {
    for i <- 0, i < 40000, i <- i + 1 {
        ChannelSend(ch, i)
    }
    AtomicIntSet(producerDone, 1)
    ChannelClose(ch)
})
producer.Start()

while true {
    v <- ChannelTryReceive(ch, 10)
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

    private const string SpawnJoin10kCode = @"
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

    private const string Await10kCode = @"
async func worker(x:int) -> int {
    return x + 1
}

sum <- 0
for i <- 0, i < 10000, i <- i + 1 {
    sum <- sum + await worker(i)
}
result <- sum
";

    private sealed record VmDiagnosticScenario(string Name, string Code);

    private sealed record PhaseMeasurement(
        double ElapsedMilliseconds,
        long AllocatedBytes,
        int Gen0Collections,
        int Gen1Collections,
        int Gen2Collections);

    private sealed record VmDiagnosticPhaseSummary(
        string PhaseName,
        double MedianMilliseconds,
        double MedianAllocatedBytes,
        int Gen0Collections,
        int Gen1Collections,
        int Gen2Collections);

    private sealed record VmDiagnosticScenarioResult(
        string Name,
        VmDiagnosticPhaseSummary Compile,
        VmDiagnosticPhaseSummary Execute);

    private sealed record VmHotspotDiagnosticReport(
        DateTime GeneratedAt,
        string RuntimeVersion,
        int IterationsPerScenario,
        IReadOnlyList<VmDiagnosticScenarioResult> Scenarios);
}
