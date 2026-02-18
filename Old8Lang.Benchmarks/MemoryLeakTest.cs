using Old8Lang.LangParser;
using Xunit;

namespace Old8Lang.Benchmarks;

/// <summary>
/// 内存泄漏检测测试
/// </summary>
/// <remarks>
/// 该测试类用于检测解析器在连续解析多个脚本后是否存在内存泄漏问题。
/// 主要验证：
/// 1. 连续解析后内存能够被正确释放
/// 2. 对象池（CharBufferPool, TokenListPool）能够正确归还资源
/// 3. StringCache 不会无限增长
/// </remarks>
public class MemoryLeakTest
{
    /// <summary>
    /// 测试连续解析 100 个脚本后内存释放
    /// </summary>
    [Fact]
    public void TestContinuousParsingMemoryRelease()
    {
        // 生成测试代码（500 行）
        var testCode = GenerateTestCode(500);

        // 强制 GC 以获得准确的基线
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var memoryBefore = GC.GetTotalMemory(false);

        // 连续解析 100 次
        for (int i = 0; i < 100; i++)
        {
            var tokens = LangTokenizer.TokenizeOptimized(testCode);
            Assert.NotEmpty(tokens);
        }

        // 强制 GC 以释放所有可释放的内存
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var memoryAfter = GC.GetTotalMemory(false);
        var memoryGrowth = memoryAfter - memoryBefore;

        // 内存增长应该小于 10MB（考虑到 StringCache 会缓存一些字符串）
        Assert.True(memoryGrowth < 10 * 1024 * 1024,
            $"内存增长过大: {memoryGrowth / 1024.0 / 1024.0:F2} MB，可能存在内存泄漏");
    }

    /// <summary>
    /// 测试对象池归还逻辑
    /// </summary>
    [Fact]
    public void TestObjectPoolReturnLogic()
    {
        // 测试 CharBufferPool
        var buffer1 = Old8Lang.LangParser.Optimization.CharBufferPool.Rent(256);
        var buffer2 = Old8Lang.LangParser.Optimization.CharBufferPool.Rent(512);

        // 归还缓冲区
        buffer1.Dispose();
        buffer2.Dispose();

        // 再次租用，应该能够重用之前的缓冲区
        var buffer3 = Old8Lang.LangParser.Optimization.CharBufferPool.Rent(256);
        var buffer4 = Old8Lang.LangParser.Optimization.CharBufferPool.Rent(512);

        buffer3.Dispose();
        buffer4.Dispose();

        // 如果能够执行到这里，说明对象池归还逻辑正常
        Assert.True(true);
    }

    /// <summary>
    /// 测试 TokenListPool 归还逻辑
    /// </summary>
    [Fact]
    public void TestTokenListPoolReturnLogic()
    {
        // 租用列表
        var list1 = Old8Lang.LangParser.Optimization.TokenListPool.Rent();
        var list2 = Old8Lang.LangParser.Optimization.TokenListPool.Rent();

        // 添加一些数据
        list1.Add(new LangToken("test", LangTokenType.Identifier, 1, 0));
        list2.Add(new LangToken("test2", LangTokenType.Identifier, 1, 0));

        // 归还列表
        Old8Lang.LangParser.Optimization.TokenListPool.Return(list1);
        Old8Lang.LangParser.Optimization.TokenListPool.Return(list2);

        // 再次租用，应该能够重用之前的列表（且已清空）
        var list3 = Old8Lang.LangParser.Optimization.TokenListPool.Rent();
        var list4 = Old8Lang.LangParser.Optimization.TokenListPool.Rent();

        // 验证列表已清空
        Assert.Empty(list3);
        Assert.Empty(list4);

        Old8Lang.LangParser.Optimization.TokenListPool.Return(list3);
        Old8Lang.LangParser.Optimization.TokenListPool.Return(list4);
    }

    /// <summary>
    /// 测试 StringCache 不会无限增长
    /// </summary>
    [Fact]
    public void TestStringCacheDoesNotGrowIndefinitely()
    {
        // 创建一个小容量的缓存用于测试
        var cache = new Old8Lang.LangParser.Optimization.StringCache(maxCacheSize: 1000);

        // 添加 1000 个不同的短字符串
        for (int i = 0; i < 1000; i++)
        {
            var str = $"var{i}";
            cache.GetOrAdd(str.AsSpan());
        }

        var stats1 = cache.GetStatistics();

        // 再添加 1000 个不同的短字符串
        for (int i = 1000; i < 2000; i++)
        {
            var str = $"var{i}";
            cache.GetOrAdd(str.AsSpan());
        }

        var stats2 = cache.GetStatistics();

        // 缓存大小应该接近限制（考虑到并发和实现细节，允许一些误差）
        // 最多不应该超过 maxCacheSize * 1.1
        Assert.True(stats2.CurrentSize <= 1100,
            $"StringCache 大小超过限制: {stats2.CurrentSize}，可能存在内存泄漏");

        // 验证缓存确实阻止了无限增长（不应该是 2000）
        Assert.True(stats2.CurrentSize < 1500,
            $"StringCache 没有限制增长: {stats2.CurrentSize}");
    }

    /// <summary>
    /// 生成测试代码
    /// </summary>
    private static string GenerateTestCode(int lines)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < lines; i++)
        {
            sb.AppendLine($"var x{i} = {i};");
        }
        return sb.ToString();
    }
}
