using System.Collections;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Bytecode.Core;
using Old8Lang.Error;

// ReSharper disable once CheckNamespace
namespace Old8Lang.Bytecode.VM;

public partial class VirtualMachine
{
    /// <summary>
    /// 执行类型操作指令
    /// </summary>
    private void ExecuteTypeOperation(Instruction instruction, CallFrame frame)
    {
        switch (instruction.OpCode)
        {
            case OpCode.Cast:
            {
                var targetTypeName = (string)instruction.Operand!;
                var vmValue = Stack.Pop();

                // 1. 如果类型完全匹配（包括泛型检查），直接返回原值
                if (CheckTypeMatch(targetTypeName, vmValue))
                {
                    Stack.Push(vmValue);
                    break;
                }

                var value = vmValue.ToObject();
                try
                {
                    // 语言值（EnumLangValue / IntLangValue 等）先取出底层原生值，
                    // 否则 Convert.ToInt32(EnumLangValue) 会因不实现 IConvertible 抛 CastError。
                    var rawValue = value switch
                    {
                        EnumLangValue enumValue => enumValue.Value,
                        IntLangValue intValue => intValue.Value,
                        DoubleLangValue doubleValue => doubleValue.Value,
                        BoolLangValue boolValue => boolValue.Value,
                        CharLangValue charValue => charValue.Value,
                        StringLangValue stringValue => stringValue.Value,
                        _ => value
                    };

                    // 执行类型转换
                    object? convertedValue = targetTypeName.ToLower() switch
                    {
                        "int" => value == null
                            ? throw new InvalidCastException("Cannot cast null to int")
                            : Convert.ToInt32(rawValue),
                        "double" => value == null
                            ? throw new InvalidCastException("Cannot cast null to double")
                            : Convert.ToDouble(rawValue),
                        "string" => rawValue?.ToString() ?? "",
                        "bool" => Convert.ToBoolean(rawValue),
                        "char" => Convert.ToChar(rawValue),
                        "list" => ConvertToList(value),
                        "array" => ConvertToArray(value),
                        "dict" => ConvertToDict(value),
                        _ when value is IDictionary or DictionaryLangValue &&
                               TryResolveClassByName(targetTypeName, out var targetClass) =>
                            ConvertDictionaryToObject(
                                value is DictionaryLangValue dictionary ? UnwrapDictionary(dictionary) : (IDictionary)value,
                                targetClass, instruction),
                        _ => value // 其他类型直接返回原值
                    };
                    Stack.PushObject(convertedValue);
                }
                catch (Exception ex)
                {
                    throw new CastError(GetPosition(instruction), DescribeValueType(value), targetTypeName,
                        ex is Old8Exception error ? error.OriginalMessage : ex.Message);
                }
            }
                break;

            case OpCode.IsType:
            {
                var targetTypeName = (string)instruction.Operand!;
                var value = Stack.Pop();
                Stack.PushBool(CheckTypeMatch(targetTypeName, value));
            }
                break;

            case OpCode.TypeOf:
            {
                var vmValue = Stack.Pop();
                string typeName = vmValue.Kind switch
                {
                    VmValueKind.Null => "null",
                    VmValueKind.Int => "int",
                    VmValueKind.Double => "double",
                    VmValueKind.Bool => "bool",
                    VmValueKind.Char => "char",
                    _ => vmValue.ToObject() switch
                    {
                        null => "null",
                        string => "string",
                        Array => "array",
                        IList => "list",
                        IDictionary => "dict",
                        var obj => DescribeValueType(obj)
                    }
                };

                Stack.PushRef(typeName);
            }
                break;

            case OpCode.DefineEnum:
            {
                // 操作数格式: [enumNameIndex, memberCount, memberDataIndex]
                var operands = (object[])instruction.Operand!;
                var enumNameIndex = Convert.ToInt32(operands[0]);
                var memberCount = Convert.ToInt32(operands[1]);
                var memberDataIndex = Convert.ToInt32(operands[2]);

                // 从常量池获取枚举名称
                var enumName = (string)_bytecodeFile.ConstantPool.GetConstant(enumNameIndex);

                // 从常量池获取成员数据
                var memberData = (object[])_bytecodeFile.ConstantPool.GetConstant(memberDataIndex);

                // 构建成员字典
                var members = new Dictionary<string, int>();
                for (int i = 0; i < memberCount; i++)
                {
                    var memberName = (string)memberData[i * 2];
                    var memberValue = Convert.ToInt32(memberData[i * 2 + 1]);
                    members[memberName] = memberValue;
                }

                // 创建枚举模板
                var enumTemplate = new EnumTemplate(
                    enumName,
                    members);

                // 将枚举模板存储到全局变量
                SetGlobalValue(enumName, enumTemplate);
            }
                break;

            case OpCode.DefineInterface:
            case OpCode.DefineMixin:
            case OpCode.ApplyMixin:
            case OpCode.CheckInterface:
                break;
        }
    }
}
