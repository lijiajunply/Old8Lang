using System.Diagnostics;
using Old8Lang.LangParser;

namespace Old8Lang.Benchmarks.Tools;

/// <summary>
/// 性能验证程序 - 测试解析器优化效果
/// </summary>
public class PerformanceValidator
{
    public static void Main(string[] args)
    {
        Console.WriteLine("=== Old8Lang 解析器性能验证 ===\n");

        // 测试 1: 小型脚本（500 行）
        TestSmallScript();

        // 测试 2: 中型项目（3000 行）
        TestMediumProject();

        // 测试 3: 大型脚本（5000 行）
        TestLargeScript();

        // 测试 4: StringCache 命中率
        TestStringCacheHitRate();

        Console.WriteLine("\n=== 性能验证完成 ===");
    }

    private static void TestSmallScript()
    {
        Console.WriteLine("【测试 1】小型脚本（500 行）");
        var testFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "small_script_500.old8");

        if (!File.Exists(testFile))
        {
            Console.WriteLine($"  ⚠️  测试文件不存在: {testFile}");
            return;
        }

        var code = File.ReadAllText(testFile);
        var sw = Stopwatch.StartNew();
        var gcBefore = GC.CollectionCount(0);

        try
        {
            var tokens = LangTokenizer.Tokenize(code);
            sw.Stop();
            var gcAfter = GC.CollectionCount(0);

            Console.WriteLine($"  ✅ 解析时间: {sw.ElapsedMilliseconds} ms");
            Console.WriteLine($"  📊 Token 数量: {tokens.Count}");
            Console.WriteLine($"  🗑️  GC Gen0 收集: {gcAfter - gcBefore} 次");
            Console.WriteLine($"  🎯 目标: < 100ms");
            Console.WriteLine($"  {(sw.ElapsedMilliseconds < 100 ? "✅ 通过" : "❌ 未达标")}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ 错误: {ex.Message}");
        }

        Console.WriteLine();
    }

    private static void TestMediumProject()
    {
        Console.WriteLine("【测试 2】中型项目（3000 行）");
        var testFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "medium_project_3000.old8");

        if (!File.Exists(testFile))
        {
            Console.WriteLine($"  ⚠️  测试文件不存在: {testFile}");
            return;
        }

        var code = File.ReadAllText(testFile);
        var sw = Stopwatch.StartNew();
        var gcBefore = GC.CollectionCount(0);

        try
        {
            var tokens = LangTokenizer.Tokenize(code);
            sw.Stop();
            var gcAfter = GC.CollectionCount(0);

            Console.WriteLine($"  ✅ 解析时间: {sw.ElapsedMilliseconds} ms");
            Console.WriteLine($"  📊 Token 数量: {tokens.Count}");
            Console.WriteLine($"  🗑️  GC Gen0 收集: {gcAfter - gcBefore} 次");
            Console.WriteLine($"  🎯 目标: < 500ms");
            Console.WriteLine($"  {(sw.ElapsedMilliseconds < 500 ? "✅ 通过" : "❌ 未达标")}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ 错误: {ex.Message}");
        }

        Console.WriteLine();
    }

    private static void TestLargeScript()
    {
        Console.WriteLine("【测试 3】大型脚本（5000 行）");
        var testFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "large_script_5000.old8");

        if (!File.Exists(testFile))
        {
            Console.WriteLine($"  ⚠️  测试文件不存在: {testFile}");
            return;
        }

        var code = File.ReadAllText(testFile);
        var sw = Stopwatch.StartNew();
        var memoryBefore = GC.GetTotalMemory(false);
        var gcBefore = GC.CollectionCount(0);

        try
        {
            var tokens = LangTokenizer.Tokenize(code);
            sw.Stop();
            var memoryAfter = GC.GetTotalMemory(false);
            var gcAfter = GC.CollectionCount(0);
            var memoryUsed = (memoryAfter - memoryBefore) / 1024.0 / 1024.0;

            Console.WriteLine($"  ✅ 解析时间: {sw.ElapsedMilliseconds} ms");
            Console.WriteLine($"  📊 Token 数量: {tokens.Count}");
            Console.WriteLine($"  💾 内存使用: {memoryUsed:F2} MB");
            Console.WriteLine($"  🗑️  GC Gen0 收集: {gcAfter - gcBefore} 次");
            Console.WriteLine($"  🎯 目标: < 800ms, < 50MB");
            Console.WriteLine($"  {(sw.ElapsedMilliseconds < 800 && memoryUsed < 50 ? "✅ 通过" : "❌ 未达标")}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ 错误: {ex.Message}");
        }

        Console.WriteLine();
    }

    private static void TestStringCacheHitRate()
    {
        Console.WriteLine("【测试 4】StringCache 命中率");

        // 创建一个包含重复标识符的测试代码
        var testCode = @"
func test1() {
    x <- 100
    y <- 200
    z <- x + y
    return z
}

func test2() {
    x <- 300
    y <- 400
    z <- x + y
    return z
}

func test3() {
    x <- 500
    y <- 600
    z <- x + y
    return z
}
";

        var cache = new Old8Lang.LangParser.Optimization.StringCache();

        try
        {
            // 使用 TokenizeOptimized 方法（如果存在）
            // 由于我们还没有实现这个方法，这里只是演示概念
            var tokens = LangTokenizer.Tokenize(testCode);

            // 手动测试缓存
            var identifiers = new[] { "x", "y", "z", "test1", "test2", "test3" };
            foreach (var id in identifiers)
            {
                for (int i = 0; i < 10; i++)
                {
                    cache.GetOrAdd(id.AsSpan());
                }
            }

            var stats = cache.GetStatistics();
            Console.WriteLine($"  📊 总请求: {stats.TotalRequests}");
            Console.WriteLine($"  ✅ 缓存命中: {stats.CacheHits}");
            Console.WriteLine($"  ❌ 缓存未命中: {stats.CacheMisses}");
            Console.WriteLine($"  📈 命中率: {stats.HitRate:P2}");
            Console.WriteLine($"  💾 缓存大小: {stats.CurrentSize}");
            Console.WriteLine($"  🎯 目标: > 50%");
            Console.WriteLine($"  {(stats.HitRate > 0.5 ? "✅ 通过" : "❌ 未达标")}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ 错误: {ex.Message}");
        }

        Console.WriteLine();
    }
}
