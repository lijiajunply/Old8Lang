namespace Old8Lang.Bytecode;

/// <summary>
/// 字节码指令
/// </summary>
public class Instruction
{
    /// <summary>操作码</summary>
    public OpCode OpCode { get; set; }

    /// <summary>操作数(可选)</summary>
    public object? Operand { get; set; }

    /// <summary>源文件路径(用于调试)</summary>
    public string? SourceFile { get; set; }

    /// <summary>源代码行号(用于调试)</summary>
    public int? LineNumber { get; set; }

    /// <summary>源代码列号(用于调试)</summary>
    public int? ColumnNumber { get; set; }

    public Instruction(OpCode opCode, object? operand = null)
    {
        OpCode = opCode;
        Operand = operand;
    }

    /// <summary>
    /// 设置调试信息
    /// </summary>
    public Instruction WithDebugInfo(string? sourceFile, int? lineNumber, int? columnNumber = null)
    {
        SourceFile = sourceFile;
        LineNumber = lineNumber;
        ColumnNumber = columnNumber;
        return this;
    }

    /// <summary>
    /// 写入二进制流
    /// </summary>
    public void WriteTo(BinaryWriter writer)
    {
        // 写入操作码
        writer.Write((byte)OpCode);

        // 写入操作数类型标记和值
        if (Operand == null)
        {
            writer.Write((byte)0); // 无操作数
        }
        else if (Operand is int intValue)
        {
            writer.Write((byte)1);
            writer.Write(intValue);
        }
        else if (Operand is long longValue)
        {
            writer.Write((byte)2);
            writer.Write(longValue);
        }
        else if (Operand is double doubleValue)
        {
            writer.Write((byte)3);
            writer.Write(doubleValue);
        }
        else if (Operand is string stringValue)
        {
            writer.Write((byte)4);
            writer.Write(stringValue);
        }
        else if (Operand is bool boolValue)
        {
            writer.Write((byte)5);
            writer.Write(boolValue);
        }
        else
        {
            throw new NotSupportedException($"不支持的操作数类型: {Operand.GetType()}");
        }
    }

    /// <summary>
    /// 从二进制流读取
    /// </summary>
    public static Instruction ReadFrom(BinaryReader reader)
    {
        var opCode = (OpCode)reader.ReadByte();
        var operandType = reader.ReadByte();

        object? operand = operandType switch
        {
            0 => null,
            1 => reader.ReadInt32(),
            2 => reader.ReadInt64(),
            3 => reader.ReadDouble(),
            4 => reader.ReadString(),
            5 => reader.ReadBoolean(),
            _ => throw new InvalidOperationException($"未知的操作数类型: {operandType}")
        };

        return new Instruction(opCode, operand);
    }

    public override string ToString()
    {
        if (Operand != null)
            return $"{OpCode} {Operand}";
        return OpCode.ToString();
    }
}
