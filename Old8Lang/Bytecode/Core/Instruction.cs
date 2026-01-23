namespace Old8Lang.Bytecode.Core;

/// <summary>
/// 字节码指令
/// </summary>
public class Instruction(OpCode opCode, object? operand = null)
{
    /// <summary>操作码</summary>
    public OpCode OpCode { get; set; } = opCode;

    /// <summary>操作数(可选)</summary>
    public object? Operand { get; set; } = operand;

    /// <summary>源文件路径(用于调试)</summary>
    public string? SourceFile { get; set; }

    /// <summary>源代码行号(用于调试)</summary>
    public int? LineNumber { get; set; }

    /// <summary>源代码列号(用于调试)</summary>
    public int? ColumnNumber { get; set; }

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
        else if (Operand is object[] arrayValue)
        {
            // 支持数组类型的操作数
            writer.Write((byte)6);
            writer.Write(arrayValue.Length);
            foreach (var item in arrayValue)
            {
                // 递归序列化数组元素
                if (item == null)
                {
                    writer.Write((byte)0);
                }
                else if (item is int intItem)
                {
                    writer.Write((byte)1);
                    writer.Write(intItem);
                }
                else if (item is string stringItem)
                {
                    writer.Write((byte)4);
                    writer.Write(stringItem);
                }
                else
                {
                    throw new NotSupportedException($"不支持的数组元素类型: {item.GetType()}");
                }
            }
        }
        else
        {
            throw new NotSupportedException($"不支持的操作数类型: {Operand.GetType()}, OpCode: {OpCode}");
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
            6 => ReadArray(reader),
            _ => throw new InvalidOperationException($"未知的操作数类型: {operandType}")
        };

        return new Instruction(opCode, operand);
    }

    /// <summary>
    /// 读取数组操作数
    /// </summary>
    private static object[] ReadArray(BinaryReader reader)
    {
        var length = reader.ReadInt32();
        var array = new object[length];

        for (int i = 0; i < length; i++)
        {
            var elementType = reader.ReadByte();
            array[i] = elementType switch
            {
                0 => null!,
                1 => reader.ReadInt32(),
                4 => reader.ReadString(),
                _ => throw new InvalidOperationException($"未知的数组元素类型: {elementType}")
            };
        }

        return array;
    }

    public override string ToString()
    {
        if (Operand != null)
            return $"{OpCode} {Operand}";
        return OpCode.ToString();
    }
}