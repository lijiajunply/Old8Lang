using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.InstanceMethods.Core;

namespace Old8Lang.InstanceMethods.Implementations.ValueType;

/// <summary>
/// LangValueType 通用实例方法基类
/// 所有 LangValueType 类型都可以使用这些方法
/// </summary>
public abstract class BaseValueTypeMethod : BaseInstanceMethod
{
    /// <summary>
    /// 目标类型为 LangValueType 基类
    /// </summary>
    public override Type TargetType => typeof(LangValueType);

    /// <summary>
    /// 将 VM 模式下的 C# 原始类型转换为 LangValueType
    /// </summary>
    protected LangValueType ConvertToLangValueType(object? value)
    {
        return value switch
        {
            null => new NullLangValue(),
            LangValueType langValue => langValue,
            int i => new IntLangValue(i),
            long l => new IntLangValue((int)l),
            double d => new DoubleLangValue(d),
            bool b => new BoolLangValue(b),
            char c => new CharLangValue(c),
            string s => new StringLangValue(s),
            _ => throw new ArgumentException($"无法将类型 {value.GetType().Name} 转换为 LangValueType")
        };
    }

    /// <summary>
    /// 将 LangValueType 转换为 VM 模式下的 C# 原始类型
    /// </summary>
    protected object? ConvertFromLangValueType(LangValueType value)
    {
        return value switch
        {
            IntLangValue intValue => (long)intValue.Value,
            DoubleLangValue doubleValue => doubleValue.Value,
            BoolLangValue boolValue => boolValue.Value,
            CharLangValue charValue => charValue.Value,
            StringLangValue stringValue => stringValue.Value,
            NullLangValue => null,
            _ => value
        };
    }
}
