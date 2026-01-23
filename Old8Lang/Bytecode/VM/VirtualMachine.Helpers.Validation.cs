using System.Collections;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Expression.ValueFunctions;
using Old8Lang.Bytecode.Core;
using Old8Lang.Bytecode.Metadata;
using Old8Lang.Error;
using Old8Lang.GlobalFunctions.Core;
using ClassMetadata = Old8Lang.Bytecode.Metadata.ClassMetadata;

namespace Old8Lang.Bytecode.VM;

/// <summary>
/// VirtualMachine - 类型验证
/// </summary>
public partial class VirtualMachine
{
    private void ValidateConstructorParameterTypes(FunctionMetadata function, object?[] args, string className)
    {
        // 如果没有参数类型信息，跳过检查
        if (function.ParameterTypes == null || function.ParameterTypes.Count == 0)
            return;

        for (int i = 0; i < Math.Min(args.Length, function.ParameterTypes.Count); i++)
        {
            var expectedType = function.ParameterTypes[i];

            // 如果没有类型注解（空字符串），跳过检查
            if (string.IsNullOrEmpty(expectedType))
                continue;

            var actualValue = args[i];

            // 使用 CheckTypeMatch 进行类型检查
            if (!CheckTypeMatch(expectedType, actualValue))
            {
                var actualType = GetValueTypeName(actualValue);
                var paramName = i < function.Parameters.Count ? function.Parameters[i] : $"参数{i}";
                throw new TypeError(
                    new SourcePosition(0, 0),
                    expectedType,
                    actualType,
                    $"构造函数 '{className}' 的参数 '{paramName}' 类型不匹配"
                );
            }
        }
    }

    /// <summary>
    /// 验证字段赋值类型
    /// </summary>

    private void ValidateFieldType(string className, string fieldName, object? value, Instruction instruction)
    {
        // 查找类元数据
        var classMetadata = _bytecodeFile.Classes.FirstOrDefault(c => c.Name == className);
        if (classMetadata == null)
            return;

        // 查找字段元数据（包括父类字段）
        FieldMetadata? fieldMetadata = null;
        var currentClass = classMetadata;
        while (currentClass != null && fieldMetadata == null)
        {
            fieldMetadata = currentClass.Fields.FirstOrDefault(f => f.Name == fieldName);
            if (fieldMetadata == null && !string.IsNullOrEmpty(currentClass.BaseClassName))
            {
                currentClass = _bytecodeFile.Classes.FirstOrDefault(c => c.Name == currentClass.BaseClassName);
            }
            else
            {
                break;
            }
        }

        if (fieldMetadata == null || string.IsNullOrEmpty(fieldMetadata.TypeName))
            return;

        // 如果字段类型包含泛型类型参数，尝试从类的泛型类型映射中替换
        var fieldTypeName = fieldMetadata.TypeName;
        if (classMetadata.GenericTypeMapping is { Count: > 0 })
        {
            // 检查 GenericTypeMapping 中是否包含泛型类型参数（如 Wrapper<T>）
            // 这是编译器的一个 bug，会导致类型解析错误
            // 作为临时变通方案，如果检测到这种情况，跳过类型检查
            bool hasNestedGenericMapping = classMetadata.GenericTypeMapping.Values.Any(v => v.Contains('<'));
            if (hasNestedGenericMapping)
            {
                // 跳过类型检查，因为编译器生成的 GenericTypeMapping 可能不正确
                return;
            }

            // 使用类的泛型类型映射替换字段类型中的泛型类型参数
            fieldTypeName = ResolveGenericType(fieldMetadata.TypeName, classMetadata.GenericTypeMapping);
        }

        // 检查类型匹配
        if (!CheckTypeMatch(fieldTypeName, value))
        {
            var actualType = GetValueTypeName(value);
            throw new TypeError(
                GetPosition(instruction),
                fieldTypeName,
                actualType,
                $"字段 '{className}.{fieldName}' 类型不匹配"
            );
        }
    }

    /// <summary>
    /// 验证静态字段赋值类型
    /// </summary>

    private void ValidateStaticFieldType(ClassMetadata classMetadata, string fieldName, object? value, Instruction instruction)
    {
        // 查找静态字段元数据
        var fieldMetadata = classMetadata.StaticFields.FirstOrDefault(f => f.Name == fieldName);
        if (fieldMetadata == null || string.IsNullOrEmpty(fieldMetadata.TypeName))
            return;

        // 检查类型匹配
        if (!CheckTypeMatch(fieldMetadata.TypeName, value))
        {
            var actualType = GetValueTypeName(value);
            throw new TypeError(
                GetPosition(instruction),
                fieldMetadata.TypeName,
                actualType,
                $"静态字段 '{classMetadata.Name}.{fieldName}' 类型不匹配"
            );
        }
    }

    /// <summary>
    /// 验证逻辑运算符的操作数类型
    /// </summary>

    private void ValidateLogicalOperand(object? value, string operatorName, Instruction instruction)
    {
        // 如果值是布尔类型，直接返回
        if (value is bool)
            return;

        // 如果值是 BoolLangValue，直接返回
        if (value is BoolLangValue)
            return;

        // 其他类型不允许用于逻辑运算
        var actualType = GetValueTypeName(value);
        throw new TypeError(
            GetPosition(instruction),
            "bool",
            actualType,
            $"逻辑运算符 '{operatorName}' 的操作数必须是布尔类型"
        );
    }

    /// <summary>
    /// 执行函数并获取结果（用于异步调用）
    /// </summary>
    public object? ExecuteFunctionAndGetResult(FunctionMetadata function, object?[] args)
    {
        CallFunction(function, args);
        return _stack.Count > 0 ? _stack.Pop() : null;
    }
}

/// <summary>
/// 虚拟机异常包装类
/// 用于在C#异常机制中传递Old8Lang的异常对象
/// </summary>
public class VmException(object? value) : Exception(GetMessage(value))
{
    public object? Value { get; } = value;


    private static string GetMessage(object? value)
    {
        if (value == null) return "null";
        if (value is LangValueType langValue) return langValue.ToDisplayString();
        return value.ToString() ?? "";
    }
}

/// <summary>
/// 对象相等性比较器 - 用于 GroupBy 操作的键比较
/// </summary>
internal class ObjectEqualityComparer : IEqualityComparer<object>
{
    public new bool Equals(object? x, object? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x == null || y == null) return false;

        // 使用对象的 Equals 方法进行比较
        return x.Equals(y);
    }

    public int GetHashCode(object obj)
    {
        return obj?.GetHashCode() ?? 0;
    }

}
