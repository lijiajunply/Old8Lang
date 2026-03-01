using System.Diagnostics;
using System.Text;
using VM = Old8Lang.Bytecode.VM.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Performance;

[Collection("Sequential")]
public class VMLargeFilePerformanceTests
{
    [Fact]
    public void LargeFile_10k_CompileAndExecute_CompletesUnderThreshold()
    {
        var code = ReadFixedLargeScript();
        var sw = Stopwatch.StartNew();
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();
        sw.Stop();

        Assert.True(sw.Elapsed.TotalSeconds < 8,
            $"10k fixed script compile+execute took {sw.Elapsed.TotalSeconds:F2}s, expected < 8s");
    }

    [Fact]
    public void LargeFile_50k_Generated_CompileOnly_CompletesUnderThreshold()
    {
        var code = GenerateLargeScript(50_000);
        var sw = Stopwatch.StartNew();
        _ = CompileHelper.CompileToBytecode(code);
        sw.Stop();

        Assert.True(sw.Elapsed.TotalSeconds < 15,
            $"50k generated script compile-only took {sw.Elapsed.TotalSeconds:F2}s, expected < 15s");
    }

    private static string ReadFixedLargeScript()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "Old8Lang.Benchmarks", "TestData", "vm_large_10000.old8");
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            current = current.Parent;
        }

        throw new FileNotFoundException("无法定位 vm_large_10000.old8");
    }

    private static string GenerateLargeScript(int lineCount)
    {
        var sb = new StringBuilder();
        sb.AppendLine("func transform(x:int, offset:int) -> int {");
        sb.AppendLine("    return (x * 3 + offset) % 100000");
        sb.AppendLine("}");
        sb.AppendLine("data <- {}");
        sb.AppendLine("result <- 0");

        for (var i = 0; i < lineCount; i++)
        {
            var value = (i * 7919) % 1_000_000 + 1;
            switch (i % 4)
            {
                case 0:
                    sb.AppendLine($"data.Add(transform({value}, {i % 97}))");
                    break;
                case 1:
                    sb.AppendLine($"result <- result + transform({value}, {i % 131})");
                    break;
                case 2:
                    sb.AppendLine($"if {value} % 2 == 0 {{ result <- result + {i % 17} }}");
                    break;
                default:
                    sb.AppendLine($"result <- result + ({value} % {(i % 23) + 2})");
                    break;
            }
        }

        sb.AppendLine("if len(data) > 0 {");
        sb.AppendLine("    result <- result + len(data)");
        sb.AppendLine("}");
        return sb.ToString();
    }
}
