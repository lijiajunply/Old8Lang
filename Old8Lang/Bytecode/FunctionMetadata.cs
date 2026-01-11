namespace Old8Lang.Bytecode;

/// <summary>
/// 函数元数据
/// </summary>
public class FunctionMetadata
{
    /// <summary>函数名称</summary>
    public string Name { get; set; } = "";

    /// <summary>参数名称列表</summary>
    public List<string> Parameters { get; set; } = new();

    /// <summary>字节码指令列表</summary>
    public List<Instruction> Instructions { get; set; } = new();

    /// <summary>局部变量数量</summary>
    public int LocalCount { get; set; }

    /// <summary>最大栈深度</summary>
    public int MaxStackSize { get; set; }

    /// <summary>是否是异步函数</summary>
    public bool IsAsync { get; set; }

    /// <summary>是否是生成器函数</summary>
    public bool IsGenerator { get; set; }

    /// <summary>函数在常量池中的索引(用于闭包)</summary>
    public int FunctionIndex { get; set; } = -1;

    /// <summary>
    /// 写入二进制流
    /// </summary>
    public void WriteTo(BinaryWriter writer)
    {
        writer.Write(Name);

        // 参数
        writer.Write(Parameters.Count);
        foreach (var param in Parameters)
            writer.Write(param);

        // 指令
        writer.Write(Instructions.Count);
        foreach (var instruction in Instructions)
            instruction.WriteTo(writer);

        // 元数据
        writer.Write(LocalCount);
        writer.Write(MaxStackSize);
        writer.Write(IsAsync);
        writer.Write(IsGenerator);
        writer.Write(FunctionIndex);
    }

    /// <summary>
    /// 从二进制流读取
    /// </summary>
    public static FunctionMetadata ReadFrom(BinaryReader reader)
    {
        var func = new FunctionMetadata
        {
            Name = reader.ReadString()
        };

        // 参数
        int paramCount = reader.ReadInt32();
        for (int i = 0; i < paramCount; i++)
            func.Parameters.Add(reader.ReadString());

        // 指令
        int instCount = reader.ReadInt32();
        for (int i = 0; i < instCount; i++)
            func.Instructions.Add(Instruction.ReadFrom(reader));

        // 元数据
        func.LocalCount = reader.ReadInt32();
        func.MaxStackSize = reader.ReadInt32();
        func.IsAsync = reader.ReadBoolean();
        func.IsGenerator = reader.ReadBoolean();
        func.FunctionIndex = reader.ReadInt32();

        return func;
    }

    public override string ToString()
    {
        return $"Function {Name}({string.Join(", ", Parameters)}) [{Instructions.Count} instructions]";
    }
}
