using System;
using System.IO;
using System.Text;

namespace Old8Lang.Benchmarks;

/// <summary>
/// 测试数据生成器，用于生成不同规模的 Old8Lang 测试脚本
/// </summary>
public static class TestDataGenerator
{
    /// <summary>
    /// 生成指定行数的测试脚本
    /// </summary>
    /// <param name="lineCount">目标行数</param>
    /// <param name="outputPath">输出文件路径</param>
    public static void GenerateTestScript(int lineCount, string outputPath)
    {
        var sb = new StringBuilder();

        // 文件头注释（约 10 行）
        sb.AppendLine("// Old8Lang 性能测试脚本");
        sb.AppendLine($"// 目标行数: {lineCount}");
        sb.AppendLine("// 生成时间: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine();

        int currentLine = 4;

        // 生成变量声明（约 10% 的行数）
        int varCount = lineCount / 10;
        for (int i = 0; i < varCount && currentLine < lineCount; i++)
        {
            sb.AppendLine($"var variable_{i} = {i * 10};");
            currentLine++;
        }

        sb.AppendLine();
        currentLine++;

        // 生成变量声明（约 10% 的行数）
        varCount = lineCount / 10;
        for (int i = 0; i < varCount && currentLine < lineCount; i++)
        {
            sb.AppendLine($"variable_{i} <- {i * 10}");
            currentLine++;
        }

        sb.AppendLine();
        currentLine++;

        // 生成函数定义（约 30% 的行数）
        int funcCount = lineCount / 30;
        for (int i = 0; i < funcCount && currentLine < lineCount; i++)
        {
            sb.AppendLine($"/// 函数 {i} 的文档注释");
            currentLine++;
            sb.AppendLine($"func testFunction_{i}(param1, param2) {{");
            currentLine++;
            sb.AppendLine($"    result <- param1 + param2 + {i}");
            currentLine++;
            sb.AppendLine($"    if (result > 100) {{");
            currentLine++;
            sb.AppendLine($"        return result * 2");
            currentLine++;
            sb.AppendLine($"    }} else {{");
            currentLine++;
            sb.AppendLine($"        return result");
            currentLine++;
            sb.AppendLine($"    }}");
            currentLine++;
            sb.AppendLine($"}}");
            currentLine++;
            sb.AppendLine();
            currentLine++;
        }

        // 生成类定义（约 20% 的行数）
        int classCount = lineCount / 50;
        for (int i = 0; i < classCount && currentLine < lineCount; i++)
        {
            sb.AppendLine($"/// 类 {i} 的文档注释");
            currentLine++;
            sb.AppendLine($"class TestClass_{i} {{");
            currentLine++;
            sb.AppendLine($"    field1 <- {i}");
            currentLine++;
            sb.AppendLine($"    field2 <- \"{i}_value\"");
            currentLine++;
            sb.AppendLine();
            currentLine++;
            sb.AppendLine($"    func constructor(value) {{");
            currentLine++;
            sb.AppendLine($"        this.field1 <- value");
            currentLine++;
            sb.AppendLine($"    }}");
            currentLine++;
            sb.AppendLine();
            currentLine++;
            sb.AppendLine($"    func method_{i}() {{");
            currentLine++;
            sb.AppendLine($"        return this.field1 + this.field2");
            currentLine++;
            sb.AppendLine($"    }}");
            currentLine++;
            sb.AppendLine($"}}");
            currentLine++;
            sb.AppendLine();
            currentLine++;
        }

        // 生成表达式和语句（填充剩余行数）
        while (currentLine < lineCount)
        {
            int statementType = currentLine % 5;
            switch (statementType)
            {
                case 0:
                    sb.AppendLine($"expr_{currentLine} <- {currentLine} + {currentLine * 2} - {currentLine / 2}");
                    break;
                case 1:
                    sb.AppendLine($"if ({currentLine} % 2 == 0) {{ temp <- {currentLine} }}");
                    break;
                case 2:
                    sb.AppendLine($"for (i <- 0, i < {currentLine % 10}, i <- i + 1) {{ loop_var <- i }}");
                    break;
                case 3:
                    sb.AppendLine($"array_{currentLine} <- [{currentLine}, {currentLine + 1}, {currentLine + 2}]");
                    break;
                case 4:
                    sb.AppendLine($"dict_{currentLine} <- {{ \"key\": {currentLine}, \"value\": \"{currentLine}\" }}");
                    break;
            }

            currentLine++;
        }

        for (int i = 0; i < funcCount && currentLine < lineCount; i++)
        {
            sb.AppendLine($"/// 函数 {i} 的文档注释");
            currentLine++;
            sb.AppendLine($"function testFunction_{i}(param1, param2) {{");
            currentLine++;
            sb.AppendLine($"    var result = param1 + param2 + {i};");
            currentLine++;
            sb.AppendLine($"    if (result > 100) {{");
            currentLine++;
            sb.AppendLine($"        return result * 2;");
            currentLine++;
            sb.AppendLine($"    }} else {{");
            currentLine++;
            sb.AppendLine($"        return result;");
            currentLine++;
            sb.AppendLine($"    }}");
            currentLine++;
            sb.AppendLine($"}}");
            currentLine++;
            sb.AppendLine();
            currentLine++;
        }

        // 生成类定义（约 20% 的行数）
        classCount = lineCount / 50;
        for (int i = 0; i < classCount && currentLine < lineCount; i++)
        {
            sb.AppendLine($"/// 类 {i} 的文档注释");
            currentLine++;
            sb.AppendLine($"class TestClass_{i} {{");
            currentLine++;
            sb.AppendLine($"    var field1 = {i};");
            currentLine++;
            sb.AppendLine($"    var field2 = \"{i}_value\";");
            currentLine++;
            sb.AppendLine();
            currentLine++;
            sb.AppendLine($"    function constructor(value) {{");
            currentLine++;
            sb.AppendLine($"        this.field1 = value;");
            currentLine++;
            sb.AppendLine($"    }}");
            currentLine++;
            sb.AppendLine();
            currentLine++;
            sb.AppendLine($"    function method_{i}() {{");
            currentLine++;
            sb.AppendLine($"        return this.field1 + this.field2;");
            currentLine++;
            sb.AppendLine($"    }}");
            currentLine++;
            sb.AppendLine($"}}");
            currentLine++;
            sb.AppendLine();
            currentLine++;
        }

        // 生成表达式和语句（填充剩余行数）
        while (currentLine < lineCount)
        {
            int statementType = currentLine % 5;
            switch (statementType)
            {
                case 0:
                    sb.AppendLine($"var expr_{currentLine} = {currentLine} + {currentLine * 2} - {currentLine / 2};");
                    break;
                case 1:
                    sb.AppendLine($"if ({currentLine} % 2 == 0) {{ var temp = {currentLine}; }}");
                    break;
                case 2:
                    sb.AppendLine($"for (var i = 0; i < {currentLine % 10}; i++) {{ var loop_var = i; }}");
                    break;
                case 3:
                    sb.AppendLine($"var array_{currentLine} = [{currentLine}, {currentLine + 1}, {currentLine + 2}];");
                    break;
                case 4:
                    sb.AppendLine(
                        $"var dict_{currentLine} = {{ \"key\": {currentLine}, \"value\": \"{currentLine}\" }};");
                    break;
            }

            currentLine++;
        }

        // 确保目录存在
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // 写入文件
        File.WriteAllText(outputPath, sb.ToString());
        Console.WriteLine($"生成测试脚本: {outputPath} ({currentLine} 行)");
    }

    /// <summary>
    /// 生成所有标准测试脚本
    /// </summary>
    public static void GenerateAllTestScripts()
    {
        var testDataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData");

        GenerateTestScript(500, Path.Combine(testDataDir, "small_script_500.old8"));
        GenerateTestScript(3000, Path.Combine(testDataDir, "medium_project_3000.old8"));
        GenerateTestScript(5000, Path.Combine(testDataDir, "large_script_5000.old8"));
    }

    /// <summary>
    /// 生成 VM 大文件性能测试脚本（固定 seed 保证可复现）
    /// </summary>
    /// <param name="lineCount">目标行数</param>
    /// <param name="seed">随机种子</param>
    /// <param name="outputPath">输出路径</param>
    public static void GenerateVmLargeScript(int lineCount, int seed, string outputPath)
    {
        var random = new Random(seed);
        var sb = new StringBuilder();

        sb.AppendLine("// VM large benchmark script");
        sb.AppendLine($"// lineCount={lineCount}, seed={seed}");
        sb.AppendLine();

        sb.AppendLine("func transform(x:int, offset:int) -> int {");
        sb.AppendLine("    return (x * 3 + offset) % 100000");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("func fold(arr) -> int {");
        sb.AppendLine("    s <- 0");
        sb.AppendLine("    for v in arr {");
        sb.AppendLine("        s <- s + v");
        sb.AppendLine("    }");
        sb.AppendLine("    return s");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("data <- {}");
        sb.AppendLine("result <- 0");
        sb.AppendLine();

        var currentLines = sb.ToString().Split('\n').Length;
        var targetBodyLines = Math.Max(0, lineCount - currentLines - 20);

        for (var i = 0; i < targetBodyLines; i++)
        {
            var value = random.Next(1, 1_000_000);
            var op = i % 6;
            switch (op)
            {
                case 0:
                    sb.AppendLine($"data.Add(transform({value}, {i % 97}))");
                    break;
                case 1:
                    sb.AppendLine($"result <- result + transform({value}, {i % 113})");
                    break;
                case 2:
                    sb.AppendLine($"if {value} % 2 == 0 {{ result <- result + {i % 17} }}");
                    break;
                case 3:
                    sb.AppendLine($"if len(data) > 0 {{ result <- result + data[len(data) - 1] % {(i % 23) + 2} }}");
                    break;
                case 4:
                    sb.AppendLine($"tmp_{i} <- transform({value}, {i % 131})");
                    break;
                default:
                    sb.AppendLine($"result <- result + ({value} % {(i % 29) + 3})");
                    break;
            }
        }

        sb.AppendLine();
        sb.AppendLine("result <- result + fold(data)");
        sb.AppendLine("if len(data) > 1000 {");
        sb.AppendLine("    result <- result + len(data)");
        sb.AppendLine("}");

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(outputPath, sb.ToString());
    }
}
