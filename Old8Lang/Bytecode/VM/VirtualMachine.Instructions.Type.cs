using System.Collections;
using System.Reflection;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.Bytecode.Core;
using Old8Lang.Bytecode.Closures;
using Old8Lang.Bytecode.Generators;
using Old8Lang.Bytecode.Interop;
using Old8Lang.Bytecode.Metadata;
using Old8Lang.Error;
using Old8Lang.GlobalFunctions.Core;
using ClassMetadata = Old8Lang.Bytecode.Metadata.ClassMetadata;

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
                var value = _stack.Pop();

                // 1. 如果类型完全匹配（包括泛型检查），直接返回原值
                if (CheckTypeMatch(targetTypeName, value))
                {
                    _stack.Push(value);
                    break;
                }

                try
                {
                    // 执行类型转换
                    object? convertedValue = targetTypeName.ToLower() switch
                    {
                        "int" => value == null
                            ? throw new InvalidCastException("Cannot cast null to int")
                            : Convert.ToInt32(value),
                        "double" => value == null
                            ? throw new InvalidCastException("Cannot cast null to double")
                            : Convert.ToDouble(value),
                        "string" => value?.ToString() ?? "",
                        "bool" => Convert.ToBoolean(value),
                        "char" => Convert.ToChar(value),
                        "list" => ConvertToList(value),
                        "array" => ConvertToArray(value),
                        "dict" => ConvertToDict(value),
                        _ => value // 其他类型直接返回原值
                    };
                    _stack.Push(convertedValue);
                }
                catch (Exception ex)
                {
                    throw new CastError(GetPosition(instruction), value?.GetType().Name ?? "null", targetTypeName,
                        ex.Message);
                }
            }
                break;

            case OpCode.IsType:
            {
                var targetTypeName = (string)instruction.Operand!;
                var value = _stack.Pop();
                _stack.Push(CheckTypeMatch(targetTypeName, value));
            }
                break;

            case OpCode.TypeOf:
            {
                var value = _stack.Pop();
                string typeName;

                if (value == null)
                {
                    typeName = "null";
                }
                else if (value is int)
                {
                    typeName = "int";
                }
                else if (value is double)
                {
                    typeName = "double";
                }
                else if (value is string)
                {
                    typeName = "string";
                }
                else if (value is bool)
                {
                    typeName = "bool";
                }
                else if (value is char)
                {
                    typeName = "char";
                }
                else if (value is Array)
                {
                    typeName = "array";
                }
                else if (value is IList)
                {
                    typeName = "list";
                }
                else if (value is IDictionary)
                {
                    typeName = "dict";
                }
                else
                {
                    typeName = value.GetType().Name;
                }

                _stack.Push(typeName);
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
                _globals[enumName] = enumTemplate;
            }
                break;

            case OpCode.DefineInterface:
            case OpCode.DefineMixin:
            case OpCode.ApplyMixin:
            case OpCode.CheckInterface:
                // 接口和Mixin在编译时处理，运行时不需要额外操作
                // 这些指令主要用于元数据记录和类型检查
                break;

        }
    }
}
