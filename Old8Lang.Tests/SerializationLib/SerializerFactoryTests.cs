using Old8Lang.SerializationLib;

namespace Old8Lang.Tests.SerializationLib;

/// <summary>
/// 序列化工厂和扩展方法测试
/// </summary>
public class SerializerFactoryTests
{
    [Fact]
    public void CreateMessagePackSerializer_ShouldWork()
    {
        var serializer = SerializerFactory.Create(SerializationFormat.MessagePack);
        Assert.IsType<MessagePackSerializer>(serializer);
    }

    [Fact]
    public void CreateProtobufSerializer_ShouldWork()
    {
        var serializer = SerializerFactory.Create(SerializationFormat.Protobuf);
        Assert.IsType<ProtobufSerializer>(serializer);
    }

    [Fact]
    public void CreateDefaultSerializer_ShouldReturnMessagePack()
    {
        var serializer = SerializerFactory.CreateDefault();
        Assert.IsType<MessagePackSerializer>(serializer);
    }

    [Fact]
    public void ExtensionMethods_ToBytes_ShouldWork()
    {
        var obj = new TestData { Value = 123, Text = "Test" };
        var bytes = obj.ToBytes();
        var result = bytes.FromBytes<TestData>();

        Assert.Equal(obj.Value, result.Value);
        Assert.Equal(obj.Text, result.Text);
    }

    [Fact]
    public void ExtensionMethods_SaveToFile_ShouldWork()
    {
        var obj = new TestData { Value = 456, Text = "File Test" };
        var filePath = Path.GetTempFileName();

        try
        {
            obj.SaveToFile(filePath);
            var result = SerializationExtensions.LoadFromFile<TestData>(filePath);

            Assert.Equal(obj.Value, result.Value);
            Assert.Equal(obj.Text, result.Text);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void ExtensionMethods_DeepClone_ShouldWork()
    {
        var obj = new TestData
        {
            Value = 789,
            Text = "Original",
            Items = [1, 2, 3]
        };

        var clone = obj.DeepClone();

        Assert.Equal(obj.Value, clone.Value);
        Assert.Equal(obj.Text, clone.Text);
        Assert.Equal(obj.Items, clone.Items);
        Assert.NotSame(obj, clone);
        Assert.NotSame(obj.Items, clone.Items);
    }

    [Fact]
    public void DeepClone_ModifyingClone_ShouldNotAffectOriginal()
    {
        var obj = new TestData
        {
            Value = 100,
            Items = [1, 2, 3]
        };

        var clone = obj.DeepClone();
        clone.Value = 200;
        clone.Items.Add(4);

        Assert.Equal(100, obj.Value);
        Assert.Equal(3, obj.Items.Count);
        Assert.Equal(200, clone.Value);
        Assert.Equal(4, clone.Items.Count);
    }
}

public class TestData
{
    public int Value { get; set; }
    public string Text { get; set; } = "";
    public List<int> Items { get; set; } = [];
}
