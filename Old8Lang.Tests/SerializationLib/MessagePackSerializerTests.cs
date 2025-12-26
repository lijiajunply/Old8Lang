using Old8Lang.SerializationLib;

namespace Old8Lang.Tests.SerializationLib;

/// <summary>
/// MessagePack 序列化测试
/// </summary>
public class MessagePackSerializerTests
{
    private readonly MessagePackSerializer _serializer = new();

    [Fact]
    public void Serialize_BasicTypes_ShouldWork()
    {
        // 测试基本类型
        var intValue = 42;
        var data = _serializer.Serialize(intValue);
        var result = _serializer.Deserialize<int>(data);
        Assert.Equal(intValue, result);

        var stringValue = "Hello, Old8Lang!";
        data = _serializer.Serialize(stringValue);
        var stringResult = _serializer.Deserialize<string>(data);
        Assert.Equal(stringValue, stringResult);
    }

    [Fact]
    public void Serialize_ComplexObject_ShouldWork()
    {
        // 测试复杂对象
        var obj = new TestObject
        {
            Id = 1,
            Name = "Test",
            Value = 3.14,
            Tags = new List<string> { "tag1", "tag2" }
        };

        var data = _serializer.Serialize(obj);
        var result = _serializer.Deserialize<TestObject>(data);

        Assert.Equal(obj.Id, result.Id);
        Assert.Equal(obj.Name, result.Name);
        Assert.Equal(obj.Value, result.Value);
        Assert.Equal(obj.Tags, result.Tags);
    }

    [Fact]
    public void SerializeToFile_ShouldWork()
    {
        var obj = new TestObject { Id = 1, Name = "File Test" };
        var filePath = Path.GetTempFileName();

        try
        {
            _serializer.SerializeToFile(obj, filePath);
            var result = _serializer.DeserializeFromFile<TestObject>(filePath);

            Assert.Equal(obj.Id, result.Id);
            Assert.Equal(obj.Name, result.Name);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void ToJson_ShouldWork()
    {
        var obj = new TestObject { Id = 1, Name = "JSON Test" };
        var json = _serializer.ToJson(obj);

        Assert.Contains("\"Id\":1", json);
        Assert.Contains("\"Name\":\"JSON Test\"", json);
    }
}

/// <summary>
/// 测试用对象
/// </summary>
public class TestObject
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public double Value { get; set; }
    public List<string> Tags { get; set; } = new();
}
