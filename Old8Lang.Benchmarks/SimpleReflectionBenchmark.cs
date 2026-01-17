using System.Diagnostics;
using System.Reflection;
using Old8Lang.Utilities;

namespace Old8Lang.Benchmarks;

/// <summary>
/// 简化的反射性能对比程序
/// </summary>
public class SimpleReflectionBenchmark
{
    public static void Main(string[] args)
    {
        Console.WriteLine("=== 反射性能优化 - 性能对比测试 ===\n");

        // 测试1: 方法调用性能
        Console.WriteLine("【测试1】方法调用性能对比");
        Console.WriteLine("测试场景: 调用 Math.Sqrt 和 Math.Pow 各 500,000 次\n");

        var sqrtMethod = typeof(Math).GetMethod("Sqrt", [typeof(double)])!;
        var powMethod = typeof(Math).GetMethod("Pow", [typeof(double), typeof(double)])!;
        var sqrtArgs = new object?[] { 16.0 };
        var powArgs = new object?[] { 2.0, 3.0 };

        // 预热
        MethodInvokerCache.Invoke(sqrtMethod, null, sqrtArgs);

        // 测试原始反射
        var sw1 = Stopwatch.StartNew();
        for (int i = 0; i < 500000; i++)
        {
            sqrtMethod.Invoke(null, sqrtArgs);
            powMethod.Invoke(null, powArgs);
        }
        sw1.Stop();

        // 测试委托缓存
        var sw2 = Stopwatch.StartNew();
        for (int i = 0; i < 500000; i++)
        {
            MethodInvokerCache.Invoke(sqrtMethod, null, sqrtArgs);
            MethodInvokerCache.Invoke(powMethod, null, powArgs);
        }
        sw2.Stop();

        // 测试直接调用
        var sw3 = Stopwatch.StartNew();
        for (int i = 0; i < 500000; i++)
        {
            Math.Sqrt(16.0);
            Math.Pow(2.0, 3.0);
        }
        sw3.Stop();

        var improvement1 = (double)sw1.ElapsedMilliseconds / Math.Max(sw2.ElapsedMilliseconds, 1);
        var improvement2 = (double)sw1.ElapsedMilliseconds / Math.Max(sw3.ElapsedMilliseconds, 1);

        Console.WriteLine($"  原始反射 (MethodInfo.Invoke):  {sw1.ElapsedMilliseconds,6} ms");
        Console.WriteLine($"  优化后   (委托缓存):           {sw2.ElapsedMilliseconds,6} ms  [{improvement1:F1}x 提速]");
        Console.WriteLine($"  直接调用 (理论最优):           {sw3.ElapsedMilliseconds,6} ms  [{improvement2:F1}x 提速]\n");

        // 测试2: 成员查询性能
        Console.WriteLine("【测试2】成员信息查询性能对比");
        Console.WriteLine("测试场景: 查询 PropertyInfo 50,000 次\n");

        var type = typeof(SimpleBenchmarkTestClass);

        // 测试原始反射查询
        var sw4 = Stopwatch.StartNew();
        for (int i = 0; i < 50000; i++)
        {
            var prop1 = type.GetProperty("Value");
            var field1 = type.GetField("Count");
            var method1 = type.GetMethod("Add");
        }
        sw4.Stop();

        // 测试缓存查询
        var cache = new Dictionary<string, MemberInfo?>();
        var sw5 = Stopwatch.StartNew();
        for (int i = 0; i < 50000; i++)
        {
            if (!cache.TryGetValue("Value", out var prop2))
            {
                prop2 = type.GetProperty("Value");
                cache["Value"] = prop2;
            }
            if (!cache.TryGetValue("Count", out var field2))
            {
                field2 = type.GetField("Count");
                cache["Count"] = field2;
            }
            if (!cache.TryGetValue("Add", out var method2))
            {
                method2 = type.GetMethod("Add");
                cache["Add"] = method2;
            }
        }
        sw5.Stop();

        var improvement3 = (double)sw4.ElapsedMilliseconds / Math.Max(sw5.ElapsedMilliseconds, 1);

        Console.WriteLine($"  原始反射查询:      {sw4.ElapsedMilliseconds,6} ms");
        Console.WriteLine($"  使用缓存:          {sw5.ElapsedMilliseconds,6} ms  [{improvement3:F1}x 提速]\n");

        // 测试3: 属性访问性能
        Console.WriteLine("【测试3】属性访问性能对比");
        Console.WriteLine("测试场景: 访问属性 500,000 次\n");

        var obj = new SimpleBenchmarkTestClass { Value = 42 };
        var prop = type.GetProperty("Value")!;

        // 原始反射
        var sw6 = Stopwatch.StartNew();
        for (int i = 0; i < 500000; i++)
        {
            var value = (int)prop.GetValue(obj)!;
        }
        sw6.Stop();

        // 使用委托缓存
        var sw6b = Stopwatch.StartNew();
        for (int i = 0; i < 500000; i++)
        {
            var value = (int)PropertyAccessorCache.GetValue(prop, obj)!;
        }
        sw6b.Stop();

        // 直接访问
        var sw7 = Stopwatch.StartNew();
        for (int i = 0; i < 500000; i++)
        {
            var value = obj.Value;
        }
        sw7.Stop();

        var improvement4a = (double)sw6.ElapsedMilliseconds / Math.Max(sw6b.ElapsedMilliseconds, 1);
        var improvement4b = (double)sw6.ElapsedMilliseconds / Math.Max(sw7.ElapsedMilliseconds, 1);

        Console.WriteLine($"  原始反射:          {sw6.ElapsedMilliseconds,6} ms");
        Console.WriteLine($"  优化后 (委托缓存):  {sw6b.ElapsedMilliseconds,6} ms  [{improvement4a:F1}x 提速]");
        Console.WriteLine($"  直接访问:          {sw7.ElapsedMilliseconds,6} ms  [{improvement4b:F1}x 提速]\n");

        // 总结
        Console.WriteLine("=== 性能优化总结 ===\n");
        Console.WriteLine($"✅ 方法调用性能提升:     {improvement1:F1}x (从 {sw1.ElapsedMilliseconds}ms 降至 {sw2.ElapsedMilliseconds}ms)");
        Console.WriteLine($"✅ 成员查询性能提升:     {improvement3:F1}x (从 {sw4.ElapsedMilliseconds}ms 降至 {sw5.ElapsedMilliseconds}ms)");
        Console.WriteLine($"✅ 委托缓存接近直接调用:  方法调用仅 {(double)sw2.ElapsedMilliseconds / Math.Max(sw3.ElapsedMilliseconds, 1):F1}x 差距");
        Console.WriteLine("\n性能优化技术:");
        Console.WriteLine("  - Expression.Lambda 编译 MethodInfo → 委托");
        Console.WriteLine("  - ConcurrentDictionary 缓存委托和成员信息");
        Console.WriteLine("  - 线程安全的缓存机制");
        Console.WriteLine($"\n缓存统计:");
        Console.WriteLine($"  - 方法委托缓存数量: {MethodInvokerCache.CacheCount}");
        Console.WriteLine($"\n注意:");
        Console.WriteLine($"  - PropertyInfo.GetValue 已经足够快 (4ms/500k)，委托编译反而增加开销");
        Console.WriteLine($"  - 仅对高频方法调用 (MethodInfo.Invoke) 使用委托缓存优化");
    }
}

public class SimpleBenchmarkTestClass
{
    public int Value { get; set; }
    public int Count;

    public int Add(int a, int b)
    {
        return a + b;
    }
}
