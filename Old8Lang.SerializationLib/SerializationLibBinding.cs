namespace Old8Lang.SerializationLib;

/// <summary>
/// Old8Lang 序列化库绑定类
/// 提供给 Old8Lang 语言使用的序列化功能
/// </summary>
public static class SerializationLibBinding
{
    /// <summary>
    /// MessagePack 序列化对象到字节数组
    /// </summary>
    public static byte[] MsgPackSerialize(object obj)
    {
        var serializer = new MessagePackSerializer();
        return serializer.Serialize(obj);
    }

    /// <summary>
    /// MessagePack 从字节数组反序列化
    /// </summary>
    public static object MsgPackDeserialize(byte[] data, Type targetType)
    {
        var serializer = new MessagePackSerializer();
        var method = typeof(MessagePackSerializer)
            .GetMethod(nameof(ISerializer.Deserialize))!
            .MakeGenericMethod(targetType);
        return method.Invoke(serializer, [data])!;
    }

    /// <summary>
    /// MessagePack 序列化到文件
    /// </summary>
    public static void MsgPackSerializeToFile(object obj, string filePath)
    {
        var serializer = new MessagePackSerializer();
        serializer.SerializeToFile(obj, filePath);
    }

    /// <summary>
    /// MessagePack 从文件反序列化
    /// </summary>
    public static object MsgPackDeserializeFromFile(string filePath, Type targetType)
    {
        var serializer = new MessagePackSerializer();
        var method = typeof(MessagePackSerializer)
            .GetMethod(nameof(ISerializer.DeserializeFromFile))!
            .MakeGenericMethod(targetType);
        return method.Invoke(serializer, [filePath])!;
    }

    /// <summary>
    /// MessagePack 转换为 JSON 字符串
    /// </summary>
    public static string MsgPackToJson(object obj)
    {
        var serializer = new MessagePackSerializer();
        return serializer.ToJson(obj);
    }

    /// <summary>
    /// Protobuf 序列化对象到字节数组
    /// </summary>
    public static byte[] ProtobufSerialize(object obj)
    {
        var serializer = new ProtobufSerializer();
        return serializer.Serialize(obj);
    }

    /// <summary>
    /// Protobuf 从字节数组反序列化
    /// </summary>
    public static object ProtobufDeserialize(byte[] data, Type targetType)
    {
        var serializer = new ProtobufSerializer();
        var method = typeof(ProtobufSerializer)
            .GetMethod(nameof(ISerializer.Deserialize))!
            .MakeGenericMethod(targetType);
        return method.Invoke(serializer, [data])!;
    }

    /// <summary>
    /// Protobuf 序列化到文件
    /// </summary>
    public static void ProtobufSerializeToFile(object obj, string filePath)
    {
        var serializer = new ProtobufSerializer();
        serializer.SerializeToFile(obj, filePath);
    }

    /// <summary>
    /// Protobuf 从文件反序列化
    /// </summary>
    public static object ProtobufDeserializeFromFile(string filePath, Type targetType)
    {
        var serializer = new ProtobufSerializer();
        var method = typeof(ProtobufSerializer)
            .GetMethod(nameof(ISerializer.DeserializeFromFile))!
            .MakeGenericMethod(targetType);
        return method.Invoke(serializer, [filePath])!;
    }

    /// <summary>
    /// Protobuf 转换为 JSON 字符串
    /// </summary>
    public static string ProtobufToJson(object obj)
    {
        var serializer = new ProtobufSerializer();
        return serializer.ToJson(obj);
    }

    /// <summary>
    /// 深度克隆对象（使用 MessagePack）
    /// </summary>
    public static object DeepClone(object obj)
    {
        var serializer = new MessagePackSerializer();
        var data = serializer.Serialize(obj);
        var targetType = obj.GetType();
        var method = typeof(MessagePackSerializer)
            .GetMethod(nameof(ISerializer.Deserialize))!
            .MakeGenericMethod(targetType);
        return method.Invoke(serializer, [data])!;
    }
}
