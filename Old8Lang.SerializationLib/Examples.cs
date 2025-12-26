namespace Old8Lang.SerializationLib;

/// <summary>
/// 序列化库使用示例
/// </summary>
public class SerializationExamples
{
    public static void Main()
    {
        Console.WriteLine("=== Old8Lang 序列化库示例 ===\n");

        // 示例 1: 基本序列化
        BasicSerializationExample();

        // 示例 2: 文件序列化
        FileSerializationExample();

        // 示例 3: 扩展方法
        ExtensionMethodsExample();

        // 示例 4: 深度克隆
        DeepCloneExample();

        // 示例 5: 性能测试
        PerformanceTest();
    }

    static void BasicSerializationExample()
    {
        Console.WriteLine("--- 示例 1: 基本序列化 ---");

        var person = new Person
        {
            Name = "张三",
            Age = 25,
            Email = "zhangsan@example.com",
            Tags = new List<string> { "开发者", "架构师" }
        };

        // 使用 MessagePack
        var msgPackSerializer = new MessagePackSerializer();
        var msgPackData = msgPackSerializer.Serialize(person);
        var msgPackResult = msgPackSerializer.Deserialize<Person>(msgPackData);

        Console.WriteLine($"原始对象: {person}");
        Console.WriteLine($"MessagePack 大小: {msgPackData.Length} 字节");
        Console.WriteLine($"反序列化结果: {msgPackResult}\n");
    }

    static void FileSerializationExample()
    {
        Console.WriteLine("--- 示例 2: 文件序列化 ---");

        var data = new DataPackage
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTime.Now,
            Values = new Dictionary<string, double>
            {
                { "温度", 25.5 },
                { "湿度", 60.0 },
                { "压力", 1013.25 }
            }
        };

        var filePath = Path.Combine(Path.GetTempPath(), "data.msgpack");

        var serializer = SerializerFactory.CreateDefault();
        serializer.SerializeToFile(data, filePath);
        Console.WriteLine($"已保存到: {filePath}");

        var loaded = serializer.DeserializeFromFile<DataPackage>(filePath);
        Console.WriteLine($"已加载: {loaded}\n");

        File.Delete(filePath);
    }

    static void ExtensionMethodsExample()
    {
        Console.WriteLine("--- 示例 3: 扩展方法 ---");

        var config = new AppConfig
        {
            AppName = "Old8Lang",
            Version = "1.0.0",
            Settings = new Dictionary<string, string>
            {
                { "Theme", "Dark" },
                { "Language", "zh-CN" }
            }
        };

        // 使用扩展方法
        var bytes = config.ToBytes();
        Console.WriteLine($"序列化后大小: {bytes.Length} 字节");

        var restored = bytes.FromBytes<AppConfig>();
        Console.WriteLine($"还原配置: {restored}\n");
    }

    static void DeepCloneExample()
    {
        Console.WriteLine("--- 示例 4: 深度克隆 ---");

        var original = new Person
        {
            Name = "李四",
            Age = 30,
            Tags = new List<string> { "产品经理" }
        };

        var clone = original.DeepClone();
        clone.Name = "王五";
        clone.Tags.Add("设计师");

        Console.WriteLine($"原始对象: {original}");
        Console.WriteLine($"克隆对象: {clone}");
        Console.WriteLine($"对象不同: {!ReferenceEquals(original, clone)}");
        Console.WriteLine($"Tags 不同: {!ReferenceEquals(original.Tags, clone.Tags)}\n");
    }

    static void PerformanceTest()
    {
        Console.WriteLine("--- 示例 5: 性能测试 ---");

        var testData = Enumerable.Range(0, 1000).Select(i => new Person
        {
            Name = $"Person{i}",
            Age = i % 100,
            Email = $"person{i}@example.com",
            Tags = new List<string> { "Tag1", "Tag2", "Tag3" }
        }).ToList();

        var serializer = new MessagePackSerializer();

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var data = serializer.Serialize(testData);
        sw.Stop();

        Console.WriteLine($"序列化 1000 个对象耗时: {sw.ElapsedMilliseconds} ms");
        Console.WriteLine($"数据大小: {data.Length / 1024.0:F2} KB");

        sw.Restart();
        var result = serializer.Deserialize<List<Person>>(data);
        sw.Stop();

        Console.WriteLine($"反序列化 1000 个对象耗时: {sw.ElapsedMilliseconds} ms");
        Console.WriteLine($"验证: {result.Count == testData.Count}\n");
    }
}

public class Person
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public string Email { get; set; } = "";
    public List<string> Tags { get; set; } = new();

    public override string ToString()
    {
        return $"{Name}, {Age}岁, {Email}, 标签: [{string.Join(", ", Tags)}]";
    }
}

public class DataPackage
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, double> Values { get; set; } = new();

    public override string ToString()
    {
        var values = string.Join(", ", Values.Select(kv => $"{kv.Key}={kv.Value}"));
        return $"ID: {Id}, 时间: {Timestamp:yyyy-MM-dd HH:mm:ss}, 数据: [{values}]";
    }
}

public class AppConfig
{
    public string AppName { get; set; } = "";
    public string Version { get; set; } = "";
    public Dictionary<string, string> Settings { get; set; } = new();

    public override string ToString()
    {
        var settings = string.Join(", ", Settings.Select(kv => $"{kv.Key}={kv.Value}"));
        return $"{AppName} v{Version}, 设置: [{settings}]";
    }
}
