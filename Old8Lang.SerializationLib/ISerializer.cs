namespace Old8Lang.SerializationLib;

/// <summary>
/// 序列化器接口
/// </summary>
public interface ISerializer
{
    /// <summary>
    /// 序列化对象到字节数组
    /// </summary>
    byte[] Serialize<T>(T obj);

    /// <summary>
    /// 从字节数组反序列化对象
    /// </summary>
    T Deserialize<T>(byte[] data);

    /// <summary>
    /// 序列化对象到文件
    /// </summary>
    void SerializeToFile<T>(T obj, string filePath);

    /// <summary>
    /// 从文件反序列化对象
    /// </summary>
    T DeserializeFromFile<T>(string filePath);
}
