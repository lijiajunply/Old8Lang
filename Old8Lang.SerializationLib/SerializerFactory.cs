namespace Old8Lang.SerializationLib;

/// <summary>
/// 序列化格式枚举
/// </summary>
public enum SerializationFormat
{
    /// <summary>
    /// MessagePack 格式（高性能、紧凑）
    /// </summary>
    MessagePack,

    /// <summary>
    /// Protocol Buffers 格式（需要预定义消息格式）
    /// </summary>
    Protobuf
}

/// <summary>
/// 序列化工厂类，提供统一的序列化器创建接口
/// </summary>
public static class SerializerFactory
{
    /// <summary>
    /// 创建指定格式的序列化器
    /// </summary>
    public static ISerializer Create(SerializationFormat format)
    {
        return format switch
        {
            SerializationFormat.MessagePack => new MessagePackSerializer(),
            SerializationFormat.Protobuf => new ProtobufSerializer(),
            _ => throw new ArgumentException($"不支持的序列化格式: {format}")
        };
    }

    /// <summary>
    /// 创建默认序列化器（MessagePack）
    /// </summary>
    public static ISerializer CreateDefault()
    {
        return new MessagePackSerializer();
    }
}

/// <summary>
/// 序列化扩展方法
/// </summary>
public static class SerializationExtensions
{
    /// <summary>
    /// 将对象序列化为字节数组
    /// </summary>
    public static byte[] ToBytes<T>(this T obj, SerializationFormat format = SerializationFormat.MessagePack)
    {
        var serializer = SerializerFactory.Create(format);
        return serializer.Serialize(obj);
    }

    /// <summary>
    /// 从字节数组反序列化对象
    /// </summary>
    public static T FromBytes<T>(this byte[] data, SerializationFormat format = SerializationFormat.MessagePack)
    {
        var serializer = SerializerFactory.Create(format);
        return serializer.Deserialize<T>(data);
    }

    /// <summary>
    /// 将对象序列化到文件
    /// </summary>
    public static void SaveToFile<T>(this T obj, string filePath, SerializationFormat format = SerializationFormat.MessagePack)
    {
        var serializer = SerializerFactory.Create(format);
        serializer.SerializeToFile(obj, filePath);
    }

    /// <summary>
    /// 从文件反序列化对象
    /// </summary>
    public static T LoadFromFile<T>(string filePath, SerializationFormat format = SerializationFormat.MessagePack)
    {
        var serializer = SerializerFactory.Create(format);
        return serializer.DeserializeFromFile<T>(filePath);
    }

    /// <summary>
    /// 克隆对象（通过序列化/反序列化）
    /// </summary>
    public static T DeepClone<T>(this T obj, SerializationFormat format = SerializationFormat.MessagePack)
    {
        var serializer = SerializerFactory.Create(format);
        var data = serializer.Serialize(obj);
        return serializer.Deserialize<T>(data);
    }
}
