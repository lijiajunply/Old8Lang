using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Old8Lang.Compiler;

public static class IlDisassembler
{
    private static readonly Dictionary<short, OpCode> OpCodeMap = BuildOpCodeMap();

    public static string Disassemble(MethodBase method)
    {
        var body = method.GetMethodBody();
        if (body == null) return $"{method.DeclaringType?.FullName}.{method.Name}: <no body>";

        var il = body.GetILAsByteArray();
        if (il == null || il.Length == 0) return $"{method.DeclaringType?.FullName}.{method.Name}: <empty>";

        var sb = new StringBuilder();
        sb.AppendLine($"{method.DeclaringType?.FullName}.{method.Name}");

        int offset = 0;
        while (offset < il.Length)
        {
            int start = offset;
            var op = ReadOpCode(il, ref offset);
            sb.Append(start.ToString("X4"));
            sb.Append(": ");
            sb.Append(op.Name);

            switch (op.OperandType)
            {
                case OperandType.InlineNone:
                    break;
                case OperandType.ShortInlineI:
                    sb.Append(' ');
                    sb.Append((sbyte)il[offset]);
                    offset += 1;
                    break;
                case OperandType.InlineI:
                    sb.Append(' ');
                    sb.Append(BitConverter.ToInt32(il, offset));
                    offset += 4;
                    break;
                case OperandType.InlineI8:
                    sb.Append(' ');
                    sb.Append(BitConverter.ToInt64(il, offset));
                    offset += 8;
                    break;
                case OperandType.ShortInlineR:
                    sb.Append(' ');
                    sb.Append(BitConverter.ToSingle(il, offset));
                    offset += 4;
                    break;
                case OperandType.InlineR:
                    sb.Append(' ');
                    sb.Append(BitConverter.ToDouble(il, offset));
                    offset += 8;
                    break;
                case OperandType.ShortInlineBrTarget:
                    {
                        sbyte delta = (sbyte)il[offset];
                        offset += 1;
                        int target = offset + delta;
                        sb.Append(' ');
                        sb.Append(target.ToString("X4"));
                        break;
                    }
                case OperandType.InlineBrTarget:
                    {
                        int delta = BitConverter.ToInt32(il, offset);
                        offset += 4;
                        int target = offset + delta;
                        sb.Append(' ');
                        sb.Append(target.ToString("X4"));
                        break;
                    }
                case OperandType.InlineSwitch:
                    {
                        int count = BitConverter.ToInt32(il, offset);
                        offset += 4;
                        sb.Append(" (");
                        for (int i = 0; i < count; i++)
                        {
                            int delta = BitConverter.ToInt32(il, offset);
                            offset += 4;
                            int target = offset + delta;
                            if (i > 0) sb.Append(", ");
                            sb.Append(target.ToString("X4"));
                        }
                        sb.Append(')');
                        break;
                    }
                case OperandType.InlineString:
                case OperandType.InlineField:
                case OperandType.InlineMethod:
                case OperandType.InlineType:
                case OperandType.InlineTok:
                case OperandType.InlineSig:
                    {
                        int token = BitConverter.ToInt32(il, offset);
                        offset += 4;
                        sb.Append(" 0x");
                        sb.Append(token.ToString("X8"));
                        break;
                    }
                case OperandType.ShortInlineVar:
                    sb.Append(' ');
                    sb.Append(il[offset]);
                    offset += 1;
                    break;
                case OperandType.InlineVar:
                    sb.Append(' ');
                    sb.Append(BitConverter.ToUInt16(il, offset));
                    offset += 2;
                    break;
                default:
                    sb.Append(" <unknown>");
                    break;
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static OpCode ReadOpCode(byte[] il, ref int offset)
    {
        byte code = il[offset++];
        if (code != 0xFE)
        {
            return OpCodeMap[(short)code];
        }

        byte code2 = il[offset++];
        short value = (short)(0xFE00 | code2);
        return OpCodeMap[value];
    }

    private static Dictionary<short, OpCode> BuildOpCodeMap()
    {
        var map = new Dictionary<short, OpCode>();
        foreach (var field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.GetValue(null) is OpCode op)
            {
                map[op.Value] = op;
            }
        }
        return map;
    }
}

