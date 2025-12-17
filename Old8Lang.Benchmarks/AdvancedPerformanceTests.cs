using BenchmarkDotNet.Attributes;
using Old8Lang.AST.Statement;
using Old8Lang.Interpreter;
using Old8Lang.LangParser;
using System.Diagnostics;
using System.Text;

namespace Old8Lang.Benchmarks;

/// <summary>
/// 高级性能基准测试
/// 测试大型数据集合处理、复杂表达式解析和内存使用情况
/// </summary>
[MemoryDiagnoser]
[SimpleJob(BenchmarkDotNet.Jobs.RuntimeMoniker.Net80)]
public class AdvancedPerformanceTests
{
    // 测试用代码片段
    private string LargeDataSetCode = "";
    private string ComplexExpressionCode = "";
    private string MemoryIntensiveCode = "";
    private string StringProcessingCode = "";
    private string ListProcessingCode = "";

    /// <summary>
    /// 初始化测试数据
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        // 大型数据集合处理测试
        LargeDataSetCode = GenerateLargeDataSetCode();

        // 复杂表达式解析测试
        ComplexExpressionCode = GenerateComplexExpressionCode();

        // 内存密集型操作测试
        MemoryIntensiveCode = GenerateMemoryIntensiveCode();

        // 字符串处理性能测试
        StringProcessingCode = GenerateStringProcessingCode();

        // 列表处理性能测试
        ListProcessingCode = GenerateListProcessingCode();
    }

    /// <summary>
    /// 生成大型数据集合处理代码
    /// </summary>
    private string GenerateLargeDataSetCode()
    {
        var code = new StringBuilder();
        code.AppendLine("func process_large_data() {");
        code.AppendLine("    // 创建大型数据集");
        code.AppendLine("    large_list <- {}");

        // 生成10000个元素的数据集
        for (int i = 0; i < 10000; i++)
        {
            code.AppendLine($"    large_list.Push({i})");
        }

        code.AppendLine("    ");
        code.AppendLine("    // 数据处理");
        code.AppendLine("    sum <- 0");
        code.AppendLine("    count <- 0");
        code.AppendLine("    for item in large_list {");
        code.AppendLine("        sum <- sum + item");
        code.AppendLine("        count <- count + 1");
        code.AppendLine("        if item > 5000 {");
        code.AppendLine("            break");
        code.AppendLine("        }");
        code.AppendLine("    }");
        code.AppendLine("    return sum");
        code.AppendLine("}");
        code.AppendLine("result <- process_large_data()");

        return code.ToString();
    }

    /// <summary>
    /// 生成复杂表达式解析代码
    /// </summary>
    private string GenerateComplexExpressionCode()
    {
        var code = new StringBuilder();
        code.AppendLine("func complex_expressions() {");
        code.AppendLine("    // 复杂数学表达式");
        code.AppendLine("    a <- 100");
        code.AppendLine("    b <- 200");
        code.AppendLine("    c <- 50");
        code.AppendLine("    ");
        code.AppendLine("    // 嵌套表达式");
        code.AppendLine("    result1 <- (a + b) * (c - (a / b)) + (b % c)");
        code.AppendLine("    ");
        code.AppendLine("    // 复杂条件表达式");
        code.AppendLine("    result2 <- ((a > b) && (b > c)) || ((a + b + c) > 300)");
        code.AppendLine("    ");
        code.AppendLine("    // 多层嵌套函数调用");
        code.AppendLine("    func helper(x) { return x * x + x }");
        code.AppendLine("    func helper2(y) { return helper(y) + helper(y + 1) }");
        code.AppendLine("    ");
        code.AppendLine("    result3 <- helper2(a) + helper2(b) - helper2(c)");
        code.AppendLine("    ");
        code.AppendLine("    // 复杂字符串表达式");
        code.AppendLine("    str1 <- \"Hello\"");
        code.AppendLine("    str2 <- \"World\"");
        code.AppendLine("    result4 <- str1 + \" \" + str2 + \" \" + a.ToStr()");
        code.AppendLine("    ");
        code.AppendLine("    return result1 + result2 + result3");
        code.AppendLine("}");
        code.AppendLine("final_result <- complex_expressions()");

        return code.ToString();
    }

    /// <summary>
    /// 生成内存密集型操作代码
    /// </summary>
    private string GenerateMemoryIntensiveCode()
    {
        var code = new StringBuilder();
        code.AppendLine("func memory_intensive() {");
        code.AppendLine("    // 创建多个大型列表");
        code.AppendLine("    list1 <- {}");
        code.AppendLine("    list2 <- {}");
        code.AppendLine("    list3 <- {}");
        code.AppendLine("    ");

        // 为每个列表添加大量数据
        for (int i = 0; i < 5000; i++)
        {
            code.AppendLine($"    list1.Push(\"string_{i}\")");
            code.AppendLine($"    list2.Push({i} * 2)");
            code.AppendLine($"    list3.Push({i}.ToFloat() / 3.14)");
        }

        code.AppendLine("    ");
        code.AppendLine("    // 创建字典");
        code.AppendLine("    dict1 <- {}");
        code.AppendLine("    ");

        for (int i = 0; i < 1000; i++)
        {
            code.AppendLine($"    dict1[\"key_{i}\"] <- \"value_{i}\"");
        }

        code.AppendLine("    ");
        code.AppendLine("    // 复杂数据操作");
        code.AppendLine("    total <- 0");
        code.AppendLine("    for item in list2 {");
        code.AppendLine("        total <- total + item");
        code.AppendLine("    }");
        code.AppendLine("    ");
        code.AppendLine("    return total");
        code.AppendLine("}");
        code.AppendLine("memory_result <- memory_intensive()");

        return code.ToString();
    }

    /// <summary>
    /// 生成字符串处理性能测试代码
    /// </summary>
    private string GenerateStringProcessingCode()
    {
        var code = new StringBuilder();
        code.AppendLine("func string_processing() {");
        code.AppendLine("    // 大量字符串操作");
        code.AppendLine("    base_string <- \"The quick brown fox jumps over the lazy dog\"");
        code.AppendLine("    result_list <- {}");
        code.AppendLine("    ");

        // 生成大量字符串操作
        for (int i = 0; i < 1000; i++)
        {
            code.AppendLine($"    result_list.Push(base_string + \"_iteration_{i}\")");
        }

        code.AppendLine("    ");
        code.AppendLine("    // 字符串拼接");
        code.AppendLine("    final_string <- \"\"");
        code.AppendLine("    for str in result_list {");
        code.AppendLine("        final_string <- final_string + str + \"|\"");
        code.AppendLine("    }");
        code.AppendLine("    ");
        code.AppendLine("    return final_string.Length()");
        code.AppendLine("}");
        code.AppendLine("string_result <- string_processing()");

        return code.ToString();
    }

    /// <summary>
    /// 生成列表处理性能测试代码
    /// </summary>
    private string GenerateListProcessingCode()
    {
        var code = new StringBuilder();
        code.AppendLine("func list_processing() {");
        code.AppendLine("    // 创建大型列表");
        code.AppendLine("    numbers <- {}");
        code.AppendLine("    ");

        // 生成数值列表
        for (int i = 0; i < 10000; i++)
        {
            code.AppendLine($"    numbers.Push({i})");
        }

        code.AppendLine("    ");
        code.AppendLine("    // 复杂列表操作");
        code.AppendLine("    even_numbers <- {}");
        code.AppendLine("    odd_numbers <- {}");
        code.AppendLine("    ");
        code.AppendLine("    for num in numbers {");
        code.AppendLine("        if num % 2 == 0 {");
        code.AppendLine("            even_numbers.Push(num * 2)");
        code.AppendLine("        } else {");
        code.AppendLine("            odd_numbers.Push(num * 3)");
        code.AppendLine("        }");
        code.AppendLine("    }");
        code.AppendLine("    ");
        code.AppendLine("    // 列表过滤和转换");
        code.AppendLine("    filtered <- {}");
        code.AppendLine("    for num in even_numbers {");
        code.AppendLine("        if num > 10000 {");
        code.AppendLine("            filtered.Push(num / 2)");
        code.AppendLine("        }");
        code.AppendLine("    }");
        code.AppendLine("    ");
        code.AppendLine("    return Len(filtered)");
        code.AppendLine("}");
        code.AppendLine("list_result <- list_processing()");

        return code.ToString();
    }

    #region 大型数据集合处理性能测试

    /// <summary>
    /// 测试大型数据集合处理性能
    /// </summary>
    [Benchmark(Description = "Large Dataset Processing")]
    public void LargeDatasetProcessing()
    {
        var interpreter = new LangInterpreter();
        var stopwatch = Stopwatch.StartNew();

        BlockStatement ast = interpreter.Build(LargeDataSetCode);
        ast.Run(interpreter.Manager);

        stopwatch.Stop();
        Console.WriteLine($"Large dataset processing time: {stopwatch.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// 测试解析大型代码的性能
    /// </summary>
    [Benchmark(Description = "Parse Large Code")]
    public void ParseLargeCode()
    {
        var interpreter = new LangInterpreter();
        var stopwatch = Stopwatch.StartNew();

        var tokens = LangInterpreter.Tokenize(LargeDataSetCode);
        var parser = new LangParser.LangParser(tokens, LargeDataSetCode);
        var ast = parser.ParseProgram();

        stopwatch.Stop();
        Console.WriteLine($"Parse large code time: {stopwatch.ElapsedMilliseconds}ms");
    }

    #endregion

    #region 复杂表达式解析性能测试

    /// <summary>
    /// 测试复杂表达式解析性能
    /// </summary>
    [Benchmark(Description = "Complex Expression Parsing")]
    public void ComplexExpressionParsing()
    {
        var interpreter = new LangInterpreter();
        var stopwatch = Stopwatch.StartNew();

        BlockStatement ast = interpreter.Build(ComplexExpressionCode);
        ast.Run(interpreter.Manager);

        stopwatch.Stop();
        Console.WriteLine($"Complex expression parsing time: {stopwatch.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// 测试数学表达式计算性能
    /// </summary>
    [Benchmark(Description = "Math Expression Calculation")]
    public void MathExpressionCalculation()
    {
        var code = @"
func math_calc() {
    result1 <- (100 + 200) * (50 - (100 / 200)) + (200 % 50)
    result2 <- ((100 > 200) && (200 > 50)) || ((100 + 200 + 50) > 300)
    result3 <- (100 * 200) / (50 + 25) - (100 - 200 + 50) * 2
    return result1 + result2 + result3
}
math_result <- math_calc()
";

        var interpreter = new LangInterpreter();
        var stopwatch = Stopwatch.StartNew();

        BlockStatement ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        stopwatch.Stop();
        Console.WriteLine($"Math expression calculation time: {stopwatch.ElapsedTicks} ticks");
    }

    #endregion

    #region 内存使用情况测试

    /// <summary>
    /// 测试内存密集型操作
    /// </summary>
    [Benchmark(Description = "Memory Intensive Operations")]
    public void MemoryIntensiveOperations()
    {
        var interpreter = new LangInterpreter();

        // 记录初始内存使用
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        long initialMemory = GC.GetTotalMemory(false);

        var stopwatch = Stopwatch.StartNew();
        BlockStatement ast = interpreter.Build(MemoryIntensiveCode);
        ast.Run(interpreter.Manager);
        stopwatch.Stop();

        // 记录执行后内存使用
        long finalMemory = GC.GetTotalMemory(false);
        long memoryUsed = finalMemory - initialMemory;

        Console.WriteLine($"Memory intensive operations time: {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Memory used: {memoryUsed / 1024.0:F2} KB");
    }

    /// <summary>
    /// 测试字符串处理内存使用
    /// </summary>
    [Benchmark(Description = "String Processing Memory")]
    public void StringProcessingMemory()
    {
        var interpreter = new LangInterpreter();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        long initialMemory = GC.GetTotalMemory(false);

        BlockStatement ast = interpreter.Build(StringProcessingCode);
        ast.Run(interpreter.Manager);

        long finalMemory = GC.GetTotalMemory(false);
        long memoryUsed = finalMemory - initialMemory;

        Console.WriteLine($"String processing memory used: {memoryUsed / 1024.0:F2} KB");
    }

    /// <summary>
    /// 测试列表处理内存使用
    /// </summary>
    [Benchmark(Description = "List Processing Memory")]
    public void ListProcessingMemory()
    {
        var interpreter = new LangInterpreter();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        long initialMemory = GC.GetTotalMemory(false);

        BlockStatement ast = interpreter.Build(ListProcessingCode);
        ast.Run(interpreter.Manager);

        long finalMemory = GC.GetTotalMemory(false);
        long memoryUsed = finalMemory - initialMemory;

        Console.WriteLine($"List processing memory used: {memoryUsed / 1024.0:F2} KB");
    }

    #endregion

    #region 综合性能测试

    /// <summary>
    /// 测试解释器与编译器性能对比
    /// </summary>
    [Benchmark(Description = "Interpreter vs Compiler Performance")]
    public void InterpreterVsCompilerPerformance()
    {
        var code = @"
func performance_test() {
    sum <- 0
    for i <- 0, i < 1000, i <- i + 1 {
        sum <- sum + (i * i + i)
    }
    return sum
}
perf_result <- performance_test()
";

        // 解释器执行
        var interpreter = new LangInterpreter();
        var interpreterStopwatch = Stopwatch.StartNew();
        BlockStatement ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);
        interpreterStopwatch.Stop();

        // 编译器执行
        var compilerStopwatch = Stopwatch.StartNew();
        Action compiledAction = Compiler.Compiler.Compile(ast, "perf_test", interpreter);
        compiledAction();
        compilerStopwatch.Stop();

        Console.WriteLine($"Interpreter time: {interpreterStopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Compiler time: {compilerStopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Speedup: {(double)interpreterStopwatch.ElapsedMilliseconds / compilerStopwatch.ElapsedMilliseconds:F2}x");
    }

    /// <summary>
    /// 测试词法分析器性能
    /// </summary>
    [Benchmark(Description = "Tokenizer Performance")]
    public void TokenizerPerformance()
    {
        // 生成包含各种语言特性的大型代码
        var largeCode = GenerateTokenizerTestCode();

        var stopwatch = Stopwatch.StartNew();
        var tokens = LangInterpreter.Tokenize(largeCode);
        stopwatch.Stop();

        Console.WriteLine($"Tokenization time: {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Tokens generated: {tokens.Count}");
        Console.WriteLine($"Tokens per ms: {tokens.Count / Math.Max(1, stopwatch.ElapsedMilliseconds)}");
    }

    /// <summary>
    /// 生成词法分析器测试代码
    /// </summary>
    private string GenerateTokenizerTestCode()
    {
        var code = new StringBuilder();

        for (int i = 0; i < 1000; i++)
        {
            code.AppendLine($"var_{i} <- {i} * {i + 1} + {i + 2}");
            code.AppendLine($"if var_{i} > 1000 {{");
            code.AppendLine($"    result_{i} <- \"large_value_{i}\"");
            code.AppendLine("} else {");
            code.AppendLine($"    result_{i} <- \"small_value_{i}\"");
            code.AppendLine("}");
            code.AppendLine();
        }

        return code.ToString();
    }

    #endregion
}