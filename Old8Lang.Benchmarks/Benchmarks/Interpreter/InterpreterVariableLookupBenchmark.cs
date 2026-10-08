using BenchmarkDotNet.Attributes;

namespace Old8Lang.Benchmarks.Benchmarks.Interpreter;

/// <summary>
/// 变量查找性能基准测试
/// </summary>
public class InterpreterVariableLookupBenchmark : InterpreterBenchmarkBase
{
    private const string LocalVariableLookupCode = @"
func test() {
    local_var <- 100
    sum <- 0
    for i <- 0, i < 1000, i <- i + 1 {
        sum <- sum + local_var
    }
    return sum
}
test()
";

    private const string OuterScopeLookupCode = @"
outer_var <- 100
func test() {
    sum <- 0
    for i <- 0, i < 1000, i <- i + 1 {
        sum <- sum + outer_var
    }
    return sum
}
test()
";

    private const string GlobalVariableLookupCode = @"
global_var <- 100
func outer() {
    func inner() {
        sum <- 0
        for i <- 0, i < 1000, i <- i + 1 {
            sum <- sum + global_var
        }
        return sum
    }
    return inner()
}
outer()
";

    private const string MultipleVariableLookupCode = @"
a <- 1
b <- 2
c <- 3
d <- 4
e <- 5
func test() {
    sum <- 0
    for i <- 0, i < 200, i <- i + 1 {
        sum <- sum + a + b + c + d + e
    }
    return sum
}
test()
";

    [Benchmark(Description = "局部变量查找 (1000次)")]
    public void LocalVariableLookup()
    {
        ExecuteCode(LocalVariableLookupCode);
    }

    [Benchmark(Description = "外层作用域变量查找 (1000次)")]
    public void OuterScopeLookup()
    {
        ExecuteCode(OuterScopeLookupCode);
    }

    [Benchmark(Description = "全局变量查找 (1000次)")]
    public void GlobalVariableLookup()
    {
        ExecuteCode(GlobalVariableLookupCode);
    }

    [Benchmark(Description = "多变量查找 (1000次)")]
    public void MultipleVariableLookup()
    {
        ExecuteCode(MultipleVariableLookupCode);
    }
}
