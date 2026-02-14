namespace Old8Lang.Bytecode.Metadata;

/// <summary>
/// 扩展方法元数据
/// </summary>
public class ExtensionMetadata
{
    /// <summary>目标类型名称</summary>
    public string TargetTypeName { get; set; } = string.Empty;

    /// <summary>扩展方法列表</summary>
    public List<FunctionMetadata> Methods { get; set; } = [];

    /// <summary>
    /// 写入二进制流
    /// </summary>
    public void WriteTo(BinaryWriter writer)
    {
        writer.Write(TargetTypeName);
        writer.Write(Methods.Count);
        foreach (var method in Methods)
        {
            method.WriteTo(writer);
        }
    }

    /// <summary>
    /// 从二进制流读取
    /// </summary>
    public static ExtensionMetadata ReadFrom(BinaryReader reader)
    {
        var metadata = new ExtensionMetadata
        {
            TargetTypeName = reader.ReadString()
        };

        int methodCount = reader.ReadInt32();
        for (int i = 0; i < methodCount; i++)
        {
            metadata.Methods.Add(FunctionMetadata.ReadFrom(reader));
        }

        return metadata;
    }
}
