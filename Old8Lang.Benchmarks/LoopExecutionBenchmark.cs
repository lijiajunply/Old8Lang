using BenchmarkDotNet.Attributes;

namespace Old8Lang.Benchmarks;

/// <summary>
/// 循环执行性能基准测试
/// </summary>
public class LoopExecutionBenchmark : InterpreterBenchmarkBase
{
    private const string SimpleLoopCode = @"
sum <- 0
for i <- 0, i < 1000, i <- i + 1 {
    sum <- sum + i
}
return sum
";

    private const string NestedLoopCode = @"
sum <- 0
for i <- 0, i < 100, i <- i + 1 {
    for j <- 0, j < 100, j <- j + 1 {
        sum <- sum + i * j
    }
}
return sum
";

    private const string TripleNestedLoopCode = @"
sum <- 0
for i <- 0, i < 50, i <- i + 1 {
    for j <- 0, j < 50, j <- j + 1 {
        for k <- 0, k < 10, k <- k + 1 {
            sum <- sum + i + j + k
        }
    }
}
return sum
";

    private const string LoopWithFunctionCallCode = @"
func add(a, b) {
    return a + b
}

sum <- 0
for i <- 0, i < 500, i <- i + 1 {
    sum <- add(sum, i)
}
return sum
";

    private const string LoopWithArrayAccessCode = @"
arr <- [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]
sum <- 0
for i <- 0, i < 100, i <- i + 1 {
    for j <- 0, j < 10, j <- j + 1 {
        sum <- sum + arr[j]
    }
}
return sum
";

    [Benchmark(Description = "简单循环 (1000次迭代)", Baseline = true)]
    public void SimpleLoop()
    {
        ExecuteCode(SimpleLoopCode);
    }

    [Benchmark(Description = "嵌套循环 (100x100)")]
    public void NestedLoop()
    {
        ExecuteCode(NestedLoopCode);
    }

    [Benchmark(Description = "三层嵌套循环 (50x50x10)")]
    public void TripleNestedLoop()
    {
        ExecuteCode(TripleNestedLoopCode);
    }

    [Benchmark(Description = "循环内函数调用 (500次)")]
    public void LoopWithFunctionCall()
    {
        ExecuteCode(LoopWithFunctionCallCode);
    }

    [Benchmark(Description = "循环内数组访问 (1000次)")]
    public void LoopWithArrayAccess()
    {
        ExecuteCode(LoopWithArrayAccessCode);
    }
}
