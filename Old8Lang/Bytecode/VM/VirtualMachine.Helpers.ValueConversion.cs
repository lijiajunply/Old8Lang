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
            long l => new IntLangValue((int)l), // 将 long 转换为 int（如果溢出会在运行时报错）
            double d => new DoubleLangValue(d),
            string s => new StringLangValue(s),
            bool b => new BoolLangValue(b),
            char c => new CharLangValue(c),
            LangValueType lvt => lvt,
            _ => throw new CastError(new SourcePosition(), value.GetType().Name, "LangValueType")
        };
    }

    /// <summary>
    /// 调用类型的扩展方法或实例方法（类似于解释器模式中的 FromClassToResult）
    /// </summary>
    /// <param name="obj">要调用方法的对象</param>
    /// <param name="methodName">方法名</param>
    /// <param name="args">方法参数</param>
    /// <returns>方法返回值</returns>

}
