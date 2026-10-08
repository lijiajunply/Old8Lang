using Old8Lang.AST.Expression.Value;
using System.Diagnostics;
using VM = Old8Lang.Bytecode.VM.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Performance;

[Collection("Sequential")]
public class VMEdgePerformanceTests
{
    [Fact]
    public void Edge_DeepRecursion_CompletesAndReturnsExpected()
    {
        var code = @"
            func deep(n:int) -> int {
                if n <= 0 {
                    return 0
                }
                return 1 + deep(n - 1)
            }

            result <- deep(120)
        ";

        var (result, elapsed) = ExecuteWithTiming(code);
        Assert.Equal(120, GetIntValue(result));
        Assert.True(elapsed.TotalSeconds < 3, $"Deep recursion took {elapsed.TotalSeconds:F2}s, expected < 3s");
    }

    [Fact]
    public void Edge_HeavyTryCatch_CompletesUnderThreshold()
    {
        var code = @"
            sum <- 0
            for i <- 0, i < 50000, i <- i + 1 {
                try {
                    if i % 10000 == 0 {
                        throw ""edge""
                    }
                    sum <- sum + i
                } catch {
                    sum <- sum + 1
                }
            }
            result <- sum
        ";

        var (result, elapsed) = ExecuteWithTiming(code);
        Assert.NotNull(result);
        Assert.True(elapsed.TotalSeconds < 5, $"Heavy try/catch took {elapsed.TotalSeconds:F2}s, expected < 5s");
    }

    [Fact]
    public void Edge_HighArgCountAndNamedArgs_CompletesUnderThreshold()
    {
        var code = @"
            func fold8(a:int, b:int, c:int, d:int, e:int, f:int, g:int, h:int) -> int {
                return a + b + c + d + e + f + g + h
            }

            func format(name:string, age:int, prefix: ""U"", suffix: ""X"") -> string {
                return prefix + "":"" + name + "":"" + age.ToStr() + "":"" + suffix
            }

            sum <- 0
            txt <- """"
            for i <- 0, i < 20000, i <- i + 1 {
                sum <- sum + fold8(i, 1, 2, 3, 4, 5, 6, 7)
                txt <- format(age: i, name: ""A"", suffix: ""Z"", prefix: ""P"")
            }
            result <- sum
        ";

        var (result, elapsed) = ExecuteWithTiming(code);
        Assert.NotNull(result);
        Assert.True(elapsed.TotalSeconds < 6, $"High arg count + named args took {elapsed.TotalSeconds:F2}s, expected < 6s");
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
