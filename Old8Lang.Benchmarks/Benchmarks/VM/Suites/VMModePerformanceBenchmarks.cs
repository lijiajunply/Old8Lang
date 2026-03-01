using BenchmarkDotNet.Attributes;
using Old8Lang.Benchmarks.Benchmarks.VM.Base;
using Old8Lang.Bytecode;

namespace Old8Lang.Benchmarks.Benchmarks.VM.Suites;

/// <summary>
/// VM 模式性能基准
/// </summary>
public class VMModePerformanceBenchmarks : VMBenchmarkBase
{
    private const string ArithmeticLoopCode = @"
sum <- 0
for i <- 0, i < 50000, i <- i + 1 {
    sum <- sum + i
}
";

    private const string DenseFunctionCallCode = @"
func add(a:int, b:int) -> int {
    return a + b
}

sum <- 0
for i <- 0, i < 30000, i <- i + 1 {
    sum <- add(sum, i)
}
";

    private const string DefaultAndNamedArgsCode = @"
func format(name:string, age:int, prefix: ""User"") -> string {
    return prefix + "":"" + name + "":"" + age.ToStr()
}

result <- """"
for i <- 0, i < 10000, i <- i + 1 {
    result <- format(age: i, name: ""A"", prefix: ""U"")
}
";

    private BytecodeFile _arithmeticLoopBytecode = null!;
    private BytecodeFile _denseFunctionCallBytecode = null!;
    private BytecodeFile _defaultAndNamedArgsBytecode = null!;

    [GlobalSetup]
    public void Setup()
    {
        _arithmeticLoopBytecode = CompileToBytecode(ArithmeticLoopCode);
        _denseFunctionCallBytecode = CompileToBytecode(DenseFunctionCallCode);
        _defaultAndNamedArgsBytecode = CompileToBytecode(DefaultAndNamedArgsCode);
    }

    [Benchmark(Description = "VM_ArithmeticLoop", Baseline = true)]
    public void ArithmeticLoop()
    {
        ExecuteBytecode(_arithmeticLoopBytecode);
    }

    [Benchmark(Description = "VM_DenseFunctionCall")]
    public void DenseFunctionCall()
    {
        ExecuteBytecode(_denseFunctionCallBytecode);
    }

    [Benchmark(Description = "VM_DefaultAndNamedArgs")]
    public void DefaultAndNamedArgs()
    {
        ExecuteBytecode(_defaultAndNamedArgsBytecode);
    }
}
