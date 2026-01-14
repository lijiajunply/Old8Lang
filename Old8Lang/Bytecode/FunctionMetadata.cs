namespace Old8Lang.Bytecode;

/// <summary>
/// 函数元数据
/// </summary>
public class FunctionMetadata
{
    /// <summary>函数名称</summary>
    public string Name { get; set; } = "";

    /// <summary>参数名称列表</summary>
    public List<string> Parameters { get; set; } = [];

    /// <summary>参数默认值列表（索引对应Parameters，null表示无默认值）</summary>
    public List<object?> DefaultValues { get; set; } = [];

    /// <summary>params参数的索引（-1表示没有params参数）</summary>
    public int ParamsParameterIndex { get; set; } = -1;

    /// <summary>字节码指令列表</summary>
    public List<Instruction> Instructions { get; set; } = [];

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

    /// <summary>异常表 - 记录try-catch-finally块的位置信息</summary>
    public List<ExceptionTableEntry> ExceptionTable { get; set; } = [];

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

        // 默认参数值
        writer.Write(DefaultValues.Count);
        foreach (var defaultValue in DefaultValues)
        {
            if (defaultValue == null)
            {
                writer.Write((byte)0); // null标记
            }
            else
            {
                writer.Write((byte)1); // 非null标记
                // 序列化默认值（支持基本类型）
                WriteDefaultValue(writer, defaultValue);
            }
        }

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
        writer.Write(ParamsParameterIndex);

        // 异常表
        writer.Write(ExceptionTable.Count);
        foreach (var entry in ExceptionTable)
            entry.WriteTo(writer);
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

        // 默认参数值
        int defaultValueCount = reader.ReadInt32();
        for (int i = 0; i < defaultValueCount; i++)
        {
            byte nullMarker = reader.ReadByte();
            if (nullMarker == 0)
            {
                func.DefaultValues.Add(null);
            }
            else
            {
                func.DefaultValues.Add(ReadDefaultValue(reader));
            }
        }

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
        func.ParamsParameterIndex = reader.ReadInt32();

        // 异常表
        int exceptionTableCount = reader.ReadInt32();
        for (int i = 0; i < exceptionTableCount; i++)
            func.ExceptionTable.Add(ExceptionTableEntry.ReadFrom(reader));

        return func;
    }

    public override string ToString()
    {
        return $"Function {Name}({string.Join(", ", Parameters)}) [{Instructions.Count} instructions]";
    }

    /// <summary>
    /// 序列化默认参数值
    /// </summary>
    private static void WriteDefaultValue(BinaryWriter writer, object value)
    {
        switch (value)
        {
            case int intValue:
                writer.Write((byte)1); // int类型标记
                writer.Write(intValue);
                break;
            case double doubleValue:
                writer.Write((byte)2); // double类型标记
                writer.Write(doubleValue);
                break;
            case string stringValue:
                writer.Write((byte)3); // string类型标记
                writer.Write(stringValue);
                break;
            case bool boolValue:
                writer.Write((byte)4); // bool类型标记
                writer.Write(boolValue);
                break;
            case char charValue:
                writer.Write((byte)5); // char类型标记
                writer.Write(charValue);
                break;
            default:
                throw new NotSupportedException($"不支持的默认参数类型: {value.GetType().Name}");
        }
    }

    /// <summary>
    /// 反序列化默认参数值
    /// </summary>
    private static object ReadDefaultValue(BinaryReader reader)
    {
        byte typeMarker = reader.ReadByte();
        return typeMarker switch
        {
            1 => reader.ReadInt32(),
            2 => reader.ReadDouble(),
            3 => reader.ReadString(),
            4 => reader.ReadBoolean(),
            5 => reader.ReadChar(),
            _ => throw new NotSupportedException($"不支持的默认参数类型标记: {typeMarker}")
        };
    }
}
