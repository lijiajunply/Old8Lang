using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Old8Lang.Utilities;
using System.Reflection;

namespace Old8Lang.Benchmarks;

/// <summary>
/// 反射性能基准测试 - 对比优化前后的性能差异
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class ReflectionPerformanceBenchmark
{
    private MethodInfo? _sqrtMethod;
    private MethodInfo? _powMethod;
    private object?[] _sqrtArgs = null!;
    private object?[] _powArgs = null!;

    [GlobalSetup]
    public void Setup()
    {
        // 获取 Math.Sqrt 和 Math.Pow 方法
        _sqrtMethod = typeof(Math).GetMethod("Sqrt", [typeof(double)])!;
        _powMethod = typeof(Math).GetMethod("Pow", [typeof(double), typeof(double)])!;

        _sqrtArgs = [16.0];
        _powArgs = [2.0, 3.0];
    }

    /// <summary>
    /// 基准测试1: 原始反射调用 (优化前)
    /// </summary>
    [Benchmark(Baseline = true, Description = "原始 MethodInfo.Invoke")]
    public double Original_Reflection_Invoke()
    {
        double total = 0;
        for (int i = 0; i < 1000; i++)
        {
            var sqrtResult = (double)_sqrtMethod!.Invoke(null, _sqrtArgs)!;
            var powResult = (double)_powMethod!.Invoke(null, _powArgs)!;
            total += sqrtResult + powResult;
        }
        return total;
    }

    /// <summary>
    /// 基准测试2: 委托缓存调用 (优化后)
    /// </summary>
    [Benchmark(Description = "优化后 委托缓存")]
    public double Optimized_DelegateCache()
    {
        double total = 0;
        for (int i = 0; i < 1000; i++)
        {
            var sqrtResult = (double)MethodInvokerCache.Invoke(_sqrtMethod!, null, _sqrtArgs)!;
            var powResult = (double)MethodInvokerCache.Invoke(_powMethod!, null, _powArgs)!;
            total += sqrtResult + powResult;
        }
        return total;
    }

    /// <summary>
    /// 基准测试3: 直接方法调用 (理论最优性能)
    /// </summary>
    [Benchmark(Description = "直接方法调用 (理论最优)")]
    public double Direct_MethodCall()
    {
        double total = 0;
        for (int i = 0; i < 1000; i++)
        {
            var sqrtResult = Math.Sqrt(16.0);
            var powResult = Math.Pow(2.0, 3.0);
            total += sqrtResult + powResult;
        }
        return total;
    }

    /// <summary>
    /// 基准测试4: 成员查询性能 - 原始反射 (优化前)
    /// </summary>
    [Benchmark(Description = "成员查询 - 原始反射")]
    public int Original_MemberLookup()
    {
        var type = typeof(TestClass);
        int count = 0;

        for (int i = 0; i < 100; i++)
        {
            var prop = type.GetProperty("Value");
            var field = type.GetField("Count");
            var method = type.GetMethod("Add");
            if (prop != null) count++;
            if (field != null) count++;
            if (method != null) count++;
        }

        return count;
    }

    /// <summary>
    /// 基准测试5: 成员查询性能 - 使用缓存 (优化后)
    /// </summary>
    [Benchmark(Description = "成员查询 - 使用缓存")]
    public int Optimized_MemberLookup()
    {
        var type = typeof(TestClass);
        var cache = new Dictionary<string, MemberInfo?>();
        int count = 0;

        for (int i = 0; i < 100; i++)
        {
            if (!cache.TryGetValue("Value", out var prop))
            {
                prop = type.GetProperty("Value");
                cache["Value"] = prop;
            }

            if (!cache.TryGetValue("Count", out var field))
            {
                field = type.GetField("Count");
                cache["Count"] = field;
            }

            if (!cache.TryGetValue("Add", out var method))
            {
                method = type.GetMethod("Add");
                cache["Add"] = method;
            }

            if (prop != null) count++;
            if (field != null) count++;
            if (method != null) count++;
        }

        return count;
    }

    /// <summary>
    /// 基准测试6: 对象属性访问 - 原始反射
    /// </summary>
    [Benchmark(Description = "属性访问 - 原始反射")]
    public int Original_PropertyAccess()
    {
        var obj = new TestClass { Value = 42 };
        var prop = typeof(TestClass).GetProperty("Value")!;
        int total = 0;

        for (int i = 0; i < 1000; i++)
        {
            var value = (int)prop.GetValue(obj)!;
            total += value;
        }

        return total;
    }

    /// <summary>
    /// 基准测试7: 对象属性访问 - 直接访问
    /// </summary>
    [Benchmark(Description = "属性访问 - 直接访问")]
    public int Direct_PropertyAccess()
    {
        var obj = new TestClass { Value = 42 };
        int total = 0;

        for (int i = 0; i < 1000; i++)
        {
            var value = obj.Value;
            total += value;
        }

        return total;
    }
}

/// <summary>
/// 测试类
/// </summary>
public class TestClass
{
    public int Value { get; set; }
    public int Count;

    public int Add(int a, int b)
    {
        return a + b;
    }
}
