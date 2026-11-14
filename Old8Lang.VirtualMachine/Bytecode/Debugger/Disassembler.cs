using System.Text;
using Old8Lang.Bytecode.Metadata;
using Old8Lang.Bytecode.Core;

namespace Old8Lang.Bytecode.Debugger;

/// <summary>
/// 字节码反汇编器，用于将字节码转换为可读的文本格式
/// </summary>
public class Disassembler
{
    private readonly ConstantPool? _constantPool;
    private readonly DebugInfo? _debugInfo;
    private readonly bool _showOffsets;
    private readonly bool _showDebugInfo;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="constantPool">常量池（可选）</param>
    /// <param name="debugInfo">调试信息（可选）</param>
    /// <param name="showOffsets">是否显示指令偏移</param>
    /// <param name="showDebugInfo">是否显示调试信息</param>
    public Disassembler(
        ConstantPool? constantPool = null,
        DebugInfo? debugInfo = null,
        bool showOffsets = true,
        bool showDebugInfo = true)
    {
        _constantPool = constantPool;
        _debugInfo = debugInfo;
        _showOffsets = showOffsets;
        _showDebugInfo = showDebugInfo;
    }

    /// <summary>
    /// 反汇编单个指令
    /// </summary>
    public string DisassembleInstruction(Instruction instruction, int offset = -1)
    {
        var sb = new StringBuilder();

        // 显示偏移量
        if (_showOffsets && offset >= 0)
        {
            sb.Append($"{offset,6:D6}: ");
        }

        // 显示操作码
        sb.Append($"{instruction.OpCode,-20}");

        // 显示操作数
        if (instruction.Operand != null)
        {
            sb.Append($" {FormatOperand(instruction)}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// 格式化操作数
    /// </summary>
    private string FormatOperand(Instruction instruction)
    {
        if (instruction.Operand == null)
            return "";

        // 根据操作码类型格式化操作数
        switch (instruction.OpCode)
        {
            case OpCode.LoadConst:
                // 显示常量值
                if (_constantPool != null && instruction.Operand is int constIndex)
                {
                    var value = _constantPool.GetConstant(constIndex);
                    return $"#{constIndex} ({FormatValue(value)})";
                }
                return $"#{instruction.Operand}";

            case OpCode.LoadLocal:
            case OpCode.StoreLocal:
                // 显示局部变量索引和名称
                return $"local[{instruction.Operand}]";

            case OpCode.LoadGlobal:
            case OpCode.StoreGlobal:
                // 显示全局变量名称
                if (_constantPool != null && instruction.Operand is int nameIndex)
                {
                    var name = _constantPool.GetConstant(nameIndex);
                    return $"\"{name}\"";
                }
                return $"#{instruction.Operand}";

            case OpCode.Jump:
            case OpCode.JumpIfFalse:
            case OpCode.JumpIfTrue:
                // 显示跳转目标
                return $"-> {instruction.Operand}";

            case OpCode.Call:
            case OpCode.CallNative:
            case OpCode.CallMethod:
                // 显示函数名称
                if (instruction.Operand is object[] { Length: >= 2 } args)
                {
                    var argCount = args[0];
                    var funcNameIndex = args[1];
                    if (_constantPool != null && funcNameIndex is int idx)
                    {
                        var funcName = _constantPool.GetConstant(idx);
                        return $"{funcName}({argCount} args)";
                    }
                    return $"#{funcNameIndex}({argCount} args)";
                }
                return instruction.Operand.ToString() ?? "";

            default:
                return instruction.Operand.ToString() ?? "";
        }
    }

    /// <summary>
    /// 格式化常量值
    /// </summary>
    private string FormatValue(object? value)
    {
        return value switch
        {
            null => "null",
            string s => $"\"{s}\"",
            char c => $"'{c}'",
            bool b => b ? "true" : "false",
            _ => value.ToString() ?? "?"
        };
    }

    /// <summary>
    /// 反汇编函数
    /// </summary>
    public string DisassembleFunction(FunctionMetadata function)
    {
        var sb = new StringBuilder();

        // 函数头部
        sb.AppendLine($"Function: {function.Name}");
        sb.AppendLine($"Parameters: {string.Join(", ", function.Parameters)}");
        sb.AppendLine($"Locals: {function.LocalCount}, MaxStack: {function.MaxStackSize}");
        if (function.IsAsync)
            sb.AppendLine("Flags: async");
        if (function.IsGenerator)
            sb.AppendLine("Flags: generator");
        sb.AppendLine();

        // 反汇编指令
        for (int i = 0; i < function.Instructions.Count; i++)
        {
            var instruction = function.Instructions[i];
            sb.AppendLine(DisassembleInstruction(instruction, i));

            // 显示调试信息
            if (_showDebugInfo && _debugInfo != null)
            {
                var location = _debugInfo.GetSourceLocation(i);
                if (location != null)
                {
                    sb.AppendLine($"        ; {location}");
                }
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// 反汇编常量池
    /// </summary>
    public string DisassembleConstantPool()
    {
        if (_constantPool == null)
            return "No constant pool available";

        var sb = new StringBuilder();
        sb.AppendLine("Constant Pool:");
        sb.AppendLine("==============");

        for (int i = 0; i < _constantPool.Count; i++)
        {
            var value = _constantPool.GetConstant(i);
            sb.AppendLine($"  #{i,4}: {FormatValue(value)}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// 反汇编整个程序（多个函数）
    /// </summary>
    public string DisassembleProgram(List<FunctionMetadata> functions)
    {
        var sb = new StringBuilder();

        // 显示常量池
        if (_constantPool != null)
        {
            sb.AppendLine(DisassembleConstantPool());
            sb.AppendLine();
        }

        // 反汇编每个函数
        for (int i = 0; i < functions.Count; i++)
        {
            if (i > 0)
                sb.AppendLine();
            sb.AppendLine("================================================================================");
            sb.AppendLine(DisassembleFunction(functions[i]));
        }

        return sb.ToString();
    }
}
