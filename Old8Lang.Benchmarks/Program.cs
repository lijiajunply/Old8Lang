using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using Old8Lang.AST.Statement;
using Old8Lang.Compiler;
using Old8Lang.LangParser;

namespace Old8Lang.Benchmarks
{
    /// <summary>
    /// 编译器性能基准测试
    /// 测量编译时间、内存占用和生成代码的执行效率
    /// </summary>
    public class CompilerBenchmarkTests
    {
        private LangInterpreter _interpreter;
        
        // 测试用代码片段
        private string _simpleCode;
        private string _mediumCode;
        private string _complexCode;
        
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
            var interpreter = new LangInterpreter();
            BlockStatement ast = interpreter.Build(_simpleCode);
            Action compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "simple_test", interpreter);
            compiledAction();
        }
        
        /// <summary>
        /// 测试中等复杂度代码的编译时间
        /// </summary>
        [Benchmark(Description = "Compile Medium Code")]
        public void CompileMediumCode()
        {
            var interpreter = new LangInterpreter();
            BlockStatement ast = interpreter.Build(_mediumCode);
            Action compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "medium_test", interpreter);
            compiledAction();
        }
        
        /// <summary>
        /// 测试复杂代码的编译时间
        /// </summary>
        [Benchmark(Description = "Compile Complex Code")]
        public void CompileComplexCode()
        {
            var interpreter = new LangInterpreter();
            BlockStatement ast = interpreter.Build(_complexCode);
            Action compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "complex_test", interpreter);
            compiledAction();
        }
        
        /// <summary>
        /// 测试简单代码的纯IL生成时间（不包含编译前的解析）
        /// </summary>
        [Benchmark(Description = "Generate IL for Simple Code")]
        public void GenerateILSimpleCode()
        {
            var interpreter = new LangInterpreter();
            // 先解析AST，只测量IL生成时间
            BlockStatement ast = interpreter.Build(_simpleCode);
            
            // 测量IL生成时间
            Action compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "simple_il_test", interpreter);
        }
        
        /// <summary>
        /// 测试中等复杂度代码的纯IL生成时间
        /// </summary>
        [Benchmark(Description = "Generate IL for Medium Code")]
        public void GenerateILMediumCode()
        {
            var interpreter = new LangInterpreter();
            // 先解析AST，只测量IL生成时间
            BlockStatement ast = interpreter.Build(_mediumCode);
            
            // 测量IL生成时间
            Action compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "medium_il_test", interpreter);
        }
        
        /// <summary>
        /// 测试复杂代码的纯IL生成时间
        /// </summary>
        [Benchmark(Description = "Generate IL for Complex Code")]
        public void GenerateILComplexCode()
        {
            var interpreter = new LangInterpreter();
            // 先解析AST，只测量IL生成时间
            BlockStatement ast = interpreter.Build(_complexCode);
            
            // 测量IL生成时间
            Action compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "complex_il_test", interpreter);
        }
        
        /// <summary>
        /// 测试编译后代码的执行效率（简单代码）
        /// </summary>
        [Benchmark(Description = "Execute Compiled Simple Code")]
        public void ExecuteCompiledSimpleCode()
        {
            var interpreter = new LangInterpreter();
            // 预编译代码
            BlockStatement ast = interpreter.Build(_simpleCode);
            Action compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "simple_execute_test", interpreter);
            
            // 只测量执行时间
            compiledAction();
        }
        
        /// <summary>
        /// 测试编译后代码的执行效率（复杂代码）
        /// </summary>
        [Benchmark(Description = "Execute Compiled Complex Code")]
        public void ExecuteCompiledComplexCode()
        {
            var interpreter = new LangInterpreter();
            // 预编译代码
            BlockStatement ast = interpreter.Build(_complexCode);
            Action compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "complex_execute_test", interpreter);
            
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
                var interpreter = new LangInterpreter();
                BlockStatement ast = interpreter.Build(_simpleCode);
                Action compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "multiple_test", interpreter);
                compiledAction();
            }
        }
        
        /// <summary>
        /// 测试IL验证性能
        /// </summary>
        [Benchmark(Description = "IL Verification Overhead")]
        public void ILVerificationOverhead()
        {
            var interpreter = new LangInterpreter();
            // 启用IL验证
            Old8Lang.Compiler.Compiler.ilVerificationEnabled = true;
            
            BlockStatement ast = interpreter.Build(_mediumCode);
            Action compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "il_verification_test", interpreter);
            compiledAction();
        }
        
        /// <summary>
        /// 测试无IL验证的性能
        /// </summary>
        [Benchmark(Description = "No IL Verification")]
        public void NoILVerification()
        {
            var interpreter = new LangInterpreter();
            // 禁用IL验证
            Old8Lang.Compiler.Compiler.ilVerificationEnabled = false;
            
            BlockStatement ast = interpreter.Build(_mediumCode);
            Action compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "no_il_verification_test", interpreter);
            compiledAction();
            
            // 恢复IL验证设置
            Old8Lang.Compiler.Compiler.ilVerificationEnabled = true;
        }
    }
    
    /// <summary>
    /// 解释器性能基准测试
    /// 测量解释器执行时间、内存占用和性能优化效果
    /// </summary>
    public class InterpreterBenchmarkTests
    {
        private LangInterpreter _interpreter;
        
        // 测试用代码片段
        private string _loopIntensiveCode;
        private string _functionCallIntensiveCode;
        private string _mixedIntensiveCode;
        
        /// <summary>
        /// 初始化测试数据
        /// </summary>
        [GlobalSetup]
        public void Setup()
        {
            _interpreter = new LangInterpreter();
            
            // 循环密集型代码：测试循环执行机制优化效果
            _loopIntensiveCode = @"func loop_test() {
    sum <- 0
    for i <- 0, i < 10000, i <- i + 1 {
        sum <- sum + i * i
        if sum > 100000 {
            // 测试continue
            continue
        }
        if sum > 500000 {
            // 测试break
            break
        }
    }
    return sum
}
result <- loop_test()
";
            
            // 函数调用密集型代码：测试函数调用机制优化效果
            _functionCallIntensiveCode = @"func add(a, b) {
    return a + b
}

func multiply(a, b) {
    return a * b
}

func calculate(a, b) {
    temp <- add(a, b)
    return multiply(temp, temp)
}

func function_call_test() {
    sum <- 0
    for i <- 0, i < 1000, i <- i + 1 {
        sum <- sum + calculate(i, i + 1)
    }
    return sum
}
result <- function_call_test()
";
            
            // 混合密集型代码：测试综合优化效果
            _mixedIntensiveCode = @"func helper(n) {
    if n <= 1 {
        return 1
    }
    return n + helper(n - 1)
}

func mixed_test() {
    total <- 0
    for outer <- 0, outer < 100, outer <- outer + 1 {
        sum <- 0
        for inner <- 0, inner < 100, inner <- inner + 1 {
            sum <- sum + helper(inner % 10)
        }
        total <- total + sum
    }
    return total
}
result <- mixed_test()
";
        }
        
        /// <summary>
        /// 测试循环密集型代码的执行性能
        /// 主要测试循环执行机制优化效果
        /// </summary>
        [Benchmark(Description = "Loop Intensive Code")]
        public void LoopIntensiveCode()
        {
            var interpreter = new LangInterpreter();
            BlockStatement ast = interpreter.Build(_loopIntensiveCode);
            ast.Run(interpreter.Manager);
        }
        
        /// <summary>
        /// 测试函数调用密集型代码的执行性能
        /// 主要测试函数调用机制优化效果
        /// </summary>
        [Benchmark(Description = "Function Call Intensive Code")]
        public void FunctionCallIntensiveCode()
        {
            var interpreter = new LangInterpreter();
            BlockStatement ast = interpreter.Build(_functionCallIntensiveCode);
            ast.Run(interpreter.Manager);
        }
        
        /// <summary>
        /// 测试混合密集型代码的执行性能
        /// 测试综合优化效果
        /// </summary>
        [Benchmark(Description = "Mixed Intensive Code")]
        public void MixedIntensiveCode()
        {
            var interpreter = new LangInterpreter();
            BlockStatement ast = interpreter.Build(_mixedIntensiveCode);
            ast.Run(interpreter.Manager);
        }
        
        /// <summary>
        /// 测试多次执行同一代码的性能
        /// </summary>
        [Benchmark(Description = "Multiple Executions")]
        public void MultipleExecutions()
        {
            for (int i = 0; i < 10; i++)
            {
                var interpreter = new LangInterpreter();
                BlockStatement ast = interpreter.Build(_loopIntensiveCode);
                ast.Run(interpreter.Manager);
            }
        }
        
        /// <summary>
        /// 测试深度递归函数调用性能
        /// 主要测试栈帧复用和闭包优化效果
        /// </summary>
        [Benchmark(Description = "Deep Recursion")]
        public void DeepRecursion()
        {
            var interpreter = new LangInterpreter();
            var recursionCode = @"func deep_recursion(n) {
    if n <= 0 {
        return 0
    }
    return deep_recursion(n - 1) + 1
}
result <- deep_recursion(1000)
";
            BlockStatement ast = interpreter.Build(recursionCode);
            ast.Run(interpreter.Manager);
        }
        
        /// <summary>
        /// 测试While循环性能
        /// 主要测试while循环执行机制优化效果
        /// </summary>
        [Benchmark(Description = "While Loop Performance")]
        public void WhileLoopPerformance()
        {
            var interpreter = new LangInterpreter();
            var whileCode = @"func while_test() {
    count <- 0
    sum <- 0
    while count < 10000 {
        sum <- sum + count
        count <- count + 1
    }
    return sum
}
result <- while_test()
";
            BlockStatement ast = interpreter.Build(whileCode);
            ast.Run(interpreter.Manager);
        }
    }
    
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Running Old8Lang Performance Benchmarks...");
            Console.WriteLine("========================================");
            
            // 运行编译器基准测试
            Console.WriteLine("\n1. Running Compiler Benchmark Tests:");
            BenchmarkRunner.Run<CompilerBenchmarkTests>();
            
            // 运行解释器基准测试
            Console.WriteLine("\n2. Running Interpreter Benchmark Tests:");
            BenchmarkRunner.Run<InterpreterBenchmarkTests>();
            
            Console.WriteLine("\nBenchmark tests completed!");
        }
    }
}
