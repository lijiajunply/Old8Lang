using System.Diagnostics;
using Old8Lang.Bytecode;
using Old8Lang.Bytecode.VM;
using Old8Lang.Interpreter;

namespace Old8Lang.Benchmarks;

/// <summary>
/// VM 性能报告生成器（输出到 Reports/）
/// </summary>
public static class VMPerformanceReport
{
    private sealed record Scenario(string Name, string Code, int Iterations, int Warmups);

    private sealed record ScenarioResult(string Name, double AvgMs, double P95Ms, long AvgAllocatedBytes);

    public static void RunAndWriteReport()
    {
        var scenarios = new[]
        {
            new Scenario(
                "ArithmeticLoop",
                @"
sum <- 0
for i <- 0, i < 50000, i <- i + 1 {
    sum <- sum + i
}
",
                Iterations: 20,
                Warmups: 3
            ),
            new Scenario(
                "DenseFunctionCall",
                @"
func add(a:int, b:int) -> int {
    return a + b
}

sum <- 0
for i <- 0, i < 30000, i <- i + 1 {
    sum <- add(sum, i)
}
",
                Iterations: 20,
                Warmups: 3
            ),
            new Scenario(
                "DefaultAndNamedArgs",
                @"
func format(name:string, age:int, prefix: ""User"") -> string {
    return prefix + "":"" + name + "":"" + age.ToStr()
}

result <- """"
for i <- 0, i < 10000, i <- i + 1 {
    result <- format(age: i, name: ""A"", prefix: ""U"")
}
",
                Iterations: 20,
                Warmups: 3
            )
        };

        var results = new List<ScenarioResult>(scenarios.Length);
        foreach (var scenario in scenarios)
        {
            results.Add(RunScenario(scenario));
        }

        var reportsDir = Path.Combine(Directory.GetCurrentDirectory(), "Reports");
        Directory.CreateDirectory(reportsDir);
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var reportPath = Path.Combine(reportsDir, $"VM_Performance_Report_{timestamp}.txt");

        using var writer = new StreamWriter(reportPath);
        writer.WriteLine("Old8Lang VM Performance Report");
        writer.WriteLine($"GeneratedAt: {DateTime.Now:O}");
        writer.WriteLine();
        writer.WriteLine("Scenario, AvgMs, P95Ms, AvgAllocatedBytes");
        foreach (var result in results)
        {
            writer.WriteLine($"{result.Name}, {result.AvgMs:F3}, {result.P95Ms:F3}, {result.AvgAllocatedBytes}");
        }

        Console.WriteLine($"VM 性能报告已生成: {reportPath}");
    }

    private static ScenarioResult RunScenario(Scenario scenario)
    {
        var bytecodeFile = CompileToBytecode(scenario.Code);
        for (int i = 0; i < scenario.Warmups; i++)
        {
            ExecuteBytecode(bytecodeFile);
        }

        var elapsedMs = new double[scenario.Iterations];
        var allocatedBytes = new long[scenario.Iterations];

        for (int i = 0; i < scenario.Iterations; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long beforeAlloc = GC.GetAllocatedBytesForCurrentThread();
            var sw = Stopwatch.StartNew();
            ExecuteBytecode(bytecodeFile);
            sw.Stop();
            long afterAlloc = GC.GetAllocatedBytesForCurrentThread();

            elapsedMs[i] = sw.Elapsed.TotalMilliseconds;
            allocatedBytes[i] = Math.Max(0, afterAlloc - beforeAlloc);
        }

        var sorted = elapsedMs.OrderBy(x => x).ToArray();
        int p95Index = (int)Math.Ceiling(sorted.Length * 0.95) - 1;
        p95Index = Math.Clamp(p95Index, 0, sorted.Length - 1);

        return new ScenarioResult(
            scenario.Name,
            AvgMs: elapsedMs.Average(),
            P95Ms: sorted[p95Index],
            AvgAllocatedBytes: (long)allocatedBytes.Average()
        );
    }

    private static BytecodeFile CompileToBytecode(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        var compiler = new BytecodeCompiler();
        return compiler.Compile(ast);
    }

    private static void ExecuteBytecode(BytecodeFile bytecodeFile)
    {
        var vm = new VirtualMachine(bytecodeFile);
        vm.Execute();
    }
}
