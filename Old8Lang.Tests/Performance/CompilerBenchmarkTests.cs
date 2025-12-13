using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Old8Lang.LangParser;

namespace Old8Lang.Tests.Performance;

/// <summary>
/// 编译器性能基准测试
/// 测量编译时间、内存占用和生成代码的执行效率
/// </summary>
[Config(typeof(CompilerBenchmarkConfig))]
public class CompilerBenchmarkTests
{
    private LangInterpreter _interpreter;
    
    // 测试用代码片段
    private string _simpleCode;
    private string _mediumCode;
    private string _complexCode;
    
    /// <summary>
    /// 基准测试配置
    /// </summary>
    private class CompilerBenchmarkConfig : ManualConfig
    {
        public CompilerBenchmarkConfig()
        {
            AddJob(Job.Default
                .WithWarmupCount(3)
                .WithIterationCount(10)
                .WithInvocationCount(1)
                .WithId("Default"));
        }
    }
    
    /// <summary>
    /// 初始化测试数据
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _interpreter = new LangInterpreter();
        
        // 简单代码：基本赋值和算术运算
        _simpleCode = @"func simple_test() {
    a <- 123
    b <- 456
    c <- a + b
    d <- a * b
    e <- b / a
    f <- b % a
    return e
}
result <- simple_test()
";
        
        // 中等复杂度代码：包含循环和条件语句
        _mediumCode = @"func medium_test() {
    sum <- 0
    for i <- 0, i < 100, i <- i + 1 {
        if i % 2 == 0 {
            sum <- sum + i
        } else {
            sum <- sum - i
        }
    }
    return sum
}
result <- medium_test()
";
        
        // 复杂代码：包含递归函数和类
        _complexCode = @"func factorial(n) {
    if n <= 1 {
        return 1
    }
    return n * factorial(n - 1)
}

class ComplexClass {
    field1 <- 0
    field2 <- 0
    
    func init(a, b) {
        this.field1 <- a
        this.field2 <- b
    }
    
    func calculate() {
        result <- 0
        for i <- 0, i < 10, i <- i + 1 {
            result <- result + this.field1 * factorial(i)
        }
        return result + this.field2
    }
}

obj <- ComplexClass(5, 10)
result <- obj.calculate()
";
    }
    
    /// <summary>
    /// 测试简单代码的编译时间
    /// </summary>
    [Benchmark(Description = "Compile Simple Code")]
    public void CompileSimpleCode()
    {
        var ast = _interpreter.Build(_simpleCode);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "simple_test", _interpreter);
        compiledAction();
    }
    
    /// <summary>
    /// 测试中等复杂度代码的编译时间
    /// </summary>
    [Benchmark(Description = "Compile Medium Code")]
    public void CompileMediumCode()
    {
        var ast = _interpreter.Build(_mediumCode);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "medium_test", _interpreter);
        compiledAction();
    }
    
    /// <summary>
    /// 测试复杂代码的编译时间
    /// </summary>
    [Benchmark(Description = "Compile Complex Code")]
    public void CompileComplexCode()
    {
        var ast = _interpreter.Build(_complexCode);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "complex_test", _interpreter);
        compiledAction();
    }
    
    /// <summary>
    /// 测试简单代码的纯IL生成时间（不包含编译前的解析）
    /// </summary>
    [Benchmark(Description = "Generate IL for Simple Code")]
    public void GenerateILSimpleCode()
    {
        // 先解析AST，只测量IL生成时间
        var ast = _interpreter.Build(_simpleCode);
        
        // 测量IL生成时间
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "simple_il_test", _interpreter);
    }
    
    /// <summary>
    /// 测试中等复杂度代码的纯IL生成时间
    /// </summary>
    [Benchmark(Description = "Generate IL for Medium Code")]
    public void GenerateILMediumCode()
    {
        // 先解析AST，只测量IL生成时间
        var ast = _interpreter.Build(_mediumCode);
        
        // 测量IL生成时间
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "medium_il_test", _interpreter);
    }
    
    /// <summary>
    /// 测试复杂代码的纯IL生成时间
    /// </summary>
    [Benchmark(Description = "Generate IL for Complex Code")]
    public void GenerateILComplexCode()
    {
        // 先解析AST，只测量IL生成时间
        var ast = _interpreter.Build(_complexCode);
        
        // 测量IL生成时间
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "complex_il_test", _interpreter);
    }
    
    /// <summary>
    /// 测试编译后代码的执行效率（简单代码）
    /// </summary>
    [Benchmark(Description = "Execute Compiled Simple Code")]
    public void ExecuteCompiledSimpleCode()
    {
        // 预编译代码
        var ast = _interpreter.Build(_simpleCode);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "simple_execute_test", _interpreter);
        
        // 只测量执行时间
        compiledAction();
    }
    
    /// <summary>
    /// 测试编译后代码的执行效率（复杂代码）
    /// </summary>
    [Benchmark(Description = "Execute Compiled Complex Code")]
    public void ExecuteCompiledComplexCode()
    {
        // 预编译代码
        var ast = _interpreter.Build(_complexCode);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "complex_execute_test", _interpreter);
        
        // 只测量执行时间
        compiledAction();
    }
    
    /// <summary>
    /// 测试多次编译同一代码的性能
    /// </summary>
    [Benchmark(Description = "Multiple Compilations")]
    public void MultipleCompilations()
    {
        for (int i = 0; i < 10; i++)
        {
            var ast = _interpreter.Build(_simpleCode);
            var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "multiple_test", _interpreter);
            compiledAction();
        }
    }
    
    /// <summary>
    /// 测试IL验证性能
    /// </summary>
    [Benchmark(Description = "IL Verification Overhead")]
    public void ILVerificationOverhead()
    {
        // 启用IL验证
        Old8Lang.Compiler.Compiler.ILVerificationEnabled = true;
        
        var ast = _interpreter.Build(_mediumCode);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "il_verification_test", _interpreter);
        compiledAction();
    }
    
    /// <summary>
    /// 测试无IL验证的性能
    /// </summary>
    [Benchmark(Description = "No IL Verification")]
    public void NoILVerification()
    {
        // 禁用IL验证
        Old8Lang.Compiler.Compiler.ILVerificationEnabled = false;
        
        var ast = _interpreter.Build(_mediumCode);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "no_il_verification_test", _interpreter);
        compiledAction();
        
        // 恢复IL验证设置
        Old8Lang.Compiler.Compiler.ILVerificationEnabled = true;
    }
}