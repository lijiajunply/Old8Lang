using Google.Protobuf;

namespace Old8Lang.SerializationLib;

/// <summary>
/// Protobuf 序列化器实现
/// 注意：被序列化的对象必须是 IMessage 类型（由 protoc 生成）
/// </summary>
public class ProtobufSerializer : ISerializer
{
    /// <summary>
    /// 序列化对象到字节数组
    /// </summary>
    public byte[] Serialize<T>(T obj)
    {
        if (obj is IMessage message)
        {
            return message.ToByteArray();
        }
        throw new InvalidOperationException(
            $"类型 {typeof(T).Name} 必须实现 IMessage 接口。请使用 protoc 生成相应的类。");
    }

    /// <summary>
    /// 从字节数组反序列化对象
    /// </summary>
    public T Deserialize<T>(byte[] data)
    {
        var type = typeof(T);

        // 查找 Parser 属性
        var parserProperty = type.GetProperty("Parser",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        if (parserProperty == null)
        {
            throw new InvalidOperationException(
                $"类型 {type.Name} 没有 Parser 属性。请确保使用 protoc 生成的类。");
        }

        var parser = parserProperty.GetValue(null) as MessageParser;
        if (parser == null)
        {
            throw new InvalidOperationException(
                $"无法获取 {type.Name} 的 Parser。");
        }

        var message = parser.ParseFrom(data);
        return (T)message;
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
        if (obj is IMessage message)
        {
            message.WriteTo(stream);
            return;
        }
        throw new InvalidOperationException(
            $"类型 {typeof(T).Name} 必须实现 IMessage 接口。");
    }

    /// <summary>
    /// 从流反序列化对象
    /// </summary>
    public T DeserializeFromStream<T>(Stream stream)
    {
        var type = typeof(T);
        var parserProperty = type.GetProperty("Parser",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        if (parserProperty == null)
        {
            throw new InvalidOperationException(
                $"类型 {type.Name} 没有 Parser 属性。");
        }

        var parser = parserProperty.GetValue(null) as MessageParser;
        if (parser == null)
        {
            throw new InvalidOperationException(
                $"无法获取 {type.Name} 的 Parser。");
        }

        var message = parser.ParseFrom(stream);
        return (T)message;
    }

    /// <summary>
    /// 转换为 JSON 字符串
    /// </summary>
    public string ToJson<T>(T obj)
    {
        if (obj is IMessage message)
        {
            return JsonFormatter.Default.Format(message);
        }
        throw new InvalidOperationException(
            $"类型 {typeof(T).Name} 必须实现 IMessage 接口。");
    }

    /// <summary>
    /// 从 JSON 字符串转换为对象
    /// </summary>
    public T FromJson<T>(string json)
    {
        var type = typeof(T);
        var parserProperty = type.GetProperty("Parser",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        if (parserProperty == null)
        {
            throw new InvalidOperationException(
                $"类型 {type.Name} 没有 Parser 属性。");
        }

        var parser = parserProperty.GetValue(null) as MessageParser;
        if (parser == null)
        {
            throw new InvalidOperationException(
                $"无法获取 {type.Name} 的 Parser。");
        }

        var message = parser.ParseJson(json);
        return (T)message;
    }
}
