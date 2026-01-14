namespace Old8Lang.Bytecode;

/// <summary>
/// 接口元数据
/// </summary>
public class InterfaceMetadata
{
    /// <summary>接口名</summary>
    public string Name { get; set; } = "";

    /// <summary>父接口列表</summary>
    public List<string> ParentInterfaces { get; set; } = [];

    /// <summary>方法签名列表（只有方法名，没有实现）</summary>
    public List<string> Methods { get; set; } = [];

    /// <summary>
    /// 写入二进制流
    /// </summary>
    public void WriteTo(BinaryWriter writer)
    {
        writer.Write(Name);

        // 父接口列表
        writer.Write(ParentInterfaces.Count);
        foreach (var parentInterface in ParentInterfaces)
            writer.Write(parentInterface);

        // 方法列表
        writer.Write(Methods.Count);
        foreach (var method in Methods)
            writer.Write(method);
    }

    /// <summary>
    /// 从二进制流读取
    /// </summary>
    public static InterfaceMetadata ReadFrom(BinaryReader reader)
    {
        var interfaceMetadata = new InterfaceMetadata
        {
            Name = reader.ReadString()
        };

        // 父接口列表
        int parentCount = reader.ReadInt32();
        for (int i = 0; i < parentCount; i++)
            interfaceMetadata.ParentInterfaces.Add(reader.ReadString());

        // 方法列表
        int methodCount = reader.ReadInt32();
        for (int i = 0; i < methodCount; i++)
            interfaceMetadata.Methods.Add(reader.ReadString());

        return interfaceMetadata;
    }

    public override string ToString()
    {
        var parentStr = ParentInterfaces.Count > 0
            ? $" extends {string.Join(", ", ParentInterfaces)}"
            : "";
        return $"interface {Name}{parentStr} {{ {Methods.Count} methods }}";
    }
}
