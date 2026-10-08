using BenchmarkDotNet.Attributes;

namespace Old8Lang.Benchmarks.Benchmarks.Interpreter;

/// <summary>
/// 函数调用性能基准测试
/// </summary>
public class InterpreterFunctionCallBenchmark : InterpreterBenchmarkBase
{
    private const string SimpleFunctionCallCode = @"
func add(a, b) {
    return a + b
}

sum <- 0
for i <- 0, i < 1000, i <- i + 1 {
    sum <- add(sum, i)
}
return sum
";

    private const string RecursiveFunctionCode = @"
func fibonacci(n) {
    if n <= 1 {
        return n
    }
    return fibonacci(n - 1) + fibonacci(n - 2)
}

return fibonacci(15)
";

    private const string DeepRecursionCode = @"
func countdown(n) {
    if n <= 0 {
        return 0
    }
    return n + countdown(n - 1)
}

return countdown(100)
";

    private const string ClosureFunctionCode = @"
func makeCounter() {
    count <- 0
    func increment() {
        count <- count + 1
        return count
    }
    return increment
}

counter <- makeCounter()
sum <- 0
for i <- 0, i < 500, i <- i + 1 {
    sum <- sum + counter()
}
return sum
";

    private const string NestedFunctionCallCode = @"
func outer(x) {
    func middle(y) {
        func inner(z) {
            return x + y + z
        }
        return inner(y + 1)
    }
    return middle(x + 1)
}

sum <- 0
for i <- 0, i < 200, i <- i + 1 {
    sum <- sum + outer(i)
}
return sum
";

    [Benchmark(Description = "简单函数调用 (1000次)", Baseline = true)]
    public void SimpleFunctionCall()
    {
        ExecuteCode(SimpleFunctionCallCode);
    }

    [Benchmark(Description = "递归函数 (fibonacci(15))")]
    public void RecursiveFunction()
    {
        ExecuteCode(RecursiveFunctionCode);
    }

    [Benchmark(Description = "深度递归 (100层)")]
    public void DeepRecursion()
    {
        ExecuteCode(DeepRecursionCode);
    }

    [Benchmark(Description = "闭包函数调用 (500次)")]
    public void ClosureFunction()
    {
        ExecuteCode(ClosureFunctionCode);
    }

    [Benchmark(Description = "嵌套函数调用 (200次)")]
    public void NestedFunctionCall()
    {
        ExecuteCode(NestedFunctionCallCode);
    }
}
