using MessagePack;
using MessagePack.Resolvers;

namespace Old8Lang.SerializationLib;

/// <summary>
/// MessagePack 序列化器实现
/// </summary>
public class MessagePackSerializer : ISerializer
{
    private readonly MessagePackSerializerOptions _options;

    /// <summary>
    /// 默认构造函数，使用标准解析器
    /// </summary>
    public MessagePackSerializer()
    {
        _options = MessagePackSerializerOptions.Standard
            .WithResolver(ContractlessStandardResolver.Instance);
    }

    /// <summary>
    /// 自定义构造函数，允许指定序列化选项
    /// </summary>
    public MessagePackSerializer(MessagePackSerializerOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// 序列化对象到字节数组
    /// </summary>
    public byte[] Serialize<T>(T obj)
    {
        return global::MessagePack.MessagePackSerializer.Serialize(obj, _options);
    }

    /// <summary>
    /// 从字节数组反序列化对象
    /// </summary>
    public T Deserialize<T>(byte[] data)
    {
        return global::MessagePack.MessagePackSerializer.Deserialize<T>(data, _options);
    }

    /// <summary>
    /// 序列化对象到文件
    /// </summary>
    public void SerializeToFile<T>(T obj, string filePath)
    {
        var data = Serialize(obj);
        File.WriteAllBytes(filePath, data);
    }

    /// <summary>
    /// 从文件反序列化对象
    /// </summary>
    public T DeserializeFromFile<T>(string filePath)
    {
        var data = File.ReadAllBytes(filePath);
        return Deserialize<T>(data);
    }

    /// <summary>
    /// 序列化对象到流
    /// </summary>
    public void SerializeToStream<T>(T obj, Stream stream)
    {
        global::MessagePack.MessagePackSerializer.Serialize(stream, obj, _options);
    }

    /// <summary>
    /// 从流反序列化对象
    /// </summary>
    public T DeserializeFromStream<T>(Stream stream)
    {
        return global::MessagePack.MessagePackSerializer.Deserialize<T>(stream, _options);
    }

    /// <summary>
    /// 转换为 JSON 字符串（用于调试）
    /// </summary>
    public string ToJson<T>(T obj)
    {
        return global::MessagePack.MessagePackSerializer.ConvertToJson(Serialize(obj));
    }

    /// <summary>
    /// 从 JSON 字符串转换为对象
    /// </summary>
    public byte[] ConvertFromJson(string json)
    {
        return global::MessagePack.MessagePackSerializer.ConvertFromJson(json);
    }
}
