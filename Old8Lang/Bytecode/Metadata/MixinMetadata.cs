namespace Old8Lang.Bytecode;

/// <summary>
/// Mixin 元数据
/// </summary>
public class MixinMetadata
{
    /// <summary>Mixin 名称</summary>
    public string Name { get; set; } = "";

    /// <summary>方法列表（包含实现）</summary>
    public List<MethodMetadata> Methods { get; set; } = [];

    /// <summary>
    /// 写入二进制流
    /// </summary>
    public void WriteTo(BinaryWriter writer)
    {
        writer.Write(Name);

        // 方法列表
        writer.Write(Methods.Count);
        foreach (var method in Methods)
            method.WriteTo(writer);
    }

    /// <summary>
    /// 从二进制流读取
    /// </summary>
    public static MixinMetadata ReadFrom(BinaryReader reader)
    {
        var mixinMetadata = new MixinMetadata
        {
            Name = reader.ReadString()
        };

        // 方法列表
        int methodCount = reader.ReadInt32();
        for (int i = 0; i < methodCount; i++)
            mixinMetadata.Methods.Add(MethodMetadata.ReadFrom(reader));

        return mixinMetadata;
    }

    public override string ToString()
    {
        return $"mixin {Name} {{ {Methods.Count} methods }}";
    }
}
