using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Bytecode.Metadata;
using Old8Lang.Error;

// ReSharper disable once CheckNamespace
namespace Old8Lang.Bytecode.VM;

/// <summary>
/// VirtualMachine - 值转换
/// </summary>
public partial class VirtualMachine
{
    private LangValueType ConvertToLangValue(object? value)
    {
        if (value == null) return new VoidLangValue();
        if (value is LangValueType langValue) return langValue;
        if (value is int intValue) return new IntLangValue(intValue);
        if (value is double doubleValue) return new DoubleLangValue(doubleValue);
        if (value is string stringValue) return new StringLangValue(stringValue);
        if (value is bool boolValue) return new BoolLangValue(boolValue);
        if (value is char charValue) return new CharLangValue(charValue);
        if (value is long longValue) return longValue is >= int.MinValue and <= int.MaxValue
            ? new IntLangValue((int)longValue)
            : new DoubleLangValue(longValue);
        if (value is short shortValue) return new IntLangValue(shortValue);
        if (value is byte byteValue) return new IntLangValue(byteValue);
        if (value is sbyte sbyteValue) return new IntLangValue(sbyteValue);
        if (value is ushort ushortValue) return new IntLangValue(ushortValue);
        if (value is uint uintValue) return uintValue <= int.MaxValue
            ? new IntLangValue((int)uintValue)
            : new DoubleLangValue(uintValue);
        if (value is ulong ulongValue) return ulongValue <= int.MaxValue
            ? new IntLangValue((int)ulongValue)
            : new DoubleLangValue(ulongValue);
        if (value is float floatValue) return new DoubleLangValue(floatValue);
        return new VoidLangValue();
    }

    /// <summary>
    /// 展平元组为列表（用于 match 表达式的元组解构）
    /// 例如：((1, 2), 3) -> [1, 2, 3]
    /// </summary>

    private List<object?> FlattenTupleHelper(TupleLangValue tuple)
    {
        return tuple.GetItems().Cast<object?>().ToList();
    }

    /// <summary>
    /// 尝试调用 BytecodeObjectInstance 的运算符重载方法
    /// </summary>

    private object? TryCallOperatorMethod(BytecodeObjectInstance obj, string methodName, object? operand)
    {
        // 查找类的元数据
        var classMetadata = _bytecodeFile.Classes.FirstOrDefault(c => c.Name == obj.ClassName);
        if (classMetadata == null)
            return null;

        // 查找方法
        var method = classMetadata.Methods.FirstOrDefault(m => m.Name == methodName);
        if (method == null)
            return null;

        // 调用方法（第一个参数是 this，第二个参数是操作数）
        var args = new object?[] { obj, operand };
        return ExecuteFunctionAndGetResult(method.Function, args);
    }

    /// <summary>
    /// 将虚拟机栈上的值转换为 LangValueType
    /// </summary>

    private LangValueType ConvertToLangValueType(object? value)
    {
        return value switch
        {
            null => new NullLangValue(),
            int i => new IntLangValue(i),
            long l => l is >= int.MinValue and <= int.MaxValue
                ? new IntLangValue((int)l)
                : new DoubleLangValue(l),
            short s => new IntLangValue(s),
            byte b => new IntLangValue(b),
            sbyte sb => new IntLangValue(sb),
            ushort us => new IntLangValue(us),
            uint ui => ui <= int.MaxValue
                ? new IntLangValue((int)ui)
                : new DoubleLangValue(ui),
            ulong ul => ul <= int.MaxValue
                ? new IntLangValue((int)ul)
                : new DoubleLangValue(ul),
            float f => new DoubleLangValue(f),
            double d => new DoubleLangValue(d),
            string s => new StringLangValue(s),
            bool b => new BoolLangValue(b),
            char c => new CharLangValue(c),
            LangValueType lvt => lvt,
            _ => throw new CastError(new SourcePosition(), value.GetType().Name, "LangValueType")
        };
    }
}
